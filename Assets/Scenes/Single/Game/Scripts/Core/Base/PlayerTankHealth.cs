using System.Collections;
using UnityEngine;

public class PlayerTankHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public float maxHealth = 300f;
    [HideInInspector] public float currentHealth;
    public System.Action<float, float> OnHealthChanged;

    [Header("Death")]
    public GameObject deathPrefab;

    void Start()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(int amount, string source = null)
    {
        if (currentHealth <= 0) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0f);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        DisablePlayerComponents();

        if (deathPrefab != null)
        {
            Instantiate(deathPrefab, transform.position, transform.rotation);
        }

        GameManager.Instance.OnPlayerTankDestroyed();
        Destroy(gameObject);

    }

    private void DisablePlayerComponents()
    {
        if (TryGetComponent<TankMovement>(out var movement))
            movement.enabled = false;
        if (TryGetComponent<TurretAiming>(out var aiming))
            aiming.enabled = false;
        if (TryGetComponent<TankShoot>(out var shoot))
            shoot.enabled = false;

    }

}