using PurrNet;
using UnityEngine;

// Tranq darts add stacks (server). Each stack slows the body and strengthens the drunk screen;
// at stacksToSleep the body falls asleep (no move / look / actions), then wakes with 0 stacks.
// Without a new hit, one stack wears off every decaySeconds.
public class TranqStatus : NetworkBehaviour, ITranqTarget {

    [SerializeField]
    private PlayerNetworkController player;

    [SerializeField]
    private PlayerMovementController movement;

    [SerializeField, Min(1)]
    private int stacksToSleep = 4;

    [SerializeField, Range(0f, 0.25f)]
    private float slowPerStack = 0.15f;

    [SerializeField, Min(0.1f)]
    private float decaySeconds = 4f;

    [SerializeField, Min(0.1f)]
    private float sleepSeconds = 8f;

    [SerializeField]
    private SyncVar<int> stacks = new(0);

    [SerializeField]
    private SyncVar<bool> asleep = new(false);

    private float nextDecayTime;
    private float wakeTime;

    public int Stacks => stacks.value;
    public bool IsAsleep => asleep.value;

    // 0 = sober, 1 = asleep. Drives the drunk camera and overlay.
    public float DrunkLevel => asleep.value ? 1f : Mathf.Clamp01((float)stacks.value / stacksToSleep);

    // Server.
    public void AddTranqStacks(int amount) {
        if (!isServer || asleep.value || amount <= 0) {
            return;
        }
        stacks.value = Mathf.Min(stacksToSleep, stacks.value + amount);
        nextDecayTime = Time.time + decaySeconds;
        if (stacks.value >= stacksToSleep) {
            asleep.value = true;
            wakeTime = Time.time + sleepSeconds;
        }
    }

    private void Update() {
        if (isServer) {
            TickServer();
        }
        if (isOwner) {
            ApplyLocal();
        }
    }

    private void TickServer() {
        if (asleep.value) {
            if (Time.time >= wakeTime) {
                asleep.value = false;
                stacks.value = 0;
            }
            return;
        }
        if (stacks.value > 0 && Time.time >= nextDecayTime) {
            stacks.value--;
            nextDecayTime = Time.time + decaySeconds;
        }
    }

    private void ApplyLocal() {
        movement.SetSpeedMultiplier(this, 1f - slowPerStack * stacks.value);
        player.SetStatusBlock(this, asleep.value
            ? PlayerNetworkController.StatusBlock.Movement | PlayerNetworkController.StatusBlock.Look | PlayerNetworkController.StatusBlock.Actions
            : PlayerNetworkController.StatusBlock.None);
    }
}
