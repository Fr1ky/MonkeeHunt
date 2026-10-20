using PurrNet;
using UnityEngine;

// The cage keys a body carries. The cages hold the truth (who has their key); the server keeps this
// body's count in sync for the HUD, the key at the hip and the name tag icon.
// - The server drops all keys when this body is netted, carried or caged (death / leaving is
//   handled by the cage noticing its holder is gone).
// - Owner: E on a dropped key (GroundKey.Interact) picks it up; first request wins.
public class KeyRing : NetworkBehaviour {

    [SerializeField]
    private NetTrap net;

    // Hangs at the hip while holding at least one key.
    [SerializeField]
    private GameObject hipKey;

    [SerializeField]
    private SyncVar<int> count = new(0);

    public int Count => count.value;

    private void Update() {
        if (isServer) {
            var held = Cage.KeysHeldBy(this);
            if (held > 0 && net && net.IsTrapped) {
                Cage.DropKeysOf(this);
                held = 0;
            }
            if (held != count.value) {
                count.value = held;
            }
        }
        if (hipKey && hipKey.activeSelf != count.value > 0) {
            hipKey.SetActive(count.value > 0);
        }
    }

    // Owner: E on a dropped key.
    public void TryPickUp(GroundKey key) {
        if (!isOwner || !key) {
            return;
        }
        RequestPickup(key);
    }

    [ServerRpc]
    private void RequestPickup(NetworkIdentity keyIdentity) {
        var key = keyIdentity ? keyIdentity.GetComponent<GroundKey>() : null;
        if (key && key.CageServer) {
            key.CageServer.PickUpKeyServer(key, this);
        }
    }
}
