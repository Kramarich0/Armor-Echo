using UnityEngine;

public class GameNetUIManager : MonoBehaviour
{
    public static GameNetUIManager Instance { get; private set; }

    [Header("UI / Camera (drag in inspector)")]
    public Camera mainCamera;
    public GameObject crosshairAimUI;
    public GameObject crosshairSniperUI;
    public GameObject sniperVignette;

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
}
