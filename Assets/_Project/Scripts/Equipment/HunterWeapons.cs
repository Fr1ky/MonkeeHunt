using System.Collections.Generic;
using PurrNet;
using UnityEngine;

// Firing for the hunter's three guns. The owner simulates its own projectile and decides hits
// (trusted friends); the server applies the effect. Everyone else sees a cosmetic projectile.
// Ammo lives on the owner's machine only.
public class HunterWeapons : NetworkBehaviour {

    private const float DryFireDelay = 0.2f;

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
        if (input.AttackPressed && !(carry && carry.IsCarrying)) {
            TryFire(weapon);
        }
    }

    private void TryFire(WeaponDefinition weapon) {
        if (IsReloading || !loadout.IsEquipReady || Time.time < nextFireTime) {
            return;
        }
        var state = StateFor(weapon);
        if (state.Magazine <= 0) {
            if (state.Reserve > 0) {
                StartReload(weapon);
            } else {
                PlaySound(weapon.dryFireSound, weapon.pitch);
                nextFireTime = Time.time + DryFireDelay;
            }
            return;
        }

        state.Magazine--;
        nextFireTime = Time.time + weapon.cooldown;
        var ray = mouseAim.GetCameraRay();
        var slot = loadout.SelectedSlot;
        SpawnProjectile(weapon, ray.origin, ray.direction, (hitCollider, point) => OnOwnProjectileHit(slot, hitCollider));
        PlaySound(weapon.fireSound, weapon.pitch);
        mouseAim.Kick(weapon.cameraKick);
        FireServer(slot, ray.origin, ray.direction);
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
        }
    }

    [ServerRpc]
    private void ReportHit(NetworkIdentity target, int slot) {
        if (HunterLoadout.InHub || !target || loadout.GetSlot(slot) is not WeaponDefinition weapon) {
            return;
        }
        WeaponEffects.ApplyServer(weapon, target.gameObject);
    }

    [ServerRpc]
    private void ReportDummyHit(NetworkIdentity dummyIdentity, int slot) {
        var dummy = dummyIdentity ? dummyIdentity.GetComponent<TestDummy>() : null;
        if (dummy && loadout.GetSlot(slot) is WeaponDefinition weapon) {
            dummy.HitServer(weapon);
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
