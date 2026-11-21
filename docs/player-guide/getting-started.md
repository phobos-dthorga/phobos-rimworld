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
  Framework, and its About file names the lowest version it accepts.
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
   chose need ([the table and diagram](../../README.md#the-mods)).
3. **D'thorga Framework.**
4. The other D'thorga mods, after the mods they change. Each About file asks for this, so RimSort does it.

**From the Steam Workshop, once the pages exist:**

1. **Subscribe to the mod, and to what it needs** (Harmony, D'thorga Framework, Vanilla Expanded Framework and so on). Its README
   says which.
2. **Turn it on** in the Mods menu or in RimSort, in the order above.
3. **Read the settings below before loading a save.**

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
| D'thorga Framework | Remove the mods that need it first. If vehicles used the systems, load the colony, press **Remove the added parts from this colony's vehicles** in its vehicle settings, then save. **Take every vehicle out of every garage**, or they go with it; likewise **recover every sling wreck**, **let a water drop run land and unload every litter**, and **take any extra fuel tank off**. Sell or use up jump-start kits and booster packs if you want a clean log |
| D'thorga Vehicles | **Scrap every one of our vehicles** (tanker, drill rig, tipper, survey truck, generator set, seabed crawler): they go with the mod. Pack up a set-up generator set first, or its cable end is left behind. Empty every garage |
| D'thorga Medical | In its settings press **Remove every condition this mod adds, from every pawn**. Deconstruct its machines and burn unit beds; sell, use or destroy its items |
| D'thorga Factory Additions | Remove the bills it added from each machine's Processes tab and let running batches finish. Deconstruct every deep factory hopper and fabrication bay |
| D'thorga Water Tap | Deconstruct every tap, and **lay a pipe back where each stood**, or the plumbing is cut in two |
| D'thorga Patches | Delete bomb, rocket and heavy-casting bills it added. With Combat Extended: use up or sell bomb cases and bomb fills, and deconstruct turret hulls built under it |
| D'thorga Radio Framework | Deconstruct every radio building and drop every hand-held |

## When something goes wrong

- **Read [help and troubleshooting](../../SUPPORT.md).** It says where the game's log is, and what a useful report needs.
- **Several other mods quietly stopped working at once since 17 November 2026?** That is a bug in Harmony 3.0.3.0 with Loading
  Progress, not in our mods; [the fix](../../SUPPORT.md#harmony-3030-and-loading-progress) is one launch option. Our mods don't need it.
- **Every D'thorga mod writes one line to the log as it loads**, naming its version and the optional mods it found. That line is
  the first thing to check.
- **Every button of ours that refuses says why**: rest the mouse on it. A building's inspect pane names the one thing it waits
  for.
