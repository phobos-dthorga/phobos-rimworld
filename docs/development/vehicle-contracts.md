# Contracts: settlements hire the player's vehicles (26 Nov 2026)

**Status (26 Nov 2026):** built in D'thorga Framework 0.308.0 (the contract system and the spare-parts kind) and D'thorga Vehicles 0.122.0
(the six truck kinds); not yet tested in game (rows 551 to 554 of [the test pass](in-game-test-pass.md)). EM9's tow and casualty flight are
not built.

Rows EM45 and EM9 of [emergent play](vehicle-emergent-play.md#h-work-on-the-road-26-nov-2026). The owner chose contracts first in group H
(26 Nov 2026, question 8), then said "Build the contracts, please".

## Read this first

- **A contract is the game's own trade request, made for vehicles.** A nearby settlement asks through the quest window; a caravan that
  reaches it presses a button there; the game's own reward picker pays. Nothing new to learn beyond the button.
- **It is offered only while the player owns a vehicle that can do it**, anywhere: on a map, in a caravan, or towed. A colony with no
  survey truck never sees a survey contract.
- **How often is a setting**: "How often settlements offer contracts", on the Crews page of D'thorga Vehicle Systems' settings, 0% to 300%
  in steps of 25%, 100% by default (as often as the game's trade requests). 0% offers none.
- **Work at the settlement keeps the caravan there.** Leaving stops the work and keeps what is done.
- **Removing the Framework with a contract open**: finish or let it run out first. The quest's own parts are ours, so an open contract's
  quest would fail to load without the mod.

## The owner's choices (26 Nov 2026, by card)

| Question | Owner's answer |
| --- | --- |
| Which kinds in the first build | **All seven**: chemfuel, water (Dubs Bad Hygiene), stone, spare parts; a ground survey, a water bore (Dubs Bad Hygiene), power for some days |
| How it pays | **The game's own rewards**, as trade requests use |
| How work at a settlement happens | **The caravan stays for the hours**: the truck burns its own fuel, the best hand works at their skill's pace, leaving stops it |
| How often | **A slider the player sets** ("how often or least often they desire"), and "I'm willing to make exceptions" to the preference for buttons over sliders |

## What it does to the player

| Question | Answer |
| --- | --- |
| **What it lets the player do** | Earn silver, goods, goodwill and royal favour with the trucks' own work: a tanker load of chemfuel or water, a tipper load of stone, spare parts, or hours of a survey truck, drill rig or generator set's work at a settlement |
| **What it stops them doing** | Nothing they could do before. A contract cannot be met with less than it asks; work at a settlement needs the right truck, a fit hand and fuel, and the caravan must stay |
| **How it changes the way they play** | The service trucks have a reason to leave home: a journey with a cargo or a job at its end, timed against the contract's limit, the night's rest and the fuel |
| **What outside the thing notices** | The settlement's faction (goodwill, as for a trade request), the game's quest window and rewards, royal favour with Royalty, the truck's logbook ("Worked a contract for ..."), the hand's skill (they learn as they work) |

**Risk (agent's grade, for the owner to confirm): low.** Nothing is put at risk that the player does not already risk by travelling; a
contract that runs out only fails its quest. So it has no on/off setting of its own beyond the frequency, which reaches 0%.

## How it works

### The quest (`PhobosVehicleContract`, Framework)

Shaped line for line as the game's `TradeRequest` quest [code: its Def in Core's `Script_TradeRequest.xml`, read with
`query-defs.py show TradeRequest --type QuestScriptDef`]:

1. A nearby settlement, within **24 tiles** [A; the trade request's is 32, cut because trucks are slower than pack animals].
2. The game's own time limit from the journey (`QuestNode_TradeRequest_RandomOfferDuration`).
3. **Our node chooses the contract** (`QuestNode_PhobosContract_Pick`): one kind the player's vehicles can do, by each kind's weight; the
   settlement must have no contract open. For work on site, it adds the work's days at the slowest pace (`ContractRule.WorkDays`) to the
   time limit, so a crew that arrives in time can always finish.
4. Our node opens it on the settlement when the quest is accepted (`QuestPart_PhobosContract`), and closes it when the quest ends.
5. The reward: the game's own `QuestNode_GiveRewards`, valued at the contract's worth times the trade request's own wealth factor and
   1.5 to 2.1, paid to the caravan, with goodwill and royal favour allowed.
6. Met: the settlement's faction gains the trade request's own goodwill (`DiplomacyTuning.Goodwill_QuestTradeRequestCompleted`, 12), the
   quest succeeds. Out of time, or the faction turns hostile: it fails.

**How often:** the quest's weight starts as the trade request's, 1.1, and the setting multiplies it. The storyteller's own chooser
(`NaturalRandomQuestChooser`) and traders' quests both read that weight, and 0 takes it out of both [code].

### The settlement's comp (`WorldObjectComp_PhobosContract`, Framework)

Added to every settlement beside the game's trade request comp (`Patches/.../SettlementContracts.xml`). It holds what is asked, the
time left, the hours done and the caravan at work; its line on the settlement's inspect pane says all four. A visiting caravan sees one
button: **Fulfil contract** for a delivery, or **Start the work** / **Stop the work** for work on site. Both use the game's own trade
request icon. A disabled button says why (too little in the tanks, no fit hand, out of fuel).

**A convoy counts as at the settlement only while it is not driving on**: the game reads its own pather, and Vehicle Framework drives a
convoy with another (`Contracts.AtSettlement`, as the depots do).

### The kinds (`PhobosContractDef`)

Each kind is XML, so another mod adds its own. A vehicle fits by what it carries (a comp, or a tank that can hold the fluid), never by
its name.

| Kind | Def | Mod | The truck | Asked | Worth [A] |
| --- | --- | --- | --- | --- | --- |
| Chemfuel delivery | `PhobosContract_Chemfuel` | Vehicles | Any tank that can hold chemfuel (Ullage, fuel bowser) | 400 to 1,000 L, in 50s, at most the largest fitting tank | Chemfuel's own market value a litre |
| Water delivery | `PhobosContract_Water` | Vehicles, Dubs Bad Hygiene only | Any tank that can hold water (Ullage, water bowser) | 600 to 1,500 L, in 50s | 1 silver a litre |
| Stone delivery | `PhobosContract_Stone` | Vehicles | Offered with a Talus tipper; any caravan's stone chunks count, towed cargo too | 40 to 80 chunks, in 5s | 15 silver a chunk |
| Spare parts (EM9) | `PhobosContract_SpareParts` | Framework | Offered to anyone with a vehicle of our systems | 1 to 3 of one named part (engine assembly, drivetrain kit, electrical kit, transmission, hydraulic actuator, electric motor) | The part's own market value |
| Ground survey | `PhobosContract_Survey` | Vehicles | Datum survey truck; hand by Intellectual | 12 to 24 hours | 70 silver an hour; 0.2 fuel an hour |
| Water bore | `PhobosContract_WaterBore` | Vehicles, Dubs Bad Hygiene only | Kelly drill rig; hand by Mining | 18 to 36 hours | 50 silver an hour; 0.6 fuel an hour |
| Power supply | `PhobosContract_Power` | Vehicles | Busbar generator set; any fit hand | 48 to 96 hours, in 12s, **day and night** | 20 silver an hour; 0.375 fuel an hour (its own full load, 9 a day) |

**The figures are the agent's**, chosen so a contract is worth about what a trade request asks (its own range, 500 to 2,500 silver, from
`QuestNode_TradeRequest_GetRequestedThing.BaseValueWantedRange`). Water's worth and a stone chunk's are guesses with no market price
behind them.

### Work on site (`Contracts.WorkStep`)

Every 250 ticks, staggered by settlement, and only while a caravan is at work:

- The caravan must still stand at the settlement; if it has left, the work stops and the player is told once. What is done is kept.
- Unless the work goes on through the night (power), it waits while the caravan rests for the night, as the game rests caravans.
- The truck that fits with the most fuel works; it burns the kind's fuel an hour from its own tank. Out of fuel, the work is held up
  until it is refuelled (the convoy's own refuelling from cargo works there).
- The caravan's best hand at the kind's skill works at `ContractRule.Pace`: 0.6 with no skill, 1.0 at level 10, 1.4 at level 20 [A].
  They learn the skill as they work (0.04 experience a tick [A], about half a mining job's).
- What holds the work up is said once, when it changes, and the work picks up again by itself when it clears.

## Not built, and why

- **EM9's tow and casualty flight**: each needs a place on the world map holding a stranded vehicle or a hurt person, a larger piece of
  work (the owner's card: "EM9's tow and casualty flight come later").
- **Water quality**: a water contract takes any water in the tanks, clean or not [A, a simplification].
- **Wear on the working truck**: only fuel is burned at the settlement; its parts do not wear for the hours [A].
- **The work's own sounds or a picture at the settlement**: none; the world map shows nothing new.

## Other factions' vehicles

| | What contracts do |
| --- | --- |
| **In a raid on us** | Nothing: contracts are offered to the player only |
| **Parked at a base we attack** | Nothing |
| **Captured** | A captured truck counts as the player's own: it can do a contract it fits |

**Cost:** nothing ticks for a settlement with no caravan at work. The storyteller's test walks the player's maps' pawns and caravans
once per quest it considers.

## Saves and removal

New saved state: the contract on each settlement (`PhobosContract*` keys) and the quest's part (`QuestPart_PhobosContract`). **Safe to
add mid-game.** Removing D'thorga Framework with a contract quest open would leave a quest whose part cannot load: **finish, decline or
let every contract run out first.** With none open, settlements' saved fields are simply not read.

## Who chose what

- **Owner (26 Nov 2026):** contracts first; build them; all seven kinds; the game's own rewards; the caravan stays for the work; a slider
  for how often.
- **Agent:** every figure marked [A]; the 24-tile reach; the slider's 25% steps and 300% end; the setting's page; the logbook entry;
  learning while working; the low risk grade; using the trade request's icon.

## Sources

- **RimWorld 1.7** by Ludeon Studios, our decompile, kept in `.local/`, read with `query-code.py`: `TradeRequestComp`,
  `QuestPart_InitiateTradeRequest`, `QuestNode_TradeRequest_Initiate`, `QuestNode_TradeRequest_RandomOfferDuration`,
  `NaturalRandomQuestChooser.GetNaturalRandomSelectionWeight`, `CaravanVisitUtility.SettlementVisitedNow`, `QuestGenUtility.AddSlateVars`,
  `WorldObject.Tick`. The game's `TradeRequest` quest Def and `Settlement` world object Def, read with `query-defs.py`. Summarised in our
  own words.
