using UnityEngine;

// Sits at a gun model's barrel tip: a short flash + light and/or a smoke puff, played on every peer
// when that gun fires. The flash pieces are optional (air guns only puff).
public class MuzzleFlash : MonoBehaviour {

    [SerializeField]
    private Renderer flash;

    [SerializeField]
    private Light flashLight;

    [SerializeField]
    private ParticleSystem puff;

    [SerializeField, Min(0.01f)]
    private float flashSeconds = 0.05f;

    private Vector3 flashScale = Vector3.one;
    private float hideTime = -1f;

    private void Awake() {
        if (flash) {
            flashScale = flash.transform.localScale;
        }
        Show(false);
    }

    public void Play() {
        if (flash) {
            flash.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            flash.transform.localScale = flashScale * Random.Range(0.8f, 1.2f);
        }
        if (flash || flashLight) {
            Show(true);
            hideTime = Time.time + flashSeconds;
        }
        if (puff) {
            puff.Play(true);
        }
    }

    private void Update() {
        if (hideTime > 0f && Time.time >= hideTime) {
            hideTime = -1f;
            Show(false);
        }
    }

    // Switching away mid-flash.
    private void OnDisable() {
        hideTime = -1f;
        Show(false);
    }

    private void Show(bool on) {
        if (flash) {
            flash.enabled = on;
        }
        if (flashLight) {
            flashLight.enabled = on;
        }
    }
}
