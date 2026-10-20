using UnityEngine;

// One monkey throwable (poop, rock, banana peel, decoy). Throw speed scales with charge.
[CreateAssetMenu(menuName = "MonkeeHunt/Throwable Definition", fileName = "Throwable")]
public class ThrowableDefinition : ScriptableObject {

    public MonkeyItem item;

    public string displayName;

    public GameObject projectilePrefab;

    [Min(0.1f)]
    public float minSpeed = 8f;

    [Min(0.1f)]
    public float maxSpeed = 18f;

    [Min(0f)]
    public float gravityScale = 1f;

    [Min(0.1f)]
    public float lifetime = 5f;

    [Min(0.01f)]
    public float radius = 0.12f;

    // Optional; silent until clips are added.
    public AudioClip throwSound;

    public AudioClip impactSound;

    public ProjectileMotion Motion => new ProjectileMotion {
        GravityScale = gravityScale,
        Lifetime = lifetime,
        Radius = radius
    };
}
