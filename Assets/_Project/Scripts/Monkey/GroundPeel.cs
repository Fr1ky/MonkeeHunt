using PurrNet;
using UnityEngine;

// A thrown banana peel lying on the ground (spawned by the server where the throw landed).
// Stays until someone steps on it: 20 damage, they tip over for 3 s, the peel disappears.
// Trips everyone, monkeys included. The server handles the trigger.
public class GroundPeel : NetworkBehaviour {

    [SerializeField, Min(0)]
    private int damage = 20;

    private bool used;

    private void OnTriggerEnter(Collider other) {
        if (!isServer || used) {
            return;
        }
        var health = other.GetComponentInParent<PlayerHealth>();
        if (!health) {
            return;
        }
        used = true;
        if (health.TryGetComponent(out TripStatus trip)) {
            trip.TripServer();
        }
        health.ApplyDamage(damage);
        Despawn();
    }
}
