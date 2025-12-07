using System.Collections;
using Serilog;
using UnityEngine;

public class PlayerTankHealth : MonoBehaviour, IDamageable
{
    [Header("Tank Definition")]
    public TankDefinition tankDef;
    [Header("Destructible Parts")]
    public Transform[] destructibleParts;
    [HideInInspector]
    public float PlayerHealth => tankDef != null ? tankDef.health : 10f;
    [HideInInspector] public float currentHealth;
    public System.Action<float, float> OnHealthChanged;

    void Start()
    {
        currentHealth = tankDef.health;
        OnHealthChanged?.Invoke(currentHealth, tankDef.health);
    }

    public void TakeDamage(int amount, string source = null)
    {
        if (currentHealth <= 0) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0f);
        OnHealthChanged?.Invoke(currentHealth, tankDef.health);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        DisablePlayerComponents();

        if (tankDef.deathPrefab != null)
        {
            CreateCorpseSafely();
        }

        GameManager.Instance.OnPlayerTankDestroyed();
        Destroy(gameObject);

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

    private void DisablePlayerComponents()
    {
        if (TryGetComponent<Tank>(out var tank))
            tank.SetMovementEnabled(false);

        if (TryGetComponent<TurretAiming>(out var aiming))
            aiming.enabled = false;

        if (TryGetComponent<TankShoot>(out var shoot))
            shoot.enabled = false;
    }

}