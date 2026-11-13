# Tie-ins with other mods: the register (13 Nov 2026)

**A register of what exists, not a design.** Every place one of our mods works with another author's mod when that mod is active,
read from each mod's `LoadFolders.xml`, the `MayRequire` entries in our Defs and the optional-mod checks in our C# on 13 November
2026. Most tie-ins are **not yet tested in game**; each mod's changelog says which are. New links waiting on the owner are in
[soft-dependency cross-links](soft-dependency-crosslinks.md) and [air-defence tie-ins](air-defence-tie-ins.md).

## Read this first

- **63 tie-in folders across six mods**, all switched on by `IfModActive` or `IfModActiveAll` in the mod's `LoadFolders.xml`, plus a
  handful of tie-ins in C# that check for a mod by name. D'thorga Radio has none (Vanilla Expanded Framework's pipe system is a hard
  dependency there, not a tie-in).
- **No tie-in hard-references another author's assembly.** C# finds their types and Defs by name through
  `src/PhobosFramework/OptionalMods.cs`; code that must name their types sits in its own assembly under the tie-in's `1.7` folder.
- **A patch for another author's fault** is a tie-in too, and is listed here as one, but its record is
  [patches for other authors' faults](temporary-patches.md), which says when each can go.
- **Keep this register current**: a change that adds, removes or widens a tie-in adds or edits its row in the same commit.

## How a tie-in is built here

| Need | How | Example |
| --- | --- | --- |
| Defs or patches only when the mod is active | A `Mods/<Other>/` folder listed in `LoadFolders.xml` with `IfModActive` | `mods/PhobosMedical/Mods/MoreInjuries` |
| Needs two mods at once | `IfModActiveAll="A,B"` | `mods/PhobosFramework/Mods/VehiclesHygiene` |
| One entry in a shared Def | `MayRequire="packageId"` on the list entry | Substitutes in `mods/PhobosFramework/Mods/VehicleFramework/Common/Defs/Phobos/Framework/VehicleSystems/PartSupplies.xml` |
| A patch to their Def | `PatchOperationFindMod`, or a patch inside the tie-in folder | Factory Additions' piped chemfuel and neutroamine |
| C# that reads their state | `OptionalMods.IsActive`, type and Def lookups by name | `src/PhobosFramework/Gases/CombatExtendedSmoke.cs` |
| C# that must name their types | A separate assembly loaded only from the tie-in's `1.7` folder | `src/PhobosPatches.Irbm`, `src/PhobosFramework.Vehicles.Pipes` |

## D'thorga Framework

| Other mod (packageId) | What the tie-in does | Kind | Where |
| --- | --- | --- | --- |
| Vehicle Framework (`SmashPhil.VehicleFramework`) | Every vehicle system: loadouts on any of its vehicles, parts, health, fittings, tunes, the sling, the fleet board, research gates; its caravan world objects | C# + XML | `Mods/VehicleFramework` |
| Dubs Bad Hygiene (`Dubwise.DubsBadHygiene`) | The shared plumbing reader and the plumbing laid beneath our things | C# | `Mods/DubsBadHygiene/1.7` |
| Vehicle Framework + Dubs Bad Hygiene | Vehicles take water from plumbing; tanker ports for plumbing and septic tanks; the survey truck's groundwater sensor | C# | `Mods/VehiclesHygiene/1.7` |
| Vehicle Framework + Rimefeller (`dubwise.rimefeller`) | The drill rig's oil-well job | C# | `Mods/VehiclesRimefeller/1.7` |
| Vehicle Framework + Vanilla Expanded Framework (`OskarPotocki.VanillaFactionsExpanded.Core`) | Parked tankers and rigs join its pipe networks; vehicles refuel from a chemfuel line | C# | `Mods/VehiclesPipes/1.7` |
| Vehicle Framework + Vanilla Expanded Framework | The vehicle garage outpost and its button on vehicle caravans | C# + XML | `Mods/VehiclesOutposts` |
| Vehicles at Work (`shengchipangxieyaotuke.vehiclesatwork`) | Its trips leave out vehicles our systems refuse to move, and a called-off trip gives our reason (0.108.4, SD1) | C#, by name | `src/PhobosFramework.Vehicles/Service/VehiclesAtWorkPatches.cs` |
| Vehicle Framework + RimHUD (`Jaxe.RimHUD`) | A RimHUD value for the vehicle a pawn is in | XML | `Mods/VehiclesRimHUD/Common` |
| Combat Extended (`CETeam.CombatExtended`) | Its smoke inhalation and smoke sensitivity read by our gas rules; vehicle hit weights and blast penetration under its armour | C#, by name | `src/PhobosFramework/Gases/CombatExtendedSmoke.cs`, `MaskFactor.cs`, `CombatExtendedRules` |
| Any mod with a cough sound | Our coughs stand down when another mod sounds one (the list is empty today) | C# | `src/PhobosFramework/Sounds/PhobosSounds.cs` |

## D'thorga Vehicles

All XML; the code is in the Framework.

| Other mod (packageId) | What the tie-in does | Where |
| --- | --- | --- |
| Vanilla Vehicles Expanded (`OskarPotocki.VanillaVehiclesExpanded`) | Loadouts for its vehicles and aircraft, empty masses, open cabs | `Mods/VanillaVehiclesExpanded` |
| Vanilla Vehicles Expanded - Tier 3 (`OskarPotocki.VanillaVehiclesExpandedTier3`) | Loadouts for the Cherokee (its sling stands down for `yaroslav.cherokeesling`), Louie, Goliath, Big Rig and the rest; climate-controlled cabs; masses | `Mods/VanillaVehiclesExpandedTier3` |
| HUMVEE (`hmv.lunartika`) | Loadout | `Mods/HUMVEE` |
| RimThunder Roaring Tiger (`aoba.rimthunder.roca`), Desert Sabre (`rimthunderteams.rimthunder.desertsabre`) | Loadouts | `Mods/RimThunderRoaringTiger`, `Mods/RimThunderDesertSabre` |
| Harrier VTOL (`potemkinvillager.av8bharrier`), Hind (`potemkinvillager.mi24hindhelicopter`), Mongoose (`potemkinvillager.mongooseattackhelicopter`) | Aircraft loadouts | `Mods/HarrierVtol`, `Mods/HindHelicopter`, `Mods/MongooseHelicopter` |
| GTek L-ATV (`killerbot.jltv`), MaxxPro (`killerbot.navistarmaxxpro`) | Loadouts | `Mods/GTekLATV`, `Mods/GTekMaxxPro` |
| Frontline Workshop (`serinairo.frontlineworkshop`), BMP-1 (`turbo.rimworld.bmp1ifv`) | Loadouts | `Mods/FrontlineWorkshop`, `Mods/BMP1` |
| Trains of the Rim (`princeofpluto.vehicles.trains`) | Boilers for its locomotives | `Mods/TrainsOfTheRim` |
| Dubs Bad Hygiene | Tanker contents: water and fecal sludge; the survey truck's groundwater node | `Mods/DubsBadHygiene` |
| Rimefeller | Napalm and crude in barrels; the drill rig's well-kit node | `Mods/Rimefeller` |
| Vanilla Chemfuel Expanded (`vanillaexpanded.vchemfuele`) | Deepchem, the drill rig's deepchem head, chemfuel through its pipes | `Mods/VanillaChemfuelExpanded` |
| Vanilla Gravship Expanded (`vanillaexpanded.gravship`) | Astrofuel and oxygen tanker contents; pressure vessels | `Mods/VanillaGravshipExpanded`, `Mods/TankerVessels` |
| Combat Extended (`ceteam.combatextended`) | FSX and prometheum tanker contents; pressure vessels | `Mods/CombatExtended`, `Mods/TankerVessels` |
| Vanilla Nutrient Paste Expanded (`vanillaexpanded.vnutriente`), VRE Sanguophage (`vanillaracesexpanded.sanguophage`), Biotech (`ludeon.rimworld.biotech`) | Nutrient paste, hemogen and wastepack tanker contents | `Mods/VanillaNutrientPasteExpanded`, `Mods/VanillaRacesSanguophage`, `Mods/Biotech` |
| Vanilla Expanded Framework | The garage outpost Def | `Mods/VehicleOutposts` |
| Vanilla Vehicles Expanded, VFE Power (`vanillaexpanded.vfepower`), RimThunder Core (`RimThunder.Core`) | `MayRequire` research prerequisites | `Defs/Phobos/Vehicles/Research.xml` |
| RimThunder, Vanilla Vehicles Expanded, Rimefeller, Vanilla Gravship Expanded | Their engines, transmissions, tyres, wheels, radiators, batteries, alternators, tanks, suspension, wiring, exhausts, castings, synthylene and oxygen canisters stand in for our parts, by role | Framework's `PartSupplies.xml` |

## D'thorga Medical

| Other mod (packageId) | What the tie-in does | Kind | Where |
| --- | --- | --- | --- |
| More Injuries (`Th3Fr3d.ExtendedInjuries`) | Fluids with its saline bag; the decompression kit for its collapsed lung; eschar can cause its gangrene; oxygen treats its hypoxia; triage finds its brain therapy; tracheotomy waits on its research | XML | `Mods/MoreInjuries` |
| More Injuries | Respects its fire-inhalation setting; our ischaemia stands down; the save bridge guards its first-load wipe | C#, by name | `src/PhobosMedical/BurnsAndSmoke`, `SaveBridge` |
| Vanilla Gravship Expanded | The whole piped-oxygen chain (hyperbaric chamber, electrolyser, bedside stand, trolley, field kit, research); a one-time recommendation letter when it is missing | XML + C# | `Mods/VanillaGravshipExpanded`, `RecommendationService.cs` |
| Dubs Bad Hygiene | The safety shower and electrolyser draw plumbed water; its showers and baths count as safety showers; its washing decontaminates | C# + XML | `Mods/DubsBadHygiene` |
| Combat Extended | Its incendiary burn joins our burn care; its smoke inhalation counts as bronchospasm and needs oxygen | XML | `Mods/CombatExtended` |
| Useful Marks (`Andromeda.UsefulMarks`) | Triage tags show as its marks; our overlay stands down | C# | `Mods/UsefulMarks/1.7` |
| Dubs Rimatomics (`Dubwise.Rimatomics`) | The radiation control line and its designators | XML (+ Framework C#) | `Mods/Rimatomics` |
| Atmosphere Overhaul (`acutus.AtmosphereOverhaul.release`), Celsius (`Garwel.Celsius`) | Our fire-heat zones stand down | XML list read by C# | `BurnsAndSmoke/Rules.xml` |

## D'thorga Factory Additions

Hard dependencies: Vanilla Expanded Framework and VFE Factory. All XML unless marked.

| Other mod (packageId) | What the tie-in does |
| --- | --- |
| [sbz] Fridge (`sbz.NeatStorageFridge`) | The corpse fridge is a factory hopper |
| Medicines+ (`Papito.MedicinesPlus.AT`), with Deep Pills (`ODs.DeepPills`, `MayRequire`) | Drug-lab recipes as medicine granulator bills |
| Ushanka's Biological Warfare (`Ushanka.BiologicalWarfare`) | Vaccines, antiserums and pills as granulator bills |
| Combat Extended | Psycho as a granulator bill |
| Dubs Rimatomics | Potassium iodide bill; its machining table's bills at the fabrication bay |
| Hemofungus (`NoSleep.Hemofungus`) | Hemogen from hemofungus |
| Complementary Odyssey (`MrHydralisk.ComplementaryOdyssey`) | Vacsop bills |
| Glitterworld Medicine Production (`MrHydralisk.GlitterworldMedicineProduction`) | Glitterworld medicine bill |
| Farmable Neutroamine (`kongkim.FarmableNeutroamine`) | Neutroamine synthesizer bill |
| Passive Exoskeletons (`AgentOrange.PassiveExoskeletons`) | Polyamide on the biofuel refinery |
| Simple Washing Machine (`local.washingmachine`) | Detergents on the refinery |
| Vanilla Cooking Expanded (`VanillaExpanded.VCookE`) | Simple soup on the conveyor oven |
| Vanilla Chemfuel Expanded (`VanillaExpanded.VChemfuelE`) | Our bills draw piped chemfuel; VFE Factory's chemfuel steps re-applied where a Combat Extended clash skips them |
| VRE Android (`vanillaracesexpanded.android`) | Granulator bills draw piped neutroamine |
| Dubs Bad Hygiene | **C# + XML**: factory machines draw plumbed water; food bills wait for water and carry food-poisoning rules |

## D'thorga Water Tap

| Other mod (packageId) | What the tie-in does |
| --- | --- |
| VFE Factory (`VanillaExpanded.VFEFactory`) | The bottle tap is a factory hopper (XML) |

## D'thorga Patches

C# lives in `src/PhobosPatches.*`. Rows marked **fault** are in [patches for other authors' faults](temporary-patches.md).

| Other mod (packageId) | What the tie-in does | Kind |
| --- | --- | --- |
| Harrier VTOL | Build cost (RimThunder's turbine where present), part list, Combat Extended bomb weights and cases, bombs and rockets at The Dead Man's Switch's ammunition printer and lathe; hover gate, sortie and flight-control rules, crew keeps a map open | XML + C# |
| Harrier, Hind, Mongoose | Rearm one ammunition kind at a time (**fault**) | C# |
| RimThunder (`RimThunder.Core`) | Heavy castings in batches at DMS Expedition's lathe | XML |
| Vehicle Framework | Fires a vehicle can never lose (**fault**, temporary); the game's progress bar on its work (**fault**) | C# |
| Spaceship Turret Hull + Combat Extended (`MK.SpaceshipTurretHull`) | Combat Extended support for its turret hulls | XML |
| Combat Extended | Guard against empty ammunition-set entries, Synthriada's fault (**fault**, temporary) | C# |
| InterRim Ballistic Missile (`kazepsi.irbm`) | Paced strategic sweep (**fault**); our blast model for the extinction warhead; quiet "area revealed" letters; Rimatomics fallout and flash-blindness; Combat Extended wall fragments setting; RimPacts assault size after a strike | C# + XML |
| RimPacts (`wowgag.RimPacts`) | War quest countdown; arrival-filter fix (**fault**); the sortie gate says why | C# |
| Visit Settlements (`ninagoblin.visitsettlements`) | Visited map stays open during a battle (**fault**); food-drop guard (**fault**) | C# |
| Prison Labor (`avius.prisonlabor`) | Crash guard (**fault**, temporary) | C# |
| Vanilla Expanded Framework outposts + Vehicle Framework | Refuses founding an outpost from a caravan holding a vehicle (a setting) | C# |

## Local only, outside the repository

Not published and not in any mod: the DMS Expedition long-range cannon roof fix and launch-site odds, kept in `.local/` on the owner's
PC (tested in game 11 Nov 2026 for the roof fix).
