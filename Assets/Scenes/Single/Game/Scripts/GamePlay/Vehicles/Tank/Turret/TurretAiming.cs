using Serilog;
using UnityEngine;

[DisallowMultipleComponent]
public class TurretAiming : MonoBehaviour
{
    [Header("References")]
    public Transform turretPivot;
    public Transform gunPivot;
    public Transform cameraTransform;
    public Transform gunEnd;

    [Header("Yaw (turret)")]
    public float yawSpeed = 120f;
    public float yawSnapAngle = 1f;
    public AudioClip yawLoopSound;
    [Range(0f, 1f)] public float yawMaxVolume = 1f;
    [SerializeField] private float yawMinVolume = 0.1f;
    [SerializeField] private Vector2 yawPitchRange = new(0.8f, 1.2f);
    [SerializeField] private float audioFadeSpeed = 5f;

    [Header("Pitch (gun)")]
    public float pitchSpeed = 90f;
    public float pitchMin = -10f;
    public float pitchMax = 25f;
    public float pitchSnapAngle = 0.8f;
    public bool invertPitch = false;

    [Header("Misc")]
    public float aimDistance = 200f;
    private float currentYaw;
    private float currentPitch;
    private AudioSource loopAudioSource;
    private bool wasRotating = false;
    [SerializeField] private float yawStartThreshold = 2f;
    [SerializeField] private float yawStopThreshold = 0.8f;
    private float smoothedSpeed;
    public TankSniperView sniperView;

    void Start()
    {
        if (turretPivot == null || gunPivot == null || cameraTransform == null)
        {
            Log.Error("[TurretAiming] Not all references assigned on {TurretName}", gameObject.name);
            enabled = false;
            return;
        }

        currentYaw = turretPivot.localEulerAngles.y;
        if (currentYaw > 180f) currentYaw -= 360f;

        currentPitch = gunPivot.localEulerAngles.x;
        if (currentPitch > 180f) currentPitch -= 360f;

        loopAudioSource = gameObject.AddComponent<AudioSource>();
        loopAudioSource.clip = yawLoopSound;
        loopAudioSource.loop = true;
        loopAudioSource.playOnAwake = false;
        loopAudioSource.volume = 0f;
        loopAudioSource.spatialBlend = 1f;
        loopAudioSource.dopplerLevel = .0f;
        AudioManager.AssignToMaster(loopAudioSource);

    }

    void LateUpdate()
    {
        if (!enabled) return;

        Vector3 aimPoint = cameraTransform.position + cameraTransform.forward * aimDistance;
        Vector3 worldDir = aimPoint - (gunPivot != null ? gunPivot.position : turretPivot.position);
        if (worldDir.sqrMagnitude < 0.0001f) return;


        Transform yawBase = turretPivot.parent != null ? turretPivot.parent : turretPivot;
        Vector3 localDirForYaw = yawBase.InverseTransformDirection(worldDir);
        localDirForYaw.y = 0f;

        bool isRotating = false;


        if (localDirForYaw.sqrMagnitude > 0.000001f)
        {
            localDirForYaw.Normalize();
            float targetYaw = Mathf.Atan2(localDirForYaw.x, localDirForYaw.z) * Mathf.Rad2Deg;
            float curYaw = turretPivot.localEulerAngles.y;
            if (curYaw > 180f) curYaw -= 360f;

            float angleDiff = Mathf.DeltaAngle(curYaw, targetYaw);


            bool shouldRotate = Mathf.Abs(angleDiff) > yawStartThreshold;
            bool isCloseEnough = Mathf.Abs(angleDiff) <= yawStopThreshold;

            if (isCloseEnough)
            {

                currentYaw = targetYaw;
                isRotating = false;
            }
            else if (shouldRotate)
            {

                currentYaw = Mathf.MoveTowardsAngle(curYaw, targetYaw, yawSpeed * Time.deltaTime);
                isRotating = true;


                float actualSpeed = Mathf.Abs(Mathf.DeltaAngle(curYaw, currentYaw)) / Time.deltaTime;
                smoothedSpeed = Mathf.Lerp(smoothedSpeed, actualSpeed, Time.deltaTime * 10f);
                float normalizedSpeed = Mathf.Clamp01(smoothedSpeed / yawSpeed); float volume = Mathf.Lerp(yawMinVolume, yawMaxVolume, normalizedSpeed);
                float pitch = Mathf.Lerp(yawPitchRange.x, yawPitchRange.y, normalizedSpeed);

                loopAudioSource.volume = Mathf.Lerp(loopAudioSource.volume, volume, audioFadeSpeed * Time.deltaTime);
                loopAudioSource.pitch = Mathf.Lerp(loopAudioSource.pitch, pitch, audioFadeSpeed * Time.deltaTime);

                if (sniperView != null && sniperView.IsSniperActive())
                {
                    if (!loopAudioSource.isPlaying)
                        loopAudioSource.Play();
                }
                else
                {
                    loopAudioSource.Stop();
                }

            }
            else
            {

                currentYaw = Mathf.MoveTowardsAngle(curYaw, targetYaw, yawSpeed * Time.deltaTime);
                isRotating = wasRotating;
            }

            turretPivot.localEulerAngles = new Vector3(0f, currentYaw, 0f);
        }

        if (!isRotating && wasRotating)
        {
            loopAudioSource.volume = Mathf.Lerp(loopAudioSource.volume, 0f, audioFadeSpeed * Time.deltaTime);
            if (loopAudioSource.volume < 0.01f && loopAudioSource.isPlaying)
                loopAudioSource.Stop();
        }
        else if (!isRotating)
        {
            loopAudioSource.volume = Mathf.Lerp(loopAudioSource.volume, 0f, audioFadeSpeed * Time.deltaTime);
            if (loopAudioSource.volume < 0.01f && loopAudioSource.isPlaying)
                loopAudioSource.Stop();
        }

        wasRotating = isRotating;

        Vector3 localDirForPitch = turretPivot.InverseTransformDirection(worldDir);
        float targetPitch = Mathf.Atan2(localDirForPitch.y, localDirForPitch.z) * Mathf.Rad2Deg;
        if (invertPitch) targetPitch = -targetPitch;
        targetPitch = Mathf.Clamp(targetPitch, pitchMin, pitchMax);

        float curPitch = gunPivot.localEulerAngles.x;
        if (curPitch > 180f) curPitch -= 360f;
        float pitchDiff = Mathf.DeltaAngle(curPitch, targetPitch);

        if (Mathf.Abs(pitchDiff) <= pitchSnapAngle)
        {
            currentPitch = targetPitch;
        }
        else
        {
            currentPitch = Mathf.MoveTowardsAngle(curPitch, targetPitch, pitchSpeed * Time.deltaTime);
        }

        gunPivot.localEulerAngles = new Vector3(currentPitch, 0f, 0f);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        yawSpeed = Mathf.Max(1f, yawSpeed);
        pitchSpeed = Mathf.Max(1f, pitchSpeed);
        pitchMin = Mathf.Clamp(pitchMin, -89f, 89f);
        pitchMax = Mathf.Clamp(pitchMax, -89f, 89f);
        if (pitchMin > pitchMax)
        {
            (pitchMax, pitchMin) = (pitchMin, pitchMax);
        }
    }
#endif
}