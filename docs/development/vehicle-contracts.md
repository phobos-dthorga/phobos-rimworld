# Contracts: settlements hire the player's vehicles (26 Nov 2026)

**Status (26 Nov 2026):** built in D'thorga Framework 0.308.0 (the contract system and the spare-parts kind; the reach fixed in 0.308.1) and D'thorga Vehicles 0.122.0
(the six truck kinds), and EM9's tow and casualty in Framework 0.311.0 ([at a place on the world
map](#at-a-place-on-the-world-map-the-tow-and-the-casualty-framework-03110)); not yet tested in game (rows 551 to 554 and 557 to 559 of
[the test pass](in-game-test-pass.md)).

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

1. A nearby settlement, within **32 tiles**, the trade request's own reach (Framework 0.308.1). 0.308.0 cut it to 24 on the agent's
   claim that trucks are slower than pack animals; the owner questioned it, and it was wrong: a truck drives far faster than a pack
   animal walks, and the time limit is the game's estimate for a plain walking caravan, so a convoy has time to spare. What limits a
   truck is ground it cannot cross and fuel, neither a reason to shorten the reach.
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

## At a place on the world map: the tow and the casualty (Framework 0.311.0)

The owner asked "Build the tow and casualty flight next" (26 Nov 2026) and answered four cards the same day:

| Question | Owner's answer |
| --- | --- |
| The tow | **Either way**: tow it to their settlement and hand it over, or mend it where it stands and it drives home |
| The casualty | **Any caravan, the usual time**: offered to anyone, brought home alive by any means |
| Danger at the place | **As the game's rescues, with its own setting** (high risk, so off by default) |
| Rewards | **The game's own**, as the first seven contracts |

**What it does to the player.** It **lets** them help a neighbour with the fleet: a recovery truck and a winch earn goodwill and rewards,
and an air ambulance has a reason to fly. It **stops** nothing they could do before. It **changes** what a convoy carries (spare parts,
a winch, medicine) and gives a journey a goal beyond trade. **Who notices:** the settlement's faction (goodwill), the game's quest window
and rewards, the world map (a new place), and the colony (the target lives among them while in their care).

