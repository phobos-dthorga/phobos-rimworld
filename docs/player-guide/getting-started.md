# Getting started

Install the mods, pick the settings that are hard to change later, and know how to take a mod out again.
[Back to the guide](README.md).

## Read this first (gotchas)

- **RimWorld 1.7 only.** No mod here supports 1.6.
- **Close RimWorld before installing or updating.** The install script refuses while the game runs, or while a File Explorer window
  is open inside an installed mod's folder, so a half-copied mod never loads.
- **Back up your save first**, every time you add, remove or update a mod. Saves are in
  `%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Saves`, which most cloud sync tools skip.
- **Update every D'thorga mod together.** They are built as a set: D'thorga Vehicles, for example, needs a recent D'thorga
  Framework, and an older Framework beside a newer Vehicles is not tested.
- **Finish or cancel any vehicle or depot bay under construction before you update.** What they are built from can change
  between versions; a frame keeps what was already delivered and then asks for the new list.
- **The install script never edits your mod list.** Switch the mods on yourself in the game's Mods menu or in RimSort.

## Installing

**Nothing is on the Steam Workshop yet.** Today every mod is built from source and copied into the game. You need Git, Python 3
and a .NET SDK that builds `net10.0`.

```text
git clone https://github.com/phobos-dthorga/phobos-rimworld.git
cd phobos-rimworld
python scripts/deploy.py --build --apply --all          build every mod, then copy every one into the game
python scripts/deploy.py --build --apply PhobosMedical  build and copy one mod (its folder under mods/)
python scripts/deploy.py --all                          only list what would be copied
```

**Code → Download ZIP on GitHub gives you source, not a working mod**: the built files are not kept on GitHub.

- **Where the game is.** The script looks in this order: `--game <folder>`, the `RIMWORLD_PATH` environment variable,
  `"rimworldPath"` in `.local/install-settings.json`, then the usual Steam folder.
- **Backups.** Whatever it replaces is copied to `.local/deploy-backups/` first.
- **Textures.** If RimSort is installed, the script also prepares our textures the way RimSort would, so you can skip RimSort's
  texture step for our mods. Without RimSort the mods still work.
- **More detail** for building: [building, testing and deploying](../development/building.md).

Then start RimWorld, open **Mods**, switch on what you want with what it needs, and let the sorter order them:

