using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TagGameManager : MonoBehaviour
{
    [SerializeField] private bool hostStartsAsIt = true;
    [SerializeField] private float reTagCooldownSeconds = 1f;

    private static TagGameManager instance;

    private readonly List<TagPlayerState> activePlayers = new List<TagPlayerState>();
    private TagPlayerState currentIt;
    private double lastTransferServerTime = -999d;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private static bool TryGetInstance(out TagGameManager manager)
    {
        manager = instance;
        if (manager != null)
            return true;

        manager = FindFirstObjectByType<TagGameManager>();
        if (manager == null)
        {
            Debug.LogError("TagGameManager not found in scene. Add it to a regular scene GameObject.");
            return false;
        }

        instance = manager;
        return true;
    }

    public static void NotifyPlayerSpawned(TagPlayerState player)
    {
        if (!TryGetInstance(out TagGameManager manager) || player == null)
            return;

        manager.RegisterPlayer(player);
    }

    public static void NotifyPlayerDespawned(TagPlayerState player)
    {
        if (!TryGetInstance(out TagGameManager manager) || player == null)
            return;

        manager.UnregisterPlayer(player);
    }

    public static bool TryTransfer(TagPlayerState sourceIt, TagPlayerState target)
    {
        if (!TryGetInstance(out TagGameManager manager))
            return false;

        return manager.TryTransferInternal(sourceIt, target);
    }

    private bool CooldownActive
    {
        get
        {
            if (NetworkManager.Singleton == null)
                return true;

            return NetworkManager.Singleton.ServerTime.Time < lastTransferServerTime + reTagCooldownSeconds;
        }
    }

    private void RegisterPlayer(TagPlayerState player)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer || player == null)
            return;

        if (!activePlayers.Contains(player))
            activePlayers.Add(player);

        EnsureCurrentItIsValid();

        if (currentIt != null)
            return;

        if (hostStartsAsIt)
        {
            TagPlayerState host = FindHostPlayer();
            if (host != null)
            {
                SetCurrentIt(host);
                return;
            }
        }

        AssignRandomIt();
    }

    private void UnregisterPlayer(TagPlayerState player)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer || player == null)
            return;

        activePlayers.Remove(player);

        if (player != currentIt)
            return;

        currentIt = null;
        AssignRandomIt();
    }

    private void EnsureCurrentItIsValid()
    {
        if (currentIt == null)
            return;

        if (!currentIt.IsSpawned || !activePlayers.Contains(currentIt))
            currentIt = null;
    }

    private TagPlayerState FindHostPlayer()
    {
        if (NetworkManager.Singleton == null)
            return null;

        ulong hostClientId = NetworkManager.ServerClientId;
        for (int i = 0; i < activePlayers.Count; i++)
        {
            TagPlayerState player = activePlayers[i];
            if (player == null || !player.IsSpawned)
                continue;

            if (player.OwnerClientId == hostClientId)
                return player;
        }

        return null;
    }

    private void AssignRandomIt()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;

        if (activePlayers.Count == 0)
        {
            ClearAllItStates();
            return;
        }

        int index = Random.Range(0, activePlayers.Count);
        SetCurrentIt(activePlayers[index]);
    }

    private void ClearAllItStates()
    {
        for (int i = 0; i < activePlayers.Count; i++)
        {
            TagPlayerState player = activePlayers[i];
            if (player == null || !player.IsSpawned)
                continue;

            player.SetIsItServer(false);
        }
    }

    private void SetCurrentIt(TagPlayerState nextIt)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;

        currentIt = nextIt;

        for (int i = 0; i < activePlayers.Count; i++)
        {
            TagPlayerState player = activePlayers[i];
            if (player == null || !player.IsSpawned)
                continue;

            player.SetIsItServer(player == currentIt);
        }
    }

    private bool TryTransferInternal(TagPlayerState sourceIt, TagPlayerState target)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return false;

        if (sourceIt == null || target == null)
            return false;

        if (!sourceIt.IsSpawned || !target.IsSpawned)
            return false;

        if (sourceIt == target)
            return false;

        EnsureCurrentItIsValid();
        if (currentIt == null)
        {
            AssignRandomIt();
            return false;
        }

        if (CooldownActive)
            return false;

        if (sourceIt != currentIt)
            return false;

        if (!sourceIt.IsIt || target.IsIt)
            return false;

        SetCurrentIt(target);
        lastTransferServerTime = NetworkManager.Singleton.ServerTime.Time;
        Debug.Log($"[TagGameManager] Transfer: {sourceIt.OwnerClientId} -> {target.OwnerClientId}");
        return true;
    }
}