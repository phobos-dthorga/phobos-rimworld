# D'thorga Factory Additions

Additions to Vanilla Furniture Expanded - Factory: an **automated fabrication bay** that makes what a
fabrication bench makes (bionics and other artificial body parts, spacer weapons and armour), and with Anomaly
or Dubs Rimatomics what a bioferrite shaper or a Rimatomics machining table makes; a **fix for the automated
machining bay**, which could lose a finished item; a **Synthesize neutroamine from human meat** bill for
the neutroamine synthesizer; a **deep factory hopper** that holds four stacks; with "[sbz] Fridge", **conveyor belts and factory machines that take corpses
straight from its corpse fridge**; and **drug lab recipes from other mods as factory bills**, each switched
on only while its mod is active (Medicines+, Deep Pills, Ushankas Biological Warfare, Combat Extended, Dubs
Rimatomics, Hemofungus, Complementary Odyssey, Glitterworld Medicine Production, Farmable Neutroamine,
Passive Exoskeletons, Simple Washing Machine).

Version 0.5.0, built 29 October 2026 for House D'thorga. **Tested in game:** the human-meat bill (owner,
28 October 2026, RimWorld 1.7) and the drug-lab bills appearing on their machines (owner, 29 October 2026).
The corpse fridge change, running the 0.3.0 bills to the end, the 0.3.2 chemfuel repair, the 0.4.0 deep
factory hopper, and everything in 0.5.0 (the fabrication bay and the machining bay fix) are not yet tested
in game.

## Read this first (gotchas)

- **Requires Harmony, Vanilla Expanded Framework and Vanilla Furniture Expanded - Factory.** Load it after all
  three, and after every mod listed above that you run (the About file asks for it, so RimSort does this for
  you). VFE Factory already needs Harmony, so you have it.
