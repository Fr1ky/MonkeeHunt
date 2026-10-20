using PurrNet;
using UnityEngine;

// Lobby hub console. The host opens the match settings with Interact; anyone else gets a hint.
public class HostConsole : MonoBehaviour, IInteractable {

    [SerializeField]
    private HostConsoleUI consoleUI;

    public void Interact(PlayerNetworkController player) {
        var networkManager = NetworkManager.main;
        if (networkManager && networkManager.isServer) {
            consoleUI.Open(player);
        } else {
            consoleUI.ShowHint("Only the host can change match settings.");
        }
    }
}
