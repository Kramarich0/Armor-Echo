using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class TankShoot : MonoBehaviour
{
    public System.Action onShotFired;
    public Transform gunEnd;
    [Header("Bullet System")]
    public BulletSlot[] bulletSlots;

    private int previousBulletIndex = -1;
    public int currentBulletIndex = 0;

    private bool isSwitchingAmmo = false;

    [Header("Shooting Settings")]
    public float shotsPerSecond = 8f;

    [Header("Effects")]
    public ParticleSystem muzzleSmoke;
    private ParticleSystem activeMuzzleSmoke;
    public AudioSource audioSource;
    public AudioClip shootSound;
    public AudioClip reloadSound;

    [Header("Reload UI")]
    public ReloadDisplay reloadDisplay;

    [Header("Recoil")]
    public float recoilBack = 0.12f;
    public float recoilDecay = 8f;
    public float recoilJitter = 0.01f;
    public float recoilPhysicalImpulse = 150f;

    [Header("Controls")]
    public InputActionReference shootAction;
    public InputActionReference switchBulletAction;

    private float nextFireTime = 0f;
    private bool reloadClipPlayed = false;
    private Vector3 originalLocalPos;
    private Vector3 recoilOffset = Vector3.zero;

    public BulletSlot CurrentBullet => bulletSlots != null && bulletSlots.Length > 0
       ? bulletSlots[currentBulletIndex]
       : null;

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = 10f;
        audioSource.maxDistance = 500f;

        originalLocalPos = transform.localPosition;

        if (muzzleSmoke != null)
        {
            activeMuzzleSmoke = Instantiate(muzzleSmoke);
            activeMuzzleSmoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = activeMuzzleSmoke.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
        }

    }

    void OnEnable()
    {
        shootAction.action?.Enable();
        switchBulletAction?.action?.Enable();
        if (switchBulletAction != null)
            switchBulletAction.action.performed += OnSwitchBullet;
    }

    void OnDisable()
    {
        shootAction.action?.Disable();
        switchBulletAction?.action?.Disable();
        if (switchBulletAction != null)
            switchBulletAction.action.performed -= OnSwitchBullet;
    }


    private void OnSwitchBullet(InputAction.CallbackContext ctx)
    {
        if (GameUIManager.Instance != null && GameUIManager.Instance.IsPaused) return;

        float val = ctx.ReadValue<float>();
        if (val > 0.5f)
        {
            string key = ctx.control.name;
            if (int.TryParse(key, out int index))
            {
                index--;
                if (index >= 0 && index < bulletSlots.Length && index != currentBulletIndex)
                {
                    if (index == currentBulletIndex) return;

                    float fullReloadTime = 1f / Mathf.Max(0.0001f, shotsPerSecond);
                    nextFireTime = Time.time + fullReloadTime;

                    Debug.Log($"Reload reset: {fullReloadTime:F2}s for {bulletSlots[index].displayName}");

                    previousBulletIndex = currentBulletIndex;
                    currentBulletIndex = index;
                    isSwitchingAmmo = true;

                    Debug.Log($"Switched to: {CurrentBullet.displayName}");
                    reloadClipPlayed = false;
                }
            }
        }
    }

    void Update()
    {
        if (GameUIManager.Instance != null && GameUIManager.Instance.IsPaused) return;
        if (shootAction.action == null) return;

        UpdateReloadDisplay();

        if (shootAction.action.WasPressedThisFrame() && Time.time >= nextFireTime)
        {
            Shoot();
        }

        UpdateReloadSound();
        UpdateRecoil();
    }

    private void UpdateReloadDisplay()
    {
        if (reloadDisplay != null)
        {
            float remainingTime = Mathf.Max(0f, nextFireTime - Time.time);
            reloadDisplay.SetReload(remainingTime, 1f / shotsPerSecond);
        }
    }

    private void UpdateReloadSound()
    {
        if (Time.time < nextFireTime)
        {
            float remainingTime = nextFireTime - Time.time;
            if (!reloadClipPlayed && remainingTime <= 2.5f && reloadSound != null)
            {
                audioSource.PlayOneShot(reloadSound);
                reloadClipPlayed = true;
            }
        }
        else
        {
            reloadClipPlayed = false;
        }
    }

    private void UpdateRecoil()
    {
        recoilOffset = Vector3.Lerp(recoilOffset, Vector3.zero, recoilDecay * Time.deltaTime);
        transform.localPosition = originalLocalPos + recoilOffset;
    }

    void Shoot()
    {
        if (CurrentBullet == null || CurrentBullet.pool == null || CurrentBullet.definition == null)
        {
            Debug.LogWarning("No valid bullet selected!");
            return;
        }

        float fireInterval = 1f / Mathf.Max(0.0001f, shotsPerSecond);
        nextFireTime = Time.time + fireInterval;

        // Recoil
        recoilOffset += -transform.forward * recoilBack;
        if (recoilJitter > 0f)
        {
            Vector3 jitter = Random.insideUnitSphere * recoilJitter;
            jitter.y = 0f;
            recoilOffset += jitter;
        }

        PlayShootEffects();
        SpawnBullet();
        ApplyRecoilForce();

        onShotFired?.Invoke();
    }

    private void PlayShootEffects()
    {
        if (shootSound != null)
        {
            var tempSource = gameObject.AddComponent<AudioSource>();
            tempSource.clip = shootSound;
            tempSource.volume = Random.Range(0.9f, 1.1f);
            tempSource.pitch = Random.Range(0.95f, 1.05f);
            tempSource.spatialBlend = 1f;
            tempSource.minDistance = 10f;
            tempSource.maxDistance = 500f;
            tempSource.rolloffMode = AudioRolloffMode.Linear;
            AudioManager.AssignToMaster(tempSource);
            tempSource.Play();
            Destroy(tempSource, shootSound.length + 0.1f);
        }

        if (activeMuzzleSmoke != null)
        {
            activeMuzzleSmoke.transform.SetPositionAndRotation(gunEnd.position, gunEnd.rotation);
            activeMuzzleSmoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            activeMuzzleSmoke.Play();
        }
    }

    private void SpawnBullet()
    {
        if (gunEnd == null) return;

        TeamComponent teamComp = GetComponentInParent<TeamComponent>();
        TeamEnum team = teamComp ? teamComp.team : TeamEnum.Neutral;
        string shooterDisplay = teamComp != null && !string.IsNullOrEmpty(teamComp.displayName)
            ? teamComp.displayName
            : gameObject.name;

        Collider[] shooterColliders = GetComponentsInParent<Collider>();

        CurrentBullet.pool.SpawnBullet(
            gunEnd.position,
            gunEnd.forward * CurrentBullet.definition.speed,
            CurrentBullet.definition,
            team,
            shooterDisplay,
            shooterColliders
        );
    }

    private void ApplyRecoilForce()
    {
        Rigidbody parentRb = GetComponentInParent<Rigidbody>();
        if (parentRb != null && recoilPhysicalImpulse > 0f)
        {
            parentRb.AddForceAtPosition(-gunEnd.forward * recoilPhysicalImpulse, gunEnd.position, ForceMode.Impulse);
        }
    }


}
