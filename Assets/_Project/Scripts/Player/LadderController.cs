using UnityEngine;

// Ported from 3dPixelZombies (fpsTesting branch): walking into the trigger switches the movement
// controller into climbing (W/S up/down, A/D sideways); leaving it switches back.
// MonkeeHunt additions: monkeys ignore ladders (they climb the rock itself), and a climbing hunter
// can't fire, aim, switch or use items until they're off the ladder.
public class LadderController : MonoBehaviour {
    private void OnTriggerEnter(Collider other) {
        if (IsMonkey(other)) {
            return;
        }
        if (other.TryGetComponent<IMovementController>(out var movement)) {
            movement.ToggleClimbing(true);
            SetBlocked(other, true);
        }
    }

    private void OnTriggerExit(Collider other) {
        if (IsMonkey(other)) {
            return;
        }
        if (other.TryGetComponent<IMovementController>(out var movement)) {
            movement.ToggleClimbing(false);
            SetBlocked(other, false);
        }
    }

    private static bool IsMonkey(Collider other) {
        return other.GetComponentInParent<MonkeyClimb>();
    }

    private void SetBlocked(Collider other, bool blocked) {
        var player = other.GetComponentInParent<PlayerNetworkController>();
        if (player) {
            player.SetStatusBlock(this, blocked
                ? PlayerNetworkController.StatusBlock.Actions | PlayerNetworkController.StatusBlock.Interact
                : PlayerNetworkController.StatusBlock.None);
        }
    }
}
