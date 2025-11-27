using Unity.Netcode;
using UnityEngine;

public class NetworkOwnerInput : NetworkBehaviour
{
    void Update()
    {
        if (IsSpawned && !IsOwner)
        {
            if (TryGetComponent<Tank>(out var tank))
                tank.SetMovementEnabled(false);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (TryGetComponent<Tank>(out var tank))
            tank.SetMovementEnabled(true);
    }

}