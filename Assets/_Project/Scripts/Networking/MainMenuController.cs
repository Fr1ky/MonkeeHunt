using PurrNet;
using PurrNet.Transports;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Direct-IP host/join menu (Proto 1). Host starts server + client and loads the lobby hub;
// PurrNet's scene module brings joining clients into the same scene.
public class MainMenuController : MonoBehaviour {

    [SerializeField]
    private NetworkManager networkManager;

    [SerializeField]
    private UDPTransport transport;

    [SerializeField]
    private string lobbySceneName = "Lobby";

    [SerializeField]
    private TMP_InputField nameField;

    [SerializeField]
    private TMP_InputField addressField;

    [SerializeField]
    private TMP_InputField portField;

    [SerializeField]
    private Button hostButton;

    [SerializeField]
    private Button joinButton;

    [SerializeField]
    private TMP_Text statusText;

    private bool hosting;

    private void Awake() {
        // Saved on this PC from the last session (the transport's defaults otherwise).
        nameField.characterLimit = LocalPlayerProfile.MaxNameLength;
        nameField.text = LocalPlayerProfile.Name;
        addressField.text = string.IsNullOrEmpty(LocalPlayerProfile.Address) ? transport.address : LocalPlayerProfile.Address;
        portField.text = (LocalPlayerProfile.Port > 0 ? LocalPlayerProfile.Port : transport.serverPort).ToString();
        SetStatus(SessionFlow.PendingMenuMessage ?? "");
        SessionFlow.PendingMenuMessage = null;
    }

    private void OnEnable() {
        hostButton.onClick.AddListener(Host);
        joinButton.onClick.AddListener(Join);
        networkManager.onServerConnectionState += OnServerConnectionState;
        networkManager.onClientConnectionState += OnClientConnectionState;
    }

    private void OnDisable() {
        hostButton.onClick.RemoveListener(Host);
        joinButton.onClick.RemoveListener(Join);
        if (networkManager) {
            networkManager.onServerConnectionState -= OnServerConnectionState;
            networkManager.onClientConnectionState -= OnClientConnectionState;
        }
    }

    private void Host() {
        if (!ApplyEndpoint()) {
            return;
        }
        hosting = true;
        SetStatus("Starting host...");
        networkManager.StartHost();
    }

    private void Join() {
        if (!ApplyEndpoint()) {
            return;
        }
        hosting = false;
        SetStatus($"Connecting to {transport.address}:{transport.serverPort}...");
        networkManager.StartClient();
    }

    private bool ApplyEndpoint() {
        if (!ushort.TryParse(portField.text, out var port)) {
            SetStatus("Invalid port.");
            return false;
        }
        transport.address = addressField.text.Trim();
        transport.serverPort = port;
        LocalPlayerProfile.Name = nameField.text.Trim();
        LocalPlayerProfile.Address = transport.address;
        LocalPlayerProfile.Port = port;
        LocalPlayerProfile.Save();
        return true;
    }

    private void OnServerConnectionState(ConnectionState state) {
        if (state == ConnectionState.Connected && hosting) {
            networkManager.sceneModule.LoadSceneAsync(lobbySceneName);
        }
    }

    private void OnClientConnectionState(ConnectionState state) {
        switch (state) {
            case ConnectionState.Connected:
                SetStatus("Connected.");
                break;
            case ConnectionState.Disconnected:
                SetStatus("Disconnected.");
                break;
        }
    }

    private void SetStatus(string message) {
        statusText.text = message;
    }
}
