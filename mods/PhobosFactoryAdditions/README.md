# D'thorga Factory Additions

Additions to Vanilla Furniture Expanded - Factory: a **Synthesize neutroamine from human meat** bill for
the neutroamine synthesizer, and, with "[sbz] Fridge", **conveyor belts and factory machines that take
corpses straight from its corpse fridge**.

Version 0.2.0, built 28 October 2026 for House D'thorga. **The human-meat bill is tested in game** (owner,
28 October 2026, RimWorld 1.7). The corpse fridge change is not yet tested in game.

## Read this first (gotchas)

- **Requires Vanilla Expanded Framework and Vanilla Furniture Expanded - Factory.** Load it after both. If
  you run Kimoju's "VFE Factories: Buffs and tweaks" or "[sbz] Fridge", load this after those too (the About
  file asks for it).
- **XML only, no DLL.** It declares RimWorld 1.6 and 1.7 and loads the same files on both. VFE Factory itself
  loads its 1.6 files on 1.7.
- **The stock bill stops taking human meat** once this mod is on. Human meat sitting in a hopper that feeds
  the stock "Synthesize neutroamine" bill will stay there until you add the human-meat bill or move it.
- **Tick Human meat in the hopper's storage filter**, and raise the hopper's priority (factory hoppers start
  at Low, so haulers will not move meat off Preferred shelves to fill one). The bill reads "Missing inputs:
  Human meat 0/20" until it is fed.
- **A belt touching the corpse fridge empties it.** Once the fridge counts as a factory hopper, any belt
  that has the fridge directly behind it or at either side pulls corpses out whenever the belt has room,
  whatever the belt's own filter says. Keep belts you do not want fed away from the fridge's four cells.
- **Safe to add to a running save.** Before removing it, remove the human-meat bill from every neutroamine
  synthesizer's Processes tab and let any running batch finish; otherwise the first load afterwards should
  log an error about the missing process and drop that bill (expected, not yet checked). The fridge change
  leaves nothing in the save.
- **Nothing is published on the Steam Workshop yet.** Install from source with `scripts/deploy.py`.
- **Tested in game: the human-meat bill only.** The owner confirmed on 28 October 2026, on RimWorld 1.7,
  that it works. The corpse fridge change, the stock bill's exclusion and removal from a save are not yet
  tested in game; what this README says about them is read from the mods' code and XML.

## What it does

### Neutroamine from human meat

| Bill | Inputs | Output | Time | Who adds it |
| --- | --- | --- | --- | --- |
| Synthesize neutroamine | 20 raw **animal** meat + 3 chemfuel | 5 neutroamine | 4800 ticks (about 2 hours) | VFE Factory; this mod excludes human meat and says so in its text |
| **Synthesize neutroamine from human meat** | 20 human meat + 3 chemfuel | **6 neutroamine** | 4800 ticks | **This mod** |
| Synthesize neutroamine from insect meat | 30 insect meat + 5 chemfuel | 10 neutroamine | 4800 ticks | Kimoju's "VFE Factories: Buffs and tweaks", if you run it |
| Synthesize neutroamine from fish meat | 4 fish + 3 chemfuel | 5 neutroamine | 4800 ticks | Kimoju's mod, with Odyssey |

The new bill uses the same input slots, sounds and effects as the stock one. With Vanilla Chemfuel
Expanded active, the synthesizer can draw the chemfuel for it from the chemfuel pipe network, as it can for
the stock bill.

