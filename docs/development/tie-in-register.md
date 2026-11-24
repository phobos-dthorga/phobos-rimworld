# Tie-ins with other mods: the register (13 Nov 2026)

**A register of what exists, not a design.** Every place one of our mods works with another author's mod when that mod is active,
read from each mod's `LoadFolders.xml`, the `MayRequire` entries in our Defs and the optional-mod checks in our C# on 13 November
2026. Most tie-ins are **not yet tested in game**; each mod's changelog says which are. New links waiting on the owner are in
[soft-dependency cross-links](soft-dependency-crosslinks.md) and [air-defence tie-ins](air-defence-tie-ins.md).

## Read this first

- **87 tie-in folders across eight mods** (recounted 25 Nov 2026: Vehicles 30, Patches 21, Factory Additions 16, Framework 10,
  Medical 7, Farming 1, Radio Framework 1, Water Tap 1; 102 entries in the `LoadFolders.xml` lists, because a folder and its `1.7` or
  `Common` subfolder are listed as separate entries), all switched on by `IfModActive` or `IfModActiveAll` in the mod's
  `LoadFolders.xml`, plus a handful of tie-ins in C# that check for a mod by name. D'thorga Radio Framework's first, Vehicle
  Framework, came in its 0.10.0 (Vanilla Expanded Framework's pipe system is a hard dependency there, not a tie-in).
- **No tie-in hard-references another author's assembly.** C# finds their types and Defs by name through
  `src/PhobosFramework/OptionalMods.cs`; code that must name their types sits in its own assembly under the tie-in's `1.7` folder.
- **A patch for another author's fault** is a tie-in too, and is listed here as one, but its record is
  [patches for other authors' faults](temporary-patches.md), which says when each can go.
- **Keep this register current**: a change that adds, removes or widens a tie-in adds or edits its row in the same commit.
  **The gate holds the folders to it** (`tests/test_tie_in_register.py`, 24 Nov 2026): each mod's section must name every
  conditional folder in its `LoadFolders.xml` as `Mods/<Folder>`, name none that is gone, and the counts above must match.
  C# tie-ins that check for a mod by name are not checked.

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
| Vehicle Framework + Dubs Bad Hygiene | Vehicles take water from plumbing; tanker ports for plumbing and septic tanks; the survey truck's groundwater sensor; hidden plumbing beneath a parked trailer that takes water (0.111.0) | C# | `Mods/VehiclesHygiene/1.7` |
| Vehicle Framework + Rimefeller (`dubwise.rimefeller`) | The drill rig's oil-well job; the parked tanker lays Rimefeller's hidden pipeline and pumps crude and chemfuel through it (0.109.0, SD4) | C# | `Mods/VehiclesRimefeller/1.7` |
| Vehicle Framework + Vanilla Expanded Framework (`OskarPotocki.VanillaFactionsExpanded.Core`) | Parked tankers and rigs join its pipe networks; vehicles refuel from a chemfuel line | C# | `Mods/VehiclesPipes/1.7` |
| Vehicle Framework + Vanilla Expanded Framework | The vehicle garage outpost and its button on vehicle caravans | C# + XML | `Mods/VehiclesOutposts` |
| Vehicles at Work (`shengchipangxieyaotuke.vehiclesatwork`) | Its trips leave out vehicles our systems refuse to move, and a called-off trip gives our reason (0.108.4, SD1, VW1); a vehicle with a depot bay is parked in it when it brings one home, and a bay counts as a lot for its Drive work (0.176.0, VW2); its Drive work fetches drivers for our waiting errands, its drivers lists are obeyed, and its trips leave a waiting truck alone (0.177.0, VW3) | C#, by name; XML (`Mods/VehiclesAtWork`) | `src/PhobosFramework.Vehicles/Service/VehiclesAtWorkPatches.cs` |
| Vehicle Framework + RimHUD (`Jaxe.RimHUD`) | A RimHUD value for the vehicle a pawn is in | XML | `Mods/VehiclesRimHUD/Common` |
| Vehicle Framework + Biotech (`Ludeon.RimWorld.Biotech`) | Mechlinked vehicles (0.282.0): the mech control link fitting and its research, after basic mechtech ([autonomous vehicles by radio](autonomous-vehicles.md), step 7b) | XML | `Mods/VehiclesBiotech/Common` |
| Vehicle Framework + The Dead Man's Switch - Expedition (`Aoba.DeadManSwitch.Expedition`) | Aircraft in flight on the world map are tracks its air defence sees and fires at, through its published `AirDefenseExtensionDef`; the warning receiver hears its radars; chaff and flares cut its hit chance and slow its locks; its launch-risk prompt before our take-offs; a closed sky (Fortified Features Framework's alert counter, by name) refuses take-off (0.119.0; AD2, AD3, AD7) | C# + XML | `Mods/VehiclesDmse` |
| RimThunder (`rimthunder.core`, `aoba.rimthunder.roca`, `rimthunderteams.rimthunder.desertsabre`) | Its vehicles are never stalled by EMP, as its authors chose, but still take the military effect (0.109.0, SD9) | XML list read by C# | `Rules_VehicleSystems.xml` |
| Combat Extended (`CETeam.CombatExtended`) | Its smoke inhalation and smoke sensitivity read by our gas rules; vehicle hit weights and blast penetration under its armour; its ammunition system setting and which ammunition cooks off, read for ammunition cooking off in a burning vehicle's cargo (Framework 0.113.0, cross-links G13); its public list of hit boxes, joined so it shoots at an armoured vehicle's full width (Framework 0.154.0) | C#, by name | `src/PhobosFramework/Gases/CombatExtendedSmoke.cs`, `MaskFactor.cs`, `CombatExtendedRules`, `src/PhobosFramework/CombatExtendedAmmo.cs`, `src/PhobosFramework.Vehicles/Armour/CombatExtendedHitWidth.cs` |
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
| EA-6B Prowler (`potemkinvillager.ea6bprowler`) | Aircraft loadout and its ICAP upgrades (0.99.0) | `Mods/EA6BProwler` |
| Close Air Support (`potemkinvillager.CloseAirSupport`) | Loadouts for the A-10 and Su-25, which start as their early versions, and their service-life fittings | `Mods/CloseAirSupport` |
| UH-60 Black Hawk (`z3nt3.uh60blackhawk`), C-17 Globemaster III (`z3nt3.c17globemaster`) | Aircraft loadouts | `Mods/UH60Blackhawk`, `Mods/C17Globemaster` |
| GTek L-ATV (`killerbot.jltv`), MaxxPro (`killerbot.navistarmaxxpro`) | Loadouts | `Mods/GTekLATV`, `Mods/GTekMaxxPro` |
| Frontline Workshop (`serinairo.frontlineworkshop`), BMP-1 (`turbo.rimworld.bmp1ifv`) | Loadouts | `Mods/FrontlineWorkshop`, `Mods/BMP1` |
| RimArrow: Search and Rescue (`rimarrow.srt`) | Helicopter loadouts for the Ka-27PS and the Mi-38: turboshafts, rotor drive, hydraulics, sling, hook mount, litter cabin (0.65.0) | `Mods/RimArrowSearchRescue` |
| Trains of the Rim (`princeofpluto.vehicles.trains`) | Boilers for its locomotives; steam engines never stalled by EMP (0.52.0) | `Mods/TrainsOfTheRim` |
| Dubs Bad Hygiene | Tanker contents: water and fecal sludge; the survey truck's groundwater node; the Pintle water bowser (0.55.0, dropped in its lite mode) | `Mods/DubsBadHygiene` |
| Rimefeller | Napalm and crude in barrels; the drill rig's well-kit node; crude and chemfuel through its pipeline (0.52.0) | `Mods/Rimefeller` |
| Vanilla Chemfuel Expanded (`vanillaexpanded.vchemfuele`) | Deepchem, the drill rig's deepchem head, chemfuel through its pipes | `Mods/VanillaChemfuelExpanded` |
| Vanilla Gravship Expanded (`vanillaexpanded.gravship`) | Astrofuel and oxygen tanker contents; pressure vessels | `Mods/VanillaGravshipExpanded`, `Mods/TankerVessels` |
| Combat Extended (`ceteam.combatextended`) | FSX and prometheum tanker contents; pressure vessels; the service trucks' part armour in millimetres (Vehicles 0.76.0) | `Mods/CombatExtended`, `Mods/TankerVessels` |
| Vanilla Nutrient Paste Expanded (`vanillaexpanded.vnutriente`), VRE Sanguophage (`vanillaracesexpanded.sanguophage`), Biotech (`ludeon.rimworld.biotech`) | Nutrient paste, hemogen and wastepack tanker contents | `Mods/VanillaNutrientPasteExpanded`, `Mods/VanillaRacesSanguophage`, `Mods/Biotech` |
| Vanilla Expanded Framework | The garage outpost Def | `Mods/VehicleOutposts` |
| Vanilla Expanded Framework | The bulk bunker is a factory hopper (Pipe System's mark), for VFE Factory's belts and machines (0.63.0, UH2) | `MayRequire` in `Defs/Phobos/Vehicles/BulkBunker/BulkBunker.xml` |
| The Dead Man's Switch (`aoba.deadmanswitch.core`) | The bulk bunker links to its automatic benches as a material source (0.63.0, UH2) | `Mods/DeadManSwitch` |
| D'thorga Radio Framework (`PhobosDthorga.RadioFramework`) | The remote control kit on our five service trucks (0.117.0): its upgrade node, and the vehicle control radio each truck carries once it is fitted ([autonomous vehicles by radio](autonomous-vehicles.md), step 4) | `Mods/RadioFramework` |
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
| Dubs Rimatomics (`Dubwise.Rimatomics`) | The radiation control line and its designators; potassium iodide taken before going in (0.16.0, SD21) | XML (+ Framework C#) | `Mods/Rimatomics` |
| VFE Medical (`vanillaexpanded.vfemedical`) | Its intravenous drip stand keeps a burn-shock patient on fluids (0.16.0, SD23) | XML list read by C# | `BurnsAndSmoke/Rules.xml` |
| Search and Rescue (`ch4acko3.searchandrescue`) | Its field responders use our hand-held kits, as two lanes in its Field rescue work type (0.17.0, SD22) | XML (work givers in its work type) | `Mods/SearchAndRescue` |
| Atmosphere Overhaul (`acutus.AtmosphereOverhaul.release`), Celsius (`Garwel.Celsius`) | Our fire-heat zones stand down | XML list read by C# | `BurnsAndSmoke/Rules.xml` |

## D'thorga Farming

| Other mod (packageId) | What the tie-in does | Where |
| --- | --- | --- |
| Combat Extended (`ceteam.combatextended`) | The combine's armour in Combat Extended's figures (2 mm, 4 MPa; farm machines stop no bullet) | `Mods/CombatExtended` |

## D'thorga Factory Additions

Hard dependencies: Harmony, D'thorga Framework, Vanilla Expanded Framework and VFE Factory. All XML unless marked.

| Other mod (packageId) | What the tie-in does | Where |
| --- | --- | --- |
| [sbz] Fridge (`sbz.NeatStorageFridge`) | The corpse fridge is a factory hopper | `Mods/SbzFridge` |
| Medicines+ (`Papito.MedicinesPlus.AT`), with Deep Pills (`ODs.DeepPills`, `MayRequire`) | Drug-lab recipes as medicine granulator bills | `Mods/MedicinesPlus` |
| Ushanka's Biological Warfare (`Ushanka.BiologicalWarfare`) | Vaccines, antiserums and pills as granulator bills | `Mods/BiologicalWarfare` |
| Combat Extended | Psycho as a granulator bill | `Mods/CombatExtended` |
| Dubs Rimatomics | Potassium iodide bill; its machining table's bills at the fabrication bay | `Mods/Rimatomics` |
| Hemofungus (`NoSleep.Hemofungus`) | Hemogen from hemofungus | `Mods/Hemofungus` |
| Complementary Odyssey (`MrHydralisk.ComplementaryOdyssey`) | Vacsop bills | `Mods/ComplementaryOdyssey` |
| Glitterworld Medicine Production (`MrHydralisk.GlitterworldMedicineProduction`) | Glitterworld medicine bill | `Mods/GlitterworldMedicine` |
| Farmable Neutroamine (`kongkim.FarmableNeutroamine`) | Neutroamine synthesizer bill | `Mods/FarmableNeutroamine` |
| Passive Exoskeletons (`AgentOrange.PassiveExoskeletons`) | Polyamide on the biofuel refinery | `Mods/PassiveExoskeletons` |
| Simple Washing Machine (`local.washingmachine`) | Detergents on the refinery | `Mods/WashingMachine` |
| Vanilla Cooking Expanded (`VanillaExpanded.VCookE`) | Simple soup on the conveyor oven | `Mods/VanillaCookingExpanded` |
| Vanilla Chemfuel Expanded (`VanillaExpanded.VChemfuelE`) | Our bills draw piped chemfuel; VFE Factory's chemfuel steps re-applied where a Combat Extended clash skips them | `Mods/VanillaChemfuelExpanded` |
| VRE Android (`vanillaracesexpanded.android`) | Granulator bills draw piped neutroamine | `Mods/VanillaRacesExpandedAndroid` |
| Vehicle Framework (`SmashPhil.VehicleFramework`) | Every part made at D'thorga Framework's vehicle parts bench as a bill on the automated fabrication bay (0.13.0, SD30) | `MayRequire` in `BenchBills_FabricationBay.xml` (no folder) |
| Dubs Bad Hygiene | **C# + XML**: factory machines draw plumbed water; food bills wait for water and carry food-poisoning rules | `Mods/DubsBadHygiene`, its `1.7` assembly |
| The Dead Man's Switch (`aoba.deadmanswitch.core`) | Factory hoppers link to its automatic benches as material sources; its feed hopper is a factory hopper whose filter belts obey (0.14.0, UH6) | `Mods/DeadManSwitch` |

## D'thorga Radio Framework

Hard dependencies: D'thorga Framework and Vanilla Expanded Framework.

| Other mod (packageId) | What the tie-in does | Where |
| --- | --- | --- |
| Vehicle Framework (`SmashPhil.VehicleFramework`) | Radios fitted to vehicles (0.10.0): a vehicle whose def names a radio fitting is built with that radio, and joins the radio net once its upgrade is done; its own assembly, `src/PhobosRadio.Vehicles` ([autonomous vehicles by radio](autonomous-vehicles.md), step 2). Since 0.15.0 (step 6) also the remote control kit fitting for any vehicle with a D'thorga loadout, in `Common` | `Mods/VehicleFramework/Common`, `Mods/VehicleFramework/1.7` |

## D'thorga Water Tap

Hard dependencies: D'thorga Framework and Dubs Bad Hygiene.

| Other mod (packageId) | What the tie-in does | Where |
| --- | --- | --- |
| VFE Factory (`VanillaExpanded.VFEFactory`) | The bottle tap is a factory hopper (XML) | `Mods/VFEFactory` |

## D'thorga Patches

C# lives in `src/PhobosPatches.*`. Rows marked **fault** are in [patches for other authors' faults](temporary-patches.md).

| Other mod (packageId) | What the tie-in does | Kind | Where |
| --- | --- | --- | --- |
| Harrier VTOL | Build cost (RimThunder's turbine where present), part list, Combat Extended bomb weights and cases, bombs and rockets at The Dead Man's Switch's ammunition printer and lathe; hover gate, sortie and flight-control rules, crew keeps a map open; its dodge chance follows its controls and warning receiver, with a chaff and flares button (0.21.0, A12, A16) | XML + C# | `Mods/HarrierVtol` |
| Harrier, Hind, Mongoose | Rearm one ammunition kind at a time (**fault**) | C# | `Mods/AircraftRearm/1.7` |
| Harrier, Hind, Mongoose, Close Air Support, EA-6B Prowler, with Combat Extended | Bombs and missiles built from parts at their real weight; the missile seeker head | XML (`Mods/AircraftMunitions`) | `Mods/HindHelicopter`, `Mods/MongooseHelicopter` (their missiles) |
| Close Air Support (`potemkinvillager.CloseAirSupport`) | Shell cost, parts and parts bill for the A-10 and Su-25; realistic handling, a setting (off) | XML + C# (its settings) | `Mods/CloseAirSupport` |
| UH-60 Black Hawk, C-17 Globemaster III | Shell cost and parts bill | XML | `Mods/UH60Blackhawk`, `Mods/C17Globemaster` |
| EA-6B Prowler | Parts list, parts bill and shell cost; rearm one kind at a time; its dodge follows its controls and warning receiver, with a chaff and flares button; the ICAP upgrades widen its jamming field; realistic handling, off (0.33.0) | XML + C# | `Mods/EA6BProwler` |
| RimThunder (`RimThunder.Core`) | Heavy castings in batches at DMS Expedition's lathe | XML | `Mods/RimThunder` |
| Vehicle Framework | Fires a vehicle can never lose (**fault**, temporary); the game's progress bar on its work (**fault**) | C# | `Mods/VehicleFramework` |
| Spaceship Turret Hull + Combat Extended (`MK.SpaceshipTurretHull`) | Combat Extended support for its turret hulls | XML | `Mods/SpaceshipTurretHull` |
| Combat Extended | Guard against empty ammunition-set entries, Synthriada's fault (**fault**, temporary) | C# | `Mods/CombatExtended` |
| InterRim Ballistic Missile (`kazepsi.irbm`) + Combat Extended | Its close-in weapon systems shoot down CE's shells (0.19.1, AD14; **fault**, temporary; a setting) | C# | `Mods/IRBM` |
| InterRim Ballistic Missile (`kazepsi.irbm`) | Paced strategic sweep (**fault**); our blast model for the extinction warhead; quiet "area revealed" letters; Rimatomics fallout and flash-blindness; Combat Extended wall fragments setting; RimPacts assault size after a strike | C# + XML | `Mods/IRBM` |
| RimPacts (`wowgag.RimPacts`) | War quest countdown; arrival-filter fix (**fault**); the sortie gate says why | C# | `Mods/RimPacts/1.7` |
| Visit Settlements (`ninagoblin.visitsettlements`) | Visited map stays open during a battle (**fault**); food-drop guard (**fault**) | C# | `Mods/VisitSettlements/1.7` |
| Vanilla Factions Expanded - Classical (`OskarPotocki.VFE.Classical`) | A scorpion raid saved mid-raid loads with its orders (**fault**) | C# | `Mods/Classical/1.7` |
| Nice Research Tab (`andromeda.niceresearchtab`) | A lock and blank removal around its cached unlock list (**fault**) | C# | `Mods/NiceResearchTab/1.7` |
| Biotech (`ludeon.rimworld.biotech`) | Mech rechargers draw real power by the mech's body size (a setting, off) | XML + C# | `Mods/Biotech` |
| Fortified Features Framework (`AOBA.Framework`) | Haulers keep its drone platforms, and mechanitors its store-carrying mechanoids, topped up with steel (0.22.0, mechanoids with Framework 0.135.0; a `PhobosTopUpDef` naming three members of its platform comp, read by D'thorga Framework) | XML, by name | `Mods/Fortified` |
| Vanilla Expanded Framework outposts + Vehicle Framework | Refuses founding an outpost from a caravan holding a vehicle (a setting) | C# | `Mods/Outposts` |

## Local only, outside the repository

Not published and not in any mod: the DMS Expedition long-range cannon roof fix and launch-site odds, kept in `.local/` on the owner's
PC (tested in game 11 Nov 2026 for the roof fix).
