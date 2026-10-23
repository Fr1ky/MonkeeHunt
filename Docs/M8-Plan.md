# M8 — Jungle Arena: Plan

> Layout decisions are in [GDD.md](GDD.md) §8. The top-down draft was agreed on 2026-10-23.
> Your choices from 2026-10-23 are marked **Decided**. Technical details I suggest are marked **Proposed**. Change any of them before the build starts.
> Look: placeholder low-poly shapes (GDD §11), like the rest of Proto 1.
> **Status: built and host-checked 2026-10-23 (approved as planned).** See Progress at the bottom; playtest with [M8-TestChecklist.md](M8-TestChecklist.md). Top-down render: [M8-Arena-TopDown.png](M8-Arena-TopDown.png).

## Done when
- A new **JungleArena** scene is the default round map. The host picks the map in the host console, and Blockout stays as the small test map.
- The arena is ~400 m across, inside a cliff wall nobody can leave:
  - river through the centre ruins, with a stone bridge, a ford and a log bridge;
  - the north plateau (~10 m) and the east ridge (~6 m), with ramps and ladders;
  - dense forests (~250 trees) with leafy canopies and vines;
  - quicksand marshes, pickups, the hunter camp and the monkey den.
- Everyone wades through the river at ~50% (no sprint, no jump). Monkeys climb cliffs, trees and ruins. Hunters use ramps or slow ladders, where they can't shoot or switch.
- Canopies hide monkeys, but shots pass through.
- The whole map comes from a re-runnable builder (`MonkeeHunt > Build Jungle Arena`) driven by a layout asset you can tune in the Inspector.
- Rounds work there end to end: spawns, timer, win conditions, capture, quicksand, supply drops, spectating.

---

## Tasks

### 1. Scene and map picker
- **Decided:** a new scene `Scenes/JungleArena.unity`. Blockout stays and can still be picked.
- **Proposed:**
  - The new scene gets the same round setup as Blockout: `RoundController`, `TeamSpawner` (Hunter / Monkey prefabs), spawn areas, the shared HUD and Esc menu prefabs, and a kill zone below the map. Everything is added to the build settings.
  - The host console gets a **Map** dropdown (JungleArena first, then Blockout), built in the Lobby scene UI (no runtime-built UI). The choice is kept in `MatchSession` and used by Start instead of the fixed "Blockout".

### 2. Builder and layout asset
- **Decided:** a re-runnable builder. Running it rebuilds the map.
- **Proposed:**
  - **Layout asset:** every number lives in `Settings/Map/JungleArenaLayout.asset`, so you can tune it in the Inspector and rebuild. That covers the radius, plateau outlines and heights, ramps, the river path, width and depth, crossings, forest areas and tree counts, vine links, ladders, ruins, marshes, pickups and spawns.
  - **What a rebuild replaces:** the builder deletes and recreates one root object, `Generated`, in the scene. Anything you place outside it survives a rebuild. Trees use a fixed random seed, so a rebuild gives the same map unless you change the seed.
  - **Editor-only:** the builder is an Editor script, so nothing about map generation ships in the game.

### 3. Ground (Unity Terrain)
- **Decided:** Unity Terrain for the ground, with rock blocks for cliffs.
- **Proposed:**
  - **Terrain:** one terrain covering the circle, 420 × 420 m, with a heightmap of ~0.8 m per sample.
  - **Shapes:** the plateau tops are flat at +10 m (north) and +6 m (east ridge), with steep terrain under the cliff rocks. Gentle bumps go elsewhere, never steeper than the controller's 35° walkable limit outside the cliffs. The riverbed is carved 0.4 m below the banks with soft edges.
  - **Ramps:** carved into the heightmap, ~10° slopes. That's about 60 m of path for the north plateau, so the long way round really is long.
  - **Paint:** terrain layers for grass, jungle floor, mud (river banks), sand (marshes) and rock (cliff tops). Their tile textures come from the placeholder texture generator.

### 4. Cliffs, boundary
- **Decided:**
  - Monkeys climb any cliff face at full climbing speed.
  - The boundary is a ~15 m cliff wall with an invisible cap.
