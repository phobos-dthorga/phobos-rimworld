# Working vehicles

**D'thorga Vehicles** adds six vehicles that do a job: find ore, drill it, haul it, move fluids, power a camp, and explore under
the sea, and five trailers any of them can tow: a flatbed, two bowsers, the Billy field kitchen and a shower trailer. It also gives other mods' vehicles the figures their real counterparts have. The submarine has
[its own chapter](submarines.md); looking after any vehicle is in [looking after vehicles](vehicle-care.md).
[Back to the guide](README.md).

## Read this first (gotchas)

- **Not yet tested in game**, except the survey truck and its boom and the Busbar's size and look (below).
- **Needs Vehicle Framework and D'thorga Framework**, loaded before it. RimWorld 1.7 only.
- **Each vehicle has its own research on the D'thorga tab**, after **vehicle parts** (the trailers after **towing and trailers**), and
  is built from the Architect's **motor pool** tab. Its upgrades need the research they name.
- **They are built from parts.** Make an engine assembly, transmission and kits at the **vehicle parts bench** first. Where
  RimThunder or Vanilla Vehicles Expanded is installed, a build asks for their parts instead.
- **To set up, someone must be aboard and the engine running.** Until then **Set up** (and the tipper's **Tip** and grab) is greyed out
  and says which is missing. Once it runs, the crew may get out: it works on by itself until it runs dry, its engine is switched off, or
  you pack it up. **If its engine stops, the work stops too**, and stays stopped even after refuelling until someone aboard starts the
  engine again.
- **A trailer has no crew: a colonist with construction work sets it up and packs it up** (the bowsers, not yet tested in game).
  **A set-up trailer cannot be hitched**: pack it up first.
- **The water bowser exists only with Dubs Bad Hygiene**, and not in its lite mode, which has no plumbing.
- **A vehicle that is set up cannot drive.** Its engine can still be started, but press **Pack up** before you move it.
- **Heavy trucks have air brakes**: after standing for hours, draft one and let it idle a few minutes before it will drive.
- **Heavy trucks have power steering** (not yet tested in game): a hit on its pump spills the fluid, and with it gone the truck crawls
  until the pump is repaired and topped up. **A laden or towing truck also wears its brakes and clutch faster** than an empty one.
- **A trailer goes on journeys with the vehicle towing it**, cargo and all (see the towing section of the vehicle care chapter).
- **Before removing the mod, scrap every one of these vehicles**: they go with it.

## The fleet

![The six working vehicles and the trailer to scale, seen from above: the Ullage bulk tanker, Datum survey truck, Kelly drill rig, Busbar generator set, Talus tipper truck, Fathom seabed crawler and Pintle flatbed trailer](images/vehicles-family.png)

*To scale: one game cell is the same size in every picture. The tanker is shown with a liquid tank fitted.*

Every name is a real word from engineering or earth science that says something true about the job. Each vehicle's description in
the game opens with its designation: the name, its first letter and a mark number. A refit adds a letter, as the Crawler's F1-S does.

| Vehicle | Designation | Size (cells) | Job | The name means |
| --- | --- | --- | --- | --- |
| **Datum survey truck** | D1 | 3 x 7 | Finds ore anywhere you can drive | The reference point every survey measures from |
| **Kelly drill rig** | K1 | 3 x 10 | Brings deep ore up fast | The square bar that turns the drill string on a rotary rig |
| **Talus tipper truck** | T1 | 3 x 8 | Hauls 2,400 kg and tips it in one go | The heap of broken rock at the foot of a cliff |
| **Ullage bulk tanker** | U1 | 3 x 10 | Carries and pumps fluids, buys them in bulk | The empty space a tank must keep above its load |
| **Busbar generator set** | B1 | 3 x 8 | A power station you can drive away | The bar that carries power into a grid |
| **Fathom seabed crawler** | F1, F1-S, F1-L | 4 x 9 | A small submarine ([its chapter](submarines.md)) | The sailor's six-foot depth unit; to get to the bottom of something |
| **Pintle flatbed trailer** | P1 | 3 x 7 | A second load behind any land vehicle | The hook-and-ring coupling a drawbar trailer hangs from |
| **Pintle water bowser** | P2 | 3 x 8 | 2,500 litres of water to a camp (Dubs Bad Hygiene) | A bowser is a tank trailer, named after Sylvanus Bowser's fuel pump |
| **Pintle fuel bowser** | P3 | 3 x 8 | 2,500 litres of chemfuel to vehicles and generators | As the water bowser |
| **Billy field kitchen** | Billy | 3 x 8 | A kitchen tent at camp; cooks in a resting caravan | The camp billy, the pot every Australian camp boils |
| **V-22 Osprey** | V-22 | 5 x 9 | A tiltrotor: lifts off like a helicopter, crosses the world like an aeroplane | Its real name (real machines keep theirs) |
| **Pintle shower trailer** | P4 | 3 x 8 | Showers and toilets at camp (Dubs Bad Hygiene) | As the bowsers |

How they work together:

```mermaid
flowchart LR
    Survey["Datum survey truck<br/>finds a deposit"] --> Drill["Kelly drill rig<br/>brings it up"]
    Drill --> Tipper["Talus tipper<br/>hauls the ore home"]
    Drill -- "deepchem, with Vanilla Chemfuel Expanded" --> Tanker["Ullage bulk tanker<br/>carries fluids"]
    Tanker -- "chemfuel" --> Drill
    Generator["Busbar generator set<br/>powers the camp"] -. "a grid within 6 cells runs<br/>the rig and survey truck electric" .-> Drill
    Generator -.-> Survey
```

## Buttons the working vehicles share

![Working vehicle buttons: set up, pack up, choose contents, compartment, load, unload, hold, bulk order, tip, overdrive, long range, power point and cable end](images/working-truck-buttons.png)

- **Set up** and **Pack up**: every truck that works standing still sets up first. Its inspect pane then says what it is waiting
  for.
- **Overdrive** (drill rig and survey truck): doubles the pace while the engine runs flat out. Fuel goes 2.5 times as fast and the
  working parts wear. It switches itself off at half health on any working part, or if the engine overheats.
- **Power point**: the drill rig and survey truck set out a small power point. With a powered conduit or generator within 6 cells,
  they run off the grid instead of their engine. The setting **Power points have the larger socket** (off) changes only the point's
  picture, for the next one a truck sets out.
- **Progress bars** show how far a drilling portion, a survey or a pump has got.
- **Every vehicle sounds like itself**: its own engine start and stop, its own door, and an engine that rises as it revs, drops at
  each gear change and growls harder under load. The heavy trucks hiss their air brakes as they stop and splutter when the tank runs
  dry. Their work has its own sounds too: the tanker's hoses and pump, the tipper's rams and tailgate, the drill rig's hammer, the
  survey truck's boom and a soft tone for a find, the generator set running. **D'thorga sounds volume** in D'thorga Framework's
  settings turns them all down or off.

![Upgrades for the working trucks: tanker tanks and hose reel; drill rig bits, mast, power pack, deepchem head, oil well kit and lights; tipper boards and bodies; survey truck arrays, probe, battery and groundwater kit; and the generator set's alternator, reel, battery, fan and heat exchanger](images/truck-upgrades.png)

*In order: the tanker's five (liquid tank to hose reel), the drill rig's seven (diamond core bit to work lights), the tipper's
three, the survey truck's six (geophone array to groundwater), and the generator set's five.*

Upgrades are fitted in Vehicle Framework's **Upgrades** tab and show on the vehicle's picture. **They cost what our fittings and tunes do** (from Framework 0.106.0, not yet tested in game): several hours' work and much more steel and components, more on a heavier vehicle, under the same two settings, *Time to fit and tune* and *Materials to fit and tune*.

## Datum survey truck

![The survey truck driving, and set up with outriggers down and its arrays fitted](images/survey-truck-fitted.png)

**Tested in game by the owner (9 November 2026): the truck and its boom work well. Its upgrades are not yet tested in game.**

1. **Research survey truck** (after Ground-penetrating scanner) and build it: it also takes 10 uranium.
2. **Park it outdoors with room beside it** and press **Set up**. Its boom swings out to whichever side is clear, about three cells
   from the middle of the truck. If both sides are blocked, it says so and will not set up.
3. **A colonist on Research work runs the survey**, at their research speed. The engine idles while they work, at about 6 to 8 chemfuel a day,
   or it runs off a grid within 6 cells of its power point at 700 W.
4. **Select the set-up truck to see what it has found.** Finds are the game's own deposits, with the game's odds and sizes.

| Fact | Figure |
| --- | --- |
| A first find | About 7 hours on average: ten times quicker than the game's scanner |
| Each later find on the same map | Takes longer than the last: one and a half times, up to eight times |

**Upgrades:** a **geophone array** (more steel) or a **radiometric probe** (40 uranium), not both; a **long-range array** that searches
nearby lands for one ore instead of this map, without the slowing that finds on this map bring (press **Long range**; a find becomes a
place on the world map to travel to); a **powered array** (three times as fast, 1,000 W, needs a grid or the battery); a **large
battery** (runs with the engine off; **the engine starts itself to recharge at 30%, burning fuel with nobody at the wheel**); work
lights. With Dubs Bad Hygiene, a **groundwater** upgrade shows the water while the truck is set up and marks the richest spot
within 15 cells. With Rimefeller, a set-up survey truck counts as the scanner a deep oil well needs.

## Kelly drill rig

![The drill rig on the road with its mast folded, and set up with the tall mast, power pack, diamond bit and work lights](images/drill-rig-fitted.png)

1. **Research drill rig** (after Deep drilling) and build it.
2. **Reverse it up to a deposit** a scanner or survey truck has found: **it drills the cell behind its rear**. Press **Set up**; a
   ring shows its reach.
3. **A colonist on Mining work drills**, with the game's portions and their Mining skill. What comes up lands beside the hole, or
   **into storage of yours within 3 cells of the hole** (a bulk bunker, a hopper, a shelf) when its settings take it and it has room.

| Fit | Drilling speed against a deep drill |
| --- | --- |
| Bare rig | 10 times: a steel lump that takes a deep drill a month takes the rig about three days |
| Diamond core bit | 20 times |
| RC (reverse-circulation) hammer | 18 times on ore, 30 times once only stone is left |

**Fit one bit or the other, not both**: pick by the job.

- **It burns close to 100 chemfuel a day while drilling**, and nearly twice that in overdrive, because drilling takes half its engine:
  its 160 tank lasts under two days of drilling. (With the setting "Fuel use follows engine load" off, it burns a flat 40 a day.)
  Fit the **electric power pack** and have a powered grid within 6 cells to drill without fuel.
- **Drilling can bring insects**, by the game's own rule and your storyteller's odds. **A faster rig meets them sooner**: the risk per
  lump is the same as a deep drill's. **Keep guards near it.**
- **At night, without work lights, it drills at 80%.**
- **With Rimefeller**, the **oil well kit** sinks a well where no ore is in reach, using steel from the rig's cargo and needing what a
  derrick needs (a working console, or a scanner or set-up survey truck for deep oil). **Build a derrick over the well head to pump.**
- **With Vanilla Chemfuel Expanded**, the **deepchem head** raises deepchem into a 300-unit buffer at ten times a pumpjack's rate,
  though the buffer and the tanker's pump set the real pace. **Without the head, deepchem stays in the ground**, and it never comes
  up as items. Empty the buffer with a tanker holding deepchem, set up and loading within reach of its hose, or a deepchem pipe.

## Talus tipper truck

![The tipper with its standard, rock and bulk bodies](images/tipper-bodies.png)

1. **Research tipper hauler** (after Electricity) and build it.
2. **Load it through its ordinary cargo window**: 2,400 kg, such as 96 granite chunks or 4,800 steel.
3. **With a driver aboard and stopped, press Tip.** Everything in its cargo lands in the outlined half-circle behind it,
   **including any spare parts you loaded**. Whatever will not fit stays aboard, and it says how many.

**Tipping into storage.** A storage building of yours inside the outlined half-circle (a bulk bunker, a hopper, a shelf) takes
whatever its storage settings allow, nearest cell first. **When it is full, the rest of those things stay aboard** instead of spilling
beside it; things it would not take still go on the ground, so a mixed load comes off in one tip. The message says how many stacks
went into storage.

**The bulk bunker** (research tipper hauler, Architect, **motor pool** tab; 120 steel) is built for this: an open 3 by 4 bay that
holds 8 things a cell, **96 chunks, one full load**. Build it with its open end (the striped wheel stop) where the truck will reverse
up to it, back the truck's tail to the stripe and press Tip.

- It starts set to take **chunks**; tick anything else in its storage settings.
- **Haulers fill it with chunks lying about the map**, as any stockpile that takes chunks. To keep it free for the truck, lower its
  priority or untick chunks until you need it.
- Its goods lie on its cells like a shelf's, so **belts take from it as from a factory hopper** (with VFE Factory), a factory machine's
  input cell on it reads that cell, a paste dispenser beside it draws from the cell it touches, and **The Dead Man's Switch's automatic
  benches link to it** like their own feed hopper.

**The loading chute** (research tipper hauler, Architect, **motor pool** tab; 30 steel) is a one-cell funnel that drops whatever
lands in it into the cargo of a vehicle parked across its **spout**, the cell it points at (outlined when you select it). Bring goods
by belt or by hand. **It starts set to take nothing: tick what it should load.** With no vehicle there, or a full one, it holds up to
four stacks and the belt behind it waits; its inspect line says what it is waiting for. Its picture is a stand-in.

**Loading from storage.** Every storage building has a **Vehicles may load from here** button, off to start. Switched on, the
loading grab may lift goods out of it, so belts or haulers can fill a bunker or a shelf while the truck is away and the grab takes the
lot when it comes. The grab never lifts from another faction's storage, or a thing a colonist is already on the way to carry.

**Upgrades:** **greedy boards** (taller sides, +600 kg), and either a **rock body** (tips half as fast again) or a **bulk body**
(3,600 kg, tips slower). And a **loading grab**, a small crane that shows folded behind the cab once fitted:

- **With a driver aboard and stopped, press Work the grab.** It lifts loose things lying within 4 cells (the blue outline) into the
  cargo, up to 500 kg a lift, every 20 seconds or so. **Each lift takes the nearest thing and the same kind lying within 2 cells of
  it**, so a heap of granite chunks comes up about 20 at a time. Nobody else has to carry.
- **It leaves things in a stockpile or on a shelf**, forbidden things and corpses, unless that storage has **Vehicles may load from
  here** switched on (above).
- It stops by itself when nothing is left in reach or the cargo is full, and says how much came aboard.
- **It cannot tip while the grab is out.** Press Stow the grab first.

**Gathering by itself.** With the loading grab fitted, the Talus gets two more buttons:

1. **Press Gather what** and choose what it fetches, exactly as for a stockpile. It starts with **chunks and raw resources**
   (steel, wood, silver and the like). The buttons at the top keep it inside one of your areas (or anywhere), and choose **where it
   unloads**: nowhere, or one of your storage buildings or stockpiles that takes something it gathers.
2. **Put someone aboard who can drive, and press Gather.** It starts its engine, drives to the nearest loose things you chose, stops
   about five cells from them, lifts everything you chose within reach, and moves on to the next pile.
3. **When its cargo is full:**
   - **With somewhere to unload,** it drives there, backs up with its tail against it and tips what it gathered, then goes back out.
     **A bulk bunker is always backed up to from its open end** (the striped wheel stop); a stockpile or a shelf from whichever side is
     nearest. When nothing is left to gather it tips the last of its load there and stops.
   - **With nowhere to unload,** it stops where it is: drive it home and tip it yourself.
4. **One message at the end** says how many loads and kilograms it gathered from how many places, how much it tipped on how many trips,
   and why it stopped. If it started the engine, it switches it off again.

- **It tips only what it gathers.** Anything else in its cargo, such as spare parts, stays aboard.
- **If its unloading place fills up, the run ends** and says so; what would not fit stays aboard. Leave room in the bunker, or set
  belts or haulers to empty it.

- **It only takes loose things.** It leaves stockpiles and shelves alone, and anything forbidden, anything a colonist is about to carry,
  and anything under the fog. **To keep something where it lies, forbid it.**
- **It works at the grab's pace**: a lift every 20 seconds or so, each taking up to 500 kg of one kind lying together. Chunks lying in
  heaps come up quickly; chunks scattered one here and one there take a lift each, and a drive between.
- **It stops if enemies come**, leaving its engine running so you can move it at once. Any other order you give it, a right-click
  somewhere, switching its engine off or pressing Stop gathering, ends the run too.
- Its inspect line says what it is doing: choosing, driving to something, or lifting, with what it has gathered so far.

## Pintle flatbed trailer

A two-axle flatbed as wide as the trucks that tow it: 2,800 kg empty, 5,000 kg of cargo, with low sides and a drawbar. Its one upgrade, the greedy boards (+1,000 kg), shows as
taller side boards.

1. **Build it** from the **motor pool** tab, after **towing and trailers** research.
2. **Load it** through its cargo tab while it stands unhitched.
3. **Hitch it**: select a land vehicle, press **Hitch a trailer** and pick the trailer, within 8 cells. A colonist who hauls walks over and
   couples it, and it is set straight behind the vehicle.
4. **Drive.** The trailer follows on the map, cuts inside corners and can be shot. **If it would run into a wall or another vehicle, the
   vehicle stops and says so**: take a wider way round, or unhitch.
5. **Unhitch** (on the vehicle or the trailer) leaves it where it stands, loaded, for later.

**Its weight is the towing vehicle's to pull.** A full trailer slows a small truck, runs its engine hotter and costs fuel; on mud or a
hill only the vehicle's own wheels grip. Only vehicles the drivetrain model works out can tow, unless you turn on **Vehicles without the
drivetrain model can tow** in D'thorga Framework's settings. A broken drawbar must be repaired before it can be hitched.

## Pintle water and fuel bowsers

Not yet tested in game. Two tank trailers, 3 x 8 cells, as long as the Busbar: a **water bowser** (only with Dubs Bad Hygiene) and a
**fuel bowser**. Each holds 2,500 litres, weighs 2,000 kg empty and pumps 400 litres an hour. They take the load to where it is needed
and stay at a camp while the truck that brought them leaves. Both look alike, a tank on the flatbed's chassis: **a blue stripe along
the tank means water, an orange one fuel** (repaint either as you like).

1. **Build** from the **motor pool** tab, after **towing and trailers** research (the water bowser also needs Dubs' **plumbing**).
2. **Park it** where the load is wanted, unhitched, and press **Set up**. A colonist with construction work comes, chocks the wheels,
   lowers the legs and runs the hoses out (1,500 work: about half an hour for an average builder). Until then the inspect pane says
   it waits for a colonist, and **Cancel set-up** calls it off.
3. **Load** or **Unload**, as on the tanker. Each tank holds only its own load, already chosen.
   - **Fuel bowser:** takes chemfuel from stacks beside it, from a chemfuel pipe (Vanilla Chemfuel Expanded, Rimefeller) or from a
     tanker; gives it to vehicles and fuelled buildings within reach of its hose (10 cells), or to a tanker.
   - **Water bowser:** takes water from plumbing with a water tower on it, or from a tanker of water; gives it to the water towers on the
     plumbing it touches. Parked, it lays hidden plumbing under itself, so a pipe touching any side joins it. It keeps the worst water
     it was filled with. **Water freezes in it below 0 °C.**
4. **Tank to tank:** one tank set up on **Unload**, another within reach of its hose on **Load**, both holding the same: the loading one
   pumps from the other at its own pump's rate. The tanker fills a bowser this way, and a bowser fills the tanker.
5. **Pack up** before you hitch it (1,000 work). A colonist comes for that too, and the pump holds at once. **A bowser cannot be
   hitched while it is set up, or while a colonist is on the way to set it up.**

**Its weight is the towing vehicle's to pull**: a full bowser weighs about 4.5 tonnes with water, 4 with fuel.

**Upgrades:** a **lagged tank** (the load takes four times as long to reach the outside temperature, so water freezes later and fuel
stays thin longer in the cold; it shows as a padded jacket) and a **second hose** (half as much again an hour; a second reel across the
front). The pump has no sound of its own yet; the hoses, relief valve, leaks and purges sound as the tanker's do.

## Billy field kitchen

Not yet tested in game. A kitchen on a 3 x 8 trailer, named for the camp billy. **It cooks with the game's own cooking**, so your
cooks, bills, ingredients and food poisoning work exactly as at home.

1. **Build** from the **motor pool** tab, after **field kitchen** research (after towing and trailers and electricity). The second range
   and the big kettle also need **field catering at scale**.
2. **Park it** with a **clear patch beside it: 6 cells out from its side, as long as the trailer** (no buildings, items, trees or
   people; either side will do). Press **Set up**. A colonist with construction work comes and pitches the tent (about an hour's
   work). If there is no room, the button is greyed out and says what is in the way.
3. Beside it stands a **canvas kitchen tent**, a real room with a canvas roof, so cooking there is **not** the slower, riskier cooking
   outdoors. Inside: a **field range**, a **field kettle**, a **shelf for meals**, a **cold box** and a **lamp**. A **power point** stands
   outside the flap.
4. **Fuel** the range and kettle with **chemfuel or wood**, and give them bills:
   - **The range** cooks everything your stoves cook, other mods' recipes too.
   - **The kettle** cooks **simple or fine meals ten at a time**, for less work than ten single meals.
5. **Power is a bonus, never a need.** Wire the power point to a grid or the **Busbar**'s cable end (within 6 cells, as any building
   wires) and the range and kettle work **a quarter faster**, the **cold box keeps raw food at 4 °C**, and the lamp lights.
6. **Pack up** before you tow it. Everything goes back aboard with its fuel, bills and contents; anything lying on the tent floor stays
   on the ground. **Packing up waits while someone is cooking.**

**On the road:** while a caravan rests for the night, its cooks turn **raw food from its packs into simple meals** at a Billy it carries,
on its own or hitched behind a vehicle: **up to 20 a cook**, burning half a unit of fuel a meal from the kitchen's burners, or chemfuel
and wood in the packs. Raw food goes nearly twice as far that way. Turn it off with **Field kitchens cook in a resting caravan** in
D'thorga Framework's settings.

**With Dubs Bad Hygiene, the kettle needs water:** 10 L a batch, from **Billy's own 200 L tank**. Fill it from a water bowser parked beside
it (bowser on **Unload**) or from plumbing; its inspect pane shows how much is left, and without water the kettle's bills wait and say why.

**Upgrades:** a **second range** (two cooks at once), a **big kettle** (meals twenty at a time), an **insulated tent** (a chemfuel heater
keeps the tent at about 15 °C, burning about 2 chemfuel a day **only while it is cold**), and a **dining annex** (a second tent in front
with a table and four chairs; needs 6 more cells clear in front). With Dubs Bad Hygiene: a **wash-up basin** (a cleaner tent, so less food
poisoning) and a **bigger water tank** (600 L). New pieces appear at the next set-up.

It looks like an olive kitchen box van with canvas shutters and a stove chimney. Its burners murmur while someone cooks, kept low on purpose.

## Pintle shower trailer

Not yet tested in game. **Only with Dubs Bad Hygiene** (not its lite mode). The field ablutions block: a 3 x 8 trailer with a 2,000 L
water tank, two showers and two flushing toilets.

1. **Build** from the **motor pool** tab, after **field ablutions** research (after towing and trailers, and Dubs' plumbing and septic
   tanks).
2. **Park it** with a clear patch beside it (5 cells out, 6 along) and press **Set up**. A colonist pitches a **canvas shower tent**
   with **two showers and two toilets** inside, a **water heater** outside the tent and a **power point**. Everything is plumbed into
   the trailer.
3. **Fill its tank**: set a water bowser beside it on **Unload** and the shower trailer on **Load**, or fill it from plumbing with a
   water tower. Every shower and flush comes off its tank.
4. **Fuel the water heater with chemfuel.** It burns only while the showers want hot water. Plugged in, it **burns half the chemfuel**.
   Without hot water, people wash cold and dislike it.
5. **The waste tank** fills as the toilets are used (about 70 flushes). Your colonists empty it into **sludge drums** when it is half
   full, as they do a septic tank, or a tanker parked against the trailer's rear can pump it out. Its inspect pane shows how full it is.
6. **Pack up** before towing: the water in its pipes goes back into the tank, and the waste stays aboard.

**Upgrades:** the water bowser's **lagged tank** and **second hose**, and a **third shower**.

## Ullage bulk tanker

![The bulk tanker as built with a bare deck, with a liquid tank fitted and its hose out, and with a pressure vessel](images/tanker-fitted.png)

1. **Research bulk tanker** (after Electricity) and build it. It comes with a bare deck.
2. **Fit a tank** in its Upgrades tab: a general, insulated or high-volume liquid tank, or a **pressure vessel** with Vanilla
   Gravship Expanded. One tank at a time. **The tanker's other upgrades branch from the tanks**: the compartments need a liquid
   tank, and the electric pump and second hose reel need any tank. **Compartments must come off before their tank can**; the pump
   and hose reel stay on the truck when you swap tanks. The Upgrade and Remove buttons say what to fit or take off first.
3. **Choose contents.**
4. **Park it on or beside** the pipes, stacks or vehicle you want, and press **Set up**.
5. Press **Load** or **Unload**. The inspect pane says what the pump is waiting for. **Hold** stops the pump.

**Stacks pumped out go into storage first**: a storage building of yours under or beside the tanker, such as a hopper at the start
of a belt, takes what its settings allow before the rest is set down beside it.

**Electric pump** (an upgrade): the pump runs off the engine unless this is fitted. With it, the tanker can set up and pump with the
engine off, on its battery, until the battery is flat; then the pump waits and the inspect pane says why. Someone still has to be
aboard to set it up.

| It carries | Pumped to and from | Watch out for |
| --- | --- | --- |
| Chemfuel | Other vehicles' fuel tanks, generators and other buildings that burn it within reach of the hose (filled first, to the level set on each), stacks, Vanilla Chemfuel Expanded's pipes, Rimefeller's pipeline | Thickens and pumps slowly in deep cold, only with the setting **Fuel thickens in deep cold** on |
| Deepchem (Vanilla Chemfuel Expanded) | The drill rig's deepchem buffer, Vanilla Chemfuel Expanded's pipes | As chemfuel |
| Crude oil (Rimefeller) | Stacks and barrels on the ground, Rimefeller's pipeline | None |
| Water | Dubs Bad Hygiene's plumbing | Freezes below 0 °C |
| Nutrient paste, hemogen | Their pipes | The whole load spoils after about 18 hours above 0 °C; the insulated tank doubles that |
| Astrofuel, oxygen | Vanilla Gravship Expanded's pipes | Oxygen vents in a fire or great heat |
| Beer, insect jelly, wort | Stacks on the ground | Wort spoils after 5 days above 0 °C |
| FSX, prometheum (Combat Extended) | Stacks on the ground | A hit may set off FSX; prometheum needs the pressure vessel and any spill burns |
| Toxic sludge (Biotech) | Wastepacks | Dissolves after 8 days above 0 °C and pollutes the ground |
| Fecal sludge (Dubs Bad Hygiene) | Septic tanks and barrels | It is what it is |
| Napalm (Rimefeller) | Stacks and barrels | |

**With Rimefeller:** park the tanker with its engine off against a Rimefeller pipeline, with its compartment set to crude oil or
chemfuel. It lays Rimefeller's hidden pipeline beneath itself and pumps to and from Rimefeller's storage tanks; the pipeline under it
goes when it drives off.

- **An empty tank must be purged before it carries something else**: select a colonist and right-click the truck (four in-game
  hours; it takes water with Dubs Bad Hygiene).
- **Taking a tank off with a load in it spills the load.** A damaged tank leaks; a destroyed tank of something flammable explodes,
  more violently the fuller it is (turn off **Flammable tank loads explode** and it only spills). A full tank slows the truck.
- **Compartments**: two upgrades split a liquid tank into two, then three, so it can carry different fluids at once. They share the
  capacity equally. Use **Compartment** to choose which one the pump and contents work on. **Food can't share a tank with anything
  else**, and the contents menu says so. Turn off **A tank needs upgrades for compartments** to give every liquid tank its
  compartments free.
- **Bulk buying**: take the tanker in a caravan to a settlement, pick the compartment and press **Bulk order**. Choose how much with
  the same number box and arrows as the trade window. You pay the game's price with the caravan's silver; what is on offer depends on
  what the settlement trades, only fluids that exist as items can be bought, and the same fluid can't be bought there again for 15
  days. Visiting traders don't sell by the tanker-load.

## Busbar generator set

![The generator set driving with its stock fan, and set up with every upgrade fitted](images/generator-set-fitted.png)

A medium diesel-electric truck, 3 by 8 cells like the Talus tipper: its engine turns a generator, electric motors drive the back axle, and parked it feeds your grid.

1. **Research generator truck** (after vehicle electrics) and build it.
2. **Press Set up** (with someone aboard and the engine running) and choose how to join the grid: **Run the cable out**, and a
   **cable end** appears where you choose: wire things to it, or touch a conduit to it; or **Join through conduits beneath it**, and
   hidden power conduits are laid under the truck (shown in the power overlay), so any conduit touching it is on its power.
3. It cannot move until you press **Pack up**.

| Fact | Figure |
| --- | --- |
| Output at full health | 2,000 W (2,650 W with the uprated alternator) |
| Fuel | Follows what the grid draws: 2 chemfuel a day doing nothing, 15 a day flat out, or 15 a day whenever batteries on the grid have room |
| Economy | 60% of the game's chemfuel generator: you pay for being able to drive it away |
| Stops when | The tank runs dry, the engine or traction generator is destroyed, or damage takes it under 400 W. Its inspect pane says which |

**Upgrades:** an **uprated alternator** (2,650 W); a **cable reel** (put the cable end up to 12 cells away); a **battery buffer**
that keeps the grid alive for a time when the engine cannot; a **heavy-duty fan** that cools a parked engine as well as driving does,
for hard work in hot weather; and an **exhaust heat exchanger** that warms the room its cable end stands in, up to 21 °C, for 25 W
of output. **Put the cable end inside the room you want warm**; outdoors the heat is wasted.

**A vehicle of ours parked beside the cable end, or beside a conduit on its grid, counts as plugged in**, as with any grid. Vehicle
Framework's own electric vehicles need a battery on the grid to charge.

**With the battery buffer fitted, the set is a hybrid** (not yet tested in game):

- **Press Battery drive** to move it with the engine off: slowly (about a third of its speed), quietly, burning no fuel, about 300 cells
  on a full buffer.
- **Out of fuel, it creeps on the battery by itself**, so you can limp it to a tanker or a fuel dump. When the battery is flat it stops
  and says so.
- **The engine fills the battery again while it runs.** The inspect pane says how far the charge will carry it.

The **service lead** charges another vehicle's battery in the field:

- **With the set set up and running, press Run the service lead** and pick one of your vehicles within 6 cells.
- It charges that vehicle's battery while it stays in, and **starts it at once if the battery is too flat to start**. No other
  vehicle, kit or booster pack is needed.
- It takes 300 W before your grid does, so the camp gets less while the lead is in.
- It comes out by itself if the set packs up or either vehicle moves away. Select the set to see the lead drawn to its vehicle.

**Pack it up before removing the mod**, or a cable end is left behind that does nothing.

## V-22 Osprey

Our first aircraft, the Bell Boeing V-22: research **tiltrotor aircraft** (after turbine engines) and build it at the motor pool.

- **Crew:** two pilots, both needed; a crew chief at the ramp; 24 seats in the cabin. **It carries 5,600 kg.**
- **It takes off and lands straight up** and flies between places at 17.5, twice as fast as any helicopter.
- **Fuel:** a 1,500 chemfuel tank that burns 200 a day. It is thirsty: plan its trips.
- **Its weights and engines are the real machine's**, so it lifts less on a hot day or high up, and with a full cabin flies below the
  height where its passengers would need oxygen.
- **Paint it:** the grey takes your main colour, the orange bands your second.
- **Still to come, before it is tested:** its rotors and tilting nacelles drawn, its upgrades (cargo hooks, ramp gun, belly gun,
  armour kit, inlet barrier and more), and our own flight over the map. Not yet tested in game.

## Other mods' vehicles

D'thorga Vehicles also gives other mods' vehicles the systems and figures their real counterparts have, so each climbs, labours and
bogs like itself. Every other vehicle gets the general systems ([looking after vehicles](vehicle-care.md)).

| Vehicle (mod) | What changes |
| --- | --- |
| Big Rig (VVE - Tier 3), M1075 HEMTT (RimThunder - Roaring Tiger) | Air brakes and an air compressor: idle before driving after a long stand |
| Humvee (HUMVEE) | A 24-volt system from two batteries, with about 60 A of charging |
| M1A2 and M1A2 SEP (RimThunder) | A gas turbine on tracks with no radiator, which burns hard even at idle |
| Harrier (Harrier VTOL) | Its real engine's lift ratings, water injection (a mechanic refills it after a hard hover), its own flight phases and a hover check at its real empty mass. Too heavy to hover onto its pad in the day's heat, it lands on a runway ([aircraft](vehicle-care.md#aircraft-and-sling-loads)) |
| Hind (Hind Helicopter Gunship), Mongoose (Mongoose Attack Helicopter) | Two turboshafts: lose one and the other carries the load and runs hot. The Hind takes a cargo hook rated at 2,400 kg; it flies across the map by itself and cannot fly water drop runs. The Mongoose takes no hook |
| Cherokee (VVE - Tier 3) | Two turboshafts, its rotor drive, and its lift checked before launch. A cargo hook rated at 11,800 kg (not with Cherokee Sling active); with a hook it can fly water drops, and its upholstery takes the litter kit |
| Ka-27PS and Mi-38 (RimArrow: Search and Rescue), every variant | Two turboshafts, a rotor drive, hydraulic flight controls and their lift checked before launch. Each takes a cargo hook rated at 5,000 kg, so it can fly water drops, and its cabin fittings take the litter kit. The Ka-27's coaxial rotors need no tail rotor |
| Louie (VVE - Tier 3), Mosquito (VVE) | One turboshaft each, lift checked before launch. The Louie takes a cargo hook rated at 1,814 kg; the Mosquito takes none |
| Frog, Toad (VVE), Goliath (VVE - Tier 3) | Their three or four thrusters are their engines: each one lost takes lift away, and a pod that cannot lift its load will not launch |
| L-ATV (GTek's Vehicles) | 370 hp, the middle of the published figures, Allison six-speed ratios, its real weight, tyres, four-wheel drive and a 740 A alternator |
| MaxxPro (GTek's Vehicles), Humvee, Land Cruiser J70, T-54 and Wiesel (Frontline Workshop), M113, M48H and the "HEMTT" (Roaring Tiger), M10 Booker and M2A3 Bradley (Desert Sabre), the BMP-1 family, both M1A2s | Their real engines' power, weights and gearboxes where published. The "HEMTT" is really the PLS, so it gets the PLS's figures. The BMP-1 mod left the Workshop on 8 November 2026; its figures wait for anyone with a copy |
| Goat DE2 (Trains of the Rim) | Diesel-electric: no gearbox, traction motors that heat when pulling hard, rail wheels, air brakes with a compressor |
| Steam locomotives (Trains of the Rim) | A boiler: hours to raise steam, a water glass, a tender or tanks to refill at a water stop, a fusible plug that melts if the water runs low. The Frisco follows the real 4500 class (a 68,000-litre tender), the tank engine the LB&SCR E2 ([special engines](vehicle-care.md#special-engines)) |
| Lightning (VVE - Tier 3) | Can run on batteries: turn on **The Lightning runs on batteries** in D'thorga Framework's settings and restart. It then charges parked over a powered conduit from the grid's stored energy (about five batteries' worth for a full charge) instead of taking chemfuel |
| Vanilla Vehicles Expanded's vehicles | Real weights: bikes lighter, tanks far heavier, so cargo matters on a tank and hardly at all on a bike. Change any weight under Vehicle Framework's "Empty mass" |
| L-ATV, MaxxPro, M1A2 SEP, M10 Booker, Harrier, Mongoose, Goat shunter, and VVE - Tier 3's vehicles except the Springbok and the Louie | Climate control: the cab holds near 21 °C by itself |
| Dirtbike, Charley and Dinghy (VVE) | Open: they shelter nobody, and the crew feels the weather. Every other vehicle here has a closed cab with a heater and an air conditioner |

Where each figure comes from is written beside it in the mod's own files: Oshkosh's and Allison's published figures for the L-ATV, a
US Army maintenance manual for the HEMTT's air brakes, All-Aero's Pegasus ratings and the Pima Air and Space Museum's empty mass for
the Harrier, and others ([the real figures record](../development/vehicle-real-figures-design.md)).

## What has been tested in game

- **Tested by the owner:** the survey truck and its boom (9 November 2026); the Busbar's size and look (13 November 2026); a
  helicopter lifting a vehicle on its sling, through take-off to the world map (12 November 2026; see
  [aircraft and sling loads](vehicle-care.md#aircraft-and-sling-loads)).
- **Not yet tested in game:** the other five vehicles and the Busbar's power, the trailers and towing, the bowsers and setting up a trailer, every upgrade, the bulk buying, and how the pictures sit. Where each upgrade's
  picture sits on its vehicle is a first placement, to be adjusted once seen in game.

What changed in each version is in [D'thorga Vehicles' changelog](../../mods/PhobosVehicles/CHANGELOG.md).
