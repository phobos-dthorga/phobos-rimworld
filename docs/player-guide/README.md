# D'thorga player guide

How to play with House D'thorga's RimWorld mods: what each one adds, what to build first, what to press, and what goes wrong
if you forget something. Also spelt **dthorga**, without the apostrophe.

This guide is for players. It says what the mods do **today**, without their history. **Who has the last word:**

| Question | The authority |
| --- | --- |
| How to play: what to build, what to press, what goes wrong | **This guide** |
| A mod's requirements, settings and figures, and what is tested | That mod's README |
| What changed, and when | That mod's changelog |

![Working trucks on a hillside at dusk: a drill rig and a tanker, with a surveyor in front](../../mods/PhobosVehicles/About/Preview.png)

## Read this first (gotchas)

- **Nothing is on the Steam Workshop yet.** Every mod installs from source today: see [getting started](getting-started.md).
- **Most of what this guide describes is not yet tested in game.** Each chapter ends with what the owner has tried in a real
  colony and what has only been checked offline. Treat everything else as "should work".
- **RimWorld 1.7 only.** No mod here supports 1.6.
- **D'thorga Framework goes first.** Every other D'thorga mod with code needs it, loaded above them. Each mod's About file asks
  for the right order, so RimSort and the game's own sorter get it right.
- **Back up your save before adding or removing any mod.** Your saves live under AppData, which most cloud sync tools skip.
- **A few settings are best decided before you start a colony**: which vehicles get the vehicle systems, the radio's map scale,
  and whether colonists fetch their own painkillers. The list is in
  [getting started](getting-started.md#settings-to-decide-before-a-colony).
- **Every mod is safe to add to a running colony. Removing one may need a step first.** The steps are in
  [getting started](getting-started.md#removing-a-mod).

## Chapters

What each mod does in a line, and how they depend on one another, is in [the project README](../../README.md#the-mods).

| Chapter | Mod or area | What it covers |
| --- | --- | --- |
| [Getting started](getting-started.md) | Every mod | Installing, load order, settings to decide before a colony, research, adding and removing a mod |
| [Looking after vehicles](vehicle-care.md) | D'thorga Framework, with Vehicle Framework | Keeping any mod's vehicles running: flat batteries, overheating, services, repairs, tuning |
| [Camping and life on the road](camping.md) | D'thorga Framework and D'thorga Vehicles, with Vehicle Framework | Tents, berths, camp gear, water, lights, the kitchen and life aboard on a long trip |
| [Working vehicles](working-vehicles.md) | D'thorga Vehicles | Finding ore, drilling it, hauling it, moving fluids and powering a camp with trucks and trailers |
| [Submarines](submarines.md) | D'thorga Vehicles and D'thorga Framework | Running a submarine: dive, hide, listen, ping, do research at sea |
| [Medical](medical.md) | D'thorga Medical | Keeping burned, gassed or badly hurt colonists alive, triage and hospital equipment |
| [Factory](factory.md) | D'thorga Factory Additions | Feeding factory machines from any storage and making bionics automatically |
| [Farming](farming.md) | D'thorga Farming | A combine that harvests a paddock row by row, and a chaser bin for its grain |
| [Water tap](water-tap.md) | D'thorga Water Tap | Bottled water kept ready, so nobody queues at the basin |
| [Patches](patches.md) | D'thorga Patches | What we fixed in other authors' mods, and what it changes for you |
| [Radio](radio.md) | D'thorga Radio Framework | Talking on the radio across the map |

## Words this guide uses

- **Button**: what the game calls a gizmo, the square buttons along the bottom when something is selected. Every button of ours
  says why when it is greyed out: rest the mouse on it.
- **Inspect pane**: the panel at the bottom left when something is selected. Ours always names the one thing a building or
  vehicle is waiting for.
- **Display mode**: most D'thorga mods show their workings at one of three depths, chosen in the mod settings. **Plain** shows
  bars and a reason in plain words, never a figure you must read. **Operator** adds the key figures. **Engineer** shows every figure
  and graph. The outcome is the same in every mode; only what you see changes.
- **The D'thorga research tab**: our research projects have their own tab in the research window, and submarines have
  **D'thorga: Submarines** (set aside for now: it shows only in a colony that has already researched some of it). An existing colony that had already done the game's research a project replaced gets that project
  free, once, so you lose nothing you could already build.
- **Not yet tested in game**: built and checked offline (builds, unit tests, reading the game's code), but not yet seen working in
  a real colony.

## About the pictures

Every picture in this guide is the mods' own art: the banners from each mod's About folder, and figures drawn from the textures the
mods ship, laid out by `scripts/art/guide_figures.py` ([how](images/README.md)). The art was made with generative AI and picked by
the owner; the record beside each texture says how. A vehicle with its upgrades is a guide's view: in the game a part may sit
or move slightly differently.

## Help

[Help and troubleshooting](../../SUPPORT.md) says where the game's log is and what a useful report needs.

Community-created content. Portions of the materials used to create this content/mod are trademarks and/or copyrighted works of
Ludeon Studios Inc. All rights reserved by Ludeon. This content/mod is not official and is not endorsed by Ludeon.
