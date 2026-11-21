# The field kitchen trailer (13 Nov 2026)

**Status (17 Nov 2026):** built in D'thorga Framework 0.121.0 to 0.122.0 and Vehicles 0.59.0 to 0.60.0 ("What was built" below): the tent and its pieces, power, batches of ten and twenty, caravan cooking, its own picture and sounds, the kettle's water with its own tank, and six upgrades; and, in Framework 0.216.0 (22 Nov 2026), cooking while a caravan drives through the night, the cooks' reach into trailers and the cold box in tow; not built: enemy bases and the dry-goods locker. Not yet tested in game.

**A research and design record.** The owner, 13 November 2026: "A
military-style portable kitchen on wheels, powered by the Busbar as required." The owner chose the same day that it **always
cooks on fuel burners, and gains its electric extras when plugged into the Busbar or a power line**. It is trailer **Q1** in
[vehicle-trailers.md](vehicle-trailers.md) and stands on that record's shared base (B0): set up and pack up, a power lead,
set-out pieces with a canvas room, and stores. Marks as there: **[code]** read on 13 Nov 2026 and summarised in our own words;
**[A]** authored; **[S]** a real-world figure with its source below; **[I]** our inference.

## Read this first

- **What it is:** a kitchen on a trailer. Towed to a camp and set up, it pitches a **canvas kitchen tent** beside itself with a
  **field range**, a **kettle** for big batches, a **serving shelf** and, with power, a **cold box** and lights.
- **It cooks with the game's own cooking**, so every stove recipe the game and other mods have works on its range, and the game's
  cooks, bills, ingredients and food poisoning work unchanged. A vehicle cannot sensibly take cooking bills itself [code], so the
  cooking happens at real buildings it sets out.
- **What it adds that a stove cannot:** batches of ten; a proper kitchen room in the field, so the outdoor penalties do not apply;
  a cold box that cooks draw from directly; and, its biggest gift, **cooking in a resting caravan**, which the game cannot do at all.
- **Power is a bonus, never a need** (owner): chemfuel or wood always cooks; the Busbar or a grid makes it faster and adds the cold
  box and lights.
- **Name, an agent pick for the owner to change:** the **Billy field kitchen**, after the Australian camp billy and the owner's own.
  The family's names (Pintle, Busbar, Talus, Ullage) are single plain words. No Def on the owner's list uses "Billy" in its name
  or label (`scripts/query-defs.py sql`, 13 Nov 2026; only a "hillbilly" backstory matched).

## Real field kitchens, for scale

- **The first towed field kitchen:** Karl Rudolf Fissler of Idar-Oberstein invented a mobile field kitchen in 1892; German soldiers
  called it the *Gulaschkanone*, the "goulash cannon", because its stove chimney looked like a gun barrel when it was limbered for
  towing [S]. A tent-based field kitchen "may take up to an hour to fully set up" [S].
- **A modern cooking module:** Kärcher Futuretech's double-walled cooking kettle holds **125 litres**, is heated by **gas or diesel
  burners**, and weighs 125 kg without its burner [S]. Their TFK 250 field kitchen is described as cooking for **up to 250 people**
  [S, from a search summary only; the maker's page was not opened].
- **The US Army's Mobile Kitchen Trailer** weighs about **5,760 lb** in its travel mode and is towed by 2.5- or 5-ton trucks [S,
  from a search summary of a Defense Logistics Agency sheet; the sheet refused automated reading, so this is **unverified**].
- **What we take from them:** diesel burners (our chemfuel), a big kettle, an hour to set up, and a trailer a medium truck tows.
  Everything else is authored for the game.

## What the code says

