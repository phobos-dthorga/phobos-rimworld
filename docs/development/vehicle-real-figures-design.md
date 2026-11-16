# Real figures for named vehicles (8 Nov 2026)

A design record, built the same day in D'thorga Vehicles 0.4.0 (Draft). **Not yet tested in game.** Part 2 of the owner's approved
proposals of 8 Nov 2026 ("real figures for the single-engine military vehicles, the L-ATV first"), after part 1,
[power and lift](vehicle-power-and-lift-design.md). Every figure comes from [the research record](vehicle-figures-research.md) and
is labelled in the Defs: **[P]** primary, **[S]** secondary, **[I]** our inference, **[A]** authored.

**Merged here on 17 Nov 2026**, each kept as written: [Kerb mass check](#kerb-mass-check-8-nov-2026); [Vehicle class by empty mass](#vehicle-class-by-empty-mass-16-nov-2026).

## Read this first

- **Named drivetrains replace the archetypes** for the vehicles below: their engines' real power (`ratedKilowatts`), idle and
  rated speeds, gear ratios where published, real curb or combat weights, tyre sizes and drive. Everything not published is
  authored and says so.
- **The final drive is worked out from the real top speed**: the rated rpm in top gear at that speed **[I]**. For the Humvee this
  gives 7.11, close to the hub and axle ratios people quote for it, though those were not verified.
- **Vehicle Framework still decides the speed**; these figures only change gear, rpm, load, heat and where power or grip runs out.
- **All the named drivetrains are in one file** (`mods/PhobosVehicles/Defs/Drivetrains_RealVehicles.xml`), loaded always because
  it names no other mod; each loadout loads only with its vehicle's mod. Both M1A2 mods share one AGT1500 Def.
- **The "no engine part" Defs were our scan's mistake**: the BMP-1 variants, the MaxxPro and the L-ATV have their engines in parent
  Defs (see [power and lift](vehicle-power-and-lift-design.md#which-vehicles-from-their-own-parts)). They are all fitted here.

## The vehicles

| Vehicle (mod) | Engine | Gearbox | Weight | Notes |
| --- | --- | --- | --- | --- |
| **L-ATV** (GTek's Vehicles) | 276 kW (370 hp) at 3,200 rpm **[owner's choice, see below]** | Allison 2500, six ratios **[P]** | 6,400 kg **[S]** | 740 A Niehoff alternator **[S]**; four-wheel drive **[P]** |
| MaxxPro (GTek's Vehicles) | 280 kW at 2,000 rpm **[P, excerpt]** | Allison 3000-series, five speeds **[S]**; ratios **[A]** | 15,422 kg **[S]** | 400 A alternator, 24 V **[S]** |
| Humvee (HUMVEE) | 119.4 kW at 3,400 rpm, idle 650 **[P: TM 9-2320-280-20-1]** | 4L80-E, 2.48 to 0.75 **[P]** | 2,500 kg **[S]** | Alternators 60 to 400 A at 28 V **[P]**; we keep 60 |
| Land Cruiser J70 (Frontline Workshop) | 1VD-FTV, 151 kW at 3,400 rpm **[S]** | H152F five-speed manual **[S]**; ratios **[A]** | 2,165 kg **[S]** | Part-time four-wheel drive: half the weight driven on the road **[A]** |
| "M1075 HEMTT" (Roaring Tiger) | C15, 447 kW **[P]**; rpm **[A]** | Allison 4500SP, six speeds **[P]**; ratios **[A]** | 24,040 kg **[P]** | **It is the PLS**, not a HEMTT; 260 A, 24 V **[P]** |
| M113 and M113(M) (Roaring Tiger) | 6V53, 158 kW at 2,800 rpm **[S, to verify]** | TX-100, three speeds **[S]** | 11,216 kg **[S]** | |
| M48H (Roaring Tiger) | AVDS-1790-2C, 560 kW **[S]**; rpm **[A]** | CD-850, two ranges **[S]** | 50 t **[S]** | Taiwan's CM-11 Brave Tiger |
| M10 Booker (Desert Sabre) | MTU 8V 199, 600 kW **[P]** | Allison 3040 MX, four **[P]** | 40 t **[S]** | At the top of its gearbox's rating |
| M2A3 Bradley (Desert Sabre) | VTA-903T, 447 kW at 2,600 rpm **[S]** | HMPT-500-3, three ranges **[S]** | 30 t **[S; 34.2 t elsewhere]** | |
| M1A2 and M1A2 SEP (both RimThunder mods) | AGT1500, 1,118 kW at 3,000 rpm **[S]** | X1100-3B, four **[P]** | 63,080 kg **[S]** | Turbine, as in 0.2.0 |
| T-54 (Frontline Workshop) | V-54, 388 kW at 2,000 rpm **[S]** | five-speed manual **[S]** | 36 t **[S]** | |
| BMP-1, -1P, -1PG, -1AM (BMP-1 IFV) | UTD-20, 220 kW at 2,600 rpm **[S]** | five-speed manual **[S]** | 13.2 t **[S]** | Swimming is not modelled |
| Wiesel (Frontline Workshop) | Audi turbo-diesel, 64 kW **[S]**; rpm **[A]** | not found; three **[A]** | 2.75 t **[S]** | |

**The L-ATV's power.** Oshkosh never published it. Secondary sources give 340, 400 or 425 hp. The Allison 2500's published rating is
254 kW (340 hp) with engine torque limiting **[P: Allison sheet SA5340EN]**, and the 2500 SP the L-ATV uses is "contact your Allison
representative". The first build used 254 kW, that published ceiling. **The owner chose the middle of 340 and 400 hp (8 Nov 2026):
370 hp, 276 kW**, which sits above the general 2500's published rating; the SP's own may be higher. In play: on a road at its in-game speed it cruises in fourth to sixth with power in hand; fully laden in
soft sand, four-wheel drive keeps its grip and the engine sets the pace (a rule test holds both).

## Simplifications

- Combat weights include crew and ammunition, which the model adds again (a few per cent; small).
- Fuel mass is left out: no density was sourced.
- Torque curves, efficiencies, shift points, drag areas and sprocket radii are authored.
- Transfer cases' low ranges are not modelled.

## To test in game (owner)

1. **The L-ATV** in Operator mode on a road and in sand, empty and laden: sensible gears, and it slows in deep sand without bogging.
2. The PLS laden on a hill: it labours but climbs.
3. A tracked vehicle (an M113 or a BMP-1) across soft ground.

## Who chose what

- **Owner (8 Nov 2026):** real figures for the single-engine military vehicles, the L-ATV first.
- **Owner (8 Nov 2026, later):** the L-ATV at 370 hp, the middle of the published figures.
- **Agent choices, owner to revise:** the final drives from top speed; every authored figure listed above.

## Kerb mass check (8 Nov 2026)

*Merged into this record on 17 Nov 2026 from `vehicle-real-figures-design.md`; kept as written.*


A dated record of the check `outstanding-work.md` listed as V8: the vehicle systems figure a vehicle's empty (kerb) mass as 12 kg per
unit of Vehicle Framework's Mass unless a loadout gives a real figure. Built the same day in D'thorga Framework 0.19.0 and D'thorga
Vehicles 0.8.0 (Drafts). **Not yet tested in game.**

### Read this first

- **Every real vehicle the owner has already carries a researched empty mass** in D'thorga Vehicles (the L-ATV, Humvee, MaxxPro,
  Land Cruiser, Wiesel, T-55, BMP-1 family, M113, Bradley, M1A2s, M10, M48, PLS, Harrier, Hind, Mongoose and the trains). The 12 kg
  rule reached only Vanilla Vehicles Expanded's fictional vehicles and unknown mods.
- **Vehicle Framework's Mass figures are not comparable between mods**, so no formula fixes them: RimThunder's M48 (Mass 1000) is a
  50 t tank (Taiwan's CM-11, secondary: Wikipedia, as [the figures research](vehicle-figures-research.md) and the M48H's Def have it;
  corrected 17 Nov 2026 from 45 t), Frontline Workshop's Land Cruiser (Mass 2000) weighs 2.2 t.
- **Owner's choice (8 Nov 2026): authored masses for the VVE vehicles** by the real class each description names, and the empty mass
  tunable per design in Vehicle Framework's settings window, so any player can correct a vehicle from another mod.

### What the rule gave (read with `query-defs`, 8 Nov 2026)

All 72 active Vehicle Framework Defs were read through `python scripts/query-defs.py` (parents resolved as the game picks them).
Real-world weights below are general, approximate class figures for comparison, not sources.

| Pattern | Examples: the rule's kerb mass, and a real vehicle of that class |
| --- | --- |
| Light vehicles too heavy | Dirtbike 360 kg (about 110); Charley motorcycle 600 kg (about 230); Dinghy 960 kg (about 350) |
| About right | Mule 2.9 t (pickup); Traveller 2.2 t (car); Big Rig 7.9 t (tractor unit); Warbird 3.4 t (P-51D, 3.5 t) |
| Armour far too light | Bulldog 5.4 t and Marshal 6.8 t (main battle tanks, 45 to 60 t); Roadkill 3.6 t (medium tank, about 30 t); Bunsen 3.1 t (light tank, about 15 t) |
| Odd | Trawler 960 kg (a small trawler weighs tonnes); Louie 9.4 t (a UH-1H Huey is about 2.4 t empty) |

**What it changed in play:** engine power follows kerb mass (power per tonne by drivetrain), so power-to-weight was consistent; but
cargo weighed far too much on tanks and too little on bikes, and Engineer mode showed figures no engineer would believe (a main
battle tank at about 108 kW).

### What was built

- **`PhobosVehicleFiguresExtension`** (Framework 0.19.0): a vehicle's own `emptyKilograms` and, optionally, `ratedKilowatts`, added
  with `PatchOperationAddModExtension`. Precedence, most specific first: the player's tuning; the vehicle's extension; the loadout's
  or drivetrain's `emptyKilograms`; Mass times `kilogramsPerVehicleMass` (12), never below 50 kg. Power: the extension's
  `ratedKilowatts`; the loadout's or drivetrain's; else power per tonne times the kerb mass.
- **Engine power for motorcycles.** At a real 110 kg the light petrol rule (60 kW per tonne) would give a dirt bike 6.6 kW; a 250 cc
  dirt bike makes about 30 kW. The Dirtbike and the Charley carry their own power (30 and 55 kW, approximate, general).
- **"Empty mass (kg)" in Vehicle Framework's settings window**, per design, filled with the figure above.
- **D'thorga Vehicles 0.8.0** adds the figures for 33 VVE vehicles in `Mods/VanillaVehiclesExpanded/Patches/Figures_VanillaVehiclesExpanded.xml` and
  `Mods/VanillaVehiclesExpandedTier3/Patches/Figures_VanillaVehiclesExpandedTier3.xml`, each with its class in a comment. Three follow a real aircraft (secondary
  sources, approximate): the Louie as a UH-1H Huey, about 2,365 kg empty
  ([Valiant Air Command](https://www.valiantaircommand.com/portfolio-collections/all-aircraft/huey)); the Cherokee as a CH-47D
  Chinook, about 10,500 kg (sources give 10,150 to 11,150 kg, for example
  [Warbirds Resource Group](https://vietnam.warbirdsresourcegroup.org/h47chinook-specifications.html)); the Warbird as a P-51D
  Mustang, about 3,465 kg ([U.S.A.A.F. Resource Center](https://www.warbirdsresourcegroup.org/URG/p51mustang_specifications.html)).
- **Left on the rule:** the three orbital pods (Frog, Toad, Goliath), which have no real class; and vehicles with no engine (the
  bicycle, the towed howitzer, railcars), which the systems do not fit.

### Who chose what

- **Owner (8 Nov 2026):** authored VVE masses; tunable empty mass.
- **Agent choices, owner to revise:** every class figure; engine power for the two motorcycles; the pods left alone.

## Vehicle class by empty mass (16 Nov 2026)

*Merged into this record on 17 Nov 2026 from `vehicle-real-figures-design.md`; kept as written.*


A design record for the idea "choose a vehicle's class by its empty mass" in [outstanding work](outstanding-work.md)'s list of
ideas (numbered I3 there on 16 Nov 2026): sort a vehicle into the light, medium or heavy class by its
empty mass in kilograms instead of Vehicle Framework's Mass figure. **Record only; nothing is built.** The owner asked for the record
on 16 Nov 2026.

### Read this first

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

### What it lets the player do, stops, and changes

| Question | Answer |
| --- | --- |
| **Lets** | A truck that weighs 5 to 8 tonnes runs on a truck's 24-volt electrics and heavy loads, and a small car runs on a petrol engine, whatever its author's Mass figure says. Another mod's vehicle given an empty mass (through `PhobosVehicleFiguresExtension` or the player's "Empty mass (kg)" setting at the next start) is classed by it |
| **Stops** | An 8-tonne truck (the Nomad) being fitted with a pickup's 12-volt electrics because its author gave it a Mass of 300 |
| **Changes** | Five vehicles' starting electrics, drivetrain and loads, as below. Engine power barely moves: it already follows the empty mass (power per tonne) |

### Why: Mass is not comparable between mods

Vehicle Framework's Mass is each author's own scale ([kerb mass check](#kerb-mass-check-8-nov-2026)): RimThunder's M48 has a Mass of
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

### Who would move (read 16 Nov 2026)

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

### How it would be built (proposal)

- **`kerbMin` and `kerbMax` on `LoadoutSelector`**, in kilograms, read through `Powertrain.KerbFor(def, null, null)`: the vehicle's
  own figure, else Mass times 12. A loadout's own drivetrain figure cannot be used, because it is the thing being chosen. The three class
  loadouts switch from `massMin`/`massMax` to these; `massMin`/`massMax` stay for other authors' loadouts and keep their meaning.
- **The player's "Empty mass (kg)" tuning does not decide the class at once**: the loadout is chosen at start-up, so a changed setting
  reclasses the design at the next start. Agent's choice: a class that changes under a running colony would swap systems mid-game.
- **The bounds live in the class loadouts' XML**, so another author or a player's patch can move them; no setting (a rule, not a
  preference).
- **A rule test** in `tests/PhobosFramework.Rules.Tests` for the bounds, and the dev self-test's report naming each vehicle's class and
  the figure that decided it.

### Saves

The loadout is never saved; it is chosen again at every start. The vehicle systems save their values and load switches by name
(`CompPhobosVehicleSystems.PostExposeData`), so a vehicle that changes class keeps the values its new loadout shares (fuel, wear,
oil) and starts any new one fresh, as it did when D'thorga Vehicles 0.8.0 gave these vehicles their empty masses. Read from the code,
not checked in play. **Safe to add mid-game** on that reading; a battery's charge across a 12-volt to 24-volt change is the row to
watch.

### Questions for the owner

1. **Build it?** It is small (one selector field pair, three loadouts, a test).
2. **The bounds**: exact (1,440 and 4,200 kg, nothing without a figure moves) or round (1,500 and 4,500 kg)?

### Who chose what

- **Owner (16 Nov 2026):** a record for this idea (class by empty mass), nothing built.
- **Agent choices, owner to revise:** the exact bounds, class decided at start-up only, no setting, the reading of each vehicle's real
  class.
