# M6 — Head-Ride: Plan

> Design decisions are in [GDD.md](GDD.md) §7.4 and §5.4 (all 16 answered on 2026-10-19).
> All technical choices and starting numbers below were **approved on 2026-10-19** (marked "Proposed" where they were suggested).
> Quicksand (and riding interrupting quicksand help) is M7.

## Done when
- A monkey that lands on top of a hunter's head grabs on (no snap or auto-aim). One rider per hunter, hunters only.
- The rider sits on the head and looks around freely. WASD steers the hunter (tug of war, rider stronger). Jump hops off in the look direction. No throwing, eating or E while riding.
- The ridden hunter's screen is ~80% covered by monkey hands with shifting gaps. They can only move: no firing, aiming, switching, reloading or E.
- The ridden hunter's shake-off minigame: 8 mouse-flick prompts. Finishing it throws the rider off.
- Any rifle / tranq / net hit on the rider throws it off and also applies the hit's normal effect.
- Thrown off (shaken or shot) = flung back and up, then tipped over until 3 s have passed, then standing again. It can remount right away.
- The ridden hunter being netted, falling asleep, tripping or dying makes the rider hop off cleanly.
- Mounting a carrier makes them drop the carried monkey (still netted). Mounting a key holder takes all their keys.

---

## Tasks

### 1. Ride state (new `MonkeyRide` on the Monkey, `RideMount` on the Hunter)
- **Proposed:** the server holds the ride, synced on the monkey: `riding` + the ridden hunter's player id. A static lookup finds "the rider of hunter X", like `HunterCarry.FindCarrierOf`.
- **Proposed:** `RideMount` on the Hunter only provides the seat point (top of the head) and a shake-off report. Body sizes: hunter capsule top at 1.33 m above its root, so the seat is at **1.38 m** (5 cm gap). Monkey capsule 0.6 m tall.
- **Proposed:** collisions between the rider and the ridden hunter are switched off while riding (on every machine), so the glued rider can never shove the hunter around.

