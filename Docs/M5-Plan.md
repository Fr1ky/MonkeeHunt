# M5 — Minigames & Capture: Plan

> Design decisions are in [GDD.md](GDD.md) §5.4 and §6.4.
> All technical choices and starting numbers were approved on 2026-10-18.
> Head-ride (and stealing keys by riding) is M6; the walkie-talkie call-in minigame is M7.
> **Status: Done (accepted 2026-10-18).** Host-verified step by step, then the full multiplayer checklist ([M5-TestChecklist.md](M5-TestChecklist.md)) passed.

## Done when
- Arrow-key direction minigame with on-screen arrows; a wrong key restarts with a new sequence.
- Netted players (monkeys, or hunters hit by friendly fire) escape by finishing the minigame and keep at it the whole time they're netted.
- A teammate looks at a netted player + E → shorter rescue minigame; moving away or getting hit cancels it.
- A hunter looks at a netted monkey + E: no resistance if it's asleep, otherwise a race (hunter 8 keys vs the monkey's escape).
- The winning hunter carries the monkey (~70% speed, no shooting). G drops it still netted, and the carrier being netted/asleep/tripped/killed drops it too.
- Cages: a Cage loadout item (2 per slot). With a cage slot selected, LMB places it: with the carried monkey inside, or empty. E picks an empty cage back up.
- A caged monkey counts as captured, can look around and throw poop, and can still be shot dead.
- Keys: the locking hunter holds the cage's key (HUD + visible on their body). Keys drop on netting/caging/death; anyone can pick them up with E. A monkey with the key + E on the cage = instant unlock.
- A monkey teammate looks at a cage + E → unlock minigame.

---

## Tasks

### 1. Shared minigame
- `DirectionMinigame` (local logic + UI): given a length, generates a random arrow sequence, reads only the arrow keys, restarts with a new sequence on a wrong key, reports success.
- New input actions **Up / Down / Left / Right** on the arrow keys, and **Drop (G)**.
- UI: a row of arrow icons with the current one highlighted, plus a short title ("Escape!", "Rescuing…", "Picking up…", "Unlocking…").
- **Approved lengths:**

| Use | Keys |
|---|---|
| Escape (monkey or hunter) | 6 |
| Rescue (teammate) | 5 |
| Pickup race (hunter) | 8 |
| Cage unlock (monkey teammate) | 6 |

