using System.Collections.Generic;
using PurrNet;
using UnityEngine;

// Firing for the hunter's three guns. The owner simulates its own projectile and decides hits
// (trusted friends); the server applies the effect. Everyone else sees a cosmetic projectile.
// Ammo lives on the owner's machine only.
public class HunterWeapons : NetworkBehaviour {

    private const float DryFireDelay = 0.2f;

    // A click this early (during the cooldown, a switch or the end of a reload) still fires once ready.
    private const float FireBufferSeconds = 0.15f;

    [SerializeField]
    private HunterLoadout loadout;

    [SerializeField]
    private PlayerInputReader input;

    [SerializeField]
    private PlayerNetworkController player;

    [SerializeField]
    private MouseAimController mouseAim;

    [SerializeField]
    private AudioSource audioSource;

    // Carrying a monkey blocks firing.
    [SerializeField]
    private HunterCarry carry;

    // The hand socket's motion (fire punch).
    [SerializeField]
    private ViewModelMotion viewMotion;

    [SerializeField]
    private LayerMask hitLayers = ~0;

    private class AmmoState {
        public int Magazine;
        public int Reserve;
    }

    private readonly Dictionary<WeaponDefinition, AmmoState> ammo = new();
    private float nextFireTime;
    private WeaponDefinition reloadingWeapon;
    private float reloadStartTime;
    private float reloadEndTime;
    private float fireQueuedUntil = -1f;

    public WeaponDefinition CurrentWeapon => loadout.Selected as WeaponDefinition;
    public bool IsReloading => reloadingWeapon;

    public float ReloadProgress => IsReloading
        ? Mathf.InverseLerp(reloadStartTime, reloadEndTime, Time.time)
        : 0f;

    public bool TryGetAmmo(WeaponDefinition weapon, out int magazine, out int reserve) {
        magazine = reserve = 0;
        if (!weapon) {
            return false;
        }
        var state = StateFor(weapon);
        magazine = state.Magazine;
        reserve = state.Reserve;
        return true;
    }

    // Ammo for a gun, created full the first time (guns can sit in any slot).
    private AmmoState StateFor(WeaponDefinition weapon) {
        if (!ammo.TryGetValue(weapon, out var state)) {
            state = new AmmoState { Magazine = weapon.magazineSize, Reserve = weapon.reserveAmmo };
            ammo[weapon] = state;
        }
        return state;
    }

    // Owner: full magazine and reserve for the guns with one of these effects (supply crates).
    public void Refill(params WeaponEffect[] effects) {
        for (int i = 0; i < HunterLoadout.SlotCount; i++) {
            if (loadout.GetSlot(i) is WeaponDefinition weapon && System.Array.IndexOf(effects, weapon.effect) >= 0) {
                ammo[weapon] = new AmmoState { Magazine = weapon.magazineSize, Reserve = weapon.reserveAmmo };
                if (reloadingWeapon == weapon) {
                    reloadingWeapon = null;
                }
            }
        }
    }

    // Owner: full magazines and reserves (round start).
    public void RefillAll() {
        ammo.Clear();
        for (int i = 0; i < HunterLoadout.SlotCount; i++) {
            if (loadout.GetSlot(i) is WeaponDefinition weapon && !ammo.ContainsKey(weapon)) {
                ammo[weapon] = new AmmoState { Magazine = weapon.magazineSize, Reserve = weapon.reserveAmmo };
            }
        }
    }

    private void Awake() {
        loadout.OnSelectionChanged += OnSelectionChanged;
        loadout.OnItemsChanged += OnItemsChanged;
    }

    // After Awake so the loadout has built its slots.
    private void Start() {
        RefillAll();
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        if (loadout) {
            loadout.OnSelectionChanged -= OnSelectionChanged;
            loadout.OnItemsChanged -= OnItemsChanged;
        }
    }

    // Hub rack picks: start the new set of guns full; a reload of a gun that left the loadout stops.
    private void OnItemsChanged() {
        if (reloadingWeapon && loadout.Selected != reloadingWeapon) {
            reloadingWeapon = null;
        }
        if (HunterLoadout.InHub) {
            RefillAll();
        }
    }

    private void Update() {
        if (!isOwner) {
            return;
        }
        if (IsReloading && Time.time >= reloadEndTime) {
            FinishReload();
        }
        if (player.ActionsBlocked) {
            return;
        }
        var weapon = CurrentWeapon;
        if (!weapon) {
            return;
        }
        if (input.ReloadPressed) {
            StartReload(weapon);
        }
        if (input.AttackPressed) {
            fireQueuedUntil = Time.time + FireBufferSeconds;
        }
        if (Time.time <= fireQueuedUntil && !(carry && carry.IsCarrying) && TryFire(weapon)) {
            fireQueuedUntil = -1f;
        }
    }

    // False while the gun isn't ready yet (the click stays queued); true once it fired, reloaded or clicked dry.
    private bool TryFire(WeaponDefinition weapon) {
        if (IsReloading || !loadout.IsEquipReady || Time.time < nextFireTime) {
            return false;
        }
        var state = StateFor(weapon);
        if (state.Magazine <= 0) {
            if (state.Reserve > 0) {
                StartReload(weapon);
            } else {
                PlaySound(weapon.dryFireSound, weapon.pitch);
                nextFireTime = Time.time + DryFireDelay;
            }
            return true;
        }

        state.Magazine--;
        nextFireTime = Time.time + weapon.cooldown;
        var ray = mouseAim.GetCameraRay();
        var slot = loadout.SelectedSlot;
        SpawnProjectile(weapon, ray.origin, ray.direction, (hitCollider, point) => OnOwnProjectileHit(slot, hitCollider));
        PlaySound(weapon.fireSound, weapon.pitch);
        PlayFireEffects(slot, weapon);
        mouseAim.Kick(weapon.cameraKick);
        FireServer(slot, ray.origin, ray.direction);
        return true;
    }

