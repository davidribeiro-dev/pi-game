using System.Collections;
using System.Collections.Generic;
using System;
using Unity.Netcode;
using UnityEngine;

public class ScenePlayerSpawner : MonoBehaviour
{
    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Fall Respawn")]
    [SerializeField] private float fallYThreshold = -80f;
    [SerializeField] private float respawnCooldownSeconds = 0.5f;

    [Header("Timing")]
    [SerializeField] private float waitForPlayersTimeoutSeconds = 10f;
    [SerializeField] private float checkIntervalSeconds = 0.1f;

    private bool hasTeleported;
    private readonly Dictionary<ulong, Transform> assignedSpawnByClient = new Dictionary<ulong, Transform>();
    private readonly Dictionary<ulong, float> lastRespawnTimeByClient = new Dictionary<ulong, float>();
    private int nextSpawnIndex;

    private void Start()
    {
        StartCoroutine(TeleportPlayersWhenReady());
    }

    private void Update()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsServer)
            return;

        if (spawnPoints == null || spawnPoints.Length == 0)
            return;

        IReadOnlyList<NetworkClient> clients = networkManager.ConnectedClientsList;
        for (int i = 0; i < clients.Count; i++)
        {
            NetworkClient client = clients[i];
            if (client.PlayerObject == null || !client.PlayerObject.IsSpawned)
                continue;

            if (client.PlayerObject.transform.position.y > fallYThreshold)
                continue;

            if (lastRespawnTimeByClient.TryGetValue(client.ClientId, out float lastRespawnTime))
            {
                if (Time.time < lastRespawnTime + respawnCooldownSeconds)
                    continue;
            }

            Transform spawnPoint = GetOrAssignSpawn(client.ClientId);
            if (spawnPoint == null)
                continue;

            TeleportPlayer(client.PlayerObject, spawnPoint);
            lastRespawnTimeByClient[client.ClientId] = Time.time;
        }
    }

    private IEnumerator TeleportPlayersWhenReady()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsServer)
            yield break;

        if (hasTeleported)
            yield break;

        hasTeleported = true;

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("ScenePlayerSpawner has no spawn points assigned.");
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < waitForPlayersTimeoutSeconds)
        {
            if (AllConnectedPlayersReady(networkManager))
                break;

            yield return new WaitForSeconds(checkIntervalSeconds);
            elapsed += checkIntervalSeconds;
        }

        TeleportAllConnectedPlayers(networkManager);
    }

    private static bool AllConnectedPlayersReady(NetworkManager networkManager)
    {
        IReadOnlyList<NetworkClient> clients = networkManager.ConnectedClientsList;
        if (clients == null || clients.Count == 0)
            return false;

        for (int i = 0; i < clients.Count; i++)
        {
            NetworkObject playerObject = clients[i].PlayerObject;
            if (playerObject == null || !playerObject.IsSpawned)
                return false;
        }

        return true;
    }

    private void TeleportAllConnectedPlayers(NetworkManager networkManager)
    {
        List<ulong> clientIds = new List<ulong>();
        IReadOnlyList<NetworkClient> clients = networkManager.ConnectedClientsList;

        for (int i = 0; i < clients.Count; i++)
            clientIds.Add(clients[i].ClientId);

        clientIds.Sort();

        for (int i = 0; i < clientIds.Count; i++)
        {
            ulong clientId = clientIds[i];
            if (!networkManager.ConnectedClients.TryGetValue(clientId, out NetworkClient client))
                continue;

            if (client.PlayerObject == null)
                continue;

            Transform spawnPoint = spawnPoints[i % spawnPoints.Length];
            assignedSpawnByClient[clientId] = spawnPoint;
            TeleportPlayer(client.PlayerObject, spawnPoint);
            lastRespawnTimeByClient[clientId] = Time.time;
            nextSpawnIndex = i + 1;
        }
    }

    private Transform GetOrAssignSpawn(ulong clientId)
    {
        if (assignedSpawnByClient.TryGetValue(clientId, out Transform existing) && existing != null)
            return existing;

        if (spawnPoints == null || spawnPoints.Length == 0)
            return null;

        Transform assigned = spawnPoints[nextSpawnIndex % spawnPoints.Length];
        nextSpawnIndex++;
        assignedSpawnByClient[clientId] = assigned;
        return assigned;
    }

    private static void TeleportPlayer(NetworkObject playerObject, Transform spawnPoint)
    {
        if (playerObject == null || spawnPoint == null)
            return;

        NetOwnerSetup ownerSetup = playerObject.GetComponent<NetOwnerSetup>();
        if (ownerSetup != null)
        {
            ownerSetup.ApplyServerTeleport(spawnPoint.position, spawnPoint.rotation);
            return;
        }

        CharacterController characterController = playerObject.GetComponent<CharacterController>();
        bool controllerEnabled = characterController != null && characterController.enabled;

        if (controllerEnabled)
            characterController.enabled = false;

        playerObject.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);

        if (controllerEnabled)
            characterController.enabled = true;
    }
}
