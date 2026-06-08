using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(TagPlayerState))]
public class TagCollision : NetworkBehaviour
{
    [SerializeField] private float serverValidationDistance = 3f;
    [SerializeField] private bool useProximityFallback = true;
    [SerializeField] private float proximityRadius = 1.3f;
    [SerializeField] private float proximityCheckInterval = 0.1f;
    [SerializeField] private LayerMask proximityMask = ~0;

    private TagPlayerState playerState;
    private float nextProximityCheckTime;
    private readonly Collider[] overlapBuffer = new Collider[32];

    private void Awake()
    {
        playerState = GetComponent<TagPlayerState>();
    }

    private void Update()
    {
        if (!useProximityFallback)
            return;

        if (!ShouldCheckForTag())
            return;

        if (Time.time < nextProximityCheckTime)
            return;

        nextProximityCheckTime = Time.time + proximityCheckInterval;
        TryProximityTransfer();
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!ShouldCheckForTag())
            return;

        if (hit.collider == null)
            return;

        TryRequestTransfer(hit.collider.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!ShouldCheckForTag())
            return;

        if (collision.collider == null)
            return;

        TryRequestTransfer(collision.collider.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!ShouldCheckForTag())
            return;

        if (other == null)
            return;

        TryRequestTransfer(other.gameObject);
    }

    private bool ShouldCheckForTag()
    {
        if (!IsSpawned || !IsOwner)
            return false;

        if (playerState == null)
            return false;

        if (!playerState.IsIt)
            return false;

        return true;
    }

    private void TryRequestTransfer(GameObject otherObject)
    {
        if (otherObject == null)
            return;

        TagPlayerState otherState = otherObject.GetComponentInParent<TagPlayerState>();
        if (otherState == null || !otherState.IsSpawned)
            return;

        if (otherState == playerState)
            return;

        RequestTransferServerRpc(otherState.NetworkObjectId);
    }

    private void TryProximityTransfer()
    {
        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            proximityRadius,
            overlapBuffer,
            proximityMask,
            QueryTriggerInteraction.Collide
        );

        TagPlayerState bestTarget = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider overlap = overlapBuffer[i];
            overlapBuffer[i] = null;

            if (overlap == null)
                continue;

            TagPlayerState otherState = overlap.GetComponentInParent<TagPlayerState>();
            if (otherState == null || !otherState.IsSpawned)
                continue;

            if (otherState == playerState)
                continue;

            float distance = Vector3.SqrMagnitude(otherState.transform.position - transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestTarget = otherState;
            }
        }

        if (bestTarget != null)
            RequestTransferServerRpc(bestTarget.NetworkObjectId);
    }

    [ServerRpc]
    private void RequestTransferServerRpc(ulong targetNetworkObjectId)
    {
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.SpawnManager == null)
            return;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out NetworkObject targetObject))
            return;

        if (targetObject == null)
            return;

        TagPlayerState targetState = targetObject.GetComponent<TagPlayerState>();
        if (targetState == null || !targetState.IsSpawned)
            return;

        if (playerState == null || !playerState.IsSpawned)
            return;

        float distance = Vector3.Distance(playerState.transform.position, targetState.transform.position);
        if (distance > serverValidationDistance)
            return;

        TagGameManager.TryTransfer(playerState, targetState);
    }
}