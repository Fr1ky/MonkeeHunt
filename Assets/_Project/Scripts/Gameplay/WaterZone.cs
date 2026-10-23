using UnityEngine;

// River water: a tall trigger volume whose transform sits at the water surface. Anyone inside whose body
// is down in the water (not on a bridge above it) wades - slowed, no sprint and no jump.
// Every machine runs it; only the owner's movement actually simulates.
public class WaterZone : MonoBehaviour {

    [SerializeField, Range(0.05f, 1f)]
    private float speedMultiplier = 0.5f;

    private void OnTriggerEnter(Collider other) {
        var movement = other.GetComponentInParent<PlayerMovementController>();
        if (movement) {
            movement.EnterWater(this, transform.position.y, speedMultiplier);
        }
    }

    private void OnTriggerExit(Collider other) {
        var movement = other.GetComponentInParent<PlayerMovementController>();
        if (movement) {
            movement.ExitWater(this);
        }
    }
}
