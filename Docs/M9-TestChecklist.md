# M9 Feel Pass — Playtest Checklist

Checked as host in the lobby hub, but never with real keys or with several players. Tick what works and note what doesn't (what you did, what you saw, who saw it). Most of this is about how it **feels**, so write down anything that's too much, too little, too slow or too twitchy.

## Setup
- Host in the editor, the others join `127.0.0.1` port `5000` (Multiplayer Play Mode virtual players or a build).
- Everything in sections A–C works in the lobby hub: step onto the **Hunters** square, then use the test range dummies. Sections D–E need a round.

---

## A. Moving (anyone)
- [ ] **Speed:** walking and sprinting feel fast (hunters 5.5 / 8 m/s, monkeys 6.6 / 9.6 m/s). Climbing and swing pumping feel quicker too.
- [ ] **Stopping:** letting go of the keys stops you almost at once, with no sliding.
- [ ] **Jump:** hunters ~1 m, monkeys ~2.5 m. Quick up, quicker down, nothing floaty.
- [ ] **Early jump:** pressing Space just before landing jumps again right on landing (bunny-hopping works).
- [ ] **Sprint FOV:** the view widens a little while sprinting and eases back when you stop. It doesn't kick in when walking into a wall or when slowed by tranq.
- [ ] **Jump / land sounds:** a soft whoosh on jumping, a thump on landing (louder after a big fall). Only you hear your own.
- [ ] **Stairs and slopes:** going down stairs and over ledges still feels right (the air gravity only applies while the float spring isn't holding you).
- [ ] **Launches:** hopping off a head, being thrown off, climb leap-offs and swing releases still carry about as far as before.

## B. Shooting (hunter)
- [ ] **Rifle:** near-instant bullet, 0.5 s between shots, 1.3 s reload. Bright muzzle flash, the gun punches back, the view kicks up and comes back down.
- [ ] **Tranq / net:** "pfft" and "thunk" sounds, smoke puffs at the barrel, faster tranq dart (flatter).
- [ ] **Early click:** clicking just before the gun is ready (cooldown, switch, end of a reload) still fires as soon as it can.
- [ ] **Switching:** 0.15 s, and the new item swings up into view.
- [ ] **Reload:** the gun dips down and tilts during the reload and comes back up at the end.
- [ ] **Sway and bob:** the held item lags slightly behind your look and bobs while walking (more when sprinting). Not distracting?
- [ ] **Impacts:** dust + sparks where rifle shots hit the world, a dark red puff on players and dummies, a green puff for darts, a grey puff for nets.
- [ ] **Remote view:** another player sees your muzzle flash, smoke, gun punch and impact puffs, and hears your shots.

## C. Hit markers (shooter / thrower)
- [ ] **Hit:** a white X on the crosshair plus a tick when you hit a dummy or a monkey.
- [ ] **Friendly:** an orange X when a hunter hits a hunter (or a monkey poops on a monkey).
- [ ] **Down:** a bigger red X and a two-tone "ding" when your hit kills, puts to sleep (4th dart) or nets someone. On dummies: the 8th rifle hit, the 4th dart or any net.
- [ ] **Monkey throws:** poop and rock hits on hunters give the X too; a throw makes a whoosh.

## D. Getting hit (needs a round)
- [ ] **Red flash:** the screen edges flash red when you lose health (stronger for bigger hits, e.g. a peel trip).
- [ ] **Arc:** a red arc around the crosshair points to where the shot or throw came from, and keeps pointing there while you turn.
- [ ] **Sounds:** a thud on gun and rock hits, a squelch on poop.

## E. Gameplay sounds (needs a round)
- [ ] **Rock:** a clack where a thrown rock lands, heard by everyone (hunters can track it).
- [ ] **Decoy:** a monkey screech when a hunter comes near a decoy.

## Things to judge while playing
- Overall speed: too fast to aim at sprinting monkeys? Does the map feel smaller now?
- Jump gravity (×1.5 up, ×2 down): snappy or too heavy?
- Recoil size and recovery speed. Gun sway and bob amount.
- Hit marker size, colours and sounds. Arc size and how long it stays.
- Muzzle flash brightness. The placeholder sounds: which ones need replacing first?
