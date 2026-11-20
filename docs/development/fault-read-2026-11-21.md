# Fault-read of D'thorga Farming, batches one and two (21 Nov 2026)

**Status (21 Nov 2026):** 66 faults and smaller points found by reading, three of them also seen in the owner's game log. None is fixed yet; what to fix first is the owner's choice. Not yet tested in game.

The owner asked for "an extensive bug and issue audit given the monstrous amount of code over the past two days" (20 Nov 2026), after
batches one and two of [D'thorga Farming](farming-batch-two.md) were built without a test between them. Five independent reads, one per
area, by fresh readers who had seen none of the work: the field run and row planning; bulk grain holds and pouring; the hitch as
extended for farming (couplings, the rigid linkage, lines of tows, the articulated tractor, draught); Farming's own C#; and Farming's
Defs, patches, text and pages. Each read ours against the game's code, Vehicle Framework's public source (SmashPhil, commit `026420b5`)
or its decompile, Combat Extended's source and the owner's Defs, with `scripts/query-code.py`, `query-defs.py` and `query-patches.py`.
The owner's `Player.log` of 20 Nov 2026 was read with `scripts/triage-log.py`. Nothing was built, run or edited.

**How sure:** every finding marked **[checked]** was confirmed again by the session that commissioned the reads, reading both sides on
21 Nov 2026. **[log]** means the owner's game log shows it. The rest are the readers' own confirmations by reading; "likely" and "a
question" are theirs. **None of this is observed in play.** Line numbers are of commit `7cb02143`.

**One claim withdrawn:** the commissioning session first told the owner the T400's rear half was "very likely buildable under
Architect > Structure". That was wrong: Vehicle Framework's implied build Def goes only into `DefDatabase<VehicleBuildDef>`
(`DefGenerator.AddImpliedDef`), and the Architect reads `DefDatabase<ThingDef>`, filled before implied Defs are made (D4).

## Read this first

- **The seed drill sows about one cell in four (F1).** Its working line is its own rear row, and the game refuses to plant in a cell a
  vehicle stands on. Row 277 could not pass.
- **The plough and cultivator drive every row twice (F2)**: an empty cell counts as work to do, so the tidy-up pass repeats the whole
  paddock. Double time and fuel, and the "ploughed" mark is given at half coverage.
- **A save and load in the middle of a tractor run breaks it (F3)**: the implement forgets its paddock; the tractor drives on working
  nothing.
- **Unhitching or losing an implement during a run starts an error loop (R1)**, and the Stop button goes with the implement.
- **Every tractor loses 40% of its engine to the combine's threshing drum while it works a row (D3).**
- **Nothing holds a machine to its row (R3).** A row is one drive order, and Vehicle Framework's cheapest path may leave the row for
  ground already cut. Likely, not certain: **the first thing to watch in a test.**
- **A silo ages its grain only when looked at (B1)**, at that moment's power and temperature for all the time since: a whole store lost
  at a click during a solar flare, or grain that never ages.
- **A machine destroyed with grain aboard loses it silently (B2).**
- **Wheat, barley, oats, rye and sorghum harvest as flour in the owner's game (F5)**, so the drill sows them from flour.
- **In the owner's log already:** the plough's empty body picture throws a red error at each start-up under Combat Extended (D1), and
  the T400's rear halves draw two warnings (D4).
- **No crash at load, no save that fails to load, and no Def that shows red without Combat Extended or dev mode.**
- **Test rows these findings change:** 272 (F7), 273 (R3, F4), 274 (R5, B4, B5), 276 (F2, R4), 277 (F1, F5), 280 (H4), 285 (H1, D5),
  291 (D3, D7), 293 (B3).

## Seen in the owner's log (20 Nov 2026, installed Farming 0.6.0 with most of batch two)

| # | What the log shows | Cause | Fix |
| --- | --- | --- | --- |
| D1 **[log] [checked]** | Error at each start: `Phobos_PloughSillion_PawnKind.lifeStages[0].bodyGraphicData`, "CropVertical error while cropping ... PloughBody_side", "no pixels with alpha" | The plough's body picture is empty on purpose (its two sides are overlays). Combat Extended's `BoundsInjector.ExtractBounds` measures every pawn kind's body picture and `Def_Extensions.CropVertical` throws on one with nothing in it. The game goes on (`GenThreading.ParallelForEach` catches per item); the plough gets no Combat Extended size | Give the body picture something Combat Extended can measure and the game does not draw, or a real part of the plough (its headstock) |
| D4 **[log] [checked]** | Two warnings: "[Phobos_TractorT400StandardRear] Implied generation for vehicles is incomplete" and the same for the B's | The rear halves name no `buildDef`, so `GeneratorVehicleBuildDef.TryGenerateImpliedDef` makes one with an empty cost list. Not in the Architect (above) | Give each rear half a `VehicleBuildDef` of its own with no `designationCategory` |
| — | Ten "Job chunk faulted" errors through `VehicleRoleHandler.DynamicDrawPhaseAt`, with our `SubmersibleDrawnAsShadow` prefix on the way | A pawn drawn in a vehicle's seat failed in the game's own draw code. No vehicle of ours draws its crew (`pawnRenderer` appears in none of our Defs), so it is another mod's vehicle | Not ours |

## The field run and row planning (R)

`src/PhobosFramework.Vehicles/Field/CompPhobosFieldRun.cs` unless said. R2, R6, R8 and R11 are the same faults as F3, F2, F4 and F14.

| # | Severity | Fault | Fix |
| --- | --- | --- | --- |
| R1 **[checked]** | Error loop | `AfterRow`, `CentreFor` and `NextRow` use `Worker!` (lines 405, 270, 342). Unhitch the implement or lose it mid-run and `Worker` is null: an exception every 250 ticks. The gizmo block returns early when the worker is null (line 751), so Stop is gone | End the run with a reason when the worker is gone; keep Stop while running |
| R3 **[checked: the mechanism]** | Wrong behaviour, likely | A row is one Goto to its far end (line 392). Vehicle Framework adds each thing's `pathCost` to a cell (`PathGridCalculator.ThingCost`), and crop plants have 14 against soil's 2, so the cheapest path may leave the row for the strip already cut. How far it strays is unmeasured | Drive a row in short hops, or check the body is still on the row's line as it moves, or give field machines `customThingCosts` of nought for plants |
| R4 **[checked]** | Wrong behaviour | Fix 3 of the first fault-read fits a row's start to the ground (`StartFor`), never its end: `BeginRow` drops the row when the far-end centre is off the map or unreachable (lines 386-396). A tractor must stand its implement's length beyond the last cell, so a paddock closed at both row ends is never worked | Walk back from the row's end to the last centre where the bodies fit, as `StartFor` does |
| R5 **[checked: the code path]** | Wrong behaviour | Fix 6 is incomplete: only the one nearest stand cell is tried (lines 496-499), and when `Broadside` finds it out of auger reach the bin is given up. From some sides of a chaser bin the gap is 3 cells against a reach of 2 (the reader's arithmetic) | Pass `Broadside` as the stand-cell search's own test |
| R7 | Silent failure | `End(Done)` runs whenever the sweep's index runs out, so rows that could not be started or stopped short are passed over: "Every row is done" with rows untouched | At the sweep's end, say so when rows still have work (needs F2 first) |
| R9 | Wrong behaviour, likely | `ImplementGap()` measures the distance between tractor and implement where they stand now, before the turn. An even-length footprint differs by a cell between facings, so alternate rows start a cell too far in; a 3-wide implement behind the 4-wide T400 may sit a cell beside its row | Work the gap out from the hitch's geometry for the row's own facing |
| R10 | Wrong behaviour, likely | A stand cell that is where the combine already stands arrives before the end facing is set, so it never turns side-on and gives the bin up. The gathering run guards this; the field run does not | Face it directly when it is already there |
| R12 | Error, narrow | `Rows[rowIndex]` has no bounds check; a paddock shrunk between a save and a load can put the index past the new plan | Bounds-check in `BeginRow` |
| R13 **[checked]** | Red error in dev mode | The rigid implements set `jackKnifeDegrees` 0 and `CompProperties_PhobosTrailer.ConfigErrors` refuses 0 or less (`Hitch/CompPhobosTrailer.cs:39`): three errors at each dev-mode start, for the plough and both cultivators | Allow 0 |
| R14 | Silent failure | A paddock deleted while the vehicle waits for a driver: the wait ends with nothing said | Say the field is gone |
| R15 | Text | "X has stopped working ." after the zone is gone; "...and then it will Harvest a paddock."; "row N of M" restarts in the sweep | Keep the label; a lower-case phrase for the order |

**Answered, from "Still unknown":** Vehicle Framework's `PositionChanged` does fire when our hitch moves a towed vehicle (its postfix
on `Thing.Position`'s setter, for any spawned vehicle).

## Bulk grain holds and pouring (B)

| # | Severity | Fault | Fix |
| --- | --- | --- | --- |
| B1 **[checked]** | Goods lost, or never spoil | A hold is aged only when selected, poured or received into (`CompPhobosBulkStore.CompTickRare` returns unless pouring), and `BulkContents.Age` applies the temperature and the store's factor of that moment to all the time since. The temperature half is a labelled simplification; the power half is not | Age at every rare tick, pouring or not |
| B2 **[checked]** | Goods lost, silent | `CompPhobosBulkHold` has no `PostDestroy`: a combine, chaser bin or drill destroyed with grain aboard spills nothing and says nothing. The field bin and silo spill and name the loss | One shared spill-and-say helper |
| B3 **[checked]** | Wrong behaviour; the owner's call | `BulkPour.Loose` takes any stack in `ResourcesRaw`: a silo beside a stockpile offers steel, silver and stone blocks, 60 tonnes of them in 16 cells. The Framework changelog does say "raw resources" | Take only what is harvested from a non-tree plant, plus Defs named as seed |
| B4 **[checked]** | Wrong behaviour | `CannotPour` asks the vehicle's own pather (line 95). The hitch keeps a towed vehicle's pather stopped, so a towed bin or drill pours, receives and loads while the tractor drives | Also refuse while the head of its line moves |
| B5 **[checked]** | Wrong behaviour | Fix 5 is incomplete: `FindHolds` skips only holds whose vehicle has a field run (line 472). The seed drill has a hold and no field run, so a full combine may fill a parked drill's hopper with the harvest | Skip any hold on a field worker, or flag the holds a harvest may go to |
| B6 | Wrong behaviour | Reach is measured from the game's footprint, a cell off Vehicle Framework's for an even-sided vehicle facing south or west (the combine, the drill) | Give `IBulkHold` a footprint; vehicles answer with `VehicleRect` |
| B7 | Wrong behaviour, low | Grain poured from the silo is placed from its centre cell, which has no region, so it can land beyond a wall; ground loading takes stacks through walls and in fog | Place from a standable cell beside it; skip fogged cells and other rooms |
| B8 **[checked: the pattern]** | Goods from nothing, rare | `GenPlace.TryPlaceThing` can merge part of a stack and then fail. The harvester's `Drop` then counts the whole stack as unplaced; the store's spill destroys the rest unnamed | Use the stack's own count after a failed place |
| B9 **[checked]** | Goods lost, tiny | `Implements.cs:439`: a short `Take` is thrown away and the cell not sown (the same as F12) | Check the count before taking |
| B10 | Wrong behaviour after a load | A pour whose target was destroyed before a save resumes onto the ground; `Start` never clears `loading` | Save an explicit mode; end a pour whose target is gone |
| B11 | Wrong behaviour, low | A destroyed store spills without ageing first; "has room" is asked before ageing | Settled by B1 |
| B12 | Cost | While a hold is selected, every frame: `ContentFinder.Get` calls, a scan of about 100 cells, LINQ and a list. `BaseMass` is an uncached stat read per plant cut | Cache icons, the ground scan and mass per Def |
| B13 | Cosmetic | A parked tractor's train weight line is stale after its hitched bin is filled | Reset the hitch in front's weight too |
| B14 | Text | "Within N cells" means two different things; leaving the map reads "Stopped by hand"; Farming's changelog says a destroyed silo "spills what it held" where it spills at most 40 heaps | One meaning; its own end line; reword the entry |
| B15 | Process | The design lists notes `farming.hold.pour` and `farming.silo.aerated`; neither exists | Add them with rows, or strike them |

**A question for the owner, not a fault:** deconstructing a full silo to move it destroys nearly all its grain (40 heaps at most are
spilled), said only afterwards. That is fix 7 as designed.

## The hitch as extended for farming (H)

H13 and H14 are R13 and D4.

| # | Severity | Fault | Fix |
| --- | --- | --- | --- |
| H1 **[checked]** | State lost | `Articulated.Assemble` counts any trailer on the front's hitch as its rear half (lines 65-70). Hitch a flatbed to a T400 front that is waiting for room and the rear half is never made, even after unhitching | Assembled only when the trailer is held by the permanent coupling |
| H2 **[checked: permanence by the towed vehicle alone]** | State stuck | `Couplings.Permanent` asks only the towed vehicle. An older truck, whose hitch names no coupling, can take an orphan rear half, and then neither offers Unhitch | Permanent only on a hitch that names the coupling |
| H3 **[checked]** | Wrong behaviour, likely | Vehicle Framework treats a vehicle with no movement role as needing no driver (`RecacheMovementPermissions`), and `CompPhobosTrailer` refuses neither draft nor move: a parked plough, drill or rear half can be drafted and driven with no engine. The older trailers share it | The trailer comp refuses draft and move, with the reason |
| H4 | Wrong behaviour | A hand-lowered implement stays lowered off the map: draught on the world map, gone after a load there; unhitching does not lift it | Lift on unhitch and on leaving the map |
| H5 | Wrong behaviour | The permanent pivot is let go by code: the depot window offers the rear half by itself, and a set-down with no room lets it go | Leave permanent pairs out of what can be left; keep the half held and retry |
| H6 **[checked]** | Wrong behaviour | `TowMoving` reads only the vehicle directly in front (line 378), which in a line is itself towed and never "moves": nothing behind a T400's rear half or behind a plough wears its tyres | Read the head of the line's pather |
| H7 **[checked]** | Silent failure | `Powertrain.ForecastTow` never passes draught to `TowRule.Modelled` (line 755), though the Framework changelog and the batch-two record say the forecast counts it. A T60 "pulls" a plough at full speed in the forecast, then crawls | The forecast sums working draught along the line |
| H8 | Wrong behaviour | Brake and clutch wear count the T400's own rear half as towed weight (about 1.8 times the wear, bare) | Take the driven weight off the towed figure there |
| H9 | Cost; a rare state fault | "No room" for the rear half is found by generating and spawning a whole vehicle and destroying it, every 250 ticks, with a message and sound each time and no check that it spawned | Test the footprint first; check `Spawned`; slow the message |
| H10 | Wrong behaviour, low | Vehicle Framework shifts a south- or west-facing even-sided vehicle by a cell as it spawns, so a set-down rear half or drill lands a cell off what was checked; the next follow step corrects it | Set the position after the spawn, as `TryPlace` does |
| H11 | Wrong information | Off the map only the first towed vehicle is listed on the fleet board and the world inspect line | Walk the whole line |
| H12 | Silent failure; a question | A T400 always counts as "towing": no bay trip, no winch button, and the winch refusal asks for an unhitch that cannot be done | Treat a permanent trailer as part of the vehicle |
| H15 | Text and state | A hitch in the middle of a line reads its own vehicle where the driver is meant: a cart can be coupled behind a plough while the tractor drives; the rear half's inspect line is wrong | Read the head of the line |

**Answered, from "Still unknown":** the rear half is made in `CompTick`, never in `PostSpawnSetup`; loops and a fourth in a line are
refused; a destroyed middle member frees both sides.

## Farming's own code (F)

`src/PhobosFarming`.

| # | Severity | Fault | Fix |
| --- | --- | --- | --- |
| F1 **[checked]** | Wrong behaviour, silent | The drill's `StripAhead` is `-(Size.z - 1)`, its own rear row (`Implements.cs:129`; `StripRule.Front`'s comment: "nought the body's own front line, minus the body's length the line just behind it"). `Sowable` refuses a cell holding anything that blocks planting (`ThingDef.BlocksPlanting`: any fillage above nought; implements have `fillPercent` 0.3), and the drill stands in those cells. Only the overhang is sown | Sow the line just behind the body (`-Size.z`) |
| F2 **[checked]** | Wrong behaviour | Tillage's `HasWork` is true for a cell with no plant (`Implements.cs:233`), so tilled rows still have work and the sweep drives them all again; `cellsWorked` doubles | Remember the cells worked this run |
| F3 **[checked]** | Silent failure after a load | The implement saves only `handLowered`; `BeginRun` is called once, at the start (`CompPhobosFieldRun.cs:239`). After a load `area` is null and `WorkCells` returns | Save the area and tallies, or hand the zone back after a load |
| F4 **[checked]** | Silent failure | The combine's `CannotWork` never asks the tank. With another product left in its single-product tank, `RoomFor` is false for every plant: it drives the paddock twice and cuts nothing | Refuse with "pour out the ... first" |
| F5 **[checked]** | Wrong behaviour | `VCE_Wheat` and `VCE_Oats` harvest `VCE_Flour` in the owner's game (`query-defs show`), and barley, rye and sorghum the same (the reader's). The cereal class has `harvestIsSeed` true, so the drill and hands sow from flour, and one cereal's flour sows another | A per-crop override, or a milled-cereal class |
| F6 **[checked]** | Goods lost | `LeaveSecondProducts` clears every second product when the straw chopper is fitted. Oats' second product is `VCE_RawOats`, 15 a plant: food, not straw | Chop only what is straw |
| F7 **[checked]** | Wrong behaviour | Patch two keeps paddock cells out of the scan's starting cells only. `WorkGiver_GrowerHarvest.JobOnCell` queues up to 40 cells around the first through `HasJobOnCell`, which knows nothing of paddocks: a colonist harvesting beside a paddock takes its ripe plants | Postfix `HasJobOnCell` instead; it also removes the inlining risk of the first fault-read's finding 10 |
| F8 | Wrong behaviour with Combat Extended, likely | With "Hands use seed too" on, a colonist with a Combat Extended loadout fetches seed and drops it before sowing, again and again | Tell Combat Extended's hold tracker about the seed |
| F9 | Cost | With the setting on and no seed in reach, every sowable cell costs a translated reason and sometimes a region search | Cache "no seed for this pawn this tick" |
| F10 | State lost | A paddock carried on a gravship arrives with "allow sowing" off and no mark: machines refuse it and hands do not sow it | Carry the mark with the zone, or restore sowing when the mark is missing |
| F11 | Design not built; stale text | The batch-two record says the plough lays Vanilla Plants Expanded's tilled soil; no code does. The Machine-worked tooltip still says "There is no seed drill yet" (D6) | Mark it not built, or build it; reword |
| F13 | Wrong behaviour, edge | The drill refuses a heated greenhouse paddock in winter where a hand would sow; the driver gets no "plants sown" record | Check the season per cell only; count the record |
| F14 | State in limbo | A combine leaving the map mid-row keeps its cut grain in a saved, unseen tally until its next run (fix 2 is incomplete); the code comment says otherwise | Settle on leaving the map |
| F15 | Cost, small | Per plant cut: an uncached stat, a thing made for the quest notice and never destroyed, a LINQ closure | Read once per line of cells |
| F16 | Goods from nothing, trivial | A crop with no harvest product costs no bought seed | A floor of seed per cell |

