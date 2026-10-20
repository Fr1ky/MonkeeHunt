using System.Collections.Generic;
using PurrNet;
using UnityEngine;

// A quicksand patch (scene object): a flat round patch with a trigger on top. The server traps any body
// standing in the trigger (QuicksandTrap decides who can be trapped). The trigger reaches ~0.8 m up,
// since standing bodies float about 0.5 m above the ground.
public class Quicksand : MonoBehaviour {

    private static readonly List<Quicksand> all = new();

    // Horizontal radius of the patch (for "is this spot in quicksand?").
    [SerializeField, Min(0.1f)]
    private float radius = 2f;

    // True if the point lies over any quicksand patch (used to pick a safe pull-out spot).
    public static bool IsOverAny(Vector3 point) {
        foreach (var patch in all) {
            if (!patch) {
                continue;
            }
            var offset = Vector3.ProjectOnPlane(point - patch.transform.position, Vector3.up);
            if (offset.magnitude <= patch.radius) {
                return true;
            }
        }
        return false;
    }

    private void OnEnable() {
        all.Add(this);
    }

    private void OnDisable() {
        all.Remove(this);
    }

    private void OnTriggerStay(Collider other) {
        var networkManager = NetworkManager.main;
        if (!networkManager || !networkManager.isServer) {
            return;
        }
        var trap = other.GetComponentInParent<QuicksandTrap>();
        if (trap) {
            trap.TryTrapServer();
        }
    }

    private void OnDrawGizmos() {
        Gizmos.color = new Color(0.85f, 0.7f, 0.35f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
