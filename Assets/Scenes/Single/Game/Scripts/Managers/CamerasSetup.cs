using Serilog;
using Unity.Cinemachine;
using UnityEngine;

public class TankCameraSetup : MonoBehaviour
{
    [Header("Virtual Cameras")]
    public CinemachineCamera mainCameraVC;
    public CinemachineCamera commanderCameraVC;

    [HideInInspector]
    public GameObject playerTank;

    public void InitializeCameras()
    {
        if (playerTank == null)
        {
            Debug.LogError("[TankCameraSetup] Player tank not assigned!");
            return;
        }

        if (mainCameraVC == null)
            mainCameraVC = GameObject.FindWithTag("MainCameraVC")?.GetComponent<CinemachineCamera>();
        if (commanderCameraVC == null)
            commanderCameraVC = GameObject.FindWithTag("CommanderCameraVC")?.GetComponent<CinemachineCamera>();

        if (mainCameraVC == null || commanderCameraVC == null)
        {
            Debug.LogError("[TankCameraSetup] One or both virtual cameras not found by tag!");
            return;
        }

        string baseTankName = playerTank.name.Replace("(Clone)", "").Trim();
        Log.Debug("playerTank base name: {baseTankName}", baseTankName);

        Transform mainPivot = playerTank.transform.Find(baseTankName + "_main_pivot");
        Transform commanderPivot = playerTank.transform.Find(baseTankName + "_commander_pivot");

        Log.Debug("mainPivot name: {mainPivot}", mainPivot);
        Log.Debug("commanderPivot name: {commanderPivot}", commanderPivot);
        if (mainPivot != null)
        {
            mainCameraVC.Follow = mainPivot;
            mainCameraVC.LookAt = mainPivot;
        }
        else
        {
            Debug.LogWarning("[TankCameraSetup] Main pivot not found!");
        }

        if (commanderPivot != null)
        {
            commanderCameraVC.Follow = commanderPivot;
            commanderCameraVC.LookAt = commanderPivot;
        }
        else
        {
            Debug.LogWarning("[TankCameraSetup] Commander pivot not found!");
        }
    }

}
