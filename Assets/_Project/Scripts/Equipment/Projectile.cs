using System;
using UnityEngine;

// Motion settings for one projectile (gun shot or monkey throw).
public struct ProjectileMotion {
    public float GravityScale;
    public float Lifetime;
    public float Radius;

    public static ProjectileMotion From(WeaponDefinition weapon) {
        return new ProjectileMotion {
            GravityScale = weapon.gravityScale,
            Lifetime = weapon.projectileLifetime,
            Radius = weapon.projectileRadius
        };
    }
}

// Local-only projectile (not networked). The shooter's copy is authoritative and reports hits via
// onHit; everyone else spawns a cosmetic copy (onHit = null) that just flies and disappears.
// Every copy plays its impact effect where it lands (one for players and dummies, one for the world).
public class Projectile : MonoBehaviour {

    private const int MaxHitsPerStep = 8;

    [SerializeField]
    private GameObject impactWorld;

    // Falls back to impactWorld when empty.
    [SerializeField]
    private GameObject impactBody;

    private readonly RaycastHit[] hits = new RaycastHit[MaxHitsPerStep];
    private ProjectileMotion motion;
    private Transform shooterRoot;
    private Action<Collider, Vector3> onHit;
    private Vector3 velocity;
    private float lifeLeft;
    private LayerMask hitLayers;

    public void Launch(ProjectileMotion motion, Vector3 origin, Vector3 velocity, Transform shooterRoot,
        LayerMask hitLayers, Action<Collider, Vector3> onHit) {
        this.motion = motion;
        this.shooterRoot = shooterRoot;
        this.hitLayers = hitLayers;
        this.onHit = onHit;
        this.velocity = velocity;
        transform.position = origin;
        if (velocity.sqrMagnitude > 0.0001f) {
            transform.rotation = Quaternion.LookRotation(velocity);
        }
        lifeLeft = motion.Lifetime;
    }

    private void Update() {
        var dt = Time.deltaTime;
        lifeLeft -= dt;
        if (lifeLeft <= 0f) {
            Destroy(gameObject);
            return;
        }

        velocity += Physics.gravity * (motion.GravityScale * dt);
        var step = velocity * dt;
        var distance = step.magnitude;
        if (distance > 0f && TryHit(step / distance, distance, out var hit)) {
            transform.position = hit.point;
            SpawnImpact(hit);
            onHit?.Invoke(hit.collider, hit.point);
            Destroy(gameObject);
            return;
        }

        transform.position += step;
        if (velocity.sqrMagnitude > 0.0001f) {
            transform.rotation = Quaternion.LookRotation(velocity);
        }
    }

    // Nearest hit along this step, ignoring triggers and the shooter's own colliders.
    private bool TryHit(Vector3 direction, float distance, out RaycastHit nearest) {
        nearest = default;
        var count = Physics.SphereCastNonAlloc(transform.position, motion.Radius, direction, hits,
            distance, hitLayers, QueryTriggerInteraction.Ignore);
        var found = false;
        for (int i = 0; i < count; i++) {
            var hit = hits[i];
            if (shooterRoot && hit.collider.transform.IsChildOf(shooterRoot)) {
                continue;
            }
            // Casts that start overlapping report distance 0 / point zero; treat as a hit at the start.
            if (hit.distance <= 0f) {
                hit.point = transform.position;
                hit.normal = -direction;
            }
            if (!found || hit.distance < nearest.distance) {
                nearest = hit;
                found = true;
            }
        }
        return found;
    }

    // The effect faces out of the surface it hit.
    private void SpawnImpact(RaycastHit hit) {
        var isBody = hit.collider.GetComponentInParent<PlayerHealth>() || hit.collider.GetComponentInParent<TestDummy>();
        var prefab = isBody && impactBody ? impactBody : impactWorld;
        if (!prefab) {
            return;
        }
        var normal = hit.normal.sqrMagnitude > 0.5f ? hit.normal : -velocity.normalized;
        Instantiate(prefab, hit.point, Quaternion.LookRotation(normal));
    }
}
