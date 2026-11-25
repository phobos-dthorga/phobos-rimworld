# Aircraft and sling loads

Flying with D'thorga Framework: the checks before take-off, what keeps an aircraft in the air, how it burns fuel and chooses its
height, carrying a vehicle on a sling, dropping people, cargo and vehicles by parachute, water drops, the air ambulance and air defence. It works on **every Vehicle Framework
aircraft from any mod**: helicopters, jets and pods. What every vehicle shares (the tabs, wear, batteries) is in [vehicle
systems](vehicle-systems.md); the aircraft of D'thorga Vehicles, such as the V-22 Osprey, are in [working
vehicles](working-vehicles.md). [Back to the guide](README.md).

## Read this first

1. **Aircraft are checked before they lift off.** Too heavy for the day's heat, with engines or thrusters out, with flight
   controls wrecked, or blind at night or in fog, **the launch button refuses and says why**: unload, repair, or wait for cooler
   air or daylight.
2. **An aircraft idling on the ground with its pilot aboard burns fuel**, and a hover burns more than flying about. **Take the
   pilot out, or undraft it, when it is not needed.** **Aircraft fuel follows the engines** in the settings turns this off.
3. **Aircraft fly each stretch between places at a height of their own**: over the highest ground on the way, and lower when their
   engines cannot climb at their weight or someone aboard has no oxygen. A stretch over mountains too high for them is refused
   before launch. **Cruising height between places** in the settings turns this off.
4. **To carry a vehicle, fit the helicopter a cargo hook and put a sling set in its cargo.** A heavier load, or a lift in a gale,
   is allowed with a warning but strains the hook's mount: **repair the mount before it gives way.**
5. **Cut the engines drops the aircraft where it is.** To land and switch off safely, switch the engines off the ordinary way: it
   lands on the nearest clear spot first.
6. **A parachute drop needs a packed parachute in the cargo for each person, each tonne of cargo and each vehicle.** Without one,
   that load stays aboard. **The wind carries everything downwind**: the drop targeter shows how far, and where the aircraft lets go.

## Before take-off

- **Helicopters, jets and pods are checked before they lift off.** Too heavy for the day's heat, or with engines or thrusters out,
  the launch button refuses and says why. **The setting can allow it at a cost to the engine, or only warn.**
