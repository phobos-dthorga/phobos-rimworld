# Per-engine realism (23 Nov 2026)

**Status (23 Nov 2026):** steps 1 and 2 built in D'thorga Framework 0.247.0 and Vehicles 0.108.0 (power by engine health, row 401;
lopsided thrust, row 402); steps 3 and 4 designed, not yet built. Nothing here is tested in game yet.

## Read this first

- **The owner's question (23 Nov 2026):** after the A-10's new engines became a matched pair, "thrust per engine, could we apply that to
  anywhere else for more realism?" **The owner's card, the same day:** all four ideas offered: power by engine health, wear per engine,
  engine-out yaw on jets only, and matched tyres and brakes per axle.
- **What the code did before:** an engine was in or out (out below 20 % of its working order), so a twin with one engine at 25 % gave all
  its power; the turbine's hot-section life was one figure for the whole vehicle; and an engine out on a twin jet cost its share of power
  and nothing else.
- **Two limits found while designing:**
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

### Step 3: matched tyres and springs per axle (designed)

- Today's matched set (Framework 0.246.0) groups every part of a fitting's roles. A per-axle grouping makes road and all-terrain tyres
  and heavy-duty springs go on both wheels of an axle together, and work once the axle is matched. Mismatched brakes or springs on one
  axle pull a vehicle to one side, and many road rules require matching tyres on an axle.

### Step 4: wear per engine (designed)

- Hot-section life becomes one figure an engine, overhauled one engine at a time, so a twin can have one engine due while the other is
  fresh. It needs a new Framework feature, a quantity kept per part, which also suits other per-part wear later (each wheel's tread).
  A re-engining stays a matched pair (Framework 0.246.0).

## Sources

- Our own code, read on 23 Nov 2026: `Powertrain.Engines`, `TurbineModel.Evaluate`, the turbine system (`Systems_Turbine.xml`).
- The V-22's interconnecting drive shaft and twin helicopters' combining gearboxes are general aviation knowledge, **not cited to a
  source here**. They only explain why those aircraft carry no yaw figure; no figure rests on them.
