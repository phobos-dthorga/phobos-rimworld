# The C-17 Globemaster III in the fold (23 Nov 2026)

**Status (23 Nov 2026):** built, not yet tested in game: D'thorga Framework 0.254.0 (four engines by wing; a named hold's weight), D'thorga
Vehicles 0.111.0 (its systems), D'thorga Patches 0.40.0 (its build cost as a parts bill). Test rows 420 to 422.

## Read this first

- **What it is:** C-17 Globemaster III (Vehicle Framework) by z3nt3 (`z3nt3.c17globemaster`, Workshop 3841001179), updated 22 Nov 2026, by
  the Black Hawk's author ([uh60-blackhawk.md](uh60-blackhawk.md)). The owner asked on 23 Nov 2026: "let's bring the C-17 Globemaster III
  into the fold".
- **The judgement (agent's, 23 Nov 2026): well made for its size, and new.** For it: 31 parts (four engines with covers, wings, ailerons,
  flaps, stabilisers, rudder, three landing wheels, a fuel tank, servos, a dashboard), Vehicle Framework's own runway launch, and its own
  features built on the game's systems: point-to-point and airdrop modes, a hold that loads vehicles, people and goods from a zone behind
  the tail, and a flyover that drops paratroopers, crates and vehicles. Seven Harmony patches, none skipping the game's code
  (`query-patches.py mod "C-17*"`): its arrival options, its sounds, the engines off on landing, and keeping a map while its flyover is
  over it. Against it: 157 subscribers, no comments yet, no public source (read from a decompile in
  `.local/decompiled/C17Globemaster-1.7-2026-11-22`, never committed); its messages are hard-coded English.
- **Its author's features are left alone**: its modes, hold, loading zone, airdrops and sounds. Its build cost becomes a parts bill.
- **Two things it did not fit, and the owner's cards (23 Nov 2026):**
  - **Four engines.** Lopsided thrust and the side tanks knew twins only. *Card: "Yes, by wing".* Both now take any number of engines,
    each placed by where its part sits across the aircraft: an outboard engine out pulls more than an inboard one, and each wing's tank
    feeds the engines under it.
  - **Its hold.** It keeps loaded vehicles in a comp of its own, outside the cargo, so our engine model saw a C-17 carrying 77 t of
    trucks as empty. *Card: "Count the hold".* A loadout may now name another mod's comp whose held things are carried weight.

## The four questions

| Question | Answer |
| --- | --- |
| What it lets the player do | Fly a C-17 whose four engines, hydraulics, avionics and tanks wear, fail and are repaired like the rest of the fleet's, and whose load weighs what it carries |
| What it stops them doing | Flying 77 t of vehicles as if empty; building it from raw steel and components alone |
| How it changes play | A heavy airlift climbs slowly and burns hard; an engine out yaws it, more for an outboard one; a holed wing tank is the cross-feed choice |
| What outside it notices | Another mod's air defence sees its size; colonists repair its parts and load its dispensers; a raider's C-17 runs the same systems |

## How

