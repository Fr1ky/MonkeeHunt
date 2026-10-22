using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum HitFeedbackKind : byte {
    Hurt,
    Splat
}

// Local HUD for getting hit: a red screen-edge flash on damage (stronger for bigger hits), red arcs
// around the crosshair pointing at where a shot or throw came from (they keep pointing there while you
// turn), and the hurt thud / poop squelch.
public class DamageHud : MonoBehaviour {

    private static readonly List<DamageHud> All = new();

    [SerializeField]
    private Image vignette;

    [SerializeField]
    private Image[] arcs;

    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private AudioClip hurtSound;

    [SerializeField]
    private AudioClip splatSound;

    [SerializeField]
    private float flashSeconds = 0.5f;

    // Flash strength for the smallest and the biggest hits (alpha of the red edge).
    [SerializeField]
    private float minFlash = 0.35f;

    [SerializeField]
    private float maxFlash = 0.85f;

    // A hit taking this share of max health (or more) gives the biggest flash.
    [SerializeField]
    private float fullFlashShare = 0.25f;

    [SerializeField]
    private float arcSeconds = 1f;

    private float flashAlpha;
    private Vector3[] arcSources;
    private float[] arcStart;
    private int nextArc;

    // Health lost as a share of max health (0-1).
    public static void Damage(float share) {
        var hud = LocalHud();
        if (hud) {
            hud.flashAlpha = Mathf.Max(hud.flashAlpha,
                Mathf.Lerp(hud.minFlash, hud.maxFlash, Mathf.Clamp01(share / hud.fullFlashShare)));
        }
    }

    // A shot or throw from another player hit you; source = where it came from.
    public static void Hit(Vector3 source, HitFeedbackKind kind) {
        var hud = LocalHud();
        if (hud) {
            hud.AddHit(source, kind);
        }
    }

    private static DamageHud LocalHud() {
        var local = PlayerNetworkController.Local;
        foreach (var hud in All) {
            if (!local || hud.gameObject.scene == local.gameObject.scene) {
                return hud;
            }
        }
        return null;
    }

    private void Awake() {
        arcSources = new Vector3[arcs.Length];
        arcStart = new float[arcs.Length];
        for (int i = 0; i < arcStart.Length; i++) {
            arcStart[i] = -10f;
        }
    }

    private void OnEnable() {
        All.Add(this);
    }

    private void OnDisable() {
        All.Remove(this);
    }

    private void AddHit(Vector3 source, HitFeedbackKind kind) {
        if (arcs.Length > 0) {
            arcSources[nextArc] = source;
            arcStart[nextArc] = Time.time;
            nextArc = (nextArc + 1) % arcs.Length;
        }
        var clip = kind == HitFeedbackKind.Splat ? splatSound : hurtSound;
        if (audioSource && clip) {
            audioSource.PlayOneShot(clip);
        }
    }

    private void Update() {
        flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, maxFlash * Time.deltaTime / flashSeconds);
        if (vignette) {
            var c = vignette.color;
            c.a = flashAlpha;
            vignette.color = c;
            vignette.enabled = flashAlpha > 0.001f;
        }

        var view = LocalView();
        for (int i = 0; i < arcs.Length; i++) {
            var t = (Time.time - arcStart[i]) / arcSeconds;
            var active = t < 1f && view;
            arcs[i].enabled = active;
            if (!active) {
                continue;
            }
            var forward = Vector3.ProjectOnPlane(view.forward, Vector3.up);
            var toSource = Vector3.ProjectOnPlane(arcSources[i] - view.position, Vector3.up);
            var angle = toSource.sqrMagnitude > 0.0001f ? Vector3.SignedAngle(forward, toSource, Vector3.up) : 0f;
            arcs[i].rectTransform.localEulerAngles = new Vector3(0f, 0f, -angle);
            var c = arcs[i].color;
            c.a = 1f - t * t;
            arcs[i].color = c;
        }
    }

    private static Transform LocalView() {
        var local = PlayerNetworkController.Local;
        var aim = local ? local.GetComponent<MouseAimController>() : null;
        return aim && aim.PlayerCamera ? aim.PlayerCamera.transform : null;
    }
}
