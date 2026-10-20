using PurrNet;
using TMPro;
using UnityEngine;

// Hub test-range target (scene object). Hunters' hits are reported like real hits (the shooter decides,
// HunterWeapons.ReportDummyHit); the server keeps the state and everyone sees it. It resets
// resetSeconds after the last hit.
// - Rifle: "-5" and the running damage total.
// - Tranq: "Tranq 2/4", "ASLEEP" at 4 stacks.
// - Net: a net over it and "NETTED".
// Every hit flashes the body briefly.
public class TestDummy : NetworkBehaviour {

    [SerializeField]
    private Renderer body;

    [SerializeField]
    private GameObject netVisual;

    [SerializeField]
    private TMP_Text label;

    [SerializeField, Min(0.5f)]
    private float resetSeconds = 4f;

    [SerializeField, Min(1)]
    private int stacksToSleep = 4;

    [SerializeField]
    private Color flashColor = new Color(1f, 0.35f, 0.25f, 1f);

    [SerializeField, Min(0.01f)]
    private float flashSeconds = 0.15f;

    [SerializeField]
    private string idleText = "DUMMY";

    [SerializeField]
    private SyncVar<int> damageTotal = new(0);

    [SerializeField]
    private SyncVar<int> lastDamage = new(0);

    [SerializeField]
    private SyncVar<int> stacks = new(0);

    [SerializeField]
    private SyncVar<bool> netted = new(false);

    // Counts hits so every peer can flash on each one.
    [SerializeField]
    private SyncVar<int> hitCount = new(0);

    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    private float resetAtServer;
    private float flashUntil;
    private MaterialPropertyBlock block;
    private Color baseColor = Color.white;
    private string shownText;

    private void Awake() {
        hitCount.onChanged += OnHit;
        if (body && body.sharedMaterial && body.sharedMaterial.HasProperty(BaseColor)) {
            baseColor = body.sharedMaterial.GetColor(BaseColor);
        }
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        hitCount.onChanged -= OnHit;
    }

    // Server.
    public void HitServer(WeaponDefinition weapon) {
        if (!isServer || !weapon) {
            return;
        }
        switch (weapon.effect) {
            case WeaponEffect.Damage:
                lastDamage.value = weapon.effectAmount;
                damageTotal.value += weapon.effectAmount;
                break;
            case WeaponEffect.Tranq:
                stacks.value = Mathf.Min(stacksToSleep, stacks.value + Mathf.Max(1, weapon.effectAmount));
                break;
            case WeaponEffect.Net:
                netted.value = true;
                break;
        }
        hitCount.value++;
        resetAtServer = Time.time + resetSeconds;
    }

    private void Update() {
        if (isServer && Time.time >= resetAtServer && (damageTotal.value != 0 || stacks.value != 0 || netted.value)) {
            damageTotal.value = 0;
            lastDamage.value = 0;
            stacks.value = 0;
            netted.value = false;
        }
        if (netVisual && netVisual.activeSelf != netted.value) {
            netVisual.SetActive(netted.value);
        }
        UpdateLabel();
        UpdateFlash();
    }

    private void UpdateLabel() {
        if (!label) {
            return;
        }
        var text = "";
        if (damageTotal.value > 0) {
            text += $"-{lastDamage.value}  (total {damageTotal.value})\n";
        }
        if (stacks.value > 0) {
            text += stacks.value >= stacksToSleep ? "ASLEEP\n" : $"Tranq {stacks.value}/{stacksToSleep}\n";
        }
        if (netted.value) {
            text += "NETTED\n";
        }
        text = text.Length > 0 ? text.TrimEnd('\n') : idleText;
        if (text != shownText) {
            shownText = text;
            label.text = text;
        }
        var cam = Camera.main;
        if (cam) {
            label.transform.rotation = Quaternion.LookRotation(label.transform.position - cam.transform.position);
        }
    }

    private void OnHit(int count) {
        flashUntil = Time.time + flashSeconds;
    }

    private void UpdateFlash() {
        if (!body) {
            return;
        }
        block ??= new MaterialPropertyBlock();
        block.SetColor(BaseColor, Time.time < flashUntil ? flashColor : baseColor);
        body.SetPropertyBlock(block);
    }
}