| Question | Finding [code] |
| --- | --- |
| Can a vehicle take cooking bills? | Every pawn is a bill giver, but with rules built for surgery: its bills are surgery bills and it is usable only in bed (`full:all.cs:126758`, `128054`, `131501-131514`). A cooking vehicle would need several fragile patches |
| What does a cooking bench need? | To be a spawned building in the cooking work giver's list (`DoBillsCook` lists the electric stove, the fuelled stove and VFE's large table stove), with an interaction cell, fuel or power as it defines (`full:all.cs:311445-311741`) |
| Where can cooks take ingredients from? | Spawned things, or **holder buildings that offer their contents** to bills (`IHaulSource`, `full:all.cs:311817-311889`). Not vehicle cargo |
| How big is the game's biggest batch? | Four meals. Simple x4 is 1,200 work for 2.0 nutrition of raw food (the Defs) |
| What does outdoors cost? | Work tables outdoors work at x0.8 (`full:all.cs:715046-715084`); food poisoning's room roll is a flat 2% outdoors, whatever the cook (`91079-91135`) |
| What do stoves burn and draw? | The electric stove 350 W, short-circuiting in rain when unroofed; the fuelled stove 160 wood per day of use, only while cooking (the Defs; `full:all.cs:552069`) |
| Can a caravan cook? | **No.** Caravan food comes only from its inventory (`full:all.cs:792740-792768`), and Vehicle Framework adds nothing |
| How do people find meals? | They search spawned meals and dispensers (`full:all.cs:273866-273870`); a shelf of meals is the serving point the game already understands |

## What the player does

1. **Tow it** behind any truck with our hitch, the Busbar included.
2. **Set it up** where the camp is: a colonist works about an hour [A: 3,000 work] to pitch the kitchen tent and set out the
   range, kettle and shelf. Stock its **fuel** (chemfuel or wood) and, with Dubs Bad Hygiene, its **water**.
3. **Plug it in, if you like:** a Busbar or a conduit within 6 cells of its power point powers it.
4. **Give it bills** at the range or kettle, exactly as at home. Cooks take ingredients from the ground, a stockpile or the cold box.
5. **People eat** from the serving shelf, at the tent's table if there is one.
6. **On the road:** while the caravan rests, its cooks turn raw food in the caravan into meals at the kitchen.
7. **Pack it up** before towing away; the button refuses while a bill is mid-way or the shelf holds meals, and says so.

## The pieces it sets out

