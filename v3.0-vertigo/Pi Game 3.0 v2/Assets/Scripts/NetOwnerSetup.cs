using Unity.Netcode;
using UnityEngine;

public class NetOwnerSetup : NetworkBehaviour
{
    [Header("Assign in prefab")]
    [SerializeField] private GameObject cameraRoot; // your camera child (with AudioListener)

    private PlayerInputHandler inputHandler;
    private PlayerLook playerLook;
    private PlayerMovement pm;
    private AdvancedMovement am;
    private Booster booster;
    private ODMGear odm;

    private void Awake()
    {
        inputHandler = GetComponent<PlayerInputHandler>();
        playerLook = GetComponentInChildren<PlayerLook>(true);
        pm = GetComponent<PlayerMovement>();
        am = GetComponent<AdvancedMovement>();
        booster = GetComponent<Booster>();
        odm = GetComponent<ODMGear>();
    }

    private void SetLocalViewActive(bool active)
    {
        if (cameraRoot != null && cameraRoot.transform.IsChildOf(transform))
            cameraRoot.SetActive(active);

        var cameras = GetComponentsInChildren<Camera>(true);
        for (int i = 0; i < cameras.Length; i++)
            cameras[i].enabled = active;

        var listeners = GetComponentsInChildren<AudioListener>(true);
        for (int i = 0; i < listeners.Length; i++)
            listeners[i].enabled = active;
    }

    public override void OnNetworkSpawn()
    {
        ApplyOwnerState(IsOwner);
    }

    public override void OnGainedOwnership()
    {
        ApplyOwnerState(true);
    }

    public override void OnLostOwnership()
    {
        ApplyOwnerState(false);
    }

    private void ApplyOwnerState(bool owner)
    {
        SetLocalViewActive(owner);

        if (pm != null) pm.enabled = owner;
        if (am != null) am.enabled = owner;
        if (booster != null) booster.enabled = owner;
        if (odm != null) odm.enabled = owner;
        if (playerLook != null) playerLook.enabled = owner;

        if (inputHandler == null)
            return;

        inputHandler.enabled = owner;
    }

    public void ApplyServerTeleport(Vector3 position, Quaternion rotation)
    {
        if (!IsServer)
            return;

        TeleportLocally(position, rotation);

        TeleportOwnerClientRpc(
            position,
            rotation,
            new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { OwnerClientId }
                }
            }
        );
    }

    [ClientRpc]
    private void TeleportOwnerClientRpc(Vector3 position, Quaternion rotation, ClientRpcParams clientRpcParams = default)
    {
        if (!IsOwner)
            return;

        TeleportLocally(position, rotation);
    }

    private void TeleportLocally(Vector3 position, Quaternion rotation)
    {
        CharacterController characterController = GetComponent<CharacterController>();
        bool hadController = characterController != null;
        bool controllerEnabled = hadController && characterController.enabled;

        if (controllerEnabled)
            characterController.enabled = false;

        transform.SetPositionAndRotation(position, rotation);

        if (controllerEnabled)
            characterController.enabled = true;
    }
}