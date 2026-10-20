using System.Collections.Generic;
using PurrNet;
using TMPro;
using UnityEngine;

// Walkie-talkie supply drop, spawned by the server on the ground where the caller stood. Everyone sees:
// a smoke marker on the spot until the crate lands, and a placeholder helicopter that flies over and
// drops the crate so it lands arrivalSeconds after the call. Then any hunter presses E on the crate to
// take its contents once; it vanishes crateSeconds after landing. Monkeys can't use it.
// - Healing: the server restores the taker to full health.
// - Capture / Damage: the taker's own machine refills (ammo and cages live there).
// The visuals are animated locally from a synced clock (sent twice a second, counted up in between).
public class SupplyDrop : NetworkBehaviour, IInteractable {

    // Same asset as the walkie-talkie item: group labels and colours.
    [SerializeField]
    private WalkieTalkieDefinition definition;

    [SerializeField]
    private Transform marker;

    [SerializeField]
    private Renderer markerRenderer;

    [SerializeField]
    private Transform helicopter;

    [SerializeField]
    private Transform rotor;

    [SerializeField]
    private Transform crate;

    [SerializeField]
    private Renderer crateRenderer;

    [SerializeField]
    private Collider crateCollider;

    [SerializeField]
    private TMP_Text crateLabel;

    // Optional; silent until a clip is added.
    [SerializeField]
    private AudioSource helicopterAudio;

    [SerializeField]
    private float flightHeight = 30f;

    [SerializeField]
    private float flightSpeed = 25f;

    // The crate falls from the helicopter for this long before landing.
    [SerializeField]
    private float crateFallSeconds = 2f;

    // The helicopter is shown while it is within this distance of the spot.
    [SerializeField]
    private float flightVisibleDistance = 150f;

    [SerializeField]
    private float rotorDegreesPerSecond = 900f;

    [SerializeField, Range(0f, 1f)]
    private float markerAlpha = 0.35f;

    [SerializeField]
    private SyncVar<SupplyGroup> group = new(SupplyGroup.Healing);

    [SerializeField]
    private SyncVar<float> arrivalSeconds = new(15f);

    [SerializeField]
    private SyncVar<float> elapsed = new(0f, 0.5f);

    [SerializeField]
    private SyncVar<bool> landed = new(false);

    // Direction the helicopter flies in from.
    [SerializeField]
    private SyncVar<float> approachYaw = new(0f);

    private float localElapsed;
    private MaterialPropertyBlock block;

    // Server only.
    private float crateSeconds = 60f;
    private readonly HashSet<PlayerID> takenBy = new();

    public SupplyGroup Group => group.value;
    public bool HasLanded => landed.value;

    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    private void Awake() {
        elapsed.onChanged += ElapsedSynced;
        UpdateVisuals();
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        elapsed.onChanged -= ElapsedSynced;
    }

    // Server: right after spawning.
    public void SetupServer(WalkieTalkieDefinition def, SupplyGroup chosen) {
        if (!isServer || !def) {
            return;
        }
        group.value = chosen;
        arrivalSeconds.value = def.arrivalSeconds;
        crateSeconds = def.crateSeconds;
        approachYaw.value = Random.Range(0f, 360f);
        localElapsed = 0f;
        elapsed.value = 0f;
    }

    private void Update() {
        localElapsed += Time.deltaTime;
        if (isServer) {
            elapsed.value = localElapsed;
            if (!landed.value && localElapsed >= arrivalSeconds.value) {
                landed.value = true;
            }
            if (landed.value && localElapsed >= arrivalSeconds.value + crateSeconds) {
                Despawn();
                return;
            }
        }
        UpdateVisuals();
    }

    private void ElapsedSynced(float value) {
        if (!isServer) {
            localElapsed = value;
        }
    }

