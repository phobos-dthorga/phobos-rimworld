# Hotboxing: smoking in a vehicle's cab (12 Nov 2026)

**A design record. Nothing is built, and nothing here is tested in game.** The owner, 12 November 2026: "having pawns do drugs in
vehicles ... When it comes to smokeleaf, you could have them hotbox and you could even have smoke FX come out near the windows." The
owner's answers the same day: write it up before anything is built, and **a stoned driver drives worse, with real consequences:
slower, with a little extra wear from rough driving, and no random crashes.** Facts marked "from the code" were read on 12 Nov 2026
with `scripts/query-code.py` and `scripts/query-defs.py` and are summarised in our own words. Every figure is authored unless a
source is named.

## Read this first

- **What it is:** a colonist riding in one of our vehicles can smoke a drug they carry or the vehicle carries, when their drug policy
  allows it. In a **closed cab** the smoke stays in, and **everyone aboard gets some of the high**, whether they chose it or not.
  An open cab, or a cab with its windows down, lets most of it out.
- **Where it works today, honestly:** **in a vehicle caravan** the game already lets passengers take drugs on their schedule (from
  the code, below), so the caravan half is mostly ours to add. **On a map, nobody aboard takes drugs at all**: passengers are not on
  the map, and taking a drug is a job only a pawn on the map can do. The on-map half needs a small service of our own that does what
  the caravan code does. Neither needs a Harmony patch on the game's drug code.
- **The driver:** a driver who is high drives slower, and a little harder on the tyres and brakes. **No random crashes** (owner).
- **Everything is a setting**, as the sling's flavour is, and one master switch turns hotboxing off entirely.
- **The science is real but small:** non-smokers in an unventilated room with cannabis smokers felt mildly sedated and showed
  cannabinoids in their blood; with ventilation they did not (Herrmann, Cone and colleagues, 2015, below). Our numbers are a game's
  simplification of that.

## What the player does

1. **Nothing new to build.** Give a colonist a drug policy that allows smokeleaf (on a schedule, or for joy), and put smokeleaf in
   their inventory or the vehicle's cargo.
2. **Drive or travel.** On a long caravan trip, or parked on a map with people aboard, a passenger whose policy says so lights up.
3. **Watch the cab.** In a closed cab the others aboard get a smaller dose and a memory: "Hotboxed" (a good one for most, a bad one
   for someone whose beliefs forbid drugs). On the map, wisps of smoke drift from the vehicle's windows while it lasts.
4. **Mind the driver.** If the driver is high, the vehicle is held to a lower speed and wears its tyres and brakes a little faster;
   the Dashboard and the inspect panel say why in words ("The driver is stoned: driving carefully slowly").
5. **To stop it:** change the drug policy, open the windows (a switch on closed-cab vehicles), keep teetotalers out of that vehicle,
   or turn the setting off.

## Lets, stops, changes

| Behaviour | Lets the player | Stops the player | Changes how they play |
| --- | --- | --- | --- |
| **Passengers smoke aboard** | Give colonists their recreation on a long trip, with drugs they already allow | Nothing new: the drug policy already decides | A caravan's drug stock becomes part of packing for a trip |
| **Secondhand smoke in a closed cab** | Share one joint's comfort among everyone aboard | Keeping a non-smoker clear-headed in the same closed cab | Who rides with whom matters; a teetotaler belongs in another vehicle or near an open window |
| **Windows down** | Ventilate the cab: the smoker keeps the high, the others barely notice | Getting the shared high and fresh air at once | A cold-weather trade: windows down lets heat out (the cab system already models it) |
| **The "Hotboxed" memory** | A small mood lift for most passengers | A teetotaler or someone whose ideology forbids drugs takes a mood hit instead | Ideology and traits shape who should ride together |
| **A stoned driver** | Nothing | Driving at full speed while high | Pick a sober driver, or accept a slower, rougher run |
| **Smoke at the windows** | See at a glance which vehicle is hotboxed | Nothing | None: it is a picture that makes the rest legible |

## The rules (all authored unless a source is named)

### Who lights up, and when