### 2. Mounting
- **Proposed detection (monkey's own machine):** while falling or level (not rising), not climbing or swinging and not blocked, a short sphere cast down from the monkey's feet against the Player layer. It must hit a hunter's capsule **within the top 0.15 m** and **within 0.3 m of its centre line**, so landing on a shoulder slides off. Players can't stand on each other today (the Player layer isn't walkable), so the precision part stays.
- **Proposed:** the monkey's machine asks the server, and the server takes the first valid request. A request is valid when the hunter is unridden, free, awake, not tripped and alive, and the monkey is free, awake and within 1.5 m of the seat. Same trust model as hits.
- **Proposed seat:** like carrying (M5), the rider's own machine glues its body to the hunter's seat every frame (kinematic) and shuts its movement off. Others see it through the normal position sync. Same caveat as carry: it may trail slightly behind a fast-moving hunter.
- On mount the server also drops the hunter's carried monkey (still netted) and moves all of the hunter's keys to the rider.

### 3. Steering (tug of war)
- **Proposed:** the rider's machine syncs its steer direction (WASD relative to its look, flattened, max length 1) about 20 times a second. The ridden hunter's machine mixes it into its own movement: `rider × 1.0 + hunter's own WASD × 0.6`, capped at full walking speed. A hunter countering perfectly still drifts at 40% walk speed.
- **Proposed:** no sprinting while ridden (for either player).

### 4. Blocks while riding / ridden
- Rider: movement off (glued), actions and E blocked. Look stays free. Jump = hop off.
- Ridden hunter: actions and E blocked. Movement and look stay free. This already cancels their own pickup / rescue / cage minigames (they cancel on blocked actions).
- **Decided 2026-10-19:** the ridden hunter **can still wipe poop (X)**; the rider can't (steer + look only).

### 5. View block
- **Proposed placeholder:** a local overlay of two big monkey hands (palm + four fingers) generated in code. Together they cover ~80% of the screen; the fingers slowly spread and close (each on its own ~1–2 s rhythm), so gaps open and shut. Dark brown, like the splat blobs.
- **Changed 2026-10-20 (your call):** the screen overlay is replaced by the rider's **real 3D hands** (`MonkeyHands`: palm, four fingers, thumb, arms from the rider's shoulders), visible to everyone. On the ridden hunter's machine they sit 13 cm in front of the eyes (scaled to the field-of-view setting) and cover ~80%; others see them at monkey-hand size on the outside of the hunter's face.
- Rider HUD hint: "RIDING: WASD steer · Jump hop off".

### 6. Shake-off (mouse flicks)
- **Proposed:** a mouse-flick mode for the shared `DirectionMinigame` (same arrow row on screen, title "Shake it off!").
  - A flick = the mouse moving about **150 px in one direction within 0.2 s** (roughly a 20° look jerk). It counts as the dominant direction (up / down / left / right).
  - After each flick, input is ignored for **0.2 s** so the swing back doesn't count as a wrong flick.
  - A wrong flick restarts with a new sequence (§5.4). Length **8**.
  - **Changed 2026-10-20 (your call):** no speed needed any more. Move the look **150 px from center** in the shown direction, then bring it back **within 50 px of center** before the next arrow counts ("look left, center, next"). While waiting, the arrow dims and the title reads "Back to center...". The tracked offset is capped at 300 px so an overshoot doesn't need a long way back.
- **Proposed:** it runs automatically the whole time the hunter is ridden, like the net escape. The hunter's machine reports success, and the server throws the rider off.

### 7. Getting off
- **Hop off** (rider's Jump, or the hunter netted / asleep / tripped / dead): **proposed** 4 m/s towards where the rider looks + 3 m/s up. No ragdoll.
- **Thrown off** (shake-off, or any gun hit on the rider): **proposed** 5 m/s away from the hunter's facing (random ±45°) + 4 m/s up. Movement stays off ~0.5 s so the fling carries. Then the peel-trip fall plays and the monkey lies there until 3 s after the throw, then stands up.
- **Proposed:** the server tells the rider's machine how it got off (one message), so the body lets go and moves the right way exactly once.
- **Proposed:** the server notes when a body trips (it already triggers the trip), so it can end the ride when the ridden hunter trips.
- Rider netted or tranq'd asleep by a shot: still thrown off (it was a gun hit), then lies netted / asleep.

### 8. Round integration
- Nothing changes in the alive counts. A ride ends cleanly when either body despawns, and on round end.

---

## Suggested build order
1. `MonkeyRide` / `RideMount`, mount detection, seat glue, collision ignore, blocks, Jump hop-off.
2. Tug-of-war steering.
3. View-block overlay + HUD hints.
4. Mouse-flick shake-off + thrown off (fling + topple).
5. Shot off by gun hits; hunter-down exits; carrier drops on mount.
6. Key stealing on mount.
7. Multiplayer test checklist (played together with M5 leftovers after M6).

## Progress
- Steps 1–6 built and checked as host on 2026-10-19 (fake bodies share the host's player id; one test played the host as the hunter, one as the monkey):
  - mount by dropping onto the head (glued to the seat, collisions off, hunter blocked except movement and wiping, 8-flick shake-off running, hands overlay up);
  - tug of war: rider pushing back with the hunter pushing forward drifted the hunter backwards at 1.70 m/s = 40% of the 4.24 m/s walk;
  - Jump hop-off (lands 1.5 m ahead, everything unblocked), remount;
  - the flick reader: up / right / down / left, a small move ignored, the swing back after a flick ignored;
  - shake-off throw (flung ~20° off straight back, tipped over, no remount until the 3 s are over, then remounts);
  - rifle shot on the rider (5 damage, thrown off), net shot (thrown off netted, its stolen key drops under the hunter);
  - mounting a carrier drops the carried monkey and moves the hunter's key to the rider;
  - the hunter netted, tripped or tranq'd asleep → the rider hops off cleanly;
  - the rider's own view: high viewpoint, "RIDING · WASD steer · Jump hop off", no hands overlay.
- **Decided 2026-10-19:** landing on a monkey the hunter is **carrying** counts as landing on the carrier's head (the carried monkey sits on top of the head, so the carrier was otherwise almost impossible to mount).
- Small calls (flagged, change any):
  - the hands overlay sits under the timer / health / minigame, so those stay readable;
  - the rider hint uses the centre status line (same spot as "NETTED").
- Not tested as host: the hunter killed mid-ride (would end the solo test round), real mouse flicks, steering and the hands seen over the network. All are in [M6-TestChecklist.md](M6-TestChecklist.md).
- Seen once during testing (not M6 code): after some stray input the hunter ended up with its root on the ground, so its float spring stopped holding it up (the downward probe starts inside the floor). Lifting it restored normal floating. Probably a hard landing in the ported controller; watch for it in playtests.

## Open points
- None.
