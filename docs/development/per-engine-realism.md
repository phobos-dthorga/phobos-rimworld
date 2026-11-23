# Per-engine realism (23 Nov 2026)

**Status (23 Nov 2026):** all four steps built in D'thorga Framework 0.247.0 and Vehicles 0.108.0 (power by engine health, row 401;
lopsided thrust, row 402; tyres and springs matched per axle, row 403; wear per engine, row 404). **Round two** (owner's cards of 23 Nov
2026) brings the first round in line with its cards and adds four smaller ideas, built in Framework 0.248.0 step by step: see
[Round two](#round-two). Nothing here is tested in game yet.

## Read this first

- **The owner's question (23 Nov 2026):** after the A-10's new engines became a matched pair, "thrust per engine, could we apply that to
  anywhere else for more realism?" **The owner's card, the same day:** all four ideas offered: power by engine health, wear per engine,
  engine-out yaw on jets only, and matched tyres and brakes per axle.
- **What the code did before:** an engine was in or out (out below 20 % of its working order), so a twin with one engine at 25 % gave all
  its power; the turbine's hot-section life was one figure for the whole vehicle; and an engine out on a twin jet cost its share of power
  and nothing else.
- **Two limits found while designing** (both lifted in round two, on the owner's cards):
  - **Yaw costs no fuel.** Holding a straight line against lopsided thrust takes rudder, which is drag, and the vehicles' rule says no
    drag or aerodynamics (owner, 13 Nov 2026). So the yaw moves handling and dodge only.
  - **Brakes are one part a vehicle** in our loadouts (`PhobosBrakes`), so they cannot be mismatched; per-axle matching applies to
    tyres and springs, which go on each wheel.

## The four steps

### Step 1: power by engine health (built, Framework 0.247.0)

| Question | Answer |
| --- | --- |
| What it lets the player do | Read a damaged engine's lost power in the aircraft's flight, climb and hover |
| What it stops them doing | Flying a shot-up aircraft as hard as a sound one: a badly damaged engine gives about half its power |
| How it changes play | Repairing an engine before it fails matters; a heavy hover or a high cruise may need a sound pair |
| What outside the vehicle notices | Nothing beyond the aircraft itself, by choice: its weaker climb and hover are what the player sees |

- **The rule** (`TurbineModel.EngineOutput`): out at or below 20 % working order, as before; above it, from half its output rising in a
  line to all of it when sound. **The shape and the figures are AUTHORED**: a turbine with torn blades and seals loses compression and gives
  less before it stops, but no published curve was used.
- **Where it reaches:** the turbine's available output (and so its temperature and wear for a demand), the hover margin, the lift check
  and the cruise height. The generators still share by engines running, and an aircraft still stays up while any engine runs.
- **Risk: moderate** (agent's grade, for the owner to confirm): an aircraft loses power it had before, in sight, while an engine still
  runs; it can come down from a hover it could hold before. **Setting:** "Damaged turbine engines give less power", on by default (the
  Flight section of D'thorga Framework's vehicle settings).
- **Other factions' vehicles:** the same rule on any faction's turbine aircraft, raiding or parked; nothing is stored.

### Step 2: engine-out yaw on twin jets (built, Framework 0.247.0, Vehicles 0.108.0)

| Question | Answer |
| --- | --- |
| What it lets the player do | See a twin jet with an engine out or damaged fly less nimbly |
| What it stops them doing | Keeping a crippled twin jet's full dodge over a map |
| How it changes play | An engine out over a defended map is a reason to leave; repairs matter |
| What outside the vehicle notices | The enemy's guns: they hit it more often |

- **The rule** (`AirCombatRule.LopsidedThrust`): the dodge is multiplied by one less the loadout's `engineOutYaw` times the difference
  between the two engines' outputs (step 1's, or in and out with its setting off). Only a twin pulls. **The A-10, Su-25 and Prowler
  have 0.15** (authored): all three carry their engines close beside the fuselage. Applied in `AirCombat.Evasion`, which every
  aircraft's map dodge reads (ours and the Patches hooks), so it costs nothing on the world map.
- **Risk: moderate** (agent's grade): more hits on a damaged jet, in sight. **Setting:** "Lopsided thrust unsettles twin jets", on.
- **Other factions' vehicles:** any faction's twin jet with a figure; read when fired at, nothing stored.

The design as first written:

- Lopsided thrust (one engine out, or by step 1 one weaker than the other) costs a twin jet some of its dodge, scaled by how far its
  engines sit from the centreline: a per-aircraft authored figure on the loadout, 0 by default. Helicopters stay at 0 (one gearbox drives
  the rotor from both engines), as does the Osprey (a cross-shaft lets either engine turn both rotors), and so do Vanilla Vehicles
  Expanded's thruster craft (the owner chose jets only). The A-10, Su-25 and Prowler all carry their engines close in, so their figure is
  small. No fuel cost (the drag rule above).

### Step 3: matched tyres and springs per axle (built, Framework 0.247.0)

| Question | Answer |
| --- | --- |
| What it lets the player do | Order tyres or springs for an axle from either wheel |
| What it stops them doing | Running an axle with one new tyre or spring and one old one and gaining from it |
| How it changes play | Tyres and springs come in pairs: twice the materials before an axle gains anything |
| What outside the vehicle notices | Nothing, by choice: it is the vehicle's own upkeep |

- **How:** a fitting's `matchedPerAxle` (with `matchedSet`) makes each axle its own group (the fittings window's axle rows,
  `PartRows`); ordering, cancelling and taking off follow the group; a fitted wheel counts only once its axle is whole. Road tyres,
  all-terrain tyres and heavy-duty springs carry it. A vehicle whose wheels are not grouped into axles keeps each wheel apart.
- **Risk: low** (agent's grade): no loss, only upkeep; no setting.
- **Other factions' vehicles:** their sets go on every free part, so whole axles.

The design as first written:

- Today's matched set (Framework 0.246.0) groups every part of a fitting's roles. A per-axle grouping makes road and all-terrain tyres
  and heavy-duty springs go on both wheels of an axle together, and work once the axle is matched. Mismatched brakes or springs on one
  axle pull a vehicle to one side, and many road rules require matching tyres on an axle.

### Step 4: wear per engine (built, Framework 0.247.0)

| Question | Answer |
| --- | --- |
| What it lets the player do | See which engine of a twin is worn, and overhaul that one |
| What it stops them doing | Treating a twin's engines as one: covering for a weak engine wears the other |
| How it changes play | A twin may need two overhauls, one kit each, at different times |
| What outside the vehicle notices | The colony's stores: kits are drawn one engine at a time |

- **How (agent's design, simpler than the per-part quantity first written below):** `EngineWear` keeps each engine's figure in a new
  save key (`PhobosVehicleSystems_EngineHotSection`, by engine role). After the systems advance, the wear the turbine system added to
  its one figure is shared among the engines by the work each did (`EngineWearRule.Shares`: output over the running engines' mean;
  AUTHORED, as a damaged engine may in truth run hotter), and the figure is set to the most worn engine's, so the dashboard, the worn
  stage and its warnings read the engine that needs it. The overhaul (`CompleteService`) does the most worn engine alone; the worn
  stage's breakdown (`mostWornEngine` on `Effect_Breakdown`) hits it. A single-engined vehicle is untouched.
- **Older saves:** each engine starts at the aircraft's figure.
- **Risk: low** (agent's grade): upkeep only; no setting.
- **Other factions' vehicles:** works the same on any faction's twin; their figures are saved with the vehicle.

The design as first written:

- Hot-section life becomes one figure an engine, overhauled one engine at a time, so a twin can have one engine due while the other is
  fresh. It needs a new Framework feature, a quantity kept per part, which also suits other per-part wear later (each wheel's tread).
  A re-engining stays a matched pair (Framework 0.246.0).

## Round two

**The owner's question (23 Nov 2026):** "Now we bring the cards in-line? And implement the smaller suggestions from before?" **The
owner's cards, the same day:** yaw burns fuel too, as a third exception to the no-drag rule; brakes one part per wheel, paired per axle;
the systems engine keeps a figure per part, generically; and all four smaller ideas (fuel cross-feed, sourced one-engine-out ratings,
rough-strip debris, engines starting in turn).

### Step 5: lopsided thrust burns fuel (built, Framework 0.248.0)

| Question | Answer |
| --- | --- |
| What it lets the player do | See a twin jet with an engine out reach less far |
| What it stops them doing | Flying a crippled twin jet home as cheaply as a sound one |
| How it changes play | An engine out shortens the way home a little: land sooner, or carry more fuel |
| What outside the vehicle notices | The colony's fuel stores, a little |

- **The rule** (`AirCombatRule.LopsidedThrustFuel`): fuel is multiplied by one plus the loadout's `engineOutYaw` times
  `TrimFuelPerYaw` (0.5, AUTHORED) times the difference between the two engines' outputs, so the A-10, Su-25 and Prowler burn 7.5 % more
  with one engine fully out. No published figure was found; a real single-engine cruise is slower and thirstier for more reasons than
  trim. It rides on the hot-section wear factor (`Powertrain.WearFuel`), so it reaches both the world map's burn and range and the
  burn on a map.
- **The rule it rests on:** the third exception to the no-drag rule (owner's card, 23 Nov 2026), written in the vehicles' `AGENTS.md`:
  moved only by the engines' outputs and the yaw figure, never a fitting, accessory, load or upgrade.
- **Risk: low** (agent's grade): a few percent of fuel. **Setting:** the same "Lopsided thrust unsettles twin jets", on.
- **Other factions' vehicles:** any faction's twin jet with a figure; nothing stored.

### Step 6: any figure kept per part (built, Framework 0.248.0)

| Question | Answer |
| --- | --- |
| What it lets the player do | Read each part's own wear under its reading on the Systems tab |
| What it stops them doing | Nothing new by itself; it carries step 4 and step 7 |
| How it changes play | As step 4 for engines; for brakes, see step 7 |
| What outside the vehicle notices | Nothing |

- **How:** a quantity's `perPart` (`PerPartSpec`: `engines`, or a `role`; `frontAxleWeight`; `restoreByAxle`) makes `PartFigures`
  keep one figure per part in the save key `PhobosVehicleSystems_PartFigures` ("quantity|part key"). After the systems advance, what
  the one figure gained is shared (`PartWearRule.Shares` for engines' work, `AxleShares` by axle, else evenly), and the figure is set
  to the most worn part's. A service restore does the most worn part, or its axle (`RestoreSet`); `Effect_Breakdown.mostWornOf`
  breaks it. Step 4's `EngineWear` became this, with `hotSectionLife` marked `<perPart><engines>true</engines></perPart>`.
- **Older saves:** 0.247.0's `PhobosVehicleSystems_EngineHotSection` is read once and taken over; nothing else changes.
- **Risk: low** (agent's grade): carried over, no new effect by itself.
- **Other factions' vehicles:** the same on any faction's vehicle; saved with it.

### Step 7: a brake at each wheel (built, Framework 0.250.0)

| Question | Answer |
| --- | --- |
| What it lets the player do | Fit heavy-duty brakes an axle at a time; see which wheel's brakes are worn |
| What it stops them doing | Fitting one brake upgrade to a whole vehicle; a hit on one wheel now damages that wheel's brake alone |
| How it changes play | Front brakes wear first and are changed first, an axle and a pad set at a time |
| What outside the vehicle notices | The colony's stores: pad sets go an axle at a time; heavy-duty brakes cost a little more in all |

- **The parts:** a loadout's added part may now be made one for each part of a role (`PartToAdd.eachOf`, with `eachLabel`; `EachPart`).
  The brakes our loadouts add are made one per wheel: the first keeps the key `PhobosBrakes` and its place, the rest are
  `PhobosBrakes_<wheel key>`, appended after every other part we add, because Vehicle Framework matches saved parts by position. Each takes
  its own wheel's cells, so a hit there reaches that brake. A design whose author drew its own brake part keeps it, as before.
- **Wear:** `brakeWear` is kept per part (step 6): the front axle weighs 1.5 against the others' 1 (AUTHORED; weight moves forward under
  braking, so front brakes do more of the work: about 60/40 on two axles). The gauge shows the worst wheel. **New pads go on the most
  worn axle, both sides together**, one pad set an axle; while another axle is past its limit the service falls due again.
- **Heavy-duty brakes** are now a matched set per axle, as tyres and springs are (step 3), at 15 steel, 2 components and 3,000 work a
  wheel (AUTHORED; 60, 8 and 12,000 on two axles, against 50, 5 and 8,000 for the one part before). Matched sets per axle now group parts
  by where they sit along the vehicle, not only wheels, so a brake at each wheel pairs by axle too.
- **Limits:** the brakes' working order is the average of the wheels' (`Input_PartEfficiency`), so one wrecked wheel brake no longer
  stops a vehicle braking at all; it takes most of them. A hit on one wheel's brake adds wear to the vehicle's figure, which is then
  shared among all the brakes, not to that wheel alone (a simplification). Mismatched brakes are not modelled as pulling to one side;
  the pairing stops them being fitted mismatched.
- **Older saves:** a vehicle saved with one brake part gains the others at its first load, each with that part's health; its brakes'
  wear starts at the one figure for every wheel. A vehicle fitted with heavy-duty brakes before keeps them on the first wheel's brake;
  the axle gains nothing until both its wheels carry them (the fittings window offers the rest).
- **Risk: moderate** (agent's grade): parts are added to saved vehicles, by the same path the glass and the extinguisher took.
- **Other factions' vehicles:** the same parts and wear on any faction's vehicle with our loadout; saved with it.

### Step 8: one-engine-out ratings (built, Framework 0.250.0, Vehicles 0.109.0)

| Question | Answer |
| --- | --- |
| What it lets the player do | Fly a twin helicopter home on one engine, on its short emergency rating |
| What it stops them doing | Using the emergency rating with both engines sound, as the engines' controls do not allow it |
| How it changes play | A very heavy hover in hot or thin air is closer to the edge; an engine out leaves a little more in hand |
| What outside the vehicle notices | Nothing |

- **The rule:** `TurbineRating.EngineOutOnly` (XML `engineOutOnly`); `TurbineSpec.EngineOut`, set by the powertrain with the drive limit
  (two or more engines, one or more out). `Top`, the temperature ladder and the hover margin skip a rating not allowed now. A T700's
  control arms its contingency when the other engine's torque falls to 51 % or below (TM 1-1520-238-10, primary).
- **The figures** (each loadout's notes name its sources):
  - **Black Hawk, T700-GE-701D** (primary, GE datasheet): 1, 1.108 (30 min), 1.162 (10 min), 1.166 (2.5 min, engine out only);
    temperatures 810, 851, 878 and 903 degrees C are the -701C's (secondary), replacing an older engine's 775 and 850 and an unsourced 815.
  - **Hind, TV3-117VMA** (primary, read through search excerpts): take-off 2,200 hp, 2,400 shp for 2.5 min with an engine out. Maximum
    continuous not found: authored at 0.9 of take-off, so 1, 1.111, 1.212. Temperatures authored.
  - **Mongoose, Gem 2 Mk 1004D** (secondary): about 815 shp continuous, 952 contingency, 1,035 for 20 seconds; take-off taken as the
    quoted 890 shp (authored). Both emergency ratings engine out only (authored). Temperatures authored.
  - **Osprey, AE 1107C** (secondary): one-engine-out 6,380 shp against 6,150 take-off, so 1.083; its time and temperature authored.
- **Left alone:** the Framework's generic helicopter turboshaft keeps its contingency usable at any time, as it serves single-engined
  helicopters of other mods.
- **Risk: moderate** (agent's grade): the Hind and Mongoose lose a little top power with both engines sound.
- **Other factions' vehicles:** the same on any faction's aircraft; nothing stored.

## Sources

- Our own code, read on 23 Nov 2026: `Powertrain.Engines`, `TurbineModel.Evaluate`, the turbine system (`Systems_Turbine.xml`).
- The V-22's interconnecting drive shaft and twin helicopters' combining gearboxes are general aviation knowledge, **not cited to a
  source here**. They only explain why those aircraft carry no yaw figure; no figure rests on them.
