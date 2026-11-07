# D'thorga Patches

Fixes and patches for other authors' mods, each switched on only while that mod is active. It copies nothing from them. It covers
**Harrier VTOL Attack Aircraft** by potemkinvillager (a realistic build cost, a fix for the jet failing to load fully alongside
RimPacts, honest bomb costs under Combat Extended with a bomb case of its own, bombs and rockets at The Dead Man's Switch's ammunition
fabricator, fuel tanks for its Drop Tanks upgrade, and a sortie's map kept open under the jet), RimThunder's heavy casting at the
rotary forger, **Spaceship Turret Hull** by MK under Combat Extended, and **InterRim Ballistic Missile** by Psionic Zephyr (a strategic
warhead's blast spread over frames instead of freezing the game).

It replaces two earlier mods, merged into it on 7 November 2026: **D'thorga Vehicle Patches** and **D'thorga Missile Patches**
([why](../../docs/development/mod-consolidation.md)).

Version 0.7.0, built 7 November 2026 for House D'thorga. **Not yet tested in game as D'thorga Patches.** The Harrier and turret hull
parts are unchanged from Vehicle Patches 0.6.0 (not yet tested in game); the missile part was tried once by the owner, see
[InterRim Ballistic Missile](#interrim-ballistic-missile).

## Read this first (gotchas)

- **Switching from D'thorga Vehicle Patches or D'thorga Missile Patches: switch both off in RimSort, and this one on, in the same
  change.** Running an old one beside this would load the same patches twice; RimSort warns you, because the About file lists them as
  incompatible. Your save needs nothing: the old mods saved nothing of their own. The missile settings carry over by themselves.
- **Needs Harmony and D'thorga Framework. Load it after the mods it patches.** The About file asks for this, so RimSort does it for
  you. **You have to switch it on yourself**: nothing else requires it. It does nothing without the mods it patches.
- **RimWorld 1.7 and 1.6.** Its small assembly is built for each. Vehicle Framework still ships only a 1.6 build, which the game
  also runs on 1.7.
- **Safe to add to a running save.** A jet you already built keeps working; only new builds pay the new cost. Bombs already made
  stay as they are.
- **Updating from 0.5.0 or earlier with Vanilla Furniture Expanded - Factory: delete every Mk. 82 and Mk. 84 bill on your automated
  machining bays first**, then add them again after the update. A bay remembers its bill's old ingredient list (heavy castings), and
  once steel or a bomb case reaches its hoppers it would throw an error every few seconds. Deleting a half-filled bill drops what it had
  taken beside the bay. Bills at the machining table and the ammunition fabricator need nothing.
- **Safe to remove.** Costs go back to the Harrier's own, and with RimPacts active the Harrier's framework fails to load fully again.
  **With Combat Extended active, deconstruct any turret hulls built under this mod first** (not yet tested either way).
  **Delete any bomb or rocket bills you added at an ammunition fabricator, and any heavy casting bills at a rotary forger, first**, because they may
  no longer list those recipes. **With Combat Extended, use up or sell your bomb cases first** and delete their bills: the cases are this
  mod's items and vanish with it.
- **The jet now needs RimThunder's turbine, if RimThunder is active.** It's a long job:
  - **Research:** engine making, Otto Cycle and turbocharged. The jet's blueprint stays hidden until they're done.
  - **An assembly platform** to make it on (RimThunder, 200 steel, 15 components).
  - **Each turbine:** 3 heavy castings (made at a smelter, 100 steel each), 15 components, 1 advanced component and 180,000 ticks of work.
- **Construction skill 10** is now needed to build the jet, as for the Hind and Mongoose.
- **Bombs cost much more under Combat Extended.** Four Mk. 82s take 4 Mk. 82 bomb cases (1,120 steel), 80 FSX, 1,200 chemfuel and 8 components.
- **Bomb cases are a new step for bombs** under Combat Extended (since 0.6.0, in place of heavy castings): make a Mk. 82 or Mk. 84 bomb
  case from steel first, at the machining table, the rotary forger or an automated machining bay. **They stack to 4**, so a hopper
  slot holds four. **If your bays' hoppers were set to take heavy castings, allow the two bomb cases there instead.**
- **A sortie's map now stays open while the jet is over it.** Before, a map from Real Ruins, RimMarket and some other mods could close
  with the jet still in the air. See [Sorties](#sorties-what-the-code-says) for what a sortie needs from you.
- **Heavy castings at a rotary forger now come 12 at a time**, from 1,200 steel. **If you set up a heavy casting bill at a forger before
  version 0.5.0, delete it and add the new one**: the old one-at-a-time bill may keep running. The smelter and assembly platform still make one at a time.
- **Turret hulls you've already built must be rebuilt** to take ammo under Combat Extended. Deconstruct and rebuild each one, **while the ship is landed**: in space the gap lets the air out. See [Turret hulls under Combat Extended](#turret-hulls-under-combat-extended).
- **InterRim Ballistic Missile: a strategic blast no longer freezes the game, but on a big map it can still take many minutes.** In
  the owner's first try the blast was still crossing an enemy town after 14 minutes. Wait for the message that it has spent itself
  before saving; a blast still running when you save is forgotten on load. See [InterRim Ballistic Missile](#interrim-ballistic-missile).
- **Not yet tested in game,** except that one missile run. Everything else here is read from the mods' code and files.

## What it does

### The Harrier's build cost

The Harrier asked for 450 steel, 150 plasteel, 25 components, 6 advanced components and 1 engine block: no landing gear, tanks,
wiring or nozzles, and six far-future components in a 1980s jet.

| Part | Stock | Now | Why |
| --- | --- | --- | --- |
| Steel | 450 | **400** | Airframe |
| Plasteel | 150 | **120** | Stands in for the composites, about a quarter of the real airframe's weight |
| Components | 25 | **30** | Avionics; the same as the Hind |
| Advanced components | 6 | **0** | The turbine's own recipe still uses 1 |
| Engine | 1 engine block | **1 RimThunder turbine** | One real engine. Without RimThunder: 3 engine blocks, as on the Hind |
| Car exhausts | 0 | **4** | The real engine's four swivelling nozzles |
| Suspension and wheels | 0 | **4 and 4** | Landing gear |
| Fuel tanks | 0 | **3** | Fuselage and wings |
| Transmission | 0 | **1** | Accessory gearbox |
| Alternators and batteries | 0 | **2 and 2** | Electrics |
| Wiring | 0 | **6** | The same as the Hind |

The counts are gameplay choices. They hold up against the same author's helicopters, reading each part's value and weight from the
mods' own files:

| Vehicle | Value of its parts | Share of its market value | Weight of its parts | Its listed mass |
| --- | --- | --- | --- | --- |
| Harrier, stock | 4,530 | 32% | 299 kg | 700 |
| **Harrier, now** | **7,895** | **56%** | **658 kg** | **700** |
| Hind | 6,655 | 55% | 573 kg | 1,100 |
| Mongoose | 5,975 | 63% | 497 kg | 600 |

### Loading alongside RimPacts

With RimPacts active, the Harrier's flight framework stops loading partway through, so more than 50 of its patches go missing,
including those for boarding, refuelling, repair, landing and attack runs. Your log would show "Error in static constructor of
FixedWingAircraftFramework.HarmonyInit". This mod fixes that, and **RimPacts' rule against landing on some maps keeps working**. Your
log will show a line starting "[D'thorga Vehicle Patches] RimPacts' landing rule". If RimPacts changes how it does this, the fix finds
nothing to do and steps aside.

### Bombs under Combat Extended

Under Combat Extended bombs are items with their real weight, but a 907 kg bomb was made from about 70 kg of materials. The case now
weighs what it should. **The two fuel and explosive fills stop short of their real weight on purpose**, because matching it would take
far too long to make (174 FSX for one Mk. 82, 858 prometheum for one incendiary).

| Bomb | Stock | Now | Materials weigh |
| --- | --- | --- | --- |
| Mk. 82 (227 kg) | 60 steel, 45 chemfuel, 2 components | 1 Mk. 82 bomb case, 20 FSX, 300 chemfuel, 2 components | 166 kg |
| Mk. 84 incendiary (907 kg) | 120 steel, 120 chemfuel, 2 components | 1 Mk. 84 bomb case, 100 prometheum, 1,500 chemfuel, 2 components | 604 kg |
| Mk. 84 EMP | 120 steel, 6 components | 1 Mk. 84 bomb case, 840 steel, 12 components | 905 kg |
| Mk. 84 smoke | 120 steel, 50 chemfuel, 1 component | 1 Mk. 84 bomb case, 760 steel, 1,000 chemfuel, 1 component | 909 kg |
| Mk. 84 poison gas (Biotech) | 120 steel, 4 wastepacks, 1 component | 1 Mk. 84 bomb case, 72 wastepacks, 1 component | 911 kg |

Their selling price now follows from their ingredients.

#### Bomb cases

Each bomb starts as an empty steel case, made first and then filled. **This replaced RimThunder's heavy castings in 0.6.0**: castings
don't stack, so on an automated machining bay each one took a whole hopper slot, and the bays clogged on them.

| Case | Steel | Weighs | Work | Crafting skill | Stack |
| --- | --- | --- | --- | --- | --- |
| Mk. 82 bomb case | 280 | 140 kg | 1,000 | 6 | 4 |
| Mk. 84 bomb case (all four Mk. 84s) | 956 | 478 kg | 3,000 | 7 | 4 |

- **Where:** the machining table; the rotary forger, with The Dead Man's Switch - Expedition; and the automated machining bay, with
  Vanilla Furniture Expanded - Factory, which gives the bay a bill for each case by itself.
- **No research of their own.** Each bomb keeps its own research.
- The weight is the real case's, so the steel balances it. They can be smelted back to steel.
- **The picture is a stand-in**, drawn by hand in code until generated art is picked.
- Only under Combat Extended: without it the cases don't exist.
Without Combat Extended nothing here changes: the jet loads its bombs from steel. To rescale a bomb, change its numbers in
`Mods/HarrierVtol/Patches/Bombs_HarrierVtol.xml`.

### Bombs and rockets at the ammunition fabricator

With The Dead Man's Switch and Combat Extended, all five bombs and the jet's three kinds of 57 mm rocket are also bills at its **ammunition
fabricator**. The machining table still makes them too, and the cost and research are the same at both. Without those two mods this does nothing.

| Item | Machine time for one bill | Colonist's Crafting skill |
| --- | --- | --- |
| Mk. 82 | 2,250 ticks (0.9 in-game hours) | 6 |
| Any Mk. 84 | 3,000 ticks (1.2 in-game hours) | 7 |
| 5 HE rockets | 6,500 ticks (2.6 in-game hours) | 4 |
| 5 HEAT rockets | 4,300 ticks (1.7 in-game hours) | 4 |
| 5 AP rockets | 2,750 ticks (1.1 in-game hours) | 4 |

The Hind and Mongoose use the same three rocket recipes, so their rockets can be made there too.

That's half the work of the machining table, and no colonist stands at the machine while it runs. It's the fabricator's own speed, the same as for
all its ammo, and it draws 1,000 W while running and 50 W idle.

- **A colonist still has to load it and collect the result.** That's the Crafting job "Operate Machine", and each needs a colonist with the skill above.
- **Finished items drop beside the machine**, whatever the bill's storage setting says. Haulers take them from there.
- **Interrupting the colonist while loading drops what they had brought** beside the machine, and the job starts again. Nothing is lost, but the
  chemfuel-heavy bombs take many trips (1,500 chemfuel for an incendiary), so keep your chemfuel close.

### Drop Tanks

The upgrade costs **2 car fuel tanks, 20 steel and 2 components**, in place of 60 steel and 2 components.

### Heavy castings at the rotary forger

With RimThunder and The Dead Man's Switch - Expedition, the **rotary forger** makes heavy castings **12 at a time**. RimThunder's
in-game name for the item is "heavy compoment", spelt that way; the forger's bill is called "make 12 heavy castings". The electric smelter and
assembly platform still make one at a time.

- **Cost:** 1,200 steel makes 12 castings and 12 lumps of steel slag: RimThunder's own recipe times 12. It needs no research of its own and no skill.
- **A colonist with Crafting work enabled does it**, as the job "Operate Machine". Loading 1,200 steel takes them about 16 trips, because steel
  comes in stacks of 75.
- **It saves colonist time.** The colonist's fixed waits, about 600 ticks to load and collect, are shared by all 12 castings, against about 500 ticks
  of work for each casting at the smelter. The machine then runs about 3,000 ticks (just over an in-game hour) on its own.
- **The bill waits until 1,200 steel can be fetched.** Below that, use the smelter.
- **Finished items drop beside the machine**, whatever the bill's storage setting says: 24 separate things, since castings and slag don't stack.
  **Keep the floor around the forger clear**, and haulers take them from there.
- **Interrupting the colonist while loading drops what they had brought** beside the machine, and the job starts again. Nothing is lost.
- The machine draws 800 W while running and 50 W idle.

### A sortie's map stays open under the jet

The game removes a site's map once no colonist is left on it, and its check can't see a pilot inside a vehicle. Vehicle Framework
covers vehicles on the ground, and the Harrier's flight framework covers aircraft in the air, but only over the game's own kinds of
site. Over a Real Ruins site, a RimMarket convoy battle or any other mod's site with its own removal rule, the map could close with
the jet still flying. Now **any vehicle of yours with a colonist aboard keeps its map open**, in the air or on the ground, whichever
mod made the map. The Hind and Mongoose get this too while the Harrier is active.

### Sorties: what the code says

Read from the Harrier's flight framework, not seen in play. The Harrier mod does all of this; this mod only keeps the map open.

- **Sortie** (the button on the jet) opens the world map. Pick a site or settlement: an open map gives "fly to" and "land" choices;
  a closed one is entered from the map edge. **Your own home map is refused** as a sortie target; use Launch to fly home.
- **Fuel decides time on station, not distance.** World flight is cheap and quick: about 0.16 fuel and one minute a tile, so a
  30-tile sortie costs about 5 fuel each way. On a map the jet burns 0.0167 a tick, 250 fuel in about 6 hours. The sortie is refused
  unless the tank holds the trip there and back, one hour over the target and a 10% reserve.
- **It comes home by itself** when its rockets and bombs are spent or fuel runs low ("low fuel, returning"), to the map it took off from
  if that map still has a runway, else the nearest home map with one. **Home needs a full beacon runway**, 24 cells by 5 for the Harrier:
  four beacons on the corners of a rectangle, so 23 cells apart along it and at least 4 apart across. With no runway at home, the
  jet circles and says so.
- **Don't land on a sortie map.** The Harrier lands vertically anywhere, but it takes off only from a beacon runway, so a jet landed
  away from one stays there. A rough landing also wrecks its engine.
- **No sortie from under a roof.** The launch check refuses it; taxi out first.

### Turret hulls under Combat Extended

This part switches on only when both Spaceship Turret Hull and Combat Extended are active.

Before, the hulls stayed ordinary turrets while Combat Extended swapped four of their guns for ammo-using ones. Those four had no way to load, and the other three still used the game's own guns. Now all seven load like Combat Extended's own turrets: colonists haul the ammo over, and the turret gets the usual reload and ammo buttons. Colonists reach a hull from any open cell beside it, so keep one clear on the inside of the ship.

| Hull | Fires | Magazine | Range |
| --- | --- | --- | --- |
| Spaceship turret hull | 5.56x45mm NATO | 100 | 48 |
| Autocannon hull and heavy autocannon hull | 20x102mm NATO | 100 | 78 |
| 40mm flak hull (was the uranium slug hull) | 40x311mmR | 20 | 86 |
| Shotgun hull | 12 gauge | 20 | 19 |
| Sniper hull | .50 BMG | 10 | 84 |
| Incendiary hull | 30x64mm fuel cells | 10 | 45 |

**What else changes:**

- **The hulls no longer explode when destroyed**, as with Combat Extended's own turrets.
- **Aim and cooldown** use Combat Extended's figures for the nearest turret of each kind.
- **The autocannon and heavy autocannon hulls now fire the same gun the same way.** The heavy one still has more hit points.
- **The uranium slug hull is renamed**, because under Combat Extended its gun is a 40mm flak gun.

**What stays the same:** build costs, work, skill needed, hit points and power. These hulls are now far cheaper than Combat Extended's own turrets, which is a known gap.

### A log warning

The jet's fuel tank logged "Parsed 2.5 as int" at every start. Writing the value the game already used, 2, stops that and changes nothing else.

## InterRim Ballistic Missile

**Merged in from D'thorga Missile Patches 0.1.0 (7 November 2026). Partly tried in game; not working well enough yet.**

**What was wrong.** When a strategic warhead lands on a settlement whose map is open, the missile mod fades the screen to white and
then, in a single frame, damages every pawn, building, item and plant on that map. On the owner's heavily modded game that frame ran
for more than two minutes until Windows closed the game as not responding.

**What this does.** The blast is queued and crossed a slice per frame, from the centre of the map outward, within a time budget (a
setting). The game keeps drawing and stays responsive. While a slice runs, the game's room rebuild is held and done once at the end
of the slice instead of after each fallen wall. Every thing the warhead would have destroyed is still destroyed, by the missile
mod's own rules: the same damage, armour penetration, thick-rock-roof shelter, core-part hit on pawns, and dismantle paths. Things
already gone by the time the wave reaches them are skipped.

**What happened in play** (the owner, 7 November 2026, a DF-5 on an enemy town): **the game did not freeze**, and Windows never
closed it. **But the blast was still crossing the map 14 minutes later**, so it is far too slow on a big map. The likely reason is
that 12 ms a frame is a small share of a frame on a game with hundreds of mods, so the blast gets only a small part of the time;
the room rebuild was not the main cost after all. This is read from the log's timings, not measured. **Work on it is paused for the
owner's proposals on what to do about this mod.**

**Messages.** A blast that runs longer than two seconds says the blast is still tearing through the place, and says when it has spent
itself.

**Settings** (Mod settings, D'thorga Patches):

| Setting | Default | What it does |
| --- | --- | --- |
| Spread a strategic blast over several moments | On | Off gives you the missile mod's own one-frame blast. |
| Time each moment may spend on the blast | 12 ms | 2 to 50 ms. Higher finishes sooner and stutters more. |
| Rebuild rooms once per moment during a blast | On | Off if a blast ever leaves rooms behaving oddly; please report it. |

**Limits.** A blast in progress is not saved: what it had not reached survives a reload. The fires, terrain changes and cloud the
warhead makes still happen in the frame the screen reaches white, as the missile mod does them.

**Dev mode**, D'thorga category: **Missile Patches: strike current map** runs an extinction-class blast on the map you are looking at
(**it destroys everything there**; use a throwaway save), **log blast queue** and **drop queued blasts**. Each finished blast logs one
line: how many things it hit, in how many seconds and frames, and any errors it swallowed.

The record is [Missile patches](../../docs/development/missile-patches.md).

## Where to find things

- The jet's blueprint is under Architect, Vehicles.
- Bombs and rockets are bills at the machining table, as before, and at The Dead Man's Switch's ammunition fabricator.
- Bomb cases (Combat Extended only) are bills at the machining table, the rotary forger and the automated machining bay.
- Heavy castings are a bill at the electric smelter and the assembly platform, one at a time, and at the rotary forger, 12 at a time.
- The turbine is a bill at RimThunder's assembly platform.

## For modders

The map fix is in `src/PhobosPatches.Harrier/AirborneCrewKeepsMap.cs`: one postfix on `MapPawns.AnyPawnBlockingMapRemoval`, the
check every site's removal rule reads. The RimPacts fix is in `src/PhobosPatches.Harrier/RimPactsArrivalFilter.cs`. **A Harmony patch method in a generic class breaks the next
patch of the same method**, because Harmony stores a patch as a method token, which can't hold generic arguments. The record is in
[Vehicle patches](../../docs/development/vehicle-patches.md). The missile part is in `src/PhobosPatches.Irbm`: a
prefix on the missile mod's `StrategicImpactCountdown.ApplyStrategicDamage` that queues the pass, and a postfix on
`Root_Play.Update` that runs it.

## Checks done

- 7 November 2026: Pipe System's hopper intake and saved bill code read (1.7 build), and the owner's PG2 save read to see why the
  machining bays clogged. The patch was run offline against the Harrier's, Combat Extended's and the forger's real files, with and
  without the forger: every step matches. **Not yet tested in game.**
- 6 November 2026: the forger's start, finish and product placement code read again (Fortified, current 1.7 build), with the 1.7 game's
  placement code. `scripts/check-factory-patches.py --mod PhobosVehiclePatches` passes. **Not yet tested in game.**
- 5 November 2026: Spaceship Turret Hull's Defs and source, Combat Extended's turret class, reload job, shot and roof code, and the 1.7 game's save loader read. The patch was run offline against both mods' real files, the owner's PG22 save read to see what's built, and `scripts/check-factory-patches.py --mod PhobosVehiclePatches` passes. **Not yet tested in game.**
- 3 November 2026: the Harrier's shipped source, its bundled flight framework (build 19o), the VTOL Raids jet add-on, Vehicle Framework,
  Combat Extended and its Vehicle Framework assembly, and the 1.7 game's map-removal code read. Every game, Vehicle Framework, Harmony 3
  and Unity member the Harrier's three assemblies call exists in RimWorld 1.7, by name and parameter count (21 assembly pairs, 0
  missing). The owner's log of that day shows no aircraft errors. **Not yet tested in game.**
- 3 November 2026: RimThunder's recipe and the forger's Def read, and no other installed mod touches that recipe. `scripts/check-factory-patches.py --mod PhobosVehiclePatches`
  passes: the forger and the recipe exist among the owner's active mods. **Not yet tested in game.**
- 3 November 2026: the fabricator's bench, job and ingredient code read in the current Fortified framework (31 Oct build), with CE's ammo injector and
  the game's load steps. `scripts/check-factory-patches.py --mod PhobosVehiclePatches` passes: the fabricator, all five bombs and all three rocket recipes
  exist among the owner's active mods. **Not yet tested in game.**
- 2 November 2026: the Harrier, its bundled framework, Vehicle Framework, RimPacts, Harmony and RimThunder read in their files and
  decompiled assemblies, and the owner's log and newest save read. The assembly builds with no warnings for RimWorld 1.6, and the
  Harmony members it calls exist with the same signatures in Harmony 3.0.0-rc.3, which the owner's 1.7 game runs.
  `scripts/check-factory-patches.py --mod PhobosVehiclePatches` passes; it can't see the Mk. 82 change, which depends on another
  mod's patch, so that one was checked by reading. **Not yet tested in game.**
