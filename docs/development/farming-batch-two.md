# D'thorga Farming, batch two: the build design (20 Nov 2026)

**Status (20 Nov 2026):** a build design only: nothing of batch two is built, and nothing is tested in game. It turns
[farm machines](farming-machines.md) "Tillage and seeding (AG1)" and the owner's decisions of 19 and 20 Nov 2026 into the code, Defs,
settings and art a build would make. Written overnight on the owner's go ("proceed at your leisure and even agency", 20 Nov 2026):
**every choice below marked [A] or "agent's" is the agent's default, held for the owner's review**; the owner's own decisions are marked
with their date.

**How to read the marks:** **[code]** read in the game's or Vehicle Framework's code on 20 Nov 2026; **[I]** our inference; **[A]** an
authored figure or the agent's choice.

## Read this first

- **What batch two makes:** three tractors (small, medium, large, all at once: owner, 19 Nov 2026), the three-point linkage, a plough and
  a cultivator carried on it, a seed drill towed on the drawbar (owner, 20 Nov 2026), and seed.
- **The linkage is the riskiest piece.** The owner chose (20 Nov 2026) that a mounted implement stays its own vehicle, carried by the
  tractor. The agent's way to build it with the least new ground: **a rigid hitch.** Our hitch already moves a trailer vehicle behind a
  vehicle cell by cell [code]; the linkage is the same machinery with the implement held straight behind the tractor at a fixed short
  distance, turning with it, instead of swinging on a drawbar. The implement never shares a cell with the tractor, so Vehicle Framework
  sees two vehicles side by side, as it does with a trailer today.
- **Art blocks the vehicles' Defs, not the code.** No stand-in art for vehicles (the owner's rule), so the tractors', plough's,
  cultivator's and drill's Defs wait for their pictures; the code, rules, tests and the building pieces go first. **Pictures are paid and
  wait for the owner's go**, at high quality for fine detail (the owner's lesson of 20 Nov 2026).
- **It stands on batch one, untested.** A fault in the field run or the hitch shows here too. The owner chose (20 Nov 2026) to test after
  batch two, as one loop: till, sow, harvest.

## What batch two lets, stops and changes

| Behaviour | Lets | Stops | Changes |
| --- | --- | --- | --- |
| Tractors | Pull and carry implements, by size | A small tractor working a heavy plough at speed (draught) | Which tractor for which job is a real choice |
| Linkage | Carry a plough or cultivator, lifted at row ends | Working while lifted, turning while lowered | Implements follow tightly, no jack-knife |
| Plough | Turn the soil: clears weeds and stubble; tilled soil with Vanilla Plants Expanded | Ploughing a sown crop | The heaviest pass: the fuel and the tractor size matter |
| Cultivator | A lighter pass that clears weeds | As the plough | Cheaper when weeds are few |
| Seed drill | Sow a whole paddock with one order | Sowing without seed; sowing out of season or below the crop's skill | Hands are freed from sowing; seed must be planned |
| Seed | Keep grain back to sow, or buy seed | Endless free machine sowing | The harvest feeds the next season |

## The linkage (Framework)

- **A coupling, not a new kind of vehicle.** The tractor's hitch names `PhobosCoupling_ThreePoint` among its couplings; a plough names it
  as the coupling it is towed by (`PhobosCouplingsExtension`, which Framework 0.189.0 already has). Coupling one is the hitch's own order and job.
- **Rigid following** [A]: while coupled by the linkage, the implement's heading is the tractor's and its centre sits a fixed distance
  behind, from the implement's Def (`CompProperties_PhobosTrailer.hitchToAxle` read as the gap, `jackKnifeDegrees` 0). The hitch's
  follow rule (`HitchRule.Follow`) gains a rigid case, game-free and tested: the axle pose is placed, not dragged.
- **Lifted and lowered** [A]: an implement on the linkage is lowered only while its tractor drives a field run's row, and lifted
  otherwise: at the headland, on a road, parked. Lifted, it draws no draught and works no cells. A plain flag on the implement, read by
  its draught and its worker; nothing saved beyond it.
- **Turning room** [I]: a carried implement lengthens the tractor; the row planner already leaves turning to Vehicle Framework's own
  pathing, and a stuck turn ends a row as it does now.
- **A tractor carries one implement and tows one trailer?** [A] **One at a time in batch two**: the linkage and the drawbar are the same
  hitch, which holds one vehicle. Real tractors do both (a drill behind a power harrow); left for later.

## The field run with an implement behind (Framework)

- **The worker may be on the implement.** The field run looks for its `IVehicleFieldWorker` on the tractor first (the combine), then on
  the vehicle its hitch holds [A].
- **The strip is the implement's.** `StripRule.Front` takes the implement's own rectangle and facing; a trailing implement's strip is the
  line under its rear edge (`StripAhead` 0, measured backwards) [A]. The row's driving line is set so the implement, not the tractor, lies
  over the row: `CentreFor` adds the hitch's fixed offset behind the tractor [A].
