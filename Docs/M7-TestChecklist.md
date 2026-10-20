# M7 — Multiplayer Test Checklist

Everything here was checked as host with stand-in bodies, but never with real players pressing real keys. Tick what works and note anything that doesn't (what you did, what you saw, who saw it).

## Setup
- Host in the editor, the others join `127.0.0.1` port `5000` (Multiplayer Play Mode virtual players or a build).
- Lobby: hunters put a **Walkie-talkie** (and a **Cage**) into slots at the loadout rack (select the slot with 1–5, look at the item, press E).
- Quicksand patches (sandy discs, 4 m wide): under the swing line (13, 20), west between the tree and boulder (−10, 14), and on the hunters' side between the tree and boulder (20, −12). The last one is closest to the hunter spawn.
- Call codes: **Healing** ↑↑↓↓←→←→, **Capture** ←←→→↑↓↑↓, **Damage** ↓→↓→↑←↑←.
- Keys: **E** (hold) = pull a teammate out, **LMB** with the walkie selected = open the code list, arrow keys = type, **LMB again** = cancel.

---

## A. Quicksand (2 hunters H1, H2 + 1 monkey M)
- [ ] **Trapped:** H1 walks onto a patch → can't move, slowly sinks (view lowers), sees "SINKING 9.x" with a red bar. Can still look around and shoot.
- [ ] **No self-escape:** WASD and Jump do nothing; after 10 s H1 dies.
- [ ] **HELP tag:** H2 sees a red "HELP! 7" counting down above H1's name tag. M does **not** see it.
- [ ] **Help:** H2 stands at the edge, looks at H1 and holds E → "Pulling out... (hold E)" bar fills in 5 s, H1 sees "Being pulled out..." and the timer stops.
- [ ] **Pulled out:** after 5 s H1 stands next to H2 on solid ground and isn't trapped again.
- [ ] **Hold resets:** letting go of E, looking away, or walking more than 2.5 m away resets H2's bar, and H1's timer runs again.
- [ ] **No enemy help:** M looks at a trapped hunter and holds E → nothing happens.
- [ ] **Ridden helper:** M rides H2 while H2 is pulling H1 out → the pull stops.
- [ ] **Rider + quicksand:** M steers H1 onto the sand → M hops off; H1 is trapped (M may land in the sand too).
- [ ] **Carrier:** H1 carries a netted monkey onto the sand → the monkey is dropped (still netted) and sinks too.
- [ ] **Netted monkey rescue:** a monkey teammate pulls a netted, sinking monkey out → it ends up free of both the sand and the net.
- [ ] **No pickup from the sand:** a hunter can't pick up a netted monkey that is stuck in quicksand.
- [ ] **Monkeys above it:** M climbing or swinging above the sand isn't trapped.
- [ ] **Remote view:** others see the trapped body sink into the patch.

## B. Walkie-talkie (2 hunters H1, H2 + 1 monkey M)
- [ ] **Slot:** slot 4 shows "Walkie-talkie x2"; selecting it shows "2 calls" and a small radio in hand.
- [ ] **Code list:** LMB opens "Call in supplies" with the three rows. Typing Down lights the Damage row and dims the others; a wrong arrow flashes red and starts over.
- [ ] **Walk while typing:** WASD still walks while the list is open; arrows only type.
- [ ] **Cancel:** LMB again, switching slot, Esc, or getting netted / ridden closes the list without using a call.
- [ ] **Call:** finishing a code uses one call ("x1"), and a coloured smoke column appears where H1 stood.
- [ ] **Everyone sees it:** H2 and M see the smoke column and the helicopter flying over at ~13 s.
- [ ] **Crate lands:** at 15 s the crate (with its group label) lands on the spot and the smoke disappears.
- [ ] **Caller down:** H1 gets netted (or killed) during the wait → the crate still arrives.
- [ ] **Healing:** a hurt hunter presses E on a Healing crate → full health.
- [ ] **Capture:** E on a Capture crate → net launcher back to 6 / 32 and cage slots full again.
- [ ] **Damage:** E on a Damage crate → rifle and tranq back to 6 / 32.
- [ ] **Once each:** the same hunter pressing E again gets nothing; H2 can still take once.
- [ ] **Monkeys can't:** M pressing E on a crate does nothing.
- [ ] **Vanishes:** the crate disappears 60 s after landing.
- [ ] **Two walkies:** a hunter with walkie in both slots 4 and 5 gets 4 calls in total (2 per slot).

## Things to judge while playing
- Quicksand: 10 s to die, 5 s to pull out, 2.5 m reach. Does it feel fair and readable?
- Sinking depth: hunters 1.2 m, monkeys 0.6 m. Patch size (4 m) and the 0.8 m-tall trap zone (jumping across the edge can still catch you).
- Call codes: easy enough to type while walking?
- The drop: 15 s, helicopter at 30 m, smoke column look, crate size, labels.
- There is no pop-up text when taking a crate; you only see your health / ammo change. Is that enough?
- The crate is solid (players bump into it).
- The slot bar is a little crowded with "Walkie-talkie x2" next to "Net Launcher".
