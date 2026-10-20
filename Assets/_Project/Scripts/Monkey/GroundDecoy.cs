using PurrNet;
using UnityEngine;

// A thrown decoy (small, hard-to-spot seed pod) lying where it landed. The server watches for
// hunters: the first one within triggerRadius sets it off - everyone hears the screech + rustle
// at its spot (sound hook, silent until clips are added) and the decoy disappears. Single use;
// monkeys don't trigger it; no timeout.
public class GroundDecoy : NetworkBehaviour {

    private const int MaxOverlaps = 16;

    [SerializeField, Min(0.5f)]
    private float triggerRadius = 4f;

    [SerializeField, Min(0.05f)]
    private float checkInterval = 0.2f;

    [SerializeField]
    private LayerMask playerLayers = ~0;

    [SerializeField]
    private AudioClip screechSound;

    [SerializeField]
    private GameObject visual;

    // Despawn shortly after the screech so the screech message reaches everyone first.
    [SerializeField, Min(0f)]
    private float despawnDelay = 0.25f;

    private readonly Collider[] overlaps = new Collider[MaxOverlaps];
    private float nextCheck;
    private bool triggered;
    private float despawnTime;

    public bool IsTriggered => triggered;

    private void Update() {
        if (!isServer) {
            return;
        }
        if (triggered) {
            if (Time.time >= despawnTime) {
                Despawn();
            }
            return;
        }
        if (Time.time < nextCheck) {
            return;
        }
        nextCheck = Time.time + checkInterval;
        var count = Physics.OverlapSphereNonAlloc(transform.position, triggerRadius, overlaps, playerLayers,
            QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++) {
            var hunter = overlaps[i].GetComponentInParent<HunterLoadout>();
            if (!hunter) {
                continue;
            }
            var health = hunter.GetComponent<PlayerHealth>();
            if (health && health.IsDead) {
                continue;
            }
            triggered = true;
            despawnTime = Time.time + despawnDelay;
            Screech(transform.position);
            return;
        }
    }

    [ObserversRpc]
    private void Screech(Vector3 position) {
        if (visual) {
            visual.SetActive(false);
        }
        if (screechSound) {
            AudioSource.PlayClipAtPoint(screechSound, position);
        }
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = new Color(0.9f, 0.6f, 0.2f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
}
