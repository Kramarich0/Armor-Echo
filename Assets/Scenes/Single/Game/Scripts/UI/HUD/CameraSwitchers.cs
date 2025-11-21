using UnityEngine;
using Unity.Cinemachine;

public class CameraSwitcher : MonoBehaviour
{
    public CinemachineCamera[] cameras;
    private int currentCamIndex = 0;

    void Start()
    {
        for (int i = 0; i < cameras.Length; i++)
            cameras[i].Priority = (i == 0) ? 10 : 0;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            cameras[currentCamIndex].Priority = 0;

            currentCamIndex++;
            if (currentCamIndex >= cameras.Length) currentCamIndex = 0;

            cameras[currentCamIndex].Priority = 10;
        }
    }
}