- **Approved:** the player doing the minigame reports success to the server, and the server accepts the first valid one (that's how the race is decided). Same trust model as hits.

### 2. Net states (extends `NetTrap`)
- Server-held state: **Free → Netted → Carried (by hunter X) → Caged (in cage Y)**, synced.
- Escape runs on the netted player's machine whenever they're Netted or Carried and awake.
- Rescue: E on a netted teammate → the rescuer's minigame; cancelled by leaving 2.5 m reach or by a status effect.
- F8 debug free stays (editor/dev builds).

### 3. Pickup & carry
- `HunterCarry` on the Hunter: E on a netted monkey → instant pickup if asleep, else the race (the monkey's escape is already running).
- While carrying: movement ×0.7, firing blocked, only cage slots selectable, **G** drops.
- **Approved (for now):** the carried monkey's own machine glues its body to a carry point above the hunter's shoulder each frame (body kinematic while carried). This keeps the existing "owner moves its own body" model.
- Carrier netted / asleep / tripped / killed → the server drops the monkey (still netted) at the carrier's feet.

### 4. Cages
- `LoadoutItem.Cage` added to the lobby loadout station. One slot = a stack of 2 cages, and the same item can fill both slots.
- **Cage** network prefab: placed by the server at the spot in front of the hunter, holding the occupant and the key holder (synced).
- LMB with a cage slot: places a cage, with the carried monkey inside if any, otherwise empty. E on an empty cage: back into a cage slot with room.
- **Approved:** the cage bars are visual only (no colliders), so poop thrown from inside and bullets from outside pass through. A low base collider is what you aim E at. The caged body is held in the cage's centre.
- **Approved:** while caged, the monkey can look, throw poop only (other slots blocked) and **eat** (to refill poop). It can't move, climb, swing or pick things up.

### 5. Keys
- Each cage has one key. `KeyRing` (networked) on players lists the keys a body carries; the HUD shows "Keys: N". A small key model on the body shows while holding ≥1.
- **Approved:** a hunter killed while holding keys **drops them too** (the GDD only says monkeys drop them).
- **Decided 2026-10-18:** hunters also drop keys when **netted**. A key vanishes once its cage is empty again (freed or killed inside). Holders show a gold key at the hip plus a key icon next to the name tag.
- Dropped keys: a networked **GroundKey** object; anyone picks it up with E.
- A monkey holding a cage's key + E on that cage = instant unlock. Without the key: the unlock minigame.
- Unlocking frees the monkey (not netted). The empty cage stays and can be retrieved.

### 6. Round integration
- The alive count treats **caged = out** (captured). Killed in a cage = dead.
- A freed monkey counts as alive again.

---

## Suggested build order
1. Input actions + `DirectionMinigame` + UI.
2. Net states + escape + rescue (also netted hunters).
3. Pickup race + carry + drop.
4. Cage item, placing, retrieving, caged state, round counting.
5. Keys: holder, HUD, body model, drops, pickup, instant unlock + unlock minigame.
6. Two-player test.

## Progress
- Steps 1–3: built and checked as host.
- Step 4 (cages): built and checked as host with fake monkey bodies: a carried monkey goes into the cage (held in the middle, the hunter holds the key), the cage count goes 2 → 1, the round counts the caged monkey as out, and caging the last monkey ends the round as a hunter win. Killed in the cage → the cage empties. Empty cages place and pick back up with E (one pickup at a time).
- Step 4 small calls (flagged, change any): cage size 1.5 × 1.5 m base, ~2 m tall, placed 1.6 m in front; the hand-held cage model is shrunk to sit low-right like the other items; a cage slot with 0 cages left stays selectable and still shows the cage in hand.
- Fixed during step 4: picking up an empty cage lost the "you got it" reply because the cage disappeared first. The reply now comes from the hunter instead.
- Step 5 (keys): built and checked as host with fake monkey bodies: the locking hunter gets the key ("Keys: 1" on the HUD, gold key at the hip). Netting the holder drops it on the ground; a monkey picks it up with E (hip key + name tag icon show); killing that monkey drops it where it died; a monkey with the key + E on the cage frees the caged monkey instantly and the key vanishes. Without the key, E starts the 6-key unlock minigame; walking away cancels it, finishing it frees the monkey and removes the key lying on the ground. E on the caged monkey's body counts as E on its cage. A round ending with a key on the ground returns to the lobby cleanly.
- Step 5 small calls (flagged, change any): keys drop at the holder's feet (several keys spread 0.4 m apart); the ground key is a bigger gold key lying flat, spinning and bobbing; the hip key hangs on the right hip and is hidden for its own owner like the rest of the body; the name tag icon sits left of the name; "Keys: N" is gold, bottom-left above the team label; unlock reach is 2.5 m from the cage (same as rescue); hunters can hold keys but E on an occupied cage does nothing for them.
- Noticed in testing: cages placed on the same spot stack on top of each other (nothing stops overlapping cages).
- **Decided 2026-10-18:** a dropped key lands where its holder last stood on solid ground (a holder who dies in the pit leaves the key at the edge). Checked as host: netted drop at the feet, pit death drop at the edge.
- Step 6: the multiplayer test checklist in [M5-TestChecklist.md](M5-TestChecklist.md) all passed (2026-10-18).
- Not tested yet (needs two players): a real remote monkey caged (its view, poop from inside, eating), two hunters picking up the same empty cage at once, the hip key / name tag icon as seen by another player, a key holder leaving the game, two players grabbing the same dropped key, two monkeys unlocking the same cage at once. (Fake bodies share the host's player id, so the "has this cage's key?" check was only tested where that couldn't mix them up.)

## Open points
- Carry alternative to try if the glue approach trails visibly in playtests: PurrNet parent sync (`NetworkTransform` syncParent under a networked carry socket on the hunter). Watch out: the carrier despawning would destroy a still-parented monkey, so unparent first.