- **Proposed:**
  - **Cliff rocks:** along each cliff line the builder places rock blocks (~4 m wide, cliff-high, slightly random tilt and depth, `M_Rock`) with a `Climbable` component. Hunters can't walk up them: the terrain behind them is too steep.
  - **Boundary wall:** a ring of the same rocks, ~15 m tall and **not** climbable, with invisible colliders up to 40 m.
  - **Kill zone:** a kill zone 20 m below the terrain, just in case.

### 5. River and crossings
- **Decided:** 0.4 m deep, nobody swims. Everyone wades anywhere at ~50% with no sprint and no jump.
- **Proposed:**
  - **Water:** a flat, slightly see-through water surface following the river path, plus trigger volumes along it.
  - **Wading:** a new `WaterZone` sets a 0.5 speed multiplier and blocks sprint and jump while you're inside. That's a small addition to the movement controller (a "no sprint / no jump" source, like the speed multipliers).
  - **Stone bridge:** at the ruins, wide and walkable.
  - **Ford:** a shallow stretch (0.1 m) in the west with **no** wading slowdown.
  - **Log bridge:** a fallen log in the east, about 1.2 m wide and walkable by both teams.
  - **Vine crossings** for monkeys at two places (see task 7).

### 6. Ladders
- **Decided (your call):** use the ladder from the 3dPixelZombies project (`fpsTesting`). Its `LadderController` is already in MonkeeHunt, unchanged: walk into the ladder's trigger, W/S climb at ~2.6 m/s (~4 s up a 10 m cliff).
- **Proposed:**
  - **Mesh:** copy `Ladder.fbx` from `fpsTesting` (with `git show`, no branch switching) and build a `Ladder` prefab like the one in its LevelBuilder scene: mesh, solid collider, thin trigger with `LadderController`. It's stretched or stacked to the cliff height.
  - **Placement:** 2 ladders on the north cliffs, 1 on the east ridge.
  - **Getting off at the top:** the trigger ends a little above the cliff top, so climbing on steps you onto the plateau. This needs a host check.
  - **Ladder additions** (two small changes the map decisions need):
    - while on a ladder, hunters can't fire, aim, switch or use items (GDD §8);
    - monkeys ignore ladders, since they climb the rock anyway.

### 7. Trees, canopy, vines
- **Decided:**
  - **~250 trees.**
  - **Leaves hide but shots pass:** canopies block sight, while bullets, darts and nets go through.
- **Proposed:**
  - **Two tree prefabs** (placeholder low-poly):
    - **jungle tree:** ~8 m trunk with a leaf cluster;
    - **big canopy tree:** ~14 m with 1–2 thick perch branches near the top.
  - **Trunks:** climbable. Leaves have no colliders, so they only hide.
  - **Perches:** walkable ledges where monkeys can stand and hide in the leaves. Without them, climbing to the top of a trunk would just drop you.
  - **Forests:** the forest areas from the draft fill with trees at least ~4 m apart, about 1 in 4 of them big. A few lone trees dot the clearings.
  - **Vines:** chains of `SwingPoint`s at ~12 m height, 4 m apart (as in Blockout), between big trees. They connect the plateau canopy and the east and west forests, and cross the river at two places. Visible vine strands hang from each point and run tree to tree.

### 8. Ruins, camp, den, hazards, pickups
- **Proposed:**
  - **Ruins:** stone platforms, broken walls, pillars and an arch on both banks around the stone bridge, as low-poly blocks.
    - **Monkeys:** the walls are climbable.
    - **Hunters:** steps stay under the 0.45 m step-up, so hunters can walk the platforms.
    - **Cover:** they give hunters cover.
  - **Hunter camp:** an open clearing in the south with tents, crates and the hunter spawn area. Supply drops land in the open as today.
  - **Monkey den:** big canopy trees on the north plateau with the monkey spawn area under them.
  - **Quicksand:** the existing `Quicksand` prefab, scaled up for the 3 marshes (by the ford, east of the ruins, north bank).
  - **Pickups:** the existing pickup prefabs, roughly 10 rock piles, 10 fruit bushes and 8 banana trees. They sit by the cliffs, ruins and forest edges, a few per area so no side is starved.

### 9. Look and visibility
- **Proposed:**
  - **Light:** one sun plus soft ambient light.
  - **Fog:** light distance fog (starting ~80 m, fully foggy at ~300 m) for depth. Flagged, because it limits long sightlines.
  - **Batching:** static batching and GPU instancing on the shared materials to keep ~1000 tree pieces cheap.

---

