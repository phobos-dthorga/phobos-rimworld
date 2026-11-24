# Fault-read of the untested work since 21 Nov (25 Nov 2026)

**Status (25 Nov 2026):** 51 faults and smaller points found by reading, across six areas; none fixed yet. The owner's choices on
fixing are recorded below once given.

The owner chose a fault-read as the next work (25 Nov 2026, question card: "Fault-read (Recommended)"), with 437 test rows waiting and
58 passed. Its scope is the gameplay code built since [the fault-read of 21 Nov](fault-read-2026-11-21.md): 171 code commits, D'thorga
Framework 0.203.0 to 0.283.0, Vehicles 0.91.0 to 0.118.0, Radio Framework 0.2.0 to 0.15.0 and Medical 0.21.0 to 0.26.1. Six independent
read-only readers, one per area, who had seen none of the work: (A) remote control, routes, mechlinked vehicles and vehicle radios;
(B) trailers, tanks and the food and water they carry; (C) camps, walk-in cabins and life aboard; (D) fire, glass, damage and jamming;
(E) engines, drivetrains and aircraft; (F) the reopened Radio Framework and D'thorga Medical's radiation work. Each read ours against
the game's code, Vehicle Framework's, Dubs Bad Hygiene's and Combat Extended's decompiles and the owner's patched Defs, with
`scripts/query-code.py` and `query-defs.py`. The owner's `Player.log` of 25 Nov 2026 (a short session loading PG22) was read with
`scripts/triage-log.py`: none of its errors points at our code. Nothing was built, run or edited.

**How sure:** **[checked]** means the session that commissioned the reads confirmed the finding again on both sides (our code and the
API it calls). The rest are the readers' own, with their confidence ("likely", "possible"). **None of this is observed in play.** Line
numbers are of commit `e91870f2`.

**Area A is the radio-autonomy session's newest work** (Framework 0.278.0 to 0.282.0), which may still be active.

**The owner's decisions (question cards, 25 Nov 2026):** **fix all now**, coordinating with the session working on a sortie fix; **area
A goes to the radio-autonomy session** to fix in its own flow (sent the same day); **D11: a drafted vehicle keeps its crew** while it
burns (the player's order wins, with its consequence); **C5: cabin riders eat and drink aboard on a map too**, as seated riders do.

## Read this first

- **Per-wheel brakes lose their wear on an older save (E1)** [checked]: on the first load after Framework 0.250.0, every per-wheel
  brake but the first comes back new, so the 0.273.3 promise ("each as worn as the single brake it had before") is not kept. The owner's
  save has about 44 vehicles.
- **A tank trailer's electric pump draws power while doing nothing (B1)** [checked; Framework 0.283.0, this session's own]: it asks
  for 750 W whenever its mode is not Hold, full, empty or loading; left on Load, a shower trailer drains a tractor's battery or keeps
  the grid paying.
- **A looping or following vehicle that runs dry posts "Out of fuel" once a second, and can never be refuelled at its stops (A1)**
  [checked].
- **Linked vehicles stop a mechanitor taking control of or resurrecting mechanoids (A3)** [checked], against "mechanoids first".
- **Carrying someone into or out of a walk-in cabin always fails (C1)** [checked]: the job ends as the patient is lifted.
- **A deck-tent sleeper loses their own bed at home (C3)** [checked].
- **Under Combat Extended, no blast cracks glass beyond its own radius (D1)** [checked]: Combat Extended's explosions never call the
  method our patch follows.
- **The radio coverage overlay paints a vehicle's radio, not a hand-held's (F1)** [checked]: about 13 dB too good.
- **Turning the radiation model off freezes anyone mid-course in their sickness for good (F2)** [checked].

## A. Remote control, routes, mechlinked vehicles, vehicle radios

