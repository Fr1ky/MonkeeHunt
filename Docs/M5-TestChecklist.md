# M5 — Multiplayer Test Checklist

> **Result: all passed (2026-10-18).**

Everything here was checked as host with stand-in bodies, but never with real players pressing real keys. Tick what works and note anything that doesn't (what you did, what you saw, who saw it).

## Setup
- Host in the editor, the others join `127.0.0.1` port `5000` (Multiplayer Play Mode virtual players or a build).
- Lobby: step onto the team squares. Hunters: at the **loadout rack** put a **Cage** into slot 4 (and slot 5 for 4 cages): select the slot with 1–5, look at the cage, press E. (The loadout station was replaced by the rack on 2026-10-20.)
- The host starts the round at the **host console**.
- Handy numbers: 4 tranq darts = asleep for 8 s. Rifle does 5 damage, monkeys have 40 HP (8 hits). **F8** frees yourself from a net (editor / dev builds only).
- Keys: arrow keys = minigame, **E** = interact, **G** = drop carried monkey, **F** = eat, **LMB** = fire / throw / place cage.

---

## A. Two players: 1 hunter (H) + 1 monkey (M)
Caging M ends the round (it's the only monkey), so do the cage step last.

- [ ] **Escape minigame:** H nets M. M sees "Escape!" with 6 arrows. A wrong arrow flashes red and gives a new sequence. WASD doesn't count. Finishing frees M.
- [ ] **No timeout:** M left in the net without pressing anything stays netted.
- [ ] **Pickup race, hunter wins:** H nets M, looks at M and presses E. H gets "Picking up..." (8 arrows) while M keeps escaping. H finishes first → H carries M.
- [ ] **Pickup race, monkey wins:** same, but M finishes first → M is free and H's minigame stops.
- [ ] **Pickup race, walk away:** H walks more than 2.5 m away during the race → H's minigame cancels.
- [ ] **Sleeping pickup:** H puts M to sleep (4 darts) and nets it. E picks M up right away. M can't escape while asleep. When M wakes up while carried, M's escape starts.
- [ ] **Carrying (H's view):** ~70% speed, can't fire, number keys / wheel only reach cage slots. M rides above H's shoulder.
- [ ] **Carrying (M's view):** M can look around, sees "Escape!", and the ride looks smooth (no stutter or trailing behind H).
- [ ] **Drop:** G drops M behind H, still netted. M keeps escaping.
- [ ] **Escape while carried:** M finishes the escape while carried → M lands behind H, free.
- [ ] **Empty cage:** H selects the cage slot and clicks with nothing carried → an empty cage appears in front. Slot shows "Cage x1". E on the cage's base picks it back up ("x2").
- [ ] **No cages left:** with "x0", clicking does nothing.
- [ ] **Cage it (ends the round):** H carries M, selects the cage slot, clicks → M is inside the cage. H sees "Keys: 1" bottom-left and "Cage x1". Round ends "Hunters win" → back to the lobby.
- [ ] **Remote view:** H sees the net on M, M riding on H's shoulder, and M sitting inside the cage. M's view from inside the cage looks right.

## B. Two players, both hunters (H1, H2)
No monkeys, so the round keeps running until the timer.

- [ ] **Friendly-fire net:** H1 nets H2. H2 gets the 6-arrow escape and can free themselves.
- [ ] **Rescue:** H1 nets H2, then looks at H2 and presses E → "Rescuing..." (5 arrows) frees H2.
- [ ] **Rescue cancels:** walking more than 2.5 m away cancels the rescue.

## C. Three players: 1 hunter (H) + 2 monkeys (M1, M2)
- [ ] **Monkey rescue:** H nets M1. M2 looks at M1, presses E, finishes 5 arrows → M1 is free. Getting netted or tranq'd asleep during it cancels it.
- [ ] **Round counter:** H cages M1 → top of the screen shows "Monkeys 1/2".
- [ ] **Caged monkey (M1's view):** can look around and throw poop out through the bars (it hits H). F eats to refill poop. Can't move, climb, swing, switch to other items or use E.
- [ ] **Key on the hunter:** M2 sees a gold key on H's hip and a key icon left of H's name. H doesn't see their own name tag.
- [ ] **Unlock minigame:** M2 looks at the cage (base or M1's body) and presses E → "Unlocking..." (6 arrows). Finishing frees M1 (not netted). H's "Keys: 1" and the hip key disappear. Counter back to "Monkeys 2/2".
- [ ] **Unlock cancels:** M2 walks more than 2.5 m from the cage during the unlock → it cancels.
- [ ] **Hunter can't unlock:** H pressing E on a cage with a monkey inside does nothing.
- [ ] **Shot in the cage:** H cages M1 again and shoots it dead (8 rifle hits). The cage is empty again, H's key is gone, M1 counts as dead.
- [ ] **Retrieve after unlock:** after M1 is freed, H can pick the empty cage back up with E.
- [ ] **Key into the pit:** H (holding a key) jumps into the pit. The key appears at the pit's edge where H last stood. (H was the only hunter, so the round ends right after; just check the key lands there before the results screen ends.)

## D. Four players: 2 hunters (H1, H2) + 2 monkeys (M1, M2)
- [ ] **Carrier netted:** H1 carries M1, H2 nets H1 → M1 drops still netted. Same when H2 tranqs H1 asleep.
- [ ] **Key drop on net:** H1 cages M1 (H1 holds the key). H2 nets H1 → the key drops at H1's feet. H1's "Keys: 1" disappears.
- [ ] **Monkey picks up the key:** M2 looks at the dropped key and presses E. M2 now has the hip key and the name tag icon (seen by others) and "Keys: 1" on their HUD.
- [ ] **Instant unlock:** M2 presses E on M1's cage → M1 is free at once, no minigame. M2's key disappears.
- [ ] **Monkey drops the key:** M2 holding a key gets netted → the key drops. Getting killed drops it too.
- [ ] **Hunter grabs a key back:** a hunter picks up a dropped key with E and now holds it.
- [ ] **Key vanishes:** with a key lying on the ground, M2 frees the caged monkey with the unlock minigame → the key on the ground disappears.
- [ ] **Same key, same time:** two players press E on one dropped key at once → only one gets it.
- [ ] **Same empty cage, same time:** two hunters press E on one empty cage at once → only one gets it back.
- [ ] **Holder leaves:** a hunter holding a key leaves the game → the key drops where they last stood.
- [ ] **Caged monkey seen by others:** H2 and M2 see M1 inside the cage, in the right spot, and see it walk out when freed.

## Things to judge while playing
- Minigame lengths: escape 6, rescue 5, pickup 8, unlock 6 arrows. Target is 3–5 seconds each.
- Cage: 1.5 × 1.5 m, ~2 m tall, placed 1.6 m in front. Hand-held cage model size and position.
- Carry speed (70%) and where the carried monkey sits.
- Key looks: hip key size, name tag icon, the spinning key on the ground, "Keys: N" position.
- Cages placed on the same spot stack on top of each other (nothing prevents overlapping cages yet).
