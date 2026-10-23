using UnityEngine;
using UnityEngine.UI;

// Local: draws the owner's poop blobs over the screen, the "Hold X to wipe" prompt while there is poop
// on screen, and the "Wiping" bar while X is held (it takes the prompt's place).
// The blob images are a fixed pool in the HUD prefab.
public class SplatOverlay : MonoBehaviour {

    [SerializeField]
    private RectTransform blobContainer;

    [SerializeField]
    private RawImage[] blobs;

    [SerializeField]
    private GameObject wipeHint;

    [SerializeField]
    private GameObject wipeRoot;

    [SerializeField]
    private Image wipeFill;

    // The alpha is the opacity of a fresh splat (wiping fades it from there).
    [SerializeField]
    private Color blobColor = new Color(0.33f, 0.2f, 0.08f, 0.7f);

    private void Awake() {
        foreach (var image in blobs) {
            image.enabled = false;
        }
        wipeRoot.SetActive(false);
        wipeHint.SetActive(false);
    }

    private void Update() {
        var local = PlayerNetworkController.Local;
        var splat = local && local.gameObject.scene == gameObject.scene ? local.GetComponent<SplatStatus>() : null;
        var count = splat ? Mathf.Min(splat.Blobs.Count, blobs.Length) : 0;
        var height = blobContainer.rect.height;
        var width = blobContainer.rect.width;

        for (int i = 0; i < blobs.Length; i++) {
            var image = blobs[i];
            if (i >= count) {
                image.enabled = false;
                continue;
            }
            var blob = splat.Blobs[i];
            var rt = image.rectTransform;
            rt.anchoredPosition = new Vector2(blob.ViewportPosition.x * width, blob.ViewportPosition.y * height);
            rt.sizeDelta = Vector2.one * (blob.Size * height);
            rt.localEulerAngles = new Vector3(0f, 0f, blob.Rotation);
            var c = blobColor;
            c.a = blobColor.a * blob.Alpha;
            image.color = c;
            image.enabled = true;
        }

        var wiping = splat && splat.IsWiping;
        wipeRoot.SetActive(wiping);
        if (wiping) {
            wipeFill.fillAmount = splat.WipeProgress;
        }
        wipeHint.SetActive(count > 0 && !wiping && !EscMenu.IsOpen);
    }
}
