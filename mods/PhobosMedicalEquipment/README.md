# D'thorga Medical Equipment

Hospital equipment for the injuries D'thorga Burns and Smoke and More Injuries bring. This first version gives patients oxygen.

Version 0.1.0, built 5 November 2026 for House D'thorga. **Not yet tested in game.** Everything here is read from the game's and
the mods' code.

## Read this first (gotchas)

- **Needs D'thorga Framework and Vanilla Expanded Framework,** loaded before it. The About file asks for that order. No Harmony.
- **Safe to add to a running colony.**
- **Before removing it: deconstruct the machines, press the settings button "Remove every oxygen condition from every pawn", save, then switch the mod off.**
- **Build the machine touching a hospital bed,** as you would a vitals monitor. One machine per bed, though it can serve two beds it touches. Any bed that accepts a vitals monitor works.
- **Nobody has to be told.** A patient in that bed who needs oxygen gets it. Use the switch to stop it.
- **The bedside stand only exists with Vanilla Gravship Expanded,** and needs its Oxygen Network research. Without that mod, use the concentrator.
- **It does not repair lungs,** and **it cannot help if the airway is closed**: that patient needs a tracheotomy.
- **Move patients out of the burning room.** A patient on oxygen still takes in the carbon monoxide around them.
- **Oxygen canisters explode when they burn.** That is Vanilla Gravship Expanded's rule, and a good reason to keep stands away from fires.
- **The art is a placeholder.**

## What it does

| Machine | Needs | Serves | Research |
| --- | --- | --- | --- |
| Oxygen concentrator | Power | Up to 2 patients | Medicine production |
| Bedside oxygen stand | 1 oxygen canister per patient per in-game hour, or an oxygen pipe | Up to 2 patients | Medicine production and Oxygen Network |

**Who gets it:** a patient in a linked bed with a condition oxygen helps, or with breathing under 80% from any cause. The worst breathing comes first. Conditions it helps: from D'thorga Burns and Smoke, carbon monoxide poisoning, smoke-damaged lungs, pneumonia and smoke inhalation; Combat Extended's smoke inhalation; More Injuries' organ hypoxia.

**What it does for them:** breathing rises by 20 points, up to normal. With D'thorga Burns and Smoke, carbon monoxide clears with a half-life of about 75 minutes, against about 320 on room air. The figures for clearance are sourced (see the [design record](../../docs/development/medical-equipment.md)); the rest are authored.

## Settings

The display mode, and the uninstall button. Plain mode shows no figures.

## For modders

Give a building `Phobos.Medical.Comps.CompProperties_PhobosOxygenSupply` and `CompProperties_Facility` to make it an oxygen machine; add a `CompProperties_Refuelable` and it burns fuel. Mark your own condition with `Phobos.Framework.Breathing.PhobosOxygenResponsiveExtension` so that it counts as needing oxygen. Balance lives in `Common/Defs/Phobos/Medical/Rules.xml` and on each machine's comp.

## Checks done (5 November 2026)

- Built against the RimWorld 1.7 and 1.6 reference assemblies; the game-free rules pass their unit tests; the patches pass the repository's checks.
- **Not checked:** anything in game. The link to beds, the pipe link, the inspect text and the feel of the numbers are all untested.

Community-created content. Portions of the materials used to create this content/mod are trademarks and/or copyrighted works of Ludeon Studios Inc. All rights reserved by Ludeon. This content/mod is not official and is not endorsed by Ludeon.
