# M7 — Hazards & Resupply: Plan

> Design decisions are in [GDD.md](GDD.md) §5.5 and §6.3 (all 20 answered on 2026-10-19).
> All technical choices, starting numbers and the call codes below were **approved on 2026-10-19** (marked "Proposed" where they were suggested).
> Banana trees and peels were already built in M4, so M7 is quicksand + walkie-talkie resupply.

## Done when
- Stepping into quicksand traps any player: they sink, can look / shoot / throw but not move, and die after 10 s unless helped.
- A teammate holds E on them for 5 s (timer paused meanwhile) to pull them out next to the helper. Letting go, looking away, leaving reach or being ridden resets the hold.
- The trapped player sees a countdown; teammates see "HELP!" + the countdown above their name tag.
- Trapped carriers drop their monkey, trapped ridden hunters lose their rider, netted monkeys sink too (helping them out also frees the net), caged monkeys never sink.
- The Blockout has 3 quicksand patches.
- Hunters can pick a Walkie-talkie for slot 4/5 (2 calls per slot). LMB opens the code list; typing a group's 8-arrow code calls it while WASD still walks.
- 15 s later a helicopter drops a crate where the caller stood (smoke marker meanwhile, seen by everyone). Every hunter can take from the crate once within 60 s: Healing = full health, Capture = net ammo + cages, Damage = rifle + tranq ammo.

---

## Tasks

### 1. Quicksand zone (`Quicksand`, scene object)
- **Proposed:** a flat sand-coloured disc on the ground with an invisible trigger **0.8 m tall** above it. Standing players float about 0.5 m above the ground, so the trigger has to reach that high.
- **Proposed:** the server traps any body inside the trigger. Exceptions: already trapped, caged, carried, or sitting on a hunter's head (riders and carried monkeys are off the ground anyway). Bodies dropped onto the sand (e.g. a netted monkey) get trapped too.

### 2. Trapped state (`QuicksandTrap` on every body)
- **Proposed:** the server holds it (trapped, time left, paused while helped), synced. Time left is sent ~4× a second and counted down locally in between, like the round timer. At 0 the server kills the body.
- **Proposed sinking:** the trapped player's own machine holds the body still (kinematic) at the spot and lowers it as time runs out. It goes down **1.2 m** for hunters and **0.6 m** for monkeys by the end, so the view sinks too. Others see it through the normal position sync.
- Blocks: movement and E. Look, firing, throwing and eating stay allowed.

