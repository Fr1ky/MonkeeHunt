using PurrNet;
using UnityEngine;

// Getting-hit feedback for a player's own machine. The server calls NotifyServer for shots and throws
// from other players (arc + sound on the DamageHud); a drop in synced health flashes the screen red.
public class HitFeedback : NetworkBehaviour {

    [SerializeField]
    private PlayerHealth health;

    private int lastHealth = -1;

    private void Awake() {
        if (health) {
            health.OnHealthChanged += OnHealthChanged;
        }
    }

    // The first health drop needs a starting value to compare against.
    private void Start() {
        if (health) {
            lastHealth = health.CurrentHealth;
        }
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        if (health) {
            health.OnHealthChanged -= OnHealthChanged;
        }
    }

    // Server: source = where the shot or throw came from.
    public void NotifyServer(Vector3 source, HitFeedbackKind kind) {
        if (isServer && owner.HasValue) {
            NotifyOwner(owner.Value, source, (byte)kind);
        }
    }

    [TargetRpc]
    private void NotifyOwner(PlayerID target, Vector3 source, byte kind) {
        DamageHud.Hit(source, (HitFeedbackKind)kind);
    }

    private void OnHealthChanged(int current, int max) {
        // Back at full (spawn, team health set, healing crate) is never damage.
        if (isOwner && lastHealth > current && current < max && max > 0) {
            DamageHud.Damage((lastHealth - current) / (float)max);
        }
        lastHealth = current;
    }
}
