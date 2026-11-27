# The radio as the comms console (27 Nov 2026)

**Status (27 Nov 2026):** shape S2 is built in Radio Framework 0.53.0 (rows 725 to 728), not yet tested in game, with the benefits and
downsides the owner chose ([as built](#as-built-radio-framework-0530)). S3, the console needing reach, is built in Radio 0.64.0 with HF's skywave, a setting off by default, as the owner chose (row 782).

## Read this first

- **The owner has answered this once already: "Later, with HF"** (27 Nov 2026, [radio ties](radio-ties-with-other-mods.md) row RT6,
  "The comms console is a long-range station"). Asked again at the radio desk the same day, the owner said: **record it first, and
  "if we are to do this, it must come with its own benefits to make it worthwhile (and possibly downsides as well to weigh things
  out?)"**. This record lays out those benefits and downsides; the timing stays the owner's.
- **Today the radio reaches 100 tiles at most** (`factionSearchTiles`), and on VHF and UHF far less without relays. Most factions'
  settlements are further. Anything that makes a faction call need radio reach would cut most of them off **until HF exists**, which is
  why RT6 waits for it. Every shape below either waits for HF, or adds to the console without taking anything away.
- Facts about the game and other mods are from the decompiled code and the Defs index (27 Nov 2026), marked **[game]** or with the
  mod's name. Everything proposed is the agent's **[A]**, for the owner to revise.

## What the console does today [game]

- **It reaches every faction, anywhere**, hostile ones included, and every passing trade ship. No distance is read anywhere: a
  requested caravan always takes the same two days, military aid the same time from across the planet as from next door.
- **The faction call** (`FactionDialogMaker`) offers, from a home map only: a trader caravan (allies, −15 goodwill), an orbital
  trader (allies, −30), military aid (allies, −25), an AI persona core's location (goodwill 40 or more, 1,500 silver), and Royalty's
  title options. Goodwill only ever falls, as the price of a request. There is no "make peace" in the game itself.
- **Other things that need a console:** ransom demands (only with a powered console), the orbital trader's arrival letter (held until a
  console exists), Biotech's radio talking for children (the game's own `CommsConsole` Def only), and a drop spot for traders.
- **What other mods in the owner's list hang on it:** RimPacts gates its Expo and Summit on a working console; The Dead Man's Switch
  Expedition jams it; Vanilla Expanded Framework adds hireables, VFE Deserters, RimMarket, RimPacts and VGE Chapter 2 add call targets;
  Research Commission and VGE Chapter 2 add jobs that cast to the console class. Nine mods patch the faction dialog, **our own
  Framework among them** (the allied supply drop, 15 goodwill, with no radio check).

## What our radio already knows about factions

`FactionDoctors` already finds which faction settlements the colony's stations reach, through relays, by the link budget: within 100
tiles, allies first, each settlement modelled as a 50 W radio on a 20 m mast (authored). The neighbours' calls, radio friends, other
factions' doctors and the logbook's "settlements in reach" all use it. So **"which factions does the radio reach, and how well" is
already answered**; the console question is only what that answer should change.

## The shapes it could take [A]

| | Shape | What changes | What it needs |
| --- | --- | --- | --- |
| **S1** | **The radio desk is a second console, reach-limited** | Our station becomes the game's console class, so the game and every mod that looks for a console count it; its menu offers only factions its radio reaches. The game's own console is untouched and still reaches everyone | A change of the stations' class (saves: see the downsides); the reach filter |
| **S2** | **The radio desk adds calls of its own** | Our desk gets its own "call on the radio" menu for factions in reach, with the benefits below. The console stays as it is; nothing of the game's or other mods' changes | Our own menu and dialog options; no change of class, no Harmony |
| **S3** | **The console needs reach (RT6 as recorded)** | The game's console itself can call only factions the radio reaches; with HF, most of the planet | Waits for HF, as the owner chose; a setting, off by default (RT6 graded it high) |
| **S4** | **Nothing until HF** | As RT6's answer stands | — |

**The agent's lean: S2 now, S3 with HF.** S2 brings the benefits without taking anything away from the console or breaking the nine
mods that patch it. S1 makes our stations count for ransom demands, RimPacts and Biotech's radio talk (worth having), but the change of
class touches saves and every mod that casts to the console.

## Benefits of its own [A]

What a radio call could give that the console does not, each tied to a figure the radio already has. **The owner picks which, if any.**

