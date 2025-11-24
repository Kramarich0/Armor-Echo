using UnityEngine;
using System.Linq;
using Serilog;

[CreateAssetMenu(fileName = "NewGun", menuName = "Weapons/GunDefinition")]
public class GunDefinition : ScriptableObject
{
    [Header("Основные параметры")]
    public string gunName = "Unnamed gun";
    public int caliber = 76;
    public float fireRate = 1f;
    public int minGunAngle = -10;
    public int maxGunAngle = 10;
    public float shootRange = 100f;
    public bool bulletUseGravity = true;

    [Header("Аудио")]
    public AudioClip shootSound;

    [Header("Снаряды и их скорости")]
    public BulletSlotDefinition[] bullets;

    [System.Serializable]
    public struct BulletSlotDefinition
    {
        public BulletDefinition bullet;
        [Tooltip("Начальная скорость этого снаряда для данной пушки")]
        public float muzzleVelocity;
    }
    
    public bool debugLogs = false;

    public float GetMuzzleVelocity(BulletDefinition bullet)
    {
        if (bullet == null) return 0f;
        if (!CanUse(bullet))
        {
            if (debugLogs) Log.Debug("Нельзя использовать данный снаряд");
            return 0f;
        }

        if (bullets != null && bullets.Length > 0)
        {
            var slot = bullets.FirstOrDefault(b => b.bullet == bullet);
            if (slot.bullet != null && slot.muzzleVelocity > 0f)
                return Mathf.Max(0.0001f, slot.muzzleVelocity);
        }
        return 0f;
    }

    public bool CanUse(BulletDefinition bullet)
    {
        if (bullet == null) return false;
        if (bullets == null || bullets.Length == 0) return true;
        return bullets.Any(b => b.bullet == bullet);
    }
}
