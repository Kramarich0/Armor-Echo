using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class PersistentNetworkManager : MonoBehaviour
{
    public static PersistentNetworkManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
    }

    public void StartClient(string ipAddress)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData(ipAddress, 7778);
        NetworkManager.Singleton.StartClient();
    }

    public void StopAll()
    {
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient)
            NetworkManager.Singleton.Shutdown();
    }
}
