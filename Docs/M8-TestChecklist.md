# M8 Jungle Arena — Playtest Checklist

Checked as host with stand-in movement, but never with real keys or several players. Tick what works and note anything that doesn't (where on the map, what you did, what you saw). Map: [M8-Arena-TopDown.png](M8-Arena-TopDown.png).

## Setup
- Host in the editor, the others join `127.0.0.1` port `5000`.
- Pick sides in the hub, open the host console: **Map = Jungle arena** (the default), Start.
- If something about the map is off, the numbers are in `Assets/_Project/Settings/Map/JungleArenaLayout.asset`. Change them, then run `MonkeeHunt > Build Jungle Arena` with the JungleArena scene open (everything under `Generated` is rebuilt).

---

## A. Getting around (anyone)
- [ ] **Spawns:** hunters start at the camp in the south (tents, flag), monkeys at the den on the north plateau.
- [ ] **Size:** a hunter sprints across in about 50 s, a monkey in about 40 s.
- [ ] **River:** walking into the water slows you to half speed, with no sprint and no jump; you're back to normal once out. The stone bridge, the log bridge and the ford (stepping stones, west) aren't slowed.
- [ ] **Boundary:** nobody can climb, jump or swing out over the outer rock wall.
- [ ] **Falling:** nothing lets you fall out of the world (the kill zone below catches it if it does).

## B. Hunters
- [ ] **Ramps:** the north plateau has a ramp at each end (west and east), the east ridge one on its west side. They're long but walkable.
- [ ] **Cliffs:** hunters can't walk or jump up cliffs anywhere else.
- [ ] **Ladders:** 2 on the north cliffs, 1 on the east ridge. Walk into one, hold W to climb (~4 s up 10 m), and at the top you step off onto the plateau. You can't fire, aim, switch or use items while on it.
- [ ] **Ruins:** you can walk onto the platforms and the bridge (one step up), and the walls and pillars give cover.
- [ ] **Supply drop:** a walkie call at the camp or in the open lands on the ground.

## C. Monkeys
- [ ] **Climbing:** cliff faces, tree trunks, ruin walls, pillars and boulders, at climbing speed. Monkeys walk straight past ladders.
- [ ] **Canopy:** big canopy trees have two branch perches inside their leaves. You can stand on them, and from below or the side you're hidden in the leaves.
- [ ] **Vines:** from a perch, jump to the vine above and swing along the chain. Chains run:
  - across the north canopy;
  - from the plateau down over the river west of the ruins;
  - down the east side over the river to the south grove;
  - between the west jungle and the south-west grove;
  - from the east forest to the ridge.
- [ ] **Bushes:** you can hide in a bush (no collision).
- [ ] **Pickups:** rock piles, fruit bushes and banana trees around the forests, cliffs and ruins.

## D. Hazards and round flow
- [ ] **Quicksand:** the three sandy marshes (west by the river, south-east of the ruins, north bank) trap you as in M7.
- [ ] **Full round:** captures, cages, keys, head-rides and the win conditions all work on the new map.

## Things to judge while playing
- Is the map too big or too small for your group? Too open (clearings, river banks) or too dense (forests)?
- Ramps vs ladders: is the long way round long enough to make ladders tempting?
- Can hunters see enough? The fog starts at 80 m and is full at 300 m.
- Vine chains: do they go where monkeys want to go? Is the canopy layer worth climbing up to?
- Wading at half speed: too punishing, or about right?
- Placeholder look: anything confusing (what's climbable, where the ford is)?
