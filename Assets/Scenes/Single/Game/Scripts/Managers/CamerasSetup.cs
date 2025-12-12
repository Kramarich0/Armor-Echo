using System.Linq;
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
            Log.Error("[TankCameraSetup] Player tank not assigned!");
            return;
        }

        if (mainCameraVC == null)
            mainCameraVC = GameObject.FindWithTag("MainCameraVC")?.GetComponent<CinemachineCamera>();
        if (commanderCameraVC == null)
            commanderCameraVC = GameObject.FindWithTag("CommanderCameraVC")?.GetComponent<CinemachineCamera>();

        if (mainCameraVC == null || commanderCameraVC == null)
        {
            Log.Error("[TankCameraSetup] One or both virtual cameras not found by tag!");
            return;
        }

        string baseTankName = playerTank.name.Replace("(Clone)", "").Trim();
        Log.Debug("playerTank base name: {baseTankName}", baseTankName);

        Transform mainPivot = playerTank.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.CompareTag("MainPivot"));

        Transform commanderPivot = playerTank.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.CompareTag("CommanderPivot"));

        Log.Debug("mainPivot name: {mainPivot}", mainPivot);
        Log.Debug("commanderPivot name: {commanderPivot}", commanderPivot);
        if (mainPivot != null)
        {
            mainCameraVC.Follow = mainPivot;
            mainCameraVC.LookAt = mainPivot;
        }
        else
        {
            Log.Warning("[TankCameraSetup] Main pivot not found!");
        }

        if (commanderPivot != null)
        {
            commanderCameraVC.Follow = commanderPivot;
            commanderCameraVC.LookAt = commanderPivot;
        }
        else
        {
            Log.Warning("[TankCameraSetup] Commander pivot not found!");
        }
    }

}
