# Your first hour with D'thorga Framework

From nothing to a working vehicle change in your own mod, built on D'thorga Framework (also spelt dthorga) and Vehicle Framework, for
RimWorld 1.7. It follows the example mod, [`examples/DthorgaExampleMod`](../../examples/DthorgaExampleMod/README.md): copy it, rename
it, load it, and see it work. Everything here is XML; no C# is needed. **The example is not yet tested in game.**

## Read this first (gotchas)

- **List D'thorga Framework in your `modDependencies` and load after it.** A mod that touches vehicles also lists Vehicle Framework
  (`SmashPhil.VehicleFramework`). D'thorga Framework brings Harmony as its own dependency.
- **Every vehicle already gets systems.** D'thorga Framework matches each vehicle to a default loadout by what it does (fuel, land, sea
  or air, mass). You write a loadout only to give a vehicle different figures, parts or systems.
- **A part you add to a vehicle goes last** (`<last>true</last>` on the part's `add`). Vehicle Framework saves a vehicle's parts by
  position; a part added in the middle hands its place's saved health to the next part. **Never insert or reorder parts.**
- **Name another mod's vehicles only from a folder loaded with that mod** (`IfModActive` in `LoadFolders.xml`), so players without it
  get no errors.
- **A misspelt vehicle name is not an error.** A loadout's `vehicleDefs` are plain names: a wrong one matches nothing, silently. Check
  with the dev action **Vehicle systems: log selected** (below).
- **Classes are named in full**, namespace included: `Phobos.VehicleSystems.PhobosVehicleLoadoutDef`,
  `Phobos.Framework.Environment.CompProperties_PhobosNightLight`.
- **Prefix every name with your own prefix**, and never end a `defName` in a digit (the game reads a thing's ID from the digits at the
  end of its name and number).
- **The schemas may still change before the first public release.** What you can rely on is in [Stability: what you can rely
  on](stability.md).

## 1. Where the files go

A mod is a folder in the game's `Mods` folder (on Steam, `steamapps\common\RimWorld\Mods`). The example's layout:

```text
DthorgaExampleMod/
  About/About.xml                         who the mod is, what it needs, its load order
  LoadFolders.xml                         which folders load, and when
  Defs/Buildings_ExampleLantern.xml       always loaded: a lantern with the Framework's night light
  Mods/VanillaVehiclesExpanded/Defs/
    Loadouts_Example.xml                  loaded only with Vanilla Vehicles Expanded: the Wisent's loadout
    Fittings_Example.xml                  ... and a fitting for the part that loadout adds
```

- **`LoadFolders.xml` is optional**; you need one only to load some folders conditionally. With one, the mod's root loads only if
  `/` is listed, as the example does.
- **Defs go in a folder named `Defs`** (any subfolders); patches in `Patches`; translation keys in `Languages/English/Keyed`.

## 2. Depending on D'thorga Framework

In `About/About.xml`, from the example:

```xml
<modDependencies>
  <li>
    <packageId>SmashPhil.VehicleFramework</packageId>
    <displayName>Vehicle Framework</displayName>
    <steamWorkshopUrl>steam://url/CommunityFilePage/3014915404</steamWorkshopUrl>
    <downloadUrl>https://github.com/SmashPhil/Vehicle-Framework</downloadUrl>
  </li>
  <li>
    <packageId>PhobosDthorga.Framework</packageId>
    <displayName>D'thorga Framework</displayName>
    <downloadUrl>https://github.com/phobos-dthorga/phobos-rimworld</downloadUrl>
  </li>
</modDependencies>
<loadAfter>
  <li>Ludeon.RimWorld</li>
  <li>SmashPhil.VehicleFramework</li>
  <li>PhobosDthorga.Framework</li>
  <li>OskarPotocki.VanillaVehiclesExpanded</li>
</loadAfter>
```

- **`modDependencies`** stops the game loading your mod without them. **`loadAfter`** puts your mod after every mod whose Defs it
  names, so its loadouts can inherit the Framework's (`ParentName="PhobosVehicleLoadoutLandHeavy"`) and name another mod's vehicles.
- **Change the example's `packageId` first** (`YourName.DthorgaExample`). It is your mod's identity: saves and other players' load
  orders hold it. Never change it once published.
- **A mod with no vehicle parts needs only D'thorga Framework.** The example's lantern uses a comp from the Framework's core, which
  needs no vehicle.

## 3. The load order

Enable, in this order:

1. Harmony
2. Vehicle Framework
3. D'thorga Framework
4. The mods whose vehicles you name (the example: Vanilla Vehicles Expanded)
5. Your mod

The game's mod list warns when an order breaks a `loadAfter` or a dependency is missing.

## 4. The example's three pieces

| Piece | What it does | Guide |
| --- | --- | --- |
| **The loadout** | The Wisent gets a 140 A alternator and 140 Ah of battery, and a **rack mount** part of its own, appended last | [A loadout for your vehicle](../vehicle-systems-modding.md#a-loadout-for-your-vehicle), [Part roles](../vehicle-systems-modding.md#part-roles) |
| **The fitting** | A **cargo rack** on that rack mount: 10% more cargo, 2% slower. Offered only on that loadout | [A fitting for your own vehicle](../vehicle-systems-modding.md#a-fitting-for-your-own-vehicle) |
| **The lantern** | Lights at dusk and burns chemfuel only while lit, with `CompProperties_PhobosNightLight` | [A light that lights only in the dark](../framework-modding.md#a-light-that-lights-only-in-the-dark-framework-02310) |

Every Def and field in it is copied from shipped XML; each file's comments name the source. Every figure is an example, authored.

**The loadout, trimmed.** It inherits the Framework's heavy land loadout and changes only what differs:

```xml
<Phobos.VehicleSystems.PhobosVehicleLoadoutDef ParentName="PhobosVehicleLoadoutLandHeavy">
  <defName>DthorgaExample_Wisent_Loadout</defName>
  <label>example heavy truck</label>
  <priority>Vehicle</priority>                     <!-- beats the defaults' Effect priority -->
  <selector>
    <vehicleDefs><li>VVE_Wisent</li></vehicleDefs> <!-- change to your vehicle -->
    <massMin>-1</massMin>                          <!-- the parent asks for mass 350 or more -->
  </selector>
  <systems Inherit="False">                        <!-- replaces the parent's list, so every system is listed again -->
    <li><def>PhobosVehicleSystem_Electrical</def><set><alternatorAmps>140</alternatorAmps> ... </set></li>
    ...
  </systems>
  <partRoles>
    <li>
      <role>DthorgaExample_RackMount</role>
      <add><key>DthorgaExample_RackMount</key><label>rack mount</label> ... <last>true</last></add>
    </li>
  </partRoles>
</Phobos.VehicleSystems.PhobosVehicleLoadoutDef>
```

**The fitting, trimmed.** It names the role, not the vehicle:

```xml
<Phobos.VehicleSystems.PhobosVehicleFittingDef>
  <defName>DthorgaExample_CargoRack</defName>
  <label>cargo rack</label>
  <roles><li>DthorgaExample_RackMount</li></roles>
  <loadouts><li>DthorgaExample_Wisent_Loadout</li></loadouts>
  <materials><Steel>50</Steel></materials>
  <work>5000</work>
  <researchPrerequisite>PhobosResearch_VehicleTuning</researchPrerequisite>
  <statFactors>
    <li><stat>CargoCapacity</stat><factor>1.1</factor></li>
    <li><stat>MoveSpeed</stat><factor>0.98</factor></li>
  </statFactors>
</Phobos.VehicleSystems.PhobosVehicleFittingDef>
```

- **A fitting must move something the vehicle has, or it is not offered.** A stat factor counts; a fitting whose only effect is
  `kilograms` is never offered.
- **The fitting and the loadout it names sit in the same folder**, so both load together or neither does.

## 5. Checking your XML offline

Before you start the game:

- **Every file must parse.** Any XML-aware editor shows a broken tag; or, with Python, from your mod's folder:

  ```text
  python -c "import pathlib, xml.etree.ElementTree as ET; [ET.parse(p) for p in pathlib.Path('.').rglob('*.xml')]; print('all parse')"
  ```

- **Check your mod's XML against ours, without the game.** Download
  [`check-vehicle-schemas.py`](https://github.com/phobos-dthorga/phobos-rimworld/blob/main/scripts/check-vehicle-schemas.py) from the
  repository's `scripts/` folder. You need **Python 3.9 or newer** ([python.org](https://www.python.org/)) and nothing else, plus
  D'thorga Framework installed in your game.

  ```text
  python check-vehicle-schemas.py --mod "C:\path\to\YourMod"
  ```

  It finds D'thorga Framework in Steam's usual folders. If your game or Workshop folder is somewhere else, add
  `--ours "<that folder>"`: the game's `Mods` folder, `steamapps\workshop\content\294100` or the Framework's own folder all work. It
  reads every version folder and load folder your mod has, applies your patches to our Defs, and your Defs may inherit ours.

  Each finding gives the file and line, the Def, what is wrong and **what to do**:
  - **ERROR**: the game will refuse it or get it wrong. Fix these first.
  - **WARNING**: worth a look, such as a part role your loadout leaves unbound (the system then treats that part as sound).
  - **NOTE**: something the check could not follow, such as a patch using XPath it cannot read.

  It exits with 1 if there are errors, so it can run in a build script. **It is no substitute for loading the game**: the game's log
  is the final word. The example mod checks clean (`--mod examples/DthorgaExampleMod`), and our own checks run it on every commit.

## 6. Dev mode and the dev actions worth knowing first

**Turn on Development mode** in the game's Options. The debug actions menu (the toolbar at the top of the screen) then has a
**D'thorga** category. The first ones to learn, for the selected vehicle:

| Dev action | What it tells you |
| --- | --- |
| **Vehicle systems: log selected** | Writes the vehicle's loadout by name, every role and the parts it bound, and each system's state to the log. **Your first check that a loadout matched.** |
| **Vehicle systems: fit now (no materials or work)** | Lists only the fittings the vehicle is offered, and fits one at once. A fitting missing here is one the offer check refused. |
| **Vehicle systems: set a quantity** | Sets any quantity (a battery's charge, the coolant's temperature) to 0, 25, 50, 75 or 100% of its range, to see a stage happen. |
| **Vehicle systems: hit a part** | A 20-point hit on a role's first part, through Vehicle Framework's own damage path, to try part-hit rules. |
| **Vehicle systems: run the self-test** | No vehicle needed: reads every vehicle you have and tries fittings, tunes, part replacement and grades on a throwaway vehicle, then opens a pass or fail report. Nothing in the colony is touched. |
| **Framework: write patched Defs at the next start** | At the next start, writes every Def with all patches applied to one file, `PhobosPatchedDefs/PatchedDefs.xml` in the game's user folder (below). It holds other authors' Defs: keep it on your computer. |

The full list is in [Dev actions](../vehicle-systems-modding.md#dev-actions) and [Tools for
developers](../framework-modding.md#tools-for-developers).

**What each vehicle was bound to, without starting a colony.** At every start, D'thorga Framework writes
`PhobosPatchedDefs/VehicleBindings.json` in the game's user folder: each vehicle design's loadout, its systems and their roles, and
each role's parts. Search it for your vehicle's `defName`.

## 7. Reading the log when something does not load

**The game's user folder on Windows** is `%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\`. RimWorld 1.7
writes its log to the **`Logs`** folder there:

- `Logs\Player.log`: this run (or the last one, once the game is closed).
- `Logs\Player-prev.log`: the run before.

What to look for:

- **Search for your prefix** (`DthorgaExample`). A line that names your `defName` is almost always the one to read first.
- **Config errors** name the Def and what is wrong with it. D'thorga Framework turns its silent XML mistakes into config errors that name
  the Def (a setting name that is no setting, a fitting with no `roles`, `work` of 0 or less, a fitting that moves no figure).
- **Lines starting `[D'thorga Framework]`** are ours. A design on which a loadout's role finds no part is named in the log's vehicle
  systems problems.
- **"Could not resolve cross-reference"** means a Def names another that is not loaded: a typo, or a mod missing from the list or
  loaded after yours.
- **In the game**, with Development mode on, the log window opens from the toolbar and shows the same lines.

**For a detail the log does not show**, D'thorga Framework's settings have **Verbose logging**: tick the log you are chasing, and its
extra lines are written. See [Logging](../framework-modding.md#logging).

## 8. What to look for in game

On a throwaway colony, with the example enabled after its dependencies:

1. **The lantern**: build an **example lantern** from the Architect's **Furniture** tab and fill it with chemfuel. It stays dark by
   day and lights at dusk; its fuel goes down only while it is lit.
2. **The loadout**: with Vanilla Vehicles Expanded, get a Wisent, select it and run **Vehicle systems: log selected**. The log line
   names `DthorgaExample_Wisent_Loadout` and lists the role `DthorgaExample_RackMount`.
3. **The fitting**: open **Fittings and tunes** from the Wisent's Dashboard. The **cargo rack** is offered once **vehicle tuning**
   is researched; or fit it at once with **Vehicle systems: fit now**. Fitted, the Wisent carries more.

## Where to go next

- **The guides**: [D'thorga Framework for modders](../framework-modding.md) (rooms, gases, health, parts bills, lamps and heaters,
  settings, logging), [vehicle systems for modders](../vehicle-systems-modding.md) (loadouts, systems, fittings, tunes) and [vehicle
  features for modders](../vehicle-features-modding.md) (cabins, camps, hitches, the sling, work rigs, aircraft).
- **The reference site**, <https://phobos-dthorga.github.io/phobos-rimworld/>: every field of every class, rebuilt from the code on
  every change. Its [XML types](https://phobos-dthorga.github.io/phobos-rimworld/xml-types.html) page shows each one as XML.
- **[Stability: what you can rely on](stability.md)**: which names will not change under you, and how you are told when something
  does.
- **Each other D'thorga mod's page**: [for modders, mod by mod](README.md).
