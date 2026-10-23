# MonkeeHunt — Game Design Document

> **Working title.** Status: Prototype 1 (Proto 1) design. Values marked **TBD** are not yet decided.

---

## 1. Overview

| | |
|---|---|
| **Genre** | First-person asymmetric team PVP |
| **Audience** | Tight-knit friend groups playing together |
| **Platform** | PC (Steam) |
| **Engine** | Unity 6000.5.5f1, URP, Input System |
| **Networking** | PurrNet, host-client (one player hosts) |
| **Art style** | Low poly-ish (art pending) |
| **Tone** | Silly and chaotic, with room to turn eldritch for certain enemies later |

### Pitch
One team plays **hunters** with guns, nets and gadgets. The other team plays an **animal**, which is either dangerous or scared/annoying. In Proto 1, hunters chase **monkeys** through a vertical jungle arena. The monkeys climb, swing, throw poop, ride hunters' heads and lure them into hazards. The hunters tranq, net and cage them.

### Pillars
1. **Friend chaos.** Friendly fire, head-riding and hazards create funny moments between friends.
2. **Asymmetry.** Hunters have firepower. Animals have mobility and mischief.
3. **Short rounds.** Games last 5–10 minutes, so you can jump in and play again.

---

## 2. Core Loop

1. Players gather in the lobby. The host picks the **enemy type**, and each player **picks a side**.
2. The round starts. Teams spawn in **different parts of the map**, with no hiding or setup phase.
3. Hunters track, slow, net and cage the animals. Animals evade, harass and lead hunters into hazards.
4. The round ends on a win condition. There is no scoring, and teams **do not swap** roles automatically.

---

## 3. Lobby & Match Setup

- **Connection (Proto 1):** direct IP. One player hosts and the others join by IP.
- **Connection (later):** Steam lobbies and invites.
- **Enemy type selector:** exists in Proto 1, but only **Monkey** is available. Each enemy type shows a **recommended team distribution**.
  - Dangerous enemies are always the minority.
  - Passive or annoying enemies (like monkeys) can be the majority, e.g. 1 hunter vs 5 monkeys.
- **Team sizes:** set freely by player choice. Players pick their own side.
- **Max player count:** 8 (for now).
- **Recommended Monkey distribution:** 3 monkeys vs 5 hunters (placeholder, to be balanced later). The lobby only **displays** it and doesn't enforce it.

### 3.1 Flow
**MainMenu** (name + IP + port, Host / Join) → **Lobby hub** → **Round** (map) → results → back to **Lobby hub**.

- **Player name:** typed in a field in the MainMenu. Name, IP and port are **saved on this PC** between sessions (with the loadout and settings). Steam names come later.
- **Lobby hub:** a small **walkable 3D room**.
  - **Team squares:** step onto the Hunters, Monkeys or **Spectate** square to pick that side. Your choice **stays** after you step off and only changes when you step onto another square. New players start unassigned (spectator) until they pick.
  - **Hub body:** a hunter-sized capsule while unassigned or on the Hunters side (the hunter kit works only on the Hunters side). Stepping onto the **Monkeys** square swaps you **in place to a real monkey body** (same size, speed, jump and kit as in a round); stepping back swaps you back.
  - **Monkey corner:** two climbable trees with a vine swing line, a rock pile, a fruit bush and a banana plant. Hub monkeys practise under hub rules: throws only affect the test dummies (poop "Splat", rock "Rock"), peels and decoys aren't placed, and throwing or eating never uses anything up.
  - **Host console:** the host walks to it and presses Interact to open the settings (enemy type, round timer) and **Start**.
- **Start rule:** the host can start **anytime, with no limits**, even with an empty side (for solo testing).
- **Spectators** (Spectate square, or never picked) **spectate** the round at start (free pick of anyone).
- **Round timer:** set by the host in the console UI, default 10 minutes.

### 3.2 Connections
- **Late join** during a round: the player **spectates** (free pick of anyone) until the round ends, then enters the lobby.
- **Disconnect mid-round:** the character is removed and the player **counts as dead**.
- **Host leaves:** the session ends and everyone returns to the MainMenu with a "Host left" message.

### 3.3 Esc Menu
- Works in the hub and during a round. It unlocks the cursor and shows **Resume / Settings / Leave** (to MainMenu) **/ Quit**.
- **Settings:** mouse sensitivity (0.25×–3×), field of view (60–100°) and volume. They apply immediately and are saved on this PC.

