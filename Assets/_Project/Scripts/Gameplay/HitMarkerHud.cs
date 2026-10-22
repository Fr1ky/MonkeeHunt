using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum HitMarkerKind : byte {
    Hit,
    Friendly,
    // Killed, put to sleep or netted.
    Down
}

// Local HUD: the X on the crosshair when your own shot or throw hits someone (white; orange on a
// teammate) and a bigger red X when it takes them down. Plays the hit tick / down sound.
// The shooter shows hits right away (the shooter decides hits); "down" comes from the server.
public class HitMarkerHud : MonoBehaviour {

    private static readonly List<HitMarkerHud> All = new();

    [SerializeField]
    private RectTransform root;

    [SerializeField]
    private Image[] ticks;

    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private AudioClip hitSound;

    [SerializeField]
    private AudioClip downSound;

    [SerializeField]
    private Color hitColor = Color.white;

    [SerializeField]
    private Color friendlyColor = new Color(1f, 0.55f, 0.1f);

    [SerializeField]
    private Color downColor = new Color(1f, 0.15f, 0.1f);

    [SerializeField]
    private float hitSeconds = 0.2f;

    [SerializeField]
    private float downSeconds = 0.5f;

    [SerializeField]
    private float downScale = 1.6f;

    private float shownAt = -10f;
    private float duration = 0.2f;
    private HitMarkerKind kind;
    private Color color;
    private float scale = 1f;

    // Shows on the HUD in the local player's scene (the lobby and the round each have one).
    public static void Show(HitMarkerKind kind) {
        var local = PlayerNetworkController.Local;
        foreach (var hud in All) {
            if (!local || hud.gameObject.scene == local.gameObject.scene) {
                hud.ShowLocal(kind);
                return;
            }
        }
    }

    private void OnEnable() {
        All.Add(this);
        SetAlpha(0f);
    }

    private void OnDisable() {
        All.Remove(this);
    }

    private void ShowLocal(HitMarkerKind newKind) {
        // A plain hit right after a "down" doesn't cut the bigger marker short.
        var downShowing = kind == HitMarkerKind.Down && Time.time - shownAt < duration;
        if (!(downShowing && newKind != HitMarkerKind.Down)) {
            kind = newKind;
            shownAt = Time.time;
            duration = newKind == HitMarkerKind.Down ? downSeconds : hitSeconds;
            color = newKind switch {
                HitMarkerKind.Down => downColor,
                HitMarkerKind.Friendly => friendlyColor,
                _ => hitColor
            };
            scale = newKind == HitMarkerKind.Down ? downScale : 1f;
        }
        var clip = newKind == HitMarkerKind.Down ? downSound : hitSound;
        if (audioSource && clip) {
            audioSource.PlayOneShot(clip);
        }
    }

    private void Update() {
        var t = (Time.time - shownAt) / duration;
        if (t >= 1f) {
            SetAlpha(0f);
            return;
        }
        // Pops in a little larger, then holds and fades.
        root.localScale = Vector3.one * (scale * (1f + 0.35f * (1f - Mathf.Clamp01(t * 5f))));
        SetAlpha(1f - t * t);
    }

    private void SetAlpha(float alpha) {
        foreach (var tick in ticks) {
            if (tick) {
                var c = color;
                c.a = alpha;
                tick.color = c;
            }
        }
    }
}
