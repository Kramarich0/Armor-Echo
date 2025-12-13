using System.Collections;
using Serilog;
using UnityEngine;

public class PlayerTankHealth : MonoBehaviour, IDamageable
{
    [Header("Tank Definition")]
    public TankDefinition tankDef;
    [Header("Destructible Parts")]
    [HideInInspector]
    public float PlayerHealth => tankDef != null ? tankDef.health : 10f;
    [HideInInspector] public float currentHealth;
    public System.Action<float, float> OnHealthChanged;
    private bool isDying = false;


    void Start()
    {
        currentHealth = tankDef.health;
        OnHealthChanged?.Invoke(currentHealth, tankDef.health);
    }

    public void TakeDamage(int amount, string source = null)
    {
        if (currentHealth <= 0 || isDying) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0f);
        OnHealthChanged?.Invoke(currentHealth, tankDef.health);

        if (currentHealth <= 0f)
        {
            isDying = true;
            Die();
        }
    }

    void Die()
    {
        if (isDying) return;
        isDying = true;

        DisablePlayerComponents();

        if (tankDef.deathPrefab != null)
        {
            CreateCorpseSafely();
        }

        GameManager.Instance.OnPlayerTankDestroyed();

        Invoke(nameof(DestroySelf), 0f);
    }

    private void DestroySelf() => Destroy(gameObject);
    private void CreateCorpseSafely()
    {
        if (tankDef?.deathPrefab == null) return;

        GameObject corpse = Instantiate(tankDef.deathPrefab, transform.position, transform.rotation);

        if (tankDef.turretName != null)
        {
            string turretName = tankDef.turretName;
            Transform corpseTurret = corpse.transform.Find(turretName);
            if (corpseTurret != null)
            {
                Transform sourceTurret = transform.Find(turretName);
                if (sourceTurret != null)
                {
                    corpseTurret.SetLocalPositionAndRotation(
                        sourceTurret.localPosition,
                        sourceTurret.localRotation
                    );
                }
            }
        }

        if (tankDef.gunName != null)
        {
            string gunName = tankDef.gunName;
            Transform corpseGun = corpse.transform.Find(gunName);
            if (corpseGun != null)
            {
                Transform sourceGun = transform.Find(gunName);
                if (sourceGun != null)
                {
                    corpseGun.SetLocalPositionAndRotation(
                        sourceGun.localPosition,
                        sourceGun.localRotation
                    );
                }
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