### 3. Helping out
- **Proposed:** E on a trapped teammate (it goes through the body's existing E handling, before the net rescue) starts a hold on the helper's machine. It progresses while E stays held, the centre of the screen stays on the trapped player, the helper stays within **2.5 m** and isn't blocked (riding blocks a hunter). Anything else resets it.
- **Proposed:** the helper's machine tells the server when the hold starts, stops and finishes. The server pauses the death timer while a valid helper is holding. On finish it pulls the player out (and frees them from a net).
- **Proposed pull-out spot:** 0.9 m to the side of the helper (whichever side isn't quicksand), on the ground.
- The helper can't pull someone out while trapped themselves (E is blocked).

### 4. Links to other systems
- A trapped carrier drops its monkey, a trapped ridden hunter loses its rider (it hops off), and a trapped body can't be picked up.
- A trapped netted monkey keeps its escape minigame; escaping the net doesn't get it out of the sand.

### 5. HUD
- **Proposed:** a small HUD element built in code:
  - trapped player: "SINKING 7.3" with a draining bar, or "Being pulled out…" while helped;
  - helper: "Pulling out… (hold E)" with a filling bar.
- Teammates: a red "HELP! 7" line above the trapped player's name tag.

### 6. Blockout
- **Proposed spots** (3 patches, 4 m wide):
  - under the swing line (13, 20);
  - between the west tree and boulder (−10, 14);
  - between the east tree and boulder (20, −12), on the hunters' side.

### 7. Walkie-talkie item
- `LoadoutItem.WalkieTalkie` + a `WalkieTalkieDefinition` asset holding: calls per slot (2), the group codes, the helicopter delay (15 s), crate lifetime (60 s) and the drop prefab.
- The loadout station gets "Walkie-talkie". The slot bar shows "Walkie-talkie x2". Calls left are kept on the owner's machine, like cages.
- Placeholder hand model: a small dark box with an antenna.

### 8. Code entry (shared minigame, new "code" mode)
- **Proposed:** the minigame panel shows one row per group: name + its 8 arrows. Each arrow typed lights up on every row that still matches; rows that no longer match dim. When no row matches, it flashes red and starts over. A full match calls that group.
- **Proposed codes** (each starts with a different arrow, so the first key already picks the row):

| Group | Code |
|---|---|
| Healing | ↑ ↑ ↓ ↓ ← → ← → |
| Capture | ← ← → → ↑ ↓ ↑ ↓ |
| Damage | ↓ → ↓ → ↑ ← ↑ ← |

- **Proposed cancels:** being blocked (net, sleep, trip, ride, Esc menu), switching slot, or LMB again. A cancelled call doesn't use up a call.

### 9. Supply drop (`SupplyDrop`, networked)
- **Proposed:** the caller's machine sends the group and its ground position; the server spawns the drop there.
  - **Smoke marker** (placeholder): a tall translucent column in the group's colour, gently pulsing, for the 15 s.
  - **Helicopter** (placeholder): a box body with a spinning rotor. It flies in at ~30 m, passes over the spot at 13 s and drops the crate, which lands at 15 s. Sound hooks stay silent until clips exist.
  - **Crate:** a box in the group's colour with a floating label ("HEALING" / "CAPTURE" / "DAMAGE"). It lasts 60 s after landing.
- **Proposed:** E by a hunter: the server checks the crate has landed and this hunter hasn't taken from it yet.
  - Healing: the server sets their health to full.
  - Capture / Damage: the server tells the hunter's machine to refill (ammo and cages live there).

---

## Suggested build order
1. Quicksand zone, trapped state, sinking, death timer.
2. Helping out, pull-out, HUD, name tag line.
3. Links to carry / ride / net.
4. Blockout patches.
5. Walkie-talkie item, loadout station, slot counts.
6. Code entry mode.
7. Supply drop (marker, helicopter, crate) and crate contents.
8. Multiplayer test checklist.

## Progress
- Steps 1–7 built and checked as host on 2026-10-19 (fake bodies share the host's player id; E-hold, look-at and LMB can't be pressed headless, so those were driven directly):
  - a body on a patch is trapped (movement and E blocked, shooting allowed), sinks on schedule (0.62 m at 52% of the hunter timer) and dies at 0;
  - help: the timer pauses while helped, the teammates' name tag shows "HELP! 9", finishing pulls the body out 0.9 m beside the helper with no re-trap; the local HUD shows "SINKING 7.5" / "Being pulled out...";
  - a trapped carrier drops its monkey; a netted monkey on the sand sinks and dies, or a monkey teammate pulls it out, which also frees the net;
  - a ridden hunter stepping in makes the rider hop off;
  - walkie: slot "Walkie-talkie x2" / "2 calls"; the code list renders three coloured rows; a wrong first arrow resets, Down leaves only Damage, a wrong arrow mid-code resets; a full code uses a call (2 → 1) and spawns the drop under the hunter;
  - drop timeline: smoke 0–15 s, helicopter visible from ~7 s, crate falls 13–15 s and lands at 15 s, the crate vanishes after its lifetime;
  - crates: Damage refills rifle + tranq only, Healing restores full health, Capture refills the net launcher and cages; a second E by the same hunter does nothing; a monkey's E does nothing;
  - switching slot or getting netted cancels a call without using it.
- Small calls (flagged, change any):
  - quicksand patch spots as listed in task 6;
  - the HUD bar sits just above the minigame panel;
  - the help starts from the body's existing E handling, so E on a trapped *and* netted teammate pulls them out first;
  - no pop-up text when taking from a crate (health / ammo / cages just change);
  - the crate is solid; the helicopter comes in from a random direction;
  - the walkie hand model is a little big on screen; "Walkie-talkie x2" crowds the slot bar a bit.
- Fixed in the review pass (2026-10-19, checked as host):
  - a body trapped while still in the air (e.g. dropping onto the sand) is set down to standing height before it sinks, instead of hanging mid-air;
  - if the helper leaves the game or their body disappears mid-help, the trapped player's timer runs again (it used to stay paused).
- Not tested as host: real E holds and the look-at check, real LMB open / cancel, anything over the network, a real player dying in quicksand (ends the solo round). All are in [M7-TestChecklist.md](M7-TestChecklist.md).

## Open points
- None.
