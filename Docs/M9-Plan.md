# M9 — Playtest Polish, part 1: Feel Pass

> Goal (your words): everything needs to feel **fast, responsive and snappy**.
> Your choices from 2026-10-22 are marked **Decided**. Technical details I suggest are marked **Proposed**. Change any of them before the build starts.
> Part 2 of M9 (jungle ambience, the rest of the audio, the remaining screen effects, the balance numbers in GDD §14) comes later.
> **Status: built and host-checked 2026-10-22, approved as planned.** See Progress at the bottom. Playtest with [M9-TestChecklist.md](M9-TestChecklist.md).

## Done when
- Everyone moves 30% faster, and jumps are higher, snappier and never swallow an early press.
- Guns fire faster bullets and recover faster. Every shot kicks and recovers, flashes at the muzzle and leaves an impact puff.
- Shooters (and monkey throwers) get an instant hit marker with a tick sound, plus a bigger marker on a kill.
- The held item moves: it swings in on switch, dips while reloading, punches back on firing, sways and bobs.
- Victims see a red flash on damage and an arc pointing to the shooter, and hear a hurt thud.
- Sprinting widens the view a little.
- Every new sound exists as a generated placeholder `.wav` that's easy to swap.

---

## Current numbers (for reference)
- **Hunters:** walk 4.2 m/s, sprint 6.1 m/s, jump 0.66 m (0.73 s in the air).
- **Monkeys:** walk 5.1 m/s, sprint 7.4 m/s, jump 2.1 m (1.3 s in the air).
- Speeding up and stopping are already near-instant (~0.06 s), so they stay as they are.
- **Jump timing:** normal gravity, a 0.5 s lockout after each jump, and a press just before landing is lost.

---

## Tasks

### 1. Movement speed
- **Decided:** **+30%** for everyone.
  - Hunters: walk 4.2 → 5.5 m/s, sprint 6.1 → 8.0 m/s.
  - Monkeys: walk 5.1 → 6.6 m/s, sprint 7.4 → 9.6 m/s (still ~20% faster than hunters).
  - Climbing and swinging pump follow ground speed, so they speed up too. Ladders also +30%.
- **Proposed:** the head-ride tug of war, tranq slow and carry slow stay relative (same percentages), so they speed up along with everything else.

### 2. Jumps
- **Decided:** hunters 0.66 → **1.0 m**, monkeys 2.1 → **2.5 m**, with heavier gravity on player bodies: **×1.5 going up, ×2 falling**.
  - Time in the air: hunter 0.73 → 0.69 s, monkey 1.3 → 1.1 s.
- **Decided:** a jump pressed up to **0.12 s before landing** still happens on landing. The lockout between jumps drops from **0.5 to 0.25 s**. The 0.1 s coyote time stays.
- **Proposed:**
  - The jump is set in **metres** (`jumpHeight`) instead of the old force number, so it's easy to tune. The controller works out the launch speed.
  - The heavier gravity only applies while the body is in the air under normal movement. It's off while climbing, swinging, glued (ride, carry, quicksand) or netted. Projectiles and throws keep normal gravity, so the poop and net ranges you just tuned stay the same.
  - ~~Launches keep their current height and distance under the new gravity, with speeds raised to match.~~ **Changed while building:** not needed. Hop-off, thrown-off, climb leap-off / top-out and swing release all switch the movement controller off for their first 0.35–0.6 s, so the heavier gravity only starts on the way down. Their height stays the same; only the tail end of the fall is quicker. Launch speeds are unchanged.
  - The float spring (your stair-hop tuning) isn't touched. The heavier gravity is off whenever the spring is holding the body.
- Note: monkeys can now reach a hunter's head (1.8 m) more easily. Mount precision stays the same.

### 3. Gun handling
- **Decided:**

| | Rifle | Tranq | Net |
|---|---|---|---|
| Projectile speed | 100 → **300 m/s** | 50 → **80 m/s** | 30 (same) |
| Time between shots | 0.7 → **0.5 s** | 0.8 (same) | 1.2 (same) |
| Reload | 1.7 → **1.3 s** | 1.75 → **1.3 s** | 2.0 → **1.5 s** |

- **Decided:** switching to any item takes **0.15 s** (was 0.25). A click up to **0.15 s early** still fires as soon as the gun is ready (after the cooldown, a switch or a reload).
- **Proposed:** ranges stay the same (rifle 50 m, tranq ~75 m), so lifetimes shrink to match (rifle 0.5 → 0.17 s, tranq 1.5 → 0.94 s). The tranq dart drops less over the same distance because it's faster.
- **Decided:** a full-power monkey throw charges in **0.6 s** (was 1 s).

### 4. Recoil that recovers
- **Decided:** each shot snaps the view up by the gun's kick (rifle 2°, tranq 1°, net 3°), and it **springs back** to where you aimed.
- **Proposed:**
  - It snaps up in ~0.05 s and returns over ~0.3 s.
  - A shot fired before it has fully recovered goes where the view points at that moment.
  - The rock flinch uses the same mechanism, so it recovers now too (today it leaves your aim moved).

### 5. Muzzle flash and impacts
- **Decided:** a flash at the barrel and a puff where shots and throws land, seen by everyone.
- **Proposed:**
  - **Muzzle:** each gun model gets a muzzle point at its barrel tip.
    - Rifle: a bright yellow flash plus a short point light (~0.05 s).
    - Tranq: a small grey puff. Net launcher: a bigger grey puff.
    - Remote players' guns flash too.
  - **Impacts:** every projectile copy (yours and the cosmetic ones on other machines) plays its puff where it lands. Small particle bursts:
    - Rifle: dust and sparks on the world, a dark red puff on players and dummies.
    - Tranq: a small green puff. Net: a grey puff.
    - Poop: a brown splash. Rock: a dust puff with a clack sound. Peel and decoy: a small dust puff.

