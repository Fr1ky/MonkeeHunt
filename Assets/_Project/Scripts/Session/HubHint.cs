using TMPro;
using UnityEngine;

// Local hub hint line, built in code (round HUD prefab, only shown in the lobby hub):
// hunters get the rack controls, everyone else is told to pick the Hunters side for a loadout.
[RequireComponent(typeof(RectTransform))]
public class HubHint : MonoBehaviour {

    [SerializeField]
    private string hunterText = "1-5 / wheel: pick a slot   ·   E on a rack item: put it there   ·   G: clear the slot";

    [SerializeField]
    private string otherText = "Step onto the Hunters square to set up a hunter loadout at the rack";

    // Line centre, relative to the bottom of the screen.
    [SerializeField]
    private Vector2 offset = new Vector2(0f, 110f);

    private TMP_Text label;

    private void Awake() {
        var canvas = GetComponentInParent<Canvas>();
        var style = canvas ? canvas.GetComponentInChildren<TMP_Text>(true) : null;
        var go = new GameObject("HubHintText", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(1200f, 32f);
        rt.anchoredPosition = offset;
        label = go.AddComponent<TextMeshProUGUI>();
        label.font = style ? style.font : TMP_Settings.defaultFontAsset;
        if (style) {
            label.fontSharedMaterial = style.fontSharedMaterial;
        }
        label.fontSize = 22f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1f, 1f, 1f, 0.85f);
        label.raycastTarget = false;
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
        var hunter = local.TryGetComponent(out PlayerInfo info) && info.Team == Team.Hunters;
        var text = hunter ? hunterText : otherText;
        if (label.text != text) {
            label.text = text;
        }
    }
}
