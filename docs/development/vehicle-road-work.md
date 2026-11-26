# Work on the road: surveys, bores, test holes, depots, ambushes and trailer faults (26 Nov 2026)

**Status (26 Nov 2026):** the running survey (EM30) and the water bore (EM34) are built in D'thorga Framework 0.317.0
([as built](#as-built-framework-03170)), not yet tested in game (rows 565 to 567 of [the test pass](in-game-test-pass.md)); the
reverse-osmosis kit is built in Framework 0.318.0 and Vehicles 0.124.0 ([as built](#the-reverse-osmosis-kit-as-built-framework-03180-vehicles-01240)), not yet tested in
game (rows 570 to 572); the survey halt (EM31), the test hole (EM35), tipping into a depot (EM37), ambush choices (EM44) and wrecked
trailers (EM42) are built in Framework 0.322.0 ([the third round](#the-third-round-the-survey-halt-the-test-hole-tipping-ambushes-and-trailer-faults-framework-03220)),
not yet tested in game (rows 582 to 586).

Rows EM30 and EM34 of [emergent play](vehicle-emergent-play.md#h-work-on-the-road-26-nov-2026), the second round of group H after
[contracts](vehicle-contracts.md). The owner, 26 Nov 2026: "Build the running survey and water bore next", then, on the cards' options
left unpicked, "Document the other possibilities before they are lost". **This record keeps every option offered, chosen or not**, so a
later change of mind starts from the same list.

## Read this first

- **The running survey**: a Datum with its long-range kit, tuned to an ore, in a **moving** convoy, with someone of Intellectual skill
  aboard who is not driving, now and then finds a lump: the game's own long-range mineral quest, its site **moved near where the convoy
  was**. Switched on per Datum.
- **The water bore** (Dubs Bad Hygiene only): a **halted** convoy's Kelly drills a bore and fills the convoy's water tanks with
  **untreated** water, by the tile's rainfall; a desert tile is a dry hole, said before drilling. It burns fuel and **wears the drill
  bit**; a hand with Mining sets the pace.
- **The reverse-osmosis kit** (the owner's idea): an upgrade for the **water bowser and the Ullage** that makes untreated water clean, with
  its own picture. **Built after this batch.**

## The owner's answers (26 Nov 2026, by card), with every option offered

Options are listed as they were put; the chosen one is first and marked.

### The running survey: what it needs

| Option | What it meant |
| --- | --- |
| **Chosen: the long-range kit and a hand** | The Datum with its long-range upgrade, tuned to an ore as on a map, and someone with Intellectual skill aboard who is not its driver; their skill sets the pace. Switched on per Datum with a caravan button |
| Any Datum and a hand | No upgrade needed; any Datum with a non-driver hand aboard finds lumps on the move, of any ore |
| Any Datum, no hand | The console runs itself; any Datum in a moving convoy prospects |

### The running survey: where a find appears

| Option | What it meant |
| --- | --- |
| **Chosen: near the convoy** | A few tiles from where the convoy was when it found it, so the route matters. The game's own quest is made, then its site is moved there before it appears |
| Near home | Where the game puts it for a scanner at home (near the colony): simpler, but the route no longer matters |

### The water bore: what water it gives

| Option | What it meant |
| --- | --- |
| **Chosen: untreated, as a river** | Dubs Bad Hygiene's untreated water, as the tanker's river intake gives: drinkable with its risks, or treated as river water is. Yield by the tile's rainfall; a desert tile gives a dry hole the convoy can see before drilling |
| Clean groundwater | Deep bore water counts as clean, with the same yield and dry holes |

The owner added: "maybe we should offer something like a reverse-osmosis kit as an upgrade? Perhaps with its own, unique, art too?"
(below).

### The water bore: what it costs

| Option | What it meant |
| --- | --- |
| **Chosen: the drill bit wears** | Each bore wears the Kelly's drill bit a little, as drilling on a map does, so many bores mean a new bit sooner. A hand with Mining sets the pace |
| Fuel and hours only | No wear: only the time at the halt, the fuel, and a hand with Mining |

### The reverse-osmosis kit: which vehicles take it

| Option | What it meant |
| --- | --- |
| **Chosen: the water bowser and the Ullage** | Fitted to what carries water: it treats any water pumped into its tank, from a bore, a river intake or a well. It draws power while it works (an electrical load), and its membrane wears and wants replacing. One kit, drawn as a cabinet on the tank |
| The Kelly's bore head | Fitted to the drill rig: only bore water comes out clean. Narrower, but tied to the bore |
| Both, one kit | The same kit fits the bowser, the Ullage and the Kelly, each drawn where it sits |

### The reverse-osmosis kit: when

| Option | What it meant |
| --- | --- |
| **Chosen: next, after this batch** | The running survey and the water bore first (bore water untreated); then the kit as its own batch, with a paid pilot picture (OpenAI, about US$0.2 for two attempts) and a pick card for the owner |
| In this batch | All three together; the paid pilot and the owner's pick before the commit |
| Record only for now | Written into the record with its design; built when the owner says |

## The design (agent's, for the owner to revise)

### The running survey (EM30)

| Question | Answer |
| --- | --- |
| **What it lets the player do** | Prospect while travelling: a journey can end with a lump of ore found on the way |
| **What it stops them doing** | A free survey: the hand at the console is not driving, and the Datum must carry the long-range kit and be tuned |
| **How it changes the way they play** | The route is chosen by the ground it crosses; the Datum earns a place in a trading or contract convoy |
| **What outside the thing notices** | The world map (a new site), the game's own quest window and its letter, the Datum's logbook |

- **When it works**: the convoy is moving (not resting, not stopped), the Datum's switch is on, its long-range kit is fitted and it is
  tuned to an ore, and a person of the player's aboard any of the convoy's vehicles, not in a driving seat, can do Intellectual work.
- **How fast**: the long-range mode's own figures on a map (a find about every 0.4 days of work, at least one by 0.8 days), times the
  hand's research speed, as the deployed truck's work is counted [code: `CompPhobosSurveyTruck`]. Nothing of the map's diminishing returns
  applies: each tile crossed is new ground.
- **The find**: the game's `LongRangeMineralScannerLump` quest, given the Datum's tuned ore and the colony's map as it is on a map; then
  its site, not yet on the world map, is moved to a free tile 2 to 6 tiles [A] from the convoy, near its route. A letter, as the
  deployed truck's find gives.
- **Real basis, unverified**: carborne gamma-ray spectrometry was a real prospecting method; a source is owed before the record leans on
  it.

### The water bore (EM34)

| Question | Answer |
| --- | --- |
| **What it lets the player do** | Cross dry country without carrying all the water: a halted convoy fills its tanks from the ground |
| **What it stops them doing** | Drilling for free (fuel, bit wear, hours), and drilling where the ground is dry |
| **How it changes the way they play** | Water planning on the road, as on a real camping trip; a bowser and a Kelly travel together |
| **What outside the thing notices** | The convoy's water (drinking, washing, cooking), the Kelly's drill bit (its wear), the logbook |

- **When it works**: Dubs Bad Hygiene active; the convoy stopped on the world map (not at a settlement's own tile [A]); a Kelly in it
  with its drill parts able to work; someone with Mining skill fit to work; water tanks in the convoy with room.
- **Yield by rainfall** [A]: the tile's yearly rainfall sets litres an hour; **below 250 mm a year the hole is dry**, said on the button
  before it is pressed. The figures are the agent's: a gameplay simplification, not a hydrogeology model.
- **Its costs**: hours of drilling before the first water, then pumping while the convoy stays; the Kelly's fuel an hour; the drill
  bit's wear, as drilling on a map wears it; the hand's Mining pace (`ContractRule.Pace`'s curve) and their learning.
- **The water** is Dubs Bad Hygiene's untreated water, as the tanker's river intake gives.
- **The first of the "work at a halt" pieces**: a convoy's halt job runs at the rare look the game gives a convoy, staggered, as meal
  halts do.

### The reverse-osmosis kit: the second round of cards (26 Nov 2026, after "Build the reverse-osmosis kit next")

| Card | Chosen | Not chosen |
| --- | --- | --- |
| Where it treats | **Parked and on the road**: also while the convoy travels, on the towing vehicle's or its own alternator (a new power path on the world map) | Parked on a map only, on the electric pump's power (the grid, a power point, or the towing vehicle's battery; the Ullage its own electrics) |
| Reject water | **A quarter is lost**: of every 4 litres treated, 3 stay and 1 runs off as reject (agent's figure: the Army's pages give no recovery ratio) | Half is lost: a harsher, lower recovery, closer to small units treating very dirty water. No loss: every litre treated stays |
| The membrane | **A membrane cartridge item**: crafted at the vehicle parts bench from components and cloth, fitted by a mechanic as a service when spent; its own paid picture, made with the kit's pilot | A service from raw materials: renewed from components and cloth carried to the vehicle, no item and no extra picture. It never wears |
| Rate and draw | **The Army's light purifier**: about 470 litres an hour (the Lightweight Water Purifier's 125 US gallons an hour from fresh water), drawing 1,500 W (agent's figure: that unit runs on a 3 kW generator, and the manual gives no draw); contaminated water too | Half that at half the power: about 235 litres an hour at 750 W. A big tactical unit: about 5,700 litres an hour (the TWPS's 1,500 gallons, a vendor's figure) at a much larger draw, sensible only on the grid or the generator set |

**The sources, read 26 Nov 2026** (by a fetch of each page): the US Army's [TM 10-4610-310-14, "Theory of
Operation"](https://waterdecontamination.tpub.com/TM-10-4610-310-14/TM-10-4610-310-140059.htm) (hosted by tpub.com), for the
Lightweight Water Purifier: "a minimum rate of 125 gallons per hour from a fresh water (less than 20,000 ppm source water) and 75 gallons
per hour from a seawater"; "A 3kW Tactical Quiet Generator is used as the primary source of power supply"; ultrafiltration before reverse
osmosis. The same output figures on the Department of Defense's [Gulflink page on the LWP](https://gulflink.health.mil/water_use/water_use_refs/n32en043/lwp.htm)
(1998). The TWPS's "at least 1,500 gallons per hour from freshwater sources and 1,200 gallons per hour from seawater" is
[Aqua-Chem's own page](https://aqua-chem.com/tactical-water-purification-system/), a maker's claim. **Not given by any of them**: the
unit's own draw, its recovery (reject) ratio, and its membranes' life. Those three figures here are the agent's.

### The reverse-osmosis kit as built (Framework 0.318.0, Vehicles 0.124.0)

The design it was built from was recorded before the second round of cards: an upgrade on the water bowser and the Ullage, treating
water while fitted and powered, a wearing membrane with its own service, a cabinet drawn on the tank. As built:

| What | How (code) |
| --- | --- |
| **The node** | "Reverse-osmosis kit" on the water bowser's tree (17, 2) and, by a Dubs Bad Hygiene patch, the Ullage's (12, 17), where it needs any one of the liquid tanks as the electric pump does. Its stat `Phobos_WaterTreatment` is **470 litres an hour** (the Lightweight Water Purifier's 125 gallons); it rebuilds the vehicle's systems (`PhobosSystemsUpgrade`). It costs 25 steel, 4 components and **its first membrane cartridge** [A] |
| **Treating** | `WaterTreatment` (core): while the kit is fitted, switched on (**Treat water**, on by default), its membrane not spent, and a compartment holds untreated water (a fluid marked `treatable`, Dubs Bad Hygiene's water, above the clean level), it passes the untreated litres through `TreatmentRule`: **a quarter runs off as reject**, the rest is counted on the compartment (`treated`, saved); once all of it has passed, the compartment is clean (quality 0). New untreated water raises the quality again, and the whole compartment is treated anew [A, a simplification: quality is one number a compartment] |
| **Power, parked** | **The Ullage** draws through its own load `waterTreatment` (1,500 W at its voltage, about 62 A at 24 V; shed third with the pump; available only with the kit). **A parked bowser** draws through the grid hook-up beneath it: its conduits are now laid for the kit too, and its meter carries the pump's watts and the kit's together |
| **Power, on the road** | **A bowser in tow** draws through the towing vehicle's new tow load `trailerTreatment`, as the trailer pump does; a trailer has no look of its own off the map, so **the towing vehicle's look treats it**, map or world map. **The Ullage** treats in a caravan on its own electrics, the engine running while the convoy drives |
| **The membrane** | The water treatment system (`PhobosVehicleSystem_WaterTreatment`, Dubs folder), which runs only where the kit is fitted (a new `needsStat` on system Defs, checked in `Enabled()`, so no vehicle without the kit is ever asked for a cartridge). Its `membraneWear` rises by the litres treated, **about 50,000 a membrane** [A]; worn at 70 %, spent at 95 %, when the kit stops. A service fits a **membrane cartridge** (made at the vehicle parts bench from a component, 10 cloth and 5 chemfuel [A]) |
| **The look** | The owner's picks: the cabinet across the bowser's rear deck and on the Ullage's left frame strip, the cartridge, and the kit's icon cut from the cabinet ([the art record](../../assets/phobos-vehicles/requests/ro-kit-paid-record.md)) |

**What the power figures mean in play** [I]: 1,500 W is about 62 A at 24 volts, so the Ullage's 100 A alternator carries the kit and its
40 A pump together only just; with the engine off, the battery goes flat in about an hour and a half. On a 12-volt tow vehicle the kit
draws about 125 A, and it is shed first, with the trailer pump, when the charging cannot keep up. A big treatment is a job for the grid, or for
the road.

**What outside notices**: the colony's and the caravan's drinking water (Dubs Bad Hygiene reads the quality when anyone drinks), the
grid's power, the towing vehicle's electrics and battery, the mechanics (the membrane's service), the crafters (the cartridge).

**Risk (agent's grade): low.** It changes nothing it is not switched to; a flat battery is the load-shedding the electrics already have.

## As built (Framework 0.317.0)

What differs from the design above, or was settled while building:

- **The running survey's state is the Datum's own** (`CompPhobosSurveyTruck`: its switch and its days towards a find, saved); a world
  component (`WorldComponent_PhobosRoadWork`) steps every player convoy a tenth of a game hour, only while Vehicle Framework's own pather
  says it is moving. The find is `RoadWork.FindOnTheRoad`: the game's quest, made with the colony's map standing in, then its not yet
  spawned site moved with `TileFinder.TryFindNewSiteTile` to 2 to 6 tiles from the convoy [A]. The quest's own text may still speak as
  if from the colony [I]. **No colony map, no find** (the game's quest needs one).
- **The running survey's buttons**: Survey on the road (a switch) and the truck's own ore button, on the Datum in a caravan with the
  long-range kit fitted. The switch borrows the long-range button's picture until its own is drawn.
- **The water bore is `RoadWork`'s bore job** (`RoadBore`, saved with the world): drilling to `BoreRule.DrillHours` (8 [A]) at the
  Mining hand's pace (`ContractRule.Pace`), by day only; then pumping, day and night, `BoreRule.LitresPerHour` by the tile's rainfall
  (0.8 L an hour a millimetre above 250, at most 1,000 L an hour [A]: a gameplay simplification, not hydrogeology), until the convoy's
  water tanks are full. It ends when the convoy drives on or the player stops it; what holds it up is said once.
- **A correction to the card**: the bit-wear option said a bore wears the bit "as drilling on a map does". On a map the bit wears only in
  overdrive [code: `CompPhobosRigWork.RunOverdrive`]. So the bore has its own rate: **0.25 health an hour of drilling** [A], about a dozen
  bores from a new 25-health bit. A wrecked bit refuses the bore.
- **Fuel**: 0.6 an hour of the Kelly's own tank, drilling or pumping [A], as its water bore contract.
- **Filling**: caravan water's source (Dubs Bad Hygiene's tie-in) gained `PutUntreated` and `Room`: every compartment already set to
  water, then each crew water tank aboard, at Dubs' untreated level, as the river intake fills. A compartment never set to water is
  left alone [A].
- **Not at another faction's settlement** [A]: that is the settlement's ground.
- **The bore button** borrows the water drop's picture until its own is drawn. **Owed**: both buttons' own icons, for the local night run.
  **Queued for night 16** (the owner, 26 Nov 2026: "Add the local GPU icons to the queue for later tonight when I'm asleep, so it doesn't
  get lost"), with the three new buttons below: `assets/vehicle-systems/requests/night-16-road-icons.json`.
- **Logbook**: "Found ... on the road" on the Datum, "Drilled a water bore ... of water" on the Kelly.

## The third round: the survey halt, the test hole, tipping, ambushes and trailer faults (Framework 0.322.0)

The owner, 26 Nov 2026: "build EM31, EM37, EM35, EM44, and EM42 next, please". No cards were asked for this round: the owner's earlier
answers (EM35's insects, EM44's roll, EM42's "warn, then allow") are kept, and **everything else here is the agent's, for the owner to
revise** (the open choices are in `config/decisions.json`). Not yet tested in game (rows 582 to 586 of [the test pass](in-game-test-pass.md)).

### The third round: read this first

- **The settings**, in D'thorga Framework's vehicle settings, Camps section: **Choose how to meet an ambush** (on), **Trailers on wrecked
  gear crawl** (on), **Insects in test holes** (off, the owner's card: high risk).
- **The survey halt and the test hole go together**: the Datum reads a tile first; only then can the Kelly prove it. **A tile is read
  once and proved once**: the ground does not change.
- **The ambush choice comes only to a convoy on the move.** A convoy camped for the night, or stopped, is caught standing and fights,
  as the game has it.
- **Simplification:** a tile's reading is not the ore a map opened there would scatter: the game makes a map's deep ore only when the map
  is made. Making the two agree is EM32's work, still to come.

### The shared piece: halt jobs

The bore's code became the shape every halt job shares (`RoadHaltJob`): a convoy's vehicle, a refusal that says why it cannot start or go
on, a step of work a tenth of a game hour, and one ending (the convoy drives on, the player stops it, or it finishes). **One job per
vehicle at a time**; the Datum and the Kelly may work at the same halt. The bore keeps its saved keys and its own list, so a save made
with Framework 0.317.0 to 0.321.0 loads with its bores in place.

### The survey halt (EM31)

| Question | Answer |
| --- | --- |
| **What it lets the player do** | Decide whether a tile is worth the Kelly, and whether a bore will come up dry, before spending a day on it |
| **What it stops them doing** | Reading a tile twice for more: the reading is fixed for the tile |
| **How it changes the way they play** | A survey becomes a stop the convoy plans for, and the Datum and the Kelly travel together |
| **What outside the thing notices** | The test hole (needs it); the caravan's inspect pane (the reading stays shown on that tile); the logbook |

- **The button**: **Survey halt** on any Datum in a convoy (no kit needed: it is the deployed truck's own deep console). Greyed with its
  reason when the convoy moves, is at someone's settlement, has nobody able in Intellectual work, the truck is out of fuel, or the tile is
  read already (the reading is shown).
- **The work**: 6 hours [A] at the console at the plain pace, by day only, at the hand's research speed times the truck's scan rate, as
  the deployed truck counts its work; the hand learns Intellectual at the truck's own rate. The console idles the engine at its set-up
  rate (8 fuel a day, the Def's).
- **The reading** [A]: the tile's deep ore, picked by the game's own deep commonality and sized within the game's own lump range and
  units a cell, from a value fixed for the tile (the same tile always reads the same); no bedrock, no ore. With Dubs Bad Hygiene, what a
  water bore would give, or that it would come up dry.

### The test hole (EM35)

| Question | Answer |
| --- | --- |
| **What it lets the player do** | Bring home a sample of a find without a drilling camp |
| **What it stops them doing** | A free yield: a day's drilling, fuel, bit wear, and with the setting on the game's insects |
| **How it changes the way they play** | A find is worth a detour; the Kelly earns its place in a convoy |
| **What outside the thing notices** | The convoy's cargo (the tipper's hold first); the game's insects; the logbook |

- **The button**: **Drill a test hole** on the Kelly, beside the water bore's. It needs this tile read with an ore; refused, with why,
  for no reading, no ore, a tile already proved, an ore the rig only pipes (chemfuel), a wrecked bit, no Mining hand, or no fuel.
- **The work**: 12 hours of drilling [A] at the plain pace, by day, at the Mining hand's pace, with the bore's fuel (0.6 an hour) and bit
  wear (0.25 health an hour) [A].
- **The yield**: 4 portions of the read ore (the game's own portion size) with the insects' setting on, **3 with it off** (the owner's
  card: "a little less"), never more than the tile holds [A]. Into the tipper's hold if the convoy has one, otherwise the convoy's cargo.
- **The insects** (setting off by default): the game's deep drill rule, rolled as the rig rolls it on a map: the storyteller's own mean
  time between infestations a drill, its difficulty factor, and the rig's own threat points (30 to 60 % of the threat points, 200 to
  1,000). A convoy has no map, so the insects open an encounter map round it, as an ambush does; the hole's yield is lost.

### Tip the load into a depot (EM37)

- **The button**: **Tip the load here** on the Talus in a convoy: everything in its hold goes into the depot on the tile, made if there
  is none. Refused, with why, for an empty hold or anything the depot itself refuses (a moving convoy, another place on the tile, ground
  that takes no depot).
- **Why it is worth a button**: the depot window could already leave items one by one; this is one press, and a lighter truck climbs
  faster, as the drivetrain moves the real mass. The depot window takes it back out.

### Ambushes: fight, drive through or turn back (EM44)

| Question | Answer |
| --- | --- |
| **What it lets the player do** | Run a gauntlet with armour and speed, or fall back from a fight they cannot win |
| **What it stops them doing** | A certain escape: a way out is a roll, and a failed one is the ambush |
| **How it changes the way they play** | Armour kits and speed matter before a fight, not only in it |
| **What outside the thing notices** | The ambushers (no fight, or theirs); the convoy (held after turning back); the logbook |

- **When**: the game's ambush (any faction's, or a manhunting pack) on a convoy of the player's vehicles **that is moving** [A: a moving
  convoy sees it coming; one at a halt is caught standing]. The game stops for a window with three choices; like the game's own
  choice windows, it closes only by a choice.
- **Fight**: the game's own ambush, unchanged.
- **The named roll, "drive through or turn back"** (the owner's card: one named roll, shaped by state): its odds are shown on each
  choice. **Drive through**: 35 % [A], plus up to 30 % for armour and up to 25 % for speed. **Turn back**: 55 % [A], plus up to 15 % for
  armour and up to 20 % for speed. Never under 5 % or over 90 %. **Speed**: the convoy's own pace on the world map, from walking pace
  (nothing) to three times it (all) [A]. **Armour**: the vehicles' mean outer armour against sharp hits, Vehicle Framework's own ratings
  with upgrades, full at 1.0 without Combat Extended and 10 mm with it [A]. **Any vehicle that cannot move rules both out.**
- **Getting through**: the convoy goes on its way. **Turning back**: it stops and waits **4 hours** [A] for the road to clear (its
  vehicles refuse to move, and the inspect pane counts it down). **A failed roll**: the game's ambush.
- **Cost to run**: nothing until an ambush; the convoy's move check looks at the wait list only while a convoy waits.

### A trailer on wrecked gear (EM42)

- **Coupling** a trailer whose running gear has a destroyed part (a part that feeds its speed) is allowed, with a plain warning first
  (the owner's card: "warn, then allow", as EM29's unwise orders).
- **Once one is behind a vehicle**, on a map or on the road, the driver is told once ("being dragged at a crawl"), and the logbook
  says so. With **Trailers on wrecked gear crawl** on, the front vehicle keeps **a quarter of its speed** [A], and every damaged
  running part of the wreck **loses 2 % of its full health an hour** it is dragged [A], so a worn tyre beside a wrecked axle goes
  too. The way out is the player's: repair it, carry on slowly, or leave it at a depot (the depot window already takes towed trailers).
- **Read on the front vehicle's rare tick** for its whole line, so a wreck two trailers back counts once.

### Other factions

The halt jobs, the reading and tipping are the player's convoys' own. **An ambush on another faction's convoy** is not asked about: the
choice is for the player's vehicles. **A captured trailer on wrecked gear** crawls like any of the player's. Raiders bring no trailers
(EM7), so nothing else changes for them.

## Who chose what

- **Owner (26 Nov 2026):** the batch; the six answers above; the reverse-osmosis kit's idea, its vehicles and its timing; documenting
  every option; the third round's five rows; the test hole's insects off; one named roll for ambushes; warn, then allow, for wrecked
  trailers; the five icons queued for night 16.
- **Agent:** every figure marked [A]; the find's 2 to 6 tiles; the 250 mm dry line; the kit's design so far; in the third round, the
  shapes of the survey halt's reading, the test hole's yield, the ambush odds and the wrecked trailer's crawl, the ambush choice for
  moving convoys only, and the three settings' places and defaults (the ambush choice and the crawl on).