| # | Benefit | How it would work | Why it is the radio's |
| --- | --- | --- | --- |
| B1 | **Help from nearby comes sooner** | A trader caravan or military aid requested by radio from a settlement in reach leaves from that settlement and takes the travel time from there, where the console's is a fixed two days | The radio reaches the settlement that is near; the console reaches a faction in the abstract |
| B2 | **A clean line costs less goodwill** | A request over a clean link costs less goodwill than over the console (say −10 for a caravan where the console takes −15); over a marginal link, the same as the console | The link's quality is the radio's own figure, shown everywhere already |
| B3 | **No console needed** | A colony with only a radio station can call the factions it reaches: an early, cheap road to trade (60 W against the console's 200 W) | The station is cheaper and earlier than a console |
| B4 | **News from neighbours** | An ally in radio reach warns of a raid or a passing trader before it arrives (the radio friends' "news" option, offered on 27 Nov and not chosen then) | Only a neighbour who can hear you can warn you |
| B5 | **Check-ins** | A short call to an ally in reach, now and then, earns a little goodwill, as the radio design's "scheduled ally check-ins" (`radio-framework-design.md`) | The game has no way for goodwill to rise by talking |
| B6 | **A QSL card for the call** | A first diplomatic call to a settlement earns its card, as a neighbour's call does | The logbook already keeps them |

## Downsides [A]

| # | Downside | Weight |
| --- | --- | --- |
| D1 | **Only factions in reach**: about 100 tiles at most today, far less on VHF without relays and high ground | Large until HF; none for S2, which adds and takes nothing away |
| D2 | **A busy desk**: the call takes a seat and a channel (Radio 0.51.0), so it competes with chats, lessons and vehicles driven by radio | Small; it is how the desk works now |
| D3 | **Jamming and weather cut it**: a jammer near the station, or a solar flare, ends the call (the game's console already fails in a solar flare) | Small, and the radio's own rules |
| D4 | **Others hear you**: a call is a transmission, which direction finding could one day trace (EW15, "we can be found too", a proposal) | None today |
| D5 | **Compatibility (S1 and S3)**: nine mods patch the faction dialog, five add call targets, two cast to the console class; a change of class could leave stations built before as plain buildings until rebuilt (unverified) | High for S1 and S3; none for S2 |

## The four questions

- **What it lets the player do:** call nearby factions from the radio, with benefits tied to how well the radio reaches them.
- **What it stops:** nothing, in S2; in S3, calling factions out of reach.
- **How it changes the way they play:** relays and high ground matter to trade and diplomacy; a station placed well is worth goodwill.
- **What outside notices:** other factions (goodwill, caravans, aid), RimPacts (if S1 or S3), the logbook.

## Risk and settings

S2 risks nothing the player has: **low**, no setting needed. S3 would refuse a call the console makes today (RT6 graded it **high**, "an
aid call refused in the middle of a raid"): **off by default**, as RT6 says.

## Cost

S2: picking which factions to offer reuses `FactionDoctors`' reach, kept a game hour; a call costs what a neighbour's call costs. B1 adds
one travel-time lookup a request. Nothing runs on its own except B4 and B5, each a check once a game day at most.

## Questions for the owner

To be put as cards when the owner reads this:

1. **Which shape**: S2 now and S3 with HF (the agent's lean); S1; S3 only, with HF; nothing until HF.
2. **Which benefits** (several may be chosen): B1 to B6.
3. **Which downsides to keep as rules**, where a shape allows a choice (D2 and D3 come with the radio anyway).

## The owner's answers (27 Nov 2026)

Each card as put, the chosen option first and marked.

| # | Card | Chosen | Not chosen |
| --- | --- | --- | --- |
| 1 | Which shape | **S2 now, S3 with HF** (the agent's lean) | S1, a second console. S3 only, with HF. Nothing until HF |
| 2 | Which benefits (several could be chosen) | **B1 sooner help, B2 cheaper on a clean line, B4 news from neighbours, B5 check-ins** (B3 and B6 come with any shape) | None left out |
| 3 | Which downsides besides the radio's own | **Both: a marginal line can mishear, and others can listen**, and the owner's words: "We could possibly solve the latter with encryption down the line. Here on Earth, that's not allowed on the amateur bands but on Rimworld, it's a lawless place, largely. Actually, that itself could maybe bring its own trouble, if you choose to use encryption." | Only the radio's own (the agent's lean) |
| 4 | B4, how news comes | **A raid warning**: the raid held back a few hours (the agent's lean) | Who's coming: information only, when the raid arrives. Traders and visitors: allies tell of them |
| 5 | What a hostile listener does | **Ambushes the requested trader**: it arrives hurt or not at all; moderate risk, on by default (the agent's lean) | Raids you sooner: high risk, off by default. Note it for later, for direction finding |
| 6 | How often a marginal line mishears | **One in four** (the agent's lean) | One in two. No requests at all on a marginal line |
| 7 | Encryption | **Record it for later** (the agent's lean) | Design it with this slice. Not at all |

## As built (Radio Framework 0.53.0)

| Piece | Where |
| --- | --- |
| The call menu on a station, for a selected colonist, refused with its reason | `CompRadio.CompFloatMenuOptions`, `Work/CallFaction.cs` (`CallFactionMenu`) |
| The call: a seat and a channel, then the game's negotiation window | `JobDriver_PhobosCallFaction`, `UI/RadioCallDialog.cs` |
| The requests, their prices, mishearing, check-ins, listeners and ambushes | `Services/RadioDiplomacy.cs`; caravans on their way and check-in times are saved in `GameComponent_PhobosRadioDiplomacy` |
| The raid warning, and the radio's first Harmony patch | `Services/RadioNews.cs`: a prefix on `Storyteller.TryFire` |
| The figures | `RadioRulesDef`, the block "Calling other factions by radio" |
| Seen in play | `radio.faction-call` (725), `radio.call-misheard` (726), `radio.trader-ambushed` (727), `radio.raid-warning` (728), `radio.trader-arrived` (725) |

- **Which factions** [A]: those whose settlement the colony's stations reach, through relays (`FactionDoctors.ReachToTalk`, kept a game
  hour), and who talk on the radio (industrial or better, not hostile). The console still reaches all.
- **The game's conditions and cooldowns are shared** with the console: an ally only, the season's temperature, a trader every 4 days,
  aid every day, no aid while their enemies visit. A request by radio and one by console cannot both be made.
- **B1** [A]: the caravan's time is the settlement's distance in tiles at a walking caravan's pace (the game's 3,300 ticks a tile), times
  1.5 for the ground, at least an hour, never more than the console's two days. An estimate: the path is not walked.
- **B2** [A]: 10 goodwill for a caravan and 20 for aid over a clean line, the console's 15 and 25 otherwise.
- **B4** [O, A]: the storyteller's raid on a colony with an ally in reach is queued three hours later [A], with a letter. Any ally in reach
  warns [A]; the raiders' path is not worked out. A raid a quest or the dev tools start, or one already queued, is never held.
  **Why the patch skips the original** (the code rules ask for the reason): the game has no other point at which a raid can be held
  without firing it; the held raid fires later through every other mod's patches, as any raid does. Vanilla Expanded Framework's prefix
  on the same method runs first; when it has skipped the incident, this does nothing.
- **B5** [A]: +2 goodwill, every 5 days for each faction, none above 75, and 200 Social experience for the caller.
- **B6**: the first call to a settlement earns its QSL card, as a neighbour's call does.
- **Mishearing** [O]: one in four on a marginal line; the goodwill is spent and the cooldown is not.
- **Listeners** [O, A]: each hostile faction with a settlement within the rules' 100 tiles that hears one of the stations directly, at
  least faintly, adds a one-in-four chance [A] that a requested caravan is ambushed; ambushed, it is lost or arrives hurt, half and half
  [A]. Hurt: about half its people take a cut or two [A]. Setting "Hostile listeners can ambush a trader you ask for", on by default.

## Encryption, for later [O, recorded]

The owner's idea, recorded for a later slice, not built: an **encryption fitting** for stations and vehicles, so a listener hears a call
but cannot understand it, and the ambush above cannot follow from it. On Earth it is not allowed on the amateur bands; on the Rim it is
the colony's choice. **Its own trouble** [A, to be designed with it]: a faction that hears encrypted traffic distrusts it (a little
goodwill lost with those that hear it), and a hostile that hears it takes the colony for a military station and takes more interest in
it. It ties to direction finding (EW15, "we can be found too") in [electronic warfare](electronic-warfare.md).

## Sources

- The game's own code, read decompiled on 27 Nov 2026: `Building_CommsConsole`, `FactionDialogMaker`, `CommsConsoleUtility`,
  `PassingShip`, `IncidentWorker_OrbitalTraderArrival`, `LearningGiver_Radiotalking`.
- The other mods' patches as `scripts/query-patches.py` and `scripts/query-defs.py` list them (27 Nov 2026); their bodies were not all
  read.
- [Radio ties with other mods](radio-ties-with-other-mods.md) (RT6, RT9), [electronic warfare](electronic-warfare.md) (EW15), the radio
  [design](radio-framework-design.md) (scheduled ally check-ins; the open question "replace the vanilla comms console, or upgrade it?").
