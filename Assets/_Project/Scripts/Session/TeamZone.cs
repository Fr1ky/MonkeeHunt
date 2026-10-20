using PurrNet;
using UnityEngine;

// Lobby hub floor square: stepping in puts you on this team (Unassigned = the Spectate square).
// Leaving keeps your choice; only stepping into another square changes it. Only the server changes teams.
public class TeamZone : MonoBehaviour {

    [SerializeField]
    private Team team;

    private void OnTriggerEnter(Collider other) {
        var networkManager = NetworkManager.main;
        if (!networkManager || !networkManager.isServer) {
            return;
        }
        var info = other.GetComponentInParent<PlayerInfo>();
        if (info) {
            info.SetTeam(team);
        }
    }
}