| # | Finding | Where | How sure |
| --- | --- | --- | --- |
| A1 | A loop or convoy vehicle that cannot be drafted (out of fuel, set-up work, a stall, component damage) is asked to start every second: Vehicle Framework's draft setter refuses with a message each time. A loop vehicle is never undrafted, so it never refuels itself at a stop | `Remote/Route.cs:244` | **Fixed** (Framework 0.284.0): asked before each drive order; told once, waits undrafted, engine off at loop stops [A]; row 507 |
| A2 | With both the route kit and the radio remote kit fitted, the cover costs ask the first registered source (the route's), so a station-driven vehicle ignores radio cover | `Remote/RemoteCover.cs:66` | **Fixed** (Framework 0.284.0): the cover is asked of the source that drives it |
| A3 | The bandwidth postfix on `UsedBandwidth` reaches the game's `CanControlMech`, `HasBandwidthToResurrect` and the band-node warning, so linked vehicles block mechanoids; gestation (`UsedBandwidthFromSubjects`) is not limited, though the comment says it is | `Remote/MechLink.cs:300-307` | **Fixed** (Framework 0.284.0): vehicles counted only while the bandwidth meter draws; gestation was never limited, the comment corrected |
| A4 | A station-driven vehicle whose kit load is shed (flat battery, a short) or whose kit part is shot off stops at once with no lost-link choice, no letter and no reason; it relinks silently later | `Remote/RemoteControl.cs:153, 165, 304, 341` | **Fixed** (Framework 0.284.0, Radio 0.16.1): a kit carried but not working is a lost link, held at once with a letter; row 508 |
| A5 | A leader with followers can join another convoy: the four-follower limit is bypassed, and a ring of vehicles can keep itself linked and drive in circles | `Remote/Route.cs:157-176` | **Fixed** (Framework 0.284.0): its followers count against the limit and take the new leader |
| A6 | A follower in the middle of a convoy that is destroyed or leaves never re-points the one behind it (the re-point needs `Spawned`, false in `PostDeSpawn`), so two followers fight for one slot | `Remote/Route.cs:208, 441-445` | **Fixed** (Framework 0.284.0): the re-point no longer needs the follower spawned |
| A7 | A loop keeps its cells with no map: arriving on another map, it drives the old map's coordinates | `Remote/Route.cs:441-445` | **Fixed** (Framework 0.284.0): a loop keeps its map's ID and waits, unlinked, elsewhere |
| A8 | A mechlinked vehicle left behind when a map closes keeps its link and its bandwidth until the world's pawn clean-up, with no button to unlink | `Remote/MechLink.cs:264-269` | **Fixed** (Framework 0.284.0): a link whose vehicle is held by nothing on the world is let go every 250 ticks |

## B. Trailers, tanks, food and water

| # | Finding | Where | How sure |
| --- | --- | --- | --- |
| B1 | The trailer's electric pump asks for its 750 W (grid, power point or ~60 A from the tractor) whenever not on Hold, even full, empty, waiting, or loading (which needs no pump) | `Tanks/CompPhobosTank.cs:553` | [checked] |
| B2 | `TakeOut` drains a compartment without clearing it: its quality, ingredients and spoil clock stay, so clean water pumped in later stays "contaminated", and pumped out marks every tower on the colony's net contaminated | `Tanks/CompPhobosTank.cs:936-951` | [checked] |
| B3 | The Billy's paste tap trips the game's "Need food hopper" alert for as long as it stands: the alert looks only for a hopper beside each dispenser | `Tanks/CompPhobosReservoir.cs:118` | [checked] game side; VNPE side read in its 1.6 source |
| B4 | The shower trailer's working water (up to 150 L) is lost on a despawn other than destruction, and on packing up into a brim-full tank | `Vehicles.Hygiene/CompPhobosTrailerPlumbing.cs:246, 413-417` | certain / possible |
| B5 | The dump valve keeps running after its setting is turned off, and its Close button disappears | `Vehicles.Hygiene/TrailerWasteGizmos.cs:26` | certain |
| B6 | Refuelling from a towed bowser on the road drops the fraction of each take | `Service/CargoRefuel.cs:77` | certain; tiny |
| B7 | Food is fresh again when pumped into an empty tank or the paste tap; a packed tap's paste never ages; a reservoir under 1 unit never serves or empties | `CompPhobosTank.cs:663`, `CompPhobosReservoir.cs:50, 81` | certain |
| B8 | Meals cooked on the road record no ingredients, so meat and vegetarian precepts never see them | `SetOut/CaravanCooking.cs` | certain; low |

## C. Camps, walk-in cabins, life aboard

| # | Finding | Where | How sure |
| --- | --- | --- | --- |
| C1 | "Carry into the cabin" and "carry out" end as the patient is lifted: the despawned-target check is on the whole job, not the walk | `Cabins/CabinJobs.cs:28, 76` | [checked] |
| C2 | A hitched trailer's deck tent can be slept in; the tractor then drives off or forms a caravan, and the people in the tent stay on its pocket map unfed, holding the old map open | `Cabins/VehicleCabins.cs:178-183`, `Cabins/DeckTents.cs:116`, `Service/MoveRefusals.cs:15-24` | likely |
| C3 | A deck-tent or cabin-bunk sleeper claims the tent bed, which unclaims their own bed at home; when the tent closes they own no bed | `Cabins/DeckTents.cs:198-209` | [checked] game side |
| C4 | A vehicle that stops existing without being destroyed (killed while unspawned, sold or abandoned from a caravan, its map removed) strands its cabin's people on a pocket map that keeps running | `Cabins/CompPhobosVehicleCabin.cs:106-110` | possible |
| C5 | Walk-in cabin riders are fed only off the map, and get no water from the crew tank | `Cabins/VehicleCabins.cs:172-183`, `LifeAboard/LifeAboard.cs:95` | possible; a design gap |
| C6 | A caravan formed while someone sleeps in a roof tent or camper bed leaves that bed standing, and the next pitch makes a new one: a free bed each time | `Camp/CompPhobosCamp.cs:1138-1145` | certain; rare |
| C7 | A sleeping cab-bunk sleeper or caravan rider in bed still reads, snacks or smokes on life aboard's looks | `LifeAboard/LifeAboard.cs:83-108` | certain |
| C8 | Closing a cabin or tent pulls the player out of the world map view | `Cabins/VehicleCabins.cs:240`, `DeckTents.cs:136` | certain |
| C9 | One colonist can be made tent or bunk sleeper of several vehicles, kept by the last only; the sleepers button changes saved state while drawn | `Camp/CompPhobosCamp.cs:149-167, 289-299` | certain |
| C10 | The walk to a bunk re-checks the bedtime refusal every tick, a hostility scan and a reach test included | `Camp/BunkSleep.cs:160` | certain; small |

## D. Fire, glass, damage, jamming

| # | Finding | Where | How sure |
| --- | --- | --- | --- |
| D1 | Under Combat Extended no blast cracks glass beyond its radius: CE's `ExplosionCE.StartExplosionCE` never calls `Explosion.StartExplosion`, the method our postfix follows | `Service/GlassBlastPatch.cs:13-15` | [checked] |
| D2 | A zone a suppression system put out relights from the zone feeding it at the next look: its spread clock is left running | `Service/FireFighting.cs:257-273, 439-445` | [checked] |
| D3 | Tyres never smoulder after the extinguisher or a suppression system puts the fire out: only the burnt-out path calls `TyreFire.WentOut` | `Service/FireFighting.cs:268, 349` | [checked] |
| D4 | Without D'thorga Patches, fires left on the map a vehicle came from still count: its crew bails out every look, suppression fires at nothing, service and bay trips refuse, a later death leaves a hull marked burnt | `PhobosFramework/Environment/AttachedFires.cs:15-45` | [checked] |
| D5 | The jamming cache keeps every temporary map it was asked about in memory until the next load | `Service/JamFields.cs:41-43, 97` | likely |
| D6 | Cracked glass costs upgrade-tree lookups every tick a vehicle moves | `Service/Glass.cs:193-203` | certain; modest |
| D7 | The spall liner added to other authors' vehicles has its label and health written in code | `Service/Spall.cs:366` | certain |
| D8 | The load report's cook-off lines hang off the tyre-burst setting | `VehicleSystemsMod.cs:519-520` | certain |
| D9 | The jamming pod's seen-in-play note is written for a parked aircraft too | `Service/AirborneEvasion.cs:31-33` | certain |
| D10 | A shot-away extinguisher bracket reads as "empty" in the alert and inspect line | `Service/FireFighting.cs:425, 632` | certain |
| D11 | A crew ordered aboard to drive a burning vehicle out can be pulled out before it moves (a design call) | `Service/CrewBailOut.cs:72` | possible |

## E. Engines, drivetrains, aircraft

| # | Finding | Where | How sure |
| --- | --- | --- | --- |
| E1 | Per-wheel brakes on an older save: `PartsOnLoad` copies the old brake's wear, then `RestorePartHealths` with no saved entry restores full health over it | `Systems/CompPhobosVehicleSystems.cs:1008-1027`, `PhobosFramework/Rules/PartHealthRule.cs:18-22` | [checked] |
| E2 | The compression brake tests only the loadout's rated kilowatts, NaN on a class drivetrain, so heavy and tracked diesels on class drivetrains (other authors' trucks and tanks) never get one | `Systems/Powertrain.cs:104-107` | [checked] |
| E3 | A vehicle in the C-17's hold is weighed by Vehicle Framework's Mass (each author's own scale) as if kilograms, 12 to 50 times too light | `Service/HeldMass.cs:36` | likely |
| E4 | A drafted turbine aircraft must light its engines again after every load (the start sequence's state is not saved) | `Systems/EngineRoundTwo.cs:33-38` | certain |
| E5 | A starved engine still counts for one-engine-out ratings and flight support; descents regenerate only with load-dependent fuel on; a thrust craft loses a thrust fitting's factor | `Systems/Powertrain.cs:176-189, 389-397, 1053-1060` | possible |

## F. Radio Framework, Medical radiation

| # | Finding | Where | How sure |
| --- | --- | --- | --- |
| F1 | The coverage overlay's "hand-held kinds" include every VehicleDef given a vehicle radio at start-up (they are ThingDefs): coverage reads about 13 dB too good, and one kind per vehicle Def multiplies the work per cell | `PhobosRadio/Services/RadioCoverage.cs:386-397` | [checked] |
| F2 | With the radiation model turned off, anyone mid-course keeps their early sickness or illness for good (vomiting, a life-threatening stage) | `PhobosMedical/Radiation/Givers/HediffGiver_PhobosRadiation.cs:18` | [checked] |
| F3 | Every Rimatomics dose call (every 15 ticks per radioactive item, per pawn in range) runs a full refresh with allocations | `PhobosMedical/Radiation/Services/RimatomicsDose.cs:94-112` | possible |
| F4 | An unpowered desk still paints coverage; cells revealed from fog stay unpainted until another change | `RadioCoverage.cs:294, 352-383` | certain |
| F5 | The desk and amplifier marks do lookups every frame, against the cost table's "fields the rare tick keeps" | `PhobosRadio/Comps/CompRadioMarks.cs:63-133` | certain; cheap |
| F6 | The coax glow chains amplifiers; the link budget uses only the strongest | `PhobosRadio/UI/RadioCoaxView.cs:301-312` | certain |
| F7 | Two dBm labels are hard-coded; the waterfall's first frame is blank or wrong when opened paused | `PhobosRadio/UI/BudgetSteps.cs:261-263`, `ITab_RadioWaterfall.cs:37, 87-94` | certain |
| F8 | "Remove all radiation" leaves the late effects, medicines and dusty clothes | `PhobosMedical/Radiation/Debug/RadiationDebugActions.cs:92-106` | certain |
| F9 | Clothes show "dusty" with the radiation model off | `PhobosMedical/BurnsAndSmoke/Comps/CompPhobosContamination.cs` | certain |

## Read and found sound

Each reader listed what it read and found sound; in short: the remote control state machine, its saves and the operator's job; tank
to tank transfers and the waste tank's lay, take-up and priority; cabins opening, closing and evacuating on a map; camp pitching and
striking; the burnt-out hull, the turret queue save and other factions' fire; brakes per wheel apart from E1, descents, turbine
ratings, the service yard and the recipe-loop fix; the radio's coverage passes, link paths and feedlines; radiation's course maths,
saves and Biotech gates. Every Seen key in these areas has its evidence row, and every Keyed string used exists.