## Suggested build order
1. JungleArena scene with the round setup; map dropdown in the host console.
2. Layout asset + builder skeleton; terrain (plateaus, ridge, ramps, riverbed) and paint.
3. Cliff rocks, boundary wall + cap, kill zone.
4. River water + `WaterZone` wading; stone bridge, ford, log bridge.
5. Ladder prefab (copied mesh), placement, action block, monkeys ignore ladders.
6. Tree prefabs, forests, perches, vines and swing points.
7. Ruins, hunter camp, monkey den, quicksand, pickups, spawns.
8. Lighting / fog, host checks of every step, then a two-player checklist.

## Host checks (planned)
- **Crossing times:** hunter sprint ~50 s, monkey ~40 s.
- **Wading:** 50% speed, no sprint or jump; the ford isn't slowed.
- **Ladders:** ~4 s up 10 m, hunters can't fire on them, monkeys pass by them.
- **Monkeys:** climb cliffs, trees and ruins; stand on perches; swing the vine chains over the river.
- **Hunters:** can't walk up cliffs, but can walk the ramps, bridges and ruin steps.
- **Hazards and items:** quicksand traps; pickups refill; supply drops land on the terrain.
- **Edges:** the boundary holds; the kill zone catches falls.
- **Round flow:** a full round on the new map, spawns at camp and den.

## Progress
- **Built on 2026-10-23.**
  - **What's generated:** `MonkeeHunt > Build Jungle Arena` builds, in ~20 s:
    - the terrain (513² heights, 5 painted layers);
    - 129 cliff pieces, the boundary ring and its cap;
    - the river with 61 wading zones, the stone bridge, ford stones and the log bridge;
    - 3 ladders, the ruins and the camp;
    - ~255 trees (≈1 in 4 canopy trees with perches), ~290 bushes and 241 swing points on 5 vine chains;
    - 8 boulders, 3 quicksand marshes and 36 pickups.
  - **Scene:** the spawn areas, overview point and kill zone are moved into place.
  - **Map picker:** the host console has the new Map dropdown, with Jungle arena as the default.
- **Checked as host:**
  - **Rounds:** a round starts on the Jungle arena from the host console. The hunter spawns at the camp, the monkey at the den on the plateau (y ≈ 10).
  - **Wading:** 2.76 m/s, half the 5.52 m/s walk. A jump in the water does nothing, for hunters and monkeys alike. The ford and the stone bridge don't count as water.
  - **Ladder:** walking into it turns on climb mode and blocks firing and items; both clear when you leave it. Monkeys ignore it.
  - **Hunter limits:** pushing into a cliff for 3 s doesn't get a hunter up. Walking the north-west ramp for 13 s reaches the plateau top (10.4 m).
  - **Monkey climbing:** it works next to a cliff rock, a tree trunk, a ruin wall and a boulder.
  - **Vines:** standing on a canopy perch (~19 m on the plateau), a swing point is in grab range.
  - **Hazards and items:** all three marshes have quicksand triggers (14 / 12 / 10 m), and the 36 pickups are networked. Supply drops find the ground with a raycast, so they work on terrain.
  - **Performance:** ~88 fps in the editor with the whole arena.
- **Changed while building:**
  - **Wading rule:** the wading zones are tall, and you only count as wading while your body's root is less than 0.4 m above the water surface. A thin trigger at the waterline missed floating bodies, and this also leaves bridges out automatically.
  - **Queued jumps:** a jump pressed just before entering the water can't fire in it any more.
  - **Spawn areas:** they now drop each spawn point onto the ground (raycast), for uneven terrain. The lobby works the same as before.
  - **Boulders:** they use a lumpy 80-face low-poly mesh, because a sphere was too detailed for a convex collider and gave a warning.
- **Small calls (flagged, change any):**
  - **Bushes:** ~290 bushes with no collision, so monkeys have low cover. They weren't in the plan, but 250 trees over ~38,000 m² of forest are ~12 m apart.
  - **Camp dressing:** 4 tents, crates, a fire pit and a flag.
  - **Overview camera:** 150 m up, 250 m south.
- **Not tested as host** (in the checklist): climbing a ladder to the top with real keys and stepping off onto the plateau, real-input climbing, swinging along the vine chains and over the river, how far you can see in the fog, two players on the map, and a full round with captures.

## Open points
- None.
