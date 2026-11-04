# D'thorga Framework: research and decisions (4 Nov 2026)

The record behind [D'thorga Framework](../../mods/PhobosFramework/README.md). **Not yet tested in game.** The owner recorded
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
5. A gas mask (Biotech or CE) against the game's smoke: breathed smoke falls to a fifth; against carbon monoxide: no change.
6. Report the load line, any error naming `Phobos.Framework`, and TPS with and without a large fire.

## Results from play

- **5 Nov 2026, read from the owner's save PG19 (not a declared test).** On the enemy city map the room gas held carbon monoxide
  in 112 cells across several rooms, up to 0.995, and three raiders carried the poisoning (0.19 to 0.69); on the home map no room
  held gas and no colonist had been in smoke. So the fire source, the room model and the save round trip all ran. Reading the
  code against the save found the gas-mask fault fixed in 0.2.1: every pawn in a Combat Extended gas mask was counted as sealed.

## Supplied oxygen (0.2.0, 5 Nov 2026)

`PhobosBreathing` answers two questions from a pawn's conditions: is it on supplied oxygen, and does it have a condition oxygen helps. Two `DefModExtension`s mark those conditions, so [Burns and Smoke](burns-and-smoke.md) and [Medical Equipment](medical-equipment.md) need no dependency on each other. It is a treatment, not a gas, which keeps it clear of an air model (owner's gas rule).

## Open points and upstream

- Door and vent discovery on 1.7 reads correctly but is unverified in play (read from the 1.7 assembly, 5 Nov 2026): `Region.Neighbors` walks the
  region's links, door regions are `RegionType.Portal` with `Region.door` set, as the game's own `RoomTempTracker` assumes, and
  `Room.ContainedAndAdjacentThings` gathers each region's lister. That a vent set in a wall appears in the lister was not read. The fallback is a
  cached door and vent list kept by a comp on those Defs.
- Gravships: the game's smoke, CE's smoke and our room gas all stay on the old map when a ship moves.
- A `CeSmokeCarrier` when a mod must add smoke under Combat Extended.
- Further own gases where they earn it (chemfuel vapour, Rimatomics, Biological Warfare tie-ins), each argued against the rule.