| Piece | What it is | Notes |
| --- | --- | --- |
| **Kitchen tent** | A canvas room, 5 x 4 cells [A], with a canvas roof | A real room: no x0.8 outdoor speed, and the game's own room cleanliness decides the poisoning roll, so a kept-clean tent cooks as safely as a kitchen at home |
| **Field range** | A cooking work table burning **chemfuel or wood** | Takes **every recipe of the game's stoves**, copied at start-up from the tables the cooking work giver lists (found by that list, never by name), so other mods' stove recipes work. With power, **x1.25 speed** [A] |
| **Kettle** | A second work table for big batches | Takes our **x10 recipes**: simple meals x10 and fine meals x10 (stews, in the world's words) at **2,400 and 3,600 work** [A], 80% of ten single meals' work, the saving a big pot gives. Same ingredients per meal as the game's |
| **Serving shelf** | A storage shelf beside the tent's door, set to meals | The game's own way of serving; prisoners' rations are hauled from here |
| **Cold box** | With power only: a locker building of **75 kg** [A] that keeps raw food at 4 °C | Cooks draw ingredients straight from it, as the game allows for holder buildings; without power it warms and says so |
| **Lights** | With power only: lamps in the tent | |

## The rules

### Fuel and power (owner: burners, plus power)

- **The range and the kettle burn chemfuel or wood** through the game's own refuelling, only while cooking, at the fuelled stove's
  rate [A: 160 wood a day of use, or 40 chemfuel, chemfuel being the denser fuel; authored]. Real field kitchens burn diesel or gas
  [S], so chemfuel is the natural fuel; wood is the fallback.
- **With power** (from the Busbar or a grid through its power lead): range and kettle cook **x1.25 faster** [A], the cold box runs,
  the tent is lit. Draw **600 W while cooking, 150 W idle** for the cold box and lights [A], well inside the Busbar's 2,000 W.
- **Without power** it cooks at the plain rate, the cold box warms, the tent is dark at night (the game's darkness penalty applies).

### Water (with Dubs Bad Hygiene only)

- The kettle's x10 batches take **10 L of water** each [A] from the kitchen's own **200 L tank** [A], filled from a water bowser,
  plumbing or our tanker. **It never fills itself.** Without Dubs Bad Hygiene there is no water to use, and none is asked for.

### Cooking in a resting caravan

- **The rule:** while a caravan is **resting for the night** and holds a field kitchen (on a tractor's hitch), each member who cooks
  turns **raw food from the caravan's inventory into simple meals** at the kitchen's speed: **up to 20 meals a cook a night** [A],
  burning the kitchen's chemfuel or wood [A: 0.5 chemfuel a meal]. Ingredients are taken as the game's simple meal takes them.
- **Food poisoning:** each batch rolls the cook's skill, as at home; the tent is not pitched on the road, so the room roll is the
  game's outdoor 2% [code].
- **Why it matters:** raw food is worth 0.5 nutrition for every 0.9-nutrition meal it becomes [code, the Defs], so cooking on the
  road nearly doubles the reach of carried raw food, and a meal beats raw food's mood penalty. **This is a large balance change**;
  it is a setting, its figures tuned in play.
- **How:** our own check on the caravan's rest, reading the kitchen in the hitch's holder. Nothing patches the game's caravan food;
  meals simply appear in the inventory and the game feeds people as it always does.

### Feeding prisoners

- The prisoner transport's ration locker ([its record](vehicle-prisoner-transport.md)) is stocked by haulers from the serving shelf,
  like any stockpile.

| Behaviour | Lets the player | Stops the player | Changes how they play |
| --- | --- | --- | --- |
| **A proper kitchen in the field** | Cook safely and at full speed far from home | Nothing | A camp or siege line eats like the colony |
| **Batches of ten** | Feed a crowd with less work | Nothing | Big operations need fewer cooks |
| **Power as a bonus** | Run faster with a Busbar, keep raw food cold | Running the cold box without power | The Busbar earns its place at camp |
| **Caravan cooking** | Turn carried raw food into meals on the road | Nothing new: it needs fuel, a cook and a stop | Long journeys pack raw food and chemfuel instead of meals |

## Upgrade tree (from its first Def, owner's rule)

**The tree as it stands**, drawn from its Defs by `python scripts/check-upgrade-trees.py --write` and kept current by the gate (never edit it by hand): a box holds alternatives, an arrow is what an upgrade needs, an "any one" joint is one of our any-of links, a crossed line joins two that cannot both be fitted, and a dashed node comes only with a tie-in.

<!-- upgrade-tree: Phobos_FieldKitchen_UpgradeTree -->
```mermaid
flowchart TB
  PhobosBillySecondRange["Second range"]
  PhobosBillyBigKettle["Big kettle"]
  PhobosBillyInsulatedTent["Insulated tent"]
  PhobosBillyDiningAnnex["Dining annex"]
  PhobosBillyWashUp["Wash-up basin (tie-in: DubsBadHygiene)"]
  PhobosBillyBiggerTank["Bigger water tank (tie-in: DubsBadHygiene)"]
  classDef tiein stroke-dasharray: 5 5
  class PhobosBillyWashUp,PhobosBillyBiggerTank tiein
```

Every node moves a figure that changes an outcome; each visible node gets its own overlay, planned before the first picture.

| Node | Does | Cost [A] |
| --- | --- | --- |
| Second range | A second range in the tent: two cooks at once | Steel 60, components 2 |
| Big kettle | x20 batches | Steel 40 |
| Insulated tent | The tent holds heat: a heater keeps it at 15 °C or more in the cold, burning chemfuel [A: 2 a day] | Cloth 80, chemfuel heater kit |
| Bigger water tank | 200 L to 600 L (Dubs Bad Hygiene) | Steel 50 |
| Dining annex | A second canvas room with a table and benches, so eaters get "ate at a table" | Cloth 100, wood 60 |
| Wash-up | A sink in the tent (Dubs Bad Hygiene): cleaning the tent takes less work [A] | Steel 30, components 1 |

**Parts bill:** wheels 4, suspension 2, frame 1, wiring 1 (as the Pintle). **Size:** at least 3 x 7 and
vehicle-sized or near (owner, 13 Nov 2026); 3 x 8, the Busbar's length, is the agent's reading
([the trailer base, B0.6](vehicle-trailers.md#b06-trailer-sizes)). A burner part role
would need a new part item; the record leaves it out unless the owner wants burners to wear.

## Other factions' vehicles

| Where | What happens | Lets the player | Stops the player |
| --- | --- | --- | --- |
| **A raid on us** | None: raids and sieges bring no kitchen (owner, 13 Nov 2026, declined siege kitchens) | — | — |
| **A base we attack** | A base may have a set-up kitchen with its tent ([vehicle-trailers.md](vehicle-trailers.md)) | Loot its food and fuel; capture it whole | Nothing |
| **A vehicle we capture** | It works for us at once | Use it | Nothing |

## Cost to run

- **On a map:** the tent, range, kettle, shelf and cold box are ordinary buildings; the power lead reads its draw on the rare tick.
- **Caravan cooking** runs once when a caravan settles for the night, and only for caravans with a kitchen.
- **Nothing runs per tick.**

## Optional tie-ins

| Mod | What changes |
| --- | --- |
| Vanilla Cooking Expanded | Its stove recipes (bakes, grills, desserts) work at the range by the copied list; its electric pot and deep fryer stay its own buildings |
| Tavern | Its oven's x4 dishes, where they are stove recipes |
| Vanilla Brewing Expanded | Hot drinks belong to the crib room's urn ([vehicle-trailers.md](vehicle-trailers.md), Q8), not here |
| Dubs Bad Hygiene | The kitchen's water tank, the wash-up node |
| Vanilla Nutrient Paste Expanded | Its portable vat stays its own; nothing overlaps |
| Campfire Stews | Dormant (last updated July 2025, no 1.7); not leaned on |

## Art and sound

- **The trailer, packed:** the tandem chassis with the kitchen body; a stove chimney, a nod to the *Gulaschkanone*, as its own
  overlay; measured and sized before generation (owner's rule).
- **Set-out pieces:** the canvas tent's walls and roof, the range, the kettle, the shelf, the cold box.
- **Sounds:** the burner's roar, a lid's clatter, the tent going up.

## Questions for the owner

**Answered by the owner, 13 Nov 2026:** (1) the name **Billy field kitchen**; (2) **batches of ten** at 80% of the work; (3)
**cooking in caravans**, as a setting on by default; (5) **no siege kitchens**: the owner did not choose them, so raids bring no
kitchen and the "A raid on us" row above is withdrawn. Question 4 (water with Dubs Bad Hygiene) was not put yet; its agent default
stands. Nothing is built yet.

**Answered by the owner, 17 Nov 2026 (the follow-up batch, built the same day in Framework 0.122.0 and Vehicles 0.60.0, not yet tested in game; check 156):** (4) **water with Dubs Bad Hygiene, 10 L a batch** for the kettle's
big batches, drawn from plumbing touching the kitchen; (6) build **the dining annex, the wash-up sink and the bigger water tank**; (7) the
**dry-goods locker later**, with the prisoner trailer or the crib room; (8) the tent heater **burns only while the tent is below 15 °C**;
(9) **no Field Tents tie-in** (agent's recommendation, owner's choice): our canvas rooms stay our own. Field Tents (lordfelix, active, updated
14 Nov 2026) makes its rooms through some 57 Harmony patches, and its open reports on 13 and 16 Nov 2026 include errors with packed tents
stored in a cargo holder and a freeze on load with large mod lists (`scripts/workshop-research.py report 3823513030`, read 17 Nov 2026).
Players with it can pitch one of its tents beside Billy themselves; a packed Field Tent carried as cargo may meet the reported error.

The questions as they were written:

1. **The name:** "Billy field kitchen" (agent pick), or another?
2. **Batches of ten** at 80% of the work (agent default), or the game's x4 only?
3. **Caravan cooking:** on by default as a setting (agent default), off by default, or not at all?
4. **Water with Dubs Bad Hygiene:** 10 L a big batch (agent default), or no water at all?
5. **Siege kitchens:** a siege may bring one (agent default), or never?

## Who chose what

- **Owner (13 Nov 2026):** a military-style kitchen on wheels, powered by the Busbar as required; burners always, power as a bonus.
- **Owner (13 Nov 2026, later):** the name Billy; batches of ten; caravan cooking as a setting on by default; canvas rooms (the
  kitchen tent); no siege kitchens.
- **Agent defaults, owner to revise:** the range copying every stove recipe; the cold box; every figure marked [A]; the upgrade tree.

## What was built (17 Nov 2026)

Built at the owner's word ("let's get this Field Kitchen plus Latrine and Shower built out", 17 Nov 2026) in D'thorga Framework 0.121.0 and
Vehicles 0.59.0, with the trailer base's power lead and set-out pieces ([vehicle-trailers.md](vehicle-trailers.md)). **Not yet tested in game**
(checks 152 to 154). How it plays is in [the player guide](../player-guide/working-vehicles.md).

**What differs from the design above, agent's choices for the owner to revise:**

- **Tent layout:** the canvas room is 6 cells out from the trailer's side and its full 8 cells long, inside 4 x 6 (the design said 5 x 4 inside): the
  range, a second range by upgrade and the kettle along the far wall, worked from the middle; the shelf and cold box along the trailer side; the lamp
  in a corner; the flap at the front, the power point outside it. It goes on whichever side is clear.
- **Fuel, one for one:** the game's refuelling counts units, so the burners take chemfuel or wood one for one at the fuelled stove's rate (160 a
  day of use, 50 held). The design's "40 chemfuel a day, chemfuel being denser" would need a fuel model of our own.
- **Power:** 450 W the range and 300 W the kettle while cooking, 100 W the cold box, 50 W the lamp (the design said 600 W cooking, 150 W idle).
- **The cold box holds four stacks of raw food** (the design said 75 kg), and is a holder of the game's own kind, so haulers fill it and cooks
  take from it.
- **Packing up keeps everything:** the range's fuel and bills, the cold box's contents and the shelf's storage settings are held inside the trailer
  while packed. The design said packing up refuses while the shelf holds meals; instead, meals on the shelf stay on the ground when it goes.
  Packing up refuses only while someone works at a piece.
- **Upgrades built:** second range, big kettle (x20), insulated tent (a chemfuel tent heater keeping it to 15 °C, about 2 chemfuel a day,
  burning whenever it is lit, as a campfire does). **Not built:** the dining annex and the wash-up node; and the bigger water tank, because
  **the kettle's water with Dubs Bad Hygiene is not built** (question 4 was never put; it needs the kettle to draw from plumbing per batch).
- **Caravan cooking:** spoilt meals use the cook's own food-poisoning chance plus the game's 2 percent for cooking outdoors; it reads the
  caravan's fuel from the kitchen's packed burners first, then chemfuel or wood in the packs. A Billy never set up has no burners yet, so it
  burns from the packs only.
- **Its own body picture** the same day: a kitchen box van on the bowsers' chassis, the owner's pick of three paid attempts (US$0.166;
  `BillyFieldKitchen.record.md`). The range, kettle and cold box are the owner's picks of that day's local candidates (range 11, kettle 13, cold box 11).
- **Sounds** (owner's picks, 17 Nov 2026): the burners while cooking, low under the game's frying; the kettle's lid as a batch starts; the
  tent and the trailer's legs. The cold box hums nothing (owner's choice).

**The follow-up batch (Framework 0.122.0, Vehicles 0.60.0, 17 Nov 2026), agent's choices for the owner to revise:**

- **Billy's own tank is a stand-in tower**, not a tank of the bowsers' kind: while set up its water lives in a hidden Dubs tower beneath it, so a
  water bowser fills it as it fills any tower; packed, the trailer keeps the litres. 200 L, 600 L with the bigger water tank.
- **The kettle draws 10 L as each batch starts**, from any towers on the plumbing beneath it; a batch broken off and taken up again after five
  seconds may draw a second time. Its bills wait without water, through the game's own bill check (no Harmony).
- **The wash-up is Dubs' plain basin**, 1 x 1, which raises a room's cleanliness: Dubs' kitchen sink does the same but is 3 x 1 and does not
  fit the tent.
- **The dining annex** is pitched against the kitchen's front, 6 cells long, so the kitchen's flap is the door between them; inside, a 2 x 2
  table and four chairs. Billy's power point moved outside the kitchen's far wall to make room.
- **The tent heater** marks its fuel used each tick below 15 °C; the game's heat pusher stops warming at the same 15 °C.

## Built: cooking while driving through the night (22 Nov 2026)

**The fault this answers.** Caravan cooking ran only while a caravan rested for the night; driving through the night (Framework 0.214.0)
means it never rests, so a caravan driving in shifts with a Billy in tow ate its food raw, and nothing said so. The agent found it on
22 Nov 2026 while checking how the berths and the trailers meet. **The owner chose** (22 Nov 2026): "Kettle simmers on the move by
default, but perhaps provide a gizmo for those who wish for meal halts?" Built in D'thorga Framework 0.216.0. Row 322 of
[the in-game test pass](in-game-test-pass.md). Not yet tested in game.

| Question | Answer |
| --- | --- |
| What it lets the player do | Eat cooked meals on a run that never stops; or stop an hour for a full kitchen's worth, caravan by caravan |
| What it stops them doing | Cooking a full night's meals without stopping; cooking on the move with nobody aboard but drivers and gunners |
| How it changes play | A long night run wants a passenger who cooks, as well as its pairs of drivers; a big crew may prefer the hour's halt |
| What outside the kitchen notices | The caravan (an hour's halt on the world map), the colonists (meals instead of raw food, cooking experience) and the message log (what was cooked, or why not) |

**Risk: low** (agent's grade, for the owner to confirm): nothing is lost or harmed; the worst case is the raw food the caravan ate before.
So it has no setting of its own: it follows **Field kitchens cook in a caravan** (the old "in a resting caravan", renamed for this).

| Piece | Where | Notes |
| --- | --- | --- |
| Which way a caravan cooks | `CaravanCookRule.WayFor` (game-free, tested) | Resting for the night: as before. Driving on at night: **simmer**, or **halt** where the player chose halts for that caravan. By day it does not cook |
| Simmering on the move | `PhobosCaravanCooking.Cook` | The kettle alone works: **half a cook's meals** [A: `onTheMoveShare` 0.5 on the kitchen's Def, so 10 a cook], the same fuel a meal, the same 2 % outdoor spoiling. **Riders only**: whoever drives or crews a vehicle tends no kettle (`Berths.IsCrew`) |
| A meal halt | `NightDriving.Halt`, and the night-rest postfix | The cooks work as at camp, then the caravan is held for **an hour** [A: `mealHaltTicks` 2,500] by answering "resting" for it, and drives on by the clock. No meal, no halt. The halt is not saved: a game loaded during one drives on, its meal already cooked |
| The choice | **Meal halts**, a switch on the caravan beside **Drive through the night**, shown while it carries a kitchen | Off by default (the owner's choice). Greyed out with its reason while that caravan stops for the night anyway. Its own icon is asked for in `assets/vehicle-systems/requests/berths-house.json`; until it is made, the button shows our switch icon |

**A gameplay simplification, labelled:** the kettle is on the trailer and its cook rides in the tractor. Read it as the stew put on before
setting out and seen to at the driver changes. Horse-drawn field kitchens were built to cook on the march; that is the agent's
recollection of the German "Gulaschkanone", **unverified here**, and no figure of ours rests on it.

**Other factions:** their caravans on the world map are not simulated person by person, so nothing here applies to them.
**Cost to run:** nothing new per tick; the hourly look that already ran now also asks the night-driving check's stored answer, and a
halt is one dictionary look-up in the night-rest answer while any caravan is halted.

**Who chose what:** the owner chose simmering by default and the halt as a per-caravan switch; the agent chose the half share, the
hour, riders only, and that a halt happens only when there was a meal to stop for.

## Built: the cooks reach the trailer's food (22 Nov 2026)

**The gap.** A towed trailer rides in its hitch and is no member of the caravan, so the game's packs never held its cargo: caravan
cooking could not see raw food or fuel loaded into the Billy itself, into its packed cold box, or into any other trailer in tow.
**The owner said build it** (22 Nov 2026). Built in D'thorga Framework 0.216.0; row 323 of [the in-game test pass](in-game-test-pass.md).
Not yet tested in game.

- **What it lets the player do:** load the kitchen with its own food and fuel and send it. **What it stops:** nothing. **How it changes
  play:** the Billy and its cold box become the caravan's larder, as they are at camp. **What outside notices:** the caravan's packs
  (meals arrive there) and the trailer's cargo (raw food and fuel leave it).
- **How:** `PhobosCaravanCooking.Larder` adds, to the caravan's packs, the cargo of every vehicle in tow (`Hitches.InTow`, along a whole
  line of tows) and what any packed cold box holds. The kitchen is now found anywhere along a line, not only first behind the tractor.
- **Still open, said plainly:** people in a caravan **eat** only from the caravan's packs. Food in a towed trailer that is not cooked
  (meals, pemmican) is not eaten on the road; load ready food into a driven vehicle. That is the game's own caravan feeding, which this
  does not patch.

## Built: the cold box in tow, and time for food in a trailer (22 Nov 2026)

**The owner chose** "cold box stays cold in tow" (22 Nov 2026). Built in D'thorga Framework 0.216.0; row 324 of
[the in-game test pass](in-game-test-pass.md). Not yet tested in game.

**What reading the code found first, and it corrects the agent's own earlier word.** The agent had told the owner the packed cold box
was "warm on the road". It was neither warm nor cold: **its food did not age at all.** The game ages a held thing only when its holder
ticks it, and two holders of ours never did: a trailer riding in its hitch off the map has no ticks (its tractor catches its tyres up,
nothing else), and the pieces a trailer packs away were never ticked, on a map or off it. So every towed trailer's cargo, and every
packed cold box, was a freezer that cost nothing. Keeping a box cold means nothing until time passes in it, so both are built together.

| Question | Answer |
| --- | --- |
| What it lets the player do | Carry fresh food for days in a kitchen's cold box, for a few amps of the towing vehicle's electrics |
| What it stops them doing | Keeping food fresh for ever, for nothing, by loading it into any towed trailer or a packed cold box |
| How it changes play | Food for a long run goes in the cold box, a fridge or the cold; what rides in a plain trailer is planned like any cargo. The tractor's battery and alternator carry one more load |
| What outside the trailer notices | The food (it ages, or does not), the towing vehicle's electrics (a load that can be shed), the Systems tab (a line and a switch) and the cooks (what is still worth cooking) |

**Risk: low** (agent's grade, for the owner to confirm): food in a trailer now spoils at the pace of any other cargo, slowly and in
plain view on each stack. **No setting, and the owner confirmed it** (22 Nov 2026: "I would rather food age if it were to do so in vanilla
anyway, as that's how the game is by default"): it is the game's own rule for cargo reaching a place it was skipped by accident, not a
new hazard; the cold box, a fridge and cold weather are the answers the game already gives.

| Piece | Where | Notes |
| --- | --- | --- |
| Time for held food | `HeldGoods` | One of the game's own rare ticks for each rarely-ticked thing (what rots or spoils): a held trailer's cargo and packed pieces at its tractor's rare tick (`AgeTowed`), a trailer's packed pieces at its own (`AgePacked`). The game's own temperature rule then applies: the cell on a map, the tile off it |
| The load | `towLoads` in the vehicle rules Def; `Loads` adds them while the vehicle tows | **trailer cold box**: the box's own working watts from its Def (100 W [A]) over the vehicle's volts, drawn only while a packed box holds food; a switch, on by default; shed with the fridge (rank 2). Other trailers can bring loads the same way, in XML |
| Cold | `ColdInTow.Chill`, told from the tractor's look; `Building_PhobosColdBox.Cold` | While the load runs, each packed box behind is told so and stays at 4 °C until two looks pass without word. Told from the tractor's side because no list of tows names a trailer riding in a hitch after a game is loaded |

**Limits, said plainly:** a packed box cannot be looked into (set the kitchen up to read its stacks). On a map, a packed kitchen hitched
to a parked vehicle also runs its box from that vehicle's battery; unhitched and packed, the box is warm. Its own icon is asked for in
`assets/vehicle-systems/requests/berths-house.json`; until then the load shows our switch icon.

**Other factions:** raiders bring no kitchens; a base's kitchen stands set out. A captured kitchen follows these rules. A raider's
vehicle on the light tier ages nothing in tow (the catch-up runs on the full tier only), which costs the player nothing.
**Cost to run:** per towing vehicle, one walk along its tow at its rare tick; per stack of food in a held trailer, the game's own rare
tick, as it would have in any cargo.

## Sources

- Wikipedia, [Field kitchen](https://en.wikipedia.org/wiki/Field_kitchen), read in full on 13 Nov 2026: Fissler's 1892 kitchen,
  the *Gulaschkanone*, the hour to set up a tent kitchen.
- Kärcher Futuretech, [Double-walled cooking kettle module](https://www.karcher-futuretech.com/en/products/mobile-catering/modules/double-walled-cooking-kettle-module-19524010.html),
  read in full on 13 Nov 2026: 125 L, gas or diesel burners, 125 kg without burner. A maker's own page; no endorsement implied.
- **Unverified, from search summaries only:** the TFK 250's "up to 250" people (Kärcher Futuretech material, as summarised); the
  US Mobile Kitchen Trailer's 5,760 lb and its towing trucks (a Defense Logistics Agency sheet, which refused automated reading).
- RimWorld 1.7 (Ludeon Studios) and Vehicle Framework 1.6 build (Smash Phil): read from our decompiles, summarised in our own words.