---

## 4. Win / Loss Conditions

| Condition | Winner |
|---|---|
| All monkeys are dead or caged | Hunters |
| All hunters are dead | Monkeys |
| Round timer expires | Monkeys |

- **Round timer:** 10 minutes by default (set by the host in the lobby).
- **Round start:** everyone spawns **frozen** in their team's spawn area (hunters at one end of the map, monkeys at the other, random free point). A **3-second countdown** runs, then play starts along with the timer.
- **Round end:** a results screen ("Hunters win" / "Monkeys win") shows for **5 seconds**, then everyone returns to the lobby hub automatically. Countdown and results durations live in a settings asset.
- A monkey counts as **captured** only while it is **caged** (see §6.4). A netted monkey is *not* out yet. A caged monkey that other monkeys free is back in play.
- **Dead players:** no respawns. They spectate a living teammate from a third-person camera. The mouse **orbits** around the teammate, and **left/right click** switches to another teammate.

### 4.1 HUD (in round)
- Round timer.
- Players alive per team (e.g. Hunters 4/5 · Monkeys 2/3).
- Own health bar.
- Own team label.
- Proto 1 does not distinguish between capturing a monkey alive and killing it. Both count as removed.

---

## 5. Shared Systems

### 5.1 Character Controller
- Both teams play in **first person**.
- The base controller comes from the user's existing controller in `E:\UnityProjects\3dPixelZombies`, branch **`fpsTesting`**. Its scripts will be copied out with git without switching branches in that project.
- Monkey-specific movement (climbing, swinging) is built on top of it.

### 5.2 Health
- Both teams have health.
- **Hunters: 100 HP. Monkeys: 40 HP.**
- In Proto 1, every character on a team has the same stats. Classes may come later.

### 5.3 Friendly Fire
- **On.** Hunters can hit each other, which makes shooting at a monkey riding a teammate's head a gamble.

### 5.4 Directional Input Minigame
Several systems share one minigame: you press a sequence of directions with the **arrow keys** (WASD stays movement), for example `← ← → ↑ ↓`.

- Sequence length: **5–6 keys on the monkey side, 8 keys on the hunter side** for now. Each use case can set its own length.
- Target time to complete: **3–5 seconds** (how long the minigame takes, not the number of keys).
- A **wrong key restarts** with a **new random sequence**.
- **Rescue / unlock** are started by a teammate looking at the netted player or the cage and pressing **E**. Moving out of reach, or being netted, tranq-asleep, tripped, ridden or killed cancels it.
- Head-ride resistance uses a "natural" variant: the on-screen UI shows the next direction, the ridden hunter **looks that way with the mouse, then back to center** before the next one counts, leaving WASD free to fight the steering (see §7.4).

| Use | Who | Length |
|---|---|---|
| Escape net | Netted monkey | 5–6 |
| Pickup race | Hunter (8) vs awake netted monkey (5–6) | 8 / 5–6 |
| Rescue a netted monkey | Monkey teammate | 5–6, shorter than escape |
| Unlock a cage | Monkey teammate | 5–6 |
| Call in resupply (walkie-talkie) | Hunter | 8 (fixed code per supply group, §6.3) |
| Shake off head-rider | Ridden hunter (look that way, back to center) | 8 |

### 5.5 Environmental Hazards
- **Quicksand.** Traps any player who steps in. If no teammate helps within **10 seconds**, the trapped player dies.
  - **Trapped:** the player slowly sinks (their view lowers over the 10 s) and can't move, but can still **look, shoot and throw**. There is **no self-escape**.
  - **Help:** a teammate looks at the trapped player within reach (2.5 m) and **holds E for 5 seconds** (progress bar). The trapped player's death timer **pauses** while this happens. Letting go, looking away or leaving reach **resets** the progress.
  - The help is **interrupted** if a monkey rides the helping player.
  - **Freed:** the helped player is **pulled out** onto solid ground next to the helper.
  - **Timer:** the trapped player sees a countdown bar; their **teammates** see "HELP!" and the countdown above the trapped player's name tag. Enemies don't see the countdown.
  - A ridden hunter who gets trapped makes the rider **hop off** (§7.4). A carrier who gets trapped **drops** the carried monkey (still netted).
  - **Netted bodies sink too:** a netted monkey on quicksand is trapped and dies unless a monkey teammate helps it out, which also frees it from the net. A trapped body can't be picked up. Caged monkeys are never trapped.
