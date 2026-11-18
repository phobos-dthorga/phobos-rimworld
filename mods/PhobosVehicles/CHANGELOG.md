# D'thorga Vehicles changelog

Maintained from 8 November 2026. Draft entries record preparation, not Steam publication; nothing is published yet. Not yet
tested in game unless an entry says the owner tested it.

## [Unreleased]

## [0.84.0] - 2026-11-19 - Draft

The Osprey's added parts are in its body, not on the left wingtip

### Fixed

- **The Osprey's generator, battery, starter, avionics bay, ferry tank tie-downs and cabin oxygen panel sit in the forward cabin.** They were all placed on the left nacelle, out on the wingtip, so fire there hit every one of them and the right nacelle carried none.
- **Its research projects moved** with the rest of the tab, so no line crosses another project. Positions only.

### Dependencies

- Needs D'thorga Framework 0.167.0 or later, which can place added parts by the design's word. With an older Framework the game reports an unknown field in the Osprey's loadout at start-up.

### Compatibility and limits

- **Not yet tested in game.** Where the real aircraft's generators and starter sit was not checked; one place in the body for all keeps both nacelles alike.

## [0.83.0] - 2026-11-19 - Draft

The Osprey's rope and hoist mounts at its ramp

### Fixed

- **The fast-rope bar and the rescue hoist sit at the Osprey's ramp**, with the aft cabin's armour; before, both were placed at the middle of its forward cabin with no armour.

### Dependencies

- Needs D'thorga Framework 0.164.0 or later.

### Compatibility and limits

- **Not yet tested in game.**

## [0.82.0] - 2026-11-19 - Draft

The Osprey's faults found by reading, before its first test

### Fixed

- **The Osprey's guns can be fitted.** Fitting the ramp gun or the belly gun threw errors, spent the materials and added no gun: the aircraft lacked the part of its definition that holds a vehicle's guns.
- **The ramp gun needs its crew chief.** It aimed and fired with the seat empty. Fitting a ramp gun now makes the crew chief's seat needed to work it.
- **The belly gun stops without power, as its text says.** It never stopped and drew nothing. It now has its own station on the electrics, drawing while the gun has a target; with no power and nobody aboard the gun stops. Its automatic aim is switched on as it is fitted, where before it did nothing until its button was found.
- **The Osprey is built from turbine engines.** Its blueprint asked for piston engines.
- **The infrared jammer and the terrain radar draw power only while the engines run.** They drained a parked aircraft's battery.
- **A rolling or hard landing wears the undercarriage.** With no part named as landing gear, the wear fell on any outer part, a wing or the cockpit as often as the gear.
- **Without Combat Extended the guns cost far less steel to load**: a twentieth of a steel for a 7.62 mm round and a quarter for a .50, where every round cost one. A belly gun load is 75 steel, not 1,500.
- **With Combat Extended, cartridges are left to Combat Extended**, which sets them itself while its ammunition system is on. With that system off the guns now load steel instead of asking for cartridges.
- **The terrain radar's text no longer promises flying blind at night and in weather**, which is not built yet. The inlet barrier's text says what it now does.

### Dependencies

- Needs D'thorga Framework 0.163.0 or later, which carries the belly gun's automatic aim and the rotor fixes.

### Compatibility and limits

- **Not yet tested in game.** Found by a fault-read of 19 November 2026, by reading the code. Still open: the parts our systems add all sit in the left nacelle.

## [0.81.0] - 2026-11-19 - Draft

The Cherokee, the Louie and the Mosquito may fly over the map on our own flight

### Added

- **With D'thorga Framework's setting Stock helicopters fly over the map on** (it starts off), Vanilla Vehicles Expanded's Cherokee and Louie and Vanilla Vehicles Expanded's Mosquito fly about the map on our flight. Their speeds over a map are ours, as their authors set none: 4 cells a second for the Cherokee and the Louie, 4.2 for the Mosquito, with turn rates, climbs and flight heights by kind. The thrust pods are not included.

### Dependencies

- Needs D'thorga Framework 0.160.0 or later. With an older Framework the game reports an unknown field in these helicopters' flight comp at start-up.

### Compatibility and limits

- **Not yet tested in game.** Over unseen ground they are drawn as the Osprey is, so one parked on unseen ground at a base you attack shows through the fog.

## [0.80.0] - 2026-11-19 - Draft

Armour kits for the service trucks, first part: the kits that need no picture

### Added

- **Three armour kits for all five service trucks**, each an upgrade after the new **truck armour** research (after flak armour): **engine and tank guards** make the engine and fuel tank rifle-proof (300 kg); **run-flat tyres** make the wheels stop shell fragments and pistol bullets (120 kg); **EMP hardening** makes an electromagnetic pulse harm the electrics as it does a military vehicle's (60 kg). With Combat Extended the guards are 8 mm of steel and the tyres 5 mm, and each kit's explanation in the Upgrades tab says what it does in words.
- **A kit's weight is carried as cargo**: the engine works harder and burns more fuel, as with the same weight aboard. On the Talus it also takes the same weight off cargo room; the other four keep their small stowage (owner's choice).
- A guarded engine still burns and a guarded tank still leaks once they are finally holed: it takes heavier fire to get there.
- The kits borrow existing icons (the Osprey's armour kit, the all-terrain tyres, the electrical kit) until their own are drawn.

### Fixed

- The README and the player guide no longer say the Osprey's armour by class is still to come: it arrived in 0.78.0.

### Dependencies

- Needs D'thorga Framework 0.158.0 or later, for EMP hardening.

### Save compatibility

- Safe with a running save. The kits are new upgrade nodes; nothing is removed or renamed.

### Compatibility and limits

- **Still to come:** the armoured cab, heavy cab plates, body plates and the ram bar, which show on the truck and wait on their pictures, and the switch to keep working when enemies are about.
- **Not yet tested in game** (test rows 224 and 225).

## [0.79.0] - 2026-11-19 - Draft

Raiders may come in aboard the Osprey

### Added

- **The Osprey may bring raiders in**: a raid of an industrial or better faction may arrive aboard it, with D'thorga Framework's setting Raiders may arrive by aircraft on. It puts them down and leaves, and can never be kept.

### Dependencies

- Needs D'thorga Framework 0.157.0 or later. With an older Framework the game reports an unknown class in the Osprey's Def at start-up.

### Compatibility and limits

- **Not yet tested in game.**

## [0.78.0] - 2026-11-19 - Draft

The Osprey armoured part by part, and the trucks' armour kits

### Changed

- **The Osprey's parts each have an armour class**, as its record says: skin everywhere, as the real aircraft is unarmoured, but the two engines and the two proprotor gearboxes, which are splinter armour and stop shell fragments and pistol bullets. With Combat Extended they are 2 mm and 5 mm of steel, where the whole aircraft read as 0.1 mm, and it is shot at across its five-cell width. The Health tab names each part's armour.
- **The Osprey's armour kit adds rifle armour to the cockpit and both cabins** rather than setting it, so it lands on the rifle class under Combat Extended too (8 mm, where it would have read 0.45 mm). Its explanation in the Upgrades tab says so in words.

### Dependencies

- Needs D'thorga Framework 0.156.0 or later.

### Save compatibility

- Safe with a running save. Nothing new is saved, and no part is added or removed.

### Compatibility and limits

- **Not yet tested in game** (test rows 220 and 223).

## [0.77.0] - 2026-11-19 - Draft

The Osprey takes a fast-rope bar and a rescue hoist

### Added

- **Two mounts at the Osprey's ramp** for the new fast-rope bar and rescue hoist fittings: two ropes, as the real aircraft's two fast-rope points, and a 600 lb hoist. **The drawn hoist shows on the ramp while it is fitted.**

### Changed

- **The hoist and ropes are fittings, not upgrades in the Osprey's tree** (owner, 19 Nov 2026): built and fitted as the cargo hook is.

### Dependencies

- Needs D'thorga Framework 0.155.0 or later, which carries the ropes and hoist. With an older Framework the game reports unknown fields in the Osprey's flight comp at start-up.

### Compatibility and limits

- **Not yet tested in game.**

## [0.76.0] - 2026-11-19 - Draft

The five service trucks are armoured part by part

### Changed

- **Every part of the Talus, Ullage, Kelly, Datum and Busbar has armour of its own, of the skin class**: thin sheet metal that stops nothing aimed at it, since no source says a civilian truck stops a bullet. Your choice of 19 November 2026. **With Combat Extended** each part is now 2 mm of steel and 4 MPa, where before the whole truck read as 0.4 mm. **Without it** each part rates 0.10 sharp and 0.08 blunt, where the whole truck rated 0.40 and 0.26, so rifle fire hurts the trucks more than before.
- **Each cell of a truck has one outer part**, your choice of 19 November 2026. The chassis is outer only where nothing else covers it; the cab, body, tank, wheels and tailgate keep their own cells; the lights, the survey truck's and generator set's radiators, the tanker's hose reel and the drill rig's rotary head and bit now sit inside. Shots at the cab now strike the cab rather than mostly the chassis, so the trucks are disabled sooner (wheels, cab) and destroyed later. A few parts lose cells another part now holds: the tipper body and tank shell over the rear wheels, the drill string and cuttings chute, the survey boom's tip, and the generator set's second front wheels under the cab.
- **Combat Extended shoots at each truck's full three-cell width** (D'thorga Framework 0.154.0).

### Dependencies

- Needs D'thorga Framework 0.154.0 or later. With an older Framework the game reports the trucks' armour extension as an unknown class at start-up.

### Save compatibility

- Safe with a running save. No part is added or removed, so saved part health stays with its part; only where parts sit and how they are armoured change.

### Compatibility and limits

- **Not yet tested in game.** The trailers, the seabed Crawler and the Osprey keep their whole-vehicle armour for now.
- The trucks' whole-vehicle armour figures stay in their Defs for collisions, which read them; every part's own armour overrides them for hits.

## [0.75.0] - 2026-11-19 - Draft

The Osprey circles

### Added

- **The Osprey can circle a spot** at 5 cells a second, its tightest circle 12 cells out, and holds by circling when it is too heavy to hover. Both figures are authored, not published.

### Fixed

- **The Osprey's armour kit no longer armours it before it is fitted.** Looking at the kit in the Upgrades tab was enough to give the cockpit and cabins its armour until the game was reloaded; saves are unharmed. Its list of effects is now written out in the node. Not yet tested in game.

### Dependencies

- Needs D'thorga Framework 0.153.0 or later, which carries circling. With an older Framework the game reports unknown fields in the Osprey's flight comp at start-up.

### Compatibility and limits

- **Not yet tested in game.**

## [0.74.0] - 2026-11-19 - Draft

The Osprey's guns: a ramp gun, 7.62 mm or .50, and a belly minigun

### Added

- **Ramp gun.** An M240 (7.62 mm) or a .50 machine gun on the Osprey's open ramp, one or the other, worked by the crew chief. It faces behind the aircraft and swings 60 degrees to each side, by hand, so it works without power. Its node needs the ramp gun mount research.
- **Belly gun.** A minigun in a turret under the belly that fires all round by itself, at a low or a high rate. It needs power: with the engines and battery dead and nobody aboard, it stops until it has power again. It weighs 360 kg, taken from the cargo room and carried as cargo. Its node needs the belly gun research, after the game's multi-barrel weapons.
- **With Combat Extended** the guns take their real cartridges, 7.62x51 mm NATO and .50 BMG, and reach as far as Combat Extended's own guns of those calibres. Without it they fire base-game bullets of a similar size and load steel.
- **Each gun stops with the aft cabin** it is mounted in, and works more slowly as that part is damaged.

### Changed

- **The research tab** has two more small Osprey projects: ramp gun mount and belly gun.

### Dependencies

- Needs D'thorga Framework 0.152.0 or later, which carries the guns' fire-mode pictures.

### Compatibility and limits

- **Not yet tested in game.** Ranges, magazine sizes, sway and the base-game bullets are authored. The belly gun is not drawn, being under the aircraft; its button shows the game's minigun until its own icon is drawn. The ramp guns' upgrade icons are interim, cut from their own pictures. The guns use the game's minigun sound, and Combat Extended's machine-gun sounds with it, until the Osprey's own sounds are made.

## [0.73.0] - 2026-11-19 - Draft

The Osprey takes off and lands rolling when too heavy to hover

### Added

- **The Osprey can take off rolling.** Up to its published rolling take-off weight, 57,000 lb against 52,600 lb straight up, it leaves for another place along a strip of 5 to 20 clear cells ahead of it, the heavier the longer. Its gear takes rough ground, so no built floor is needed. It lands rolling on a strip of 20 cells when it cannot hover.

### Dependencies

- Needs D'thorga Framework 0.151.0 or later, which carries the rolling take-off. With an older Framework the game reports unknown fields in the Osprey's flight profile at start-up.

### Compatibility and limits

- **Not yet tested in game.** The strip lengths are authored, not published figures.

## [0.72.0] - 2026-11-19 - Draft

The Osprey's upgrades: second hook, armour kit, infrared jammer, terrain radar, inlet barrier

### Added

