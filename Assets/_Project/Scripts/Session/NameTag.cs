using TMPro;
using UnityEngine;

// Floating name above a player, tinted by team, always facing the local camera.
// Hidden on the local player's own body. A small key icon sits left of the name while the body
// carries cage keys. While the body is stuck in quicksand, its teammates see a red "HELP! <seconds>"
// line above the name.
public class NameTag : MonoBehaviour {

    [SerializeField]
    private PlayerInfo playerInfo;

    [SerializeField]
    private TMP_Text label;

    [SerializeField]
    private KeyRing keyRing;

    [SerializeField]
    private Transform keyIcon;

    // Gap between the icon and the start of the name.
    [SerializeField]
    private float keyIconGap = 0.12f;

    [SerializeField]
    private Color unassignedColor = new Color(0.85f, 0.85f, 0.85f);

    [SerializeField]
    private Color huntersColor = new Color(1f, 0.65f, 0.3f);

    [SerializeField]
    private Color monkeysColor = new Color(0.8f, 0.55f, 0.3f);

    [SerializeField]
    private string helpColor = "#FF4A3D";

    private QuicksandTrap sand;

    // Seconds shown in the HELP line (-1 = no line), so the text is only rebuilt when it changes.
    private int shownHelpSeconds = -1;

    private void OnEnable() {
        sand = playerInfo.GetComponent<QuicksandTrap>();
        playerInfo.OnInfoChanged += Refresh;
        Refresh();
    }

    private void OnDisable() {
        playerInfo.OnInfoChanged -= Refresh;
    }

    private void Refresh() {
        label.enabled = !playerInfo.isOwner;
        shownHelpSeconds = -1;
        label.text = playerInfo.DisplayName;
        label.color = playerInfo.Team switch {
            Team.Hunters => huntersColor,
            Team.Monkeys => monkeysColor,
            _ => unassignedColor
        };
    }

    private void LateUpdate() {
        var cam = Camera.main;
        if (cam) {
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }
        UpdateHelpLine();
        if (keyIcon) {
            var show = label.enabled && keyRing && keyRing.Count > 0;
            if (keyIcon.gameObject.activeSelf != show) {
                keyIcon.gameObject.SetActive(show);
            }
            if (show) {
                var p = keyIcon.localPosition;
                keyIcon.localPosition = new Vector3(label.textBounds.min.x - keyIconGap, p.y, p.z);
            }
        }
    }

    // Teammates of a body stuck in quicksand see "HELP! <seconds>" above its name.
    private void UpdateHelpLine() {
        var seconds = -1;
        if (label.enabled && sand && sand.IsTrapped) {
            var local = PlayerNetworkController.Local;
            var localInfo = local ? local.GetComponent<PlayerInfo>() : null;
            if (localInfo && localInfo.Team == playerInfo.Team) {
                seconds = Mathf.CeilToInt(sand.TimeLeft);
            }
        }
        if (seconds == shownHelpSeconds) {
            return;
        }
        shownHelpSeconds = seconds;
        label.text = seconds >= 0
            ? $"<color={helpColor}>HELP! {seconds}</color>\n{playerInfo.DisplayName}"
            : playerInfo.DisplayName;
    }
}
