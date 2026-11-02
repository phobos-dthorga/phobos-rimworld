# D'thorga Vehicle Patches

Patches for other authors' vehicle mods, each switched on only while that mod is active. It lets RimThunder's heavy casting be made at
the rotary forger, and covers **Harrier VTOL Attack Aircraft** by potemkinvillager: a realistic build cost, a fix for the jet failing to load fully alongside RimPacts,
honest bomb costs under Combat Extended, bombs and rockets at The Dead Man's Switch's ammunition fabricator, and fuel tanks for its
Drop Tanks upgrade. It copies nothing from those mods.

Version 0.3.0, built 3 November 2026 for House D'thorga. **Not yet tested in game.**

## Read this first (gotchas)

- **Load it after the mods it patches.** The About file asks for this, so RimSort does it for you. **You have to switch it on
  yourself**: nothing else requires it. It needs nothing else to run, and does nothing without the mods it patches.
- **Safe to add to a running save.** A jet you already built keeps working; only new builds pay the new cost. Bombs already made
  stay as they are.
- **Safe to remove.** Costs go back to the Harrier's own, and with RimPacts active the Harrier's framework fails to load fully again.
  **Delete any bomb or rocket bills you added at an ammunition fabricator, and any heavy casting bills at a rotary forger, first**, because they may
  no longer list those recipes.
- **The jet now needs RimThunder's turbine, if RimThunder is active.** It's a long job:
  - **Research:** engine making, Otto Cycle and turbocharged. The jet's blueprint stays hidden until they're done.
  - **An assembly platform** to make it on (RimThunder, 200 steel, 15 components).
  - **Each turbine:** 3 heavy castings (made at a smelter, 100 steel each), 15 components, 1 advanced component and 180,000 ticks of work.
- **Construction skill 10** is now needed to build the jet, as for the Hind and Mongoose.
- **Bombs cost much more under Combat Extended.** Four Mk. 82s take 8 heavy castings, 80 FSX, 1,200 chemfuel and 8 components.
- **Heavy castings are a new step for bombs too**, if RimThunder is active. Without RimThunder, bomb cases cost steel instead.
- **Not yet tested in game.** Everything here is read from the mods' code and files.

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

| Bomb | Stock | Now (with RimThunder) | Materials weigh |
| --- | --- | --- | --- |
| Mk. 82 (227 kg) | 60 steel, 45 chemfuel, 2 components | 2 heavy castings, 20 FSX, 300 chemfuel, 2 components | 176 kg |
| Mk. 84 incendiary (907 kg) | 120 steel, 120 chemfuel, 2 components | 6 heavy castings, 100 prometheum, 1,500 chemfuel, 2 components | 576 kg |
| Mk. 84 EMP | 120 steel, 6 components | 6 heavy castings, 840 steel, 12 components | 877 kg |
| Mk. 84 smoke | 120 steel, 50 chemfuel, 1 component | 6 heavy castings, 760 steel, 1,000 chemfuel, 1 component | 881 kg |
| Mk. 84 poison gas (Biotech) | 120 steel, 4 wastepacks, 1 component | 6 heavy castings, 72 wastepacks, 1 component | 883 kg |

Without RimThunder, each 2 heavy castings become 280 steel and each 6 become 956. Their selling price now follows from their ingredients.
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

With RimThunder and The Dead Man's Switch - Expedition, the heavy casting is also a bill at the **rotary forger**. RimThunder's in-game name for it is
"heavy compoment", spelt that way. The electric smelter and assembly platform still make it too.

- **Same recipe:** 100 steel makes 1 casting and 1 lump of steel slag. It needs no research of its own and no skill.
- **A colonist with Crafting work enabled does it**, as the job "Operate Machine".
- **It saves no colonist time.** The machine's part takes only 250 ticks, but loading and collecting take a colonist about 600 ticks in all, against about 500 at
  the smelter. It's useful for keeping smelters free or for colonists who do crafting, not for speed.
- **Finished items drop beside the machine**, whatever the bill's storage setting says. Haulers take them from there.
- The machine draws 800 W while running and 50 W idle.

### A log warning

The jet's fuel tank logged "Parsed 2.5 as int" at every start. Writing the value the game already used, 2, stops that and changes nothing else.

## Where to find things

- The jet's blueprint is under Architect, Vehicles.
- Bombs and rockets are bills at the machining table, as before, and at The Dead Man's Switch's ammunition fabricator.
- Heavy castings are a bill at the electric smelter, the assembly platform and the rotary forger.
- The turbine is a bill at RimThunder's assembly platform.

## For modders

The RimPacts fix is in `src/PhobosVehiclePatches/RimPactsArrivalFilter.cs`. **A Harmony patch method in a generic class breaks the next
patch of the same method**, because Harmony stores a patch as a method token, which can't hold generic arguments. The record is in
[Vehicle patches](../../docs/development/vehicle-patches.md).

## Checks done

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
