using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Local quicksand bar, built in code: the trapped player sees "SINKING 7.3" with a draining bar (or
// "Being pulled out..." while a teammate holds E); a helper sees "Pulling out... (hold E)" filling up.
// Put it on a full-screen RectTransform in the round HUD.
[RequireComponent(typeof(RectTransform))]
public class QuicksandHud : MonoBehaviour {

    [SerializeField]
    private Vector2 barSize = new Vector2(420f, 18f);

    // Bar centre, relative to the screen centre.
    [SerializeField]
    private Vector2 barOffset = new Vector2(0f, -150f);

    [SerializeField]
    private Color sinkingColor = new Color(0.9f, 0.3f, 0.2f, 0.95f);

    [SerializeField]
    private Color helpedColor = new Color(0.95f, 0.8f, 0.3f, 0.95f);

    [SerializeField]
    private Color helpingColor = new Color(0.4f, 0.85f, 0.4f, 0.95f);

    private GameObject panel;
    private RectTransform fill;
    private Image fillImage;
    private TMP_Text label;

    private void Awake() {
        Build();
        panel.SetActive(false);
    }

    private void Update() {
        var local = PlayerNetworkController.Local;
        var inScene = local && local.gameObject.scene == gameObject.scene;
        var sand = inScene ? local.GetComponent<QuicksandTrap>() : null;
        var helping = QuicksandTrap.ActiveHelp;

        if (sand && sand.IsTrapped) {
            var fraction = Mathf.Clamp01(sand.TimeLeft / sand.DeathSeconds);
            Show(sand.IsBeingHelped ? "Being pulled out..." : $"SINKING  {sand.TimeLeft:0.0}",
                fraction, sand.IsBeingHelped ? helpedColor : sinkingColor);
        } else if (helping && inScene) {
            Show("Pulling out...  (hold E)", QuicksandTrap.ActiveHelpProgress, helpingColor);
        } else if (panel.activeSelf) {
            panel.SetActive(false);
        }
    }

    private void Show(string text, float fraction, Color color) {
        if (!panel.activeSelf) {
            panel.SetActive(true);
        }
        label.text = text;
        fill.anchorMax = new Vector2(fraction, 1f);
        fillImage.color = color;
    }

    private void Build() {
        // Same font and outline as the rest of the HUD (looked up before adding our own text).
        var canvas = GetComponentInParent<Canvas>();
        var style = canvas ? canvas.GetComponentInChildren<TMP_Text>(true) : null;

        panel = new GameObject("QuicksandBar", typeof(RectTransform));
        var rt = (RectTransform)panel.transform;
        rt.SetParent(transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = barSize;
        rt.anchoredPosition = barOffset;
        var background = panel.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.55f);
        background.raycastTarget = false;

        var fillGo = new GameObject("Fill", typeof(RectTransform));
        fill = (RectTransform)fillGo.transform;
        fill.SetParent(rt, false);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = fill.offsetMax = Vector2.zero;
        fillImage = fillGo.AddComponent<Image>();
        fillImage.raycastTarget = false;

        var labelGo = new GameObject("Label", typeof(RectTransform));
        var lrt = (RectTransform)labelGo.transform;
        lrt.SetParent(rt, false);
        lrt.anchorMin = new Vector2(0f, 1f);
        lrt.anchorMax = new Vector2(1f, 1f);
        lrt.pivot = new Vector2(0.5f, 0f);
        lrt.sizeDelta = new Vector2(0f, 34f);
        lrt.anchoredPosition = new Vector2(0f, 4f);
        label = labelGo.AddComponent<TextMeshProUGUI>();
        label.font = style ? style.font : TMP_Settings.defaultFontAsset;
        if (style) {
            label.fontSharedMaterial = style.fontSharedMaterial;
        }
        label.fontSize = 26f;
        label.alignment = TextAlignmentOptions.Bottom;
        label.color = Color.white;
        label.raycastTarget = false;
    }
}
