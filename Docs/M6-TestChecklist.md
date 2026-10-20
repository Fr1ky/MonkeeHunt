# M6 — Multiplayer Test Checklist

Everything here was checked as host with stand-in bodies, but never with real players pressing real keys. Tick what works and note anything that doesn't (what you did, what you saw, who saw it).

## Setup
- Host in the editor, the others join `127.0.0.1` port `5000` (Multiplayer Play Mode virtual players or a build).
- Lobby: step onto the team squares. One hunter should put a **Cage** in a slot at the loadout rack (select the slot with 1–5, look at the cage on the rack, press E) for the key tests.
- The host starts the round at the **host console**.
- Handy numbers: monkey ~0.55 m tall, eye height ~0.45 m, jumps ~2 m high; hunter head top ~1.8 m off the ground. A monkey can reach a hunter's head with a jump from close by, or drop from trees and boulders.
- Keys: **Space** = jump (mount by landing on a head, hop off while riding), **WASD** = steer while riding, **look the shown way, then back to center** = shake-off, **X** = wipe poop, **F8** = free yourself from a net (editor / dev builds).

---

## A. Two players: 1 hunter (H) + 1 monkey (M)
- [ ] **Mount:** M jumps or drops onto the top of H's head → M sits on H's head. Landing on H's shoulder or side doesn't mount (M slides off).
- [ ] **No mount on the way up:** M jumping up past H's head doesn't grab on; only landing does.
- [ ] **Rider view:** M sees from up high, can look around freely, and sees "RIDING · WASD steer · Jump hop off". Throwing, eating, E and X do nothing.
- [ ] **Steering:** M holds W → H walks where M looks, even if H doesn't touch the keys.
- [ ] **Tug of war:** H walks against M's push → H slows down a lot but still drifts M's way (~40% walk speed). Sprinting doesn't help either of them.
- [ ] **View block:** H's screen is mostly covered by M's real (3D) hands, with M's arms coming down from above. The fingers spread and close, so H catches glimpses. A third player sees M's hands over H's face. The timer, health and minigame arrows stay readable.
- [ ] **Ridden hunter limits:** H can't fire, aim, switch weapons, reload or press E. X still wipes poop.
- [ ] **Shake-off:** H sees "Shake it off! (look that way, then back)" with 8 arrows. Looking the shown way ticks one off; the title then says "Back to center..." and the next arrow only counts once H looks back near where they started. Speed doesn't matter. A wrong direction gives a new sequence.
- [ ] **Thrown off:** finishing the shake-off flings M back and up, then M lies tipped over until ~3 s have passed and stands up again. M can't remount during that time, and can right after.
- [ ] **Hop off:** M presses Jump → M jumps off cleanly in the direction it looks (no fall-over).
- [ ] **Remote view (H's machine):** H sees M sitting on top of its own head area (or catches it when looking up). Watch for M trailing behind H's head while H moves fast.
- [ ] **Remote view (M's machine):** H walks smoothly while M steers. M stays on H's head without jittering off.
- [ ] **Hunter trips:** M steers H onto a banana peel → H falls over and M hops off cleanly.
- [ ] **Hunter in the pit:** M steers H into the pit → H dies, M hops off (M falls in too if it doesn't jump away first).

## B. Three players: 2 hunters (H1, H2) + 1 monkey (M)
- [ ] **One rider only:** with M on H1, nothing else can mount H1 (test with a second monkey if you have 4 players).
- [ ] **Shot off by rifle:** H2 shoots M on H1's head → M takes 5 damage and is thrown off. A miss that hits H1 damages H1 (friendly fire).
- [ ] **Shot off by tranq:** H2 tranqs M → M gets a stack and is thrown off.
- [ ] **Shot off by net:** H2 nets M → M is thrown off and lies netted.
- [ ] **Hunter netted:** H2 nets H1 while M rides → M hops off cleanly, H1 gets the escape minigame (the shake-off disappears).
- [ ] **Hunter asleep:** H2 puts H1 to sleep (4 darts) → M hops off.
- [ ] **Hunter killed:** H2 kills H1 while M rides → M hops off where H1 was.
- [ ] **Interrupted minigame:** M lands on H1 while H1 is picking up / rescuing → H1's minigame cancels and the shake-off starts.

## C. Keys and carrying (2 hunters + 2 monkeys, or a cage already placed)
- [ ] **Key steal:** H1 holds a cage key ("Keys: 1"). M lands on H1 → H1's keys go to M at once (M's hip key and name tag icon show, H1's disappear).
- [ ] **Stolen key unlocks:** M hops off, walks to the cage and presses E → the caged monkey is free at once.
- [ ] **Rider netted with a key:** H2 nets M while it rides H1 with a stolen key → the key drops on the ground under H1.
- [ ] **Carrier drops:** H1 carries a netted monkey. M lands on H1's head (landing on the carried monkey counts too) → H1 drops the carried monkey (still netted) behind them.

## Things to judge while playing
- How hard the mount is: feet must land within the top 0.15 m of the head and within 0.3 m of its centre line.
- Steering mix: rider 1.0 vs hunter 0.6. Does the hunter feel able to fight back?
- Shake-off: 8 directions, each a look ~150 px of mouse travel from center, then back within ~50 px. Too easy, too hard, or picking up wrong directions?
- The smaller monkey (0.55 m, ~2 m jumps): easy enough to spot and hit for hunters? Too easy to reach heads?
- Hop off: 4 m/s forward + 3 m/s up. Thrown off: 5 m/s away + 4 m/s up, ±45°.
- Hands overlay: coverage (~80%), how often the gaps open, colour.
- Whether the rider trails behind the hunter's head over the network (same glue approach as carrying).
- Wipe: the ridden hunter can wipe poop, the rider can't.