**Risk (agent's grade):** with the setting off, low: the place is quiet, and a casualty dies only of their own wounds, which carry no
bleeding (the game's own rule for its refugee). With it on, high: the game's own rescue threats.

### The quest (`PhobosVehicleSiteContract`, Framework)

A second quest, shaped line for line as the game's `OpportunitySite_DownedRefugee` [code: Core's `Script_DownedRefugee.xml`]: the
storyteller's points and challenge rating; the asking settlement within 32 tiles; our pick node, told `onSite`; a time limit of **7 to 15
days** (the refugee's own); a site tile near the colony; the threat chance from our node (`QuestNode_PhobosContract_SiteDanger`: the
game's 70% while the setting is on, inside the game's own `QuestNode_ViolentQuestsAllowed`); the site from our part
(`PhobosContractTarget`) and the game's `DownedRefugeeQuestThreat` threats; the rewards; and an end for success, for the target killed
or destroyed, and for the time running out. **The rewards come by drop pod**: the game's caravan reward part pays only into a caravan
the signal names, and a vehicle mended on the spot has none (`QuestPart_GiveToCaravan.Notify_QuestSignalReceived`, read).

The same frequency setting scales this quest's weight, which starts at the refugee quest's own, 1.

### The place and what waits there

`SitePartWorker_PhobosContractTarget` makes the target when the quest is made, as the game's refugee worker does, and holds it in the
site part until the map is made:

- **The stranded vehicle**: one land vehicle with our systems, never a trailer, no more advanced than the faction (a design with no tech
  level counts as industrial), offered only by an industrial or better settlement. The parts that move it are wrecked, the most
  important first, until it cannot move (usually one: the "named part from our fault table" EM9 asked for), and it keeps **40%** of
  its fuel [A] so it can drive once mended.
- **The casualty**: one of the faction's ordinary people, made new, so never a leader or anyone the player knows.

`GenStep_PhobosContractTarget` puts it on the map: the vehicle on open ground it fits (`SlingPlacement.TryFindClear`), the person downed
with wrecked legs and no bleeding wounds, as `GenStep_DownedRefugee` does.

### In the player's care (agent's choice, for the owner to revise)

**The game lets only the player's own people and prisoners into a caravan** (`CaravanFormingUtility.AllSendablePawns`), and keeps the
game's own lodgers out of the caravan window (`Dialog_FormCaravan.AllSendablePawns` passes no lodgers). So when the map is made, the target
**joins the player's faction for the contract**, with its own faction kept on the settlement's contract. It can then be rescued,
carried, winched, mended and taken in a caravan like the player's own, with no patch on the game. **It goes back to its people**:

- at the settlement's **Hand over** button: out of any vehicle, caravan or tow, back to its faction, into the world;
- for a vehicle mended anywhere (checked once a game hour): once it can move again, its people drive it home;
- when the quest ends any other way (time out, the faction turning hostile), from wherever it is, even the colony, with a message.

**What follows** [I]: while in care, a casualty is a colonist: their death brings the game's colonist-death moods, and once healed they
could be set to work until the time runs out. A stranded vehicle in care counts in the colony's wealth on the map it stands on.

## Every option offered (26 Nov 2026)

The owner's word, the same day: "Document the other possibilities before they are lost". Each card as it was put; the chosen option
first and marked.

| Card | Chosen | Not chosen |
| --- | --- | --- |
| Which kinds in the first build | **All seven** | Deliveries only (chemfuel, water, stone, parts; work on site later). Work on site only (survey, bore, power; deliveries later) |
| How a contract pays | **The game's own rewards** | Silver and goodwill only: a fixed payment, simpler and predictable, unlike the game's other quests |
| How work at a settlement happens | **The caravan stays for the hours** | Done on the press: with the right truck, a hand and the fuel in the caravan, one press does the job and takes the fuel |
| How often | **A slider the player sets** (the owner's own answer, then "I'm willing to make exceptions") | As often as trade requests, with an on/off setting. Half as often. Twice as often |
| The tow | **Either way** | Tow it to their town only (claiming it instead keeps it, but the quest fails and the faction is angry). Fix it where it stands only |
| The casualty | **Any caravan, the usual time** | Fly them home, tight time: offered only with an aircraft with the litter kit, a time limit a walking caravan would miss. Bring them to your colony: heal them, then they go home, or offer to join as the game's refugee does |
| Danger at the place | **As the game's rescues, with its own setting (off)** | As the game's rescues, always, with no setting. Never: the place is always quiet |
| How the tow and casualty pay | **The game's rewards, as the other contracts** | Goodwill only: a favour between neighbours. Rewards, and the casualty may sometimes ask to join the colony instead of going home |

## Not built, and why

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
let every contract run out first.** With none open, settlements' saved fields are simply not read. A stranded vehicle or casualty
still in the player's care when the mod is removed would stay in the colony as the player's own.

## Who chose what

- **Owner (26 Nov 2026):** contracts first; build them; all seven kinds; the game's own rewards; the caravan stays for the work; a slider
  for how often.
- **Owner (26 Nov 2026, later):** questioned the 24-tile reach; it is back to the trade request's 32. Then the tow and casualty, with
  the four answers above.
- **Agent (tow and casualty):** the custody design; rewards by drop pod; the stranded vehicle's choice and its 40% fuel; 900 and 700
  silver of worth; the hourly check; the site's label and icon.
- **Agent:** every figure marked [A]; the slider's 25% steps and 300% end; the setting's page; the logbook entry;
  learning while working; the low risk grade; using the trade request's icon.

## Sources

- **RimWorld 1.7** by Ludeon Studios, our decompile, kept in `.local/`, read with `query-code.py`: `TradeRequestComp`,
  `QuestPart_InitiateTradeRequest`, `QuestNode_TradeRequest_Initiate`, `QuestNode_TradeRequest_RandomOfferDuration`,
  `NaturalRandomQuestChooser.GetNaturalRandomSelectionWeight`, `CaravanVisitUtility.SettlementVisitedNow`, `QuestGenUtility.AddSlateVars`,
  `WorldObject.Tick`. The game's `TradeRequest` quest Def and `Settlement` world object Def, read with `query-defs.py`. Summarised in our
  own words.
