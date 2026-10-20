# M2 — Lobby & Round Flow: Plan

> Design decisions are in [GDD.md](GDD.md) §3–4.
> Technical choices marked **Approved** were confirmed on 2026-10-17.
> **Status: Done (accepted 2026-10-17).** Two-player tested: spawning, death, win, return to lobby. Not yet two-player tested: leaver, late join, host leaving, Spectate-square spectating; check during later playtests.

## Done when
- Players enter a name, host or join by IP, and land in a walkable lobby hub.
- Standing in a team zone sets your side, and your capsule takes on that team's look.
- The host uses the console to set the round timer (enemy type: Monkey only, with its recommended split shown) and press Start.
- Everyone loads into the map at their team's spawn area, frozen through a 3-second countdown.
- The HUD shows the timer, players alive per team, own health and own team.
- Falling into the kill zone kills you. Dead players orbit-spectate teammates (click to cycle).
- A win (team eliminated or timer out) shows results for 5 seconds, then everyone returns to the hub.
- Leavers count as dead, late joiners spectate, and a host leaving sends everyone to the MainMenu.
- The Esc menu offers Resume / Leave / Quit.

---

## Tasks

### 1. Prefabs
- `PlayerBase` prefab (the current Player) → prefab variants **`Hunter`**, **`Monkey`** and **`LobbyPlayer`**.
- Team look: placeholder colors. **Approved:** hunters orange (current), monkeys brown, lobby neutral grey.
- Hub look change: **Approved:** the LobbyPlayer swaps its *material color* by synced team rather than respawning a different prefab. It's cheaper and has no respawn hitch. Real models will swap a model instead.

### 2. Session data (server-side)
- **Approved:** a server-held `MatchSession` (plain C#, lives on the host for the whole session) that stores each player's **name**, **team** (Unassigned / Hunters / Monkeys) and the **match settings** (enemy type, round time).
- A client sends its name to the server once after connecting.
- Each scene shows this data through its own networked controller with synced fields, so clients never read `MatchSession` directly.

### 3. Lobby hub scene
- A small room (about 20×20 m) with two colored floor zones (Hunters / Monkeys), a host console and a few spawn points.
- `LobbyController` (networked): synced player list (name, team) and synced settings. On Start it loads the round scene.
- `TeamZone` triggers: the server updates the player's team on enter and exit.
- Floating name tags above players.
- **Host console UI:** enemy type dropdown (Monkey) + recommended-split text, a round timer field (default 10:00) and a **Start** button. Only the host can open it.

### 4. Round scene (Blockout)
- Two spawn areas at opposite ends, plus a **kill zone**: a pit in the blockout and a catch-all plane below the map.
- `RoundSpawner` (server): spawns each player's `Hunter` or `Monkey` prefab at a random free point in their team's area. Unassigned players and late joiners get no body and go to spectate.
- `RoundController` (networked): states **Countdown (3 s) → Playing → Results (5 s)**, with a synced timer and synced alive counts. The server checks win conditions, then loads the Lobby hub.
- Frozen during the countdown: movement is locked and look stays free. **Approved:** look stays free so players can glance around.

### 5. Health & death
- `PlayerHealth` (networked): synced current/max HP. Hunters 100, monkeys 40, per prefab variant. The server applies all damage.
- `KillZone`: deals lethal damage on enter (server).
- On death: the body is removed (**Approved:** despawned, no ragdoll yet), the player is marked dead and the local player switches to spectating.
- Disconnect mid-round: counts as dead.

### 6. Spectating
- `SpectatorCamera` (local only): orbits the followed target with the mouse; left/right click cycles targets.
- Dead players can follow **living teammates** only. Unassigned players and late joiners can follow **anyone**.
- If nobody valid is left, the camera stays at a fixed overview point.

### 7. HUD & screens
- In-round HUD: timer, alive per team, own health bar, own team label.
- Countdown overlay ("3 / 2 / 1 / GO").
- Results overlay ("Hunters win" / "Monkeys win").
- Esc menu (hub + round): Resume / Leave → MainMenu / Quit. Unlocks the cursor while open.
- MainMenu: add a **Name** field. **Not saved** between sessions for now. Show a "Host left" / "Disconnected" message on return.

### 8. Settings asset
- `MatchFlowSettings` (ScriptableObject): countdown 3 s, results 5 s, default round time 600 s, plus team HP (100 / 40).

---

## Suggested build order
1. Prefab variants + `PlayerHealth` + `KillZone` (can be tested in the current Blockout).
2. `MatchSession` + MainMenu name field + Lobby hub scene with zones and name tags.
3. Host console + Start → round scene + `RoundSpawner`.
4. `RoundController` (countdown, timer, win check, results, return to lobby).
5. Spectator camera + death handling + leavers / late joiners.
6. HUD, overlays, Esc menu, host-left handling.
7. Two-player test pass (you), then fixes.

## Resolved points
- **Hub layout:** just the basics. A ~20×20 m room with two colored floor zones, the host console and a few boxes. Art comes later.
- **Name tags:** always on, tinted by team (grey when unassigned).
- **Kill zone:** a hole in the ground into a dark pit, with an invisible kill trigger at the bottom (plus the catch-all plane below the map).
