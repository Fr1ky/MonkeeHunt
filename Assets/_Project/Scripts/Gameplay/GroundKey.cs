using PurrNet;
using UnityEngine;

// A dropped cage key lying on the ground (server-spawned by its Cage). Anyone picks it up with E;
// the first request wins. The cage despawns it once it is empty again.
public class GroundKey : NetworkBehaviour, IInteractable {

    // Spins and bobs so it is easy to spot.
    [SerializeField]
    private Transform model;

    [SerializeField]
    private float spinDegreesPerSecond = 90f;

    [SerializeField]
    private float bobHeight = 0.05f;

    [SerializeField]
    private float bobSpeed = 2f;

    private Vector3 modelBasePosition;

    // Server only.
    public Cage CageServer { get; private set; }

    public void SetCageServer(Cage cage) {
        CageServer = cage;
    }

    private void Awake() {
        if (model) {
            modelBasePosition = model.localPosition;
        }
    }

    public void Interact(PlayerNetworkController player) {
        if (player.TryGetComponent(out KeyRing ring)) {
            ring.TryPickUp(this);
        }
    }

    private void Update() {
        if (model) {
            model.Rotate(0f, spinDegreesPerSecond * Time.deltaTime, 0f, Space.World);
            model.localPosition = modelBasePosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
        }
        // Safety net: its cage is gone.
        if (isServer && isSpawned && !CageServer) {
            Despawn();
        }
    }
}
