using UnityEngine;

public enum WeaponEffect : byte {
    Damage,
    Tranq,
    Net
}

// One hunter gun. All numbers are tuning values (M3-Plan.md task 1).
[CreateAssetMenu(menuName = "MonkeeHunt/Weapon Definition", fileName = "Weapon")]
public class WeaponDefinition : EquipmentDefinition {

    [Header("Effect")]
    public WeaponEffect effect = WeaponEffect.Damage;

    // Damage for Damage, stacks for Tranq, unused for Net.
    [Min(0)]
    public int effectAmount = 5;

    [Header("Projectile")]
    public GameObject projectilePrefab;

    [Min(0.1f)]
    public float projectileSpeed = 100f;

    // Multiplier on Physics.gravity (0 = straight line).
    [Min(0f)]
    public float gravityScale;

    [Min(0.05f)]
    public float projectileLifetime = 0.5f;

    [Min(0.01f)]
    public float projectileRadius = 0.05f;

    [Header("Handling")]
    [Min(0f)]
    public float cooldown = 0.7f;

    [Min(1)]
    public int magazineSize = 6;

    [Min(0)]
    public int reserveAmmo = 32;

    [Min(0f)]
    public float reloadTime = 1.7f;

    // Degrees of upward camera kick per shot.
    [Min(0f)]
    public float cameraKick = 2f;

    [Header("Audio")]
    public AudioClip fireSound;

    public AudioClip dryFireSound;

    public AudioClip reloadSound;

    [Range(0.5f, 2f)]
    public float pitch = 1f;
}