**Found sound:** the harvest steps match the game's `JobDriver_PlantWork`; all eight mods patching `Plant.PlantCollected` tolerate a
vehicle as harvester; "never a sown crop" holds (`sown` is saved by the game); every patch target and injected type is exact.
**What the plough does destroy, by design:** any plant not sown and not a tree, ambrosia and wild healroot included. Worth a line in the
guide.

## Farming's Defs, patches, text and pages (D)

D1 and D4 are above; D2 is R13; D6 is in F11.

| # | Severity | Fault | Fix |
| --- | --- | --- | --- |
| D3 **[checked]** | Wrong behaviour | `PhobosShaftLoad_Threshing` is available to any vehicle with a field run comp (`When_FieldWorkerFitted`) and on while a row is worked. Every tractor has that comp, so each loses 40% of its rated power while it ploughs or sows | Gate it on the harvester |
| D5 **[checked: the arithmetic]** | Wrong placement, likely | The T400 rear halves set `axleToCentre` 0.09 and 0.11. `HitchRule.CentreOf` puts the picture's middle that far ahead of the axle, so it is 1.51 behind the pivot where the export records measure 1.69 and 1.61: the halves overlap at the hinge | −0.09 and −0.11 |
| D7 | Text promises what no figure gives | The T60 F and T130 RC say they grip better on soft ground; they differ only in weight and price. The wide front says "a little more load on the engine"; the load is fixed | Give the figure, or reword |
| D8 | Page | The README asks for Framework "0.200.0 or later"; soft ground grip and loading from the ground are still under Unreleased there | Name the version once it has one |
| D9 | Pages behind the Defs | The About description covers batch one only; the README and guide name neither the grain storage nor the articulated tractors research, nor the fast auger; "Removing it" names only combines and bins | Bring the three pages up to batch two |
| D10 | Page | "Each drawn on the machine once fitted" is not true of liquid ballast, the tank extension, the straw chopper or the fast auger | Say which are drawn |
| D11 | Cosmetic | Mechanised harvesting and wide grain fronts sit in column 0 with what they need, so their lines run backwards on the research tab | One column right |
| D12 | Comment; a question | The Combat Extended patch's comment names the chaser bin; only the combine is patched. Batch two has no armour figures | Correct, or arm batch two by class |
| D13 | A question | A towed bin's or drill's auger costs no engine power from anyone, though its text says the tractor drives it | Charge it to the vehicle towing, or reword |
| D14 | Comment | "No upgrade tree yet" in three Defs; the Ludeon disclaimer run onto the README's last credits line | Tidy |

**Found sound:** all 91 texture, icon and sound paths; every class and field; no defName a thing can carry ends in a digit; the upgrade
trees, the seed's price, the trader patches, the crop patches, the language keys and every figure in the README's table.

## Not checked by anyone

- Anything in play.
- Smart Farming's four patches on sowing and harvesting: it has no decompile or public source on this machine.
- How far a machine strays from its row (R3), and how a 4-wide vehicle turns at a headland.
- Other mods' patches on rot and on `GenPlace`.
- Whether the installed Vehicle Framework build matches the source clone beyond the lines compared.
- Our Defs with every patch applied: the patched-Defs file predates batch two.
