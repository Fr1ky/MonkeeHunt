using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Local screen overlay for the tranq: a tinted vignette that grows with stacks, and a dark
// screen with "Zzz" while asleep. Placeholder look until a post-processing pass
// (texture: Settings/Ui/T_TranqVignette, from MonkeeHunt > Generate Placeholder Textures).
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
}