- **In a caravan (from the code):** the game already runs every caravan member's scheduled drugs every 120 ticks
  (`CaravanDrugPolicyUtility.CheckTakeScheduledDrugs`, `RimWorld-1.7:RimWorld.Planet.Caravan.cs:442`), taking the drug from the
  caravan's inventory (`Caravan_NeedsTracker.IngestDrug`, `RimWorld-1.7:RimWorld.Planet.Caravan_NeedsTracker.cs:243-256`). Vehicle
  Framework makes passengers caravan members: it replaces `Caravan.PawnsListForReading` with its list of everyone, passengers
  included (`Patch_CaravanHandling.AllPawnsAndVehiclePassengers`, `VehicleFramework-1.6:Vehicles.decompiled.cs:44482-44486`), and
  its caravan does not override the caravan's interval tick (`VehicleCaravan.Tick`, `:87934-87955`). So **a passenger in a vehicle
  caravan already smokes on schedule.** What the game lacks is smoking **for joy** on a trip: a caravan's joy comes from abstract joy
  kinds (`Caravan_NeedsTracker.TrySatisfyJoyNeed`, `:258-276`), never from a drug. We would add a joy check of our own: a passenger
  whose joy is low and whose policy allows the drug for joy lights up, as the game's `JoyGiver_TakeDrug` would at home (authored:
  at most once every few in-game hours each).
- **On a map (from the code):** passengers are held in the vehicle's role handlers and ticked from there
  (`VehiclePawn.TickHandlers`, `:17282-17288`, through `VehicleRoleHandler.DoTick`, `:17945-17948`). An unspawned pawn's needs and
  health still tick (`Pawn.TickInterval`, `RimWorld-1.7:Verse.Pawn.cs:1626-1676`), so the high wears off and joy falls, but **taking a
  drug is a job**, and a pawn off the map starts no jobs. So on a map, **nobody aboard smokes today.** Ours: on the vehicle's rare
  tick, the same check as the caravan's (scheduled drugs, then joy), taking the drug from the passenger's inventory or the cargo and
  ingesting it the way the caravan code does (`Thing.Ingested`), which runs the game's own outcome doers, tolerance and addiction.
- **Which drugs:** found by their effect, never by a list of names (owner's rule): a drug whose ingestion is **smoking**, read from
  its `ingestible.ingestSound` being the game's smoke sound (`Ingest_Smoke`; from the Defs: smokeleaf joints and flake use it, yayo is
  snorted and go-juice injected). Other mods' smoked drugs join by the same test.
- **Who counts as a smoker for the memory:** the one who lit up. Everyone else aboard in the same closed cab is a bystander.

### The secondhand dose

