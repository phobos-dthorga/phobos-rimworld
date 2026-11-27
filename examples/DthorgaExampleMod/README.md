# D'thorga Framework example mod

A small, complete RimWorld 1.7 mod that builds on D'thorga Framework (also spelt dthorga), for you to copy and rename. It is XML
only: no C#, no art, nothing to compile. **Not yet tested in game.** Walk through it step by step in [your first
hour](../../docs/modding/first-hour.md).

## Read this first (gotchas)

- **Needs Vehicle Framework and D'thorga Framework**, loaded before it. D'thorga Framework brings Harmony as its own dependency.
- **The vehicle part needs Vanilla Vehicles Expanded** (the Wisent truck). Without it, only the lantern loads, with no errors: the
  vehicle files sit in a folder loaded only while that mod is active.
- **Rename before you publish**: the `packageId` first, then the `DthorgaExample_` prefix. Never change either once players have
  your mod in a save.
- **The rack mount part is appended last.** Vehicle Framework saves a vehicle's parts by position, so a part you add to a vehicle
  that saves already hold goes at the end (`<last>true</last>`). Never insert or reorder parts.
- **Removing the example from a save**: the vehicle systems drop saved parts that no loadout appends any more, at the next load, so
  the rack mount goes, and the cargo rack fitted on it should go with it (not yet tested). Try it on a throwaway colony, not one you
  care about.

## What it shows

| File | What it is | Copied from |
| --- | --- | --- |
| `About/About.xml` | The mod's identity, its dependencies and load order | D'thorga Vehicles' About file |
| `LoadFolders.xml` | Loads the vehicle files only with Vanilla Vehicles Expanded | D'thorga Vehicles' `LoadFolders.xml` |
| `Defs/Buildings_ExampleLantern.xml` | **A lantern that lights only in the dark**, with D'thorga Framework's night light. It needs no vehicle | The Framework's camp lantern; [guide](../../docs/framework-modding.md#a-light-that-lights-only-in-the-dark-framework-02310) |
| `Mods/VanillaVehiclesExpanded/Defs/Loadouts_Example.xml` | **A loadout**: the Wisent gets a 140 A alternator and 140 Ah of battery, and a rack mount part of its own | D'thorga Vehicles' Big Rig loadout and the Framework's heavy land loadout; [guide](../../docs/vehicle-systems-modding.md#a-loadout-for-your-vehicle) |
| `Mods/VanillaVehiclesExpanded/Defs/Fittings_Example.xml` | **A fitting**: a cargo rack on that mount, 10% more cargo and 2% slower | The Framework's heavy-duty springs and auxiliary fuel tank; [guide](../../docs/vehicle-systems-modding.md#a-fitting-for-your-own-vehicle) |

Every figure is an example, authored, not a real truck's.

**No translation keys.** None of these Defs has a field that takes a key: labels and descriptions stay in the Defs, in English, for
translators to override with `DefInjected`. A key is needed where a field asks for one, such as an `Effect_Message`'s `message` in
a system of your own ([effects](../../docs/vehicle-systems-modding.md#effects)); put those in `Languages/English/Keyed/`.

## Try it

1. **Copy the `DthorgaExampleMod` folder** into the game's `Mods` folder (where `RimWorld.exe` is; on Steam, `steamapps\common\RimWorld\Mods`).
2. **Enable it after its dependencies**: Harmony, Vehicle Framework, D'thorga Framework, then Vanilla Vehicles Expanded if you have
   it, then this mod. The game's mod list warns you if the order is wrong.
3. **Turn on Development mode** (Options), start or load a throwaway colony, and read the log for red lines that name
   `DthorgaExample`.

## What to rename first

1. **`packageId`** in `About/About.xml`: `YourName.YourModName`. It is your mod's identity, held by saves and other mods' load
   orders.
2. **`name`, `author` and `description`** in the same file. Keep Ludeon's two disclaimer sentences in the description.
3. **The `DthorgaExample_` prefix** on every `defName`, the part role and the part `key`. Use your own short prefix. A `defName`
   must never end in a digit.
4. **The folder name**, to match your mod.
5. **`VVE_Wisent`** in the loadout's `vehicleDefs`, to your own vehicle, and move the loadout and fitting to a folder that loads
   with the mod that defines it (or to `Defs/` if your own mod defines it).

## What to look for in game

- **The lantern**: in the Architect's **Furniture** tab, as "example lantern". Build it and fill it with chemfuel. It stays dark by
  day and lights at dusk; its fuel goes down only while it is lit.
- **The loadout**: with Vanilla Vehicles Expanded, spawn a Wisent from the debug actions menu, or build one, select it and run the dev action
  **Vehicle systems: log selected** (in the debug actions menu, under **D'thorga**). The log line names the loadout
  `DthorgaExample_Wisent_Loadout` and lists the role `DthorgaExample_RackMount`. The Wisent's Health tab shows a **rack mount**.
- **The fitting**: open **Fittings and tunes** from the Wisent's Dashboard. The **cargo rack** is offered once **vehicle tuning** is
  researched. Or skip the work with the dev action **Vehicle systems: fit now (no materials or work)**: it lists only what the selected
  vehicle is offered, so a fitting missing there is one the offer check refused. Fitted, the Wisent's cargo capacity goes up.

## Where next

- [Your first hour](../../docs/modding/first-hour.md): this example, step by step, with dev mode, the log and the offline checks.
- [Stability: what you can rely on](../../docs/modding/stability.md): which names will not change under you.
- The guides: [D'thorga Framework for modders](../../docs/framework-modding.md), [vehicle systems for
  modders](../../docs/vehicle-systems-modding.md) and [vehicle features for modders](../../docs/vehicle-features-modding.md).
- The reference site, with every field of every class: <https://phobos-dthorga.github.io/phobos-rimworld/>.

Portions of the materials used to create this content/mod are trademarks and/or copyrighted works of Ludeon Studios Inc. All rights
reserved by Ludeon. This content/mod is not official and is not endorsed by Ludeon.
