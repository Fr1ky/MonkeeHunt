using TMPro;
using UnityEngine;

// Local hub hint line (round HUD prefab, only shown in the lobby hub): hunters get the rack controls,
// monkeys the monkey corner, everyone else is told to pick the Hunters side for a loadout.
public class HubHint : MonoBehaviour {

    [SerializeField]
    private TMP_Text label;

    [SerializeField]
    private string hunterText = "1-5 / wheel: pick a slot   ·   E on a rack item: put it there   ·   G: clear the slot";

    [SerializeField]
    private string otherText = "Step onto the Hunters square to set up a hunter loadout at the rack";

    [SerializeField]
    private string monkeyText = "Monkey corner: climb, swing, E picks up, F eats   ·   throws only hit the dummies and never run out";

    private void Awake() {
        label.enabled = false;
    }

    private void Update() {
        var local = PlayerNetworkController.Local;
        var show = HunterLoadout.InHub && local && local.gameObject.scene == gameObject.scene && !EscMenu.IsOpen;
        if (label.enabled != show) {
            label.enabled = show;
        }
        if (!show) {
            return;
        }
        var team = local.TryGetComponent(out PlayerInfo info) ? info.Team : Team.Unassigned;
        var text = team switch {
            Team.Hunters => hunterText,
            Team.Monkeys => monkeyText,
            _ => otherText
        };
        if (label.text != text) {
            label.text = text;
        }
    }
}
