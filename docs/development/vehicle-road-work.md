# Work on the road: the running survey, the water bore and the reverse-osmosis kit (26 Nov 2026)

**Status (26 Nov 2026):** the running survey (EM30) and the water bore (EM34) are built in D'thorga Framework 0.317.0
([as built](#as-built-framework-03170)), not yet tested in game (rows 565 to 567 of [the test pass](in-game-test-pass.md)); the
reverse-osmosis kit comes after them and is not built.

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

### The reverse-osmosis kit (after this batch)

The owner's idea, recorded as designed so far [A]: an upgrade node on the Pintle water bowser and the Ullage; while fitted and powered
(the vehicle's battery and alternator, or a hook-up), water pumped into the tank comes in clean, at a rate a kit can treat; its
membrane is a wearing part with its own service; the picture is a cabinet on the tank, paid art under the art rules (pilot first, the
owner's pick). Open for that batch: how fast it treats, how much power, whether it treats water already in the tank, the membrane's
life and what replaces it.

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
- **Logbook**: "Found ... on the road" on the Datum, "Drilled a water bore ... of water" on the Kelly.

## Who chose what

- **Owner (26 Nov 2026):** the batch; the six answers above; the reverse-osmosis kit's idea, its vehicles and its timing; documenting
  every option.
- **Agent:** every figure marked [A]; the find's 2 to 6 tiles; the 250 mm dry line; the kit's design so far.
