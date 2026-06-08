using System;
using Unity.Netcode;
using UnityEngine;

public class TagPlayerState : NetworkBehaviour
{
    private readonly NetworkVariable<bool> isIt = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public bool IsIt => isIt.Value;

    public event Action<bool> ItStateChanged;

    public override void OnNetworkSpawn()
    {
        isIt.OnValueChanged += OnItValueChanged;

        if (IsServer)
            TagGameManager.NotifyPlayerSpawned(this);

        ItStateChanged?.Invoke(isIt.Value);
    }

    public override void OnNetworkDespawn()
    {
        isIt.OnValueChanged -= OnItValueChanged;

        if (IsServer)
            TagGameManager.NotifyPlayerDespawned(this);
    }

    public void SetIsItServer(bool value)
    {
        if (!IsServer)
            return;

        isIt.Value = value;
    }

    private void OnItValueChanged(bool oldValue, bool newValue)
    {
        ItStateChanged?.Invoke(newValue);
    }
}