- **The implement's position event** (Vehicle Framework's `PositionChanged`) fires for a towed vehicle only if our hitch's move raises it:
  **still unknown** ([batch one](farming-batch-one.md#still-unknown)). The run will watch the implement's cell itself on its own tick if the
  event does not come [A].
- **Draught** (already in Framework 0.189.0) is the implement's `IVehicleDraught`: its working width, by the Iowa State figures in
  [farm machines](farming-machines.md#draught-and-field-capacity), only while lowered.

## The implements (Farming)

| Implement | Coupling | Footprint [A] | Width [A] | Draught [A, from Iowa State's medium soil] | What it does to a cell |
| --- | --- | --- | --- | --- | --- |
| Plough | Three-point | 3 by 3 | 3 cells | 7.3 kN a metre, a cell taken as 1.5 m (agent's scale) | Clears weeds, stubble and wild plants; never a sown crop. Marks the cell ploughed; with Vanilla Plants Expanded, lays its tilled soil (`VCE_TilledSoil` [code]) on soil that can take it |
| Cultivator | Three-point | 3 by 3 | 4 cells | 4.4 kN a metre | Clears weeds and wild plants; never a sown crop. Marks it cultivated |
| Seed drill | Drawbar | 3 by 4 | 4 cells | 1.0 kN a metre | Sows the paddock's crop in empty cells, by the game's own checks |

- **Sowing follows the game's own steps** [code, `JobDriver_PlantSow`, `WorkGiver_GrowerSow.JobOnCell`]: the crop is the zone's;
  `GrowthSeasonNow`; light, roof and blocker checks; the crop's research and `sowMinSkill` against the driver's Plants skill; then
  `GenSpawn.Spawn` the plant, `sown = true`, growth just above nothing, and the game's "sowed a plant" history events with the driver.
- **Neither plough nor cultivator ever destroys a sown crop**, as the combine never cuts an unripe one.
- **The marks feed the soil rules later** (Field care: "ploughed", "cultivated"), saved per paddock in the paddock component. Until the
  soil rules exist, they show on the paddock's line and do nothing else [A].

## Seed (owner's decisions of 19 Nov 2026; the agent's way)

- **The drill carries seed in a bulk hold** (already in Framework 0.189.0), by weight, refilled by pouring.
- **Kept-back seed is the crop's own grain** [A]: pour a crop's grain into the drill and it sows that crop. The owner asked for seed
  "kept back from each harvest and cleaned"; the agent's way of cleaning is **a cleaning loss as grain goes in** (a tenth [A]), so no new
  item per crop is needed and any mod's crop works. A drill holding grain of another crop refuses that paddock and says so.
- **Generic seed** from traders sows any crop, at a higher price (owner): one item, `PhobosFarming_Seed` [A], with a trader stock rule.
- **How much a cell takes** [A, from the Birchip Cropping Group and Teagasc figures in the record]: about 3 % of the cell's harvest by
  weight, accumulated as a fraction, so a rice cell (6 units at 0.03 kg) takes about 5 grams.
- **Hand sowing stays free by default; a switch, "Hands use seed too", off** (owner). With it on, a colonist's sowing job takes the seed
  from a stockpile; without seed, the job is refused with the reason. A Harmony postfix on the sowing work giver's check [A, reason in
  code when built].

## Tractors (Farming)

| Tractor [A] | Footprint | Rated power | Empty mass | Couplings |
| --- | --- | --- | --- | --- |
| Small | 3 by 5 | 60 kW | 4 t | Drawbar, three-point |
| Medium | 3 by 6 | 130 kW | 7 t | Drawbar, pick-up hitch, three-point |
| Large | 4 by 7 | 300 kW | 15 t | Drawbar, pick-up hitch, three-point |

Figures authored [A], labelled approximate, in the ranges of real farm tractors (general knowledge, not read from a source). The
"needs about 110 kW; this tractor has 75" line comes from the tow forecast with draught, already in Framework 0.189.0.

## Settings (batch two, proposed)

| Setting | Default |
| --- | --- |
| Hands use seed too | Off (owner) |

## Art (all paid, waiting for the owner's go)

Three tractor bodies, the plough, cultivator and drill, the seed sack; one pilot first (the medium tractor), at high quality for fine
detail. Overlays: the implements lifted and lowered (a lifted one drawn shorter) [A].

## The order to build (agent's)

1. The rigid linkage in the hitch rule, with tests; the field run finding a worker behind; lifted and lowered. (Framework, code only.)
2. The three workers and the seed rule, with tests. (Farming, code only.)
3. Seed: the generic item and trader stock, the cleaning loss, the hand-sowing switch.
4. The Defs, once their pictures exist.

## Still unknown

- Whether Vehicle Framework's position event fires for a vehicle our hitch moves (also in batch one).
- How a carried implement looks while turning on Vehicle Framework's pathing: the tractor's turn is a few cells wide.
- Whether reading the driver's Plants skill for `sowMinSkill` matches every mod's sowing rules.
