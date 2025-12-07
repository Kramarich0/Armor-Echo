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
    [Header("Tank Definition")]
    public TankDefinition tankDef;
    [Header("Destructible Parts")]
    public Transform[] destructibleParts;

    [HideInInspector] public float currentHealth;
    public System.Action<float, float> OnHealthChanged;
    private bool isDead = false;

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
        currentHealth = tankDef.health;
        OnHealthChanged?.Invoke(currentHealth, tankDef.health);

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
        OnHealthChanged?.Invoke(currentHealth, tankDef.health);

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

        if (tankDef.deathPrefab != null)
        {
            CreateCorpseSafely();
        }

        int ticketCost = GetTicketCost();
        string victimName = teamComp != null ? teamComp.DisplayName : null ?? gameObject.name;

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
            GameObject corpse = Instantiate(tankDef.deathPrefab, transform.position, transform.rotation);

            CopyTankPose(transform, corpse.transform);

            if (destructibleParts == null || destructibleParts.Length == 0) return;

            foreach (Transform part in destructibleParts)
            {
                Transform corpsePart = corpse.transform.Find(part.name);
                if (corpsePart == null) continue;

                if (!corpsePart.TryGetComponent<MeshRenderer>(out var mesh)) continue;

                if (!corpsePart.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb = corpsePart.gameObject.AddComponent<Rigidbody>();
                    rb.mass = 5f;
                }
                rb.isKinematic = false;

                Vector3 center = mesh.bounds.center;

                Vector3 forceDir = Random.onUnitSphere;
                float forceMag = corpsePart == corpse.transform ? Random.Range(1f, 3f) : Random.Range(2f, 6f);
                rb.AddForceAtPosition(forceDir * forceMag, center, ForceMode.Impulse);

                Vector3 torque = Random.onUnitSphere * Random.Range(5f, 15f);
                rb.AddTorque(torque, ForceMode.Impulse);
            }
        }
        catch (System.Exception e)
        {
            Log.Error(e, "Ошибка при создании трупа для объекта {TankName}", name);
        }
    }

    private void CopyTankPose(Transform source, Transform target)
    {
        target.SetLocalPositionAndRotation(source.localPosition, source.localRotation);
        for (int i = 0; i < source.childCount; i++)
        {
            Transform srcChild = source.GetChild(i);
            Transform tgtChild = target.Find(srcChild.name);
            if (tgtChild != null)
            {
                CopyTankPose(srcChild, tgtChild);
            }
        }
    }


    private void EnsureCorpseHasWheels(GameObject corpse)
    {
        var corpseWheels = corpse.GetComponentsInChildren<WheelCollider>();
        if (corpseWheels.Length == 0)
        {
            Log.Warning("Префаб трупа {TankName} не имеет WheelCollider'ов! Это может вызвать баги.", tankDef.deathPrefab.name);
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