    private void StartReload(WeaponDefinition weapon) {
        var state = StateFor(weapon);
        if (IsReloading || state.Reserve <= 0 || state.Magazine >= weapon.magazineSize) {
            return;
        }
        reloadingWeapon = weapon;
        reloadStartTime = Time.time;
        reloadEndTime = Time.time + weapon.reloadTime;
        PlaySound(weapon.reloadSound, weapon.pitch);
    }

    private void FinishReload() {
        var weapon = reloadingWeapon;
        reloadingWeapon = null;
        var state = StateFor(weapon);
        var moved = Mathf.Min(weapon.magazineSize - state.Magazine, state.Reserve);
        state.Magazine += moved;
        // The hub's test range never uses up the reserve.
        if (!HunterLoadout.InHub) {
            state.Reserve -= moved;
        }
    }

    private void OnSelectionChanged(int slot) {
        fireQueuedUntil = -1f;
        // Switching away cancels a reload in progress.
        if (IsReloading && loadout.Selected != reloadingWeapon) {
            reloadingWeapon = null;
            if (audioSource) {
                audioSource.Stop();
            }
        }
    }

    private void OnOwnProjectileHit(int slot, Collider hitCollider) {
        // The shooter may have died (or the round ended) while the projectile was still flying.
        if (!this || !isSpawned) {
            return;
        }
        var dummy = hitCollider.GetComponentInParent<TestDummy>();
        if (dummy) {
            HitMarkerHud.Show(HitMarkerKind.Hit);
            ReportDummyHit(dummy, slot);
            return;
        }
        // In the hub, shots only affect the dummies.
        if (HunterLoadout.InHub) {
            return;
        }
        var target = hitCollider.GetComponentInParent<PlayerHealth>();
        if (!target || target.gameObject == gameObject) {
            return;
        }
        // Another hunter = friendly fire.
        HitMarkerHud.Show(target.GetComponent<HunterLoadout>() ? HitMarkerKind.Friendly : HitMarkerKind.Hit);
        ReportHit(target, slot);
    }

    [ServerRpc]
    private void FireServer(int slot, Vector3 origin, Vector3 direction) {
        FireObservers(slot, origin, direction);
    }

    [ObserversRpc(excludeOwner: true)]
    private void FireObservers(int slot, Vector3 origin, Vector3 direction) {
        if (loadout.GetSlot(slot) is WeaponDefinition weapon) {
            SpawnProjectile(weapon, origin, direction, null);
            PlaySound(weapon.fireSound, weapon.pitch);
            PlayFireEffects(slot, weapon);
        }
    }

    [ServerRpc]
    private void ReportHit(NetworkIdentity target, int slot) {
        if (HunterLoadout.InHub || !target || loadout.GetSlot(slot) is not WeaponDefinition weapon) {
            return;
        }
        // Before the effect: a killing hit despawns the target.
        if (target.TryGetComponent(out HitFeedback feedback)) {
            feedback.NotifyServer(transform.position, HitFeedbackKind.Hurt);
        }
        if (WeaponEffects.ApplyServer(weapon, target.gameObject) && owner.HasValue) {
            ConfirmDown(owner.Value);
        }
    }

    [ServerRpc]
    private void ReportDummyHit(NetworkIdentity dummyIdentity, int slot) {
        var dummy = dummyIdentity ? dummyIdentity.GetComponent<TestDummy>() : null;
        if (dummy && loadout.GetSlot(slot) is WeaponDefinition weapon && dummy.HitServer(weapon) && owner.HasValue) {
            ConfirmDown(owner.Value);
        }
    }

    // The shooter's big marker: the hit killed, put to sleep or netted its target.
    [TargetRpc]
    private void ConfirmDown(PlayerID target) {
        HitMarkerHud.Show(HitMarkerKind.Down);
    }

    // Muzzle flash on the gun model and the hand socket's punch (every peer).
    private void PlayFireEffects(int slot, WeaponDefinition weapon) {
        var model = loadout.ViewModel(slot);
        var muzzle = model ? model.GetComponentInChildren<MuzzleFlash>(true) : null;
        if (muzzle) {
            muzzle.Play();
        }
        if (viewMotion) {
            viewMotion.Punch(weapon.cameraKick * 0.5f);
        }
    }

    private void SpawnProjectile(WeaponDefinition weapon, Vector3 origin, Vector3 direction,
        System.Action<Collider, Vector3> onHit) {
        if (!weapon.projectilePrefab) {
            return;
        }
        var go = Instantiate(weapon.projectilePrefab, origin, Quaternion.LookRotation(direction));
        go.GetComponent<Projectile>().Launch(ProjectileMotion.From(weapon), origin, direction.normalized * weapon.projectileSpeed,
            transform, hitLayers, onHit);
    }

    private void PlaySound(AudioClip clip, float pitch) {
        if (!audioSource || !clip) {
            return;
        }
        audioSource.pitch = pitch;
        audioSource.PlayOneShot(clip);
    }
}
