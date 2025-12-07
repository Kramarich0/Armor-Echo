using System.Collections;
using UnityEngine;

public class PlayerTankHealth : MonoBehaviour, IDamageable
{
    [Header("Tank Definition")]
    public TankDefinition tankDef;
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
            Instantiate(tankDef.deathPrefab, transform.position, transform.rotation);
        }

        GameManager.Instance.OnPlayerTankDestroyed();
        Destroy(gameObject);

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