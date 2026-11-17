# Topping up machines that hold a material (18 Nov 2026)

**Status (18 Nov 2026):** built in D'thorga Framework 0.134.0 (the work, the Def type, the setting) and D'thorga Patches 0.22.0 (the drone platforms of Fortified Features Framework); nothing else is named yet. Not yet tested in game.

## Read this first

- **What it is:** hauling work that refills machines which hold a store of one material and only offer a right-click order to fill
  it. Which machines is said in data (`PhobosTopUpDef`), never in code.
- **Why:** the owner, 18 November 2026: with The Dead Man's Switch's engineering drone bays "colonists do not refill them with steel
  automatically and it must be an entirely manual process"; "genericize this where possible in Framework too because there's a
  number of areas in my game that are like this, and I'll be bringing them to you now as I encounter them".
- **A new case is one Def**, in the IfModActive folder of the mod whose machine it names. No C# and no patch.
- **It reads other authors' code by member name.** A renamed member makes that Def stand down with one warning; nothing breaks.

## What it lets the player do, stops them doing, and changes

- **Lets:** leave such machines alone; haulers keep them filled as they keep a generator fuelled.
- **Stops:** nothing. The right-click order still works, and the setting switches the work off.
- **Changes:** steel is drawn from stockpiles without an order, so a colony short of steel sees it go to the platforms. Hauling
  priority decides when; the work sits below ordinary hauling (priority 20 in Hauling, authored).

## What was found [code]

Read from Fortified Features Framework's build of 12 Nov 2026 (the installed one), decompiled into `.local/decompiled`:

- The drone platforms (`DMS_EngineeringBay`, `DMS_TrashBin_Base`, `DMS_Hound_Base`, the FPV nest) carry `Fortified.CompMechPlatform`,
  which holds the steel in its own container and spends `CostPerPawn` of it on each drone it sends out.
- Its only way in is `CompFloatMenuOptions` (`CompMechPlatform.cs:493-518`): a right-click **Fill** order that makes the game's
  haul-to-container job for `AmountToAutofill` of `Props.fixedIngredient`, with further stacks queued. No work giver names the comp
  (`python scripts/query-code.py refs CompMechPlatform`), so nothing is ever hauled unasked. This is a missing convenience in
  another author's design, not a fault.
- `AmountToAutofill` is `maxToFill` less what it holds, and **`maxToFill` is set once, to the platform's starting store**
  (`CompMechPlatform.cs:485`); no gizmo changes it. So the Fill order, by hand or ours, fills an engineering bay to 60 steel, not to
  its capacity of 360.
- The comp follows the game's own carrier mechanoid closely (the same field and property names).

## How it works

| Piece | What it does | Where |
| --- | --- | --- |
| `PhobosTopUpDef` | Names a comp class and three member paths on it: how much more it wants (a whole number), of what (a ThingDef), and optionally the least worth a walk. Dots walk into members (`Props.fixedIngredient`); private members and base classes are found | `src/PhobosFramework/TopUp/PhobosTopUpDef.cs` |
| `TopUps` | Finds, once, every building Def carrying a named comp; says which of the player's buildings on a map want enough; makes the job | `src/PhobosFramework/TopUp/TopUps.cs` |
| `TopUpRule` | Game-free: worth a walk once the least load is missing (an order by hand always); bring what is wanted less what others already carry | `src/PhobosFramework/Rules/TopUpRule.cs`, tested |
| `WorkGiver_PhobosTopUp` | Hauling work; skipped at once with no Def, the setting off, or nothing wanting | `TopUps.cs`, `Common/Defs/Phobos/Framework/TopUp.xml` |
| The job | The game's own haul-to-container with the further stacks queued: exactly what the machine's own Fill order gives, so it fills as it does by hand | `TopUps.JobFor` |
| Drone platforms | `PhobosTopUp_FortifiedMechPlatform`: `AmountToAutofill`, `Props.fixedIngredient`, least load `CostPerPawn` (one drone's worth) | `mods/PhobosPatches/Mods/Fortified/Defs/TopUp_Fortified.xml` |

**Cost to run.** Nothing ticks and nothing is saved. A hauler looking for work reads each named building's want through reflection
(a property read or two per building); with no named building on the map the work giver returns at once. Finding material is the
game's own search, run only for a building that wants a full least load.

**Names read at run time.** The three member names are in a Def, so `scripts/check-targets.py` cannot check them against the other
mod's assembly; they are checked at start-up instead, and the load line names any Def that stands down. Teaching check-targets to
read `PhobosTopUpDef` files is a possible later step.

## For other modders

```xml
<Phobos.Framework.TopUp.PhobosTopUpDef>
  <defName>MyMod_TopUp_Hopper</defName>
  <compClass>MyMod.CompHopper</compClass>
  <wantedMember>SpaceLeft</wantedMember>
  <ingredientMember>Props.material</ingredientMember>
  <leastLoad>20</leastLoad>
</Phobos.Framework.TopUp.PhobosTopUpDef>
```

The comp (or its parent thing) must be a container the game's haul-to-container job can fill: an `IThingHolder` whose
`GetDirectlyHeldThings` accepts the material. `leastLoadMember` reads the least load from the comp where it has a natural one.

## Limits and open questions

- **Fill level.** Platforms fill to their starting store, as their own order does. **For the owner:** should ours fill them to full
  capacity instead (360 steel in an engineering bay)? It would need the Def to read capacity and holdings, a small addition.
- **Buildings only** (agent default). A platform carried by a mechanoid, such as The Dead Man's Switch's carrier automatroids, is not
  topped up: hauling to something that walks is another job.
- **Other factions.** The work is the player's haulers' only; another faction's platforms start full in Fortified's own code and are
  not touched.
- **The next cases** are the owner's to bring as they meet them; each is one Def if the machine reads like this one.

## Who chose what

- **Owner (18 Nov 2026):** that the drone bays should be refilled automatically, and that the answer be generic in the Framework.
- **Agent defaults:** the setting and its default (on); the work's priority; one drone's worth as the least load; buildings only;
  filling to the level the machine's own order reaches; the Def living in D'thorga Patches under Fortified's folder.

## Sources

- Fortified Features Framework (AOBA), its `CompMechPlatform`, read from the installed assembly; summarised in our own words.
- The Dead Man's Switch (AOBA), `1.7/Defs/Things_Building/DMS_DronePlatform.xml`, for which buildings carry the comp and their figures.