- **Banana peels.** Thrown by monkeys (§7.3). A player who steps on one takes **20 damage** and **topples over for 3 seconds**.
- The environment is designed to matter for gameplay. Monkeys win fights by luring hunters into hazards.

---

## 6. Hunters

### 6.1 Loadout
- **5 slots, fully customizable:** every slot takes any item, or stays empty:
  - **Guns:** hunting rifle, tranq gun, net launcher. **Each gun at most once** per loadout.
  - **Gear** (any number of times, e.g. 5 walkie-talkies):
    - **Cage:** placeable (see §6.4). One slot holds a stack of **2 cages**.
    - **Binoculars:** zoom only.
    - **Walkie-talkie:** special item, 2 calls per round per slot (see §6.3).
  - Default (first time): hunting rifle, tranq gun, net launcher, binoculars, empty.
- **Picking items:** on **racks in the lobby hub**. A hunter selects a slot (1–5 / wheel), looks at an item on the rack and presses **E**: that slot takes it. Picking a gun that is already in another slot **swaps** the two slots. **G** clears the selected slot. The loadout is remembered for the round and saved on this PC.
- **Hub test range:** in the hub, hunters hold their real loadout and can use it on **dummy targets** that show hits, tranq stacks and nets. In the hub nothing runs out (reserves, cages, calls), and shots only affect the dummies.
- **Switching:** number keys **1–5** and the **mouse wheel**. While right mouse is held (aiming / zooming), the wheel **never switches slots**.
- Planned later items: tracking beeper, health kit, water, etc.

### 6.2 Weapons
- **All three guns are single-shot** (one shot per click, short cooldown) and fire **projectiles**.
- **Projectile speed/range** keep the reference feel but are **scaled to first person** (configurable per gun).
- **Hit detection:** the **shooter's machine decides** hits and reports them (trusted friends).
- **Reload:** **R**, plus an **automatic reload** when firing an empty magazine with ammo in reserve.
- **Recoil:** each shot snaps the view up by a small kick and it springs back to where you aimed. No spread (for now).
- **Feel (M9):** fast bullets (rifle 300 m/s, tranq 80 m/s), 0.15 s weapon switch, early clicks still fire once the gun is ready, muzzle flash, impact puffs, and a hit marker (plus a bigger one when your hit kills, puts to sleep or nets someone). See [M9-Plan.md](M9-Plan.md).
- **Aiming:** hold **right mouse** on a gun for a small zoom (~1.3x).
- **Hunting rifle:** deals **5 damage** per hit.
- **Tranq gun:** each hit adds a **tranq stack**. Each stack slows the target (walking, climbing and swing pumping alike) and strengthens a **full-screen "drunk" effect**. At **4 stacks** the target falls **asleep**. Stacks wear off one at a time when the target goes a while without being hit.
- **Net launcher:** fires a net that **traps** the target in place (see §6.4); a **rope net drapes over** the trapped player (they see the ropes around them). A level shot reaches ~20 m. F8 frees yourself in editor / dev builds.
- **Ammo:** 6 rounds per magazine, 32 rounds total. Same for all three guns for now, **configurable per weapon**.
- **Feel reference:** projectile speed and general vibe come from the **assault rifle** in the modular weapons system in `E:\UnityProjects\3dPixelZombies\3d pixel art testing`, branch **`codex/topdown-multiplayer`**.
- **Damage:** hunting rifle 5. Tranq gun and net launcher deal **0** damage.
- **Friendly fire:** all three guns affect hunters too. The rifle damages, the tranq slows and puts to sleep, and the net traps. Netted hunters escape the same way as monkeys.

### 6.2.1 Binoculars
- With binoculars equipped, **hold right mouse** to zoom, with lower mouse sensitivity and a two-lens overlay.
- While zoomed, the **mouse wheel** changes zoom between **2x and 8x** (about x1.25 per notch). Each press starts at **4x**.
- Items with scopes added later (e.g. sniper-scoped guns) use the same scroll zoom.

