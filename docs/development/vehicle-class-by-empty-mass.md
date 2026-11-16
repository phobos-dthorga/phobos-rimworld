# Vehicle class by empty mass (16 Nov 2026)

A design record for idea I3 in [outstanding work](outstanding-work.md): sort a vehicle into the light, medium or heavy class by its
empty mass in kilograms instead of Vehicle Framework's Mass figure. **Record only; nothing is built.** The owner asked for the record
on 16 Nov 2026.

## Read this first

- **Five vehicles on the owner's list would change class**, all from Vanilla Vehicles Expanded and all wheeled: the Traveller drops
  to light; the Hermano, Highwayman, Roadrunner and Nomad rise to heavy. Nothing else on the list moves.
- **Real vehicles are not affected.** Every real vehicle we support (the Humvee, L-ATV, MaxxPro, the RimThunder and Frontline
  Workshop vehicles, the Harrier, the trains' locomotives with figures) has its own loadout named for it, and never reaches the class
  split. The class decides only for vehicles with no loadout of their own.
- **Tracked vehicles, boats and electric vehicles are not affected either**: their own loadouts outrank the class split (the tracked
  loadout by rank 1, boats by vehicle type, the Lightning by its electric patch).
- **Proposed bounds keep every vehicle without a figure exactly where it is**: 1,440 kg and 4,200 kg are today's Mass bounds (120 and
  350) times the 12 kg per Mass that a vehicle without a figure is given. Only vehicles with a real empty mass can move.
- **Not yet tested in game; not built.**

## What it lets the player do, stops, and changes

| Question | Answer |
| --- | --- |
| **Lets** | A truck that weighs 5 to 8 tonnes runs on a truck's 24-volt electrics and heavy loads, and a small car runs on a petrol engine, whatever its author's Mass figure says. Another mod's vehicle given an empty mass (through `PhobosVehicleFiguresExtension` or the player's "Empty mass (kg)" setting at the next start) is classed by it |
| **Stops** | An 8-tonne truck (the Nomad) being fitted with a pickup's 12-volt electrics because its author gave it a Mass of 300 |
| **Changes** | Five vehicles' starting electrics, drivetrain and loads, as below. Engine power barely moves: it already follows the empty mass (power per tonne) |

## Why: Mass is not comparable between mods

Vehicle Framework's Mass is each author's own scale ([kerb mass check](vehicle-kerb-mass-check.md)): RimThunder's M48 has a Mass of
1,000 and weighs 45 t; Frontline Workshop's Land Cruiser has 2,000 and weighs 2.2 t; Vanilla Vehicles Expanded's Nomad has 300 and
we give it 8 t. The three class loadouts in D'thorga Framework (`Loadouts_Default.xml`) split land vehicles by Mass:

| Class | Mass today | What the class gives |
| --- | --- | --- |
| Light | under 120 | Light petrol drivetrain, 12-volt electrics |
| Medium | 120 to 349.99 | Heavy diesel drivetrain, a light vehicle's 12-volt electrics (pickups, light trucks) |
| Heavy | 350 and up | Heavy diesel, 24-volt electrics, the heavy set of loads; its own group in the settings (`CombustionHeavy`) |

We already hold a better figure. Since Framework 0.19.0 every vehicle has an empty mass (`Powertrain.KerbFor`): its own
`PhobosVehicleFiguresExtension`, else a drivetrain's figure, else Mass times 12, never under 50 kg. D'thorga Vehicles gives 33 Vanilla
Vehicles Expanded vehicles an authored empty mass by real class.

## Who would move (read 16 Nov 2026)

Read with `python scripts/query-defs.py show` for each of the 78 active vehicle Defs (Mass from `vehicleStats`, parents resolved as
written, without other mods' patches) and our own XML for the empty masses. 37 have a loadout of their own and are left out. Of the
41 left, these change class under the proposed bounds:

| Vehicle | Mass | Class by Mass | Empty mass (ours, authored) | Class by empty mass |
| --- | --- | --- | --- | --- |
| Traveller | 180 | medium | 1,300 kg | **light** |
| Highwayman | 220 | medium | 4,400 kg | **heavy** |
| Roadrunner | 260 | medium | 5,000 kg | **heavy** |
| Nomad | 300 | medium | 8,000 kg | **heavy** |
| Hermano | 320 | medium | 5,500 kg | **heavy** |

Also differing on paper, but taken by another loadout first: the Badger, Bunsen and Roadkill (tracked), the Trawler (a boat), the
Lightning (electric). Our Pintle trailers and the Crawler differ on paper too, but trailers fit no systems and the Crawler has its
own submarine set; worth one line in a check when built. **Agent's reading:** each of the five moves the way its description's real
class would: the Traveller is a car, the other four are trucks or buses of several tonnes.

The Scytheman (4,000 kg) stays medium, 200 kg under the bound; rounding the bounds to 1,500 and 4,500 kg would keep the Highwayman
medium and move vehicles without a figure whose Mass sits between 120 and 125 or 350 and 375 (none on the owner's list today). The
exact bounds are proposed for that reason.

## How it would be built (proposal)

- **`kerbMin` and `kerbMax` on `LoadoutSelector`**, in kilograms, read through `Powertrain.KerbFor(def, null, null)`: the vehicle's
  own figure, else Mass times 12. A loadout's own drivetrain figure cannot be used, because it is the thing being chosen. The three class
  loadouts switch from `massMin`/`massMax` to these; `massMin`/`massMax` stay for other authors' loadouts and keep their meaning.
- **The player's "Empty mass (kg)" tuning does not decide the class at once**: the loadout is chosen at start-up, so a changed setting
  reclasses the design at the next start. Agent's choice: a class that changes under a running colony would swap systems mid-game.
- **The bounds live in the class loadouts' XML**, so another author or a player's patch can move them; no setting (a rule, not a
  preference).
- **A rule test** in `tests/PhobosFramework.Rules.Tests` for the bounds, and the dev self-test's report naming each vehicle's class and
  the figure that decided it.

## Saves

The loadout is never saved; it is chosen again at every start. The vehicle systems save their values and load switches by name
(`CompPhobosVehicleSystems.PostExposeData`), so a vehicle that changes class keeps the values its new loadout shares (fuel, wear,
oil) and starts any new one fresh, as it did when D'thorga Vehicles 0.8.0 gave these vehicles their empty masses. Read from the code,
not checked in play. **Safe to add mid-game** on that reading; a battery's charge across a 12-volt to 24-volt change is the row to
watch.

## Questions for the owner

1. **Build it?** It is small (one selector field pair, three loadouts, a test).
2. **The bounds**: exact (1,440 and 4,200 kg, nothing without a figure moves) or round (1,500 and 4,500 kg)?

## Who chose what

- **Owner (16 Nov 2026):** a record for I3, nothing built.
- **Agent choices, owner to revise:** the exact bounds, class decided at start-up only, no setting, the reading of each vehicle's real
  class.