- **A heavy Harrier on a hot day lands on a runway** instead of its pad (with D'thorga Patches).
- **Flight controls**: an aircraft whose control surfaces, actuators, hydraulics or wiring are wrecked below 35% **cannot take
  off**, and the launch button says why.
- **Avionics broken or flat**: it will not take off at night or in fog (see [avionics](#avionics)).
- **Engines start in turn** (the setting **Aircraft engines start one after another**, on): an aircraft's
  engines light one after another, a few seconds each, and it may not take off until all are running.
- **Grit on rough ground** can strike an engine when an aircraft takes off, lands or taxis anywhere but a floor (never while it
  merely stands with its engines on). **Build a concrete strip** for low-intake jets such as the Prowler and the Harrier.
- **A closed sky**: attacking a base whose alarm closes its airspace, your aircraft there cannot take off until the alert ends.

## Avionics

**Every aircraft has flight instruments and navigation**, on its own instrument panel or an added **avionics bay**.

- **Broken or flat, the aircraft will not take off at night or in fog.** Repair it, charge the battery or wait for daylight. The
  setting **Blind take-offs: warn only** lets it go anyway.
- **Without navigation, or with only basic navigation in the dark**, a flight across the world map is slower and burns more fuel.
  Military aircraft carry full navigation and fly straight in anything.
- **Rough landings** (on by default, in D'thorga Framework's settings): landing in poor visibility with the instruments out, or
  with the flight controls shot about, may come down hard and damage the landing gear. Never the crew.
- **Aim in the air** (on by default): a damaged or basic sight misses more, and every aircraft misses more at night without a
  night sight and in bad weather without a radar. Bombs and rockets scatter wider.

**Avionics kits**: any aircraft can be fitted with one it lacks, from its fittings window. Shot out with
its part, a kit stops working until repaired.

| Kit | What it gives |
| --- | --- |
| **Navigation set** | Keeps a flight on track in the dark and fog |
| **Autopilot** | Halves the wandering and the risk of a rough landing |
| **Night sight** | Aims at night as by day |
| **Radar** | Gives back half of what rain, fog and snow take from the aim |

## Engines, controls and fuel tanks

- **Heavy controls.** Helicopters and planes with servos, and the Harrier, fly on hydraulics: if the fluid leaks or the pump is
  hit, the controls go heavy and it flies slower. Top up the fluid in a service.
- **Damaged flight controls** below 70% slow it; below 35% it cannot take off.
- **An engine out** slows it to 0.8 of its speed, and a message says so.
- **A damaged turbine engine gives less power** (the setting **Damaged turbine engines give less power**, on by default, in the
  Flight section): from about half of it when badly damaged to all of it when sound, so a shot-up aircraft
  climbs and hovers weaker before an engine stops.
- **A twin jet with an engine out, or one weaker than the other, dodges a little less fire and burns a little more fuel** holding
  a straight line (the setting **Lopsided thrust unsettles twin jets**, on by default). Helicopters and the Osprey do not, as one
  drive shares their engines' work.

| Twin jet (the A-10, Su-25 and Prowler) | At most |
| --- | --- |
| Less fire dodged | 15% |
| More fuel burned | 7.5% |

- **A twin helicopter's emergency power opens only with an engine out** (the Black Hawk, Hind, Mongoose and Osprey). With both engines sound they stop at their take-off rating, so **a very heavy hover in hot or thin air may strain
  them**; with one engine out the other may run at its short emergency rating, hot.
- **An aircraft's generator** gives about half its output at ground idle and all of it from cruise.
- **Twins keep a tank on each side, with a cross-feed button:**

| Cross-feed | What happens |
| --- | --- |
| **Automatic** | The crew opens it when one side runs dry |
| **Shut** | A holed tank empties only its own side, and that engine stops |
| **Open** | Both engines keep running, but a leak drains all the fuel |

### Gas turbines

**Gas turbines** (aircraft, and the M1A2 tanks) wear their hot section when run past their limits, and twice as fast when held at
take-off power longer than allowed; a message says when one has run too long.

- **A hot-section overhaul** at a dyno bay puts it right. It takes a **hot-section kit** from the parts bench, or 10 plasteel and
  3 components while your colony takes raw materials for services.
- **With one of two engines out, the other runs hot.**
- **What a turbine drives besides the rotor or the tracks runs it a little hotter**: its generator, cabin air and, on the Harrier
  in a hover, air for the puffer jets. The Systems tab lists them.
- **On a twin, each engine keeps its own hot-section life**: the one doing more work wears faster, the
  Systems tab lists each, and an overhaul does the most worn engine, so a twin may need two overhauls, one kit each. A worn hot
  section's breakdowns hit the worn engine.
- **A twin-engined aircraft's new engines go in as a matched pair**: see [the vehicle workshop](vehicle-workshop.md#fittings).

## Fuel

**An aircraft burns fuel by what its engines are doing** (the setting **Aircraft fuel follows the engines**, on by default).

| Where | What it burns |
| --- | --- |
| **Between places**, in good order, nothing switched on | Exactly what its author set |
| **Between places**, with a worn hot section, cabin air or heavy electrical loads | A few per cent more |
| **Over a map, flying about** | What its author set |
| **A lift-off, a climb, a hover** | More: a Harrier hovering down onto its pad burns about twice what it does flying about |
| **Idling on the ground with a pilot aboard** | About a quarter to two-fifths of the flying rate |

- **The range shown before a flight already counts** the extra for a worn hot section, cabin air and electrical loads, and the
  height of every stretch.
- **Take the pilot out, or undraft it, when it is not needed**: an idling aircraft with a pilot aboard burns fuel.
- **An upgrade that changes its fuel use counts everywhere**: the Harrier's plasteel armour burns about
  15% more over a map as well as between places.
- **A helicopter on station over a map works as hard as its load and the air ask**: the Hind and the
  Mongoose burn less flying about light on a cool day, and more heavy on a hot, high one.
- **A slung load makes a flight slower and thirstier**, and the range shown is shorter.

## Height between places

**Each stretch between places is flown at a height of its own** (the setting **Cruising height between places**, on by default). The launch screen's tooltip says when a stretch is not flown at the aircraft's best height, and why. The
fuel it shows, and whether it says there is enough, count the height of every stretch.

| Why | What happens |
| --- | --- |
| **Climbing over high ground** | It keeps 300 m above the ground on the way, 600 m over mountains; a helicopter forced up burns a little more |
| **Someone aboard has no oxygen** | It keeps below 3,810 m |
| **Kept down by its engines** | Heavy, or with an engine lost, it cannot climb as high |
| **Refused** | The ground on the way stands higher than it can climb |

- **Oxygen.** A crew whose oxygen set is broken, or passengers in a cabin that is not pressurised, keep it low. **A cabin oxygen**
  fitting (Fittings and tunes) gives everyone aboard oxygen while the engine runs. **An aircraft with a cracked canopy** loses its
  pressure, and flies low unless everyone aboard has oxygen.
- **A jet flown low is slower and thirstier**: a Harrier with its oxygen out flies at about 84% of its speed and burns about 40%
  more over the same ground. **The Harrier cruises high on its own oxygen**; with the oxygen set out (damaged, without power, or
  with no engine to feed it) it flies its long legs low. Repair the oxygen set (on its avionics part).
- **Crews feel thin air when oxygen fails at height**: tick it in the settings if you want a crew whose oxygen is shot out up high
  to be short of breath for a few hours.
- **Refused:** the tooltip says about how many kilograms lighter it must be, or that it must fly round. Pick a stop to one side of
  the mountains.
- **An engine lost on the way** (to an air defence's hit): a twin flies on lower and slower if it still can. If the ground ahead
  is now too high but the way back is not, it **turns back and lands where it last stopped**, and a message says why. If neither,
  it struggles across, slow and thirsty.

## Thin air and hot days

**The higher a place stands, the thinner its air.** The world map's terrain tab shows each tile's elevation; a map is as high as
its tile.

| Height | Gas turbine's power | A helicopter needs, to hover |
| --- | --- | --- |
| The sea | All | Its usual power |
| 500 m | 94% | 2% more |
| 1,000 m | 89% | 5% more |
| 1,500 m | 83% | 8% more |
| 2,500 m | 74% | 13% more |
| 4,000 m | 61% | 22% more |

- **A helicopter is hit twice**: its engines give less and its rotor needs more. One that lifts a full load at the coast may
  refuse to lift it from a high plateau, and says so. **Lighten it, or wait for cooler air.**
- **Where it lands counts too**. When you pick a destination, a line warns if it would arrive too heavy
  to hover in the air there. With the default setting it then **comes in for a rolling landing**, which wears its undercarriage a
  little; it never stops to refuel in such air. "Allow it, at a cost to the engine" lands it straight down and strains its
  engines.
- Piston engines and land vehicles in high country: see [vehicles on the road](vehicles-on-the-road.md#thin-air-in-high-country).
  **Thin air at height** in the settings turns it off.

## On the world map

- **In flight, an aircraft's systems keep running**: the cab is heated or cooled, the generator charges the battery, and wear and
  faults can arrive in the air. Where **climate control** is fitted it works the heater and air conditioner itself, so their own
  switches read "left to the climate control".
- **Select an aircraft in flight** and its inspect pane says how many more tiles its fuel flies. **Zoom out** to see an amber
  circle at that distance (it is far out: a helicopter's is a good way round the planet). An aircraft that can reach anywhere says
  so and has no circle.

### Refuelling on the way

**Aircraft refuel by setting down on the way**.

1. Load the fuel it burns into the aircraft's cargo.
2. Press **Refuel *aircraft* from cargo** on its buttons before launching (or in flight).
3. Pick a destination: you can then pick one beyond the tank's reach when the fuel aboard covers it. The tooltip says how many
   stops it will make.

- **Whenever the tank falls to a quarter** it sets down on the land below, pours, waits a little while and flies on.
- **It never sets down on water, in mountains or on an enemy's ground**: over the sea it flies on until it reaches land, so **a
  long sea crossing still needs the fuel in the tank**. Nothing pours fuel into an aircraft in the air.
- **At a depot that holds its fuel**, an aircraft with this switched on and below half a tank sets down and fills up by itself
  (see [depots](vehicles-on-the-road.md#depots)).

## Cut the engines

**Cut the engines** is a button on an aircraft in the air. **It falls where it is**: everyone aboard is
hurt, a load on the hook drops, and over a building, rough ground or water it is wrecked. Coming down hard goes in the logbook.
**To land and switch off safely, switch the engines off the ordinary way**: it lands on the nearest clear spot first.

## Carrying a vehicle on a sling

1. **Fit the helicopter a cargo hook** (Fittings and tunes). The **Cherokee**, the **Louie**, the **Hind**, and RimArrow's
   **Ka-27PS** and **Mi-38** have a mount for one.
2. **Put a sling set in its cargo** (made at the vehicle parts bench).
3. **Press Lift a vehicle** and pick one of yours parked outdoors nearby. A colonist hooks it up.
4. **It flies with the helicopter, drawn under it.** The Hind flies across the map: its load hangs under it there and comes down
   once it has landed.
5. **Where it lands, the game pauses and asks where to put the load**: click open ground within reach, or
   right-click to let the crew pick the nearest clear spot. If there was no room, press **Set the load down** once there is.

**From a hover**: a helicopter flying over the map by itself (the Osprey; the Cherokee, the Louie and the
Mosquito with their setting on; and the Hind) can lift without landing.

- Press **Lift a vehicle** in the air; once the load is hooked up, the helicopter hovers beside it and takes it.
- In the air with a load, **Set load down here** picks a spot, and the helicopter hovers beside it and lowers it.
- **Emergency release** drops it where it hangs, and it lands hard.
- **It cannot land with a load hanging**: set it down first.
- **It must be able to hover with the load on**, so a heavy load on a hot day is refused.

**How much the hook takes:**

- **The hook takes a set weight, less on hot days**: the load, its cargo and people.
- **A heavier load, or a lift in a gale or a thunderstorm, is allowed with a warning**. At lift-off the
  hook's mount loses health by the share the load is over, plus a quarter in bad weather, and a mount strained to nothing **gives
  way**, dropping the load back from a few metres. **Repair the mount before it goes.**
- **A slung flight is slower and burns more fuel**, and the range shown is shorter.
- **A towing or towed vehicle cannot be lifted**: unhitch it first.

### Emergency release on the world map

**On a helicopter in flight on the world map, Emergency release lets the load fall**. It lands hard,
badly damaged, and anyone in it is hurt. It lies there as a **wreck**: **right-click it with a caravan to Recover it**, then
repair it, drive it, tow it or lift it home. **A helicopter that crashes drops its load too**, beside the wreckage.

### Lifting a hull

**What a destroyed vehicle leaves can be carried home the same way**.

- Press **Lift a vehicle or a hull** and click the hull; the helicopter's crew hooks it up at once, with no colonist needed.
- Set down at home, **take it apart for its salvaged parts**.
- A hull weighs what the vehicle did empty, so a heavy wreck needs a strong hook.
- **Do not release a hull in flight**: it breaks up and nothing is left.

**Each of these is a setting** under **Sling loads** in D'thorga Framework's settings: hot days, ground crew, weather, slower
flight, and release and drops.

## Dropping by parachute

Aircraft that can drop (the Osprey; the C-17 with its setting on) put people, cargo and vehicles down by parachute, anywhere they can
fly, without landing. The design and its sources: [parachute airdrops](../development/airdrop.md).

1. **Make the parachutes at a rigger's table** (Motor pool, after researching vehicle parts): a **personnel parachute** for each
   jumper, a **cargo parachute** for each bundle of up to a tonne of cargo, a **heavy-drop rig** for each vehicle. **Load them into
   the aircraft's cargo.**
2. **Load what goes**: people in its passenger seats (the pilots stay aboard), goods in its cargo, a vehicle in its hold.
3. **Drop pass**: press it in the air and point at the ground. **The line shows where the aircraft lets go and the ring where loads
   may land**; the words by the mouse give the wind and the drift. It flies straight over the line, drops people, then cargo, then
   vehicles, and flies on.
4. **Drop here** (aircraft that hover): it climbs to the height parachutes are dropped from and drops where it is, then comes back
   down. **Drop low** drops at once from its present height, below the safe height: **landings are much rougher, and too low a
   canopy never opens.**
5. **From the world map**, a loaded aircraft offers **Airdrop on** a map (it makes its pass and flies home), **Airdrop here** on an
   empty place (people form a caravan there; cargo alone waits at a depot to collect) and **Gift by airdrop** to a friendly
   settlement (cargo only, for goodwill).

**Landing:**

- **The wind decides where things land.** Real crews do not jump over 13 knots: above it, and at night, you are warned, and
  landings are rougher. With Combat Extended the wind is its own; without, a wind of D'thorga's wanders the same way.
- **People are rarely hurt**, about 5 in 1,000 jumps, more often at night, in strong wind, with heavy kit, on their first five jumps
  and when they must come down away from open ground. Each jump goes on their record, with a good thought, or a bad one if hurt.
- **Cargo lands still strapped in its bag**, beside its spent canopy, **marked for a colonist to de-rig** (about a quarter of an
  hour), and comes out beside it. **A vehicle lands on its platform**, worn a little, and now and then hard. Nothing lands on a
  roof or inside a wall: it comes down on the nearest open ground.
- **Every canopy is left on the ground as a spent parachute**, every platform as a spent platform. **Carry them home and repack them
  at the rigger's table**, for little or nothing.
- **Parachute ankle braces** (tailoring bench, no research) nearly halve a jumper's chance of being hurt. Put them on before boots.
- **A guided cargo parachute** (rigger's table, after microelectronics) steers its bundle onto the point whatever the wind. The
  aircraft uses these before plain cargo parachutes.
- **Packed parachutes have a quality**, from the packer's Crafting skill. With **Packers' skill decides whether canopies fail** on
  (off by default), a badly packed canopy can fail: a jumper lands roughly on the reserve, and cargo or a vehicle lands hard.
- **Enemies drop too**: some raids by air come by parachute, upwind of open ground outside your turrets. Shoot at the aircraft on its
  pass; with Combat Extended, a CIWS shoots at them as they come down. Their spent canopies are yours to take.
  InterRim Ballistic Missile's CIWS also brings them down: half come down unable to fight, the rest roughly; and it fires at
  their aircraft as it crosses (**CIWS fires at enemy aircraft** in D'thorga Patches, on). A load Combat Extended's CIWS shoots down
  comes down where it is: its people unable to fight, a vehicle landing hard, rather than everyone inside killed.
- **If you leave a map while an aircraft is flying over it**, the aircraft carries on home if it was heading there, or comes down
  on the world map as a caravan.

**The hold:** an aircraft with a hold (the Osprey takes one vehicle up to 2 by 3 cells and 2,500 kg) takes a vehicle of yours aboard
with **Load into hold**: park it inside the cyan area drawn behind the ramp when the aircraft is selected. Its crew rides in the passenger seats. **Unload hold** drives it out of the
ramp. No aircraft, and nothing carrying, towing or holding another vehicle.

**Settings** (Aircraft page, Airdrops): **Airdrop landings can hurt and damage** (on); **Vehicles may drop with their crew inside**
(off: high risk, after the Soviet drops of 1973 and 1976); **Packers' skill decides whether canopies fail** (off); **Other authors' aircraft use D'thorga airdrops** (off: the C-17's own
airdrop is replaced by ours while it is on).

## Fighting a fire from the air

**A helicopter with a cargo hook and a fire bucket in its cargo** (made at the vehicle parts bench) **gets Water drop**.

- **Click a fire within 50 cells**: it flies to the nearest open water, fills the bucket, empties it over the fire and comes back
  to where it stood.
- **A parked tanker or water bowser of yours holding water also fills it** (needs Dubs Bad Hygiene for
  water in tanks): the water comes out of the tank when you order the run.
- **It needs water within reach, open sky over the fire, its crew, fuel and flying weather**; the button says what is missing.
- **A full bucket puts out the fires on about 3 cells across.**
- **The Hind cannot fly water drop runs.**
- Click the helicopter while it flies to see what it is doing.

## An air ambulance

**Fit the litter kit to the helicopter's cabin** (Fittings and tunes). The kit takes a fifth of the cargo
space.

| Helicopter | Where the litter kit goes |
| --- | --- |
| The Cherokee, the Louie | The upholstery |
| RimArrow's Ka-27PS and Mi-38 | The cabin fittings |
| The Hind | The fuselage |
| The Osprey | Its cabin keeps its litters with either kit |

- **Right-click a downed person with a colonist selected and choose Carry to a litter aboard...** Up to 4 people lie on the
  litters.
- **Put a colonist who can doctor in the crew and medicine in the cargo**: while the helicopter stands, they tend the patients. In
  flight nobody gets worse.
- **Press Unload casualties at the other end**, then take them to bed. **Leaving by caravan sets the patients down first.**
- **The litter kit with vitals monitor** adds a **packed vitals monitor**, which haulers bring from
  storage: tending on the litters is **7% better** while it has power, about 8 A on a 12-volt aircraft while someone lies there.
  Taking it off gives the monitor back.

**Take casualties aboard** makes the parked helicopter the place casualties go.

- With it switched on, doctors (and Search and Rescue's field responders) carry downed people to its litters whenever it is nearer
  than their bed, or they have no bed, until every litter is taken; then it says so.
- Prisoners and guests are carried too; anyone set to no medical care is not. Someone bleeding badly is treated where they lie
  first when a field medic is at hand.
- **A downed stranger who is no enemy** (a refugee from a crashed pod, a hurt traveller) is carried too.
  Set down, a colonist must still be told to rescue them, as the game asks for any stranger.
- **Switch it off before you leave** if you do not want it working at the next landing site. It cannot be switched on while the
  helicopter moves or flies.

## Air defence, chaff and flares

With **The Dead Man's Switch - Expedition**:

- **Your aircraft can be shot at on the world map.** Flying to or past a defended settlement, its radars track the aircraft and
  may lock on and fire. A hit damages it, and people in its seats can be hurt. **It comes down at a crash site only if it can no
  longer fly**: controls shot away, every engine out or the airframe broken. Otherwise it flies on, damaged.
- **Before sending one near a defence, a prompt names the danger** and asks you to confirm.
- **Warning receivers** (the Harrier, the Hind and the Mongoose) tell you when a radar tracks the aircraft, locks on, or launches
  at it. Shot out or without power, they are silent.
- **Low flyers are seen later**: helicopters and the low cruise stay under the radars' horizon longer.
- **A closed sky**: attacking a base whose alarm closes its airspace, your aircraft there cannot take off until the alert ends.
- **To fly as before**, untick **The Dead Man's Switch - Expedition's air defence sees aircraft**.

**Chaff and flares:**

- **Make countermeasure cartridges** at the vehicle parts bench: ten for 10 steel and 20 chemfuel, after the turbines research.
  Keep them in storage.
- **Colonists doing hauling work load them into a parked aircraft's dispensers** by themselves, once ten or more are missing.
  **Never into its cargo**: cartridges in the cargo are not fired. The inspect text says how many are loaded.
- **The dispensers hold 60 in a Harrier and 128 in a Hind.**
- **Aircraft with dispensers fire two at every missile.** With a working warning receiver a shot is much less likely to hit;
  without one, they fire late and help less.
- **The Harrier's Chaff and flares button**, on a map in the air (with D'thorga Patches), makes it harder to hit for ten seconds.
  Sound, it dodges as its author set; with shot-up controls or a dead warning receiver it is easier to hit.
- **Enemy Harriers and Hinds carry chaff and flares too**: each arrives with its dispensers a quarter to three-quarters full, and
  one you capture keeps what is left.

How flight works underneath is in the reference page on [flight](../development/systems/flight.md).

## Tested in game

What the owner has passed, by row of [the test list](../development/in-game-test-pass.md). A row passed **in part** says which part.

| Row | What the owner passed | Date |
| --- | --- | --- |
| 49 | A sling lift with the Cherokee, landing with the load | 18 Nov 2026 |
| 55 | The slung load seen from take-off to landing, and set down where you choose | 18 Nov 2026 |
| 57 | **In part:** the load's place at landing, by hand or picked for you (emergency release and the weather warning not yet) | 18 Nov 2026 |
| 103 | A turbine held at take-off power too long wears its hot section faster | 18 Nov 2026 |
| 140 | The warning receiver's messages | 19 Nov 2026 |
| 147 | Aircraft systems keep running in flight | 18 Nov 2026 |
| 187 | Aircraft burn fuel by what their engines do | 18 Nov 2026 |
| 197 | An aircraft sets down on a journey to refuel from its cargo | 18 Nov 2026 |
| 199 | Each stretch of a flight has a height of its own | 21 Nov 2026 |
| 254 | Chaff and flares are loaded into the dispensers, not the cargo | 23 Nov 2026 |
| 282 | An aircraft's fuel upgrade counts over a map | 21 Nov 2026 |

Everything else in this chapter is not yet tested in game.
