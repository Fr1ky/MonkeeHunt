using PurrNet;
using PurrNet.Transports;
using UnityEngine;

// Lives next to the NetworkManager (DontDestroyOnLoad) and keeps MatchSession in step with the server:
// fresh session when the server starts, entries removed when players leave.
public class MatchSessionHost : MonoBehaviour {

    [SerializeField]
    private NetworkManager networkManager;

    [SerializeField]
    private MatchFlowSettings settings;

    private void OnEnable() {
        networkManager.onServerConnectionState += OnServerConnectionState;
        networkManager.onPlayerLeft += OnPlayerLeft;
    }

    private void OnDisable() {
        if (networkManager) {
            networkManager.onServerConnectionState -= OnServerConnectionState;
            networkManager.onPlayerLeft -= OnPlayerLeft;
        }
    }

    private void OnServerConnectionState(ConnectionState state) {
        if (state == ConnectionState.Connected || state == ConnectionState.Disconnected) {
            MatchSession.Reset(settings.defaultRoundSeconds);
        }
    }

    private void OnPlayerLeft(PlayerID player, bool asServer) {
        if (asServer) {
            MatchSession.Remove(player);
        }
    }
}
