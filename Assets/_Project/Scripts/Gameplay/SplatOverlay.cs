using UnityEngine;
using UnityEngine.UI;

// Local: draws the owner's poop blobs over the screen and the "Wiping" bar while X is held.
// Uses a generated blob texture until real art exists.
public class SplatOverlay : MonoBehaviour {

    [SerializeField]
    private RectTransform blobContainer;

    [SerializeField]
    private GameObject wipeRoot;

    [SerializeField]
    private Image wipeFill;

    [SerializeField]
    private Color blobColor = new Color(0.33f, 0.2f, 0.08f, 1f);

    [SerializeField, Min(1)]
    private int poolSize = 8;

    private RawImage[] pool;

    private void Awake() {
        var texture = BuildBlobTexture(256);
        pool = new RawImage[poolSize];
        for (int i = 0; i < poolSize; i++) {
            var go = new GameObject("Blob" + i, typeof(RectTransform));
            go.transform.SetParent(blobContainer, false);
            var image = go.AddComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
            image.enabled = false;
            pool[i] = image;
        }
        wipeRoot.SetActive(false);
    }

    private void Update() {
        var local = PlayerNetworkController.Local;
        var splat = local && local.gameObject.scene == gameObject.scene ? local.GetComponent<SplatStatus>() : null;
        var count = splat ? Mathf.Min(splat.Blobs.Count, pool.Length) : 0;
        var height = blobContainer.rect.height;
        var width = blobContainer.rect.width;

        for (int i = 0; i < pool.Length; i++) {
            var image = pool[i];
            if (i >= count) {
                image.enabled = false;
                continue;
            }
            var blob = splat.Blobs[i];
            var rt = image.rectTransform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(blob.ViewportPosition.x * width, blob.ViewportPosition.y * height);
            rt.sizeDelta = Vector2.one * (blob.Size * height);
            rt.localEulerAngles = new Vector3(0f, 0f, blob.Rotation);
            var c = blobColor;
            c.a = blob.Alpha;
            image.color = c;
            image.enabled = true;
        }

        var wiping = splat && splat.IsWiping;
        wipeRoot.SetActive(wiping);
        if (wiping) {
            wipeFill.fillAmount = splat.WipeProgress;
        }
    }

    // Lumpy round blob with a few drips: white with alpha, tinted by the RawImage color.
    private static Texture2D BuildBlobTexture(int size) {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var centre = (size - 1) * 0.5f;
        var rng = new System.Random(1234);
        var lobes = new float[7];
        for (int i = 0; i < lobes.Length; i++) {
            lobes[i] = (float)rng.NextDouble() * Mathf.PI * 2f;
        }
        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                var dx = (x - centre) / centre;
                var dy = (y - centre) / centre;
                var angle = Mathf.Atan2(dy, dx);
                var radius = 0.62f;
                for (int i = 0; i < lobes.Length; i++) {
                    radius += 0.05f * Mathf.Sin(angle * (i + 2) + lobes[i]);
                }
                // Drips running down.
                var drip = Mathf.Max(0f, Mathf.Sin(dx * 9f + 1.3f)) * Mathf.Clamp01(-dy) * 0.35f;
                var d = Mathf.Sqrt(dx * dx + dy * dy);
                var edge = radius + (dy < 0f ? drip : 0f);
                var a = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(edge - 0.06f, edge + 0.02f, d));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return tex;
    }
}
