using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Local screen overlay for the tranq: a tinted vignette that grows with stacks, and a dark
// screen with "Zzz" while asleep. Placeholder look until a post-processing pass.
public class TranqOverlay : MonoBehaviour {

    [SerializeField]
    private RawImage vignette;

    [SerializeField]
    private Image sleepCover;

    [SerializeField]
    private TMP_Text sleepText;

    [SerializeField]
    private Color tint = new Color(0.35f, 0.75f, 0.35f, 1f);

    [SerializeField, Range(0f, 1f)]
    private float maxVignetteAlpha = 0.6f;

    [SerializeField, Range(0f, 1f)]
    private float sleepAlpha = 0.92f;

    [SerializeField]
    private float fadeSpeed = 3f;

    private float vignetteLevel;
    private float sleepLevel;

    private void Awake() {
        vignette.texture = BuildVignetteTexture(128);
    }

    private void Update() {
        var local = PlayerNetworkController.Local;
        var tranq = local && local.gameObject.scene == gameObject.scene ? local.GetComponent<TranqStatus>() : null;
        var targetVignette = tranq ? tranq.DrunkLevel : 0f;
        var targetSleep = tranq && tranq.IsAsleep ? 1f : 0f;

        vignetteLevel = Mathf.MoveTowards(vignetteLevel, targetVignette, fadeSpeed * Time.deltaTime);
        sleepLevel = Mathf.MoveTowards(sleepLevel, targetSleep, fadeSpeed * Time.deltaTime);

        var c = tint;
        c.a = vignetteLevel * maxVignetteAlpha;
        vignette.color = c;
        vignette.enabled = c.a > 0.001f;

        sleepCover.color = new Color(0f, 0f, 0f, sleepLevel * sleepAlpha);
        sleepCover.enabled = sleepLevel > 0.001f;
        sleepText.enabled = sleepLevel > 0.5f;
    }

    // White radial vignette: transparent centre, opaque edges. Tinted via the RawImage color.
    private static Texture2D BuildVignetteTexture(int size) {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var centre = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                var dx = (x - centre) / centre;
                var dy = (y - centre) / centre;
                var d = Mathf.Sqrt(dx * dx + dy * dy);
                var a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 1.15f, d));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return tex;
    }
}