- **It has code now (0.5.0).** One small assembly, `PhobosFactory.dll`, built for RimWorld 1.6 and loaded on
  1.7 too, as VFE Factory's own assembly is. **Build it before deploying** (`dotnet build
  src/PhobosFactory/PhobosFactory.csproj`); the deploy script says "no assembly built" if you forget.
- **The fabrication bay looks exactly like the automated machining bay** until it has art of its own. Select it
  to see which is which. It needs **Complex factories and Fabrication** research.
- **A fabrication bay bill appears only once its bench recipe is unlocked**: the item's research (for example
  Bionics), Ideology memes, and Rimatomics' own research for its parts. Until then it is not in the Add
  process menu.
- **Many spacer items need four to six ingredients** (marine armour: plasteel, uranium, gold, advanced
  components) and the bay has **three input cells**. Use **deep factory hoppers** on the input cells, and pipe
  chemfuel in with Vanilla Chemfuel Expanded.
- **Bionics, weapons and armour leave one at a time.** These items don't stack, so the bay (and the machining
  bay, after the fix) **waits while an identical item is still in the output hopper**. Its inspect text then
  says "Output factory hopper too full (max 0.5 units)": Vanilla Expanded's wording, here meaning "take the
  finished item away first". **Run a belt out of the output hopper.**
- **The machining bay fix changes VFE Factory's own bills** (owner's go-ahead, 29 October 2026). Before it, a
  finished item vanished when an identical one was still in the output hopper; now the machine waits. The
  startup log says how many bills it fixed.
- **The new bills keep their mods' research.** A bill appears in the machine's Add process menu only once
  its research is done: Medicines+ projects for its drugs, each disease's vaccine project for Biological
  Warfare, Deathrest (Biotech) for hemogen, Neutroaleaf refining for leaf neutroamine, and so on. Complex
  factories (granulator, synthesizer) or Basic factories (refinery) as well.
- **Some outputs stack small, so empty the output hopper.** Hemogen packs stack to 10, vaccines and
  antiserums to 15, medicine to 25. A batch waits ("output hopper full") while the stack on the output cell
  is above the bill's cap, which is set below each stack limit so nothing is lost.
- **Glitterworld medicine needs five input hoppers:** the usual three behind the granulator plus the two
  behind its corners. Four ingredients, one stack per hopper cell. Not yet seen in game: if the corner
  cells do not light up as input slots, feed the fourth ingredient by belt into one of the three.
- **The infusions need both Deep Pills and Medicines+.** Deep Pills only defines the raw plants when
  Medicines+ is active.
- **A reproduction pill bill makes one pill at a time.** It takes a pathogen sample, and samples do not
  stack, so one sample per hopper cell.
- **The stock bill stops taking human meat** once this mod is on. Human meat sitting in a hopper that feeds
  the stock "Synthesize neutroamine" bill will stay there until you add the human-meat bill or move it.
- **Tick the ingredient in the hopper's storage filter**, and raise the hopper's priority (factory hoppers
  start at Low, so haulers will not move items off Preferred shelves to fill one). A bill reads "Missing
  inputs" until it is fed.
- **The deep factory hopper looks exactly like the ordinary one** on the map (VFE Factory's hopper draws
  fixed textures). Select it to see which it is. Like every factory hopper it starts at **Low** priority.
- **A belt touching the corpse fridge empties it.** Once the fridge counts as a factory hopper, any belt
  that has the fridge directly behind it or at either side pulls corpses out whenever the belt has room,
  whatever the belt's own filter says. Keep belts you do not want fed away from the fridge's four cells.
- **Safe to add to a running save.** Before removing it, remove every bill this mod added from each
  machine's Processes tab and let any running batch finish; otherwise the first load afterwards should log
  an error about a missing process and drop that bill (expected, not yet checked). **Deconstruct every deep
  factory hopper and every fabrication bay** too; the items on a hopper stay where they are. The fridge
  change and the machining bay fix leave nothing in the save.
- **Nothing is published on the Steam Workshop yet.** Install from source with `scripts/deploy.py`.
- **Tested in game: the human-meat bill, and the 0.3.1 fix.** The owner confirmed on 28 October 2026, on
  RimWorld 1.7, that the human-meat bill works, and on 29 October 2026, with 0.3.2 installed, that the bills
  now appear on the machines ("Everything seems to be good in-game now"). Running each new bill to the end,
  the corner input cells and the chemfuel-tap repair are not yet confirmed in game.

## What it does

### The automated fabrication bay (0.5.0)

VFE Factory's automated machining bay gets a bill for every item made at a **machining table**, and nothing
else. That is why MSE2's steel bone replacements had bills and bionics didn't: bionics are made at the
**fabrication bench**. The fabrication bay closes that gap (owner's choice of benches and of a separate
building, 29 October 2026):

| Bench it takes over | Needs | Bills on your mod list (counted from the mods' files, before patches) |
| --- | --- | --- |
| Fabrication bench | always | about 135: bionic and other artificial body parts (vanilla, Royalty, Biotech, Odyssey, MSE2's plasteel bones and modules, Glittertech, Dubs Bad Hygiene), spacer guns, spacer armour and helmets |
| Bioferrite shaper | Anomaly | about 18: ghoul parts, bioferrite guns, shard lances, ritual mask |
| Rimatomics machining table | Dubs Rimatomics | 27: reactor and weapon upgrade parts, warheads, sabot rounds, MOPP and radiation suits |

**How the bills are made.** The game doesn't store these as bills, so the mod builds them at load from each
item's own recipe, **after every other mod's changes** (Combat Extended's costs, MSE2's parts):

- **Ingredients:** the item's cost. An item made of a material takes **steel** for metal, any **textile** for
  fabric, any **leather** for leather, or its one possible material (VFE Factory's own rule, so the result is
  made of whatever was fed in).
- **Time:** the bench's work times **four** (VFE Factory's machining rule). No skill needed; everything comes
  out at **normal quality**.
- **Research:** the bench's research (Fabrication, Bioferrite shaping or Rimatomics activation) plus the
  item's own. And the bill shows only while the **bench recipe itself is available**, so Ideology memes and
  Rimatomics' own research steps still gate it.
- **Chemfuel** in a recipe comes **from the pipes** with Vanilla Chemfuel Expanded.
- **Not automated:** anything a machine already makes (the four bioferrite guns VFE's machining bay makes),
  buildings (the hologram base), and items made of **a material plus parts** (Pandora's Exopack, ExoHelmet
  and ExoVisor, Anomaly's ceremonial hood): Pipe System gives a finished item the material of the **last**
  ingredient it took, so those could come out made of components. The startup log names every skipped item.

**Expect two lines in the log at startup**, both starting "[D'thorga Factory Additions]": one giving, per
bench, how many bills the bay got and what was skipped; one saying on how many machining bay bills the
output limit was made safe. Those counts are the real ones for your game; the table above is an estimate.

The building itself inherits everything from the machining bay (size, the three input cells behind and
beside it, output on the interaction cell in front, power, heat, overclocking, boosters, heatsinks, piped
chemfuel, the two mechanoid shredding bills) and differs in:

| | Automated machining bay | Automated fabrication bay |
| --- | --- | --- |
| Cost | 440 steel, 18 components, 5 advanced components | **300 steel, 150 plasteel, 12 components, 10 advanced components** |
| Work | 5000 | **7000** |
| Research | Complex factories | **Complex factories, Fabrication** |

Cost and work are agent defaults, open to tuning.

### The machining bay output fix (0.5.0)

**What went wrong (read in Pipe System's and the game's code, 29 October 2026; not seen in game).** When a
batch finishes, Pipe System adds it to a matching stack in the output hopper if that stack holds no more than
the bill's output limit. The game counts two identical guns as "stackable" even though a gun stacks to 1, so
with VFE's limit of 1 the new gun is "added" to the old one, none of it fits, and the rest is never placed:
**the gun vanishes and its materials are spent.** It happens whenever an identical item is still sitting in
the output hopper, which a belt out of the hopper usually prevents.

**The fix (owner's go-ahead, 29 October 2026).** At load, every bill on the machining bay gets the largest
output limit that can't lose anything: for an item that doesn't stack, **0.5**, so the machine **waits** while
any identical item is there; for a stacking item, its stack limit minus one batch. The fabrication bay's
bills get the same rule. The two mechanoid shredding bills are left alone: their steel depends on the
mechanoid's size, so no fixed limit fits.

### Neutroamine from human meat

| Bill | Inputs | Output | Time | Who adds it |
| --- | --- | --- | --- | --- |
| Synthesize neutroamine | 20 raw **animal** meat + 3 chemfuel | 5 neutroamine | 4800 ticks (about 2 hours) | VFE Factory; this mod excludes human meat and says so in its text |
| **Synthesize neutroamine from human meat** | 20 human meat + 3 chemfuel | **6 neutroamine** | 4800 ticks | **This mod** |
| Synthesize neutroamine from insect meat | 30 insect meat + 5 chemfuel | 10 neutroamine | 4800 ticks | Kimoju's "VFE Factories: Buffs and tweaks", if you run it |
| Synthesize neutroamine from fish meat | 4 fish + 3 chemfuel | 5 neutroamine | 4800 ticks | Kimoju's mod, with Odyssey |
| **Synthesize neutroamine from neutroaleaves** | 25 neutroaleaves | 5 neutroamine | 5000 ticks | **This mod**, with Farmable Neutroamine |

The new bills use the same input slots, sounds and effects as the stock one. With Vanilla Chemfuel
Expanded active, the synthesizer can draw the chemfuel for the human-meat bill from the chemfuel pipe
network, as it can for the stock bill.

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

### Drug lab recipes as medicine granulator bills (0.3.0)

Every bill is the other mod's drug lab recipe scaled the way VFE Factory scales its own drug bills: a
**batch of four** (four times the inputs) in **four times the lab's work** per unit, no skill needed, the
mod's own research plus Complex factories. Values are read from each mod's XML on 28 October 2026 for the
versions then installed; a later update to a source mod may change its lab recipe without changing these.

**Medicines+** (15 bills, one of them authored):

| Bill | Inputs | Output | Time (ticks) | Research |
| --- | --- | --- | --- | --- |
| aniracetam | 12 neutroamine, 4 medicine | 4 | 16000 | Advanced utility medicine |
| AntiTox | 12 neutroamine, 8 herbal medicine | 4 | 12800 | Basic utility medicine |
| clozapine | 8 neutroamine, 20 smokeleaf leaves | 4 | 12800 | Basic utility medicine |
| cyanide | 4 neutroamine | 4 | 6400 | Medicine basics |
| heroin | 8 crimson bulbs, 8 morphine, 4 neutroamine | 4 | 28800 | Advanced profit drugs |
| hydra | 8 crimson bulbs, 4 neutroamine | 4 | 19200 | Crimson bulb refining |
| Joy | 4 ambrosia, 40 raw fungus, 4 neutroamine | 4 | 9600 | Advanced profit drugs |
| addictol | 12 neutroamine, 20 chemfuel | 4 | 16000 | Advanced profit drugs |
| morphine | 20 opium, 20 psychoid leaves | 4 | 16000 | Basic utility medicine |
| penicillin | 4 neutroamine, 4 herbal medicine | 4 | 8000 | Basic utility medicine |
| **penicillin from fungus** | 40 raw fungus, 4 neutroamine | 4 | 8000 | Basic utility medicine |
| smelling salts | 4 psychoid leaves, 4 neutroamine | 4 | 12800 | none |
| ThornWeed serum | 20 ThornWeed needles, 20 steel, 4 neutroamine | 4 | 14400 | Advanced utility medicine |
| Tyrox | 8 neutroamine, 4 penicillin, 4 crimson bulbs | 4 | 22400 | Advanced utility medicine |
| Upper | 40 psychoid leaves, 12 neutroamine | 4 | 14400 | Basic profit drugs |

**Penicillin from fungus is authored, not a port.** Penicillin is made by Penicillium moulds, so a fungal
feedstock is the honest route to it; Medicines+'s own recipe goes through herbal medicine. The fungus bill
keeps the same neutroamine, work and research, and asks 10 raw fungus per unit, a gameplay choice meant to
make it a bulkier path rather than a cheaper one (raw fungus is worth about a tenth of herbal medicine).
Ask for a different ratio if play shows it beats the herbal path.

**Deep Pills infusions** (with Medicines+; batch of eight, from the x8 lab recipe, no research):

| Bill | Inputs | Output | Time (ticks) |
| --- | --- | --- | --- |
| peyote infusion | 32 peyote fruits | 8 peyote | 4000 |
| opium infusion | 32 opium poppies | 8 opium | 4000 |
| thorn weed infusion | 32 thorns of weed | 8 ThornWeed needles | 4000 |

**Ushankas Biological Warfare** (16 bills; seven diseases: flu, wound infection, malaria, necroa, plague,
scaria, sleeping sickness):

| Bill | Inputs | Output | Time (ticks) | Research |
| --- | --- | --- | --- | --- |
| each vaccine | 100 pathogen, 32 neutroamine, 4 penoxycyline | 4 | 41600 | that disease's vaccine project |
| each antiserum | 100 pathogen, 24 neutroamine, 4 glitterworld medicine | 4 | 28800 | that disease's vaccine project |
| malaria pills, sleeping sickness pills | 1 sample, 6 neutroamine | 1 | 2400 | Pathogen production |

Pathogen samples, the disease sampler and flesh-breaker stay at the drug lab.

**Singles:**

| Bill | Mod | Inputs | Output | Time (ticks) | Research |
| --- | --- | --- | --- | --- | --- |
| psycho | Combat Extended | 64 psychoid leaves | 4 | 9600 | none |
| potassium iodide | Dubs Rimatomics | 12 neutroamine | 4 | 8000 | Penoxycyline production |
| hemogen from hemofungus | Hemofungus (Biotech) | 60 hemofungus | 4 hemogen packs | 6400 | Deathrest |
| vacsop from steel | Complementary Odyssey | 4 vacdew, 20 steel | 4 | 9600 | Drug production |
| vacsop from vacstone | Complementary Odyssey | 4 vacdew, 20 vacstone blocks | 4 | 9600 | Drug production |
| glitterworld medicine | Glitterworld Medicine Production | 20 synthread, 4 components, 16 plasteel, 4 medicine | 4 | 28800 | Glitterworld medicine production |

The lab's vacsop recipe takes steel **or** vacstone blocks; a factory bill takes one exact item, so there
are two bills.

### Chemistry on the automated biofuel refinery (0.3.0)

| Bill | Mod | Inputs | Output | Time (ticks) | Research |
| --- | --- | --- | --- | --- | --- |
| polyamide | Passive Exoskeletons | 75 chemfuel, 8 plasteel | 25 | 24000 | Biofuel refining, Drug production |
| laundry detergent | Simple Washing Machine | 100 chemfuel, 4 neutroamine | 12 | 24000 | Sterile materials |
| heavy-duty laundry detergent | Simple Washing Machine | 100 chemfuel, 12 neutroamine | 12 | 32000 | Sterile materials |

With Vanilla Chemfuel Expanded active these three can draw their chemfuel from the pipe network, as the
refinery's stock bills do. With Vanilla Races Expanded - Android active, the granulator bills that take
neutroamine can draw it from that mod's neutroamine network (mirrors VFE Factory's own patch; untested).

### The deep factory hopper (0.4.0)

A factory hopper that holds **four stacks** on its one cell instead of one. Cost **35 steel and 1
component**, 800 work (the ordinary hopper: 20 steel, 400 work); same research and Architect tab. All three
figures are agent defaults, open to tuning.

**Why it helps.** A factory machine does not tie an input cell to one ingredient: while a bill waits, it
looks at every item on each of its input cells and takes whatever that bill still needs, keeping a running
count of each ingredient inside the machine. So one hopper can feed several ingredients. With an ordinary
hopper (one stack) that jams easily: a belt carrying two ingredients drops a stack the machine has had enough
of, the hopper is full, and the other ingredient waits behind it on the belt for ever. A deep hopper has room
for one stack of each of four ingredients, and more of the same item merges into its stack.

| Use | What to expect (read from the code, not yet seen in game) |
| --- | --- |
| **Input**, fed by one belt carrying mixed ingredients | The belt keeps filling it until all four places hold something; the machine takes what it needs from any of them. |
| **Input**, filled by haulers | Tick every ingredient in its storage filter and raise its priority. |
| **Output** of items that do not stack (weapons, bionics, armour) | Holds up to four **different** finished items. An identical one waits until the first is taken away (with the 0.5.0 fix; before it, the identical one vanished). Items made of a material (a steel helmet) are placed separately, up to four. |
| **Output** of items that stack | Same as an ordinary hopper: the batch waits once that stack reaches the bill's output cap. |
| Under a belt that pulls from it | The belt takes one whole stack at a time, the first it finds. |

It can share a storage group with ordinary hoppers (same group tag), and inherits everything else from VFE
Factory's hopper: input and output markings, the allow taking and allow inserting toggles, research.

**Tip for the automated machining bay** (three input cells, up to four solid ingredients plus chemfuel): put a
deep hopper on the input cell fed by the mixed belt, or deep hoppers on all three. Chemfuel needs no hopper
with Vanilla Chemfuel Expanded: the bay's bills draw it from the pipes.

### Chemfuel taps and piped astrofuel (needs Vanilla Chemfuel Expanded; 0.3.2)

VFE Factory sets up two things for Vanilla Chemfuel Expanded: the **chemfuel tap** (a 1 x 1 basin that fills
itself with chemfuel from the pipe network) counts as a **factory hopper**, and the refinery's **astrofuel**
bill (with Vanilla Gravship Expanded) takes chemfuel **straight from the pipes**. Both live at the end of one
VFE Factory patch, and that patch stops early when Combat Extended and Vanilla Factions Expanded - Deserters
are also active: CE removes an armour-piercing shell bill that an earlier step edits. This mod applies the
two missing steps itself, each only if it is still missing. Expected, not yet seen in game:

- A belt with a chemfuel tap behind it, or at either side, carries chemfuel away from it.
- A machine whose input cell sits on a tap takes chemfuel straight from it, no belt needed.
- The astrofuel bill runs on piped chemfuel, as the refinery's other chemfuel-using bills do.

As with any factory hopper, **a belt touching a tap pulls from it whenever the belt has room**, so keep
belts you don't want fed away from taps.

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

- Research **Complex factories** (VFE Factory) for the granulator and synthesizer, **Basic factories** for
  the refinery. Kimoju's mod also asks for **Biofuel refining** on the synthesizer.
- Build the machine (Architect, Factories), open its **Processes** tab, click **Add process** and pick the
  bill. Bills whose research is not done are not listed yet.
- The **deep factory hopper** is under Architect, Factories, beside the factory hopper (Basic factories).
- The **automated fabrication bay** is under Architect, Factories, in the same tab as the machining bay
  (Complex factories and Fabrication). It must stand on factory floor, like every factory machine.
- Human meat comes from VFE Factory's automated mincer ("Mince human corpses") or a butcher table.
- The **corpse fridge** is under Architect, Temperature, after Air conditioning ("[sbz] Fridge").

## For modders

Every bill is a Pipe System `ProcessDef` (Vanilla Expanded Framework). The human-meat bill, defName
`PhobosFactory_SynthesizeNeutroamineFromHumanMeat`, is in `Common/Defs/ProcessDefs/`; every 0.3.0 bill is
in `Mods/<SourceMod>/Defs/ProcessDefs/`, beside the patch that adds it to its machine, and that folder
loads only while the source mod is active (`LoadFolders.xml`). Change a yield by patching the result count:

```xml
<Operation Class="PatchOperationReplace">
  <xpath>Defs/PipeSystem.ProcessDef[defName="PhobosFactory_SynthesizeNeutroamineFromHumanMeat"]/results/li[thing="Neutroamine"]/count</xpath>
  <value><count>5</count></value>
