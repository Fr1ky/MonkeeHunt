using UnityEngine;

// Lobby hub body: the hunter kit (loadout in hand, guns, aim, cages, walkie-talkie) only works while the
// player is on the Hunters side. On any other side it is switched off and the held item hidden.
public class HubKitGate : MonoBehaviour {

    [SerializeField]
    private PlayerInfo info;

    // The hunter kit components (HunterLoadout, HunterWeapons, HunterAim, HunterCages, HunterRadio).
    [SerializeField]
    private Behaviour[] kit;

    // Hand socket holding the item models.
    [SerializeField]
    private GameObject viewSocket;

    public bool IsHunter => info && info.Team == Team.Hunters;

    private void OnEnable() {
        info.OnInfoChanged += Apply;
        Apply();
    }

    private void OnDisable() {
        if (info) {
            info.OnInfoChanged -= Apply;
        }
    }

    private void Apply() {
        var on = IsHunter;
        foreach (var component in kit) {
            if (component && component.enabled != on) {
                component.enabled = on;
            }
        }
        if (viewSocket && !on) {
            viewSocket.SetActive(false);
        } else if (viewSocket && on && !viewSocket.activeSelf) {
            viewSocket.SetActive(true);
        }
    }
}
