# M4 — Monkey Kit: Plan

> Design decisions are in [GDD.md](GDD.md) §5.5 and §7.1–7.3.1.
> All technical choices and starting numbers were approved on 2026-10-18.
> Head-ride is M6, quicksand M7.
> **Status: Done (accepted 2026-10-18).** Host-verified step by step. Not yet two-player tested: climbing/swinging with a real keyboard, remote throws, splat + X wipe, peel trip seen remotely, decoy vs a real hunter, pickup races; check during later playtests.

## Done when
- Monkeys are smaller with a lower camera, run faster and jump higher.
- Jump onto a tagged tree/rock to climb (W/S/A/D, Jump leaps off). Jump near a swing point to swing (W/S pump, Jump releases with momentum). Hunters can't do either.
- Monkeys have slots 1 Poop, 2 Rock, 3 Banana peel, 4 Decoy, with keys 1–4 and the wheel. Hold LMB to charge a throw.
- E picks up rocks, fruit and bananas (carry caps). F eats food (+3 poop, a banana also gives a peel). Pickup spots refill after ~30 s.
- Poop hits stack splats on the target's screen; hold X ~2 s to wipe (partial if stopped). This works on monkeys too.
- Rocks make noise where they land (sound hook, silent for now) and flinch a hunter they hit.
- Peels stay on the ground until stepped on: 20 damage, the body tips over for 3 s. Monkeys trip too.
- Decoys lie until a hunter comes within 4 m, then screech (hook, silent for now) and vanish.
- Monkey HUD: slot bar with counts, poop bar, food count, wipe progress.
- The Blockout map gets climbable trees/rocks, a swing line and respawning pickup spots.

---

## Tasks

### 1. Monkey body & movement
- Monkey prefab variant overrides: capsule ~1.1 m tall, eye height ~0.9 m, smaller body visual, name tag lower.
- **Changed 2026-10-20:** monkeys are now **half size** (~0.55 m tall, eyes ~0.45 m; hitbox, legs spring, name tag, net, key, swing grip and quicksand depth halved, so the step height is ~0.22 m) and **jump twice as high as before** (~2 m; jump strength 227.7 → 322).
- **Approved numbers:**
  - First, **all players move ×1.65** (walk 90 → 148.5, sprint bonus 40 → 66), set on PlayerBase.
  - Monkeys then get **another ×1.2** (walk 178.2, sprint bonus 79.2) and **~60% higher jumps**, set on the Monkey variant.
- Note: step height (0.45 m) stays the same.
- **Changed 2026-10-22 (M9 feel pass):** everyone +30% (hunters walk 5.5 / sprint 8.0 m/s, monkeys 6.6 / 9.6 m/s); jumps are set in metres (hunters 1.0 m, monkeys 2.5 m, raised to 3.25 m on 2026-10-23) with heavier gravity in the air (×1.5 rising, ×2 falling); a full-power throw charges in 0.6 s (was 1 s); swing pump +30%.

### 2. Climbing
- `Climbable` marker component on trees/rocks (**Approved:** component rather than a Unity tag, so other data such as climb speed can live on it later).
- `MonkeyClimb` (monkey only): Jump while touching a climbable surface → stick to it. W/S/A/D move along the surface, Jump leaps off (up + away from the surface). Built on the ported controller's existing climbing mode (ladder).
- **Approved:** leap-off 6 m/s away + 4 m/s up. No stamina for now.
- **Changed 2026-10-18:** climb speed = the monkey's ground speed (walk ~5.1 m/s, Shift sprint ~7.4 m/s), replacing the original 3 m/s.

### 3. Swinging
- `SwingPoint` component on placed vine anchors.
- `MonkeySwing` (monkey only): Jump while within grab range of a swing point → attach on a rope of the current length (physics pendulum). W/S add force along the swing direction. Jump releases with the current velocity plus a small upward boost.
- **Approved:** grab range 3 m; the nearest point in front of you wins; pump force tunable; a rope line visual (a stretched thin cylinder) shown to everyone.

### 4. Monkey slots, pickups, eating
- `MonkeyInventory` (networked, owner-side counts like hunter ammo): poop 5/5, rocks 0/3, peels 0/2, decoys 3/3, food 0/3. Slot switching reuses the hunter-style input (1–4 + wheel).
- `PickupSpot` (networked, server state): type Rock / Fruit / Banana, "available" synced, refills after 30 s. E on it → server checks it's still available and tells the picker to add the item (if under the cap).
- F eats one food: +3 poop (cap 5). A banana also adds 1 peel (cap 2).
- **Approved:** a new **Eat (F)** and **Wipe (X)** input action.

### 5. Throwing
- Hold LMB to charge (0 → 1 s), release to throw from the camera along a gravity arc. Same "thrower decides hits" approach as hunter guns. Others see a cosmetic copy.
- **Approved throw speeds (min → max charge):** poop 8 → 18 m/s, rock 10 → 22, peel 6 → 14, decoy 8 → 18.
  - **Changed 2026-10-20:** poop 10 → 22 m/s with gravity ×0.7 (about twice the range: a full throw aimed 10° up lands ~26 m away instead of ~13 m).
- On landing:
  - **Poop:** a player hit → splat. A miss disappears (decals later).
  - **Rock:** noise at the landing point (sound hook). A hunter hit → small camera flinch.
  - **Peel / decoy:** the server spawns a **networked ground object** at the landing point, so everyone sees the same one.

### 6. Effects
- **Splat** (`SplatStatus`, owner-side screen effect, triggered by the server):
  - each hit adds a blob (random screen position/size, up to ~8 visible);
  - hold X: progress bar, all blobs fade together; releasing early leaves them partly faded;
  - works on hunters and monkeys.
- **Peel trip** (server detects someone stepping on a peel):
  - 20 damage, removes the peel, tells the owner to topple;
  - owner: the **Visualisation** (body mesh, head and camera) tips over in a short animation, lies there for 3 s with move/look/actions blocked, then stands back up;
  - others see it through Visualisation's existing rotation sync. The root is never rotated;
  - **approved trade-off:** the collider stays upright while the body lies on the ground, so shots hit the standing capsule for those 3 s.
- **Decoy** (networked ground object): the server checks for hunters within 4 m → plays the screech for everyone (sound hook) → despawns.

### 7. HUD
- Monkey HUD: slot bar 1–4 with counts, poop bar (5 pips), food count, throw charge meter, wipe progress bar.
- Hunter HUD also gets the wipe progress bar (hunters get splatted too).
- **Changed 2026-10-23 (your call):** splats are bigger (45–80% of screen height, was 30–55%), solid in the centre and fading to 70% opaque at the edges; a "Hold X to wipe" prompt shows above the crosshair while there's poop on screen, and the wipe bar moved there too (it used to overlap the minigame panels).

### 8. Blockout additions
- 4–5 climbable "trees" (tall brown pillars) and 2 boulders, a line of 4 swing points between two trees, 3 rock piles, 3 fruit bushes and 2 banana trees (with bananas on the ground beneath).

---

## Suggested build order
1. Monkey body + movement numbers.
2. Climbing.
3. Swinging.
4. Inventory, pickup spots, eating, HUD basics.
5. Throwing (charge, arc, cosmetic copies) + rocks.
6. Poop + splat + wipe.
7. Banana peels + topple.
8. Decoys.
9. Blockout props, two-player test.

## Open points
- None.
