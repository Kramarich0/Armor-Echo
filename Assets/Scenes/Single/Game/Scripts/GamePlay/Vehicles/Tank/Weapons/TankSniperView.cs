using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

public class TankSniperView : MonoBehaviour
{
    [Header("Input")]
    public InputActionReference toggleViewAction;
    public InputActionReference zoomAction;

    [Header("References")]
    public Transform gunEnd;
    public Camera mainCamera;
    public Camera sniperCamera;
    public GameObject crosshairAimUI;
    public GameObject crosshairSniperUI;
    public GameObject sniperVignette;
    public TankShoot tankShoot;

    [Header("Sniper Settings (игровой зум)")]
    public float zoomStep = 2f;
    public float zoomSpeed = 8f;
    public float zoomFOVReduction = 15f;
    public float maxZoomOffset = 0.35f;
    public float positionOffset = -0.2f;

    [Header("Optional follow settings")]
    public float followSpeed = 10f;
    private bool isSniperView = false;
    private float zoomTarget = 0f;
    private float zoomCurrent = 0f;
    private float normalFOV;
    private Vector3 stableOffset;
    private float recoilDecay = 5f;
    private Vector3 currentRecoilPosition = Vector3.zero;
    private float currentRecoilAngle = 0f;
    private Vector3 targetRecoilPosition = Vector3.zero;
    private float targetRecoilAngle = 0f;

    public AudioMixer masterMixer;
    public string normalSnapshotName = "Normal";
    public string sniperSnapshotName = "Sniper";
    private AudioMixerSnapshot normalSnapshot;
    private AudioMixerSnapshot sniperSnapshot;

    void Start()
    {
        toggleViewAction.action?.Enable();
        zoomAction.action?.Enable();

        if (mainCamera != null) normalFOV = mainCamera.fieldOfView;
        crosshairAimUI?.SetActive(true);
        crosshairSniperUI?.SetActive(false);
        if (sniperCamera != null) sniperCamera.enabled = false;
        sniperVignette?.SetActive(false);

        if (sniperCamera != null && gunEnd != null)
            stableOffset = sniperCamera.transform.position - gunEnd.position;

        if (tankShoot != null)
            tankShoot.onShotFired += OnSniperShot;

        zoomTarget = 0f;
        zoomCurrent = 0f;
        if (masterMixer != null)
        {
            normalSnapshot = masterMixer.FindSnapshot(normalSnapshotName);
            sniperSnapshot = masterMixer.FindSnapshot(sniperSnapshotName);
        }
    }

    void Update()
    {
        if (toggleViewAction != null && toggleViewAction.action.WasPressedThisFrame())
            ToggleSniperView();

        if (isSniperView)
            HandleZoom();

        UpdateCamera();
    }

    void HandleZoom()
    {
        float scroll;
        if (zoomAction != null && zoomAction.action != null)
        {
            Vector2 v = zoomAction.action.ReadValue<Vector2>();
            scroll = v.y;
        }
        else
        {
            scroll = Input.GetAxis("Mouse ScrollWheel");
        }

        if (Mathf.Abs(scroll) > 0.001f)
        {
            float delta = scroll * 0.2f * zoomStep;
            zoomTarget = Mathf.Clamp01(zoomTarget + delta);
        }


        zoomCurrent = Mathf.MoveTowards(zoomCurrent, zoomTarget, Time.deltaTime * zoomSpeed);
    }

    void UpdateCamera()
    {
        if (!isSniperView || sniperCamera == null || gunEnd == null) return;

        targetRecoilPosition = Vector3.Lerp(targetRecoilPosition, Vector3.zero, Time.deltaTime * recoilDecay * 0.5f);
        targetRecoilAngle = Mathf.Lerp(targetRecoilAngle, 0f, Time.deltaTime * recoilDecay * 0.5f);

        currentRecoilPosition = Vector3.Lerp(currentRecoilPosition, targetRecoilPosition, Time.deltaTime * 15f);
        currentRecoilAngle = Mathf.Lerp(currentRecoilAngle, targetRecoilAngle, Time.deltaTime * 15f);


        Vector3 zoomOffset = gunEnd.forward * (maxZoomOffset * zoomCurrent);
        Vector3 basePos = gunEnd.position + stableOffset + gunEnd.forward * positionOffset + zoomOffset + currentRecoilPosition;
        sniperCamera.transform.position = Vector3.Lerp(sniperCamera.transform.position, basePos, Time.deltaTime * followSpeed);

        Quaternion baseRot = Quaternion.LookRotation(gunEnd.forward, gunEnd.up);
        Quaternion recoilLocalRot = Quaternion.Euler(currentRecoilAngle, 0f, 0f);
        Quaternion finalRot = baseRot * recoilLocalRot;

        sniperCamera.transform.rotation = Quaternion.Slerp(sniperCamera.transform.rotation, finalRot, Time.deltaTime * followSpeed);
        float minFOV = Mathf.Clamp(normalFOV - zoomFOVReduction, 15f, normalFOV - 2f);
        sniperCamera.fieldOfView = Mathf.Lerp(normalFOV, minFOV, zoomCurrent);
    }


    void ToggleSniperView()
    {
        if (GameUIManager.Instance != null && GameUIManager.Instance.IsPaused) return;
        isSniperView = !isSniperView;

        if (!isSniperView)
        {
            currentRecoilPosition = Vector3.zero;
            currentRecoilAngle = 0f;
            targetRecoilPosition = Vector3.zero;
            targetRecoilAngle = 0f;
        }

        if (mainCamera != null) mainCamera.enabled = !isSniperView;
        if (sniperCamera != null) sniperCamera.enabled = isSniperView;

        if (crosshairAimUI != null) crosshairAimUI.SetActive(true);
        if (crosshairSniperUI != null) crosshairSniperUI.SetActive(false);

        if (crosshairAimUI != null && crosshairAimUI.TryGetComponent<CrosshairAim>(out var crossAim))
            crossAim.SetCamera(mainCamera);

        if (crosshairSniperUI != null && crosshairSniperUI.TryGetComponent<CrosshairAim>(out var crossSniper))
            crossSniper.SetCamera(sniperCamera);

        sniperVignette?.SetActive(isSniperView);

        if (isSniperView)
        {
            zoomTarget = Mathf.Clamp01(zoomTarget);
            zoomCurrent = Mathf.Clamp01(zoomCurrent);
            sniperSnapshot.TransitionTo(0.2f);
        }
        else
            normalSnapshot.TransitionTo(0.2f);
    }

    void OnDestroy()
    {
        if (tankShoot != null)
            tankShoot.onShotFired -= OnSniperShot;
        toggleViewAction?.action?.Disable();
        zoomAction?.action?.Disable();
    }

    void OnSniperShot()
    {
        if (!isSniperView || sniperCamera == null || gunEnd == null) return;

        float minFOV = Mathf.Clamp(normalFOV - zoomFOVReduction, 15f, normalFOV - 2f);
        float currentFOV = Mathf.Lerp(normalFOV, minFOV, zoomCurrent);
        float recoilScale = currentFOV / normalFOV;

        targetRecoilPosition += 0.18f * recoilScale * -gunEnd.forward;
        targetRecoilPosition += Random.Range(-0.05f, 0.05f) * recoilScale * gunEnd.right;
        targetRecoilAngle -= Random.Range(5.5f, 6f) * recoilScale;

        recoilDecay = 1.5f + (1f - zoomCurrent) * 2.5f;
    }
    
    public bool IsSniperActive()
    {
        return isSniperView;
    }


}