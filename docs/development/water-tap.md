# Water tap: research and decisions (4 Nov 2026)

The record behind [D'thorga Water Tap](../../mods/PhobosWaterTap/README.md). **Not yet tested in game.** The player guide is the mod's README; this
holds the search, the code readings and who chose what. The code readings are of Dubs Bad Hygiene (DBH, by Dubwise) on 1.7, decompiled to the ignored
`.local/decompiled/DBH-1.7`, and summarised here in our own words.

## Did anyone already build this?

The owner asked for a tap, and hoped to be proven wrong that nobody had made one. Twenty Workshop searches on 4 November 2026 (water tap, water bottle,
bottled water, water dispenser, water cooler, bottling, and DBH with tap, bottle and storage) found one near miss:

| Mod | What it does | Verdict |
| --- | --- | --- |
| [Dubs Bad Hygiene: Auto Water Dispenser](https://steamcommunity.com/sharedfiles/filedetails/?id=3822969770) (46 subscribers, one release, 20 Oct 2026, 1.6 and 1.7, no comments) | Its page lists a drinking-water dispenser and a medical rehydration machine, with settings for a hydration threshold and a check frequency. Its preview shows water going from a machine into a person in bed | **Probably not a bottle source**: it looks like it hydrates pawns directly. This is my inference from the page and the picture. Its code wasn't read, because it isn't installed and the item page is region-locked |
| [Gravship Water Systems](https://steamcommunity.com/sharedfiles/filedetails/?id=3587768178) | A water generator and tank for gravships | Water supply, not bottles |
| [Dubs Bad Hygiene: Water Pit](https://steamcommunity.com/sharedfiles/filedetails/?id=3668353845) | A primitive water source to drink and wash at | No bottles |

The search can't prove absence, and the near miss could do more than its page says. It could be worth a look for players who want patients hydrated
automatically, which this mod doesn't do.

## How DBH handles bottles

- `DBH_WaterBottle`: stack limit 10, mass 0.5, category Foods, not tradeable. Deteriorates outdoors. The Def carries `DBHThirst="true"`, and a Harmony
  prefix on `DirectXmlLoader.DefFromNode` drops any Def with that attribute while thirst is off. The same prefix drops `DBHLite="true"` Defs in lite mode,
  which includes the Plumbing research. It reads the attribute on any mod's Def, so **our tap uses both attributes and vanishes when its references would**.
- Who finds bottles: `ClosestSanitation.FindBestDrink` lists every `DBH_WaterBottle` on the map through `listerThings`, and needs nothing but forbid, reach and
  reservation checks. So bottles on a storage building need no special support.
- Packing costs 0.5 water per bottle (`SanitationToils.FillBottleFromThing`). The "stockpile 10 or 20" job costs 0.04 a tick for 60 ticks, 2.4 per bottle. The tap
  uses **0.5**, which also matches the bottle's mass: half a kilogram of water is about half a litre. DBH's two figures disagree, so this is a choice.
- `PlumbingNet.PullWater` draws from towers on the net in a random order and stops when satisfied. Its `contam` answer is the worst of the towers it touched, so on
  a mixed net the answer varies. The tap computes the worst of all towers instead.
- A tower's quality is Treated with a working filter on the net and Untreated without one (`CompWaterStorage`), or Contaminated after an incident or similar. So it can change when
  a filter loses power.
- Fixtures join the net through `CompProperties_Pipe` with mode `Sewage`, which covers both water and waste. Its `PostSpawnSetup` destroys a same-mode pipe under the
  building, and the building then conducts.

### The gap the tap would have widened

`CompContamination` holds the bottle's level, and DBH reads it when someone drinks (`IngestedPostfix` to `SanitationUtil.ContaminationCheckDrinking`). But it has no
stack methods. The game's `ThingWithComps.SplitOff` makes a new thing, and only a comp's `PostSplitOff` carries state across, so a split piece gets the default, `Treated`.
Likewise nothing stops a treated and an untreated stack merging. A basin hands bottles straight to a pawn, so this rarely shows. A tap's stack is split every time a pawn
takes bottles, so without a fix **a tap would launder untreated water**.

`CompBottleWater`, patched onto the bottle Def, adds `PostSplitOff` and `AllowStackWith` and a label. It uses comp hooks, so no Harmony. It saves nothing, so it's safe to add and remove.

## Decisions

| Decision | Chosen by |
| --- | --- |
| A tap that makes real bottles, modelled on Vanilla Chemfuel Expanded's chemfuel tap, which the owner showed in a screenshot | Owner |
| A separate mod rather than part of D'thorga Factory Additions, since that mod requires VFE Factory and this one needs only DBH | Agent, while building; owner may revise |
| A real plumbing fixture with a pipe comp, not a building that reads a neighbouring pipe. Factory Additions' oven reads a neighbour so that nothing is deleted on removal. A tap belongs on the network, and the cost is one pipe to re-lay | Agent |
| One stack, set between 0 and 10 with two commands | Agent |
| One bottle per 250 ticks, 240 a day at most; on my estimate of 2 bottles per pawn-day in mild weather, about 100 colonists | Agent, authored |
| No power, 20 steel, 600 work | Agent, authored |
| Storage priority Important, as on the chemfuel tap, so that pawns haul bottles away only if the player sets that up | Agent |
| No colony-wide stock limit. Hauling and belts stop when their storage is full, and the player controls that. Add one if play shows a flood | Agent; open to change |
| Refuse contaminated water, and bottle untreated water as untreated. A tap that refused untreated water would do nothing in most colonies before filtration research | Agent |
| Different water doesn't stack, instead of a merged stack taking the worst level. They are separate bottles, and a clean stack isn't spoiled by one dirty bottle | Agent |
| Apply the stack rule to all bottles, not only the tap's: bottles have one Def | Agent |

## What this changes for the owner's colony

The stack rule means colonists on untreated water can now get sick from bottles, about 0.2% a drink of "drank dirty water" plus diarrhoea (read from the code on 27 Oct 2026). This was
always DBH's intent for a bottle, and a basin already does it. It's the one change the owner might notice without building a tap.
