using PurrNet;
using UnityEngine;

// Monkey throwing. Hold left mouse to charge (0 -> 1 over chargeSeconds), release to throw the selected
// item along a gravity arc from the camera. Works anytime (ground, air, climbing, swinging).
// Like hunter guns, the thrower's copy decides what it hit; everyone else sees a cosmetic copy.
// Landing effects: poop = splat on whoever it hits; rock = noise + flinch on a hunter;
// peel / decoy = networked object on the ground where it lands.
// Hub (no round): throws only affect the test dummies; peels and decoys just puff where they land.
public class MonkeyThrow : NetworkBehaviour {

    [SerializeField]
    private PlayerNetworkController player;

    [SerializeField]
    private PlayerInputReader input;

    [SerializeField]
    private MonkeyInventory inventory;

    [SerializeField]
    private MouseAimController mouseAim;

    // Caged monkeys can only throw poop.
    [SerializeField]
    private NetTrap net;

    // Indexed by MonkeyItem (Poop, Rock, Peel, Decoy).
    [SerializeField]
    private ThrowableDefinition[] throwables = new ThrowableDefinition[MonkeyInventory.SlotCount];

    [SerializeField]
    private LayerMask hitLayers = ~0;

    [SerializeField, Min(0.05f)]
    private float chargeSeconds = 1f;

    // Spawn a bit in front of the camera so the throw doesn't start inside nearby geometry.
    [SerializeField]
    private float spawnForward = 0.3f;

    [SerializeField]
    private float rockFlinchDegrees = 3f;

    // Networked peel the server places where a thrown peel lands.
    [SerializeField]
    private GameObject groundPeelPrefab;

    [SerializeField]
    private GameObject groundDecoyPrefab;

    [SerializeField]
    private LayerMask groundLayers = ~0;

    private bool charging;
    private float chargeStart;

    public bool IsCharging => charging;
    public float Charge01 => charging ? Mathf.Clamp01((Time.time - chargeStart) / chargeSeconds) : 0f;

    private void Update() {
        if (!isOwner) {
            return;
        }
        if (player.ActionsBlocked) {
            charging = false;
            return;
        }
        if (!charging) {
            if (input.AttackPressed && inventory.Count(inventory.Selected) > 0) {
                charging = true;
                chargeStart = Time.time;
            }
            return;
        }
        if (input.AttackReleased || !input.AttackHeld) {
            var charge = Charge01;
            charging = false;
            Throw(charge);
        }
    }

    private void Throw(float charge) {
        var item = inventory.Selected;
        if (net && net.State == NetState.Caged && item != MonkeyItem.Poop) {
            return;
        }
        var def = Definition(item);
        if (!def || !inventory.TryConsume(item)) {
            return;
        }
        var ray = mouseAim.GetCameraRay();
        var origin = ray.origin + ray.direction * spawnForward;
        var velocity = ray.direction * Mathf.Lerp(def.minSpeed, def.maxSpeed, charge);
        SpawnProjectile(def, origin, velocity, (hitCollider, point) => OnOwnThrowLanded(item, hitCollider, point));
        PlaySound(def.throwSound, origin);
        ThrowServer((int)item, origin, velocity);
    }

    private void OnOwnThrowLanded(MonkeyItem item, Collider hitCollider, Vector3 point) {
        // The thrower may have died (or the round ended) while the throw was still flying.
        if (!this || !isSpawned) {
            return;
        }
        if (HunterLoadout.InHub) {
            OnHubThrowLanded(item, hitCollider, point);
            return;
        }
        switch (item) {
            case MonkeyItem.Poop:
                var splatTarget = hitCollider.GetComponentInParent<SplatStatus>();
                if (splatTarget && splatTarget.gameObject != gameObject) {
                    // Another monkey = friendly fire.
                    HitMarkerHud.Show(splatTarget.GetComponent<HunterLoadout>() ? HitMarkerKind.Hit : HitMarkerKind.Friendly);
                    ReportPoopHit(splatTarget);
                }
                break;
            case MonkeyItem.Peel:
                PeelLanded(point);
                break;
            case MonkeyItem.Decoy:
                DecoyLanded(point);
                break;
            case MonkeyItem.Rock:
                RockLanded(point);
                var target = hitCollider.GetComponentInParent<PlayerFlinch>();
                if (target && target.gameObject != gameObject) {
                    // Rocks only affect hunters, so only they get a marker.
                    if (target.GetComponent<HunterLoadout>()) {
                        HitMarkerHud.Show(HitMarkerKind.Hit);
                    }
                    ReportRockHit(target);
                }
                break;
        }
    }