</Operation>
```

`Common/Patches/NeutroamineSynthesizer.xml` adds the human-meat bill to the synthesizer's process list and
puts `Meat_Human` on the stock bill's `disallowedThingDefs`. Remove that second operation to let the stock
bill take human meat again.

Rules a new bill follows here, from reading Pipe System's code (28 October 2026): one result per bill (only
the first is checked against the output hopper's filter); `maxOutputCount` set, and at most the result's
stack limit minus the batch count (above that, the surplus of a finished batch is destroyed; at zero, a
blocked output is destroyed instead of waiting), and **0.5 for an item that doesn't stack** (the game counts
two identical items as stackable even at a stack limit of 1, so a limit of 1 loses the second one; read
29 October 2026); `researchPrerequisites` hides the bill from the Add menu
until done; any ingredient may come from any input slot, but a hopper cell holds one stack. The offline
checker `scripts/check-factory-patches.py` enforces the first three against the installed game.

**Give every file in a `Mods/<SourceMod>/` folder that folder's name** (`MedicineGranulator_MedicinesPlus.xml`,
not `MedicineGranulator.xml`). RimWorld keys a mod's Defs and Patches files by their path inside each load
folder and loads only the copy in the folder listed last; the others are skipped without a message. That
hid most of 0.3.0's bills. `scripts/check-mod-layout.py` now refuses a shared path.

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

**Automating another bench (0.5.0).** Add a `Phobos.Factory.BenchBillTemplateDef` naming the bench and a
factory machine; the assembly turns every item that bench makes into a bill on that machine at load. The
fabrication bay's three templates are in `Common/Defs/BenchBillTemplateDefs/` and `Mods/Rimatomics/Defs/`:

```xml
<Phobos.Factory.BenchBillTemplateDef>
  <defName>YourMod_SmithyBills</defName>          <!-- part of every bill's defName; saved, never rename -->
  <machine>PhobosFactory_AutomatedFabricationBay</machine>
  <sourceBenches><li>ElectricSmithy</li><li>FueledSmithy</li></sourceBenches>
  <researchPrerequisites><li>Smithing</li></researchPrerequisites>
  <billLabel>Make {0}</billLabel>                  <!-- {0}: the item's label -->
  <billDescription>Make {0} as a smithy would.</billDescription>
  <firstPriorityInBillList>400</firstPriorityInBillList>
  <autoInputSlots><li>(0,0,3)</li><li>(-3,0,0)</li><li>(3,0,0)</li></autoInputSlots>
  <sustainerDef>VFEFactory_AutomatedMachiningSustainer</sustainerDef>
  <effecterDef>VFEFactory_Default</effecterDef>