- **The Osprey's upgrade tree.** Each has its own small research after tiltrotor aircraft. **Second cargo hook:** with the cargo hook fitting fitted, its sling lifts 6,804 kg instead of 4,536 kg. **Armour kit:** rifle-proof plates in the cockpit and cabins, 360 kg taken from its cargo room. **Infrared jammer:** two small turrets on the sponsons that make air defences hit far less often, drawing power. **Terrain-following radar:** a fuller nose, drawing power. **Inlet barrier:** filters on the nacelles, for 3% of the engines' power.
- **The Osprey can sling a load** with the cargo hook fitting, as every helicopter can. Its stretchers, ferry tanks and cabin oxygen are the same fittings every aircraft takes.

### Changed

- **The research tab** has five small Osprey projects after tiltrotor aircraft.

### Dependencies

- Needs D'thorga Framework 0.150.0 or later, which carries the upgrades' systems. With an older Framework the game reports unknown stats and an unknown upgrade class at start-up.

### Save compatibility

- Safe with a running save. An Osprey already built gets its tree, its sling and its hook mount at the next load.

### Compatibility and limits

- **Not yet tested in game.** The terrain radar does nothing yet beyond its draw and its look: what it lifts, slowing aircraft at night and in weather, comes later for every aircraft. The armour kit's Combat Extended figures come with armour by class. Its node icons are made from its own art until the drawn ones are picked. Its guns come next.

## [0.71.0] - 2026-11-19 - Draft

The Osprey flies over the map

### Added

- **The V-22 Osprey flies over the map on D'thorga Framework 0.147.0's own flight**: it takes off straight up, flies to any spot, holds in a hover and lands where you choose, at up to 7 cells a second (authored, in proportion to its speed between places), 60 m up (authored). Its engines read the flight: a hover asks its fitted hover figure, forward flight 0.6 of it. It is drawn over hidden ground while it flies there. A patch file of its own, so the Osprey's Def is unchanged.

### Dependencies

- Needs D'thorga Framework 0.147.0 or later.

### Save compatibility

- Safe with a running save.

### Compatibility and limits

- Leaving the map and arriving are still Vehicle Framework's, from the ground. Not yet tested in game.

## [0.70.0] - 2026-11-19 - Draft

The Osprey's rotors and tilting nacelles

### Added

- **The Osprey's rotors and nacelles are drawn.** Two three-bladed rotors, still with the engines off and spinning when drafted, the two turning opposite ways; nacelles in its paint. On take-off it rises with the nacelles upright, then tips them forward through the climb away, each rotor flattening and moving forward; landing does the reverse. A nacelle below 60% health shows soot and holes. With the inlet barrier fitted (an upgrade still to come), its nacelles show the filter.

### Dependencies

- Needs D'thorga Framework 0.146.0 or later, which draws them. With an older Framework the game reports the Osprey's tiltrotor look as an unknown class at start-up.

### Save compatibility

- Safe with a running save. Ospreys already built show their rotors at the next load.

### Compatibility and limits

- **Not yet tested in game.** On the map the nacelles stay upright: tilting in flight over the map comes with our own flight. Its upgrades, guns, armour and sounds follow.

## [0.69.0] - 2026-11-19 - Draft

The V-22 Osprey, our first aircraft: its first slice

### Added

- **The V-22 Osprey, a tiltrotor aircraft.** Research **tiltrotor aircraft** (6,000, after turbine engines) and build it at the motor pool. Two pilots fly it, a crew chief rides at the ramp and 24 can sit in the cabin; it carries 5,600 kg. It takes off and lands straight up and flies between places at 17.5, twice as fast as any helicopter, on a 1,500 chemfuel tank that burns 200 a day. Its weights, engine power and fuel are the real machine's, so the general systems judge its hover, cruise height and cabin oxygen as they do any aircraft. Its own art: the body in your colours (the grey takes the main paint, the orange bands the second), and damage pictures on the cockpit, cabins, wings and tail.

### Changed

- **The research tab** has a new project, tiltrotor aircraft, after turbine engines.

### Dependencies

- Needs D'thorga Framework 0.144.0 or later, as before.

### Save compatibility

- Safe with a running save. Remove it only once no Osprey is left, as for our other vehicles.

### Compatibility and limits

- **Not yet tested in game.** This is the Osprey's first slice: it flies on Vehicle Framework's own take-off and landing for now, and its rotors and tilting nacelles are not drawn yet (the renderer is the next slice). Its upgrades, ramp gun and belly gun, armour by class and our own flight over the map follow in later slices, before the first test.

## [0.68.0] - 2026-11-19 - Draft

The Talus unloads its gathering at a bulk bunker by itself

### Added

- **The Talus unloads by itself.** Choose a bulk bunker or a stockpile under **Unload at** in its **Gather what** window: each time it is full it backs up there tail first, tips what it gathered (spare parts stay aboard) and goes back out. **The bulk bunker is always backed up to from its open end**, the striped wheel stop, never against a wall. D'thorga Framework 0.144.0 does the work, and its lifts now take about 20 granite chunks lying together.

### Dependencies

- Needs D'thorga Framework 0.144.0 or later. With an older Framework the game reports the bunker's backing-up side as an unknown class at start-up.

### Save compatibility

- Safe with a running save. Bunkers already built get their open end at the next load.

### Compatibility and limits

- **Not yet tested in game.** Which end is open is read from the bunker's picture, which turns with it; a bunker built before this version keeps its rotation, so its open end is where it always was.

## [0.67.0] - 2026-11-19 - Draft

The Talus gathers by itself once its loading grab is fitted

### Added

- **The Talus gathers by itself.** With its loading grab fitted it gets **Gather** and **Gather what**: choose what it fetches as for a stockpile (chunks and raw resources to start, anything but corpses to choose from), put someone aboard who can drive, and it collects loose things about the map until its cargo is full or nothing it can reach is left. It never takes from storage or anything forbidden, and stops where it is. D'thorga Framework 0.143.0 does the work.

### Dependencies

- Needs D'thorga Framework 0.143.0 or later. With an older Framework the game reports the Talus's gathering comp as an unknown class at start-up.

### Save compatibility

- Safe with a running save. A Talus already built gets the buttons at its next load once its grab is fitted.

### Compatibility and limits

- **Not yet tested in game.** As slow as the grab: a full load of chunks takes about 1.9 in-game days.

## [0.66.0] - 2026-11-18 - Draft

The Harrier, the Hind and the Mongoose get cruising figures of their own

### Added

- **The Harrier, the Hind and the Mongoose fly each stretch between places at a height of their own**, with D'thorga Framework 0.141.0's cruising height. The Harrier is built to cruise at 7,000 m (authored) with a published 15,240 m ceiling, read as 11,000 m; the Hind at 1,000 m (authored) with 4,500 m; the Mongoose at 1,000 m with 4,725 m. The ceilings come from secondary sources named in the loadouts.

### Dependencies

- Needs D'thorga Framework 0.141.0 or later for the figures to mean anything; with an older Framework the game reports the unknown cruise block once at start-up.

### Save compatibility

- Safe with a running save. Nothing new is saved.

### Compatibility and limits

- Not yet tested in game.

## [0.65.5] - 2026-11-18 - Draft

Upgrade trees drop a marker that does nothing now

### Changed

- **Our eleven upgrade trees no longer carry the research marker.** Vehicle Framework 1.7.2537 enforces the research on every tree itself, so the marker had nothing left to do. Every upgrade still waits for the same research and says which project (D'thorga Framework 0.137.2).

### Dependencies

- Needs D'thorga Framework 0.137.2 or later beside it, and Vehicle Framework 1.7.2537 or later for upgrade research to be checked.

### Compatibility and limits

- Safe with a running save. No change in play.
- Not yet tested in game (test row 195).

## [0.65.4] - 2026-11-18 - Draft

Own pictures for the Crawler's upgrades and Billy's tent lamp and heater

### Changed

- **The Seabed Crawler's laboratory, advanced laboratory, laboratory refit, scrubber and sensor refit have their own pictures** (the owner's picks of 18 November 2026), replacing ones drawn in code.
- **Billy's tent lamp and tent heater have their own pictures** instead of the game's standing lamp and heater; the insulated tent upgrade shows the new heater.

### Compatibility and limits

- Pictures only; no change in play. A lamp or heater already set out shows its new picture at once. Safe with a running save.
- Not yet tested in game.

## [0.65.3] - 2026-11-18 - Draft

Billy's kettle takes its water again with Dubs Bad Hygiene

### Fixed

- **Billy's kettle took no water with Dubs Bad Hygiene.** The patch that gives the kettle its plumbing and its 10 litres a batch
  failed at start-up with an error in the log, so batches cooked without water. It now applies. Found in the owner's log of
  18 November 2026, on Vehicles 0.65.0.

### Compatibility and limits

- A kettle already set out takes its plumbing at the next set-up: pack Billy up and set it up again.
- Not yet tested in game.

## [0.65.2] - 2026-11-18 - Draft

Which real engines hold their power in high country

### Changed

- **Each real engine says whether it is turbocharged**, for D'thorga Framework 0.132.0's thin air. Turbocharged engines hold their
  rated power to about 1,500 m: the L-ATV, the MaxxPro, the Land Cruiser, the PLS, the Bradley, the M48H, the M10 Booker, the Wiesel
  and Trains of the Rim's diesel-electric shunter. The HMMWV, the M113, the T-54 and the BMP-1 have no turbocharger and lose power
  from the sea up, about a tenth at 1,000 m. It tells only where power holds a vehicle back: a climb, soft ground, a heavy tow. The
  Abrams' gas turbine loses power with height all the way.
- **The Harrier's engine has a size and a new idle fuel share** (both authored), for the Framework's aircraft fuel: its alternator and
  bleed air are now the small share of the engine's work they really are, and idling on the ground burns about a quarter of what
  flying about does.

### Dependencies

- Install D'thorga Framework 0.132.0 with it. An older Framework does not know the new figures and reports them as errors at start-up.

Not yet tested in game.

## [0.65.1] - 2026-11-18 - Draft

The diesel-electric shunter charges its battery at idle

### Fixed

- **Trains of the Rim's diesel-electric shunter** now charges its battery while idling: its engine turns its alternator 4 times faster
  (authored), using D'thorga Framework 0.129.1's alternator ratio. Before, at 450 rpm the alternator never reached its charging speed,
  so a shunter standing with its lights on ran its battery flat.

### Dependencies

- Needs D'thorga Framework 0.129.1 or later.

### Compatibility and limits

- Safe to add to an existing save. Trains of the Rim declares only RimWorld 1.6, so this is checked by arithmetic only; not yet tested
  in game.

## [0.65.0] - 2026-11-17 - Draft

RimArrow: Search and Rescue's helicopters join the vehicle systems

### Added

- **The Ka-27PS and the Mi-38** (RimArrow: Search and Rescue, by Turbo; owner's request of 17 Nov 2026) are fitted as the helicopters
  they are. Before, they counted as jets: a turbofan, the jet's flight rules, and no rotor drive, sling, hook mount or cabin. Now each has
  two turboshafts bound to its own engines, its own transmission, gearbox and rotor as the rotor drive, hydraulic flight controls, full
  navigation and an autopilot, and a mount for the cargo hook. Its cabin fittings take the litter kit, and every shared fitting and
  upgrade family that suits a helicopter suits them, beside RimArrow's own upgrade trees. Every variant (Ka-27PSD, Mi-38T, Mi-38PS) is
  covered.
- **Sling ratings:** 5,000 kg for each. The Mi-38's is the maker's figure. For the Ka-27 none was found, so it is its civil derivative
  the Ka-32's certified figure, approximate. The Ka-27 has coaxial rotors and no tail rotor; the Mi-38's tail rotor stays RimArrow's own part.

### Compatibility and limits

- Loaded only with RimArrow: Search and Rescue active, and after it. RimArrow's own save conversion re-creates missing parts from the
  vehicle's Def list, which includes the hook mount added here, so the two agree. Any faction's Ka-27 or Mi-38 gets the same loadout; it
  changes how the vehicle runs, not who owns one. No settings were added or changed. Test row 177. Not yet tested in game.

### Save compatibility

- Safe to add to a running colony; a helicopter you already own gains its hook mount at the next load. The owner's colony had neither
  helicopter when this was written.

## [0.64.2] - 2026-11-17 - Draft

The bulk tanker's upgrades branch from its tanks, with D'thorga Framework 0.128.0

### Fixed

- **The tanker's upgrade tree branches from its tanks** (owner, 17 Nov 2026): the compartments, the second hose reel and the electric
  pump could be fitted to a bare deck. Now the compartments need a liquid tank and must come off before it, and the hose reel and
  electric pump need any tank but stay on the truck when the tank is swapped. Lines in the Upgrades tab show which tanks each hangs
  from, and the buttons say what to fit or take off first. The tree is laid out again to match.

### Save compatibility

- Node keys are unchanged. A tanker already carrying compartments, a hose reel or a pump without a tank keeps them.

### Compatibility and limits

- Needs D'thorga Framework 0.128.0: install the two together, or an older Framework logs an error for the tree. Test row 176. Not
  yet tested in game.

## [0.64.1] - 2026-11-17 - Draft

The bulk bunker's own picture

### Changed

- **The bulk bunker has its own picture**, AI-generated with OpenAI's image API and picked by the owner from two attempts (17 Nov
  2026), in place of the stand-in drawn in code. No change in play. The loading chute's picture is still a stand-in.

