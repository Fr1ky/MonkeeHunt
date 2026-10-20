using UnityEngine;
using UnityEngine.UI;

// Local: black "two lenses" mask over the screen while the hunter zooms with binoculars.
public class BinocularOverlay : MonoBehaviour {

    [SerializeField]
    private RawImage mask;

    private void Awake() {
        mask.texture = BuildMaskTexture(512, 256);
        mask.enabled = false;
    }

    private void Update() {
        var local = PlayerNetworkController.Local;
        var aim = local && local.gameObject.scene == gameObject.scene ? local.GetComponent<HunterAim>() : null;
        mask.enabled = aim && aim.IsBinocularsZoomed;
    }

    // Opaque black everywhere except two soft-edged overlapping circles.
    private static Texture2D BuildMaskTexture(int width, int height) {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var radius = height * 0.46f;
        var left = new Vector2(width * 0.5f - radius * 0.72f, height * 0.5f);
        var right = new Vector2(width * 0.5f + radius * 0.72f, height * 0.5f);
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                var p = new Vector2(x, y);
                var d = Mathf.Min(Vector2.Distance(p, left), Vector2.Distance(p, right)) / radius;
                var a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.85f, 1f, d));
                tex.SetPixel(x, y, new Color(0f, 0f, 0f, a));
            }
        }
        tex.Apply();
        return tex;
    }
}
