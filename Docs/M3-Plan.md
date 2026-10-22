# M3 — Hunter Kit: Plan

> Design decisions are in [GDD.md](GDD.md) §6.1–6.2.1.
> All technical choices and starting numbers were approved on 2026-10-17.
> **Status: Done (accepted 2026-10-17).** Host-verified: loadout + switching, firing/ammo/reload, rifle damage, tranq stacks/sleep, net trap + F8, gun zoom, binoculars + scroll zoom (2x-8x), lobby loadout station. Not yet two-player tested: remote held weapons, tracers and shot sounds, friendly fire between real players; check during later playtests.

## Done when
- Hunters spawn with **hunting rifle (1), tranq gun (2), net launcher (3)** and their lobby-picked **slots 4–5**. Only binoculars exist yet; cage and walkie-talkie arrive in M5/M7.
- Number keys 1–5 and the mouse wheel switch slots, and other players see what you're holding.
- Guns fire single projectiles with a cooldown, 6-round magazines and 32 in reserve. **R** reloads, and firing an empty magazine auto-reloads.
- Rifle hits deal 5 damage. Tranq hits stack slow + drunk screen, and 4 stacks = asleep. Net hits trap the target in place. All of it affects hunters too (friendly fire).
- Hold right mouse to aim (small zoom) with a gun, or to zoom ~4x with binoculars.
- HUD: crosshair, ammo (magazine / reserve), slot bar, reload indicator.
- Placeholder gun models, projectile visuals and the reference rifle SFX.
- A debug key frees netted players (until M5).

---

## Tasks

### 1. Weapon data
- `WeaponDefinition` ScriptableObject per gun: name, slot, projectile prefab, speed, gravity, lifetime, cooldown, magazine size, reserve ammo, reload time, camera kick, effect type (Damage / Tranq / Net) and amount, view model and sounds.
- **Starting numbers** (approved 2026-10-17; all live in the assets and are easy to tune):

| | Rifle | Tranq | Net |
|---|---|---|---|
| Projectile speed | 100 m/s | 50 m/s | 20 m/s |
| Gravity | none | light | yes (arcing) |
| Lifetime / range | 0.5 s (50 m) | 1.5 s (~75 m) | 2 s |

**Changed 2026-10-20:** the net launcher now fires at 30 m/s with gravity ×0.6 and lasts 2.5 s (a level shot lands ~20 m away instead of ~11 m), and the net over a trapped body is a draped rope mesh.

**Changed 2026-10-22 (M9 feel pass, [M9-Plan.md](M9-Plan.md)):** rifle 300 m/s (lifetime 0.17 s, still 50 m), 0.5 s between shots, reload 1.3 s; tranq 80 m/s (lifetime 0.94 s, still ~75 m), reload 1.3 s; net reload 1.5 s; switching takes 0.15 s; the camera kick now recovers.
| Cooldown between shots | 0.7 s | 0.8 s | 1.2 s |
| Reload | 1.7 s | 1.75 s | 2.0 s |
| Camera kick | 2° | 1° | 3° |
| Magazine / reserve | 6 / 32 | 6 / 32 | 6 / 32 |

- Weapon switch time: **0.25 s** (the reference `equipDuration`).

### 2. Hunter loadout & switching
- `HunterLoadout` (networked) on the Hunter prefab. It holds the 5 slots and the **selected slot**, which is synced so others see the held weapon.
- **Approved:** ammo per weapon lives on the **owner's machine only** (only they need it). Resupply in M7 tells the owner to refill.
- **Replaced 2026-10-20** by the fully customizable loadout and hub racks ([Loadout-Hub-Plan.md](Loadout-Hub-Plan.md)).
- Lobby **loadout station**: a second console in the hub. Walk up + E opens a picker for slot 4 and slot 5 (options for now: Binoculars, Empty). The choice is stored per player on the server and applied when the round body spawns.
- **Approved:** default loadout if nothing is picked = slot 4 binoculars, slot 5 empty.

### 3. Firing & projectiles
- The shooter spawns and simulates its own projectile, moving it each frame with a sphere-cast against the world and other players.
- On hit, the shooter reports it to the server (target + weapon), and the server applies the effect.
- Other players get a **cosmetic** copy of the projectile (spawn message → local visual, no hit logic).
- **Approved:** a projectile can't hit its own shooter. Hits on a player's capsule use the Player layer.

### 4. Effects
- **Damage:** server calls `PlayerHealth.ApplyDamage` (already exists).
- **Tranq** — `TranqStatus` (networked, server-owned stacks):
  - **Proposed set (approved):** 4 hits = asleep. Each stack: −15% move speed and a stronger drunk wobble. A stack decays every 4 s without a new hit. Sleep 8 s: can't move or look, screen dark. You wake with 0 stacks.
  - **Approved drunk effect (simple, to be improved later):** camera roll/sway + slight FOV wobble + tinted vignette overlay, scaled by stacks. No post-processing volume yet.
- **Net** — `NetTrap` (networked): trapped flag, net visual around the body, movement locked (look stays free). **Approved debug key: F8** frees yourself, working in editor/dev builds only.
- Movement slow needs a speed multiplier on `PlayerMovementController` (a small addition to the ported controller).

### 5. Aim & binoculars
- Guns: hold RMB → FOV 75 → ~58 (1.3x), lerped.
- Binoculars: hold RMB → FOV ~19 (4x), mouse sensitivity scaled by the zoom, vignette overlay. Can't fire while zoomed (binoculars aren't a gun anyway).

### 6. Visuals, HUD, audio
- Placeholder view models: colored boxes per gun (rifle long brown, tranq short green, net launcher wide grey) and binoculars. The same models are held by remote players.
- Projectile visuals: rifle = small bright tracer, tranq = green dart, net = grey blob that opens on hit.
- HUD: centre crosshair, `6 / 32` ammo, 5-slot bar with the selected slot highlighted, reload progress.
- SFX: copy the reference rifle shot, dry fire and reload `.wav`s. Tranq and net reuse them at different pitch until real sounds exist.

### 7. Monkeys
- No weapons in M3. Tranq, net and rifle effects apply to them.

---

## Suggested build order
1. `WeaponDefinition` assets + `HunterLoadout` + switching + placeholder view models.
2. Firing, projectiles, hit reporting, rifle damage, ammo/reload, HUD.
3. Tranq status (slow, drunk, sleep, decay).
4. Net trap + debug free.
5. ADS + binoculars.
6. Lobby loadout station + applying picks at spawn.
7. SFX, polish, two-player test.

## Open points
- None.
