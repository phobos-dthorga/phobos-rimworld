# D'thorga Framework: research and decisions (4 Nov 2026)

The record behind [D'thorga Framework](../../mods/PhobosFramework/README.md). **Partly tested in game:** the owner reports the gas
layer and supplied oxygen working through Burns and Smoke and Medical Equipment (5 Nov 2026); the health layer (0.3.0) is not yet
tested in game. The owner recorded
the idea on 22 October 2026 ("an extendable Phobos framework of its own", [project direction](project-direction.md)); the
first consumer, [D'thorga Burns and Smoke](burns-and-smoke.md), made it concrete on 4 November 2026. The Ostranauts repository's
`PhobosFramework` is the companion: the same role, a different game.

## The owner's rules (4 Nov 2026)

- **Existing effects first, own gases when they earn it.** Infer effects from gases and functionality that already exist
  (vanilla or modded) where they are present; add a gas of our own when its value is greater than what inference could give,
  when nothing exists to infer from, or on the owner's call-out. Keep a **manual override** so the player or owner can force
  our own gases regardless.
- **Additive only.** Gases are brought into the environment at runtime; there is no air composition, oxygen, pressure or vacuum
  model. The owner named Atmosphere Overhaul's problems, outer space above all, as what to avoid.
- **Per room, like the game's temperature**, light and performant; the owner chose this over per-cell diffusion.
- **Absorb Combat Extended's black smoke rather than run a second smoke.** In practice (below) the framework reuses CE's smoke
  inhalation *condition* as the signal, which needs no reference to CE's assembly; a density carrier is deferred.
- **One shared framework** for the code every Phobos mod repeats, so documentation, code and art unify now. Existing mods adopt
  its helpers when next touched.

## What already carries a gas (read from code, 4 Nov 2026)

| Carrier | What it does | Where read |
| --- | --- | --- |
| The game's gas grid (`Verse.GasGrid`, `GasUtility`) | Four fixed gases, 0..255 per cell (blinding smoke, Biotech's tox gas, rot stink, Anomaly's deadlife dust); `AddGas` is public and overflows to neighbours; gas moves only through cells that are not full-fillage, so walls block it and doors pass it only while open; vents equalise it; each cell is visited for fading every ~64 ticks, losing 4 smoke per visit, halved under a roof, 25 times faster in vacuum; the grid ticks only once any gas has ever been added. Blinding smoke's only vanilla effects are a 0.7 aim factor and blocked line of sight; tox gas, rot stink and deadlife dust get health effects in `GasUtility.PawnGasEffectsTickInterval` every 50 ticks. The game's fires only throw smoke particles. | 1.7.4998 assembly decompiled to the console with ilspycmd; no 1.6 differences found |
| Combat Extended 16.7.4 (`CombatExtended.Smoke`, `Harmony_Fire_Tick`, `Harmony_GasUtility`) | Roofed fires raise `Gas_BlackSmoke` things (density 0..12800, +1800 per fire per 60 ticks) that spread cell to cell and leak outdoors; every 50 ticks a pawn in it gets `SmokeInhalation` (lethal at 1, self-clearing 0.15 a day, scaled by the `SmokeSensitivity` stat, which gas masks and power and recon helmets set to 0). `Smoke.GetOpacity()` and `UpdateDensityBy` are public. `Controller.settings` is a public static field and `Settings.SmokeEffects` a public property. | `.local/decompiled/CombatExtended/full`; CE's README names CC BY-NC-SA 4.0 |
| Vanilla Expanded Framework | No gas model; its gas-producer comps call the game's `AddGas`. | `.local/decompiled/VEF` |
| Atmosphere Overhaul | A full atmosphere with its own gases, per-cell heat, oxygen for fires, blast injuries. 1.6 only; off in the owner's game. | `.local/decompiled/AtmosphereOverhaul` |

The game keeps room temperature the way the framework now keeps room gas: `Room.Temperature`; doors call
`GenTemperature.EqualizeTemperaturesThroughBuilding(door, rate 1, one way)` every 34 ticks while open and 375 while closed
(`BuildingProperties.doorTempEqualizeIntervalOpen/Closed`, `doorTempEqualizeRate`); vents every rare tick at rate 14, two-way
(`Building_Vent.TickRare`); room state survives wall changes through `TemperatureVacuumCache`, a per-cell cache the room
updater averages into each new room, saved by `TemperatureVacuumSaveLoad`.

## Decisions

- **Reuse before making** (owner's rule). Smoke is never the framework's: Combat Extended's inhalation condition is the smoke
  signal while CE's smoke effects run, otherwise the game's blinding smoke at the pawn's cell through the pawn's toxic
  environment resistance or a gas-mask tag (`SignalRule`, `MaskRule`). Fires add the game's smoke under a roof only when CE's
  smoke is not in use (`MapComponent_PhobosFireSmoke`), so burning rooms are smoky on both paths. (Agent design, owner's rule.)
- **Carbon monoxide is the first own gas** (owner accepted the case): nothing in the game or CE carries it, and a gas that
  lingers in a shut room after the smoke has cleared, drifts through an open door and ignores a filter mask gives more than a
  number inferred from smoke. Fires add it on both paths.
- **The source mode is the override** (owner): a framework setting, Automatic, Always own, Never; a gas Def can pin itself
  (`alwaysOwn`) or name the existing kind it defers to (`preferExisting`). `SourceRule` is the one place that decides.
- **The engine copies the temperature design** (agent choice): one state per room, kept only for rooms that hold gas; a pass
  every 60 ticks; exchange through doors found by walking a room's regions to their portal neighbours and reading
  `Building_Door.Open`, and through vents found among the room's contained and adjacent things, at the game's own rates
  scaled to the pass (`GasMixRule.OpeningCoefficient`); transfer toward the amount-weighted mean, never past it, so repeated
  openings converge; openings onto the outdoors, doorways and vacuum are sinks; roof gaps vent their share at the gas's
  open-roof half-life; each gas fades at its own half-life; rooms in vacuum empty. A room that disappears or changes shape is
  spread over the rooms its cells now belong to, amount-weighted (the game's cell cache, done with the state's own cell list);
  saves hold cells, not room numbers, because rooms are renumbered on every load.
- **No reference to Combat Extended's assembly** (agent choice): the setting, the condition and the stat are read by name.
  A density carrier for CE's smoke (`GetOpacity`, `UpdateDensityBy`, in a tie-in assembly under `Mods/CombatExtended`) waits
  until a mod needs to add smoke while CE runs. CE is CC BY-NC-SA 4.0 by its README; referencing an installed DLL without
  copying would be recorded in the notices when that day comes.
- **Shared helpers** (owner): optional mods and Defs by name, the one-line load report, the display mode, a rules-Def base,
  the debug category, and a cleanup service for uninstalling. Existing mods adopt them when next touched (AGENTS.md).

## Performance notes

Rooms without gas cost nothing: the engine returns at once when its dictionary is empty. With gas, one pass per 60 ticks walks
only the rooms in the dictionary, their regions' portal neighbours and their adjacent things. The fire source walks the map's
fires once per 60 ticks and returns at once when there are none. The game's own gas grid, once any smoke has been added,
visits 1/64 of cells per tick for fading and 1/32 for spreading, as it does after any smoke grenade. Nothing here is measured yet.

## Test protocol (owner runs it)

1. Load with the framework on; read its load line (gas layer, Combat Extended smoke in use or not, own gases and source mode).
2. Dev mode, settings: switch on "Show gases at the mouse". Roof a 5x5 wooden room, spawn three fires, hover: carbon monoxide
   rising; with CE on, CE's black smoke as before; with CE's smoke effects off or CE absent, the game's smoke appears instead.
3. Put the fires out and shut the door: carbon monoxide stays for hours. Open the door: the next room gets some. Build a wall
   through the room: both halves keep it. Remove the roof: it fades in minutes.
4. Save and reload: the gas is still there. Switch the source mode to Never: fires add no carbon monoxide.
5. Without Combat Extended, a gas mask against the game's smoke: breathed smoke falls to a fifth; with Combat Extended, CE's own
   smoke inhalation is the signal and its masks stop it. Against carbon monoxide: no change either way.
6. Report the load line, any error naming `Phobos.Framework`, and TPS with and without a large fire.
7. With D'thorga Triage on, select a colonist: one "Doctors may operate" button. The dev action "Framework: log care choices of
   selected pawns" lists the group, care level and each choice as default or saved. Change it, save, reload: the choice is kept.

## Results from play

- **5 Nov 2026, read from the owner's save PG19 (not a declared test).** On the enemy city map the room gas held carbon monoxide
  in 112 cells across several rooms, up to 0.995, and three raiders carried the poisoning (0.19 to 0.69); on the home map no room
  held gas and no colonist had been in smoke. So the fire source, the room model and the save round trip all ran. Reading the
  code against the save found the gas-mask fault fixed in 0.2.1: every pawn in a Combat Extended gas mask was counted as sealed.
- **7 Nov 2026, the owner, on 0.3.1 then 0.4.1.** Selecting a dead colonist's corpse threw `InvalidCastException` (Corpse to Pawn)
  from the per-person care comp, in its gizmos and inspect text (read from `Player.log`). Display only: the colonist had died
  first. Fixed in 0.4.1; **the owner reports the error gone**. How the corpse came to carry the comp is not known (agent's guess:
  another mod gives corpses a race or copies comps); the fix doesn't depend on it.

## Supplied oxygen (0.2.0, 5 Nov 2026)

`PhobosBreathing` answers two questions from a pawn's conditions: is it on supplied oxygen, and does it have a condition oxygen helps. Two `DefModExtension`s mark those conditions, so [Burns and Smoke](burns-and-smoke.md) and [Medical Equipment](medical-equipment.md) need no dependency on each other. It is a treatment, not a gas, which keeps it clear of an air model (owner's gas rule).

## Health layer (0.3.0, 6 Nov 2026)

Built with [Triage](triage.md) as its first consumer and adopted by Emergency Dosing and Medical Equipment in the same change
(owner's direction, 6 Nov 2026: use the shareable framework even where other mods must change). `Patients` answers who the
doctors look after (the group a pawn belongs to, the player's care level); `ConditionDanger` and the game-free
`ConditionDangerRule` answer how dangerous a condition is and how soon it kills, taking the soonest of the death clock's mean,
the climb to a lethal severity and the game's bleed-out estimate (the mean of a death clock is an expectation, labelled so);
`Doctors` wraps the game's own "a doctor could come" test and finds the best surgeon for a recipe; `MapPatientCache` is the
per-map, rebuilt-when-asked list Dosing's work giver already used; `MedicalTreatment` and its lapsing comp moved in from
Medical Equipment with the comp's linger time now on the comp's properties rather than Medical Equipment's rules Def (same
value, 600 ticks). `CompPhobosPatient` carries per-person choices for any Phobos mod (`PatientPreferences.Register`), drawn as
one button per choice; it is put on every humanlike race by a startup sweep rather than an XML patch, because a patch on
`race/intelligence` misses Humanoid Alien Races and races that inherit it (agent choice, from More Injuries' separate HAR patch).

**Surgery bars (0.4.0, 7 Nov 2026):** `SurgeryBars.Register(Func<Pawn, string?>)` lets the mod that owns something stopping surgery (D'thorga Medical Equipment's pressure chamber) say so, with the reason already translated and in-world; `Patients.SurgeryBarred(pawn, out reason)` is what a mod that queues operations (D'thorga Triage) reads, so it can hold an operation and say why without a reference to the other mod. Asked for by the Triage session, 7 Nov 2026.

**Who counts as a patient (0.4.2, 7 Nov 2026):** `Patients.IsFleshPerson`, `IsLyingDown` and `IsLyingPatient` are the one shared answer; each mod adds its own need and care check on top. Medical Equipment uses them; Emergency Dosing's own `Downed || InBed()` adopts them when next touched. Prompted by a vehicle checked as a patient in the owner's game.

**Plumbing (0.5.0, 7 Nov 2026):** `PhobosFramework.Hygiene`, loaded only with Dubs Bad Hygiene, holds `PlumbingFeed`: the one reader of Dubs' water, moved from D'thorga Factory Additions. `TryPull(building, litres, out quality)` searches the cells under and beside a building; `TryPullFrom(net, litres, out quality)` takes a net the caller knows (a fixture's own pipe). Both answer flowing, no plumbing or empty towers. Factory Additions and Medical Equipment's electrolyser use it; D'thorga Water Tap's own reader adopts it when next touched.

## Merge helpers (0.6.0, 7 Nov 2026)

Made for the owner's decision to merge seven mods into two ([mod consolidation](mod-consolidation.md)), and used by both merged mods
in the same change. `SettingsImport` reads the settings file a mod's old folder and Mod class left behind, through the game's own
public `LoadedModManager.ReadModSettings`, and copies its fields into the object the game already handed the mod, so the game keeps
saving that one (`ModSettings.Mod` has an internal setter, so a read object cannot be handed to the game directly). It mirrors the
game's private file naming, `Config/Mod_<folder>_<class>.xml` (read from the 1.6 and 1.7 assemblies). `SettingsUi.Section` and
`SettingsScroll` give a long settings window headings and a scroll bar. `LoadReport` takes an optional part name.

**Considered and not built here** (agent's calls, for the owner): a frame-budgeted work queue (from D'thorga Patches' missile fix),
which waits for the owner's proposals on that mod; and a base class for interval map components, which would change the timing code of
parts that are tested in game, so it waits until one of them is next changed for its own sake.

## Work queue (0.7.0, 7 Nov 2026)

Made for D'thorga Patches' strategic blast, the owner's standing request that shared code live in the framework. `FrameWorkQueue`
runs `FrameWork` jobs one at a time, a slice per frame within the job's budget (`SliceBudgetRule`, game-free and tested), from
`GameComponent_PhobosFrameWork.GameComponentUpdate`: the game calls component updates every frame, paused or not (`Game.UpdatePlay`,
read from the 1.7 assembly), so the framework still needs no Harmony. A new or loaded game empties the queue; jobs are never saved.
`RoomRebuildHold` sets `RegionAndRoomUpdater`'s private `working` flag by reflection for a slice and rebuilds once at its end.
The candidate base class for interval map components is still not built (see 0.6.0 below).

## Shared helpers for the vehicle systems (0.15.0, 8 Nov 2026)

- **Generic functions go to the core** (owner's rule of 8 Nov 2026, in AGENTS.md): `Rules/CombatExtendedRules.cs` (how Combat
  Extended reads hit weights and blast penetration, with tests) and `Debug/DebugSelection.cs` (the selected things, comps and world
  objects for dev actions) came in with the vehicle systems' CE corrections and serve every Phobos mod. 0.16.0 added
  `Environment/HeldTemperature.cs` (a registry giving things held inside something a fixed temperature, as transport pods have; the
  one Harmony postfix stays in the vehicle part because the core has no Harmony) and `Items/ItemFinder.cs` (a tool in stock a pawn can
  fetch, or aboard a caravan). 0.17.0 added `Rules/History.cs` (a history ring and its save codec, with tests),
  `Persistence/ScribeHistory.cs`, `UI/PhobosWidgets.cs` (gauge, warning light, history graph, section, tachometer) and
  `UI/TabState.cs` (per-thing state for a shared inspector tab), built for the vehicle tabs and open to every Phobos tab. Their
  namespace is `Phobos.Framework.Interface`, not `.UI`: a namespace named `UI` would hide the game's `Verse.UI` class from every
  `Phobos.Framework.*` namespace. 0.18.0 added `Items/Charges.cs` (`CompPhobosCharges`, a thing that holds a few uses, with the
  `PhobosFramework_ChargesFull` recipe filter) and `Rules/ChargeRules.cs`, `Rules/ShelterComfort.cs` (how a pawn feels about a
  shelter's temperature against their comfort range), `WorldMap/BiomeGround.cs` (a world tile's ground from its biome's Defs;
  namespace `Phobos.Framework.WorldMap`, not `.World`, which would hide `Verse.World`) and a condition on `ItemFinder`'s finders.
  0.20.0 added `Patching/PatchFlags.cs`: `PatchFlags.Register` (a mod's setting, read by XML patches) and `PatchOperationPhobosFlag`
  (operations applied only while that setting is on; mods register in their constructors, which run before patches). 0.21.0 added
  `Health/LeavesLasting.cs` (a condition that leaves a lasting one on its part when it heals, by its worst) and `Rules/LastingRule.cs`;
  the uninstall sweep's `CleanupService.Sweeping` tells it to stand aside. 0.21.1 added `Rules/SupplyRules.cs` (several jobs' needs
  against stock); 0.23.0 `UI/PhobosTabs.cs` (opening a world inspector tab from code); 0.25.0 `Rules/DetailTierRule.cs` (Full, Lite
  or Off by faction and presence, tested) and `Debug/PhobosTiming.cs` (a dev-mode timing accumulator, free outside dev mode);
  0.27.0 `Debug/PatchedDefsDump.cs` (every Def with all patches applied, written once on a developer's request: a patch operation
  of ours keeps the game's combined XML document, which later mods go on patching, and a startup class writes it); 0.28.0
  `Logging/GameLogKeeper.cs` and `Logging/HostLog.cs` (the game's log turned back on after its limit; lines under our own engine
  category), `Rules/GameLogRules.cs` (message signatures, a bounded tally, the revival bound) and `Rules/TickThrottle.cs`, all tested.

## Open points and upstream

- Door and vent discovery on 1.7 reads correctly but is unverified in play (read from the 1.7 assembly, 5 Nov 2026): `Region.Neighbors` walks the
  region's links, door regions are `RegionType.Portal` with `Region.door` set, as the game's own `RoomTempTracker` assumes, and
  `Room.ContainedAndAdjacentThings` gathers each region's lister. That a vent set in a wall appears in the lister was not read. The fallback is a
  cached door and vent list kept by a comp on those Defs.
- Gravships: the game's smoke, CE's smoke and our room gas all stay on the old map when a ship moves.
- A `CeSmokeCarrier` when a mod must add smoke under Combat Extended.
- Further own gases where they earn it (chemfuel vapour, Rimatomics, Biological Warfare tie-ins), each argued against the rule.
- **A Vehicle-Framework-optional part for vehicle systems** (owner, 8 Nov 2026): the schema-driven electrical, cooling and
  lubrication service, loaded only with Vehicle Framework the way the Dubs Bad Hygiene tie-in is; designed in
  [vehicle systems](vehicle-systems-design.md), to be built after the owner's review. 1.7 only.

## Pipes beneath things (9 Nov 2026, Framework 0.57.0)

The owner's rule, choosing "joined, vehicles too": anything that takes water or piping has its own pipes beneath it, as Dubs' fixtures and Vanilla Expanded's connected buildings do, and a gravship does while it travels.

**Lets the player:** run a pipe to any side of a machine or a parked vehicle, and carry a run on through it. **Stops them:** nothing; the old way of touching a pipe still works. **Changes play:** a vehicle parked across a run bridges it, and driving off breaks that run again.

How it is built (agent's design): `PipeUnderlay` lays the piping mod's own hidden pipe on each cell of a footprint that has none. This replaced the first idea, giving buildings Dubs' own pipe comp, because that comp deletes the plumbing under a building, and on a load can meet a pipe that spawns after it and log an overlap. So nothing the player laid is ever touched, a save converts at its next look, and removing the mod leaves ordinary hidden pipe.

| Thing | When it joins | Limits |
| --- | --- | --- |
| Oxygen electrolyser, safety shower, conveyor oven | Always, with Dubs Bad Hygiene | Dubs' lite mode has no plumbing: nothing is laid |
| A player vehicle that takes water, or a tanker with Dubs water or septic ports | Parked, with plumbing under or against it | Looks about once every 33 game seconds (8 rare ticks), and when it stops; leaves as soon as it starts moving |
| A tanker or rig with a Vanilla Expanded Framework fluid | The same, for that fluid's net | A net with no hidden pipe lays its ordinary one, under the vehicle |

Not yet tested in game. In particular unproven: that spawning hidden pipes under a parked vehicle leaves Vehicle Framework's pathing alone, and that a gravship carries a building's or a vehicle's laid pipes as expected.

## Parts bills (11 Nov 2026, Framework 0.95.0)

The owner asked for every vehicle's build cost to draw on other mods' parts as the tanker's did, and then whether the mechanism could be generic for a later chemical industry mod's placed buildings. It is a core service: `Phobos.Framework.Bills` (`PhobosRoleSupplyDef`, `PhobosBillDef`, `PartsBills`) over the game-free `BuildBillRule`. The whole design, an example for a building and the limits are in [parts bills](parts-bills.md). Not yet tested in game.

## Node looks (12 Nov 2026, Framework 0.98.0)

**A vehicle's picture can change with its upgrades** (`CompProperties_PhobosNodeLooks`, `CompPhobosNodeLooks`, the rule `NodeLookRule` with
tests). Vehicle Framework's own node overlays can only add a picture on top; they cannot take a stock picture away, so a stock fan would
show under a heavy-duty one. A slot lists pictures in order: the first whose node is fitted shows, else the one with no node (the stock
look), else nothing. The comp changes the picture on Vehicle Framework's upgrade-completed and refund-completed events and at every spawn;
nothing is saved. First consumers: the generator set's fan (owner, 12 Nov 2026: "#1 is the default, #2 is the upgrade"), the tipper's plain,
rock and bulk bodies, and the drill rig's folded and tall masts. Pictures that move later (the mast swinging, the body sliding, the fan
spinning) are slots so a comp can move them. Not yet tested in game.

**A parked engine can be given a fan** (the same release): `Input_Moving` takes an optional `stoppedBonusStat`, added to its stopped
value and never past its moving value, and the cooling system's radiator names the new stat `Phobos_StoppedAirflow` (default 0, so every
vehicle cools as before). The generator set's heavy-duty fan gives 0.3, taking a parked radiator from 0.7 of its moving rate to the full
rate ([its record](vehicle-generator-set.md)).

## Progress bars, fading pictures and damage pictures (12 Nov 2026, Framework 0.100.0)

**Progress bars** (owner: "there needs to be progress bars in many areas, where appropriate"). A job uses the game's own
`WithProgressBar`. Vehicle Framework's `JobDriver_WorkVehicle` draws its bar only when the job has no effecter, and its effecter defaults to
the vehicle's repair effect, so its bar never showed: `WorkBars.WithBar` adds it to our drivers that extend it (service, diagnose, repair
with supplies, the sling hook-up), and those carry `IDrawsOwnWorkBar` so D'thorga Patches' `VehicleWorkBarPatch`, which does the same for
Vehicle Framework's own jobs, leaves them alone. Rig work reads `StepProgress` (the drill rig's portion, deepchem unit or side job, the
survey's way to a certain find). Work with no job, the tank's pump and the tipper's slide, draws `MapProgressBar` from the comp's PostDraw:
the game's bar is a mote that must be renewed every tick, which a rare-ticking comp cannot do, so this draws the same bar (0.68 by 0.12
cells, the game's colours, the closest zoom only) directly.

**Fading pictures** (`FadingOverlay`, `FadeRule` with tests): one long-lived mote over a building, made more or less see-through through the
colour the game multiplies a mote by, building at one rate and fading at another. Motes are never saved, so the owner snaps it to its
state after a load. First use: the liquid oxygen store's frost (D'thorga Medical 0.15.0), which DMS2 draws the same way, a frost picture
under the game's Mote shader; ours is our own picture, drawn in code.

**Damage pictures** (`CompProperties_PhobosDamageLooks`): pictures shown while a part is below a share of its health, checked every rare tick.
The bulk tanker's tank comp did this for its own tanks first and keeps its own (its scopes by tank kind); every other vehicle uses this.

**Extras on moving parts**: a set-up moving part's `extras` are pictures on its own canvas, shown while their node is fitted and posed with
the part. First use: the survey truck's geophone array and resistivity reel on its boom.

Not yet tested in game.

