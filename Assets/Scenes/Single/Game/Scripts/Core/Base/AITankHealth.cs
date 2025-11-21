using System.Collections;
using Serilog;
using UnityEngine;

[RequireComponent(typeof(TeamComponent))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(TankAI))]
[RequireComponent(typeof(TankCollisionDamage))]
public class AITankHealth : MonoBehaviour, IDamageable
{
    private static readonly WaitForSeconds _waitForSeconds0_1 = new(0.1f);
    private static readonly WaitForFixedUpdate _waitForFixedUpdate = new();

    [Header("Health")]
    public float maxHealth = 10f;
    [HideInInspector] public float currentHealth;
    public System.Action<float, float> OnHealthChanged;
    private bool isDead = false;

    [Header("Death")]
    public GameObject deathPrefab;

    private TeamComponent teamComp;
    private string lastAttackerName;

    private Rigidbody _cachedRigidbody;
    private TankAI _cachedTankAI;
    private TankCollisionDamage _cachedCollisionDamage;
    private Collider[] _cachedColliders;
    private WheelCollider[] _cachedWheelColliders;
    private Renderer[] _cachedRenderers;

    void Start()
    {
        teamComp = GetComponent<TeamComponent>();
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        CacheComponents();
    }

    private void CacheComponents()
    {
        _cachedRigidbody = GetComponent<Rigidbody>();
        _cachedTankAI = GetComponent<TankAI>();
        _cachedCollisionDamage = GetComponent<TankCollisionDamage>();
        _cachedColliders = GetComponentsInChildren<Collider>();
        _cachedWheelColliders = GetComponentsInChildren<WheelCollider>();
        _cachedRenderers = GetComponentsInChildren<Renderer>();
    }

    public void TakeDamage(int amount, string source = null)
    {
        if (isDead || currentHealth <= 0) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0f);
        lastAttackerName = source;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        if (!this || !gameObject.activeInHierarchy)
        {
            Log.Warning("[AITankHealth] Попытка умереть уничтоженного объекта: {TankName}", name);
            return;
        }

        StartCoroutine(SafeDeathSequence());
    }

    private IEnumerator SafeDeathSequence()
    {
        if (!this) yield break;

        if (deathPrefab != null)
        {
            CreateCorpseSafely();
        }

        int ticketCost = GetTicketCost();
        string victimName = teamComp != null ? teamComp.displayName : null ?? gameObject.name;

        if (teamComp != null)
        {
            GameManager.Instance.OnTankDestroyed(teamComp, ticketCost, lastAttackerName, victimName);
        }

        yield return _waitForFixedUpdate;
        DisableAllComponentsImmediately();

        yield return _waitForSeconds0_1;

        if (this && gameObject)
        {
            Destroy(gameObject);
        }
    }

    private void DisableAllComponentsImmediately()
    {
        if (_cachedRigidbody)
        {
            _cachedRigidbody.linearVelocity = Vector3.zero;
            _cachedRigidbody.angularVelocity = Vector3.zero;
            _cachedRigidbody.isKinematic = true;
        }

        for (int i = 0; i < _cachedColliders.Length; i++)
        {
            if (_cachedColliders[i])
                _cachedColliders[i].enabled = false;
        }

        for (int i = 0; i < _cachedWheelColliders.Length; i++)
        {
            if (_cachedWheelColliders[i])
            {
                _cachedWheelColliders[i].motorTorque = 0f;
                _cachedWheelColliders[i].brakeTorque = 0f;
            }
        }

        if (_cachedTankAI) _cachedTankAI.enabled = false;
        if (_cachedCollisionDamage) _cachedCollisionDamage.enabled = false;

        for (int i = 0; i < _cachedRenderers.Length; i++)
        {
            if (_cachedRenderers[i])
                _cachedRenderers[i].enabled = false;
        }
    }

    private void CreateCorpseSafely()
    {
        try
        {
            Vector3 spawnPos = transform.position;
            GameObject hull = Instantiate(deathPrefab, spawnPos, transform.rotation);

            EnsureCorpseHasWheels(hull);

            if (hull.TryGetComponent<Rigidbody>(out var rbHull))
            {
                Vector3 randomForce = new(
                    Random.Range(-1f, 1f),
                    Random.Range(0.5f, 2f),
                    Random.Range(-1f, 1f)
                );
                rbHull.AddForce(randomForce, ForceMode.Impulse);

                Vector3 randomTorque = new(
                    Random.Range(-3f, 3f),
                    Random.Range(-5f, 5f),
                    Random.Range(-3f, 3f)
                );
                rbHull.AddTorque(randomTorque, ForceMode.Impulse);
            }
        }
        catch (System.Exception e)
        {
            Log.Error(e, "Ошибка при создании трупа для объекта {TankName}", name);
        }
    }

    private void EnsureCorpseHasWheels(GameObject corpse)
    {
        var corpseWheels = corpse.GetComponentsInChildren<WheelCollider>();
        if (corpseWheels.Length == 0)
        {
            Log.Warning("Префаб трупа {TankName} не имеет  WheelCollider'ов! Это может вызвать баги.", deathPrefab.name);
        }
    }

    private int GetTicketCost()
    {
        if (_cachedTankAI)
        {
            return _cachedTankAI.CurrentTankClass switch
            {
                TankClass.Light => 100,
                TankClass.Medium => 200,
                TankClass.Heavy => 300,
                _ => 150
            };
        }
        return 200;
    }
}