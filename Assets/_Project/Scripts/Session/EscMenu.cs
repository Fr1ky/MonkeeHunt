using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Esc menu for the lobby hub and the round: Resume / Leave (to MainMenu) / Quit.
// Frees the cursor and freezes the local body while open.
public class EscMenu : MonoBehaviour {

    [SerializeField]
    private GameObject panel;

    [SerializeField]
    private Button resumeButton;

    [SerializeField]
    private Button leaveButton;

    [SerializeField]
    private Button quitButton;

    public static bool IsOpen { get; private set; }

    private void Awake() {
        panel.SetActive(false);
        IsOpen = false;
    }

    private void OnEnable() {
        resumeButton.onClick.AddListener(Close);
        leaveButton.onClick.AddListener(Leave);
        quitButton.onClick.AddListener(Quit);
    }

    private void OnDisable() {
        resumeButton.onClick.RemoveListener(Close);
        leaveButton.onClick.RemoveListener(Leave);
        quitButton.onClick.RemoveListener(Quit);
        IsOpen = false;
    }

    private void Update() {
        var keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) {
            return;
        }
        // World menus (host console, loadout station) handle their own Esc.
        if (ModalUI.BlocksEscMenu) {
            return;
        }
        if (IsOpen) {
            Close();
        } else {
            Open();
        }
    }

    private void Open() {
        panel.SetActive(true);
        IsOpen = true;
        var local = PlayerNetworkController.Local;
        if (local) {
            local.SetControlLocked(true);
        } else {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void Close() {
        panel.SetActive(false);
        IsOpen = false;
        var local = PlayerNetworkController.Local;
        if (local) {
            local.SetControlLocked(false);
        } else {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Leave() {
        IsOpen = false;
        if (SessionFlow.Instance) {
            SessionFlow.Instance.Leave();
        }
    }

    private void Quit() {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