### 6.3 Resupply (Walkie-Talkie)
- Special item with **2 uses** per round.
- Supplies are split into **groups**, for example **Healing**, **Capture** and **Damage**.
- Each group has its own **arrow-key code**, like Helldivers 2 stratagems. The hunter picks a group by entering that group's code (§5.4).
- A **support helicopter** arrives **15 seconds** after the call and drops the chosen group. No cooldown.
- **Calling:** with the walkie-talkie slot selected, **left mouse** opens a list of the groups and their 8-arrow codes. Typing any group's code with the arrow keys calls that group; a wrong arrow starts over. **WASD keeps walking** while typing. Being netted, tranq-asleep, tripped, ridden or killed cancels the call.
- **Uses:** 2 calls per walkie-talkie slot per round, used up when the code is finished.
- **Drop spot:** where the caller stood when they finished the code. A **smoke marker** shows the spot for the 15 s and a helicopter flies over to drop the crate. **Everyone** sees and hears it, so monkeys can come and cause trouble.
- The drop still arrives if the caller is netted or killed while waiting.
- **Crate:** any hunter presses **E** on it to take its contents **once each**. It stays for **60 s** after landing, then vanishes. Monkeys can't use it.
- **Groups:**
  - **Healing:** restores the hunter to full health. A separate health-kit item may come later.
  - **Capture:** refills the net launcher (full magazine and reserve) and tops cage slots back up to full. Hunters without a cage slot get net ammo only.
  - **Damage:** refills the hunting rifle and tranq gun (full magazines and reserve).

### 6.4 Net → Cage → Capture Flow
1. **Net:** a hunter hits a monkey with the net launcher. A netted monkey stays trapped **until it escapes**, with no timeout.
2. **Escape:** the netted monkey plays the escape minigame (5–6 keys). Finishing it frees the monkey. It **keeps escaping the whole time it is netted**, including while being picked up and carried.
3. **Rescue:** a monkey teammate looks at the netted monkey and presses **E** to start a shorter rescue minigame.
4. **Tranq:** a netted monkey can be tranq'd. A sleeping monkey **sleeps through being picked up**, so it can't escape.
5. **Pickup:** a hunter looks at the netted monkey and presses **E**.
   - **Asleep:** no resistance.
   - **Awake:** a **race minigame** starts. The hunter plays the pickup minigame (8 keys) while the monkey plays its escape minigame, and whoever finishes first wins.
   - A hunter can pick up a monkey **even without a cage left** (they then have to get one).
6. **Carry:** the hunter carries the netted monkey at **reduced speed (~70%)** and **can't shoot**. They can switch only to a cage slot. **G** drops the monkey (still netted).
   - If the carrier is netted, tranq-asleep, tripped, ridden, trapped in quicksand or killed, the monkey **drops, still netted**.
7. **Cage:** with a cage slot selected, **left mouse** places the cage in front of the hunter, with the carried monkey inside.
   - Hunters can also place **empty cages** anywhere, and pick an empty cage back up with **E**.
   - The cage is **inescapable** from the inside, and the caged monkey counts as captured (§4).
   - A cage with a monkey in it **can't be moved**.
   - The hunter who locked the cage **holds its key**. The key shows on that hunter's HUD and **hangs visibly on their body**, so monkeys know whom to ride.
8. **Caged monkey:** can **look around and throw poop** out of the cage, but can't move or do anything else. Bars don't protect it: hunters **can still shoot it dead** (it then counts as dead instead of captured).
9. **Freeing a caged monkey:** other monkeys can unlock the cage in two ways:
   - look at the cage, press **E** and play the unlock minigame, or
   - get the key (steal it by riding the holder, §7.4, or pick up a dropped one) and press **E** on the matching cage: an **instant** unlock.
10. **Dropped key:** if a monkey carrying a key is netted, caged or killed, the **key drops**. A hunter carrying a key drops it when **netted or killed**. Anyone (hunters or monkeys) can pick it up with E.
   - A dropped key lands where its holder **last stood on solid ground**, so a holder lost in a pit leaves it at the edge.
   - A key only exists while its cage has a monkey locked in it. Once the cage is empty again (monkey freed or killed inside), the key **vanishes** from whoever holds it or from the ground.
   - Key holders show a gold key at the hip **and** a small key icon next to their name tag.
11. **Netted hunters** (friendly fire) escape with the same minigame and length as monkeys (5–6 keys). Hunter teammates can rescue them. Monkeys can't pick hunters up.

---

## 7. Monkeys