</Phobos.Factory.BenchBillTemplateDef>
```

Optional fields: `excludedThings` (items to leave at the bench), `workMultiplier` (default 4),
`maxOutputCount` (default 75; lowered per item to a safe limit), `spawnOnInteractionCell` (default true).

**Safe output limits on another machine.** Add `<li Class="Phobos.Factory.SafeOutputLimitsExtension" />` to
its `modExtensions`, as `Common/Patches/MachiningBay.xml` does for the machining bay. Bills whose result
count is decided at run time (a custom result worker) are left alone.

## Checks done

- 28 October 2026: the stock bill's ingredient matching read in Vanilla Expanded Framework's Pipe System
  code (category match, exclusion list honoured, no human-meat filter anywhere in VFE Factory). The stock,
  insect and fish values read from the installed mods' XML. The belt's pull rule and the machines' input
  rule read in VFE Factory's and Pipe System's code; Adaptive Storage Framework's published source read for
  how it holds items. `scripts/check-mod-layout.py` passes.
- 28 October 2026, owner, in game on RimWorld 1.7: the human-meat bill works.
- 28 October 2026, 0.3.0: every source recipe's values read from the installed mods' XML, the granulator's
  bill rules read in Pipe System's code, and `scripts/check-factory-patches.py` run against the owner's
  installation (RimWorld 1.7, 299 active mods): every patch xpath matches one node, every Def name resolves,
  every output cap is within its stack limit.
- 29 October 2026, owner, in game: the 0.3.0 bills were missing from the machines (RimDex listed them).
  Cause found in RimWorld 1.7's file loader and confirmed by re-running the improved checker on the 0.3.0
  files (41 of 45 bills on no machine); fixed in 0.3.1. The checker passes on 0.3.1.
- 29 October 2026, owner, in game on RimWorld 1.7 with 0.3.2: "Everything seems to be good in-game now"
  after checking that the bills appear on the granulator, synthesizer (human meat back) and refinery.
- 29 October 2026, 0.5.0: VFE Factory's machining bill generator, Pipe System's output step, menu and
  ingredient cache, the game's stacking and recipe code, and Dubs Rimatomics' recipe unlock patch read in
  their decompiled 1.7 assemblies. The assembly builds with no warnings for RimWorld 1.6; every game and
  Pipe System member it calls exists in the 1.7 assemblies. The output-limit rule has unit tests, and
  `scripts/check-factory-patches.py` passes against the owner's installation (RimWorld 1.7, 300 active mods),
  including the three bench templates. The bill counts above come from a scan of the installed mods' files.
- **Not checked:** whether each new bill feeds, finishes and outputs as expected; whether the two corner
  input cells work; whether a belt takes chemfuel from a tap (0.3.2); whether the stock bill now refuses human meat, whether a belt
  actually lifts a corpse out of the fridge, and what happens on removal. For 0.5.0: everything in game,
  starting with the two startup log lines, the bay's Add process menu, and one bionic made end to end.
