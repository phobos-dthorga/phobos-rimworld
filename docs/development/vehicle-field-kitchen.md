# The field kitchen trailer (13 Nov 2026)

**A research and design record. Nothing is built, and nothing here is tested in game.** The owner, 13 November 2026: "A
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

Every node moves a figure that changes an outcome; each visible node gets its own overlay, planned before the first picture.

| Node | Does | Cost [A] |
| --- | --- | --- |
| Second range | A second range in the tent: two cooks at once | Steel 60, components 2 |
| Big kettle | x20 batches | Steel 40 |
| Insulated tent | The tent holds heat: a heater keeps it at 15 °C or more in the cold, burning chemfuel [A: 2 a day] | Cloth 80, chemfuel heater kit |
| Bigger water tank | 200 L to 600 L (Dubs Bad Hygiene) | Steel 50 |
| Dining annex | A second canvas room with a table and benches, so eaters get "ate at a table" | Cloth 100, wood 60 |
| Wash-up | A sink in the tent (Dubs Bad Hygiene): cleaning the tent takes less work [A] | Steel 30, components 1 |

**Parts bill:** wheels 4, suspension 2, frame 1, wiring 1 (as the Pintle), on the 3 x 7 tandem chassis [A]. A burner part role
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

## Sources

- Wikipedia, [Field kitchen](https://en.wikipedia.org/wiki/Field_kitchen), read in full on 13 Nov 2026: Fissler's 1892 kitchen,
  the *Gulaschkanone*, the hour to set up a tent kitchen.
- Kärcher Futuretech, [Double-walled cooking kettle module](https://www.karcher-futuretech.com/en/products/mobile-catering/modules/double-walled-cooking-kettle-module-19524010.html),
  read in full on 13 Nov 2026: 125 L, gas or diesel burners, 125 kg without burner. A maker's own page; no endorsement implied.
- **Unverified, from search summaries only:** the TFK 250's "up to 250" people (Kärcher Futuretech material, as summarised); the
  US Mobile Kitchen Trailer's 5,760 lb and its towing trucks (a Defense Logistics Agency sheet, which refused automated reading).
- RimWorld 1.7 (Ludeon Studios) and Vehicle Framework 1.6 build (Smash Phil): read from our decompiles, summarised in our own words.
