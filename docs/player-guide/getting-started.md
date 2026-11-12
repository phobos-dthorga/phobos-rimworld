# Getting started

Install the mods, pick the settings that are hard to change later, and know how to take a mod out again.
[Back to the guide](README.md).

## Read this first (gotchas)

- **Close RimWorld before installing or updating.** The install script refuses while the game runs, so a half-copied mod never
  loads.
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
```

The script finds the game for you in the usual Steam folder; [installing the mods](../installing-mods.md) has the other ways to
point it at the game, and where it keeps a backup of whatever it replaces. **Code → Download ZIP on GitHub gives you source, not a
working mod.**

Then start RimWorld, open **Mods**, switch on what you want with what it needs, and let the sorter order them:

1. Harmony, Core and the DLCs (the game puts these first).
2. **Vanilla Expanded Framework, Vehicle Framework, Dubs Bad Hygiene, VFE - Factory**: whichever of these the D'thorga mods you
   chose need ([the table](README.md#the-mods)).
3. **D'thorga Framework.**
4. The other D'thorga mods, after the mods they change. Each About file asks for this, so RimSort does it.

When the Steam Workshop pages exist, you will subscribe to a mod and to what it needs, and the order above still applies.

## Settings to decide before a colony

Most settings can change at any time. These are the ones that are awkward to change in the middle of a colony, or that you should
know about before your colonists meet them. Find them under **Options > Mod settings**.

| Mod and section | Setting | Starts as | Why decide now |
| --- | --- | --- | --- |
| D'thorga Framework: vehicle systems | **Which vehicles** (all, Only D'thorga vehicles, per mod, per kind) | All | **Needs a restart.** Every Vehicle Framework vehicle, from any mod, gains a battery, coolant, oil, tyres, brakes and wear unless you narrow this. Switching vehicles off later is safe |
| D'thorga Framework: vehicle systems | **Engines and drivetrains** | Full | A laden truck in sand, mud or snow slows, down to a crawl. **Drivetrain shown, speeds unchanged** keeps every vehicle at its author's speeds |
| D'thorga Framework: vehicle systems | When an aircraft is too heavy or too hot to lift off or hover onto its pad | Use the runway | A heavy jet on a hot day lands on a runway instead of its pad, and a laden helicopter may refuse to lift off. "Allow it, at a cost to the engine" and "warn only" are the others |
| D'thorga Framework: vehicle systems | Jump-start leads must be crafted / Every vehicle carries a booster pack | On / Off | A flat battery needs a **jump-start kit** or a **booster pack** you made beforehand. Make one before you need it |
| D'thorga Framework: vehicle systems | Destroyed parts need replacing | On | A destroyed engine stops a vehicle until a new one is made and fitted |
| D'thorga Framework: vehicle systems | Boiler explosions / Realistic feed water | Off / Off | Steam locomotives (Trains of the Rim) only. On, a boiler run dry can explode, and a tender takes its real 68,000 litres |
| D'thorga Framework | Parts for building | Other mods' parts where installed | **Needs a restart.** Where RimThunder or Vanilla Vehicles Expanded makes a part, our vehicles ask for theirs. "Always our own parts" asks only for ours |
| Vehicle systems, D'thorga Medical, the radio, each on its own | How much to show | Plain | How much each screen shows: Plain, Operator or Engineer ([what they mean](README.md#words-this-guide-uses)). Change it whenever you like |
| D'thorga Medical: emergency dosing | People in pain fetch their own painkillers | On | **Check your drug policies first.** A colonist on their feet in pain takes the best painkiller their policy allows, so addiction and overdose are real risks |
| D'thorga Medical: burns and smoke | Tox gas acts fast | Off | Off (slow-acting), gas stings, seems to pass, and **the harm comes from about 4 hours later**, over two or three days. Keep gassed colonists in bed |
| D'thorga Medical: burns and smoke | Fires heat the air around them in the open | On | Standing beside a blaze in the open now hurts; crops near it stop growing |
| D'thorga Medical: triage | How far the doctors may go without orders, by default | To save life (guests: never) | Doctors queue only life-saving operations unless you raise it, per person or by group |
| D'thorga Radio Framework | Metres per map cell | 1 | **Changes which links work.** Changing it mid-colony changes every contact's range |

Two settings you may never touch, but should know about:

- **D'thorga sounds volume** (D'thorga Framework, 100%): one slider for every D'thorga sound, from silent to twice as loud.
- **Keep the game's log going after its message limit** (D'thorga Framework, on): RimWorld stops logging after 10,000
  messages; this restarts it, up to 25 times a session, so a report still has the end of the story.

## Research

Our research projects sit on their own **D'thorga** tab in the research window, and submarines on **D'thorga: Submarines**. An
existing colony that had already researched what one of our projects replaced gets that project free, once, when it loads.

| Project | After | Opens |
| --- | --- | --- |
| Vehicle parts | Machining | The vehicle parts bench, its engine assembly, transmission, drivetrain and electrical kits, and the inspection pit |
| Vehicle electrics | Electricity | The generator truck's research |
| Vehicle tuning | Vehicle parts | The dyno bay, for tunes and grades |
| Heavy-duty parts | Vehicle parts | Heavy-duty grades of parts |
| Vehicle depots | Vehicle parts, Electricity | The depot bay |
| Booster packs | Batteries | The booster pack |
| Turbine engines | Fabrication, Vehicle parts | The gas turbine assembly |
| Tipper hauler, bulk tanker, generator truck, drill rig, survey truck | Vehicle parts, and Electricity, vehicle electrics, Deep drilling or Ground-penetrating scanner | Each working vehicle ([working vehicles](working-vehicles.md)) |
| Submersible hull and its branch | Vehicle parts, Batteries | The Fathom seabed crawler and its upgrades ([submarines](submarines.md)) |
| Clinical equipment, respiratory care, burn care, anaesthesia, bronchodilators, piped oxygen | Medicine production, Hospital bed, Vitals monitor, Drug production, Oxygen Network | D'thorga Medical's equipment ([medical](medical.md#equipment)) |
| Local radio | Microelectronics basics | Radios, the base station and antennas ([radio](radio.md)) |

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
| D'thorga Framework | Remove the mods that need it first. If vehicles used the systems, load the colony, press **Remove the added parts from this colony's vehicles** in its vehicle settings, then save. **Take every vehicle out of every garage**, or they go with it. Sell or use up jump-start kits and booster packs if you want a clean log |
| D'thorga Vehicles | **Scrap every one of our vehicles** (tanker, drill rig, tipper, survey truck, generator set, seabed crawler): they go with the mod. Pack up a set-up generator set first, or its cable end is left behind. Empty every garage |
| D'thorga Medical | In its settings press **Remove every condition this mod adds, from every pawn**. Deconstruct its machines and burn unit beds; sell, use or destroy its items |
| D'thorga Factory Additions | Remove the bills it added from each machine's Processes tab and let running batches finish. Deconstruct every deep factory hopper and fabrication bay |
| D'thorga Water Tap | Deconstruct every tap, and **lay a pipe back where each stood**, or the plumbing is cut in two |
| D'thorga Patches | Delete bomb, rocket and heavy-casting bills it added. With Combat Extended: use up or sell bomb cases, and deconstruct turret hulls built under it |
| D'thorga Radio Framework | Deconstruct every radio building and drop every hand-held |

## When something goes wrong

- **Read [help and troubleshooting](../../SUPPORT.md).** It says where the game's log is, and what a useful report needs.
- **Every D'thorga mod writes one line to the log as it loads**, naming its version and the optional mods it found. That line is
  the first thing to check.
- **Every button of ours that refuses says why**: rest the mouse on it. A building's inspect pane names the one thing it waits
  for.