### 7.1 Movement
- **Body:** much smaller than hunters (**~0.55 m** tall) with a low eye height (**~0.45 m**), so monkeys can hide behind low cover. Their step-up height is about 0.22 m, so higher ledges need a jump.
- **Ground movement:** faster than hunters and jumps much higher (placeholder: another ×1.2 walk/sprint on top of the hunters' speed; a jump reaches **~3.25 m** (raised 30% on 2026-10-23), against a hunter's 1 m; tunable). Since M9 everyone moves 30% faster (hunters walk 5.5 / sprint 8 m/s, monkeys 6.6 / 9.6 m/s) and falls with heavier gravity, so jumps are snappy.
- **Hunter speed** (for reference): everyone moves ×1.65 compared to the originally ported controller (walk 148.5, sprint bonus 66 in controller units).
- **Sprint.**
- **Climb (monkeys only):** works on any tree and rock tagged as climbable. **Jump while touching** a climbable surface to grab on. Then **W** climbs up, **S** down, **A/D** sideways, and **Jump** leaps off. Climbing is as fast as walking, and **Shift** sprints (same speeds as on the ground). The map borders are the limit for Proto 1.
- **Swing (monkeys only):** only on placed vines and swing points. **Jump near a swing point** to grab it, then swing on a rope with physics. **W/S** pump the swing, and **Jump** releases you with the swing's momentum.

### 7.2 Slots & Pickups
- **Slots** work like the hunters': **1 Poop, 2 Rock, 3 Banana peel, 4 Decoy**. Keys 1–4 and the mouse wheel switch, and **left mouse** throws the selected item.
- **Throwing:** **hold left mouse to charge** (short lob → long throw), release to throw along a gravity arc.
- **Pickups:** look at a rock, fruit or banana and press **E**.
- **Carry caps (placeholder):** rocks 3, banana peels 2, food (fruit or bananas) 3, poop bar 5, decoys 3 (not refillable).
- **Food:** carried, eaten later with **F**. Eating any fruit gives **+3 poop**. A banana also gives **1 peel** (if under the peel cap). Eating at a full poop bar is allowed (the poop is wasted).
- **Respawn:** rock piles, fruit bushes and banana trees refill **~30 s** after being taken (tunable).

### 7.3 Throwables
- **Poop:** from the **poop bar** (5).
  - A hit adds a **splat blob** at a random spot on the target's screen (45–80% of the screen height; solid in the centre, fading gradually to 70% opaque at the edges). More hits = more of the screen covered. Splats **do not clear on their own**.
  - **Hold X ~2 s** to wipe everything. Stopping early only fades the splats **partially**. While there's poop on your screen, a **"Hold X to wipe"** prompt shows above the crosshair (the wipe bar takes its place while you hold X).
  - Poop also splats **other monkeys** (friendly fire).
- **Rocks:** picked up in the world and thrown to **make noise** where they land, as a decoy. Hitting a hunter gives a **tiny camera flinch**, no damage.
- **Banana peels:** bananas **grow on banana trees** around the map and **lie on the ground** beneath them. Thrown peels **stay until someone steps on them**. A player who steps on one takes **20 damage** and their **body tips over** for **3 seconds**, then stands back up. Peels trip **monkeys too**.

