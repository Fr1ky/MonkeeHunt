using UnityEngine;

public class LadderController : MonoBehaviour {
    private void OnTriggerEnter(Collider other) {
        if (other.TryGetComponent<IMovementController>(out var movement)) {
            movement.ToggleClimbing(true);
        }
    }

    private void OnTriggerExit(Collider other) {
        if (other.TryGetComponent<IMovementController>(out var movement)) {
            movement.ToggleClimbing(false);
        }
    }
}
