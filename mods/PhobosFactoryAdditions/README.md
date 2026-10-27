# D'thorga Factory Additions

Additions to Vanilla Furniture Expanded - Factory. This version adds one bill: **Synthesize neutroamine
from human meat**, for the neutroamine synthesizer.

Version 0.1.0, built 28 October 2026 for House D'thorga. **Not yet tested in game.**

## Read this first (gotchas)

- **Requires Vanilla Expanded Framework and Vanilla Furniture Expanded - Factory.** Load it after both. If
  you run Kimoju's "VFE Factories: Buffs and tweaks", load this after that too (the About file asks for it).
- **XML only, no DLL.** It declares RimWorld 1.6 and 1.7 and loads the same files on both. VFE Factory itself
  loads its 1.6 files on 1.7.
- **The stock bill stops taking human meat** once this mod is on. Human meat sitting in a hopper that feeds
  the stock "Synthesize neutroamine" bill will stay there until you add the human-meat bill or move it.
- **Tick Human meat in the hopper's storage filter**, and raise the hopper's priority (factory hoppers start
  at Low, so haulers will not move meat off Preferred shelves to fill one). The bill reads "Missing inputs:
  Human meat 0/20" until it is fed.
- **Safe to add to a running save.** Before removing it, remove the human-meat bill from every neutroamine
  synthesizer's Processes tab and let any running batch finish; otherwise the first load afterwards should
  log an error about the missing process and drop that bill (expected, not yet checked).
- **Nothing is published on the Steam Workshop yet.** Install from source with `scripts/deploy.py`.
- **Not yet tested in game.** Everything below is read from the mods' code and XML.

## What it does

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

## Where to find things

- Research **Complex factories** (VFE Factory). Kimoju's mod also asks for **Biofuel refining**.
- Build the **neutroamine synthesizer** (Architect, Factories), open its **Processes** tab, click **Add
  process** and pick **Synthesize neutroamine from human meat**.
- Human meat comes from VFE Factory's automated mincer ("Mince human corpses") or a butcher table.

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
take human meat again. `Mods/VanillaChemfuelExpanded/` loads only while that mod is active.

## Checks done

- 28 October 2026: the stock bill's ingredient matching read in Vanilla Expanded Framework's Pipe System
  code (category match, exclusion list honoured, no human-meat filter anywhere in VFE Factory). The stock,
  insect and fish values read from the installed mods' XML. `scripts/check-mod-layout.py` passes.
- **Not checked:** anything in game. Whether the patches apply cleanly, whether the bill appears, feeds and
  outputs, and what happens on removal are all untested.
