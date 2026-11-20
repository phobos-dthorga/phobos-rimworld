# Looking after vehicles

With **Vehicle Framework** active, **D'thorga Framework** gives vehicles the insides real ones have: a battery that goes flat, an
engine that runs hot, oil, tyres and brakes that wear, and parts that fail when a service is overdue. This chapter is how to keep
them running. It applies to **every Vehicle Framework vehicle from any mod**, not only ours. [Back to the guide](README.md).

![A mechanic under a truck's raised bonnet at dusk, steam rising from the engine](../../mods/PhobosFramework/About/Preview.png)

## Read this first (gotchas)

- **Not yet tested in game.** Everything in this chapter is built and checked offline; none of it has been seen in a real colony
  except where the [last section](#what-has-been-tested-in-game) says so.
- **It changes every Vehicle Framework vehicle you have**, unless you narrow **Which vehicles** in the settings
  (**D'thorga Framework: vehicle systems**). That needs a restart, so **decide before you load a colony**. Switching vehicles off
  later is safe.
- **Build a vehicle parts bench early** (research **vehicle parts**, then the Architect's **motor pool** tab). Our parts, kits and
  booster packs are made only there.
- **Make a jump-start kit or a booster pack before you need one**, and carry one in every caravan. A flat battery is the commonest
  way to be stranded.
- **Loaded vehicles slow on soft ground.** A heavy truck in sand, mud or snow labours, and **crawls with "Wheels spinning"** where it
  has no grip (never below 15% of its speed). **Drivetrain shown, speeds unchanged** in the settings keeps every vehicle at its
  author's speeds.
- **An aircraft idling on the ground with its pilot aboard burns fuel**, and a hover burns more than flying about. Take the pilot
  out when it is not needed. **Aircraft fuel follows the engines** in the settings turns this off.
- **High country takes engine power**: an engine without a turbocharger, a gas turbine and a helicopter's lift all lose some
  where the land stands high. **Thin air at height** in the settings turns this off.
- **Aircraft fly each stretch between places at a height of their own**: over the highest ground on the way, and lower when their
  engines cannot climb at their weight or someone aboard has no oxygen. A stretch over mountains too high for them is refused before
  launch. **Cruising height between places** in the settings turns this off.
- **Removing D'thorga Framework or Vehicle Framework from a colony that used the systems: press the removal button first.** Load
  the colony, press **Remove the added parts from this colony's vehicles** in the settings, save, then remove the mod. Without it,
  saved vehicles may fail to load. Before that, also **take every vehicle out of every garage**, **recover every sling wreck and lost convoy** on the
  world map, **let a water drop run land and unload every litter**, and **take any extra fuel tank off**: whatever is left in them goes
  with the mod.

## The three tabs

Select a vehicle. Beside Vehicle Framework's own tabs you will find three of ours. They work on the world map too, on a caravan
or an aircraft in flight.

| Tab | Answers | Shows |
| --- | --- | --- |
| **Dashboard** | Does anything need me now? | Gauges, warning lamps, what the vehicle is waiting for, and its switches. Click a gauge to open Systems at that system |
| **Health** (called **Vehicle** on the world map) | How hurt is it? | The vehicle's picture with hurt parts tinted, its speed and strength and what each part does to them, every part with its health, conditions, fitting and grade, and a **Wear** block saying when tread, brakes, battery and service will next need a look ("Tread: worn in about 4 days"). **Like parts share a row** (landing wheels, panels) whose bar is split into a segment for each part: click the row to open it, or rest on a segment to see that part |
| **Systems** | What exactly is going on? | Every reading, one row each, opening to its graph when clicked; parts as small bars; cards for the **weapons** (ready or out and why, how fast, how they turn), the **running gear** (tracks, and why it cannot move), the **electrical loads** (running, cut to save the battery or without power) and a **crew** in danger; an **Only what needs attention** tick box. Hidden in Plain mode |

**Rest the mouse on any reading or part** to learn what it means, what moves it, and what losing it costs.

**The Logbook tab** (not yet tested in game) is the vehicle's life story, newest first, kept for as long as you have it (up to 200
entries): every journey it set out on and where it ended, with the distance, the fuel burnt and who drove; every repair and service
with the mechanic's name; every fault with what caused it; tows, winch recoveries, the day it came under fire, the day you took it
from another faction. It is on the world map too. Only your own vehicles keep one: a captured vehicle's begins the day you take it.

**Armour.** On our service trucks every part has an armour class, named by what it stops, and the part's line on the Health tab
says it in every display mode: "skin armour: stops nothing aimed at it: thin sheet metal". All five trucks are skin today: they are
civilian trucks, and rifle fire goes through them. Each cell of a truck has one outside part (the cab, the body, a wheel, the
chassis), so a shot at the cab strikes the cab, and what sits inside is hurt once the outside part over it is holed. With Combat
Extended, shots strike a truck across its whole width.

The **inspect pane** at the bottom left names only the readings worth a look ("cab freezing"), with their figures in Operator and
Engineer modes, and what the vehicle is waiting for. Every other reading is on the Dashboard and Systems tabs.

### Warning lamps

A lamp lights on the Dashboard when a system needs attention, as on a real dashboard; a soft chime sounds when a red one comes
on. Rest the mouse on a lamp for the reason.

![The dashboard's warning lamps: check engine, battery, starter, frozen battery, running hot, coolant, fuel, oil level, oil hot, oil pressure, gearbox, tyres, brakes and cab](images/dashboard-lamps.png)

## What wears out and what fixes it

The short version: **service when it is due, keep a kit in the cargo, and park somewhere warm in winter.**

| What you see | Why | What to do |
| --- | --- | --- |
| **Won't start**: a flat or very cold battery | Lights and heaters ran it down; the alternator is damaged; deep frost | **Jump-start it** (below), park with a powered conduit touching it, engine off, to charge from the grid, warm it up, repair the alternator |
| **Running hot**, then overheating, then boiling over | A holed radiator, low coolant, hard work in the heat, a snapped drive belt | Stop and let it cool; repair the radiator; a **coolant top-up**, due at 60% |
| Oil low, hot or losing pressure | Oil burns as the engine runs, faster when worn; a damaged oil pump | A **service** tops it up |
| Slower, then failing tyres | Tread wears, up to three times as fast off the roads | A service with a **tyre set** |
| Slower, then **won't drive** | Brakes wear with every stop, faster laden, towing or fast | A service with a **brake pad set**; **heavy-duty brakes** wear slower |
| Loses its top gears, slips under load | A damaged gearbox or clutch | Repair the gearbox and clutch |
| **Brakes worn**, then **no brakes**: it drives, but runs on past where it stops and strikes what is in its way | Brake pads wear as the vehicle moves, more when towing | New pads with a **brake pad set**. **Do not drive it near people or walls until then** |
| **Clutch slipping**, then **worn through**: it crawls, with a quarter of its drive | The clutch wears a little at each take-off, more with a heavy load | A new clutch with a **clutch kit** |
| A little slower after crossing water | **Wet brakes**: the driver takes it gently | Nothing: a short drive dries them |
| Labours on hills, in mud and towing, and thirstier, in the desert | **A clogged air filter**: dust, far worse in a sandstorm. The engine loses power, so on a road it keeps its speed | An engine **service** fits a clean one; a **heavy-duty air filter** clogs slower |
| A heavy truck **crawls** | **Power steering** failed: its pump is hit or its fluid low | Repair the pump; a **power steering top-up** (2 chemfuel) |
| **Low air**, then **won't drive** | Air brakes on heavy trucks need air: a leak, a damaged compressor, or standing for hours | At **low air**, keep the engine running; below it the brakes lock until it idles a few minutes |
| A weak fuel pump after running out of fuel | Running the tank dry with the engine on costs the pump 5 %. A pump at half or less starves the engine, which then labours on hills and under load | Keep some fuel in the tank; repair the pump |
| **Won't start** below about -8 °C | Cold fuel gels (only with the **cold fuel** setting on) | Warm the vehicle in a heated room |
| Parts failing now and then | More often once a service is due (120 running hours), far more once overdue (180) | A **service** |

**Services start by themselves when due.** The **Service when possible** button asks for one now. Haulers bring the supplies to the
vehicle's cargo, then a constructor does the work. A service takes kits made at the parts bench: a **tyre set**, a **brake pad
set**, a **clutch kit**, an **engine service kit**, a **drive belt**, and a **hot-section kit** for a turbine's overhaul by a tuning rig. A colony from before kits existed keeps paying in raw materials until you switch
off **This colony: services take raw materials instead of kits** in the settings.

**The coolant top-up** takes water from a plumbing pipe beside the parked vehicle with Dubs Bad Hygiene, or 3 chemfuel without it.

**With Vehicles at Work, colonists leave a wrecking vehicle alone.** While a vehicle is boiling over, has under a tenth of its coolant
or has no oil pressure, its colonists do not take it out for trips or hauls on their own, and that mod's "why it won't go" line
says why. A trip already under way may still finish. **Your own orders still go through**, and so do Deliver and Gather. Once a mechanic
has refilled it, or it has cooled, they use it again.
**Pipes and conduits beneath a parked vehicle.** Whenever a vehicle of ours stands with its engine off, it lays hidden pieces of the
networks it uses across its footprint: plumbing if it takes water, power conduit if it has a battery, and the pipe of any line it uses (a
tanker's contents, a chemfuel line for its fuel). **You see them in the plumbing and power overlays**, not on the floor. Run a pipe or
conduit to any side and it joins; a conduit run from a powered grid **runs the vehicle on the grid**: its lights, heaters and turret
drives (up to 60 A) and its battery's charge, paid for in watts from your grid. The grid does so only when it has power to spare and
has switched the vehicle on, and never while its engine runs, when the engine does the work; a conduit touching only a corner, or a
powered machine beside it, does not count. An autonomous turret with a flat battery and no grid stops
until it has power, unless someone aboard turns it by hand. They go the moment its engine starts or it drives
off, and come back when it stops with its engine off. A vehicle set up for work keeps the pipes its work needs. A piece you deconstruct
stays gone until the vehicle is parked again, and gives nothing back.

**Refuelling from a chemfuel pipe** (with Vanilla Chemfuel Expanded): park a fuelled vehicle against a chemfuel pipe and switch its engine off. The
chemfuel pipe beneath it joins the line and fills from the line's tanks, 30 chemfuel an hour; the inspect pane says how full it is, and a message says when it is
full. A vehicle set up for work packs up first. **The depot bay** fills its own tank from a chemfuel pipe touching it, so haulers need not fill it.

![Our parts and kits: engine assembly, transmission, turbine assembly, turbine blade set, reduction gearbox, flight computer, hydraulic actuator, electric motor, drivetrain kit, electrical kit, tyre set, brake pad set, engine service kit, hot-section kit, drive belt, jump-start kit, booster pack, sling set and fire bucket](images/vehicle-parts-and-kits.png)

### Jump-starting

**Select a colonist and right-click the flat vehicle.** You need one of:

- **another vehicle parked within 8 cells and a jump-start kit**; or
- **a booster pack** (research **booster packs**). It holds 4 starts, gains one back every 2 hours in the cargo of a running vehicle,
  and is recharged fully by a bill at the parts bench. No second vehicle and no kit needed.

In a caravan on the world map, a crew member can jump-start from another vehicle in the caravan with a kit aboard, or with a booster
pack. **Every vehicle carries a booster pack** in the settings (off) does away with packs altogether.

**Cold weather:** cold oil makes a start harder on the battery. A **block heater** fitting keeps a vehicle parked beside power warm,
and the **night heater** keeps a caravan's engine warm at camp (needs the **Night heater at camp** setting and the vehicle's night
heater switch on before it leaves). **Electric vehicles have their own night heater** (not yet tested in game): the same switch and
setting, keeping the cab and the drive battery warm through the night, paid for from the drive battery, about a fifth of its range on a
ten-hour night.

### When the charging fails

With the engine running, if the alternator or generator is wrecked, its drive belt breaks, or the battery runs low faster than the
charging can fill it, the vehicle **switches loads off to save the battery**: air conditioning first, then climate control, the cab heater,
the night heater, work lamps, and the lights last. **Instruments and avionics stay on.** A message says which went off, the Dashboard
shows **saving the battery**, and the inspect line names them and says **about how long the battery has left**. Everything comes back
by itself when the charging recovers; your switches stay as you set them. With the engine stopped nothing is switched off, so lights
left on still run the battery down.

## Damage and repairs

- **Parts can carry conditions** shown under the part on the Health tab: a holed radiator keeps leaking coolant, a shot battery
  drains itself, a boiled-over engine has a warped head and runs hotter (badly warped, it also loses power and labours on hills and
  under load). **Repair the part to full health to clear them.**
- **Every fault says what caused it** (not yet tested in game): under the fault on the Health tab, and in a diagnosis report, a grey
  line reads "Cause: a bullet hit, 2 hours ago, at Amber Anchorage", or names the reading that strained it ("coolant temperature
  (boiling)"). A fault from before this was added has no line.
- **A shot-through fuel tank leaks fuel**, even parked, where the vehicle's maker did not already make its tank leak. **A hit on the
  battery or alternator can short the wiring**: a message names what stopped working (the headlights, a heater...), and the Systems
  tab says it is dead. Repair the part, or patch it in the field.
- **Field repairs**: away from a base, a mechanic can patch leaks and shorts with a component from the cargo. A patch slows the damage
  so the vehicle can limp home; it is not a repair.
- **A destroyed part needs replacing.** At 0% it waits for supplies in the vehicle's cargo and a mechanic; then ordinary repair
  carries on. **An engine or transmission costs the whole part** (made at the parts bench), and so does a helicopter's rotor
  gearbox (a **reduction gearbox**), an aircraft's flight-control channel or avionics bay (a **flight computer**), a hydraulic ram or an
  aircraft's servos (a **hydraulic actuator**, or its steel and components while your colony still accepts raw materials for services),
  and a traction motor or a generator truck's generator (an **electric motor**); anything else half its materials. A
  vehicle whose engine is destroyed cannot move until it has a new one.
- **In a caravan**, select it on the world map and press **Repair** for a vehicle that needs it; it must have stopped and have a
  mechanic. **The parts can be anywhere in the convoy's cargo**, not only in the broken vehicle (not yet tested in game).
- **A fire on a vehicle cooks it from the inside**: the cab grows stifling, the coolant heats, and the radiator, the tyres and the battery burn. Put it out
  within an in-game hour or expect a hot engine and a radiator to repair; left two hours, the engine boils over.
- **A destroyed vehicle leaves its hull** (not yet tested in game; the setting **Destroyed vehicles leave a hull**, on by default): yours or
  anyone's, where it fell. Mark it to **Deconstruct**: it gives **salvaged vehicle parts**, worth a quarter of the vehicle, on top of the
  quarter of its materials Vehicle Framework drops where it is destroyed. A raiders' aircraft brought down leaves nothing.
- **Salvaged parts pay for a salvaged-grade part**: order the salvaged grade for a destroyed part in **Fittings and tunes**, and the
  salvaged parts in the cargo pay for it as new materials would. It takes less punishment and faults more easily, as that grade does.
- **Damage shows on our vehicles' pictures** once a part is below 60%: scorches, holes and tears.
- **EMP** can stall a vehicle for a moment and drains its battery: a civilian truck loses about 40% of its charge, an armed vehicle,
  which is hardened, about 16%. Steam engines and RimThunder's vehicles are never stalled. Raiders' vehicles are hit the same way,
  so EMP grenades and mortars stop them too. **Jump-start a drained truck** before driving off.

![Vehicle buttons: service, patch leaks, jump-start, diagnose, repair in a caravan, fittings and tunes, headlights, cab heater, air conditioner, climate control, night heater, assign vehicle, go to bay, the sling's lift, set down and leave, the hitch's hitch and unhitch, water drop and unload casualties](images/vehicle-buttons.png)

### Knocked out, not destroyed

Not yet tested in game. A vehicle loses its jobs before it is a wreck, and every faction's vehicles do, yours and theirs alike.

- **A weapon stops when its mount is shot to pieces**: at a tenth of the mount's health or less. The mount is the vehicle's own turret
  part, or a **turret ring** (ground vehicles) or **weapon pylons** (aircraft) added at the gunner's seat. **Repair the mount** and the
  weapon works again. Its button says why it is out.
- **Damage slows it first**: as the mount is hurt the weapon turns, aims and reloads slower, to a little over half its speed. A gunner
  with poor sight aims slower, and one with hurt hands turns and reloads slower.
- **A destroyed track stops a tank.** Wheels still limp on. A stopped vehicle says why: "Cannot move: left track destroyed." Yours
  send a message when it happens.
- **The Systems tab shows all of this**: a **Weapons** card (each weapon, its mount's health, ready or out and why, how fast it works
  and how it turns), a **Running gear** card (tracks, and why the vehicle cannot move), an **Electrical loads** card (what is running,
  cut or without power) and a red **Crew** card when people are aboard a burning or dead vehicle.
- **On your vehicles, a powered turret needs power.** The **turret drive** draws from the battery while someone mans it. With the
  battery flat, or the load shed when the charging fails, the crew **cranks it round by hand**: a fifth of the speed, aiming twice as
  slowly, but it never stops. Machine guns on their own ring or pintle need no power, including a commander's gun on top of a
  turret; a coaxial gun goes with its turret. Aircraft have no turret drive. **Keep the engine running in a long fight.**
- **A tank's turret can be blown off** when its ammunition rack goes up: a hit on a badly damaged rack, or ammunition cooking off in a
  fire. Only tanks with an ammunition rack part (RimThunder's), and only while ammunition cook-off is on. The crew rarely survives.
- **Field repairs** also **re-fits a thrown track or a wrecked wheel** with a component from
  the cargo, back to a quarter of its health: enough to drive home slowly. It still needs replacing properly.
- **Crews leave a burning vehicle or a dead hull** (one that can neither move nor fire). Raiders and other factions climb out by
  themselves, leaving the vehicle for you to claim. **Yours never leave on their own**: the **Crew in danger** alert lists them.

## The workshop

![The vehicle parts bench, the dyno bay, the inspection pit and the depot bay](images/vehicle-workshop.png)

All four are in the Architect's **motor pool** tab.

| Building | Research | What it is for |
| --- | --- | --- |
| **Vehicle parts bench** | Vehicle parts | Makes our engines, transmissions, kits, sets and booster packs; recharges booster packs |
| **Inspection pit** | Vehicle parts | Speeds the parts bench |
| **Dyno bay** | Vehicle tuning | **Diagnose** a vehicle (a report with its tinted picture and, in Operator and Engineer, a graph of the last day). Needed for **tunes and changes of grade** on a working part, and for the two **heavy overhauls** (machining a badly warped cylinder head, a turbine's hot-section overhaul), within 5 cells of the vehicle and powered. Replacement parts fitted here go in at 60% health instead of 25%. Select it for **Vehicles in reach** |
| **Depot bay** | Vehicle depots | A home for one vehicle. Press **Assign vehicle** on the bay, then **Go to bay** on the vehicle (needs a driver aboard). On the pad it refuels by itself (chemfuel, while the bay has power) and mechanics need 70% of the usual work. It only moves when you tell it to, or, **with Vehicles at Work**, when a colonist parks it: a vehicle switched on there for trips or hauling is brought home to its bay instead of a parking lot |

Our vehicles and the depot bay are **built from parts** by role (an engine, a fuel tank, tyres and so on). Where an installed mod makes
a part that suits (RimThunder, Vanilla Vehicles Expanded), a build asks for theirs; otherwise ours. **Parts for building** in the
settings changes that, after a restart.

### Making parts

- **Bills run from the top.** The vehicle parts bench works its bills in order, top to bottom. A bill low in the list may wait a long
  time while the ones above keep the bench busy. Move the part you need most to the top.
- **Give the parts somewhere to go.** Kits and parts go in a storage category of their own, **Vehicle parts**. Stockpiles you made
  before installing don't accept new categories by themselves, so tick Vehicle parts in at least one stockpile or shelf. "Do until
  you have X" only counts parts in storage, so without one the bench keeps making more than you asked for.
- **Keep materials close.** A stockpile of steel and components beside the bench saves a long walk for every part.

## Fittings, tunes and grades

![Fittings, tunes and grades: uprated radiator, oil cooler, turbocharger, heavy-duty alternator, deep-cycle battery, heavy-duty springs, road tyres, block heater, cargo hook, the economy, stock and power tunes, the tall, stock and short final drives, and the salvaged and heavy-duty grades](images/fittings-and-tunes.png)

Open **Fittings and tunes** from the vehicle's Dashboard, from a right-click with a colonist selected, or from the button a vehicle
shows while a dyno bay is in reach. It works on any vehicle, from any mod, that has a loadout.

- **Fittings** go on one part each: uprated radiator, oil cooler, turbocharger, heavy-duty alternator, deep-cycle battery,
  heavy-duty springs, road tyres, block heater, a snorkel for light vehicles that cross rivers, a cargo hook for helicopters, and a fast-rope bar and rescue hoist for the Osprey. **A fitting is lost if its part is destroyed.**
  Wheels are grouped into axles; a choice on an axle is ordered for each of its wheels.
- **More fittings** (not yet tested in game): **heavy-duty brakes** (wear slower), a **transmission cooler** (a cooler gearbox when
  hauling heavy in the heat), a **gear-reduction starter** (a cold engine starts on a tired battery), **cab insulation** (on a vehicle
  with a cab part: the cab keeps its warmth and the night heater burns less), a **fuel heater** (fuel never gels), a **battery warmer**
  (parked by a powered grid, the battery stays above freezing) and a **heavy-duty air filter** (for deserts). **An engine, a battery and
  every other part still take one fitting each**, so choose: a snorkel or a heavy-duty air filter, a deep-cycle battery or a warmer.
- **Tunes** set up the whole engine (economy, stock, power) and the final drive (tall, stock, short). Piston engines only. **A tall
  final drive cannot make a vehicle faster than it was built to go.** With a **turbocharger** fitted, a third row, **Boost**, offers low,
  standard and high boost: high boost gives a tenth more power for more heat and fuel. Take the turbocharger off and the boost stops.
- **Grades**: **salvaged** parts are cheap, frail and fault-prone, and cannot hold a fitting; **heavy-duty** parts cost more, take
  more punishment and shrug off faults. Choose one when a part is destroyed, or to refit a working part.
- **It is real work.** Materials are brought, then a mechanic works: a turbocharger on a 5-tonne vehicle is about 12 hours and 100
  steel and 10 components; a power tune about 10 hours. Bigger vehicles cost more. **Demanding work needs a minimum Construction
  skill**; a mechanic below it is told why.
- **Our own vehicles' upgrades cost the same way**, under the same three settings (**Time to fit and tune**, **Materials to fit and
  tune**, **Bigger vehicles cost more to fit and tune**); taking one off refunds a share of what it cost. Other mods' upgrades keep
  their own costs.
- **Rest the mouse on any choice** to see what it would change ("engine power up, fuel burned up" in Plain mode). Its details stay in the strip at the foot of the window until you rest on another choice, so you can move down and scroll them.
- **Families** (not yet tested in game): above the parts, **Fit all** orders a whole family in one go, on every part it suits; each
  piece can still be fitted on its own below. The **cold-weather pack** puts a block heater in every engine, a deep-cycle battery, a
  gear-reduction starter, a fuel heater and cab insulation in, and switches the night heater on. Ordering it again orders only what is
  missing; a battery warmer goes on a battery only when you choose it there yourself. **All-terrain tyres** go on every wheel: far more grip in mud, sand and snow, a little slower,
  faster wear. New tyres renew the tread: each wheel fitted takes away its share of the wear. **Work lights** fit work lamps to the vehicle's **lamp bracket** (the drill rig's and survey truck's own work light upgrade gives them
  too): at night, while the vehicle stands still, they light the ground round it so work there goes at daylight pace, drawing on the
  battery. They light by themselves when your people are near (see [Headlights and work lamps](#headlights)), or by their button. **Long range** fits an
  **auxiliary fuel tank** to a land vehicle or boat (half as much fuel again) or a **ferry tank** to an aircraft (three quarters as
  much again); either costs cargo space, and the range on the world map grows with the fuel aboard. Taking one off drains the extra
  fuel into containers beside the vehicle. **Recovery** fits a **recovery winch** to the vehicle's winch mount: see
  [Recovering a stuck vehicle](#recovering-a-stuck-vehicle). On our own trucks (the tanker, survey truck, drill rig and tipper) the
  winch shows on the front bumper and the auxiliary tank between the wheels; other mods' vehicles keep their own pictures.

## Cabs and comfort

- **A closed cab shelters its passengers**: they feel the cab's temperature, not the weather, while the heater or air conditioner
  has the engine to run on. The heater runs off the engine's heat, so a cab warms only once the engine has.
- **Modern vehicles have climate control** and hold the cab near 21 °C by themselves. Older ones have a **cab heater** (below 5 °C
  outside) and an **air conditioner** (above 26 °C) you switch by hand. Each draws on the battery or alternator.
- **Passengers have moods about it**: glad of a warm cab in the cold, unhappy in one too cold or hot for them.
- **Open vehicles shelter nobody**: bikes and open boats feel the weather.
- **An idling engine warms the room it stands in**, so a truck in a garage heats it.

## Fuel, and what the engine drives

Not yet tested in game. With **Fuel use follows engine load** on (the default), a vehicle with the full drivetrain model burns fuel
for what its engine actually does.

- **At an ordinary cruise with nothing extra on, it burns the rate its author set.** Climbing, towing and soft ground burn more;
  an empty run on a road burns less.
- **Idling burns about half of what it used to**, a quarter of the cruise rate.
- **Whatever the engine drives costs fuel and a little power**:

| What | When it works | What it takes |
| --- | --- | --- |
| **Alternator** | Always, for the lights, heaters and whatever the batteries are taking | A little: about half a kilowatt for lights and a heater; more just after a start, while the battery refills |
| **Air-conditioner compressor** | While the cab is being cooled | 2.6 kW holding a cool cab, up to 5.6 kW cooling a hot one |
| **Radiator fan** | When the engine runs hot | A lot at high engine speed, almost nothing at idle |
| **Air brakes' compressor** (heavy trucks) | While the air is topped up after stops | About 1 % of the engine |
| **A tanker's pump** | While it pumps on the engine | 10 kW |

- **You feel it most standing still.** An idling heavy truck with its air conditioner on burns a third to two-thirds more than one
  without, and a small car more than twice as much. On a road the same loads are a few per cent.
- **On a climb, in mud or under a heavy tow, switching loads off gets a little speed back**: what they take comes off what is left
  for driving.
- **The Systems tab lists what the engine is driving** in Operator and Engineer display modes, with kilowatts in Engineer mode.
- **A worn or damaged drive belt slips under a heavy load.** A message says so: the battery charges less and the engine cools
  less. Switch something off, and have the belt replaced.
- **Working vehicles burn by the work**: a drill rig drilling takes half its engine, close to 100 chemfuel a day.
- **To go back to flat rates**, switch **Fuel use follows engine load** off in D'thorga Framework's settings, under vehicle systems.

### Thin air in high country

Not yet tested in game. **The higher a place stands, the thinner its air**, and an engine that breathes it has less to give. The
world map's terrain tab shows each tile's elevation; a map is as high as its tile.

| Height | Engine with no turbocharger | Turbocharged engine | Gas turbine | A helicopter needs, to hover |
| --- | --- | --- | --- | --- |
| The sea | all its power | all | all | its usual power |
| 500 m | 95 % | all | 94 % | 2 % more |
| 1,000 m | 89 % | all | 89 % | 5 % more |
| 1,500 m | 85 % | all | 83 % | 8 % more |
| 2,500 m | 75 % | 89 % | 74 % | 13 % more |
| 4,000 m | 63 % | 74 % | 61 % | 22 % more |

- **You only feel lost power where power is what holds the vehicle back**: a climb, mud, sand, a heavy tow. On a road it keeps
  its speed.
- **A turbocharger holds an engine's power to about 1,500 m**, and loses it more slowly above that. Trucks, modern tanks and locomotives have one as built; the HMMWV, the M113, the T-54 and the BMP-1 do not. Fitting a
  **turbocharger** to an engine without one gives it the same hold.
- **A helicopter is hit twice**: its engines give less and its rotor needs more. One that lifts a full load at the coast may refuse
  to lift it from a high plateau, and says so. Lighten it, or wait for cooler air.
- **Where it lands counts too** (not yet tested in game). When you pick a destination, a line warns if it would arrive too heavy to
  hover in the air there. With the default setting it then **comes in for a rolling landing**, which wears its undercarriage a
  little; it never stops to refuel in such air. "Allow it, at a cost to the engine" lands it straight down and strains its engines.
- **Radiators cool a little less** in thin air; the colder air up there mostly makes up for it.
- **While it matters, the inspect pane says** "The air is thin up here" and how much power the engine has left.
- Electric vehicles are not affected. Other factions' vehicles are not affected.
- **To switch it off**, untick **Thin air at height** in D'thorga Framework's settings, under vehicle systems.

## Camp accessories: fridge, house battery and power outlet

Research **vehicle electrics**, make the kit at the **vehicle parts bench**, put it in the vehicle's cargo and fit it from the
fittings window. Every land vehicle has a **fridge bay**, a **house battery tray** and an **outlet panel** for them. Taking one off puts
the kit back in the cargo.

| Accessory | What it does | What it costs to run |
| --- | --- | --- |
| **12-volt fridge** (20 kg) | Keeps up to 30 kg of the cargo's food at 4 °C, choosing what would spoil first. It has its own switch | 0.2 to 1.5 A, more in the heat |
| **House battery** (25 kg) | A second 100 Ah battery for the fridge and outlet, so they never flatten the battery that starts the engine. Below a tenth it cuts them off | Charges while driving once the starting battery is full, or on shore power |
| **Power outlet** (4 kg) | Switched on while parked, the conduit beneath the vehicle powers lamps, a radio or a bench wired to it, up to 300 W (600 W on a 24 V truck) | What it gives, in amps: a 200 W radio is about 20 A on 12 V |

- **Without a house battery, the fridge and outlet drain the starting battery with the engine off.** A fridge alone flattens a stock
  battery in under two days parked; the vehicle warns when the battery gets too weak to start.
- **To run a camp longer, leave the engine idling**: the outlet keeps its conduit down and the alternator carries it, burning fuel.
- **The outlet gives only what its devices ask**, and never fills a colony's batteries. Switched on, the vehicle draws nothing from
  that grid.
- Each accessory takes its weight off the cargo capacity.

## Headlights

- **Lights switch themselves, by default** (the setting **Lights switch themselves**, on): headlights light in the dark (night, dusk
  or an eclipse) while a vehicle is driven or its engine runs, and go out when it is parked with the engine off. **Work lamps** light at night while it stands still
  with your people near it, and go out an hour after the last one leaves; with the engine off and no power cable they light only while
  the battery is more than half full. **Switch a vehicle's headlights or work lamps button off to keep its lights dark.** The **Systems**
  tab says what a switched-on light is waiting for (dark, a stop, people near, a driver).
- **With the setting off, the headlights button is off until you press it.** Switched on, a vehicle lights the ground ahead of it at
  night, engine running or not. People working or fighting there are no longer slowed by the dark. The light follows the vehicle as it drives and turns.
- **With the engine off, the lights run the battery down**, and left on all night they can flatten it. A message warns when the
  battery may soon be too weak to start the engine: start it, or switch off the lights and heaters. **Once it is flat, everything
  electrical goes out**, the lights and the night heater included, and a message names what went out. It all comes back once the
  engine runs (jump-start it) or the vehicle is plugged in.
- **The vehicle's inspect panel says what the battery is doing**, in words: which lights or heaters are running it down, that it is
  charging, or that it is flat. In Operator and Engineer display modes it also says **about how much fuel the vehicle burns a day**,
  and in Engineer mode what for: the engine, its work, its heaters or a leak.
- **Driving in the dark with the headlights off is slower**: a land vehicle goes at 85 % of its speed over ground the game counts as
  dark, and its warning lamp says **no lights**. Lamps and lit roads count as light.
- **Other factions' vehicles light up only while they drive**, so a raid arriving at night shows itself.
- **Turn the light off** with the setting **Headlights light the ground** in the vehicle systems settings, if many vehicles at night
  slow the game.

## The driver, the weather and rivers

- **Who drives matters.** A driver who is hurt, drugged, half-blind, one-handed, freezing or tired drives slower: up to a tenth
  slower when merely tired or a little off, a fifth when impaired, three tenths at the very worst. Nothing is ever crashed for it.
  A worse driver also wears tyres and brakes a little faster. **Rest, medicine or a fresh driver put it right**; the vehicle's
  Systems tab shows how fit its driver is. Someone with one eye still drives near full pace. A driver who breaks down mentally cannot
  drive at all until it passes or someone else takes the wheel. **Raiders' drivers count too**: a raid with a hurt or tired driver
  comes in slower, as does a raider with no headlights at night.
- **Raiders arrive with the fuel their journey left them** (not yet tested in game): a raid from a faction far away comes in with
  its tanks low, one from next door nearly full. **Hole a raid truck's fuel tank** and it leaks fuel onto the ground until it runs
  dry, sooner the further it came; a stranded raid truck is soon blown up by its own crew, so keep clear of it.
- **Practice makes a gentler driver** (not yet tested in game). The hours a colonist spends at the wheel of one kind of vehicle make
  them easier on its brakes and tyres, and a practised pilot lands more gently. **Never faster.** The Systems tab shows the driver's
  practice; a colonist's Records show their hours at the wheel. **Keep the same people on the same vehicles** to get the most from it.
- **A vehicle has a regular driver**: whoever has driven it most. The fleet board's pane names them. They are upset when it is destroyed
  or lost out on the world map, and glad when a lost one is brought home.
- **Rain, snow and ice take grip.** It only tells where grip is what holds a vehicle back: hills, mud and towing. A laden truck or
  a heavy tow may crawl or stall in a blizzard or on ice that it would pull through in the dry. All-terrain tyres help. The setting
  **Rain, snow and ice take grip** turns it off.
- **Light vehicles and rivers.** A car, buggy or light utility vehicle (under 2 tonnes) with its engine running in a river can draw
  water into its engine and damage it; a message warns as it enters the water. **A short crossing is a small gamble; driving along
  a river is asking for it.** Fit a **snorkel** to cross safely. Heavier vehicles ford the shallow rivers unaided.
- **A cold snap warns you** (not yet tested in game): while one holds, or it is colder than -15 C, the **Vehicles won't start** alert
  names your parked vehicles that would not start now, and why, before a raid makes you find out.
- **Exhaust in closed rooms** (not yet tested in game; a setting, **off** by default): an engine or a generator running indoors fills the
  room with carbon monoxide, about 70 g an hour at idle and twice that working hard. In a closed single garage that is a dangerous level
  within about an hour and a half; doors, vents and gaps in the roof let it out. With **D'thorga Medical**, breathing it poisons people.
- **A short circuit reaches a plugged-in vehicle** (not yet tested in game): when the grid shorts out, a vehicle whose hook-up lead joins it
  has its wiring shorted, as a hit on its battery would, and its fault says the surge came from the grid.
- **A tanker can fill from a river** (not yet tested in game, needs Dubs Bad Hygiene): parked with a side against a river, lake or the
  sea, its pump draws water straight in, untreated, when there is no plumbing to draw from.
- **Ammunition in a burning vehicle can go off** (with Combat Extended's ammunition in use). Each blast uses part of what is aboard
  and grows with it. **Get the ammunition out or the fire out quickly, and keep people clear.** The setting **Ammunition aboard can
  cook off in a fire** turns it off.

## On the world map

- **A caravan moves as its slowest vehicle**, and its explanation names it. A vehicle that will not move stops the caravan; its
  inspect text says why.
- **A convoy is told when the snow comes** (not yet tested in game): its inspect pane says in how many days snow reaches the ground
  where it stands, or lifts, and the same for where it is going. Snow makes the going slower and thirstier, so leave before it or wait.
- **A stranded convoy raises an alert** (not yet tested in game): **Convoy stranded** names the vehicle, why it cannot go on, what
  could fix it there with what the convoy carries, and how far home is. Your choices are yours: fix it where it stands, camp and wait,
  send for parts or a tow, or leave it behind.
- **Every journey goes in the vehicle's logbook** (not yet tested in game), and one that went wrong (a breakdown, a halt, someone hurt
  aboard) also sends a letter when the vehicle reaches a map. **Letters for troubled journeys** in the settings turns the letters off.
  Drivers' records count their **kilometres driven** (a stretch of the world map is about 30 km), mechanics' their **vehicle repairs**,
  and winch drivers their **vehicles recovered**; long journeys, roadside repairs and winch recoveries can turn up in your colonists'
  art.
- **The ground matters**: deserts are sand, swamps mud, jungle undergrowth, and winter adds snow. A road is firm whatever the land.
- **Select a vehicle caravan to see its reach** on the fuel aboard, outlined in amber on the globe, and how many hours of driving
  the fuel allows. **Select an aircraft in flight** and its inspect pane says how many more tiles its fuel flies; **zoom out** to see an
  amber circle at that distance (it is far out: a helicopter's is a good way round the planet). An aircraft that can reach anywhere
  says so and has no circle.
- **Refuel from cargo** (not yet tested in game): carry the fuel your vehicles burn in the caravan's cargo, then press **Refuel *vehicle* from
  cargo** on the caravan. Whenever its tank falls to a quarter, the crew top it up from the cans with a short stop, so the caravan no longer
  halts for fuel. **It starts off for every vehicle**, so fuel you carry to sell is safe until you switch it on. The fuel line and the reach on
  the globe count the cans for every vehicle switched on. It works on the world map only.
- **Aircraft refuel by setting down on the way** (not yet tested in game): load the fuel it burns into the aircraft's cargo and press
  **Refuel *aircraft* from cargo** on its buttons before launching (or in flight). You can then pick a destination beyond the tank's reach
  when the fuel aboard covers it; the tooltip says how many stops it will make. Whenever the tank falls to a quarter it sets down on the land
  below, pours, waits a little while and flies on. **It never sets down on water, in mountains or on an enemy's ground**: over the sea it
  flies on until it reaches land, so a long sea crossing still needs the fuel in the tank. Nothing pours fuel into an aircraft in the air.
- **The fleet button** on the bottom bar lists every vehicle you own, wherever it is, grouped by place, with its picture, what it is
  doing, the one thing it most needs, its fuel and its worst part (the redrawn page is not yet tested in game). A trailer sits under what
  pulls it. At the bottom are vehicles lying lost on the world map, hulls waiting to be taken apart, and other factions' vehicles standing
  unguarded that you could claim. **Choose a row** (a click, or the up and down arrow keys) and the pane on the right shows where it is
  hurt, its faults and what caused them, and its newest logbook lines; **Enter**, or a click on the chosen row, goes to it. Tick **Only
  what needs attention** to hide the rest.
- **A vehicle parked in the wild is lost after a while**; a letter warns you three days ahead.
- **A lost convoy stays where it stopped** (not yet tested in game; the setting **Lost convoys stay where they stopped**, on by default):
  when the last colonist of a convoy dies on the road, its vehicles, cargo and anyone else aboard wait on the world map as a **lost
  convoy**. Right-click it with a caravan to **Recover** it, then drive, tow or lift the vehicles home.
- **Garages** (needs Vanilla Expanded Framework): a stopped convoy can set one up. Vehicles left there stay with no time limit, and
  whoever is stationed there mends them from steel and components you bring.
- **Services and coolant top-ups need a map.** Repairs and jump-starts do not.

## Aircraft and sling loads

- **Helicopters, jets and pods are checked before they lift off.** Too heavy for the day's heat, or with engines or thrusters out,
  **the launch button refuses and says why**: unload, repair, or wait for cooler air. A heavy Harrier on a hot day lands on a runway
  instead of its pad (with D'thorga Patches). The setting can allow it at a cost to the engine, or only warn.
- **Cut the engines** (not yet tested in game): a button on an aircraft in the air. **It falls where it is**: everyone aboard is hurt,
  a load on the hook drops, and over a building, rough ground or water it is wrecked. To land and switch off safely, switch the engines
  off the ordinary way instead: it lands on the nearest clear spot first. Coming down hard goes in the logbook.
- **Avionics.** Every aircraft has flight instruments and navigation, on its own instrument panel or an added **avionics bay**.
  **Broken or flat, the aircraft will not take off at night or in fog**; repair it, charge the battery or wait for daylight (the
  setting **Blind take-offs: warn only** lets it go anyway). Without navigation, or with only basic navigation in the dark, a flight
  across the world map is slower and burns more fuel; military aircraft carry full navigation and fly straight in anything.
- **In flight on the world map, an aircraft's systems keep running**: the cab is heated or cooled, the generator charges the battery,
  and wear and faults can arrive in the air. Where **climate control** is fitted it works the heater and air conditioner itself, so
  their own switches read "left to the climate control".
- **Heavy controls.** Helicopters and planes with servos, and the Harrier, fly on hydraulics: if the fluid leaks or the pump is hit,
  the controls go heavy and it flies slower. Top up the fluid in a service.
- **Gas turbines** (aircraft, and the M1A2 tanks) wear their hot section when run past their limits, and twice as fast when held at
  take-off power longer than allowed; a **hot-section overhaul**
  (10 plasteel, 3 components) puts it right. With one of two engines out, the other runs hot. **What a turbine drives besides the
  rotor or the tracks runs it a little hotter**: its generator, cabin air and, on the Harrier in a hover, air for the puffer jets.
  The Systems tab lists them.
- **An aircraft burns fuel by what its engines are doing** (not yet tested in game; the setting **Aircraft fuel follows the
  engines**, on by default):
  - **Flying between places**, one in good order with nothing switched on burns exactly what its author set. A worn hot section,
    cabin air and heavy electrical loads add a few per cent, and the range shown before a flight already counts them.
  - **Over a map**, flying about burns what its author set. A lift-off, a climb and a hover burn more: a Harrier hovering down
    onto its pad burns about twice what it does flying about.
  - **An upgrade that changes its fuel use counts everywhere** (not yet tested in game): the Harrier's plasteel armour burns about
    15 % more over a map as well as between places.
  - **Idling on the ground with a pilot aboard burns fuel**, where it used to burn none: about a quarter to two-fifths of the flying
    rate. **Take the pilot out, or undraft it, when it is not needed.**
  - **A helicopter on station over a map works as hard as its load and the air ask** (not yet tested in game): the Hind and the
    Mongoose burn less flying about light on a cool day, and more heavy on a hot, high one.
- **Each stretch between places is flown at a height of its own** (not yet tested in game; the setting **Cruising height between
  places**, on by default). The launch screen's tooltip says when a stretch is not flown at the aircraft's best height, and why:
  - **Climbing over high ground.** It always keeps 300 m above the ground on the way, 600 m over mountains. A helicopter forced up
    over a range burns a little more.
  - **Kept down because someone aboard has no oxygen**: a crew whose oxygen set is broken, or passengers in a cabin that is not
    pressurised, keep it below 3,810 m. A **cabin oxygen** fitting (Fittings and tunes) gives everyone aboard oxygen while the engine
    runs. **A jet flown low is slower and thirstier**: a Harrier with its oxygen out flies at about
    84 % of its speed and burns about 40 % more over the same ground. Repair the oxygen set (on its avionics part).
  - **Kept down by its engines**: heavy, or with an engine lost, it cannot climb as high.
  - **Refused**: the ground on the way stands higher than it can climb. The tooltip says about how many kilograms lighter it must
    be, or that it must fly round; pick a stop to one side of the mountains.
  - **An engine lost on the way** (to an air defence's hit): a twin flies on lower and slower if it still can. If the ground ahead is
    now too high but the way back is not, it **turns back and lands where it last stopped**, and a message says why. If neither, it
    struggles across, slow and thirsty.
  - The fuel the launch screen shows, and whether it says there is enough, count the height of every stretch.
- **Thin air in high country** takes power from the engines and asks more of the rotor: see
  [thin air in high country](#thin-air-in-high-country).
- **Carrying a vehicle on a sling** (lifting and landing tested in game by the owner, with the Cherokee):
  1. Fit the helicopter a **cargo hook** (Fittings and tunes). The **Cherokee**, the **Louie**, the **Hind**, and RimArrow's **Ka-27PS**
     and **Mi-38** have a mount for one (all but the Cherokee not yet tested in game). The Hind flies across the map: its load hangs under it there and comes down once
     it has landed.
  2. Put a **sling set** in its cargo.
  3. Press **Lift a vehicle** and pick one of yours parked outdoors nearby. A colonist hooks it up.
  4. It flies with the helicopter, drawn under it. **Where it lands, the game pauses and asks where to put the load** (not yet tested
     in game): click open ground within reach, or right-click to let the crew pick the nearest clear spot. If there was no room, press
     **Set the load down** once there is.

  5. **From a hover** (not yet tested in game): a helicopter flying over the map by itself (the Osprey; the Cherokee, the Louie and the
     Mosquito with their setting on; and the Hind) can do it without landing. Press **Lift a vehicle** in the air; once the load is hooked
     up, the helicopter hovers beside it and takes it. In the air with a load, **Set load down here** picks a spot, and the helicopter
     hovers beside it and lowers it; **Emergency release** drops it where it hangs, and it lands hard. It cannot land with a load
     hanging: set it down first. **It must be able to hover with the load on**, so a heavy load on a hot day is refused.

  The hook takes a set weight today, less on hot days: the load, its cargo and people. **A heavier load, or a lift in a gale or a
  thunderstorm, is allowed with a warning** (not yet tested in game): at lift-off the hook's mount loses health by the share the load is
  over, plus a quarter in bad weather, and a mount strained to nothing **gives way**, dropping the load back from a few metres. Repair
  the mount before it goes. **A slung flight is slower and burns more fuel**, and the range shown is shorter.

- **Emergency release** (world map, not yet tested in game): on a helicopter in flight, it lets the load fall. It lands hard, badly
  damaged, and anyone in it is hurt. It lies there as a **wreck**: right-click it with a caravan to **Recover** it, then repair it,
  drive it, tow it or lift it home. **A helicopter that crashes drops its load too**, beside the wreckage.
- **Each of these is a setting** under **Sling loads** in D'thorga Framework's settings: hot days, ground crew, weather, slower flight,
  and release and drops.
- **Fighting a fire from the air** (not yet tested in game): a helicopter with a cargo hook and a **fire bucket** in its cargo (made at
  the vehicle parts bench) gets **Water drop**. Click a fire within 50 cells: it flies to the nearest open water, fills the bucket, empties
  it over the fire and comes back to where it stood. **A parked tanker or water bowser of yours holding water also fills it** (not yet
  tested in game, needs Dubs Bad Hygiene for water in tanks): the water comes out of the tank when you order the run. **It needs water within reach, open sky over the fire, its crew, fuel and
  flying weather**; the button says what is missing. **The Hind cannot fly water drop runs.** A full bucket puts out the fires on about 3 cells across. Click the helicopter while
  it flies to see what it is doing.
- **An air ambulance** (not yet tested in game): fit the **litter kit** to the helicopter's cabin (Fittings and tunes; on the Cherokee and the Louie
  it goes on the upholstery, on RimArrow's Ka-27PS and Mi-38 on the cabin fittings, on the Hind on the fuselage). Right-click a downed person with a colonist selected and choose **Carry to a litter aboard...**. Up to 4 people
  lie on the litters. **Put a colonist who can doctor in the crew and medicine in the cargo**: while the helicopter stands, they tend the
  patients. In flight nobody gets worse. Press **Unload casualties** at the other end, then take them to bed. **Leaving by caravan sets
  the patients down first.** The kit takes a fifth of the cargo space.
  **Take casualties aboard** (not yet tested in game) makes the parked helicopter the place casualties go: with it switched on, doctors
  (and Search and Rescue's field responders) carry downed people to its litters whenever it is nearer than their bed, or they have no
  bed, until every litter is taken; then it says so. Prisoners and guests are carried too; anyone set to no medical care is not. Someone
  bleeding badly is treated where they lie first when a field medic is at hand. **A downed stranger who is no enemy** (a refugee from a
  crashed pod, a hurt traveller) is carried too (not yet tested in game); set down, a colonist must still be told to rescue them, as the
  game asks for any stranger. **Switch it off before you leave** if you do not want
  it working at the next landing site. It cannot be switched on while the helicopter moves or flies.

**Flight controls** (not yet tested in game): an aircraft whose control surfaces, actuators, hydraulics or wiring are wrecked below 35 %
**cannot take off**, and the launch button says why; damaged below 70 %, it flies slower. **An engine out** slows it to 0.8 of its
speed and a message says so. An aircraft's generator gives about half its output at ground idle and all of it from cruise.

**Air defence, chaff and flares** (with **The Dead Man's Switch - Expedition**; not yet tested in game):

- **Your aircraft can be shot at on the world map.** Flying to or past a defended settlement, its radars track the aircraft and may
  lock on and fire. A hit damages it, and people in its seats can be hurt. **It comes down at a crash site only if it can no longer
  fly**: controls shot away, every engine out or the airframe broken. Otherwise it flies on, damaged.
- **Before sending one near a defence, a prompt names the danger** and asks you to confirm.
- **Warning receivers** (the Harrier, the Hind and the Mongoose) tell you when a radar tracks the aircraft, locks on, or launches at it.
  Shot out or without power, they are silent.
- **Chaff and flares.** Make **countermeasure cartridges** at the vehicle parts bench (ten for 10 steel and 20 chemfuel, after the
  turbines research) and keep them in storage: **colonists doing hauling work load them into a parked aircraft's dispensers** by
  themselves, once ten or more are missing. Never into its cargo: cartridges in the cargo are not fired. The dispensers hold 60 in a
  Harrier and 128 in a Hind, and the inspect text says how many are loaded. Aircraft with dispensers fire two at every missile: with a working warning receiver a shot is much less likely to hit; without one, they fire late and help less.
- **The Harrier's Chaff and flares button**, on a map in the air (with D'thorga Patches), makes it harder to hit for ten seconds.
  Sound, it dodges as its author set; with shot-up controls or a dead warning receiver it is easier to hit.
- **Enemy Harriers and Hinds carry chaff and flares too**: each arrives with its dispensers a quarter to three-quarters full, and
  one you capture keeps what is left.
- **A closed sky.** Attacking a base whose alarm closes its airspace, your aircraft there cannot take off until the alert ends.
- **Oxygen.** The Harrier cruises high on its own oxygen. With the oxygen set out (damaged, without power, or with no engine to feed it)
  it flies its long legs low, slower and using more fuel. Tick **Crews feel thin air when oxygen fails at height** if you want a crew
  whose oxygen is shot out up high to be short of breath for a few hours.
- **Low flyers are seen later**: helicopters and the low cruise stay under the radars' horizon longer.
- To fly as before, untick **The Dead Man's Switch - Expedition's air defence sees aircraft**.

**Turrets without power aim worse** (not yet tested in game): a powered turret cranked by hand has no stabiliser and no powered
sights, so it hits less often on the move and at night, and worst of all moving at night. Keep the engine running in a fight.

## Towing a trailer

Not yet tested in game. Any land vehicle can tow a trailer such as the [Pintle flatbed](working-vehicles.md#pintle-flatbed-trailer).

- **Press Hitch a trailer** and pick one within 8 cells; a colonist couples it and it is set behind the vehicle. **Unhitch** lets it go.
- **The trailer stays on the map** and follows the hitch. **If it would hit a wall or another vehicle, the vehicle stops and says so.**
- **The engine pulls the trailer and its load too**: lower gears, more heat, more fuel, a crawl on soft ground or uphill. The trailer adds
  no grip.
- **Only vehicles the drivetrain model works out can tow**, unless you turn on **Vehicles without the drivetrain model can tow** (under
  **Towing** in D'thorga Framework's settings), which slows them by a plain rule instead.
- **Before you hitch, read the label beside the mouse**: it says what the tow would be like on the ground where the vehicle stands
  (full speed, slowed by the engine or by grip, a crawl, or too heavy), or why it cannot be hitched. It is a forecast: hills and mud on
  the way can make it worse.
- **A towing vehicle's panels say what holds it back**: the inspect panel, the Dashboard and the Systems tab name what it tows, its
  weight, and whether the engine holds it to a gear, the wheels cannot grip, or it can only crawl.
- **Load a hitched trailer as any vehicle**, through its cargo window; haulers walk to it wherever it stands.
- **A trailer goes on journeys with its vehicle**: form a caravan with the vehicle and the trailer comes along, with its cargo. Its
  weight slows the caravan. On arrival it is set down behind the vehicle, still hitched; at the map's edge, where there is no room behind,
  it is set down beside and swings into line as you drive off.
- **A towing or towed vehicle cannot be lifted on a sling**: unhitch it first.
- **A trailer or vehicle that is set up cannot be hitched**, nor one a colonist is on the way to set up: pack it up first. A towed vehicle
  cannot be set up either.
- **Two tanks within reach of a hose pump one into the other**: set one up on **Unload** and the other on **Load**, both holding the same
  fluid. A trailer that takes water lays hidden plumbing beneath itself while it stands unhitched.
- **A trailer's tyres wear as it is towed**, on the map and on journeys, faster off the roads, just like a vehicle's. Its **Health** tab
  shows them, and its **Systems** tab lists the tread and the tyre service. **A service fits new tyres from a tyre set**, as for any
  vehicle: haulers bring it to the trailer and a constructor does the work. Worn-through tyres can burst and damage the trailer's axles. A trailer has no Dashboard: it has no driver or engine.

## Recovering a stuck vehicle

Not yet tested in game. Fit the **recovery winch** (the **Recovery** family in the fittings window) to a land vehicle.

- **Press Winch a vehicle** and pick a stuck, broken or empty vehicle within 12 cells. **Another faction's vehicle can be winched too**
  where nobody is aboard and none of theirs stands armed nearby (not yet tested in game): pull a wreck clear before deciding whether
  to claim it. It stays theirs until you do. **Someone must be aboard to work the winch.** It
  drags the vehicle in a cell at a time, slower the heavier the pull, and stops beside you. **Stop winching** ends it at any time.
- **Whether the winch can manage** is worked out from both vehicles' weight and ground: a vehicle in deep mud is far harder to move than
  one on gravel, and one whose wheels are wrecked is dragged, which is harder still. The winch pulls up to 5,443 kg, but never more than
  your vehicle holds on its brakes, so **park the winching vehicle on firm ground**. A refusal says both figures.
- **Then tow it home**: with the winch fitted, **Hitch a trailer or vehicle** also takes an ordinary land vehicle. Up to 1.5 times your
  vehicle's weight is a fair tow; **a heavier one is allowed with a warning** (not yet tested in game), goes slowly, and wears your
  brakes and clutch fast. It follows on a tow bar like a trailer; anyone aboard rides along. **Undraft it first** so it rolls free. It has its
  own **Unhitch** button.
- **The tanker's electric pump** (an upgrade) pumps with the engine off, on the battery, until it is flat; without it the pump runs
  off the engine. **The Seabed Crawler's laboratories, sonar and listening array** also draw on the battery while they work.
- **The winch runs on the battery** (about 150 amps while it pulls). It will not start on a flat battery, and a long pull that runs the
  battery flat stops it. **Run the engine while winching.**
- **Another faction's vehicle** must be claimed first; then it can be winched out and towed home.

## Other factions' vehicles

- **Raiders' and visitors' vehicles run a simpler model**: they take hits, but nothing in them heats, wears or runs down by itself,
  which keeps a raid cheap. Capture one and it wakes to the full model on your settings.
- **Claim a vehicle**: select a colonist and right-click it. Nobody may be aboard, and none of its owners' armed people within 12
  cells. It takes from half an hour to six hours by size. **Taking one from a faction that is not hostile costs 30 goodwill**, and
  the order says so. Machines' vehicles cannot be claimed.
- They may come with fittings suited to their tech level, and keep them when captured.

## Special engines

| Vehicle | What is different |
| --- | --- |
| **Electric vehicles** | A traction battery that gives less in the cold and heat and wears with age; charges parked over a powered conduit, from the network's stored energy. Brakes wear a third as fast; the cab's heater and air conditioner cost range. **New battery modules**: 10 components, 20 steel. An overheated battery catches fire only with the **Battery fires** setting (off). The Lightning (D'thorga Vehicles) is electric only with **The Lightning runs on batteries** on |
| **Steam locomotives** (Trains of the Rim) | Hours to raise steam from cold; a water glass; refill at a **water stop** (with Dubs Bad Hygiene, from water towers). Run it dry and the fusible plug melts. **Decide two settings before you run one**: **Boiler explosions** (off; on, a boiler without a fusible plug, such as the American 4-8-4, explodes when its water runs below the firebox crown) and **Realistic feed water** (off: a water stop draws a tenth of the real tender, 6,800 litres for the 4-8-4, under one Dubs water tower's 8,000; on, the real 68,000) |
| **Diesel-electric locomotives** | Traction motors heat when pulling hard from a standstill. Ease the load and let them cool |
| **Boats** | Cool with sea water: no radiator |
| **Aircraft** | A battery, generator, service and gas turbine; their engines cool unlike a car's, so they have no coolant |
| **Helicopters** named by D'thorga Vehicles | A rotor drive that is **over-torqued** if asked for more than its gearbox takes |
| **The Harrier** (with D'thorga Vehicles) | Water injection for a heavy hover: refill 227 litres, from plumbing beside it with Dubs Bad Hygiene, otherwise free |

## How a vehicle gets its systems

- **By what it is, not its name**: what it runs on, and whether it drives, floats or flies. Light land vehicles (Vehicle Framework mass
  under 120) run on petrol, medium ones (120 to 350) on diesel, heavy ones (350 and more) on 24 V and diesel; any vehicle with tracks
  is tracked. Other mods (D'thorga Vehicles first) can give a named vehicle its own figures.
- **The engine counts as running** while the vehicle is drafted, moving in a caravan or flying, with fuel aboard. Idling counts as half
  the work.
- **Per vehicle, in Vehicle Framework's own settings** (each vehicle's page in its mod settings) you can change the battery capacity,
  alternator output, starter draw, engine heat, radiator cooling, thermostat, oil use, tyre wear, gearbox heat, the air brakes'
  compressor and leak, and the **empty mass** (the weight with nothing aboard: real vehicles named by D'thorga Vehicles carry their
  real weights, others start at 12 kg per Vehicle Framework mass). Each starts at the vehicle's own figure.
- **With Combat Extended**, the added parts take the engine's cells and armour, so shots can reach them. A hit on a part counts only the health
  the part really lost, so a round the armour stopped drains nothing.
- **With RimHUD**, a value named **Vehicle (D'thorga Framework)** can be added to its pawn display: which vehicle a person is aboard
  and in what role, with the fuel, the cab and what the vehicle waits for in its tooltip.
- **Figures and supplies are gameplay choices**, each labelled with its source or as authored. Chemfuel stands in for oil, coolant
  and the rubber in tyres, because the game has none of them.

## What has been tested in game

- **Tested by the owner:**
  - a sling lift with the Cherokee, from hooking up through take-off to the world map, where the load is listed (12 November
    2026). **The landing and setting the load down are not yet tested in game;**
  - headlights light the ground at night, and work lamps light the work round a parked vehicle (13 November 2026);
  - families and tunes ordered several at once queue up properly (13 November 2026);
  - **Diagnose**: the mechanic's look-over arrives and lists every part and reading (11 November 2026). The report's tinted picture
    and graph are not yet tested in game;
  - a damaged oil pump wears the engine down over time, and a damaged gearbox slows the vehicle (10 November 2026);
  - an upgrade waiting for its research refuses and names the project (10 November 2026);
  - the dev-mode self-test passes on a throwaway vehicle, and the dev-mode cost reading finds the vehicle model within its budget
    (9 November 2026).
- **Not yet tested in game:** everything else: most of the vehicle systems, the workshop and its buildings, the mechanic's jobs for
  fittings and tunes, claiming and the other world-map features.
- **Read from the code and checked offline:** every system above passes its rule tests, and a dev-mode **"Vehicle systems: run the
  self-test"** checks most of it on a throwaway vehicle without touching your colony.

Every setting, with its default, is listed in [D'thorga Framework's README](../../mods/PhobosFramework/README.md#settings); what
changed in each version is in [its changelog](../../mods/PhobosFramework/CHANGELOG.md).
