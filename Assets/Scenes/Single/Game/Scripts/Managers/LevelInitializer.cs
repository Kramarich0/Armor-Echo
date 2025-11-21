using UnityEngine;

public class LevelInitializer : MonoBehaviour
{
    void Start()
    {
        GameManager.Instance?.InitializeLevel();
    }
}