**No Ideology thought fires.** Rendering fat for chemistry is not eating (owner's choice, 28 October 2026).

**Why 6 and not 5.** The 20 percent bump is an **authored gameplay value**, not a calculation. The reasoning:
VFE Factory's lore has the synthesizer render fat out of the meat, and a human carcass carries far more fat
than the game animals that supply most colony meat. Whole-body fat in adults averages roughly a quarter to
two fifths of body mass (US NHANES survey, 1999 to 2004, by DXA scan: men 23 to 31 percent, women 32 to 42
percent, by age group; [CDC MMWR report](https://www.cdc.gov/mmwr/preview/mmwrhtml/mm5751a4.htm), primary
source). Raw deer meat holds about 2 to 3 grams of fat per 100 grams ([USDA FoodData Central entry
173855](https://getfoodfacts.com/food/game-meat-deer-raw-173855), read through an aggregator site, so a
secondary source). Those two figures are not like for like (a whole body against a lean cut), and the game's
"meat" item is neither, so the bump is a nod to the direction of the difference, not its size.

### The corpse fridge as a belt and factory source (needs "[sbz] Fridge")

VFE Factory's belts pull only from **factory hoppers**: either the hopper building itself, or any storage
building whose Def is flagged as one. Factory machines use the same flag to decide which cells they may grab
ingredients from. This mod flags the **corpse fridge** from seobongzu's "[sbz] Fridge" (an Adaptive Storage
Framework building, 2 x 2, 20 corpses). Expected from the code, not yet seen in game:

- A belt with the fridge directly behind it, or at either side, pulls one corpse at a time out of the
  fridge whenever the belt has room, and carries it on as it would from a hopper.
- An automated mincer whose input cell (the cell in front of its hopper side) lands on one of the fridge's
  four cells takes corpses straight from the fridge, no belt needed.
- A belt pointed **into** the fridge already delivered corpses to it before this mod; that is stock
  behaviour for any storage the belt's item fits.
- Colonists still haul to and from the fridge as before. VFE Factory's "allow taking" and "allow inserting"
  rules apply to the real hopper building only, so the fridge gains no such toggles.

Only the corpse fridge is flagged. The mod's other fridges could be flagged the same way (one patch
operation each); ask if you want belts fed from them too.

## Where to find things

- Research **Complex factories** (VFE Factory). Kimoju's mod also asks for **Biofuel refining**.
- Build the **neutroamine synthesizer** (Architect, Factories), open its **Processes** tab, click **Add
  process** and pick **Synthesize neutroamine from human meat**.
- Human meat comes from VFE Factory's automated mincer ("Mince human corpses") or a butcher table.
- The **corpse fridge** is under Architect, Temperature, after Air conditioning ("[sbz] Fridge").

## For modders

The bill is a Pipe System `ProcessDef` (Vanilla Expanded Framework), defName
`PhobosFactory_SynthesizeNeutroamineFromHumanMeat`, in `Common/Defs/ProcessDefs/`. Change the yield by
patching its result count:

```xml
<Operation Class="PatchOperationReplace">
  <xpath>Defs/PipeSystem.ProcessDef[defName="PhobosFactory_SynthesizeNeutroamineFromHumanMeat"]/results/li[thing="Neutroamine"]/count</xpath>
  <value><count>5</count></value>
</Operation>
```

`Common/Patches/NeutroamineSynthesizer.xml` adds the bill to the synthesizer's process list and puts
`Meat_Human` on the stock bill's `disallowedThingDefs`. Remove that second operation to let the stock bill
take human meat again. `Mods/VanillaChemfuelExpanded/` and `Mods/SbzFridge/` load only while those mods are
active.

To let belts and machines pull from any other storage building, add Pipe System's extension to its Def:

```xml
<Operation Class="PatchOperationAddModExtension">
  <xpath>Defs/ThingDef[defName="YourStorageDef"]</xpath>
  <value>
    <li Class="PipeSystem.FactoryHopperExtension">
      <isfactoryHopper>true</isfactoryHopper>
    </li>
  </value>
</Operation>
```

It works only for storage whose items stay spawned on its cells (vanilla shelves, Adaptive Storage buildings);
the belt reads the map's thing grid. The full reading is in
[docs/development/vfe-factory-conveyors.md](../../docs/development/vfe-factory-conveyors.md).

## Checks done

- 28 October 2026: the stock bill's ingredient matching read in Vanilla Expanded Framework's Pipe System
  code (category match, exclusion list honoured, no human-meat filter anywhere in VFE Factory). The stock,
  insect and fish values read from the installed mods' XML. The belt's pull rule and the machines' input
  rule read in VFE Factory's and Pipe System's code; Adaptive Storage Framework's published source read for
  how it holds items. Every patch xpath matched exactly one node in the installed mods' XML.
  `scripts/check-mod-layout.py` passes.
- 28 October 2026, owner, in game on RimWorld 1.7: the human-meat bill works.
- **Not checked:** whether the stock bill now refuses human meat, whether a belt actually lifts a corpse
  out of the fridge, and what happens on removal are all untested.