1. Harmony, Core and the DLCs (the game puts these first).
2. **Vanilla Expanded Framework, Vehicle Framework, Dubs Bad Hygiene, VFE - Factory**: whichever of these the D'thorga mods you
   chose need ([the table below](#what-each-mod-needs), or [the diagram](README.md#the-mods)).
3. **D'thorga Framework.**
4. The other D'thorga mods, after the mods they change. Each About file asks for this, so RimSort does it.

**From the Steam Workshop, once the pages exist:**

1. **Subscribe to the mod, and to what it needs** (Harmony, D'thorga Framework, Vanilla Expanded Framework and so on). Its README
   says which.
2. **Turn it on** in the Mods menu or in RimSort, in the order above.
3. **Read the settings below before loading a save.**

## What each mod needs

**Switch on everything in a mod's Needs column too**, or the game warns and the mod does not work. The rest is optional: Loads
after and Loads before only set the order when those mods are on, and Works with names the mods that switch on extra content of
ours when they are on. This table is made from each mod's own files, so it is always current.

<!-- generated: table mod-requirements -->
Generated from each mod's own files by `python scripts/doc-tables.py --write`: change those files, not this table.

| Mod | Package ID | Needs | Loads after | Loads before | Game versions | Works with (optional) |
| --- | --- | --- | --- | --- | --- | --- |
| D'thorga Factory Additions | `PhobosDthorga.FactoryAdditions` | Harmony, Vanilla Expanded Framework, D'thorga Framework, Vanilla Furniture Expanded - Factory | RimWorld, Royalty, Ideology, Biotech, Anomaly, Odyssey, D'thorga Framework, Harmony, Vanilla Expanded Framework, Vanilla Chemfuel Expanded, Vanilla Furniture Expanded - Factory, VFE Factories: Buffs and tweaks, Adaptive Storage Framework, [sbz] Fridge, Medicines+, Deep Pills, Ushankas Biological Warfare, Combat Extended, Dubs Rimatomics, Hemofungus, Complementary Odyssey, Glitterworld Medicine Production, Farmable Neutroamine, Passive Exoskeletons, [DG]Simple Washing Machine, Vanilla Cooking Expanded, Dubs Bad Hygiene, Vanilla Races Expanded - Android | none | 1.7 | [DG]Simple Washing Machine, [sbz] Fridge, Combat Extended, Complementary Odyssey, Dubs Bad Hygiene, Dubs Rimatomics, Farmable Neutroamine, Glitterworld Medicine Production, Hemofungus, Medicines+, Passive Exoskeletons, The Dead Man's Switch, Ushankas Biological Warfare, Vanilla Chemfuel Expanded, Vanilla Cooking Expanded, Vanilla Races Expanded - Android |
| D'thorga Farming | `PhobosDthorga.Farming` | Vehicle Framework, D'thorga Framework | RimWorld, Vanilla Expanded Framework, Vehicle Framework, D'thorga Framework, Vanilla Cooking Expanded, Vanilla Plants Expanded, Vanilla Plants Expanded - More Plants | none | 1.7 | Combat Extended |
| D'thorga Framework | `PhobosDthorga.Framework` | Harmony | RimWorld, Harmony, Combat Extended, Dubs Bad Hygiene, Vehicle Framework, Vanilla Expanded Framework, Rimefeller, The Dead Man's Switch - Expedition | none | 1.7 | Dubs Bad Hygiene, Vehicle Framework, Vehicle Framework and Biotech, Vehicle Framework and Dubs Bad Hygiene, Vehicle Framework and Rimefeller, Vehicle Framework and RimHUD, Vehicle Framework and The Dead Man's Switch - Expedition, Vehicle Framework and Vanilla Expanded Framework, Vehicle Framework and Vehicles at Work |
| D'thorga Medical | `PhobosDthorga.Medical` | Harmony, D'thorga Framework, Vanilla Expanded Framework | RimWorld, Harmony, Vanilla Expanded Framework, D'thorga Framework, Combat Extended, More Injuries (Continued), More Injuries: Organ Failure, Medical System Expansion 2, Search and Rescue, Hospitality (Continued), Vanilla Gravship Expanded - Chapter 1, Vanilla Furniture Expanded - Medical Module, Dubs Bad Hygiene, Useful Marks | none | 1.7 | Combat Extended, Dubs Bad Hygiene, Dubs Rimatomics, More Injuries (Continued), Search and Rescue, Useful Marks, Vanilla Gravship Expanded - Chapter 1 |
| D'thorga Patches | `PhobosDthorga.Patches` | Harmony, D'thorga Framework | RimWorld, Biotech, Harmony, D'thorga Framework, Vanilla Expanded Framework, Vehicle Framework, Vanilla Vehicles Expanded, RimThunder - Core, RimPacts - Diplomacy Overhaul, Combat Extended, `potemkinvillager.fixedwingaircraft`, Harrier VTOL Attack Aircraft, Spaceship Turret Hull, InterRim Ballistic Missile, Hind Helicopter Gunship, Mongoose Attack Helicopter, Close Air Support: A-10 Warthog & Su-25 Frogfoot, EA-6B Prowler Electronic Attack Aircraft, UH-60M Black Hawk (Vehicle Framework), C-17 Globemaster III (Vehicle Framework), Visit Settlements, Vanilla Factions Expanded - Classical, Nice Research Tab | none | 1.7 | Biotech, C-17 Globemaster III (Vehicle Framework), Close Air Support: A-10 Warthog & Su-25 Frogfoot, Combat Extended, EA-6B Prowler Electronic Attack Aircraft, Fortified Features Framework, Harrier VTOL Attack Aircraft, Harrier VTOL Attack Aircraft or Hind Helicopter Gunship or Mongoose Attack Helicopter or Close Air Support: A-10 Warthog & Su-25 Frogfoot or EA-6B Prowler Electronic Attack Aircraft, Hind Helicopter Gunship, InterRim Ballistic Missile, Mongoose Attack Helicopter, Nice Research Tab, RimPacts - Diplomacy Overhaul, RimThunder - Core, Spaceship Turret Hull and Combat Extended, UH-60M Black Hawk (Vehicle Framework), Vanilla Expanded Framework and Vehicle Framework, Vanilla Factions Expanded - Classical, Vehicle Framework, Visit Settlements |
| D'thorga Radio Framework | `PhobosDthorga.RadioFramework` | D'thorga Framework, Vanilla Expanded Framework | RimWorld, Royalty, Ideology, Biotech, Anomaly, Odyssey, Harmony, Vanilla Expanded Framework, D'thorga Framework, Vehicle Framework, Combat Extended | none | 1.7 | Combat Extended, Vehicle Framework |
| D'thorga Vehicles | `PhobosDthorga.Vehicles` | Vehicle Framework, D'thorga Framework | RimWorld, Vanilla Expanded Framework, Vehicle Framework, D'thorga Framework, Vanilla Vehicles Expanded, Vanilla Vehicles Expanded - Tier 3, HUMVEE, RimThunder - Roaring Tiger, RimThunder - Desert Sabre - Continued, Trains of the Rim, Harrier VTOL Attack Aircraft, Hind Helicopter Gunship, Mongoose Attack Helicopter, Close Air Support: A-10 Warthog & Su-25 Frogfoot, EA-6B Prowler Electronic Attack Aircraft, UH-60M Black Hawk (Vehicle Framework), C-17 Globemaster III (Vehicle Framework), GTek's Vehicles - Oshkosh L-ATV, GTek's Vehicles - Navistar MaxxPro MRAP, Frontline Workshop, `turbo.rimworld.bmp1ifv`, Rimarrow: Search & Rescue | none | 1.7 | Biotech, C-17 Globemaster III (Vehicle Framework), Close Air Support: A-10 Warthog & Su-25 Frogfoot, Combat Extended, D'thorga Radio Framework, Dubs Bad Hygiene, EA-6B Prowler Electronic Attack Aircraft, Frontline Workshop, GTek's Vehicles - Navistar MaxxPro MRAP, GTek's Vehicles - Oshkosh L-ATV, Harrier VTOL Attack Aircraft, Hind Helicopter Gunship, HUMVEE, Mongoose Attack Helicopter, Rimarrow: Search & Rescue, Rimefeller, RimThunder - Desert Sabre - Continued, RimThunder - Roaring Tiger, The Dead Man's Switch, Trains of the Rim, `turbo.rimworld.bmp1ifv`, UH-60M Black Hawk (Vehicle Framework), Vanilla Chemfuel Expanded, Vanilla Expanded Framework, Vanilla Gravship Expanded - Chapter 1, Vanilla Gravship Expanded - Chapter 1 or Combat Extended, Vanilla Nutrient Paste Expanded, Vanilla Races Expanded - Sanguophage, Vanilla Vehicles Expanded, Vanilla Vehicles Expanded - Tier 3 |
| D'thorga Water Tap | `PhobosDthorga.WaterTap` | D'thorga Framework, Dubs Bad Hygiene | RimWorld, Dubs Bad Hygiene, D'thorga Framework, Vanilla Expanded Framework, Vanilla Furniture Expanded - Factory | none | 1.7 | Vanilla Furniture Expanded - Factory |

**Needs** must be switched on too. **Loads after** and **Loads before** only set the order when those mods are on; they are not needed. **Works with** lists the mods that switch on extra content of this mod when they are on ("or": any one of them; "and": all of them).
<!-- end generated -->

## Settings to decide before a colony

Most settings can change at any time. These are the ones that are awkward to change in the middle of a colony, or that you should
know about before your colonists meet them. Find them under **Options > Mod settings**: one entry per mod, each with its pages down
the left. **Type in the search box to find a setting on any page**, tick **Changed only** to see what you have changed (marked with a
dot), and **Reset this page to defaults** puts a page back. A setting that needs a restart says so beside its name, and closing the
window after changing one lists them.

| Mod and page | Setting | Starts as | Why decide now |
| --- | --- | --- | --- |
| D'thorga Vehicle Systems: Which vehicles | **Which vehicles** (all, Only D'thorga vehicles, per mod, per kind) | All | **Needs a restart.** Every Vehicle Framework vehicle, from any mod, gains a battery, coolant, oil, tyres, brakes and wear unless you narrow this. Switching vehicles off later is safe |
| D'thorga Vehicle Systems: Engines and driving | **How engines and drivetrains work** | Full | A laden truck in sand, mud or snow slows, down to a crawl. **Drivetrain shown, speeds unchanged** keeps every vehicle at its author's speeds |
| D'thorga Vehicle Systems: Aircraft and sling loads | When an aircraft is too heavy or too hot to lift off, hover onto its pad or land at a journey's end | Use the runway | A heavy jet on a hot day lands on a runway instead of its pad, a laden helicopter may refuse to lift off, and one that cannot hover where it arrives comes in for a rolling landing. "Allow it, at a cost to the engine" and "warn only" are the others |
| D'thorga Vehicle Systems: Engines and driving | Jump-start leads must be crafted / Every vehicle carries a booster pack | On / Off | A flat battery needs a **jump-start kit** or a **booster pack** you made beforehand. Make one before you need it |
| D'thorga Vehicle Systems: Repairs, service and fittings | Destroyed parts need replacing | On | A destroyed engine stops a vehicle until a new one is made and fitted |
| D'thorga Vehicle Systems: Steam and electric | Boiler explosions / Realistic feed water | Off / Off | Steam locomotives (Trains of the Rim) only. On, a boiler run dry can explode, and a tender takes its real 68,000 litres |
| D'thorga Framework: General | Parts for building | Other mods' parts where installed | **Needs a restart.** Where RimThunder or Vanilla Vehicles Expanded makes a part, our vehicles ask for theirs. "Always our own parts" asks only for ours |
| D'thorga Vehicle Systems (Display, sound and stories), D'thorga Medical (General), the radio, each on its own | How much to show | Plain | How much each screen shows: Plain, Operator or Engineer ([what they mean](README.md#words-this-guide-uses)). Change it whenever you like |
| D'thorga Medical: Emergency dosing | People in pain fetch their own painkillers | On | **Check your drug policies first.** A colonist on their feet in pain takes the best painkiller their policy allows, so addiction and overdose are real risks |
| D'thorga Medical: Caustic gas | Tox gas acts fast | Off | Off (slow-acting), gas stings, seems to pass, and **the harm comes from about 4 hours later**, over two or three days. Keep gassed colonists in bed |
| D'thorga Medical: Burns and smoke | Fires heat the air around them in the open | On | Standing beside a blaze in the open now hurts; crops near it stop growing |
| D'thorga Medical: Triage | How far the doctors may go without orders, by default | To save life (guests: never) | Doctors queue only life-saving operations unless you raise it, per person or by group |
| D'thorga Radio Framework | Metres per map cell | 1 | **Changes which links work.** Changing it mid-colony changes every contact's range |

Two settings you may never touch, but should know about:

- **D'thorga sounds volume** (D'thorga Framework, 100%): one slider for every D'thorga sound, from silent to twice as loud.
- **Keep the game's log going after its message limit** (D'thorga Framework, on): RimWorld stops logging after 10,000
  messages; this restarts it, up to 25 times a session, so a report still has the end of the story.
- **Say when D'thorga code was part of an error** (D'thorga Framework, on): after an error that passed through D'thorga's code,
  the log says so in one line, so you know to tell D'thorga about it too, and not only the other mod's author.

## Standby power

D'thorga Framework makes idle buildings use less power, and puts indoor lights out when no one is in the room. It is on by default.

- **Lights:** an indoor light goes dark when no one awake is in its room, and comes on within about a second when someone walks in.
  A sleeping colonist doesn't need it. Outdoor lights and sun lamps stay as they are.
- **Machines:** a workbench, research bench or television draws its full power only while someone works at it or watches it; a
  turret only while it has a target; a hydroponics basin only while something grows in it; a vitals monitor only while a bed beside
  it is occupied; a powered door only while it stands open. Idle, each draws **10%** of its power. Its power line shows the lower
  figure, and its description says it is on standby.
- **Nothing stops working.** A turret on standby still fires, a door still opens at once, and a bench starts at full power the
  moment someone starts work.
- **Find it under Options > Mod settings > D'thorga Framework > General, Standby power**: a switch for each kind, and how much an
  idle building draws (0% to 30%).
- **It switches itself off while LightsOut or Turn It On and Off - RePowered is active**, as they do the same job. Switch those off
  to use it.
- Safe to add to a colony and to remove from one.

## Research

Our research projects sit on their own **D'thorga** tab in the research window, and submarines on **D'thorga: Submarines**, which is
set aside for now and shows only in a colony that has already researched some of it ([submarines](submarines.md)). An
existing colony that had already researched what one of our projects replaced gets that project free, once, when it loads.

| Project | After | Opens |
| --- | --- | --- |
| Vehicle parts | Machining | The vehicle parts bench, its engine assembly, transmission, drivetrain and electrical kits, hydraulic actuator, electric motor (and, after Fabrication, the flight computer), and the inspection pit |
| Vehicle electrics | Electricity | The generator truck's research |
| Vehicle tuning | Vehicle parts | The dyno bay, for tunes and grades |
| Heavy-duty parts | Vehicle parts | Heavy-duty grades of parts |
| Vehicle depots | Vehicle parts, Electricity | The depot bay |
| Booster packs | Batteries | The booster pack |
| Turbine engines | Fabrication, Vehicle parts | The gas turbine assembly and the turbine blade set it is built around, the reduction gearbox and the hot-section kit |
| Tipper hauler, bulk tanker, generator truck, drill rig, survey truck | Vehicle parts, and Electricity, vehicle electrics, Deep drilling or Ground-penetrating scanner | Each working vehicle ([working vehicles](working-vehicles.md)) |
| Submersible hull and its branch | Vehicle parts, Batteries | The Fathom seabed crawler and its upgrades ([submarines](submarines.md)) |
| Clinical equipment, respiratory care, burn care, anaesthesia, bronchodilators, piped oxygen | Medicine production, Hospital bed, Vitals monitor, Drug production, Oxygen Network | D'thorga Medical's equipment ([medical](medical.md#equipment)) |
| Local radio | Microelectronics basics | Radios, the base station and antennas ([radio](radio.md)) |

**Vehicle upgrades wait for the research they name**, on every vehicle, ours or another mod's (the Harrier's incendiary and EMP
bombs, for two). Select the upgrade and the **Upgrade** button names the project still to finish; pressing it says the same.

## Adding a mod to a running colony

**Every D'thorga mod is safe to add to a colony you are already playing.** Back up the save, switch the mod on, load. Two
things to know:

- **D'thorga Medical replaced five older mods** (Burns and Smoke, Emergency Dosing, Triage, Medical Equipment, Medical Save
  Bridge). **Switch all five off and D'thorga Medical on in the same change.** Your colony and settings carry over.
- **D'thorga Patches replaced D'thorga Vehicle Patches and D'thorga Missile Patches** the same way: both off, this one on.

RimSort warns you if an old one is left on.

## Removing a mod

**Do these steps first, then save, then switch the mod off.** Without them the colony still loads, with error lines in the log
for what went missing, and with vehicles the save may fail to load.

| Mod | Before you remove it |
| --- | --- |
| D'thorga Framework | Remove the mods that need it first. If vehicles used the systems, load the colony, press **Remove the added parts from this colony's vehicles** in **D'thorga Vehicle Systems** settings, then save. Before that, **delete every vehicle park and service yard zone**, **take every vehicle out of every garage**, **recover every sling wreck, lost convoy and depot** on the world map, **let a water drop run land and unload every litter** (switch off **Take casualties aboard** first, or carriers keep bringing more), and **take any extra fuel tank off**: whatever is left in them goes with the mod. Jump-start kits and booster packs vanish with an error line each, so sell or use them up for a clean log. A line per missing map component on the next load is harmless |
| D'thorga Vehicles | **Scrap every one of our vehicles** (tipper, tanker, drill rig, survey truck, generator set, seabed crawler, every trailer, the Osprey): they go with the mod. Pack up a set-up generator set first, or its cable end is left behind. **Take every vehicle out of every garage**, and **empty and deconstruct every bulk bunker and loading chute**, or their goods are left on the ground. Other mods' vehicles go back to the general systems |
| D'thorga Medical | In its settings press **Remove every condition this mod adds, from every pawn**. Deconstruct its machines and burn unit beds; sell, use or destroy its items |
| D'thorga Factory Additions | Remove the bills it added from each machine's Processes tab and let running batches finish. Deconstruct every deep factory hopper and fabrication bay |
| D'thorga Water Tap | Deconstruct every tap, and **lay a pipe back where each stood**, or the plumbing is cut in two |
| D'thorga Patches | Delete bomb, rocket and heavy-casting bills it added. With Combat Extended: use up or sell bomb cases, bomb fills and missile seeker heads, and deconstruct turret hulls built under it |
| D'thorga Radio Framework | Deconstruct every radio building and drop every hand-held |

## When something goes wrong

- **Read [help and troubleshooting](../../SUPPORT.md).** It says where the game's log is, and what a useful report needs.
- **Several other mods quietly stopped working at once since 17 November 2026?** That is a bug in Harmony 3.0.3.0 with Loading
  Progress, not in our mods; [the fix](../../SUPPORT.md#harmony-3030-and-loading-progress) is one launch option. Our mods don't need it.
- **Every D'thorga mod writes one line to the log as it loads**, naming its version and the optional mods it found. That line is
  the first thing to check.
- **Every button of ours that refuses says why**: rest the mouse on it. A building's inspect pane names the one thing it waits
  for.

## Tested in game

What the owner has passed, by row of [the test list](../development/in-game-test-pass.md). A row passed **in part** says which part.

| Row | What the owner passed | Date |
| --- | --- | --- |
| 267 | The settings pages: search, **Changed only**, reset and the restart reminder | 21 Nov 2026 |
| 301 | **In part:** the D'thorga tab (UI Rethemed - Architect kept Motor pool apart until grouped in its Menu layout) | 20 Nov 2026 |
| 341 | Standby power: idle buildings use less, empty rooms go dark | 21 Nov 2026 |

Everything else in this chapter is not yet tested in game.