    private void UpdateVisuals() {
        var code = definition ? definition.CodeFor(group.value) : null;
        var color = code != null ? code.color : Color.white;
        var t = localElapsed;
        var arrival = arrivalSeconds.value;
        var dropStart = arrival - crateFallSeconds;
        var spot = transform.position;
        block ??= new MaterialPropertyBlock();

        if (marker) {
            var showMarker = !landed.value;
            if (marker.gameObject.activeSelf != showMarker) {
                marker.gameObject.SetActive(showMarker);
            }
            if (showMarker && markerRenderer) {
                var c = color;
                c.a = markerAlpha * (0.75f + 0.25f * Mathf.Sin(Time.time * 3f));
                block.SetColor(BaseColor, c);
                markerRenderer.SetPropertyBlock(block);
            }
        }

        if (helicopter) {
            var direction = Quaternion.Euler(0f, approachYaw.value, 0f) * Vector3.forward;
            var along = (t - dropStart) * flightSpeed;
            var showHeli = Mathf.Abs(along) <= flightVisibleDistance;
            if (helicopter.gameObject.activeSelf != showHeli) {
                helicopter.gameObject.SetActive(showHeli);
            }
            if (showHeli) {
                helicopter.SetPositionAndRotation(spot + Vector3.up * flightHeight + direction * along,
                    Quaternion.LookRotation(direction));
                if (rotor) {
                    rotor.Rotate(Vector3.up, rotorDegreesPerSecond * Time.deltaTime, Space.Self);
                }
            }
            if (helicopterAudio && helicopterAudio.clip && helicopterAudio.isPlaying != showHeli) {
                if (showHeli) {
                    helicopterAudio.Play();
                } else {
                    helicopterAudio.Stop();
                }
            }
        }

        if (crate) {
            var showCrate = landed.value || t >= dropStart;
            if (crate.gameObject.activeSelf != showCrate) {
                crate.gameObject.SetActive(showCrate);
            }
            if (showCrate) {
                var fall = landed.value ? 1f : Mathf.Clamp01((t - dropStart) / crateFallSeconds);
                crate.position = spot + Vector3.up * (flightHeight * (1f - fall));
                if (crateRenderer) {
                    block.SetColor(BaseColor, color);
                    crateRenderer.SetPropertyBlock(block);
                }
                if (crateLabel) {
                    crateLabel.text = code != null ? code.label : group.value.ToString();
                    crateLabel.color = color;
                    var cam = Camera.main;
                    if (cam) {
                        crateLabel.transform.rotation = Quaternion.LookRotation(crateLabel.transform.position - cam.transform.position);
                    }
                }
            }
            if (crateCollider && crateCollider.enabled != landed.value) {
                crateCollider.enabled = landed.value;
            }
        }
    }

    // E on the landed crate: hunters only.
    public void Interact(PlayerNetworkController player) {
        if (!landed.value || !player.TryGetComponent(out HunterLoadout _)) {
            return;
        }
        RequestTake(player);
    }

    [ServerRpc(requireOwnership: false)]
    private void RequestTake(NetworkIdentity takerIdentity, RPCInfo rpc = default) {
        var body = takerIdentity ? takerIdentity.GetComponent<PlayerNetworkController>() : null;
        if (!landed.value || !body || !body.owner.HasValue || body.owner.Value != rpc.sender) {
            return;
        }
        if (!body.TryGetComponent(out PlayerInfo info) || info.Team != Team.Hunters) {
            return;
        }
        var health = body.GetComponent<PlayerHealth>();
        if (health && health.IsDead) {
            return;
        }
        // Every hunter once.
        if (!takenBy.Add(rpc.sender)) {
            return;
        }
        if (group.value == SupplyGroup.Healing && health) {
            health.RestoreFull();
        }
        Granted(rpc.sender, group.value);
    }

    // Taker's machine: ammo and cages are kept there.
    [TargetRpc]
    private void Granted(PlayerID target, SupplyGroup granted) {
        var local = PlayerNetworkController.Local;
        if (!local) {
            return;
        }
        var weapons = local.GetComponent<HunterWeapons>();
        switch (granted) {
            case SupplyGroup.Capture:
                if (weapons) {
                    weapons.Refill(WeaponEffect.Net);
                }
                if (local.TryGetComponent(out HunterLoadout loadout)) {
                    loadout.RefillCages();
                }
                break;
            case SupplyGroup.Damage:
                if (weapons) {
                    weapons.Refill(WeaponEffect.Damage, WeaponEffect.Tranq);
                }
                break;
        }
    }
}
