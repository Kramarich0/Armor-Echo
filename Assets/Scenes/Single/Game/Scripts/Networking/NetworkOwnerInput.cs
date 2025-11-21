using Unity.Netcode;
using UnityEngine;

public class NetworkOwnerInput : NetworkBehaviour
{
    void Update()
    {
        if (IsSpawned && !IsOwner)
        {
            if (TryGetComponent<TankMovement>(out var movement))
                movement.enabled = false;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (TryGetComponent<TankMovement>(out var movement))
            movement.enabled = true;
    }
}