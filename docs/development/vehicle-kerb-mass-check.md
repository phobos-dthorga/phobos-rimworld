# Kerb mass check (9 Nov 2026)

A dated record of the check `outstanding-work.md` listed as V8: the vehicle systems figure a vehicle's empty (kerb) mass as 12 kg per
unit of Vehicle Framework's Mass unless a loadout gives a real figure. Built the same day in D'thorga Framework 0.19.0 and D'thorga
Vehicles 0.8.0 (Drafts). **Not yet tested in game.**

## Read this first

- **Every real vehicle the owner has already carries a researched empty mass** in D'thorga Vehicles (the L-ATV, Humvee, MaxxPro,
  Land Cruiser, Wiesel, T-55, BMP-1 family, M113, Bradley, M1A2s, M10, M48, PLS, Harrier, Hind, Mongoose and the trains). The 12 kg
  rule reached only Vanilla Vehicles Expanded's fictional vehicles and unknown mods.
- **Vehicle Framework's Mass figures are not comparable between mods**, so no formula fixes them: RimThunder's M48 (Mass 1000) is a
  45 t tank, Frontline Workshop's Land Cruiser (Mass 2000) weighs 2.2 t.
- **Owner's choice (9 Nov 2026): authored masses for the VVE vehicles** by the real class each description names, and the empty mass
  tunable per design in Vehicle Framework's settings window, so any player can correct a vehicle from another mod.

## What the rule gave (read with `query-defs`, 9 Nov 2026)

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

## What was built

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

## Who chose what

- **Owner (9 Nov 2026):** authored VVE masses; tunable empty mass.
- **Agent choices, owner to revise:** every class figure; engine power for the two motorcycles; the pods left alone.
