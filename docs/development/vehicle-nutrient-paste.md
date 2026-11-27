# Nutrient paste on vehicles (23 Nov 2026)

**Status (23 Nov 2026):** built: NP1 and NP2 in Framework 0.271.0 and Vehicles 0.115.0, NP3 to NP5 in Framework 0.273.0 and Vehicles 0.116.0; not yet tested in game (rows 449, 450, 452 to 454). Before that: record only. The owner chose all five pieces and their main figures (below); every "agent
default" waits for the owner's word.

**A design record.** The owner, 23 November 2026, asking how trailers are filled: "Should any of these take/support nutrient paste?",
then "We should also add support for loading via nutrient paste piping", "the Ullage could benefit very much from that", and by card:
all four pieces offered, plus "we could cook paste into meals of their own ... I'm happy if that calls for art assets, paid or
otherwise". Spoilage: "Depends on whether the holding tank(s) are insulated or not!". Facts marked **[code]** were read on 23 Nov 2026
with `scripts/query-defs.py` (Defs as patched in the owner's game) and `scripts/query-code.py` over Vanilla Nutrient Paste Expanded's
1.6 decompile, summarised in our own words; its 1.7 build may differ. **[A]** marks an authored figure.

## Read this first

- **Only with Vanilla Nutrient Paste Expanded** (VNPE), an optional tie-in: without it nothing here appears. The owner also runs its
  Reimagined Progression add-on, which keeps the one paste network **[code]**.
- **One unit of paste is one nutrient paste meal** **[code]**: VNPE's tap draws one unit for each meal it serves.
- **Paste carries its ingredients.** VNPE's vats remember what their paste was made from, and every meal served takes the ingredients
  of every vat on the network **[code]**. A tank must carry that list with the paste and hand it back, or paste made from human meat
  would come out "clean" at the other end: a quiet way round cannibal and vegetarian rules. The list is read and written through the
  game's own ingredient comp, so nothing of ours references VNPE's code.
- **Spoilage follows the tank, as the owner chose.** Our tanks already spoil food by its own days, slower in the cold, and an
  **insulated tank** halves the rate **[code]**. Paste uses that rule with its own figure.

## What the research found [code]

| Thing | Figure |
| --- | --- |
| VNPE's pipe network | `VNPE_NutrientPasteNet`, the only paste network in the owner's game |
| A vat | 360 units in the owner's game (Reimagined Progression's figure); its contents need power |
| A paste meal | 0.9 nutrition, "awful" meal; it starts to rot after 0.75 days once served |
| A simple meal, for comparison | 0.9 nutrition; rots after 4 days |
| Our tank's pipe port | Already moves chemfuel and deepchem through Vanilla Expanded Framework's pipes (`Port_PipeNet`) |
| Our insulated tank | Food spoils half as fast; contents take about a day to reach the outside temperature |

## The pieces