### 6. Hit and kill markers
- **Decided:** an X on the crosshair plus a tick sound when you hit a player or dummy. A bigger red X on a kill. Monkey throws (poop, rock) get the hit marker too.
- **Proposed:**
  - The hit marker shows **instantly** on your machine (the shooter decides hits). The kill marker comes from the server once the hit has actually killed.
  - Hitting a **teammate** shows the X in **orange**, so you notice friendly fire.
  - The big marker also shows when your dart puts someone **to sleep** or your net **traps** them. Those are the "got them" moments in a capture game.

### 7. Held item motion
- **Decided:** the held item swings in when switched, dips while reloading, sways when looking around and bobs while walking. Firing punches it back.
- **Proposed:**
  - Swing-in takes the 0.15 s switch time.
  - The reload dip lasts the whole reload.
  - The fire punch moves the gun ~4 cm back and tilts it up a few degrees, recovering in ~0.12 s.
  - Sway lags behind your look by up to ~2°.
  - The walking bob grows with speed (stronger when sprinting).
  - You see all of it. Other players see the fire punch and the swing-in (the reload isn't sent over the network).

### 8. Getting hit
- **Decided:**
  - A red screen-edge flash on damage, stronger for bigger hits.
  - A red arc around the crosshair pointing toward where a shot or throw came from.
  - A hurt thud for the victim on gun and rock hits.
- **Proposed:**
  - **Flash:** on any health loss (rifle, peel trip, etc.).
  - **Arc:** on any hit from another player's shot or throw (rifle, tranq, net, rock, poop). It keeps pointing at the source's world position while you turn, and fades over ~1 s.
  - **Poop:** a squelch instead of the thud.
  - No extra flinch on rifle hits (not picked).

### 9. Sprint FOV kick
- **Decided:** the view widens **~6°** while sprinting and eases back when you stop.
- **Proposed:**
  - It eases in and out over ~0.2 s and only kicks in while you're actually moving faster than a walk.
  - Aiming zoom overrides it.
  - It adds on top of the Esc-menu FOV setting.

### 10. Placeholder sounds
- **Decided:** I generate simple sounds in code (no downloads) and save them as `.wav` files in `Assets/_Project/Audio/Generated/`, so they can be swapped for real ones later.
- **Proposed set:**
  - Hit tick, kill confirm and hurt thud. These are 2D, only you hear them.
  - Poop squelch.
  - Impact sounds for dirt and body hits, in 3D at the hit point.
  - Tranq "pfft" and net "thunk" fire sounds, replacing the pitched rifle shot.
  - Jump and land sounds.
- **Proposed:** also fill the two silent gameplay hooks from M4 with placeholders: the **rock landing noise** and the **decoy screech**. They're how hunters track monkeys by ear (GDD §9).

---

## Suggested build order
1. Movement speed + jumps (heavier gravity, jump in metres, early-press window, shorter lockout, launch retunes).
2. Gun numbers, switch time, fire buffering, throw charge.
3. Generated placeholder sounds.
4. Recoil recovery + held item motion.
5. Muzzle flash + impacts.
6. Hit / kill markers.
7. Getting-hit flash, arc and hurt sound.
8. Sprint FOV kick.
9. Host checks of each step, then a feel checklist for the next playtest (together with the M6 / M7 / hub checklists).

## Progress
- All steps built on 2026-10-22 and checked as host in the lobby hub (the host on the Hunters side, shooting the test-range dummies):
  - **Walk:** 5.51 m/s measured (5.52 expected). Stopping takes ~0.15 s to under 0.05 m/s.
  - **Jump:** apex 0.999 m, 0.72 s in the air. A press 0.09 s before landing jumped 0.01 s after touchdown.
  - **Rifle:** bullet 300 m/s, muzzle flash on, gun punch, hit marker on each dummy hit, shots 0.5 s apart, the 1.3 s reload after 6 shots. The 8th hit (40 damage = a monkey's health) showed the big red marker 0.07 s later. A click queued 0.1 s early fired as soon as the cooldown ended (0.507 s after the first shot).
  - **Recoil:** the 2° kick went back to 0 within 0.6 s.
  - **Held item:** the swing-in finishes within the 0.15 s switch; the reload dip (8 cm, 20°) holds for the reload and settles 0.2 s after it.
  - **Getting hit:** a hit from the left showed the arc on the left. 15 damage flashed the screen edges red.
  - **Screenshots:** muzzle flash, dust + sparks on the floor, dark red puff on the dummy, the X marker.
- Fixed while testing: the jump launch now adds half a physics step of gravity (it peaked 5 cm low); the first damage a body took didn't flash (the previous health wasn't known yet).
- Small additions (flagged, change any):
  - **Throws:** monkey throws play a whoosh (the throw sound slot was empty).
  - **Jump / land sounds:** only you hear your own.
  - **Lockout timer:** the jump lockout now counts down while you're blocked or locked. Before, the float spring could stay off after being netted mid-jump.
- **Not tested as host** (in the checklist): sprint FOV and walking bob (need real keys), monkey throw markers / squelch / rock clack / decoy screech, how all of it looks and sounds on other players' machines, and the feel itself.

## Open points
- None.
