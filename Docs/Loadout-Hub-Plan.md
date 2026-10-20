# Loadout & Hub Update: Plan

> Decided with you on 2026-10-20: all 5 hunter slots take any item, each gun at most once, gear repeats freely; items are picked from racks in the hub; hunters can try their loadout on dummies in the hub; name / IP / port / loadout are remembered; a settings menu in the Esc menu.
> All technical choices and numbers below were **approved on 2026-10-20**, including: picking a gun that is already in another slot swaps the two slots; nothing runs out in the hub; in the hub only dummies react to hits (no friendly fire).

## Done when
- A hunter's 5 slots can hold any of: hunting rifle, tranq gun, net launcher, binoculars, cage, walkie-talkie, or nothing. Each gun at most once; gear any number of times (e.g. 5 walkie-talkies).
- In the hub, a hunter picks a slot with 1–5 / the wheel, looks at an item on the rack and presses E: that slot takes it. The old dropdown station is gone.
- In the hub, hunters hold their real loadout and can use it: fire at dummies, zoom binoculars, place cages, call in supply drops.
- Dummy targets in the hub show hits, tranq stacks and nets, then reset.
- The round body spawns with exactly the hub loadout.
- Name, IP, port, loadout and settings are remembered on this PC between sessions.
- The Esc menu has Settings: mouse sensitivity, field of view, volume.

---

## Tasks

### 1. Loadout data
- `LoadoutItem` gains HuntingRifle, TranqGun, NetLauncher (next to Empty, Binoculars, Cage, WalkieTalkie).
- **Proposed:** a hunter body holds one synced "loadout" value (the 5 items). The server checks the rule (each gun at most once) and stores it per player for the session, like the slot 4–5 picks today.
- **Proposed default** (first time, or nothing saved): rifle, tranq gun, net launcher, binoculars, empty (same as now).
- **Proposed:** picking a gun that is already in another slot **swaps** them: the gun moves to the selected slot and that slot's old item goes where the gun was. **G** clears the selected slot in the hub.
- In a round, empty slots are skipped by the wheel and number keys as today. In the hub, empty slots can be selected (so you can fill them).

### 2. Racks (replace the loadout station)
- **Proposed:** a long table along the north wall behind the Hunters square, with the 6 items laid out as their hand models scaled up (×4), each with a floating name.
- Look at an item + E → the selected slot takes it. Only works for players on the Hunters side; the rack's sign tells others to step onto the Hunters square first.

### 3. Hunter kit in the hub
- **Proposed:** the hub body gets the hunter kit (no respawn, as approved in M2), switched on only while the player's side is Hunters. It holds the selected item in hand and can fire, aim, zoom, place / pick up empty cages, and call supply drops.
- **Proposed:** in the hub nothing runs out: magazines still empty and reload, but reserves, cages and walkie calls are never used up.
- **Proposed:** in the hub, shots and nets **don't affect players**, only dummies (no friendly fire in the lobby).
- The hub gets the round's HUD (slot bar, ammo, crosshair, binocular overlay, code panel). **Proposed:** the round HUD becomes one shared prefab used by both scenes, plus a hub hint line: "1–5 / wheel: pick a slot · E on a rack item: put it there · G: clear the slot".

### 4. Dummies (test range)
- **Proposed:** 3 dummies on stands along the south wall, about 10–15 m from the Hunters square.
- Each shows what hit it: "-5" with a running damage total for the rifle, "Tranq 2/4" (and "ASLEEP" at 4) for the tranq gun, a net over it and "NETTED" for the net launcher. A short colour flash on every hit. It resets 4 s after the last hit.
- Hits are reported like real hits (the shooter decides, the server shows it to everyone).

### 5. Remember choices
- **Proposed:** saved on this PC (Unity PlayerPrefs): name, IP, port, the 5 loadout items, and the settings. Loaded when the game starts; the MainMenu fields start with the saved values.

### 6. Settings (Esc menu)
- **Proposed:** a "Settings" button in the Esc menu opens a small panel:
  - mouse sensitivity: 0.25×–3×, default 1×;
  - field of view: 60–100°, default 75°;
  - volume: 0–100%, default 100%.
- Changes apply immediately and are saved. Binocular / gun zoom stay relative to the chosen field of view.
- The shake-off mouse distances (150 px out, 50 px back) count raw mouse movement, so the sensitivity setting doesn't change them.

---

## Progress
- Built and checked as host on 2026-10-20:
  - hub body: kit off and "step onto the Hunters square" hint while unassigned; on the Hunters side the kit switches on (held item, slot bar, ammo, hint with the rack controls);
  - rack picks: slot 4 ← walkie, slot 5 ← walkie (gear repeats), slot 4 ← rifle swaps with slot 1, G clears a slot and the empty slot stays selected; every change is saved on this PC and on the server;
  - dummies hit by real projectiles: rifle "-5 (total 10)", tranq "Tranq 2/4", net "NETTED" with the net shown; all reset 4 s after the last hit (the net arcs, so aim a bit high at range);
  - in the hub, rifle and net hits on another player do nothing; reloads keep the reserve; walkie calls and cages aren't used up;
  - starting a round: the Hunter body has exactly the hub loadout, and calls are used up again in the round;
  - settings: the Esc menu has a Settings button opening the sliders; FOV 90 / sensitivity 1.5 / volume 60% apply at once and are saved when the menu closes.
- Small calls (flagged, change any):
  - rack items lie on a long table along the north wall, as their hand models scaled to ~0.8 m, with names above;
  - the three dummies stand 2.6 m apart by the south wall under a "TEST RANGE" sign;
  - the round HUD is now the shared prefab `Prefabs/UI/HudCanvas.prefab` (Blockout and Lobby both use it);
  - the old dropdown station (and its two scripts) was removed;
  - "1 call" / "1 cage" singular on the HUD.
- Not tested as host: real E / G / number-key presses, two players seeing each other's held items in the hub, saved values surviving a full restart of the game (cleared after testing).

## Suggested build order
1. Loadout data (items, sync, validation, swap, round spawn).
2. Hunter kit in the hub + shared HUD + hint line.
3. Racks.
4. Dummies.
5. Remember choices.
6. Settings.
7. Host check + test checklist.
