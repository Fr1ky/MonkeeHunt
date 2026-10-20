using System;
using PurrNet;
using UnityEngine;

// Networked health. The server applies all damage; clients read the synced values.
// Max health comes from MatchFlowSettings per team (set by TeamSpawner via Initialize).
public class PlayerHealth : NetworkBehaviour {

    [SerializeField]
    private SyncVar<int> maxHealth = new(100);

    [SerializeField]
    private SyncVar<int> currentHealth = new(100);

    public int MaxHealth => maxHealth.value;
    public int CurrentHealth => currentHealth.value;
    public bool IsDead => currentHealth.value <= 0;

    // (current, max) — fires on every peer when either synced value changes.
    public event Action<int, int> OnHealthChanged;

    // Server only. Fires once, right before the body is despawned.
    public event Action<PlayerHealth> OnDied;

    private void Awake() {
        currentHealth.onChanged += HealthChanged;
        maxHealth.onChanged += HealthChanged;
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        currentHealth.onChanged -= HealthChanged;
        maxHealth.onChanged -= HealthChanged;
    }

    // Server only: sets max and refills.
    public void Initialize(int max) {
        if (!isServer) {
            return;
        }
        maxHealth.value = Mathf.Max(1, max);
        currentHealth.value = maxHealth.value;
    }

    public void ApplyDamage(int amount) {
        if (!isServer || IsDead || amount <= 0) {
            return;
        }
        currentHealth.value = Mathf.Max(0, currentHealth.value - amount);
        if (currentHealth.value == 0) {
            OnDied?.Invoke(this);
            Despawn();
        }
    }

    public void Kill() {
        ApplyDamage(currentHealth.value);
    }

    // Server only: back to full health (Healing supply crate).
    public void RestoreFull() {
        if (isServer && !IsDead) {
            currentHealth.value = maxHealth.value;
        }
    }

    private void HealthChanged(int value) {
        OnHealthChanged?.Invoke(currentHealth.value, maxHealth.value);
    }
}
