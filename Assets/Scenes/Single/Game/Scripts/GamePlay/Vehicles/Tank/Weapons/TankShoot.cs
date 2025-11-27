using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class TankShoot : MonoBehaviour
{
    Tank owner;
    public System.Action onShotFired;
    [Header("Gun")]
    public Transform gunEnd;
    [Header("Bullet System")]
    public BulletSlot[] bulletSlots;

    public int currentBulletIndex = 0;

    [Header("Effects")]
    public ParticleSystem muzzleSmoke;
    private ParticleSystem activeMuzzleSmoke;
    public AudioSource audioSource;

    [Header("Reload UI")]
    public ReloadDisplay reloadDisplay;

    [Header("Recoil Settings")]
    public float recoilDecay = 8f;
    public float recoilJitter = 0.01f;
    [Tooltip("Base multiplier for visual recoil")]
    public float visualRecoilMultiplier = 1f;
    [Tooltip("Base multiplier for physical recoil")]
    public float physicalRecoilMultiplier = 1f;
    [Tooltip("Max visual recoil distance to prevent excessive movement")]
    public float maxVisualRecoil = 0.3f;

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


    void Awake()
    {
        owner = GetComponentInParent<Tank>();
        if (owner == null)
            Debug.LogError("TankShoot: Не найден компонент Tank в родителях!");
    }

    void Start()
    {
        owner = GetComponentInParent<Tank>();
        if (owner == null)
            Debug.LogError("TankShoot: Не найден компонент Tank в родителях!");

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
            activeMuzzleSmoke.transform.SetParent(gunEnd);
            activeMuzzleSmoke.transform.localPosition = Vector3.zero;
            activeMuzzleSmoke.transform.localRotation = Quaternion.identity;

            var main = activeMuzzleSmoke.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            activeMuzzleSmoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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

                    float fullReloadTime = 1f / Mathf.Max(0.0001f, owner.FireRate);
                    nextFireTime = Time.time + fullReloadTime;

                    Debug.Log($"Reload reset: {fullReloadTime:F2}s for {bulletSlots[index].displayName}");

                    currentBulletIndex = index;

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
            reloadDisplay.SetReload(remainingTime, 1f / owner.FireRate);
        }
    }

    private void UpdateReloadSound()
    {
        if (Time.time < nextFireTime)
        {
            float remainingTime = nextFireTime - Time.time;
            if (!reloadClipPlayed && remainingTime <= 2.5f && owner.ReloadSound != null)
            {
                audioSource.PlayOneShot(owner.ReloadSound);
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

        float fireInterval = 1f / Mathf.Max(0.0001f, owner.FireRate);
        nextFireTime = Time.time + fireInterval;

        CalculateAndApplyRecoil();
        PlayShootEffects();
        SpawnBullet();

        onShotFired?.Invoke();
    }

    private void CalculateAndApplyRecoil()
    {
        if (CurrentBullet.definition == null) return;

        float caliber = CurrentBullet.definition.caliber;
        float bulletMass = CurrentBullet.definition.massKg;
        float muzzleVelocity = owner.GetMuzzleVelocity(CurrentBullet.definition);

        float bulletMomentum = bulletMass * muzzleVelocity;

        float baseVisualRecoil = bulletMomentum * caliber / 10000f;
        float visualRecoil = Mathf.Min(baseVisualRecoil * visualRecoilMultiplier, maxVisualRecoil);

        float physicalRecoil = bulletMomentum * caliber / 5000f * physicalRecoilMultiplier;

        recoilOffset += -transform.forward * visualRecoil;
        if (recoilJitter > 0f)
        {
            Vector3 jitter = Random.insideUnitSphere * recoilJitter;
            jitter.y = 0f;
            recoilOffset += jitter;
        }

        ApplyPhysicalRecoil(physicalRecoil);

        Debug.Log($"Recoil - Caliber: {caliber}mm, Mass: {bulletMass}kg, Velocity: {muzzleVelocity}m/s, " +
                 $"Visual: {visualRecoil:F4}, Physical: {physicalRecoil:F2}N");
    }

    private void ApplyPhysicalRecoil(float recoilForce)
    {
        Rigidbody parentRb = GetComponentInParent<Rigidbody>();
        if (parentRb != null && recoilForce > 0f)
        {
            parentRb.AddForceAtPosition(-gunEnd.forward * recoilForce, gunEnd.position, ForceMode.Impulse);
        }
    }

    private void PlayShootEffects()
    {
        if (owner.ShootSound != null)
        {
            var tempSource = gameObject.AddComponent<AudioSource>();
            tempSource.clip = owner.ShootSound;
            tempSource.volume = Random.Range(0.9f, 1.1f);
            tempSource.pitch = Random.Range(0.95f, 1.05f);
            tempSource.spatialBlend = 1f;
            tempSource.minDistance = 10f;
            tempSource.maxDistance = 500f;
            tempSource.rolloffMode = AudioRolloffMode.Linear;
            AudioManager.AssignToMaster(tempSource);
            tempSource.Play();
            Destroy(tempSource, owner.ShootSound.length + 0.1f);
        }

        if (activeMuzzleSmoke != null)
        {
            activeMuzzleSmoke.transform.position = gunEnd.position;
            activeMuzzleSmoke.transform.rotation = gunEnd.rotation;
            activeMuzzleSmoke.Play();
        }
        else
        {
            if (muzzleSmoke != null)
            {
                ParticleSystem ps = Instantiate(muzzleSmoke, gunEnd.position, gunEnd.rotation);
                ps.Play();
                Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
            }
        }
    }


    private void SpawnBullet()
    {
        if (gunEnd == null) return;

        if (owner != null && CurrentBullet.definition != null)
        {
            if (owner.Caliber != CurrentBullet.definition.caliber)
                Debug.LogWarning($"[TankShoot] Gun caliber ({owner.Caliber}mm) != ammo caliber ({CurrentBullet.definition.caliber}mm) for {CurrentBullet.displayName} on {gameObject.name}");
        }

        TeamComponent teamComp = GetComponentInParent<TeamComponent>();
        TeamEnum team = teamComp ? teamComp.team : TeamEnum.Neutral;
        string shooterDisplay = teamComp != null && !string.IsNullOrEmpty(teamComp.displayName)
            ? teamComp.displayName
            : gameObject.name;

        Collider[] shooterColliders = GetComponentsInParent<Collider>();

        float muzzle = 0f;
        if (owner != null && CurrentBullet.definition != null)
        {
            muzzle = owner.GetMuzzleVelocity(CurrentBullet.definition);
        }

        Vector3 spawnVelocity = gunEnd.forward * muzzle;

        CurrentBullet.pool.SpawnBullet(
            gunEnd.position,
            spawnVelocity,
            CurrentBullet.definition,
            team,
            shooterDisplay,
            shooterColliders
        );
    }


}