- **Four engines by wing** (Framework): `AirCombatRule.Asymmetry` takes each engine's output and its arm (its part's place across the
  aircraft), so the twin rule is its special case (a twin's arms stay one each side by order, as its yaw figure assumes). The dodge
  (`LopsidedThrust`) and the fuel (`LopsidedThrustFuel`) read it through `Powertrain.ThrustAsymmetry`. `SideTanks` takes each engine's
  side from the same arms: each side burns by the work of the engines it feeds, and a dry side starves all of them. The C-17's engines sit
  at -5, -4, 4 and 5 cells across, as the mod draws them.
- **A named hold** (Framework): `heldMassComps` on a loadout lists comps by full type name; `HeldMass` adds what each holds (a vehicle by
  its mass stat, as the C-17's own sum does) to the powertrain's mass. Read by name: no reference to the other mod's assembly.
- **Its loadout** (Vehicles, `Loadouts_C17.xml`): four F117-PW-100s at 179.9 kN each (primary, the Air Force's fact sheet), maximum
  continuous authored at 0.9; 128,140 kg empty and 108,000 kg of fuel (secondary); a 28,000 ft cruise and a 45,000 ft ceiling (primary);
  full navigation, autopilot, weather radar, a missile warner and 120 flare cartridges (authored count); fly-by-wire hydraulics on its
  servos; its three landing wheels as landing gear; its tank as the fuel tank. Yaw figure 0.2, grit exposure 0.3, side tanks: all authored.
- **Its parts bill** (Patches): four engines and generators, four hydraulic pumps and actuators, three landing gear, two tanks, battery,
  wiring and avionics; the shell keeps 2,000 steel, 500 plasteel, 300 silver and 40 components (from 2,250, 600, 300, 120 and 450
  chemfuel). Authored, for the owner's review.

## The fleet audit that followed (23 Nov 2026)

The owner asked, the same day: "Do any of our other aircraft/vehicles and their definitions need updating?" Every aircraft loadout was
tabulated against this session's new fields (engines bound, drivetrain, flight profile, yaw, grit, side tanks, hold), and the land
vehicles' brakes against their bills and repair supplies. Found and done:

- **A fault of ours (Framework 0.252.0): grit never judged at a stock launch.** An aircraft on Vehicle Framework's own launch (the Black
  Hawk, the C-17, the Cherokee, RimArrow's, Vanilla Vehicles Expanded's) is off the map for its whole take-off and landing, so no flight
  phase ever saw either; its "Taxi" phase is simply "engines on", so it took a small chance of grit for as long as it stood drafted.
  Fixed: one judgement at Vehicle Framework's launch event and one when it is next seen back on the ground (`StripDebris.OnLaunch`,
  `Update`), for profiles with no take-off phase of their own; taxiing counts only on the move (`movingOnly`).
- **Three twins left out of round two by oversight:** the Cherokee, the Ka-27PS and the Mi-38 now carry side tanks, a grit figure of
  their own, and a twin turboshaft whose contingency opens only with an engine out (`PhobosDrivetrain_AeroTurboshaftTwin`, authored
  figures). Sourcing their real engines' ratings (the TV3-117 family, the TV7-117V) is open.

Found and left, for the owner:

- **Thruster craft (Vanilla Vehicles Expanded's Frog, Toad and Goliath)** bind three or four thrusters and could now take lopsided
  thrust and side tanks; the owner chose "jets only" for the yaw (23 Nov 2026), so they are left.
- **The Black Hawk's own interior** is a pocket map: people walking in it are not weighed (about 70 kg each on a 10 t helicopter).
  `heldMassComps` reads holders, not maps; left as too small to matter.
- **Brakes at each wheel:** no parts bill lists brakes, so no build cost moved; a wrecked wheel brake is repaired as a small mechanical
  part each (12 steel, 2 components), so a whole set costs four times what the one part did. Trains keep their brake pipe; a tracked
  vehicle gets a brake a track. Nothing to change.

## The audit's follow-ups (23 Nov 2026)

The owner's yes, the same day, to both follow-ups offered:

- **The Ka-27PS and Mi-38's own engines** (Vehicles 0.112.0). The TV7-117V: 2,800 shp take-off and 3,750 shp emergency (secondary: AIN,
  19 Dec 2013; Wikipedia); Rostec's 3,140 hp "emergency mode" is read as a longer one-engine-out rating, our inference, so both are kept
  (`PhobosDrivetrain_TV7117`). The Ka-27PS's TV3-117V gives 2,225 shp (secondary: Wikipedia) and no one-engine-out rating was found, so
  it shares the Hind's TV3-117VMA ladder, moved to `Drivetrains_RealVehicles.xml` for both.
- **The thruster craft** (Framework 0.255.0, Vehicles 0.112.0). The Frog (three thrusters), Toad and Goliath (four each) take lopsided
  thrust (yaw 0.25, 0.25 and 0.2, authored) and side tanks. The Toad's thrusters sit at its corners in its design; the Frog's and
  Goliath's each cover the whole body, so a loadout may now give each engine's place (`engineArms`): the Frog two either side and one on
  the centreline (which draws on both tanks), the Goliath as the Toad's corners. All authored.

## Other factions' vehicles

A raider's or an enemy base's C-17 gets the same systems; its parts can be shot out on the ground, and a captured one keeps them. Its hold
counts for any faction's.

## Risk

Moderate (agent's grade): a laden C-17 is heavier to our engine model than before, so it may climb and cruise lower; nothing of the
player's is put at risk. A C-17 frame under construction keeps what was delivered and then asks for the new list.