| ID | Piece | What it does | New code |
| --- | --- | --- | --- |
| NP1 | **Ullage on paste pipes** | A new tank content, nutrient paste. Set up on **Load** against a paste pipe it draws from the vats, taking their ingredients; on **Unload** it fills another network's vats and registers its ingredients in them. Also loads from and unloads to paste meals in stacks beside it | An ingredient list per compartment, saved; the pipe port's two hooks to read and write it |
| NP2 | **Eaten from a tank on the road** | Once a caravan's packs hold nothing anyone can eat, a hungry person eats a paste meal made from any tank it carries (one unit, with its ingredients, through the game's own eating, so moods and ideology rules apply); the caravan's days of food count it | A paste source beside the trailer food already built |
| NP3 | **A paste bowser** | A third Pintle bowser on the water and fuel bowsers' chassis, holding only paste, so a light truck can supply an outpost's vats | A trailer Def and its art (paid: the tank's colours, a stripe in its own colour) |
| NP4 | **A paste tap at the Billy** | A set-out piece of the field kitchen: a tap that serves paste meals, drawing from a paste tank of ours set up on **Unload** within hose reach | A dispenser of ours on the game's own, fed from a tank; its art (paid) |
| NP5 | **Cooking paste into meals** | Recipes at the game's stoves and the Billy's range and kettle that turn paste meals and a little fresh food into a meal of ours, better than paste | Recipes and a meal Def; the meal's art (paid) |

## The four questions

| Question | Answer |
| --- | --- |
| What it lets the player do | Feed an outpost or an expedition from the home vats; move paste between networks; eat on the road without packing meals; turn paste into something people do not hate |
| What it stops them doing | Leaving paste in a warm, plain tank for long: it spoils |
| How it changes play | The paste vat becomes a supply depot; insulation and the season matter for a paste run; a paste tanker is worth guarding |
| What outside the vehicle notices | The colonists (meals, moods, ideology rules through the ingredients), the vats and taps on the networks, the caravan's days of food, the cooks |

**Risk: low** (agent's grade, for the owner to confirm): spoiled paste is lost by the unit, with the tank's warning first, as for our
other foods. No new setting; the existing **Field kitchens cook in a caravan** setting covers NP5 on the road.

**Other factions:** none bring trailers or tankers of paste; a captured paste tanker or bowser is loot with whatever it holds.

## The owner's choices (23 Nov 2026, by card)

| Choice | The owner's answer |
| --- | --- |
| Which pieces | All five, NP1 to NP5 |
| Spoilage | By insulation: **4 days in a plain tank, 8 in an insulated one**, slower in the cold as the tank rule already does |
| NP5's meal | **Both grades**, "and it could depend on the upgrades/customizations of the vehicle/trailer itself" |
| NP4's source | **Both**: a paste tank of ours on Unload within reach, and a small paste tank of the Billy's own |

**How the agent reads "depends on the upgrades" (agent default, for the owner to revise):** at home, the game's stoves cook both
grades. On the Billy, the plain kitchen cooks the simple bake; a new Billy upgrade, **paste kitchen**, adds its own paste tank, the tap
and the fine bake. On a caravan's night camp, the fine bake needs the Billy to carry that upgrade.

## Figures first proposed (agent's; superseded where the owner chose)

| Figure | Proposal | Why |
| --- | --- | --- |
| Paste's spoil time in a plain tank | 2 days at room warmth, slower in the cold; an insulated tank 4 (**the owner chose 4 and 8**) | Longer than a served meal's 0.75 days (it is sealed), far shorter than a powered vat (which keeps it) |
| Weight | 0.44 kg a unit | A paste meal's own mass |
| The paste bowser | 2,500 units, as the other bowsers | One chassis, one size |
| The tap's reach | 10 cells, a hose's | As tank to tank |
| NP5's meal | "Paste bake": a simple-grade meal from 0.6 nutrition of paste and 0.3 of vegetables or meat | Stretches fresh food, lifts paste from awful to simple |

## Order (agent's proposal)

NP1 first (the Ullage, the owner's first wish), then NP2 (it makes NP1 matter on the road), NP3, NP4 and NP5 with their art.

## The Billy's paste, after the owner's report (27 Nov 2026)

The owner found the Billy's paste counted nowhere though the paste kitchen was fitted, and no paste pipe beneath the trailer ("this
problem goes deeper"). What was wrong, and what Framework 0.347.0 and Vehicles 0.129.3 did (not yet tested in game: rows 687 and 688):

| Fault | Cause | Now |
| --- | --- | --- |
| No paste on the Billy's panel | The paste lived only in the set-out tap; the trailer's panel showed its water tank and nothing of its pieces' stores | The panel lists every piece's store, standing or packed |
| Nothing at all until a set-up | The tap was made at the next set-up after the upgrade | Made packed as soon as the upgrade is bought |
| A packed tap forgot the bigger tank | It looked up its trailer only among standing pieces | It finds the trailer holding it too |
| Packed paste never spoiled | Packed pieces were aged only when they held items (the cold box); the tap holds none | A packed piece's store is aged too |
| No paste pipe beneath it | The pipe link laid a net beneath set-out pieces only for Vanilla Expanded Framework's own resource comps; ours is a store of ours, and a packed trailer laid only its tank's nets | The store's load's nets are laid beneath the tent and tap, or the packed trailer |
| The pipe would carry nothing | The tap filled only from a tank's hose | A run with vats fills the store at each pipe look, with the vats' ingredients (**agent's choice**: the pipe shows, so it should carry) |
| A caravan could not eat it | Caravan food read tanks only | It eats from a packed store after its packs and trailers, and its days of food count it |

Only the Billy's paste tap keeps a store of this kind, so no other vehicle or upgrade had these faults; its other upgrade pieces burn
chemfuel (the line was already laid) or use Dubs water (its own plumbing link). **Still a limit:** the caravan-forming window does not
count paste in tanks or a packed tap.

## The paste bowser's upgrades

The fuel bowser's, copied (NP3): the lagged tank, which halves the paste's spoiling, and the second hose.

<!-- upgrade-tree: Phobos_PasteBowser_UpgradeTree -->
```mermaid
flowchart TB
  PhobosBowserLagging["Lagged tank"]
  PhobosBowserSecondHose["Second hose"]
  PhobosTrailerElectricPump["Electric pump"]
```
