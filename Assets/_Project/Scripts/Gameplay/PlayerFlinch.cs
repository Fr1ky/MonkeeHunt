using PurrNet;
using UnityEngine;

// Small camera jolt on the owner when something hits them (e.g. a thrown rock). Server triggers it.
public class PlayerFlinch : NetworkBehaviour {

    [SerializeField]
    private MouseAimController mouseAim;

    // Server.
    public void FlinchServer(float degrees) {
        if (isServer && owner.HasValue) {
            FlinchOwner(owner.Value, degrees);
        }
    }

    [TargetRpc]
    private void FlinchOwner(PlayerID target, float degrees) {
        mouseAim.Kick(degrees);
    }
}