    // Hub practice: dummies react to poop and rocks, rocks still make their noise, nothing else happens.
    private void OnHubThrowLanded(MonkeyItem item, Collider hitCollider, Vector3 point) {
        if (item == MonkeyItem.Rock) {
            RockLanded(point);
        }
        var dummy = hitCollider.GetComponentInParent<TestDummy>();
        if (dummy && (item == MonkeyItem.Poop || item == MonkeyItem.Rock)) {
            HitMarkerHud.Show(HitMarkerKind.Hit);
            ReportDummyThrowHit(dummy, (int)item);
        }
    }

    [ServerRpc]
    private void ReportDummyThrowHit(NetworkIdentity dummyIdentity, int item) {
        if (dummyIdentity && dummyIdentity.TryGetComponent(out TestDummy dummy)) {
            dummy.ThrowHitServer((MonkeyItem)item);
        }
    }

    [ServerRpc]
    private void ThrowServer(int item, Vector3 origin, Vector3 velocity) {
        ThrowObservers(item, origin, velocity);
    }

    [ObserversRpc(excludeOwner: true)]
    private void ThrowObservers(int item, Vector3 origin, Vector3 velocity) {
        var def = Definition((MonkeyItem)item);
        if (def) {
            SpawnProjectile(def, origin, velocity, null);
            PlaySound(def.throwSound, origin);
        }
    }

    [ServerRpc]
    private void PeelLanded(Vector3 point) {
        SpawnOnGround(groundPeelPrefab, point);
    }

    [ServerRpc]
    private void DecoyLanded(Vector3 point) {
        SpawnOnGround(groundDecoyPrefab, point);
    }

    // Server: places a networked object on the ground below where a throw landed.
    private void SpawnOnGround(GameObject prefab, Vector3 point) {
        if (!prefab) {
            return;
        }
        // Settle on the ground below the landing point (it may have hit a wall or a player).
        var position = point;
        if (Physics.Raycast(point + Vector3.up * 0.5f, Vector3.down, out var hit, 4f, groundLayers,
                QueryTriggerInteraction.Ignore)) {
            position = hit.point;
        }
        var rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        var spawned = UnityProxy.Instantiate(prefab, position, rotation, gameObject.scene);
        networkManager.Spawn(spawned);
    }

    [ServerRpc]
    private void RockLanded(Vector3 point) {
        RockNoise(point);
    }

    // Everyone (including the thrower) hears the rock where it lands.
    [ObserversRpc]
    private void RockNoise(Vector3 point) {
        var def = Definition(MonkeyItem.Rock);
        if (def) {
            PlaySound(def.impactSound, point);
        }
    }

    // Poop splats anyone it hits (hunters and other monkeys).
    [ServerRpc]
    private void ReportPoopHit(NetworkIdentity target) {
        if (target && target.TryGetComponent(out SplatStatus splat)) {
            splat.AddSplatServer();
            if (target.TryGetComponent(out HitFeedback feedback)) {
                feedback.NotifyServer(transform.position, HitFeedbackKind.Splat);
            }
        }
    }

    [ServerRpc]
    private void ReportRockHit(NetworkIdentity target) {
        // Rocks only jolt hunters.
        if (target && target.TryGetComponent(out PlayerFlinch flinch) && target.TryGetComponent(out HunterLoadout _)) {
            flinch.FlinchServer(rockFlinchDegrees);
            if (target.TryGetComponent(out HitFeedback feedback)) {
                feedback.NotifyServer(transform.position, HitFeedbackKind.Hurt);
            }
        }
    }

    private ThrowableDefinition Definition(MonkeyItem item) {
        var index = (int)item;
        return index >= 0 && index < throwables.Length ? throwables[index] : null;
    }

    private void SpawnProjectile(ThrowableDefinition def, Vector3 origin, Vector3 velocity,
        System.Action<Collider, Vector3> onHit) {
        if (!def.projectilePrefab) {
            return;
        }
        var go = Instantiate(def.projectilePrefab, origin, Quaternion.identity);
        go.GetComponent<Projectile>().Launch(def.Motion, origin, velocity, transform, hitLayers, onHit);
    }

    private static void PlaySound(AudioClip clip, Vector3 point) {
        if (clip) {
            AudioSource.PlayClipAtPoint(clip, point);
        }
    }
}
