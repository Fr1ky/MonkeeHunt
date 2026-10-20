using PurrNet;
using PurrNet.Transports;
using UnityEngine;
using UnityEngine.SceneManagement;

// Lives on the NetworkManager (DontDestroyOnLoad). Ends the session and returns to the MainMenu:
// - Leave(): the local player chose to leave (Esc menu). A leaving host ends the session for everyone.
// - Unexpected client disconnect after having been connected: "Host left" message in the MainMenu.
// Destroying the NetworkManager stops server and client; the MainMenu scene brings a fresh one.
public class SessionFlow : MonoBehaviour {

    [SerializeField]
    private NetworkManager networkManager;

    [SerializeField]
    private string mainMenuSceneName = "MainMenu";

    [SerializeField]
    private string hostLeftMessage = "Host left the game.";

    // Shown once by the MainMenu after returning.
    public static string PendingMenuMessage;

    public static SessionFlow Instance { get; private set; }

    private bool wasConnected;
    private bool leaving;

    private void Awake() {
        Instance = this;
    }

    private void OnDestroy() {
        if (Instance == this) {
            Instance = null;
        }
    }

    private void OnEnable() {
        networkManager.onClientConnectionState += OnClientConnectionState;
    }

    private void OnDisable() {
        if (networkManager) {
            networkManager.onClientConnectionState -= OnClientConnectionState;
        }
    }

    public void Leave() {
        leaving = true;
        ReturnToMenu(null);
    }

    private void OnClientConnectionState(ConnectionState state) {
        if (state == ConnectionState.Connected) {
            wasConnected = true;
        } else if (state == ConnectionState.Disconnected && wasConnected && !leaving) {
            leaving = true;
            ReturnToMenu(hostLeftMessage);
        }
    }

    private void ReturnToMenu(string message) {
        PendingMenuMessage = message;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Destroy(networkManager.gameObject);
        // Async so the old NetworkManager is gone before the MainMenu's new one wakes up
        // (PurrNet destroys a second NetworkManager while the first still exists).
        SceneManager.LoadSceneAsync(mainMenuSceneName);
    }
}
