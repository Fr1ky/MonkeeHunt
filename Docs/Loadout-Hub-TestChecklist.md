# Loadout & Hub — Test Checklist

Everything here was checked as host with stand-in bodies and direct calls, but never with real key presses or a second player. Tick what works and note anything that doesn't.

## Setup
- Host in the editor, a second player joins `127.0.0.1` port `5000`.
- Hub layout: the **loadout rack** is the long table by the north wall behind the Hunters square; the **test range** (3 dummies) is by the south wall.

## A. Loadout at the rack (one player is enough)
- [ ] **Not a hunter yet:** standing unassigned, no item is held, the hint says to step onto the Hunters square, and E on the rack does nothing.
- [ ] **Kit on:** on the Hunters square the slot bar, ammo, crosshair and held item appear; the hint shows "1-5 / wheel: pick a slot · E on a rack item · G: clear the slot".
- [ ] **Pick:** press 4, look at the walkie-talkie on the rack, press E → slot 4 becomes "Walkie-talkie".
- [ ] **Gear repeats:** press 5, E on the walkie-talkie again → two walkie-talkie slots. Five are fine too.
- [ ] **Gun swap:** with the rifle in slot 1, press 4 and E on the rifle → the rifle moves to slot 4 and slot 4's old item goes to slot 1.
- [ ] **Clear:** select a slot, press G → it becomes empty ("-"); empty slots can still be selected in the hub with 1–5 and the wheel.
- [ ] **Round:** start a round → the hunter has exactly the hub loadout in the same slots. Empty slots are skipped by the wheel in the round.
- [ ] **Saved:** quit the game (or stop Play), start again, host → the name, IP and port fields and the loadout are as you left them.

## B. Test range
- [ ] **Rifle:** hit a dummy → "-5 (total 5)", "total 10" on the second hit; it flashes on each hit.
- [ ] **Tranq:** "Tranq 1/4" … "ASLEEP" at 4.
- [ ] **Net:** a net appears over the dummy and "NETTED" (aim a bit high: the net arcs).
- [ ] **Reset:** 4 s after the last hit the dummy shows "DUMMY" again.
- [ ] **Never runs out:** fire a whole magazine and reload → the reserve stays 32. Cages and walkie calls aren't used up.
- [ ] **Cages:** place an empty cage with LMB; E on it removes it again.
- [ ] **Walkie:** a call in the hub drops a crate in the hub as in a round.
- [ ] **No friendly fire in the hub:** shooting or netting the other player in the hub does nothing.
- [ ] **Two players:** each sees the other's held item change when they pick from the rack or switch slots; each sees the dummies react to the other's shots.

## C. Settings (Esc menu)
- [ ] **Open:** Esc → "Settings" under Resume opens the sliders panel beside the menu.
- [ ] **Sensitivity / FOV / volume** change immediately (look speed, view width, sound).
- [ ] **Zoom:** gun aim and binocular zoom still work and stay relative to the chosen FOV.
- [ ] **Saved:** after a restart the settings are still as set.
- [ ] **Closing:** Resume / Esc closes the settings panel too.

## Things to judge while playing
- Rack: easy to read and aim at? Item models big enough on the table?
- Test range distance (~10–15 m from the Hunters square) and dummy size.
- Settings ranges: sensitivity 0.25×–3×, FOV 60–100°.
