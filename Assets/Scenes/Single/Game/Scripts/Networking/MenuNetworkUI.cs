using UnityEngine;
using TMPro;
using Unity.Netcode;

public class MenuNetworkUI : MonoBehaviour
{
    public TMP_InputField ipInput;

    public void OnHostButton()
    {
        PersistentNetworkManager.Instance.StartHost();

        NetworkManager.Singleton.SceneManager.LoadScene("Level1_coop", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    public void OnJoinButton()
    {
        string ip = ipInput.text.Trim();
        if (string.IsNullOrEmpty(ip)) ip = "127.0.0.1";

        PersistentNetworkManager.Instance.StartClient(ip);

    }
}