## [0.64.0] - 2026-11-17 - Draft

The loading chute, with D'thorga Framework 0.127.0 (universal hoppers UH13)

### Added

- **The loading chute**: a one-cell steel funnel that drops whatever lands in it into the cargo of a vehicle parked across its spout,
  the cell it points at. Fill it by belt (VFE Factory) or by hand; with no vehicle there, or a full one, it holds up to four stacks and
  waits. It starts set to take nothing, so haulers never fill it unasked: tick what it should load. Tipper research, motor pool tab,
  30 steel (agent defaults, authored). Its picture is a stand-in drawn in code.

### Changed

- The bulk bunker's stand-in picture is now drawn by the same script as the chute's; the picture itself is unchanged. Its generated
  picture is to come from the OpenAI image API (owner's choice, 17 Nov 2026).

### Dependencies

- D'thorga Framework 0.127.0, for the chute's workings.

### Save compatibility

- Safe to add to a running colony. To remove the mod, empty and deconstruct every loading chute, as with the bulk bunker.

### Compatibility and limits

- New test row 174. Not yet tested in game.

## [0.63.0] - 2026-11-17 - Draft

The bulk bunker, with D'thorga Framework 0.125.0 (universal hoppers UH2)

### Added

- **The bulk bunker**: a 3 by 4 open concrete bay, 8 things a cell, which holds 96 chunks, one full load of the Talus (owner's choice
  of size, 17 Nov 2026). A tipper backs up to its open end and tips straight in. It starts set to take chunks; anything else can be
  allowed in its storage settings. Its goods lie on its cells like a shelf's, so VFE Factory's belts take from it and its machines read
  it on an input cell, a paste dispenser beside it draws from the cell it touches, and with The Dead Man's Switch its automatic benches
  link to it as to their own feed hopper. Unlocked by the tipper research (owner's choice); built from the motor pool tab, 120 steel
  (authored).
- Its picture is a stand-in drawn in code (owner, 17 Nov 2026), until generated art is picked.

### Dependencies

- D'thorga Framework 0.125.0, for the tip into storage.

### Save compatibility

- Safe to add to a running colony. To remove the mod, empty and deconstruct every bulk bunker first: the game removes the building and
  its goods are left lying on the ground.

### Compatibility and limits

- Haulers fill it with chunks lying about the map, as they would any stockpile that takes them; lower its priority or untick chunks
  to keep it free for a truck.
- Vanilla Nutrient Paste Expanded's grinder does not read it: the grinder looks only at a hopper's centre cell.
- New test rows 167 and 169. Not yet tested in game.

## [0.62.0] - 2026-11-17 - Draft

Power steering on our heavy trucks, with D'thorga Framework 0.123.0

### Added

- **Power steering** (D'thorga Framework 0.123.0's cross-links G17) on the Busbar generator set and on the L-ATV, MaxxPro and HMMWV tie-ins,
  whose loadouts list their own systems; our other heavy trucks take it from the Framework's heavy loadout. The Busbar also has its fuel tank
  (which a shot can now hole) and its cab (for cab insulation) named.

### Changed

- The shower trailer's water heater Def no longer calls its picture a stand-in, and the bulk tanker's seven upgrade icons each have an asset record saying which overlay they were cut from. No change in play.

### Dependencies

- D'thorga Framework 0.123.0.

### Save compatibility

- Safe to add to a running colony: the power steering pump is appended to each truck, whole.

### Compatibility and limits

- No settings changed. Not yet tested in game (row 162 of the test pass).

## [0.61.1] - 2026-11-17 - Draft

Capitalised upgrade names

### Fixed

- Every upgrade's name now starts with a capital letter, as other vehicle mods' do: the upgrade tab shows a name exactly as written, so the kitchen's bigger water tank read in lower case (owner's report, 17 November 2026). 55 upgrades across every vehicle, the tie-in ones included (the tanker's three gas vessels in a follow-up the same day), and a test now refuses a lower-case one. Names and saves are otherwise unchanged. Not yet tested in game.

### Changed

- The README names what the owner has tested in game: the survey truck and its boom (9 November 2026) and the Busbar's size and look (13 November 2026). No change in play.
- The README is cut to today's gotchas, a list of the vehicles linking the player guide, and links for modders; the how-to lives in the guide and the version notes in this changelog. No change in play.
- Comments in the Defs and the Crawler's texture record name the design records they now live in, after the merges of 17 November 2026. No change in play.
- The Busbar's Def comments give the right day for its change of size, 13 November 2026. No change in play.

## [0.61.0] - 2026-11-17 - Draft

Research for the trailers

### Added

- **Four research projects on the D'thorga tab** (research-gates audit of 17 Nov 2026, the owner's picks, costs as proposed): **towing and
  trailers** (1,000, after vehicle parts) for the flatbed and the bowsers; **field kitchen** (2,000, after towing and trailers and electricity)
  for Billy, and with it caravan cooking; **field catering at scale** (1,500, after field kitchen) for Billy's second range and big kettle;
  and, with Dubs Bad Hygiene, **field ablutions** (1,500, after towing and trailers and Dubs' plumbing and septic tanks; Dubs Central
  Heating's heating too where it is on) for the shower trailer.

### Balance

- Until now every trailer opened with vehicle parts. **The water bowser also needs Dubs' plumbing**, and **Billy's wash-up basin and bigger
  water tank need it too**.

### Save compatibility

- Safe to add to a running game. **A colony that had vehicle parts is granted the four projects once, free**, so it keeps everything it could
  build. A new colony researches them. Trailers already built are untouched.

### Compatibility and limits

- Needs D'thorga Framework 0.122.1. No settings changed. Not yet tested in game (test row 157).

## [0.60.0] - 2026-11-17 - Draft

Billy's water, dining annex and wash basin

### Added

- **Water for Billy's kettle** (with Dubs Bad Hygiene only; owner, 17 Nov 2026): each batch of ten or twenty takes 10 L from the plumbing under the
  kitchen. **Billy carries its own 200 L water tank**, filled from a water bowser beside it or from plumbing, as any water tower is; without water
  the kettle's bills wait and its inspect pane says why. Without Dubs Bad Hygiene the kettle needs no water.
- **Three upgrades for Billy**: a **dining annex**, a second canvas room pitched against the kitchen's front with a table and four chairs, so the
  crew eat at a table (it needs a patch 6 cells longer in front of the trailer); a **wash-up basin** in the tent (Dubs Bad Hygiene), which keeps
  the tent cleaner, so food cooked there spoils people less often; and a **bigger water tank**, 600 L (Dubs Bad Hygiene).

### Changed

- **The tent heater burns only while the tent is below 15 degrees** (owner, 17 Nov 2026), and nothing in warm weather; its inspect pane says so.
- **Billy's power point stands outside the tent's far wall**, clear of the dining annex.

### Compatibility and limits

- Needs D'thorga Framework 0.122.0. No settings changed. Not yet tested in game (test row 156).
- No tie-in with Field Tents (owner's choice): a packed Field Tent carried as vehicle cargo may meet that mod's own reported error.

### Save compatibility

- Safe to add to a running game. A Billy set up before this version sets out its new pieces at its next set-up.

## [0.59.0] - 2026-11-17 - Draft

Billy the field kitchen, and the Pintle shower trailer

### Added

- **Billy field kitchen** (with D'thorga Framework 0.121.0; the owner's design of 13 Nov 2026, built at the owner's word on 17 Nov 2026): a 3 x 8
  trailer that any vehicle with a hitch tows. A colonist sets it up in about an hour of work and it pitches a canvas kitchen tent beside itself, a
  real room, with a field range, a field kettle, a shelf for meals, a cold box, a lamp and a power point outside the flap; packing up takes it all
  in again, with the range's fuel and bills kept. The range burns chemfuel or wood and cooks every recipe the game's and other mods' stoves cook.
  The kettle cooks simple and fine meals ten at a time for 80 percent of the work. Plugged into a grid or the Busbar, the range and kettle work a
  quarter faster, the cold box keeps its food at 4 degrees and the lamp lights. Its upgrades: a second range, a big kettle (meals twenty at a
  time) and an insulated tent with a chemfuel heater.
- **Caravan cooking at Billy**: once a night a resting caravan's cooks turn its raw food into simple meals at a Billy it carries, on its own
  wheels or hitched, up to twenty a cook, burning half a unit of fuel a meal (the setting is D'thorga Framework's, on by default).
- **Pintle P4 shower trailer** (with Dubs Bad Hygiene only; owner's choices of 17 Nov 2026: flushing toilets with a waste tank, a chemfuel water
  heater with power a bonus): the water bowser's chassis with a 2,000 L water tank. Set up, it pitches a canvas shower tent with two of Dubs'
  showers and two flushing toilets, a chemfuel water heater outside and a power point; everything is plumbed into its tank and its waste tank. Fill
  it from a water bowser or plumbing. Dubs' colonists empty its waste tank into sludge drums at half full, or a tanker parked against its rear
  pumps it out. Plugged in, the heater burns half the chemfuel. Its upgrades: the water bowser's lagged tank and second hose, and a third shower.

### Compatibility and limits

- Needs D'thorga Framework 0.121.0. The shower trailer exists only with Dubs Bad Hygiene, and not in Dubs' lite mode.
- **Billy has its own picture**, a kitchen box van on the bowsers' chassis (the owner's pick of three paid attempts, US$0.166, 17 Nov 2026); the
  **shower trailer keeps the bowsers' picture** with its teal stripe (owner's choice). The range, kettle, cold box and water heater have their
  own pictures, the owner's picks of the candidates made that day; the lamp and tent heater use the game's own pictures.
- **Sounds** (the owner's picks of 17 Nov 2026): the range's and kettle's burners while cooking, kept low under the game's frying (owner: a
  background, not a nuisance); the kettle's lid as a batch starts; the tent pitched and struck; a trailer's legs at set-up and pack-up, on the
  bowsers too; the Pintle trailers' pump, so **the bowsers' pump is heard now**; and the water heater while it burns. The cold box is silent
  (owner's choice).
- Not built in this version: water for Billy's kettle with Dubs Bad Hygiene, the kitchen's dining annex and wash-up sink, enemy bases with set-up
  trailers (owner, 17 Nov 2026: a later batch). Not yet tested in game.

### Save compatibility

- Safe to add to a running game. Pack both trailers up before removing the mod: a pitched tent's walls, roof and pieces belong to it.

## [0.58.0] - 2026-11-17 - Draft

Every aircraft's figures for air defence, and the Harrier's high cruise

### Added

- **Each aircraft's speed, cruising height, low-cruise height and size on radar** (with D'thorga Framework 0.119.0; air-defence tie-ins
  AD3): the Harrier fast and high (7,500 m), the Hind and the Cherokee big and low, the Mosquito and Louie low, the thrust pods slow, the
  Warbird and Smuggler between. All authored on The Dead Man's Switch - Expedition's own scales; they matter only with that mod.
- **The Harrier cruises high** (avionics A14): with its oxygen set out it flies its long legs low, slower and thirstier.

### Compatibility and limits

- Needs D'thorga Framework 0.119.0. No settings changed. Not yet tested in game.

### Save compatibility

- Safe to add to a running game; nothing new is saved.

## [0.57.0] - 2026-11-17 - Draft

An electric pump for the bulk tanker

### Added

- **An electric pump upgrade for the Ullage bulk tanker** (with D'thorga Framework 0.116.0; owner, 17 Nov 2026). The pump runs off the
  engine as before; with the upgrade it can set up and pump with the engine off, on the battery, until the battery is flat. 20 steel,
  2 industrial components and 2,000 work (authored). Nothing shows on the body; its picture in the upgrade tree is the deep-cycle
  battery fitting's, reused.

### Compatibility and limits

- Needs D'thorga Framework 0.116.0. No settings changed. Not yet tested in game.

### Save compatibility

- Safe to add to a running game; the node is new and nothing it adds is saved beyond Vehicle Framework's own upgrade record.

## [0.56.2] - 2026-11-16 - Draft

Mounting points marked as not counting toward a vehicle's destruction

### Fixed

- **The cargo hook mount on the Cherokee, the Louie and the Hind no longer keeps a wreck standing** (with D'thorga Framework
  0.114.5, which does the same for the tank cradle, lamp bracket, winch mount and ferry tank tie-downs on every vehicle). It can still
  be hit and broken.

### Compatibility and limits

- No settings were added or changed. Not yet tested in game.

### Save compatibility

- Safe to add to a running game. Nothing new is saved.

## [0.56.1] - 2026-11-16 - Draft

Fewer stray flecks in our vehicles' paint

### Fixed

- **Fewer stray specks of unpainted metal on our vehicles' paint** (the owner's choice, 16 November 2026, after a new checker counted them): weathering drawn into the pictures had been left out of the paint, so it showed as pale or grey flecks once the vehicle was tinted, worst on the Fathom Crawler's dark hull. Specks wholly inside a painted panel now take its paint; bolt heads and other details drawn in their own outline stay as drawn. Counted on each vehicle's front picture: the Ullage tanker 18 to none, the Crawler 87 to 22, its sensor refit 272 to 24, its research refit 36 to 3, the generator set 29 to 15, the bowser 15 to 9, the drill rig 6 to 5, the flatbed 3 to 2, the survey truck 5 to 1; the tipper had none. Shapes and upgrade pictures are unchanged. Not yet tested in game.

### Compatibility and limits

- No settings were added or changed.
- The few flecks left are mostly blends where two paints meet; the checker keeps counting them.

### Save compatibility

- Safe to add to a running game.

## [0.56.0] - 2026-11-16 - Draft

Our vehicles get the new driver system, and the bowsers get their own picture

### Added

- **The Pintle water and fuel bowsers have their own picture**: a pale tank with a walkway and hatches on the Pintle chassis, a hose reel and pump at the rear, picked by the owner (16 November 2026). Both share it and are told apart by the stripe along the tank, **blue on the water bowser and orange on the fuel bowser**, which the player can repaint. Until now both borrowed the Pintle flatbed's picture. **Their upgrades show**: the lagged tank as a padded jacket over the tank, the second hose as a second reel across the front of the tank. Drawn the same length as before. AI-generated with the OpenAI image API (US$0.33 for seven attempts); see the texture record. Not yet tested in game.

- **The driver system on every vehicle this mod gives its own list of systems**: our generator set, and the GTek L-ATV and MaxxPro, the HUMVEE, the RimThunder tanks and HEMTT, and the Trains of the Rim locomotives from their authors' mods. A driver who is hurt, drugged or tired drives them slower, gently, as on every other vehicle. Needs D'thorga Framework 0.113.0. Not yet tested in game.

### Fixed

- **Cleaner paint on every vehicle of ours** (the owner's report of the bowser, 16 November 2026): the parts that take the player's colours are now worked out a whole panel at a time, not a dot at a time, so no stray flecks of the old colour sit along stripes and edges, and no panel is half painted. The Ullage tanker, Datum survey truck, Kelly drill rig, Busbar generator set, Talus tipper, Pintle flatbed and bowsers, and the Fathom Crawler with both refits are made again from their original pictures; their shapes and upgrade pictures are unchanged. Not yet tested in game.

### Compatibility and limits

- No settings were added or changed here; the driver system is switched off in D'thorga Framework's list of systems.
- A lagged water bowser shows the jacket's own orange edge, not its blue stripe. The pump still has no sound of its own.

### Save compatibility

- Safe to add to a running game.

## [0.55.0] - 2026-11-13 - Draft

Two tank trailers: the Pintle water bowser and the Pintle fuel bowser

### Added

- **Pintle fuel bowser** (Pintle P3): a tank trailer of chemfuel, 3 x 8 cells, 2,500 litres, built from the motor pool after vehicle parts research. Any vehicle with a hitch tows it; a colonist sets it up where it stands, and its pump fills vehicles and fuelled buildings within 10 cells, or a tanker beside it, and takes chemfuel from stacks beside it, a chemfuel pipe or a tanker. Needs D'thorga Framework 0.111.0. Not yet tested in game.
- **Pintle water bowser** (Pintle P2), with Dubs Bad Hygiene only: a tank trailer of water, 3 x 8 cells, 2,500 litres. It fills from plumbing with a water tower on the line, or a tanker of water, and fills the water towers on the plumbing it touches at a camp. Parked, it lays hidden plumbing beneath itself, so a pipe touching any side joins it. It keeps the worst water it was filled with, and water freezes in it below 0 C. Not yet tested in game.
- **Two upgrades on each**: a **lagged tank** (the load takes four times as long to reach the outside temperature) and a **second hose** (the pump moves half as much again). Their icons are the tanker's for the same things.

### Balance

- Each bowser holds 2,500 litres, weighs 2,000 kg empty and pumps 400 litres an hour, two thirds of the Ullage's pump. The capacity follows the Ullage's general tank (3,000 litres over six rows of shell; a bowser has five). All authored, labelled approximate; the US Army's M149 water trailer, about 1,514 litres, is the real-world reference for scale.
- Building one takes 260 steel and 2 components for its shell, and its parts by role: four wheels, two springs, a frame, wiring and a hose.

### Compatibility and limits

- **Their pictures are the Pintle flatbed's**, scaled to their size, until their own are made (the owner's rule: no stand-in body art). Their upgrades show nothing on the picture yet.
- **The pump has no sound yet**: working machinery is never shared. Their hoses, relief valve, leak and purge sound as the tanker's do, being the same things.
- The water bowser is dropped in Dubs Bad Hygiene's lite mode, where there is no plumbing and no water to carry.
- Raiders bring no bowsers, and none stand at enemy bases yet.

### Save compatibility

- Safe to add to a running game. **Before removing the mod, scrap every bowser**: they go with it. Removing Dubs Bad Hygiene removes the water bowser.

## [0.54.1] - 2026-11-13 - Draft

The Pintle trailer's picture is sharp at its new size

### Fixed

- **The Pintle flatbed trailer's picture is drawn sharp at its new size.** Since 0.54.0 the game stretched a picture made for the old size over the bigger trailer, a little softer than every other vehicle of ours; it is now exported again from the same full-size original at the new size, with the greedy boards refitted to it. The look is the same. Not yet tested in game.
- **The towing eye at the front of the drawbar is open**: the ground shows through the ring, where white showed before. Not yet tested in game.

## [0.54.0] - 2026-11-13 - Draft

The Pintle flatbed trailer is as wide as the trucks that tow it

### Changed

- **The Pintle flatbed trailer is 3 x 7 cells**, as wide as the trucks that tow it (it was 2 x 5, which the owner had found too small for some time). Its picture is the same, scaled up evenly, and its parts sit on the new footprint: the drawbar at the front, the two axles behind the middle, the lights at the back. It follows a little further behind the vehicle towing it, in proportion. Not yet tested in game.

### Balance

- **It carries 5,000 kg** (was 3,000 kg) and weighs 2,800 kg empty (was 2,000 kg); the greedy boards add 1,000 kg (was 600 kg). A real medium tandem flatbed of about this size carries 3.5 to 5 tonnes (general); the figures are our choice. Building one takes 250 steel and more work (was 180).

### Save compatibility

- **Park every Pintle in open ground before updating**: a trailer already standing beside a wall or a building grows by a cell on each side and two in length, and may end up overlapping it.

## [0.53.0] - 2026-11-13 - Draft

Avionics fits for every aircraft

### Added

- **Avionics fits** (needs D'thorga Framework 0.110.0): the Harrier carries full navigation, an autopilot, radar, a full sight, a night sight, a warning receiver, countermeasures and oxygen; the Hind full navigation, an autopilot, a full sight, a warning receiver and countermeasures; the Mongoose full navigation, a full sight, a night sight and a warning receiver; the Cherokee full navigation and an autopilot; the Warbird a gunsight; every other aircraft flight instruments and basic navigation. Not yet tested in game.
- **Hydraulic flight controls** on the Mosquito, Louie, Cherokee, Smuggler and Warbird (their servos) and the Harrier (D'thorga Patches' hydraulics part). Not yet tested in game.
- The Smuggler and the Warbird have loadouts of their own; both still have a turbofan for now, as before.

## [0.52.0] - 2026-11-13 - Draft

The tanker's new ports, and steam engines are never stalled by EMP

### Added

- **The bulk tanker's chemfuel fills generators** and other buildings that burn it within reach of its hose, before anything is set down on the ground (needs D'thorga Framework 0.109.0). Not yet tested in game.
- **With Rimefeller, crude oil and chemfuel pump through Rimefeller's own pipeline** beneath the parked tanker (needs D'thorga Framework 0.109.0). Not yet tested in game.

### Changed

- **Trains of the Rim's steam locomotives are never stalled by EMP**: nothing electronic keeps a steam engine running. Their electrics are still hit. Not yet tested in game.

### Compatibility and limits

- One unit of crude in the tanker counts as one unit in Rimefeller's tanks, our choice: Rimefeller's code gives no rate between its oil barrels and its stored crude.

## [0.51.1] - 2026-11-13 - Draft

Vehicle sounds sit in the game's mix

### Fixed

- **Our vehicles were too quiet** (owner, 13 November 2026, on reference speakers against the rest of the game). Engines are 4 dB louder (starting, idling, labouring, stopping, running out of fuel), so an engine now plays above the game's own chemfuel generator, and doors 3 dB louder, a little above the game's doors. Every vehicle sound now carries as far as the game's own sounds and other authors' vehicles do, so it is heard at an ordinary zoom: before, an engine faded to nothing a little way out from the closest zoom, and a door was heard only right up close. Consoles and other small machinery carry a little less far. The generator truck's running set stays at the game's own generator level, as a parked generator. Not yet tested in game.

### Compatibility and limits

- The sound files are the ones the owner picked, at the same level; only how loudly and how far the game plays them changed. D'thorga Framework's D'thorga sounds volume still scales every one.

## [0.51.0] - 2026-11-13 - Draft

The Busbar generator set is a 3 by 8 truck, heavier, with a bigger generator

### Changed

- **The Busbar is 3 by 8 cells, drawn 4 by 9, like the Talus tipper** (owner, 13 November 2026: it "feels really small in size and needs to be brought inline with the other vehicles"; it was 2 by 6, the only two-cell-wide truck of ours). Its picture and every overlay (the fans, the set-up outriggers and cable, the uprated alternator, cable reel, battery box, heat exchanger and both damage pictures) were exported again from the same originals, scaled down, never up. Its parts sit in the new cells, and the conduits it lays beneath itself when set up follow its larger footprint. Not yet tested in game.

### Balance

- **Heavier:** 8,000 kg empty (was 6,000), with a 130 kW engine (was 100), about the same power per tonne. Vehicle Framework mass 347, cargo 40, fuel tank 200, 17 chemfuel a day driving, 230 hit points, 450 steel in its shell and 15,000 work to build. All authored.
- **A bigger generator: 2,000 W at full health** (was 1,200), 2 chemfuel a day doing nothing and 15 a day flat out (was 1.5 and 9), the same 60 % of the game's chemfuel generator's economy as before (1,000 W for 4.5 a day). The uprated alternator adds 650 W (was 400). It stops under 400 W of damage (was 240). Authored.
- **Its fittings, tunes and upgrades cost a little more**, because they are priced by its weight: about 7.5 % more work and 15.5 % more materials. The uprated alternator now asks about 16,900 work and 158 steel at the Standard presets.

### Save compatibility

- **Park any Busbar in open ground before updating.** A Busbar in a save grows where it stands by a cell in width and two in length, and Vehicle Framework claims the larger footprint without checking what is there, so one parked against a wall or a building ends up overlapping it and may be stuck. Its parts, upgrades and fittings carry over unchanged. A Busbar set up in the save keeps its cable end or conduits; packing it up and setting it up again lays them for its new size.

### Compatibility and limits

- **Tested in game by the owner, 13 November 2026:** the Busbar's new size and look ("looks much better now"). Its output, the conduits and an older save's Busbar are not yet tested in game.


## [0.50.0] - 2026-11-13 - Draft

The thrust pods take D'thorga Framework's flight-controls system

### Balance

- **Every upgrade on our seven vehicles costs far more time and materials**, priced like D'thorga Framework's fittings and tunes (owner, 13 November 2026): the Ullage tanker, the drill rig, the Pintle trailer, the Busbar generator set, the Seabed Crawler, the Datum survey truck and the Talus tipper. The figures in their upgrade trees are now the base the Framework prices from. Not yet tested in game.

### Changed

- The Frog, Toad and Goliath carry D'thorga Framework 0.106.0's flight-controls system, as every other aircraft does through the shared aircraft loadout: with their stabilisers or other flight-control parts wrecked below 35 % they cannot take off, and damaged they fly slower. Not yet tested in game.

### Save compatibility

- Safe: the system holds no saved figures of its own.

## [0.49.1] - 2026-11-13 - Draft

Clean backgrounds on the winch, the tipper's grab, the drill rig's mast and the survey truck's dish

### Fixed

- White left between parts is gone from the recovery winch on all four trucks, the tipper's loading grab, the drill rig's folded and tall mast, and the survey truck's long-range dish, in all three facings; the edges of a few other pictures are slightly cleaner. Found by an AI that tells background from white paint, each checked by eye.

### Save compatibility

- Safe to add to a running game: only pictures change.

### Compatibility and limits

- Not yet tested in game. Each picture was checked by eye on dark ground before and after, and the owner approved the set (13 Nov 2026).

## [0.49.0] - 2026-11-12 - Draft

The Pintle trailer's own picture, and the grab, winch and long-range tank show when fitted

### Added

- The Pintle flatbed trailer has its own picture: a sand-grey drawbar flatbed with a dark plank deck and a tandem pair of wheels just behind its middle, in place of the tipper's borrowed body. Its greedy boards upgrade now shows as taller side boards round the deck.
- The tipper's loading grab shows folded behind the cab once the upgrade is fitted.
- On the tanker, survey truck, drill rig and tipper, the recovery winch shows on the front bumper and the auxiliary fuel tank between the wheels while each is fitted (D'thorga Framework 0.105.0). The generator set has no winch mount or tank cradle, so it shows neither.
- All new pictures are AI-generated with OpenAI's image model from our own art and picked by the owner; each passed the check against the game's art. The asset records beside the textures say how each was made.

### Changed

- The trailer's paint colours follow its new picture: sand-grey with an orange stripe, as before, matched to the new body.
- The README no longer says the Pintle flatbed trailer cannot go on a journey: from D'thorga Framework 0.104.0 it travels with the vehicle towing it. Documentation only; nothing changes in this mod.
- The README no longer calls the generator set's, drill rig's and tipper's pictures stand-ins or the Crawler's upgrade pictures still to come, and lists the generator set's heavy-duty fan among its upgrades. Documentation only; nothing changes in play.

### Dependencies

- D'thorga Framework 0.105.0 or later, for the pictures shown by a fitting. With an older Framework the game reports the winch and tank pictures' settings as errors and skips them.

### Save compatibility

- Safe to add to a running game. A trailer already built takes its new picture at once. Nothing new is saved.

### Compatibility and limits

- Not yet tested in game. Where each new picture sits is the agent's first placement, to be adjusted once seen; the winch overlaps the front edge of the bonnet a little, because the bodies are drawn almost to the front of their canvas.

## [0.47.0] - 2026-11-12 - Draft

Our vehicles take the upgrade families of D'thorga Framework 0.103.0

### Compatibility and limits

- Our trucks take the recovery family too: a winch mount appears on each one's Health tab, and with the winch fitted a truck can winch a stuck vehicle in and tow it home.
- With D'thorga Framework 0.103.0, our trucks take the cold-weather pack, all-terrain tyres and work lights from the fittings window's new Families section. The generator set takes no work lamps (it lists its own parts), and the survey truck and the drill rig keep their own work lights upgrade beside them. Not yet tested in game.

## [0.46.0] - 2026-11-12 - Draft

The Louie and the Hind can carry a vehicle on a sling

### Added

- **The Louie (Vanilla Vehicles Expanded - Tier 3) and the Hind (Hind Helicopter Gunship) take a cargo hook** (owner, 12 Nov 2026), as the Cherokee does: fit the cargo hook in Fittings and tunes, and they can lift a vehicle on a sling. Not yet tested in game.
- **The Louie's hook takes 1,814 kg**, the Bell UH-1H Huey's 4,000 lb hook (approximate: secondary sources, Wikipedia's "Cargo hook (helicopter)" and a DCS World UH-1H manual). **The Hind's takes 2,400 kg**, the Mi-24's external sling load (approximate: secondary sources, Warrior Lodge's and Military Machine's Mi-24 pages; the hook's own rating was not found). Both are less on hot days, as the Cherokee's is.
- **Both take the litter kit**: on the Louie in its upholstery, on the Hind in its fuselage, which holds its troop cabin (the same Mi-24 sources give it 4 stretchers, as the kit's four litters).

### Compatibility and limits

- The Hind flies across the map by itself, so it needs D'thorga Framework 0.102.0 for its load to hang under it and come down only once it has landed. It cannot fly water drop runs.
- The Mosquito and the Mongoose have no hook: the owner chose the Louie and the Hind.

### Changed

- The Cherokee's loadout no longer says every other helicopter has a sling: only those with a hook mount and a rating in their own loadout do.

## [0.45.0] - 2026-11-12 - Draft

The first trailer. **Not yet tested in game.**

### Added

- **The Pintle flatbed trailer** (owner, 11 Nov 2026: one medium flatbed first): built at the motor pool after vehicle parts research. 2,000 kg empty, 3,000 kg of cargo loaded through its cargo tab while it stands unhitched, and one upgrade, greedy boards (600 kg more). Any land vehicle with D'thorga Framework's hitch can tow it (Framework 0.101.0). Its parts are a bill like the trucks': four tyres, two springs, a frame and wiring.
- It borrows the Talus tipper's body for its picture (owner, 11 Nov 2026); a picture of its own waits for the owner's go.
- **Service lead**, a new upgrade for the generator set (owner, 12 Nov 2026: a batch 3 signature): a heavy cable that charges or starts one of your vehicles within 6 cells while the set runs, taking 300 W before the camp's grid (D'thorga Framework 0.101.0). Electricity research; 30 steel and 2 components.
- **Loading grab**, a new upgrade for the Talus tipper truck (owner, 12 Nov 2026): a small crane behind the cab that lifts loose chunks and stacks within 4 cells into the cargo, up to 500 kg a lift, with only the driver (D'thorga Framework 0.101.0). Machining research; 120 steel and 4 components. No picture on the body yet.
- **Litter kit** for the Cherokee (owner, 12 Nov 2026: a batch 3 signature): its upholstery is now the cabin a litter kit of D'thorga Framework is fitted to, beside Vanilla Vehicles Expanded's own upgrades, never inside them. With a cargo hook it can also carry the Framework's **fire bucket** for water drops.

### Balance

- Figures authored and held for the owner's review: the service lead's 6 cells and 300 W, the grab's 4 cells, 20 seconds a lift and 500 kg, both upgrades' costs, and the trailer's weights, its lengths (axle 3.2 cells behind the hitch) and its 70-degree jack-knife limit.
- The litter kit's 4 litters, its fifth of the cargo space and its cost, and the fire bucket's 3,000 litres, 100 litres a cell, 50 cells' reach and cost, are authored and held for the owner's review (D'thorga Framework's rules).

### Save compatibility

- Safe to add mid-game.

## [0.44.0] - 2026-11-12 - Draft

Damage you can see, and the survey truck's boom upgrades on the boom. **Not yet tested in game.**

### Added

- **Damage shows on the generator set, drill rig, tipper, survey truck and Seabed Crawler**: scorches, holes and tears over the cab, the housing, the box, the deck or the hull once that part is below 60% health, as on the bulk tanker. The marks are the tanker's, cropped and fitted to each vehicle.
- **The survey truck's geophone array and resistivity reel show on its boom** and swing out with it when the truck sets up. The reel comes with Dubs Bad Hygiene's groundwater upgrade.

### Compatibility and limits

- Made with generative AI (OpenAI's image model for the geophones and reel, the owner's picks of 12 November 2026); the damage marks reuse the bulk tanker's. Where each picture sits is the agent's placement, to be adjusted once seen in game.

### Save compatibility

- Safe to update mid-game. Nothing new is saved.

### Dependencies

- Needs D'thorga Framework 0.100.0 or later.

## [0.43.0] - 2026-11-12 - Draft

Every vehicle has its own pictures now, chosen by the owner on 12 November 2026, and the generator set gains a heavy-duty fan. **Not yet tested in game.**

### Added

- **Heavy-duty fan** for the generator set: a bigger cooling fan, shown in place of the stock one. A parked engine's radiator gets no air from driving; with this fan it cools as well as on the road, so a set left working hard in hot weather stays cool. 30 steel and 1 component, after Electricity. The figure is authored.

### Changed

- **The drill rig and the tipper have their own pictures**, where both wore the bulk tanker's. The drill rig shows its mast folded in its cradle (a longer one with the tall mast), the rotary head over the hole, outriggers down while set up, and its power pack, deepchem tank, bits and work lights as they are fitted. The tipper shows its plain body, or its rock or bulk body in place of it.
- **The generator set's pictures replace the drawings made in code**: the truck, its outriggers and cable while set up, the cooling fan, the battery, the reel, the uprated alternator, and now the exhaust heat exchanger, which has its own icon in place of the game's heater.
- **The survey truck** shows its outriggers while set up, and its probe, dish, powered array, battery and work lights as they are fitted.
- **The Seabed Crawler** shows its bottoming wheels, listening array and sonar dome as they are fitted, and their icons are new pictures. Pictures for the arm, the radar mast and the seabed drill are ready for when those upgrades are built.
- The generator set can be repainted, as the other trucks can.

### Compatibility and limits

- Made with generative AI (OpenAI's image model), picked by the owner, each passing the check against copying the game's art; the record beside each vehicle's pictures says how. Where each part sits on its vehicle is the agent's placement, to be adjusted once seen in game.
- Still to come: the drill rig's well kit (its picture was refused twice by the image service, so it waits for the owner's own machine), the geophone spikes and resistivity reel, which ride on the survey truck's moving boom, and the damage pictures of the new vehicles.
- The mast, the rotary head, the fan and the tipping body are pictures of their own so they can move later; for now they stand still.

### Save compatibility

- Safe to update mid-game. The new upgrade is a new node; existing vehicles keep their upgrades, and their pictures follow them.

### Dependencies

- Needs D'thorga Framework 0.98.0 or later.

## [0.42.0] - 2026-11-11 - Draft

The generator set's own upgrade, from the owner's decisions of 11 November 2026. **Not yet tested in game.**

### Added

- **An exhaust heat exchanger for the Busbar generator set.** While the set makes power, the room its cable end stands in is warmed, more as more power is drawn, until it reaches 21 C. Outdoors the heat goes to waste. It costs 40 steel and 2 components, needs Electricity, and its back pressure takes 25 W off the output. Its icon is the game's heater until it has a picture of its own; it has no overlay yet.

### Dependencies

- Needs D'thorga Framework 0.97.0 or later.

### Save compatibility

- Safe to update mid-game. A new upgrade node; nothing is removed or renamed.

## [0.41.0] - 2026-11-11 - Draft

Every vehicle built from parts, by role (with D'thorga Framework 0.95.0). Asked for by the owner on 11 November 2026. **Not yet tested in game.**

### Changed

- **Every vehicle is built from parts**: the survey truck, generator set, tipper, drill rig and seabed crawler each list an engine, a fuel tank and the rest of their real layout (tyres by their axles, a suspension per axle, electrics and cooling, an exhaust, a frame casting on the heavy ones, the pump or compressor their job needs). Each is RimThunder's or Vanilla Vehicles Expanded's part where installed and suited to the vehicle's weight, and ours otherwise. Before, they asked only for our three kits.
- **Hull steel raised to sit beside similar trucks**: survey truck 220 to 300, generator set 320 to 340, tanker 280 to 400, tipper 260 to 420, drill rig 360 to 560, seabed crawler 350 to 480. Every figure is authored and held for the owner's review; the design record parts-bills.md lists what each vehicle comes to.
- The tanker also asks for an exhaust.
- **Hoses**: the tanker (two sets), the drill rig and the tipper ask for 20 chemfuel a set, standing for rubber as it does in tyres and drive belts; with Rimefeller installed, 20 synthylene a set.
- **The seabed crawler's first air**: 20 steel of flasks, or 30 oxygen canisters with Vanilla Gravship Expanded installed.
- **Three upgrades take a real part**: the generator set's uprated alternator asks for an alternator, and its battery buffer and the survey truck's large battery for two batteries each, beside less steel and fewer components. With Vanilla Vehicles Expanded installed those are its car alternator and car batteries; with neither parts mod the three cost exactly what they did. The other 28 upgrades are plate, fittings and our own made items, and are unchanged.
- **The bulk tanker's build cost is a parts bill.** It lists an engine, a transmission, a fuel tank, six tyres, three suspensions, an alternator, a battery, wiring and a radiator; each is RimThunder's or Vanilla Vehicles Expanded's part where that mod is installed and suits the tanker's 9 tonnes, and ours otherwise. With both mods the list is as before, but for the engine.
- **The tanker's engine is the class its weight says.** With RimThunder it asked for a heavy engine; at 9 tonnes it now asks for a medium one, as a repair already did. With Vanilla Vehicles Expanded alone it still asks for two engine blocks.
- With neither mod, a vehicle asks for our engine assembly (two on a truck, three on the drill rig), our transmission, one drivetrain kit and one electrical kit beside its hull.
- The tanker's blueprint no longer asks for RimThunder's or Vanilla Vehicles Expanded's own research beside ours: those mods' parts already need that research to be made.
- **Research, the same for every land vehicle**: with Vanilla Vehicles Expanded installed, the drill rig's and survey truck's projects now also ask for its basic vehicles research, as the tipper's, tanker's and generator set's already did. A project already finished stays finished.

### Compatibility and limits

- Finish or cancel any vehicle frame of ours before installing: a frame already being built keeps what was delivered and then asks for the new list. The same goes for one of the three upgrades above if it is under way.
- The four patch folders that swapped the tanker's cost are gone; D'thorga Framework 0.95.0 or later is needed.

## [0.40.1] - 2026-11-11 - Draft

Turbine ratings named in plain words (with D'thorga Framework 0.92.1). **Not yet tested in game.**

### Fixed

- The Harrier's and the real vehicles' turbine ratings carry labels a player can read: maximum continuous, take-off, maximum power, short lift dry and wet. The Systems tab and the inspect text showed the Defs' own names.

## [0.40.0] - 2026-11-11 - Draft

The Cherokee can carry a vehicle on a sling (with D'thorga Framework 0.92.0), and the mod names every vehicle of ours. **Not yet tested in game.**

### Added

- **The Cherokee takes a cargo hook**: its loadout adds a hook mount under the belly and rates its hook at 11,800 kg, the CH-47F Chinook's centre hook (US Army fact sheet, republished by the 127th Wing), the Cherokee being Vanilla Vehicles Expanded's tandem-rotor cargo helicopter. With Yaroslav's Cherokee Sling active it stands down, so the Cherokee keeps that mod's airlift alone.

### Changed

- The mod's description and README now carry the spelling dthorga beside D'thorga, so a search without the apostrophe finds it. The description says it is community-created by House D'thorga.
- The description and README name every vehicle of ours (Talus, Ullage, Kelly, Datum, Busbar and Fathom), and say it is safe to remove only once none of them is left; both still said so of the bulk tanker alone, and the description that the other trucks were still to come.

### Dependencies

- The Cherokee's hook needs D'thorga Framework 0.92.0; without it the loadout's hook rating does nothing.

### Save compatibility

- Safe on an existing save: a Cherokee gains its hook mount when the save loads.

## [0.39.1] - 2026-11-11 - Draft

Our vehicles get names of their own (owner, 11 November 2026: the agent's proposals, with the owner's X numbers for test hulls). **Not yet tested in game.**

### Changed

- Each vehicle takes a real engineering or earth-science word in front of its plain name: the **Talus** tipper truck, **Ullage** bulk tanker, **Kelly** drill rig, **Datum** survey truck, **Busbar** generator set and **Fathom** seabed crawler, and the **X-1** submarine test hull. Each description opens with its designation, such as Talus T1, and the README says what each word means.
- The Crawler's refits read Fathom F1-S, sensor refit, and Fathom F1-L, laboratory refit.
- Upgrades take the trade's own words: the tipper's high sides are **greedy boards**, the drill's diamond bit a **diamond core bit** and its hammer bit an **RC hammer** (reverse circulation), and the generator set's larger generator an **uprated alternator**.

### Save compatibility

- Safe on an existing save: only names and descriptions changed. Vehicles, refits and upgrades already in a save keep working and show the new names.

### Dependencies

- Pairs with D'thorga Framework 0.82.1, which renames the tunes and the tuning rig; either works without the other.

## [0.39.0] - 2026-11-11 - Draft

Our vehicles at work, heard (owner, 11 November 2026: phase 2 of the sound catalogue; the owner took every one of the agent's shortlist picks and accepted mild clipping in two). **Not yet tested in game.**

### Added

- **Tanker**: hoses coupled and uncoupled, the pump running while it loads or unloads, the relief valve venting, a steady hiss while the tank leaks, air rushing through it in a purge.
- **Tipper**: the rams lifting the body, the load sliding out (a clatter of rock from the rock body, a pour from the bulk body, thuds from the standard body) and the tailgate banging shut.
- **Drill rig**: outriggers and mast on setting up and packing up; drilling, with its own sound on the hammer bit and on shore power; a portion down the cuttings chute; winding down when the ground is spent.
- **Survey truck**: the boom swinging out and back, the console while it surveys, the powered array humming, the long-range dish turning, and one soft tone for a find.
- **Generator truck**: outriggers and the cable reel on setting up and packing up.
- **Seabed Crawler and the submarine test hull**: diving, surfacing, settling on the bottom and lifting off; while under, faint hull creaks, the air scrubber (once fitted) and the laboratory at work; a soft tone when the listening array hears a contact.
- D'thorga Framework's D'thorga sounds volume setting turns them all down or off.

### Compatibility and limits

- Made with generative AI (Stable Audio 3 Small SFX, run on the owner's PC), picked by the owner from the agent's shortlist, and levelled against a comparable sound of the game's own; the record beside each sound file says how. Agent choices made at export, for the owner to hear: the survey console uses take 42 in place of 43, which had no steady stretch to loop; the hull's creaks are evened out so their loop holds a steady level; a faint whine near 11 kHz is filtered out of the rig setting up.
- The Crawler surfacing and the generator truck setting up keep a little clipping that every take had; the owner accepted it.
- Not yet heard in game.

### Save compatibility

- Safe on an existing save: only sounds are added.

### Dependencies

- Needs D'thorga Framework 0.82.0 or later, for the work sounds.

## [0.38.0] - 2026-11-11 - Draft

The Seabed Crawler gets its own picture, a sonar set and two hull refits (owner, 11 November 2026). **Not yet tested in game.**

### Added

- **The Seabed Crawler has its own picture** in house colours, a grey-blue hull with a muted orange band on the sail, in all four facings; the repaint tool recolours the hull and the band separately. The submarine test hull wears it too. AI-generated with the OpenAI image API (the owner's pick of four), widened by script to fill the boat's 4 by 9 cells; see the texture record beside it. Agent-authored, held for the owner's review.
- **Sonar set**, after the listening array: its own research (active sonar) and part (sonar transducer). A ping reaches 60 cells. See D'thorga Framework 0.81.0 for what a ping does.
- **Two hull refits, the owner's idea**, each with its own research and part, and each changing the boat's picture. They exclude each other: a Crawler is a hunter or a laboratory.
  - **Sensor refit** (faired sensor sail, after the sonar set): a smooth faired sail; hears 20 cells further and pings 30 further, carries 200 less.
  - **Laboratory refit** (laboratory hull section, after the advanced laboratory): a longer hull with a long panelled deck; a third scientist's seat and 15 per cent of a bench faster research, a little slower.
- New icons for the three upgrades and their parts, drawn in code.

### Balance

- Agent-authored figures, to tune in play: sonar 60 cells; sensor refit +20 listening, +30 sonar, -200 cargo; laboratory refit +0.15 laboratory speed, -0.5 move speed; research 1,500, 2,000 and 2,500; the refit parts at Crafting 8.

### Compatibility and limits

- With Vehicle Framework's custom shaders turned off in its settings, a refitted Crawler shows the base hull after a load; its refit still works.
- The upgrades' pictures on the hull (wheels, listening strips, sonar dome and the rest) are still to come, and wait for the owner's go on the cost.

### Save compatibility

- Safe on an existing save. The sonar transducer is a new part added at the end of the Crawler's list. A Crawler placed by an earlier version gains it when the save loads, new and undamaged, because D'thorga Framework adds any part a design has gained since the save.

### Dependencies

- Needs D'thorga Framework 0.81.0 or later, for the sonar.

## [0.36.0] - 2026-11-11 - Draft

The Seabed Crawler grows to 4 by 9 cells, and its picture is planned (owner, 11 November 2026). **Not yet tested in game.**

### Changed

- **The Seabed Crawler and the submarine test hull are 4 by 9 cells, drawn 5 by 10** (owner: "I want the players to feel the weight and size of it"); they were 3 by 8. Every part keeps its place in the list and takes new cells across the wider hull; the crew role has a second cell. Mass and body size are unchanged.

### Added

- The Crawler's art record and the request for its first picture, docs/development/vehicle-seabed-crawler.md: the real boat from above, the six visible upgrades placed on the canvas before the first picture, the prompt and the costs. No picture is bought until the owner says so, and the Crawler still borrows the bulk tanker's picture.

### Save compatibility

- A Crawler or test hull placed by an earlier version keeps its old 3 by 8 shape in the save; destroy it in dev mode and place a fresh one. Nothing else changes.

### Dependencies

- Needs D'thorga Framework 0.76.0 or later, as before.

## [0.35.0] - 2026-11-11 - Draft

Our vehicles' own voices (owner, 10 and 11 November 2026). **Not yet tested in game.**

### Added

- **Every vehicle sounds like itself**: its own engine start and stop, its own door (two or three takes, so a crew boarding does not repeat one sound), and an engine that follows the drive, an idling loop and a labouring one blended by load and pitched by rpm. The bulk tanker, tipper and drill rig hiss their air brakes as they stop; the trucks splutter when they run out of fuel. The Seabed Crawler powers up and down, hums and churns, and has its own hatch; the submarine test hull shares the Crawler's (owner's choice).
- The generator truck's engine falls quiet while it is set up, where its own running sound plays.

### Changed

- **The bulk tanker no longer borrows an engine sound from Vanilla Vehicles Expanded**; it has its own.

### Compatibility and limits

- All made with generative AI (Stable Audio 3 Small SFX, run on the owner's PC), picked by the owner from the agent's shortlist, levelled against the game's chemfuel generator, metal being set down and its small hiss; the records beside the sound files say how. Four engine loops (the tanker's and generator truck's labouring, the tipper's idling and labouring) carry a little clipping from the model, accepted by the owner for now. Not yet heard in game.

### Dependencies

- Needs D'thorga Framework 0.76.0 or later.

### Save compatibility

- Safe on an existing save: nothing new is saved.

## [0.34.0] - 2026-11-10 - Draft

Sounds from D'thorga Framework 0.75.0 (owner, 10 November 2026). **Not yet tested in game.**

### Added

- **The submarine test hull pings when it is spotted**, as the Seabed Crawler does: it shares the Crawler's sounds (owner, 10 November 2026).
- Every vehicle here gets D'thorga Framework 0.75.0's system sounds: refuelling, cargo, brakes, a failed start, the warning chime and the rest.

### Dependencies

- Needs D'thorga Framework 0.75.0 or later.

### Save compatibility

- Safe on an existing save: nothing new is saved.

## [0.33.0] - 2026-11-10 - Draft

Working trucks run on their hydraulics (owner, 10 November 2026). **Not yet tested in game.**

### Added

- The drill rig, the survey truck and the tipper carry hydraulics, bound to their own pumps, lines and tipping rams: a hit on a line bleeds the fluid, and the work slows, then stops, until a mechanic tops it up. The survey truck gains a set of hydraulic lines beside its pump.

### Dependencies

- Needs D'thorga Framework 0.73.0 or later.

### Save compatibility

- Safe on an existing save: these trucks gain hydraulics with full fluid.

## [0.32.0] - 2026-11-10 - Draft

The Seabed Crawler's listening array (owner, 10 November 2026). **Not yet tested in game.**

### Added

- **Hydrophones research** on the D'thorga: Submarines tab (1,500), after submersible hull.
- **Hydrophone array**, made at the parts bench from 30 steel and 4 components (Crafting 6), filed under vehicle parts.
- **Listening array upgrade** for the Seabed Crawler, the first of a second row of upgrades for its sensors. It needs the research and one hydrophone array. It hears hostile movement within 40 cells at rest, 20 on the move, and things on land at half those distances. It finds other boats under the water by their noise.
- The array sits at the bow, outside the pressure hull. Damage there shortens what it hears.
- With D'thorga Framework 0.72.0, a fire on the Crawler goes out once it is fully under.

### Compatibility and limits

- The upgrade's icon is drawn in code; the Crawler still borrows the bulk tanker's picture. The array and its range are authored for play.

### Dependencies

- Needs D'thorga Framework 0.72.0 or later.

### Save compatibility

- Safe on an existing save. The new hull part is added at the end of the Crawler's parts, so a Crawler already built takes it on at full health.

## [0.31.0] - 2026-11-10 - Draft

The first vehicle sounds (owner, 10 November 2026). **Not yet tested in game.**

### Added

- **The generator truck runs audibly**: set up with its engine running, it sounds like the game's own chemfuel generator, from the truck. It falls quiet when packed up, out of fuel or too damaged to run.
- **A soft ping when watchers spot the Seabed Crawler** under water, at the hull, alongside the message saying it has been seen.
- D'thorga Framework's sounds volume setting scales both.

### Compatibility and limits

- Both sounds were made with generative AI (Stable Audio 3 Small SFX, run on the owner's PC) and picked by the owner, then levelled against the game's chemfuel generator and its long-range mineral scanner; the records beside the sound files say how. Not yet heard in game.

### Dependencies

- Needs D'thorga Framework 0.71.0 or later, for the sound fields and the volume setting.

### Save compatibility

- Safe on an existing save: nothing new is saved. A generator truck already set up starts its sound at its next power check, within a few seconds.

## [0.30.2] - 2026-11-10 - Draft

A figure for D'thorga Framework 0.70.0. **Not yet tested in game.**

### Fixed

- The military trucks, the tanks and the diesel locomotive now say their electrics run at 24 volts, as their loads were written for, so lights and heaters cost their engines the right amount when accessories load the engine.

### Dependencies

- Needs D'thorga Framework 0.70.0 or later for the figure to count; older Framework versions ignore it.

### Save compatibility

- Safe on an existing save: nothing new is saved.

## [0.30.1] - 2026-11-10 - Draft

Storage categories (owner, 10 November 2026). **Not yet tested in game.**

### Changed

- The Seabed Crawler's fittings (the bottoming wheel set, scrubber rack, laboratory fittings and analyser bench) are filed under **vehicle parts** with the rest of the parts.

### Save compatibility

- Safe on an existing save: shelves and stockpiles keep accepting exactly what they did, because a storage filter remembers items, not categories. A new shelf set to the parent category takes these too.

## [0.30.0] - 2026-11-10 - Draft

Research only a laboratory at sea can do, and the advanced laboratory it leads to (owner, 10 November 2026). **Not yet tested in game.**

### Added

- Advanced marine science, the first research that can only be done aboard: scientists seated in the Seabed Crawler's wet laboratory work on it, while the benches at home carry on with the colony's project. It needs marine science and the multi-analyzer. Not yet tested in game.
- The advanced laboratory, an upgrade after the wet laboratory, made from one analyser bench (a new part from the vehicle parts bench, Crafting 8). It takes on any project a hi-tech research bench can, and its scientists work at a hi-tech bench's speed. Not yet tested in game.

### Balance

- The advanced laboratory's speed matches the game's hi-tech research bench (1.0, without a multi-analyzer's bonus). The research cost (2,500) and the part's cost are authored and untuned.

### Save compatibility

- Safe on an existing save: a new project, a new part and a new upgrade.

## [0.29.0] - 2026-11-10 - Draft

The Seabed Crawler becomes a research vessel (owner, 10 November 2026). **Not yet tested in game.**

### Added

- The wet laboratory, an upgrade for the Seabed Crawler with its own research (marine science) and its own crafted part (laboratory fittings, made at the vehicle parts bench). It adds two scientist's seats. Scientists with an Intellectual skill of 7 or more carry on the colony's research from them, at three quarters of a simple research bench's speed at rest and half that under way, on any project that needs no particular bench. Not yet tested in game.
- A Workshop preview: working trucks on a ridge at dusk, with the name added by script, from the owner's picks of 10 November 2026. No change in play. Its line, "Working trucks that need looking after", is agent-written, for the owner to change.

### Fixed

- The Seabed Crawler's upgrades are spaced so that their labels do not collide or clip at the window's edge. Not yet tested in game.

### Balance

- The laboratory's speed matches the game's simple research bench (0.75); the research cost, the part's cost and the half rate under way are authored and untuned.

### Save compatibility

- Safe on an existing save. A Crawler built before this gains an empty laboratory space, which does nothing until the upgrade is fitted. Removing the upgrade puts anyone aboard ashore first, as Vehicle Framework does whenever a role is removed.

## [0.28.1] - 2026-11-10 - Draft

From the owner's test of the research tree. **Not yet tested in game.**

### Fixed

- The survey truck's and drill rig's upgrade names no longer run into each other or off the edge of the window.

### Save compatibility

- Safe on an existing save: only where the upgrades are drawn has changed.

## [0.28.0] - 2026-11-10 - Draft

The owner's go on the first real submarine (10 November 2026). **Not yet tested in game.**

### Added

- The Seabed Crawler: a 3 by 8 submarine built in shallow water. As built it can only dive, hide and come home. It borrows the bulk tanker's picture until it has its own.
- Two upgrades, each with its own research and crafted part: bottoming wheels, to rest on the bottom, and an air scrubber, for six more hours under and slower stale air.
- A research tab, D'thorga: Submarines, with three projects.

### Balance

- All costs and figures are authored and untuned.

### Save compatibility

- Safe to add. Destroy any Seabed Crawler before removing this mod.

### Compatibility and limits

- The research laboratory, sonar, radar, arm, drill, reactor and oxygen candles are designed, not built.

## [0.27.0] - 2026-11-10 - Draft

The research tree, at the owner's request (docs/development/research-tree.md). **Not yet tested in game.** Every cost is authored, a gameplay choice to tune in play.

### Added

- Each of the five trucks has its own research project on the D'thorga research tab. With other mods active, some also ask for those mods' research.

### Changed

- Truck upgrades now need the research they name, and an upgrade still waiting for it says which project. Pressure vessels now need Fabrication.

### Save compatibility

- Safe on an existing save. The first time a colony loads this version, each truck's project is finished for free if the research it replaces was already done. Trucks already built and upgrades already fitted or under way stay.
- Needs D'thorga Framework 0.61.0 or later. If this mod is removed, the first load logs a one-time error for each of its research projects, and the next save is clean.

## [0.26.0] - 2026-11-10 - Draft

From the owner's answers on submarines (10 November 2026). **Not yet tested in game.**

### Changed

- The submarine test hull can now also dive in chest-deep water, so one hull can try both depths. It is still dev-only and not a finished design.

## [0.25.0] - 2026-11-09 - Draft

From the owner's request for a framework for submarines (9 November 2026). **Not yet tested in game.**

### Added

- A submarine test hull, for trying out the framework's diving. It cannot be built: it appears only through a dev action, borrows the bulk tanker's picture and has no upgrades. It is not a finished design.

### Save compatibility

- Safe to add. Delete any test hull before removing this mod.

## [0.23.2] - 2026-11-09 - Draft

### Changed

- The README says a working vehicle can have its engine started while set up. Not yet tested in game.

### Dependencies

- Needs D'thorga Framework 0.54.0.

## [0.23.1] - 2026-11-09 - Draft

### Changed

- The upgrade icons of the survey truck, drill rig, generator set and tipper have proper pictures, replacing the badges drawn in code. Generated with OpenAI's image API, recorded beside the textures. Not yet tested in game.

## [0.23.0] - 2026-11-09 - Draft

### Added

- The survey truck has its own picture, and its sensor boom swings out to the clearer side when it sets up, over two seconds. It needs room beside it, and says so if there is none. Generated with OpenAI's image API, recorded beside the textures. **Tested in game by the owner, 9 November 2026, on this version: the survey truck and its boom work well.** Its upgrades were not reported on separately.

### Dependencies

- Needs D'thorga Framework 0.51.0.

## [0.22.1] - 2026-11-09 - Draft

### Balance

- The survey truck is now 3 by 7 cells instead of 2 by 7, as wide as the Humvee and the Big Rig (owner's decision, before its art is made). If you have already built one, its parts keep their health. Not yet tested in game.

## [0.22.0] - 2026-11-09 - Draft

### Added

- Overdrive on the survey truck and the drill rig: a toggle that doubles the pace while the engine runs flat out, fuel goes 2.5 times as fast and the working parts wear, cutting out by itself at half health or an overheating engine. Not yet tested in game.

### Dependencies

- Needs D'thorga Framework 0.50.0.

## [0.21.1] - 2026-11-09 - Draft

### Balance

- The survey truck finds deep ore ten times as fast as the game's scanner: about 7 hours on average for a first find, certain within about 14; the long-range search likewise. Each find on a map still slows the next (owner's decision: an order of magnitude over what a vehicle copies). Not yet tested in game.
- The drill rig drills at ten times a deep drill, the diamond bit at twenty, the hammer bit at eighteen on ore and thirty on stone (owner's decision). The oil well kit is unchanged: it keeps the derrick's own time and steel. Not yet tested in game.

### Fixed

- The hammer bit made the rig slower on stone than on ore, the opposite of its description. Not yet tested in game.

### Dependencies

- Needs D'thorga Framework 0.49.1.

## [0.21.0] - 2026-11-09 - Draft

### Added

- The tipper truck: carries 2,400 kg and tips its whole cargo onto the ground behind it with one button, with upgrades for high sides, a rock body or a bulk body. It looks like the bulk tanker until it has its own picture. Not yet tested in game.

### Dependencies

- Needs D'thorga Framework 0.49.0.

## [0.20.0] - 2026-11-09 - Draft

### Added

- An oil well kit for the drill rig, with Rimefeller: it sinks a well from steel in its cargo, as a derrick would, and leaves the well head for a derrick to pump. A set-up survey truck also counts as the scanner a deep well needs. Not yet tested in game.
- A groundwater sensor for the survey truck, with Dubs Bad Hygiene: while set up and selected it shows the water and marks the richest spot nearby. It never makes water. Not yet tested in game.

### Dependencies

- Needs D'thorga Framework 0.48.0.

## [0.19.0] - 2026-11-09 - Draft

### Added

- A deepchem head for the drill rig, with Vanilla Chemfuel Expanded: it raises deepchem into a 300-unit buffer at a pumpjack's rate at best, for a tanker parked beside it or a pipe laid to it. Deepchem never comes up as items. Not yet tested in game.

### Dependencies

- Needs D'thorga Framework 0.47.0.

## [0.18.0] - 2026-11-09 - Draft

### Added

- The survey truck: finds deep ore under any map with a colonist's research work, slowing with each find on a map, with upgrades for favouring ores, long-range searching, a fast powered mode, a large battery that recharges itself and work lights. It looks like the bulk tanker until it has its own picture. Not yet tested in game.
- The drill rig: drills deposits with the game's own portions at 80% of a deep drill, with upgrades for a diamond bit, a hammer bit, a tall mast, an electric power pack and work lights. Active drilling can bring insects, as with any deep drill. It looks like the bulk tanker until it has its own picture. Not yet tested in game.

### Fixed

- The generator set's engine would have caught fire at every hit. It now has a one in four chance, and only when the engine is nearly gone (about a fifth of its health), the figures Vanilla Vehicles Expanded's engines use. Not yet tested in game.

### Dependencies

- Needs D'thorga Framework 0.46.0.

### Compatibility and limits

- Not built yet: deepchem drilling and the oil well kit on the rig, and the groundwater report and oil prospecting on the survey truck. The rig leaves deepchem in the ground until then.

## [0.17.0] - 2026-11-09 - Draft

### Added

- The generator set: a small diesel-electric truck that drives to where power is wanted, sets up and feeds your grid, with fuel use that follows the load. Upgrades for a larger generator, a cable reel and a battery buffer. Pictures are stand-ins drawn in code. Not yet tested in game.

### Dependencies

- Needs D'thorga Framework 0.44.0.

### Compatibility and limits

- A working vehicle light and running on battery alone with the engine off are not built yet.

### Changed

- The README now describes the bulk order window. Text only.
- The bulk tanker is now built from the motor pool tab of the Architect menu, not the Vehicles tab, so it is easy to find on a long mod list. Needs D'thorga Framework 0.43.0. Not yet tested in game.

### Fixed

- Water cannot be chosen for the bulk tanker when Dubs Bad Hygiene is in its lite mode, where it could never be pumped. **Not yet tested in game.**
- The bulk tanker no longer looks damaged when it is new: its damage marks now appear only while the part is really damaged. Found in the owner's first look. **Not yet tested in game.**

### Balance

- The bulk tanker is now 3 by 10 cells, the size of RimThunder's biggest truck, instead of 2 by 6, which looked tiny beside everything else (owner's first look). A tanker already built keeps its place on the map, so it may overlap what is beside it: move it clear first.

### Changed

- A leaking or burst tank now leaves the right mess: blood from hemogen, slime from nutrient paste, water from water and an oil smear from deepchem, as well as the chemfuel puddles it already left. **Not yet tested in game.**
- A note in the M1A2's loadout file now says where its oil's temperature comes from (D'thorga Framework 0.29.1 fixed it). Text only; nothing in game changes here.
- The mod description and the README's Coming later section now describe what 0.10.0 does (turbines, boilers, real figures, climate control, the electric Lightning) instead of 0.1.0, and name the five utility vehicles planned, each with an upgrade tree, with parts from RimThunder and Vanilla Vehicles Expanded where present (owner's directions, 8 November 2026; design record docs/development/utility-vehicles-design.md). Text only; nothing in game changes.
- Dates in this mod's records that were written a day ahead now read 8 November 2026. Text only; nothing in game changes.

### Compatibility and limits

- BMP-1 IFV (Vehicle Framework), by Turbo, is no longer on the Steam Workshop (Steam showed no public item on 8 November 2026: removed or made private). Its loadout here loads only while that mod is active, so it stays dormant and costs nothing; a player with their own copy still gets it.

## [0.16.0] - 2026-11-09 - Draft

Buying by the tanker-load, air brakes and readable upgrades (owner, 9 November 2026). **Not yet tested in game.** Needs D'thorga Framework 0.38.0.

### Added

- The bulk tanker has air brakes and an air compressor, like the other heavy trucks: after standing for hours it must idle a few minutes before it can drive.
- The bulk tanker can buy fluids in bulk at a settlement, with the tanker in your caravan.

### Changed

- Each upgrade shows its name above its icon, and some have shorter names so that they fit. Their saved keys are unchanged.

## [0.15.0] - 2026-11-09 - Draft

Compartments for the bulk tanker (owner, 9 November 2026). **Not yet tested in game.** Needs D'thorga Framework 0.37.0.

### Added

- Two upgrades divide a liquid tank into two, then three compartments, one to each hatch, so it can carry different fluids on one trip. Pressure vessels stay whole.

### Balance

- Food cannot share a tank with anything else. Water, nutrient paste, hemogen and wort are listed as food by hand, since no item of theirs carries a nutrition value; beer and insect jelly count by their own.

## [0.14.0] - 2026-11-09 - Draft

Our vehicle parts moved to D'thorga Framework (owner, 9 November 2026). **Not yet tested in game.**

### Changed

- The engine assembly, drivetrain kit and electrical kit are now provided by D'thorga Framework 0.36.0 and always exist, not only when neither RimThunder nor Vanilla Vehicles Expanded is active. Their internal names are unchanged.

### Dependencies

- Needs D'thorga Framework 0.36.0.

## [0.13.0] - 2026-11-09 - Draft

Things the game keeps as items that are really fluids (owner, 9 November 2026). **Not yet tested in game.** Needs D'thorga Framework 0.35.0.

### Added

- Beer, wort and insect jelly out of the box.
- FSX and prometheum with Combat Extended. FSX may go off when the tank is hit. Prometheum travels only in a pressure vessel, and any spill burns.
- Toxic wastepacks as sludge with Biotech: they dissolve after 8 days unless frozen, as packs do, and a spill or a dissolved load pollutes the ground.
- Fecal sludge with Dubs Bad Hygiene, pumped from septic tanks or barrels and set down as barrels.
- Napalm and crude oil barrels with Rimefeller.

### Changed

- Pressure vessels now also appear with Combat Extended.

### Balance

- Ten units to one FSX or prometheum, thirty to a wastepack, twenty-five to a sludge barrel. A blast stops at 15 cells. All authored but the sludge barrel, which is Dubs' own figure.

## [0.12.0] - 2026-11-08 - Draft

The first vehicle of our own: the bulk tanker (owner, 8 November 2026). **Not yet tested in game.** Needs D'thorga Framework 0.34.0. Design: docs/development/vehicle-bulk-tanker.md. Art: OpenAI gpt-image-2, picked and accepted by the owner, recorded in the textures folder.

### Added

- The bulk tanker: a 2 by 6 heavy truck whose tank is an upgrade. Three liquid tanks (general, insulated, high-volume), and three pressure vessels (standard, light, heavy-wall) where Vanilla Gravship Expanded gives it a gas to carry, plus a second hose reel.
- It carries chemfuel out of the box, refuelling vehicles and pumping stacks on the ground. Deepchem, chemfuel pipes, water, nutrient paste, hemogen, astrofuel and oxygen each come with the mod that gives them meaning.
- It is built from the parts your mods provide: RimThunder's engine and running gear, Vanilla Vehicles Expanded's electrics and cooling, or three parts of our own, made at the machining table, if you have neither.

### Compatibility and limits

- Not safe to remove from a save once a tanker is built: scrap it first, as with any Vehicle Framework vehicle.
- All health, cost, mass, capacity and pump figures are authored and untuned.
- Not built yet: RimThunder engine upgrades for the tree, the fire monitor, crude oil from Rimefeller, and helixien gas. The overlays are not repainted with the truck.

## [0.11.0] - 2026-11-08 - Draft

The garage, at the owner's request (8 November 2026). **Not yet tested in game.**

### Added

- With Vanilla Expanded Framework: the garage, a staffed outpost on the world map. It costs 200 steel and 10 components, needs Construction 6 among those who stay, and gives three bays per person up to twelve. Every three days it repairs from its stores, 40 points per skill level, a steel for each 10 points and a component for each 400. All of these are authored balance.

### Compatibility and limits

- The world-map icon is a stand-in, the game's caravan picture, until art is picked.

### Save compatibility

- Safe to add to a save. **Before removing it, take every vehicle out of every garage**: a garage and the vehicles in it go with the mod.

## [0.10.0] - 2026-11-08 - Draft

Automatic climate control by era, with D'thorga Framework 0.22.0 (owner, 8 November 2026: by technology and age). **Not yet tested in game.**

### Added

- Climate control on the modern vehicles: the L-ATV, the MaxxPro, the M1A2 SEP, the M10 Booker, the Harrier, the Mongoose and the Goat shunter, and Vanilla Vehicles Expanded's Tier 3 vehicles except the Springbok and the Louie. Older designs keep the heater and air conditioner by hand. An agent reading of each vehicle's era, for the owner to revise.

### Dependencies

- Needs D'thorga Framework 0.22.0 or later.

## [0.9.1] - 2026-11-08 - Draft

With D'thorga Framework 0.21.2. **Not yet tested in game.**

### Fixed

- The steam locomotives' cabs never warmed: the firebox now keeps the cab warm below 5 degrees while the locomotive is in steam, with a switch.

## [0.9.0] - 2026-11-08 - Draft

The Lightning as an electric van, with D'thorga Framework 0.20.0. **Not yet tested in game.** Design: docs/development/vehicle-electric-design.md.

### Added

- With D'thorga Framework's setting The Lightning runs on batteries (off by default, needs a restart), Vanilla Vehicles Expanded - Tier 3's Lightning runs on electricity: about 72 kWh, charged from a powered network with stored energy in about 10 hours, with the same range as before and 500 kg more weight. All figures authored.

### Dependencies

- Needs D'thorga Framework 0.20.0 or later.

### Save compatibility

- Turning the setting off again returns Lightnings to chemfuel on the next start. What a built Lightning's charge becomes then is Vehicle Framework's to decide and was not checked: empty its battery first if in doubt.

## [0.8.0] - 2026-11-08 - Draft

Empty masses for Vanilla Vehicles Expanded, with D'thorga Framework 0.19.0 (owner's choice, 8 November 2026). **Not yet tested in game.** Design: docs/development/vehicle-real-figures-design.md.

### Balance

- 33 Vanilla Vehicles Expanded vehicles weigh what a real vehicle of their class weighs, instead of 12 kg per Vehicle Framework Mass: the Dirtbike 110 kg instead of 360, the Bulldog 45 t instead of 5.4 t, the Trawler 8 t instead of 960 kg. Cargo now weighs on tanks and bikes as it should. All authored, approximate; three follow a real aircraft (Louie as a UH-1H, Cherokee as a CH-47D, Warbird as a P-51D, secondary sources).
- The Dirtbike and the Charley get a motorcycle's engine power (30 and 55 kW), since the light-car rule would have left a real-weight bike with 6.6 kW.

### Dependencies

- Needs D'thorga Framework 0.19.0 or later.

### Compatibility and limits

- The three orbital pods (Frog, Toad, Goliath) keep the general rule: no real vehicle to weigh them by.

### Save compatibility

- Safe to add and remove; the figures live in the vehicle designs, not in saves.

## [0.7.0] - 2026-11-08 - Draft

The cab, with D'thorga Framework 0.16.0. **Not yet tested in game.**

### Added

- Every vehicle named here gets the cab system: a closed cab with a heater and an air conditioner, and the night heater for caravans.
- Vanilla Vehicles Expanded's Dirtbike, Charley and Dinghy are marked open: they shelter nobody.

### Dependencies

- Needs D'thorga Framework 0.16.0 or later.

## [0.6.0] - 2026-11-08 - Draft

The owner's answers of 8 November 2026. **Not yet tested in game.**

### Balance

- The L-ATV's engine is now 370 hp (276 kW), the middle of the published 340 and 400 hp (owner's choice), up from 340.
- Water stops draw a tenth of the steam locomotives' real tender and tank capacities by default: 6,800 litres for the 4-8-4 and 496 for the tank engine. The realistic feed water setting restores 68,000 and 4,955.

### Changed

- The 4-8-4 carries no fusible plug, as US rules never required one, so with the boiler explosions setting on, letting its water run below the crown explodes the boiler. The tank engine keeps its plug.

### Dependencies

- Needs D'thorga Framework 0.14.0 or later.

## [0.5.0] - 2026-11-08 - Draft

Steam boilers: part 3 of the owner's approved proposals of 8 November 2026. **Not yet tested in game.**

### Added

- Trains of the Rim's Frisco 4-8-4 and Tommy 0-6-0T, chemfuel and wood alike, get a boiler: the Frisco with the 4500 class's 68,000 L tender and a big engine's slow start, the tank engine with the LB&SCR E2's 4,955 L of tank water and a quicker one. Their brake pipes are their brakes.

### Dependencies

- Needs D'thorga Framework 0.13.0 or later.

### Save compatibility

- Safe to add to and remove from a running colony: the locomotives gain or drop the boiler as their loadout says.

## [0.4.0] - 2026-11-08 - Draft

Real figures for named vehicles: part 2 of the owner's approved proposals of 8 November 2026, the L-ATV first. **Not yet tested in game.** Design: docs/development/vehicle-real-figures-design.md.

### Added

- GTek's L-ATV gets the real vehicle's engine power, the Allison 2500's six ratios, its weight, tyres and four-wheel drive, and its 740 A alternator. Its power was never published, so it uses the transmission's published ceiling, and says so.
- Real engine power, weights and gearboxes, where published, for the MaxxPro, the Humvee, the Land Cruiser J70, the T-54, the Wiesel, the M113 and M113(M), the M48H, the M10 Booker, the M2A3 Bradley, the four BMP-1s and both M1A2s.

### Changed

- RimThunder's "M1075 HEMTT" is the PLS truck, so it now has the PLS's engine, gearbox, weight and 260 A electrics.
- The Humvee's alternator now cites the Army manual (60 to 400 A at 28 V); it keeps 60 A.

### Compatibility and limits

- Unpublished figures (several gear ratios, some rated speeds, every torque curve) are authored and labelled. Combat weights include crew and ammunition the model adds again (small).

### Save compatibility

- Safe to add to and remove from a running colony: nothing new is saved.

## [0.3.0] - 2026-11-08 - Draft

Power and lift: the owner's approved proposals of 8 November 2026, part 1. **Not yet tested in game.**

### Added

- Vanilla Vehicles Expanded's Frog and Toad and Tier 3's Goliath fly on their own thrusters (three, four and four): each thruster lost takes lift away, and a pod that cannot lift its load will not launch. The Goliath is built heavy, as the mod describes it.
- The Mosquito and the Louie get a turboshaft and their rotor masts as a rotor drive; the Louie is the mod's nod to the Huey, and like the real UH-1H its transmission, not its engine, limits its lift.
- The Hind and the Mongoose gain a main rotor gearbox part and a rotor drive limit.
- The Cherokee's transmissions and rotor masts become its rotor drive, and its lift is checked before it launches.
- Trains of the Rim's Goat DE2 runs as a diesel-electric: no gearbox, traction motors that heat when pulling hard, rail wheels, and air brakes from its brake pipe with a compressor.

### Dependencies

- Needs D'thorga Framework 0.12.0 or later. Vanilla Vehicles Expanded's loadouts load only while it is active.

### Compatibility and limits

- The pods' lift figures are authored for fictional craft. The Louie's drive limit follows the UH-1H's transmission limit and the Mongoose's the A129's gearbox; the Cherokee's, the Hind's and the Mosquito's are authored, as no figure was found.
- The Harrier's engine figures are labelled as the Pegasus 11-21's, the early AV-8B's engine.

### Save compatibility

- Safe to add to and remove from a running colony: these vehicles gain or drop the new systems and an air compressor part as their loadouts say.

## [0.2.0] - 2026-11-08 - Draft

D'thorga Framework 0.11.0's engines, drivetrains and gas turbines, fitted to named vehicles. **Not yet tested in game.**

### Added

- The Harrier gets its Pegasus engine: lift ratings from maximum continuous to the short wet rating, a 227 litre water injection tank, the jump jet's flight phases and a hover check at its real empty mass. Too heavy or too hot to hover onto its pad, it uses a runway by default (the setting is D'thorga Framework's; the gate is D'thorga Patches').
- The Hind and the Mongoose get two turboshafts each, bound to their starboard and port engine parts, and the helicopter's flight phases.
- Vanilla Vehicles Expanded - Tier 3's Cherokee (its tandem-rotor transport) gets two turboshafts, bound to its left and right turbo shaft parts; it flies through Vehicle Framework's own launch, so its phases are taxiing and cruising.
- RimThunder's M1A2 and M1A2 SEP get the gas turbine system and a tracked turbine drivetrain: high fuel use at idle, a hot start on a weak battery.

### Dependencies

- Needs D'thorga Framework 0.11.0 or later. The Harrier, Hind, Mongoose and Cherokee loadouts load only while their mods are active.

### Compatibility and limits

- The turbine temperature limits are authored placeholders, labelled in the files. The M1A2's auxiliary power unit is not built (it needs a patch to RimThunder's upgrade trees).

### Save compatibility

- Safe to add to and remove from a running colony: these vehicles gain or drop the turbine and water systems as their loadout says. Nothing the engine model works out is saved.

## [0.1.0] - 2026-11-08 - Draft

The first version, a stand-in while D'thorga Framework's vehicle systems are proved (owner, 8 November 2026: the framework before
any vehicle of ours). No vehicles of its own yet. **Not yet tested in game.**

### Added

- Vanilla Vehicles Expanded - Tier 3's Big Rig and RimThunder's HEMTT get air brakes: after standing long, they idle a few minutes before they can drive.
- The Humvee runs a 24-volt system from two batteries, with a smaller charging output, as the real HMMWV does.
- RimThunder's M1A2 and M1A2 SEP have gas-turbine engines, so they get no coolant system.
- Trains of the Rim's steam locomotives keep no vehicle systems, since a boiler is not a car engine; its diesel shunter gets the general ones.

### Dependencies

- Needs Vehicle Framework and D'thorga Framework 0.9.0 or later. Each mod named above is optional: its vehicles' loadout loads only while it is active.

### Compatibility and limits

- RimWorld 1.7 only.
- Every vehicle not named here gets D'thorga Framework's general systems.

### Save compatibility

- Safe to add to and remove from a running colony: the named vehicles switch between their own systems and the general ones, and saved vehicles keep or drop the air compressor as their design does.