- **The source (verified 12 Nov 2026):** Herrmann ES, Cone EJ, Mitchell JM, Bigelow GE, LoDico C, Flegel R, Vandrey R. "Non-smoker
  exposure to secondhand cannabis smoke II: Effect of room ventilation on the physiological, subjective, and behavioral/cognitive
  effects." *Drug and Alcohol Dependence* 151 (2015): 194-202, doi:10.1016/j.drugalcdep.2015.03.019
  ([PubMed 25957157](https://pubmed.ncbi.nlm.nih.gov/25957157/), [PMC4747424](https://pmc.ncbi.nlm.nih.gov/articles/PMC4747424/);
  Johns Hopkins Medicine; a [Johns Hopkins summary](https://hub.jhu.edu/2015/05/15/secondhand-marijuana-smoke-study/)). In our
  words, from the abstract: non-smokers sat for an hour with smokers in a small chamber. **Unventilated**, they had cannabinoids in
  their blood and urine, a slightly raised heart rate and mild to moderate sedative effects ("pleasant", more tired, less alert).
  **With ordinary ventilation** (11 air changes an hour) their blood levels were much lower, with no sedative effect, no impairment
  and no positive urine screen. The full text was not read; the record relies on the abstract.
- **Our rule (a simplification, authored):** each bystander in a **closed cab with windows up** gets the smoker's dose times a
  **bystander share** (0.3, authored), so a joint (severity 0.5 of the high, from the Def) gives each bystander 0.15. **Windows down,
  or an open cab:** a share of 0.05 (authored), a trace with no effect worth showing, in the spirit of the ventilated sessions. The
  game's own high hediff is used, with its consciousness and moving penalties (from the Def: `SmokeleafHigh`, consciousness -0.3,
  moving -0.1, fading at 1 a day), scaled by severity as the game scales it.
- **No tolerance or addiction from secondhand smoke** (authored): the study found no meaningful dose, and making bystanders addicted
  would be a punishment out of proportion.
- **Per-vehicle, not a room gas.** The Framework's `PhobosGases` model works per room on a map, and a cab is not a map room. A cab
  needs only one number, the smoke still aboard, kept on the vehicle and not saved (authored to clear within an in-game hour,
  faster with windows down). Cheaper and simpler than a gas; the record's choice.

### The "Hotboxed" memory

- **Bystanders** get "Hotboxed": a small good memory (+3 mood for a day, authored), **or** a bad one ("Hotboxed against my will", -5,
  authored) for a pawn the game calls a teetotaler: their ideology forbids recreational drugs, or they are unwilling to take one
  (from the code: `PawnUtility.IsTeetotaler` and `HistoryEventDefOf.IngestedRecreationalDrug`, the same tests the game uses before
  offering a drug). A pawn with the game's chemical-fascination traits counts as a willing bystander.
- **The smoker** gets the game's own memories and ideology events from ingesting, as at home (the game's `CompDrug.PostIngested`
  records the event). Nothing is added for them.

### The stoned driver

- **Owner's call:** slower and a little more wear, **no crashes**.
- **Our rule:** the driver's **consciousness** sets a **speed cap** on the vehicle: full speed at 0.9 or more, falling to 0.7 of its
  speed at 0.6 consciousness and below (authored; a joint alone takes consciousness from 1.0 to 0.7, giving about 0.8 of speed). It
  joins the caps our systems already multiply together (`CompPhobosVehicleSystems.SpeedCap`, which feeds the move-speed postfix
  `SpeedCapPatch` and the caravan's `CaravanSpeedCapPatch` in `src/PhobosFramework.Vehicles/Service/Patches.cs`). **Wear:** tyre and
  brake wear times 1.1 while the driver is below 0.9 (authored), through the same wear factors the tyre fittings use.
- **Why it suits:** NHTSA summarises driving studies under cannabis as showing more weaving, slower reactions, longer following
  distances and worse divided attention, with some studies finding no effect
  ([NHTSA, Drug-impaired driving](https://www.nhtsa.gov/node/138601); its 2017 report to Congress was not read). Drivers who slow
  down and leave more room is close to what we model. A secondary account of an NHTSA crash-risk study found no increase in crash
  risk once other factors were controlled (unverified; not relied on). It fits the owner's "no crashes" all the same.
- **Today's code reads nothing about the driver** (from our code: no capacity or skill is read in the drivetrain); this would be the
  first. Vehicle Framework itself only refuses a driver who lacks consciousness or manipulation entirely
  (`VehicleRoleHandler.CanOperateRole`, `:18000-18004`) and sorts drivers by a capacity (`RoleAssignment.Resort`, `:11566`).
- **The text:** Plain: "The driver is stoned: driving slowly." Operator adds the share of speed kept. In-world words only.

### Smoke at the windows

- While a cab holds smoke **on a map**, a small smoke puff rises at the vehicle's cab cells every few seconds (the game's own smoke
  fleck through `FleckMaker`, as our claim order already throws puffs; authored: one puff every 2 to 4 seconds, more with windows down,
  for as long as the smoke lasts). Drawn only for vehicles on the map you are looking at, and only while smoke is aboard.
- The cab cells come from the loadout's cabin role where one exists (the Cherokee's upholstery, the litter kit's cabin), otherwise
  the middle of the vehicle's front half.

## Windows

- **Closed-cab vehicles get a "Windows" switch** (down or up; up by default), with its own icon. Down: smoke clears fast and the
  bystander share drops to the open-cab figure; the cab also exchanges heat with the air faster (the cab system's existing flow, a
  factor authored).
- **Open cabs** (`PhobosVehicleCabinExtension.closed` false, `src/PhobosFramework.Vehicles/Defs/SystemDefs.cs:559-564`) are always
  "windows down" and get no switch.

## Other factions' vehicles

| Where | What happens | Lets the player | Stops the player |
| --- | --- | --- | --- |
| **A raid on us** | A raider's passengers carry drugs as raiders do. On the lite tier (owner, 11 Nov 2026) they get no on-map smoking check, so nothing costs anything; a rare flavour roll may light one raider up on arrival, slowing a stoned driver | Notice smoke at a raider's windows and a slower vehicle | Nothing |
| **A base we attack** | Parked vehicles are empty; nothing runs | Nothing | Nothing |
| **A vehicle we capture** | It follows our rules at once | Use it as ours | Nothing |

Raiders arriving a little slower because their driver lit up is the flavour the owner suggested; it is a small effect on a rare roll,
and off with the setting.

## Cost

- **Nothing per tick.** The on-map check runs on the vehicle's rare tick (every 250 ticks) and only for vehicles with someone aboard;
  the caravan check rides on the game's own 120-tick drug check. The smoke number and the driver's cap are read from values worked
  out on that rare tick. Puffs are drawn only for a visible vehicle with smoke aboard.
- With several vehicles on a map, an empty vehicle costs a count check.

## Optional tie-ins

- **D'thorga Medical, Burns and Smoke:** none needed; secondhand cannabis smoke is not the smoke inhalation that part models.
- **Combat Extended:** none; CE's smoke is a battlefield gas on the map.
- **Drug mods (Vanilla Expanded and others):** their smoked drugs join by the smoke sound test, with their own outcome doers.
- **Ideology:** read through the game's own events, so ideology mods that add drug precepts work through them.

## What Vehicle Framework makes hard

- **On a map, passengers start no jobs.** The record's answer is our own rare-tick check that ingests directly, as the caravan code
  does; the alternative, putting a passenger out to smoke and back in, would be slow and look wrong.
- **Vehicle Framework's 1.7 build** may change how passengers tick. Our check would read only `AllPawnsAboard` and the handlers'
  pawns, which have not changed between the builds read.

## Questions for the owner

1. **Which drugs?** Smoked drugs only (smokeleaf and flake in the base game, by the smoke sound), or smokeleaf-like social drugs only?
   Flake is a hard drug; a secondhand flake high is darker. (Agent default: every smoked drug, with flake's bystander memory always
   a bad one.)
2. **The windows switch:** add it (agent default), or treat every closed cab as unventilated?
3. **Raiders hotboxing on the way in:** a rare flavour roll (agent default), or never?
4. **The figures above** (bystander share 0.3, driver cap down to 0.7 of speed, wear ×1.1): fine to start with, tuned in play?

## Who chose what

- **Owner (12 Nov 2026):** the idea, with smoke at the windows; a design record first; a stoned driver drives worse with real
  consequences, slower with a little more wear, and no random crashes.
- **Agent defaults, owner to revise:** every figure above; smoked drugs found by the smoke sound; no tolerance or addiction from
  secondhand smoke; a per-vehicle smoke number rather than a gas; the windows switch; the teetotaler test for the bad memory; the
  raiders' rare roll; the driver read by consciousness alone.

## Sources

- Herrmann ES, Cone EJ, Mitchell JM, Bigelow GE, LoDico C, Flegel R, Vandrey R (2015), *Drug and Alcohol Dependence* 151:194-202:
  [PubMed 25957157](https://pubmed.ncbi.nlm.nih.gov/25957157/), [PMC4747424](https://pmc.ncbi.nlm.nih.gov/articles/PMC4747424/); the
  [Johns Hopkins summary](https://hub.jhu.edu/2015/05/15/secondhand-marijuana-smoke-study/). Abstract and summary read; full text
  not read.
- NHTSA, [Drug-impaired driving](https://www.nhtsa.gov/node/138601): its summary of driving studies under cannabis. Its 2017
  Marijuana-Impaired Driving Report to Congress was not read.
- RimWorld 1.7 (Ludeon Studios) and Vehicle Framework (Smash Phil): read from our decompiles, summarised in our own words.