### 7.3.1 Decoys
- Each monkey has **3 decoys**, **thrown the same way as poop**.
- A decoy is a small, hard-to-spot seed pod that lies where it lands until triggered.
- It triggers when a **hunter** walks within **4 m** (monkeys don't set it off). It plays a loud monkey screech + rustle, then disappears. **Single use.**
- Rock throws work as decoys too.

### 7.4 Head-Ride
- **Mount:** the monkey **lands on top of a hunter's head** and grabs on automatically. There is no auto-aim or snap, so it takes a precise jump or drop (from a tree, swing or rock).
  - Only **hunters** can be ridden, and only by **one monkey** at a time.
- **Duration:** the ride lasts until the rider is shaken off, shot off or hops off, or the hunter goes down. It can go on indefinitely.
- **Rider:** sees from its own eyes on top of the head, with free mouse look.
  - **WASD steers** the hunter, relative to where the rider looks, for example into hazards.
  - It can't throw, eat or press E while riding. **Jump** hops off cleanly, in the direction it looks.
- **Steering (tug of war, rider stronger):** the rider's WASD pushes the hunter and the hunter's own WASD pushes back. The hunter can slow it down but not stop it alone.
- **View block:** the rider's **real hands** (3D, with its arms reaching down) cover the hunter's eyes: about **80%** of the hunter's screen, with fingers that spread and close so the hunter catches glimpses. Everyone sees the hands over the hunter's face.
- **Ridden hunter:** can only move (to fight the steering). No firing, aiming, switching, reloading or E.
- **Shake-off:** the ridden hunter's resistance minigame. An on-screen prompt shows a direction; the hunter **looks that way with the mouse, then back to center** (a grace area around where they started) before the next prompt counts. 8 prompts; a wrong direction restarts with a new sequence (§5.4). Finishing it throws the rider off.
- **Shot off:** any rifle, tranq or net hit on the rider throws it off, and the shot also has its normal effect (5 damage, a tranq stack, or the net). Friendly fire puts the ridden hunter at risk.
- **Thrown off** (shaken or shot off): the monkey **ragdolls for 3 seconds**. Placeholder: it's flung back and up, then lies tipped over like a peel trip, then stands up. It can mount again right away.
- **Hunter goes down:** if the ridden hunter is netted, falls asleep, trips on a peel, gets trapped in quicksand or dies, the rider **hops off** cleanly (no ragdoll).
- **Carrier:** mounting a hunter who carries a netted monkey makes them **drop it** (still netted). The carried monkey sits on top of the carrier's head, so landing on it counts as landing on the head.
- **Key stealing:** landing on a key holder **takes all their cage keys** at once (§6.4).
- Riding a hunter who is helping a teammate out of quicksand **interrupts** the help (§5.5).

### 7.5 Combat Limits
- Monkeys **cannot kill hunters outright**. They only harass and lead them into map dangers.

---

## 8. Map — Proto 1: Jungle Arena

- **Shape:** a circular arena, bounded by a cliff wall.
- **Size (decided 2026-10-23):** about **400 m across**. Hunters sprint across in ~50 s, monkeys in ~40 s.
- **Layout (decided 2026-10-23, from the top-down draft):**
  - **River** from west to east through the centre, with a meander.
  - **Ruins** in the centre on both banks, joined by a **stone bridge**. Also a **ford** in the west and a **fallen-log bridge** in the east.
  - **North plateau** with ~10 m cliffs. The **monkey den** sits in its canopy forest.
  - **Hunter camp** in an open clearing in the south.
  - **East ridge** with ~6 m cliffs south of the river, and a **boulder field** in the south-west.
  - **Dense jungle** with climbable trees in the west, north (plateau canopy), south-west, east and a small grove south of the ruins.
  - **Vines** link the canopy and cross the river (monkey highways).
  - **Quicksand marsh** on the river banks (by the ford, east of the ruins, north bank).
  - **Banana trees, fruit bushes and rock piles** spread around the forests, cliffs and ruins.
- **River (decided):** everyone can **wade anywhere at ~50% speed** (no sprint, no jump). The bridges and the ford are the fast ways across, and monkeys can also swing over on vines.
- **Cliffs (decided):** monkeys climb any cliff face at full climbing speed. Hunters take the **ramps** (the long way round) or **ladders**, which are short but slow (~4 s up a 10 m cliff) and block shooting and switching items while on them.
- **Verticality:** a vertical canopy layer for climbing and swinging.
- **Spawns:** monkeys at the den (north), hunters at the camp (south).
- **Built (M8, 2026-10-23):** the `JungleArena` scene, generated by `MonkeeHunt > Build Jungle Arena` from `Settings/Map/JungleArenaLayout.asset`. The host picks the map in the host console (Blockout stays as a test map). Canopy leaves hide monkeys but let shots through; big canopy trees have perches to stand on. Top-down render: [M8-Arena-TopDown.png](M8-Arena-TopDown.png).
- **Ambient fauna:** audio only for Proto 1. NPC wildlife comes later.

---

## 9. Audio & Feedback (Proto 1)

- Jungle ambience and ambient fauna sounds.
- Gameplay sounds that carry information: rock impacts and decoy noises that hunters can hear and track.
- Screen effects: tranq "drunk" effect, poop splat, head-ride view block.
- Minigame UI: shows the key sequence, plus the directional prompts for head-ride resistance.
- **Hit feedback (M9):** getting hit flashes the screen edges red and shows a red arc pointing to the shooter or thrower, with a hurt thud (a squelch for poop). Sprinting widens the view ~6°.
- **Placeholder textures:** generated in code (`MonkeeHunt > Generate Placeholder Textures`) into `Assets/_Project/Settings/Ui` (splat blob, tranq vignette, binocular mask, minigame arrow, damage vignette, hit arc) and `Settings/Fx` (particle dot). All HUD and menu parts are real objects in `HudCanvas.prefab` / `EscMenuCanvas.prefab`; nothing in the UI is built at runtime.
- **Placeholder sounds (M9):** generated in code (`MonkeeHunt > Generate Placeholder Sounds`) into `Assets/_Project/Audio/Generated`: hit tick, kill confirm, hurt, squelch, impacts, tranq / net shots, jump / land, throw, rock clack, decoy screech. Replace the files with real sounds anytime.

---

## 10. Networking Notes

- **PurrNet**, host-client model with one player hosting.
- Friends trust each other, so there is **no anti-cheat**. Use PurrNet's **most permissive** ruleset, which allows client-authoritative logic where it's convenient.
- Proto 1 connects by direct IP. Steam transport comes later.

---

## 11. Proto 1 Scope

### In scope
- Lobby with direct-IP host/join, side picking, and an enemy-type selector (Monkey only) showing the recommended distribution.
- First-person controller ported from `3dPixelZombies` / `fpsTesting`.
- Hunters: hunting rifle, tranq gun, net launcher, binoculars, walkie-talkie helicopter resupply, cages and keys.
- Monkeys: sprint, climb, swing, rocks, poop and the poop bar with food refills, banana peels, decoy noises, head-ride.
- Shared directional minigame (net escape, net rescue, cage unlock, resupply call-in, head-ride resistance).
- Health, friendly fire, death, and third-person teammate spectating.
- Hazards: quicksand and banana peels.
- Round timer (configurable, default 10 minutes) and win conditions.
- One map: the circular jungle arena.
- Placeholder low-poly art.

### Out of scope (Proto 1)
- Steam lobbies and invites.
- Proximity voice chat.
- Scoring, progression, cosmetics.
- Automatic role swapping between rounds.
- Classes or stat differences within a team.
- Enemy types other than Monkey.
- NPC fauna (beyond audio).
- Any difference between capturing alive and killing.
- Additional hunter items beyond binoculars.
- Anti-cheat and server authority.

---

## 12. Milestones (draft)

| # | Milestone | Done when |
|---|---|---|
| M1 ✅ | Project setup | PurrNet installed, controller ported, two players can host and join by IP and move around |
| M2 ✅ | Lobby & round flow | Side picking, enemy selector, spawns, timer, win conditions, spectating |
| M3 ✅ | Hunter kit | Damage and tranq guns, ammo, net launcher, binoculars, friendly fire |
| M4 ✅ | Monkey kit | Climb, swing, rocks, poop and food, banana peels, decoys |
| M5 ✅ | Minigames & capture | Shared directional minigame, net escape, rescue, cages, keys and cage unlock |
| M6 | Head-ride | Ride, view block, steering, resistance minigame, key stealing |
| M7 | Hazards & resupply | Quicksand and teammate help, walkie-talkie helicopter resupply (banana trees and peels were done in M4) |
| M8 | Jungle arena | Blockout of the circular map with ruins, river and canopy (layout, size, river and cliff rules decided 2026-10-23, GDD §8; plan: [M8-Plan.md](M8-Plan.md)) |
| M9 | Playtest polish | Audio, screen effects, tuning pass (part 1, the feel pass, built 2026-10-22: [M9-Plan.md](M9-Plan.md)) |

Milestone order and grouping still need your sign-off.

---

## 13. Future Vision (outside Proto 1 scope)

- **More enemy types,** both dangerous (minority) and passive or annoying (majority).
- **Eldritch turn:** certain enemies transform. For example, a monkey becomes a monster when it is killed, or when it isn't captured in time.
- **Capture vs kill** having different outcomes.
- **Hunter classes** with different stats and loadouts, plus more items (bait, traps, tracking gear, etc.).
- **NPC fauna** that animals can blend in with.
- **Proximity voice chat.**
- **Steam** lobbies and invites.
- **Objectives** that change round length and goals for each enemy type.

---

## 14. Open Questions

1. Balance numbers to tune once there's a working version: team distribution (placeholder 3 monkeys vs 5 hunters), minigame lengths. (Map size was decided on 2026-10-23: ~400 m across.)
