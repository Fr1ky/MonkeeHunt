using PurrNet;
using UnityEngine;

// Trigger that kills any player entering it. Only the server applies the kill.
public class KillZone : MonoBehaviour {

    private void OnTriggerEnter(Collider other) {
        var networkManager = NetworkManager.main;
        if (!networkManager || !networkManager.isServer) {
            return;
        }
        var health = other.GetComponentInParent<PlayerHealth>();
        if (health) {
            health.Kill();
        }
    }
}
