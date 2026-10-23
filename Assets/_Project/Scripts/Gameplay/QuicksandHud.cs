using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Local quicksand bar (round HUD prefab): the trapped player sees "SINKING 7.3" with a draining bar (or
// "Being pulled out..." while a teammate holds E); a helper sees "Pulling out... (hold E)" filling up.
public class QuicksandHud : MonoBehaviour {

    [SerializeField]
    private GameObject panel;

    // Stretched over the bar; its right edge follows the fraction.
    [SerializeField]
    private RectTransform fill;

    [SerializeField]
    private Image fillImage;

    [SerializeField]
    private TMP_Text label;

    [SerializeField]
    private Color sinkingColor = new Color(0.9f, 0.3f, 0.2f, 0.95f);

    [SerializeField]
    private Color helpedColor = new Color(0.95f, 0.8f, 0.3f, 0.95f);

    [SerializeField]
    private Color helpingColor = new Color(0.4f, 0.85f, 0.4f, 0.95f);

    private void Awake() {
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
}
