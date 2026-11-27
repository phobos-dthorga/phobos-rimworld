# D'thorga Framework

The shared library the D'thorga mods build on. On its own it changes little you will notice: it tells other mods what a colonist
is breathing, makes fires fill roofed rooms with the game's own smoke when Combat Extended's smoke is not running, carries carbon
monoxide from fires, and holds the per-person care choices D'thorga Medical offers. **With Vehicle Framework it gives every
vehicle real systems**: a battery, an engine that runs hot, oil, tyres and brakes that wear, and parts a mechanic services.

One of House D'thorga's mods (also spelt dthorga).

Version 0.340.1, built 27 November 2026 for House D'thorga. **Partly tested in game:** the owner has seen the gas layer and supplied
oxygen working (5 November 2026) and a handful of vehicle features (listed at the end of each [vehicle chapter](../../docs/player-guide/vehicle-systems.md)).
**Everything else, the health layer and most of the vehicle systems included, is not yet tested in game.**

## Read this first (gotchas)

- **Partly tested in game.** The owner has passed the gas layer and supplied oxygen (through D'thorga Medical, 5 November 2026),
  standby power, the settings pages, and about forty vehicle features, among them lights, low air, wet brakes, fittings and tunes,
  glass, fuel use, towing, trips to the dyno bay, the service yard, the fleet board, sling loads and aircraft fuel. The lists,
  with dates and what passed only in part, end [Getting started](../../docs/player-guide/getting-started.md#tested-in-game) and
  each vehicle chapter, from [vehicle systems](../../docs/player-guide/vehicle-systems.md#tested-in-game). **Everything else, the
  health layer and most of the vehicle systems included, is not yet tested in game.**
- **Needs Harmony**, which nearly every mod list already has.
- **Load it before any D'thorga mod, and after The Dead Man's Switch - Expedition, Rimefeller and Vanilla Expanded Framework**
  where you have them. Its About file asks for that order, so RimSort sorts it for you; loaded before one of them, its tie-in with
  that mod is refused with a red error.
- **RimWorld 1.7 only.** The vehicle systems switch on only with **Vehicle Framework**; garages and pipe lines need **Vanilla
  Expanded Framework** as well.
- **The vehicle systems change every Vehicle Framework vehicle you have, from any mod.** To narrow that, set **Which vehicles** in
  **D'thorga Vehicle Systems**' settings **before you load a colony**: it needs a restart. Switching vehicles off later is safe.
- **Settings to decide before a colony**: **Which vehicles** and **Parts for building** (both need a restart); **How engines and
  drivetrains work** (**Drivetrain shown, speeds unchanged** keeps every vehicle at its author's speed); the rule for an aircraft
  too heavy or too hot to lift off; and, for steam locomotives, **Boiler explosions** and **Realistic feed water**. The reasons
  are in [Getting started](../../docs/player-guide/getting-started.md#settings-to-decide-before-a-colony).
- **Safe to add to a running colony.** It saves the carbon monoxide in rooms and the care choices you set; a save without it loads
  with no gas.
- **Before removing it** (after the mods that need it): if vehicles used the systems, load the colony, press **Remove the added
  parts from this colony's vehicles** in **D'thorga Vehicle Systems**' settings and save, or saved vehicles may fail to load.
  **Empty parks, yards, garages, sling wrecks, convoys, depots, litters and extra fuel tanks first**: whatever is in them goes
  with the mod. **Finish or let run out every open contract from a settlement** too: its quest would not load. The whole list is in [Removing a mod](../../docs/player-guide/getting-started.md#removing-a-mod).
- **Build a vehicle parts bench early** (research **Vehicle parts**; Architect > **D'thorga** > **Motor pool**): our parts, kits
  and booster packs are made only there. **Tick the Vehicle parts storage category in a stockpile**, or the bench overshoots "Do
  until you have X".
- **Make a jump-start kit or a booster pack before you need one**, and carry one in every caravan.
- **Keep the engine running in a long fight or a long winch pull**: powered turrets and the recovery winch draw on the battery.
  Heaters left on with the engine off run the battery flat too, and so do a 12-volt fridge or a power outlet without a house
  battery fitted.
- **Drive work starts switched off**: tick it in the work tab, or a truck that calls for a driver waits, and no vehicle is taken
  to the dyno bay by itself.
- **Storage switches**: every storage building has a **"Vehicles may load from here"** switch, **off to start** (only with it on
  can a truck's loading grab lift goods out), and every storage building and stockpile a **"Vehicles may unload here"** switch, on
  to start only for the bulk bunker. A tipper fills storage behind it first and keeps the rest aboard.
- **A truck gathering by itself never takes from storage, nor anything forbidden: forbid what you want left.**
- **With The Dead Man's Switch - Expedition, a defended settlement's radars can shoot at your aircraft on the world map.** Make
  **countermeasure cartridges** at the parts bench and keep them in storage, and keep the avionics repaired.
- **Carbon monoxide ignores gas masks.** Only sealed headgear with its own air (Combat Extended's power and recon helmets) keeps
  it out. It lingers in a shut room for hours after a fire: open a door or a vent.
- **Without Combat Extended, burning rooms fill with the game's smoke**, which spoils aim and sight. With it, its **smoke
  effects** setting decides: on, its smoke is what colonists breathe; off, the game's smoke takes over. Gas does not travel with a
  gravship.
- **With Dubs Bad Hygiene, a coolant top-up and the Harrier's injection water need a plumbing pipe with water** beside the parked
  vehicle. Without it, the top-up takes chemfuel and the Harrier's water is free.
- **Standby power stands down by itself while LightsOut or Turn It On and Off - RePowered is active**: switch them off to use it.
- **Rain soaks open fields is off by default**; with Dubs Bad Hygiene its own **Rain irrigation** option must be on too.
- **The pipes and conduits our things lay beneath them are hidden**: look for them in the plumbing and power overlays.

## What it adds

- **Room gases**: what a colonist is breathing, for other mods; the game's smoke in burning roofed rooms when Combat Extended's
  smoke is not running; carbon monoxide from fires, drifting through open doors and vents. What it does to people is D'thorga
  Medical's ([medical](../../docs/player-guide/medical.md#carbon-monoxide)).
- **The health layer**: who the doctors look after, how urgent each condition is, and a button per care choice on each person, for
  D'thorga Medical ([triage](../../docs/player-guide/medical.md#triage)).
- **Standby power**: idle buildings draw a tenth of their power and lights in empty rooms go dark ([standby
  power](../../docs/player-guide/getting-started.md#standby-power)).
- **The D'thorga tabs**: everything we build is in the Architect's **D'thorga** tab, in sub-tabs (Motor pool, Farming, Medical,
  Radio) as each mod adds them, and our research has its own tab
  ([research](../../docs/player-guide/getting-started.md#research)).
- **A glass chain**, from glass sand to armoured panes, for our vehicles' glass ([the glass
  chain](../../docs/player-guide/vehicle-damage-and-repairs.md#the-glass-chain)).
- **Sounds**: one **D'thorga sounds volume** for every D'thorga mod. The sounds were made with generative AI on the owner's PC and
  picked by the owner ([how](../../docs/development/sound-generation.md)).
- **Help with faults**: the game's log turned back on when it stops itself at 10,000 messages, a line when D'thorga code was part
  of an error, and a message naming a colonist who keeps fetching and putting away the same thing.
- **With Vehicle Framework, vehicle systems** that fit what each vehicle is, by what it runs on and whether it drives, floats or
  flies. The five vehicle chapters are the full guide:
  - [Vehicle systems](../../docs/player-guide/vehicle-systems.md): the Dashboard, Health and Systems tabs, batteries, engines,
    coolant, oil, tyres, brakes, services, jump-starts, lights, cabs and special engines.
  - [Damage and repairs](../../docs/player-guide/vehicle-damage-and-repairs.md): hits, knocked-out weapons, glass, fire, replacing
    parts, hulls, and other factions' vehicles.
  - [The vehicle workshop](../../docs/player-guide/vehicle-workshop.md): the parts bench, inspection pit, dyno bay and depot bay,
    parts, fittings, tunes, grades and building upgrades.
  - [Vehicles on the road](../../docs/player-guide/vehicles-on-the-road.md): grip, hills, fuel, the world map, depots, towing, the
    winch, trips to the dyno bay, parks and the service yard.
  - [Aircraft and sling loads](../../docs/player-guide/aircraft-and-sling-loads.md): take-off checks, fuel and height, avionics,
    the sling, water drops, the air ambulance and air defence.
  - [Camping](../../docs/player-guide/camping.md) and [submarines](../../docs/player-guide/submarines.md).

## Settings

Under **Options > Mod settings**: **D'thorga Framework**, and with Vehicle Framework **D'thorga Vehicle Systems**. Each has its
pages down the left, as the game's own Options window does. **Type in the search box to find a setting on any page**; tick
**Changed only** to see just what you have changed (each marked with a dot); **Reset this page to defaults** puts one page back. A
setting that needs a restart says so beside its name, and closing the window after changing one lists them (at the main menu it
offers to restart). RimWorld saves only what you change.

### D'thorga Framework

| Page | Setting | What it does | Default |
| --- | --- | --- | --- |
| General | Where gases come from | Automatic: the game's and other mods' gases where they exist, the framework's own where nothing else carries one. Or always the framework's own, or never (mods that need one fall back to a simpler estimate) | Automatic |
| General | Parts for building | Whose parts a build of ours asks for: **Other mods' parts where installed**, or **Always our own parts**. **Needs a restart.** Repairs accept either | Other mods' parts where installed |
| General | Keep machines and mechanoids topped up | Colonists doing hauling work refill machines that hold a store of material and only offer a right-click fill order, such as a drone platform's steel, and a mechanitor refills mechanoids that carry one, as they repair them (named by D'thorga Patches). Off: by hand only | On |
| General | How full to fill them (under the one above) | Its own fill level (as the machine's own fill order), at least half, at least three-quarters, or full. Never less than its own level | Its own fill level |
| General | Call drones home while they can still get there | A drone on a power cell (named by D'thorga Patches: The Dead Man's Switch's) is called home as soon as its power would barely cover the walk back along its real path, with half as much again to spare, so it gets home and gives its steel back instead of dying on the way. Drafted drones are left alone. Off: only the drones' own mod's call, at two hours of power | On |
| General | Carry drone wrecks home | Haulers carry a drone's wreck to its own platform, or the nearest with room that holds the same material, which keeps what the wreck is worth when butchered; the rest is set down beside it. Off: wrecks are hauled like other bodies | On |
| General | Idle buildings use less power | Standby power: a workbench, research bench, television, turret, hydroponics basin, monitor or powered door draws its full power only while in use, and the share below while idle; indoor lights in empty rooms go dark and draw nothing. Checked about once a second of game time. Stands down while LightsOut or Turn It On and Off - RePowered is active | On |
| General | Lights go dark in empty rooms; Animals turn the lights on (under the one above) | An indoor light is lit only while someone awake is in its room; outdoor lights and sun lamps are left alone. With the second, animals count too | On; Off |
| General | Workbenches, research benches and televisions; Turrets, hydroponics and monitors; Powered doors (under the one above) | One switch for each kind. A turret on standby still fires and a door still opens at once | On |
| General | Power on standby (under the one above) | How much of its full power an idle building draws: 0%, 5%, 10%, 20% or 30% | 10% |
| General | Rain soaks open fields | Rain wets the soil crops grow in outdoors within the hour, as it wets the ground under vehicles, and wet soil only ever helps crops. With Dubs Bad Hygiene it fills Dubs' own irrigation (needs its Rain irrigation option on), which Dubs dries; without it, soaked open ground is up to 1.8 times as fertile, falling back as the ground dries. Changes at once | Off |
| General | Notice when a colonist keeps fetching and putting away the same thing | Tells you once, by name, when a colonist fetches the same thing and hauls it away again several times within a couple of hours: usually two orders disagree, such as their sidearms and their loadout. It never stops a job | On |
| General | D'thorga sounds volume | Every D'thorga mod's sounds, from 0% (silent) to 200%; at 100% each matches a comparable game sound. Changes at once | 100% |
| Log and reports | Keep the game's log going after its message limit | Turns the game's log back on when it stops at 10,000 messages, at most 25 times a session, with a line naming the message that filled it | On |
| Log and reports | Say when D'thorga code was part of an error | After an error whose trace shows D'thorga code or a D'thorga patch on its path, writes one line saying so, so a report reaches D'thorga too. Never says D'thorga caused it. Once per kind of error, at most 50 a session | On |
| Log and reports | Write a colony mood file at each save | Writes every colonist's, slave's and prisoner's thoughts to `PhobosSnapshots/ColonyMood.xml` in the RimWorld user folder, for the mood report in D'thorga Companion and phobos-modkit. Changes nothing in play | On |
| Log and reports | Show gases at the mouse (dev mode; shown only there) | A label at the mouse names every gas in that cell's room | Off |
| Log and reports | Write every Def with all patches applied at the next start (dev mode; shown only there) | For mod developers: one file, `PhobosPatchedDefs/PatchedDefs.xml`, then it turns itself off | Off |
| Detailed logs | Verbose logging: everything, and Verbose: one tick per log | Extra detail in the game's log for chasing a fault, for everything or one part at a time; capped so it cannot flood the log | Off |
| Testing | Note features as they are seen working (for testing) | The first time each feature is seen working in a session, writes one line to `PhobosSnapshots/SeenInPlay.jsonl` in the RimWorld user folder and one to the log, for the Tests page in D'thorga Companion. A line says the feature ran without an error, never that it looked right. Changes nothing in play | Off |
| Testing | Say when a listed test happens (dev mode only) | With the notes above and dev mode on, a message in the corner the first time in a session that something on the test list happens. Needs the test rows our install script writes into the installed mod; a Workshop copy has none and says nothing | Off |

### D'thorga Vehicle Systems (with Vehicle Framework)

| Page | Setting | What it does | Default |
| --- | --- | --- | --- |
| Which vehicles | Every vehicle with a fitting design / Only D'thorga vehicles; a tick per vehicle mod and per kind | Which vehicles get systems. **Needs a restart**; turning vehicles off is safe | Every vehicle, every mod and kind |
| Systems | Figures only, no consequences | Everything shows; nothing happens to the vehicle | Off |
| Systems | A tick per system (battery, cooling, oil, driver and the rest) | Switch one system off everywhere | All on |
| Engines and driving | How engines and drivetrains work: Drivetrain off / Drivetrain shown, speeds unchanged / Full drivetrain | Whether engine power and grip slow a vehicle (down to a crawl, never below 15% of its speed) | Full |
| Engines and driving | Fuel use follows engine load | Laden climbs burn more, empty road runs less, idling about half, and whatever the engine drives costs fuel. At an ordinary cruise the author's rate. Off: the author's flat rate, and working vehicles' own fixed rates | On |
| Engines and driving | Accessories load the engine | What the engine drives besides the wheels takes its power: the alternator, the radiator fan when hot, the air conditioner, the air brakes' compressor, a tanker's pump. Less power for driving, a little more heat, and more fuel with load-following fuel | On |
| Engines and driving | Brakes heat and fade on descents | On the world map hills go up and then down; going down, the engine holds a vehicle back (more with an exhaust brake, or the compression brake every diesel of 250 kW or more has) and the brakes take the rest, which heats them; hot brakes fade, so a laden vehicle slows. Off: descents still wear the pads but never heat them | On |
| Engines and driving | Fuel thickens in deep cold | Below about -8 °C chemfuel gels and the engine will not start | Off |
| Engines and driving | Thin air at height | The higher the country, the less power an engine without a turbocharger and a gas turbine have, the more a helicopter needs to hover, and the less a radiator cools. Your own vehicles with the full drivetrain model only. Off: everywhere is sea level | On |
| Engines and driving | Rain, snow and ice take grip | Wet and frozen ground takes grip on hills, in mud and when towing | On |
| Engines and driving | Rough ground wears tyres and suspension | Up to three times the tyre wear off the roads | On |
| Engines and driving | Vehicles you did not send pass over your traps | A vehicle of yours driving without your order (a bay trip, a run, a tow, a caravan setting off) never sets off a trap; enemy vehicles still do | On |
| Engines and driving | Vehicles without the drivetrain model can tow | Lets other vehicles tow on a plain rule | Off |
| Engines and driving | Vehicles that tow take a wider way round | A vehicle towing keeps off walls and fences, and looks for another way where its trailer would not fit | On |
| Engines and driving | Vehicle effects | Off, Light or Full: faults show as smoke in the colour of what burns (blue oil, black fuel, white steam), sparks and drips; Light thins the rest, Off draws nothing | Full |
| Engines and driving | Headlights light the ground | Switched-on headlights light the ground at night; off if many vehicles slow the game | On |
| Engines and driving | Lights switch themselves | Headlights light while driven at night, work lamps while parked at night with people near (on battery only above half charge); off, the buttons switch them by hand | On |
| Engines and driving | Jump-start leads must be crafted | A jump-start needs a jump-start kit | On |
| Engines and driving | Every vehicle carries a booster pack | No kit or pack needed | Off |
| Aircraft and sling loads | Aircraft fuel follows the engines | An aircraft burns its author's rate flying about or cruising in good order; a hover or a climb burns more, a worn engine and what is switched on a little more, and idling on the ground with a pilot aboard burns fuel too. Off: the authors' flat rates | On |
| Aircraft and sling loads | Cruising height between places | Each stretch of a flight between places is flown at a height of its own: over the highest ground on the way, lower where the engines cannot climb at the aircraft's weight or someone aboard has no oxygen. A jet flown low is slower and thirstier; a stretch over ground too high to climb is refused before launch. Your own aircraft with cruising figures only. Off: every flight is flown as its author made it | On |
| Aircraft and sling loads | Damaged turbine engines give less power | A damaged turbine gives from half its power, badly damaged, to all of it, sound, so a shot-up aircraft flies, climbs and hovers weaker before an engine stops | On |
| Aircraft and sling loads | Lopsided thrust unsettles twin jets | A twin-engined jet with one engine out or weaker dodges less fire and burns a little more fuel. Helicopters and the Osprey are not affected | On |
| Aircraft and sling loads | Aircraft engines start one after another | Turbines light one after another, a few seconds each, and the aircraft may not take off until all are running | On |
| Aircraft and sling loads | Turbine compressors get dirty | A turbine's compressor slowly gets dirty as it runs, faster on rough ground, costing up to 5% of its power and about 2% more fuel until a mechanic washes it with water. Jets, helicopters and turbine tanks | On |
| Aircraft and sling loads | When an aircraft is too heavy or too hot to lift off, hover onto its pad or land at a journey's end | Refuse it (use the runway, or lighten it) / allow it, at a cost to the engine / allow it, with a warning only | Refuse it: use the runway, or lighten it |
| Aircraft and sling loads | Blind take-offs: warn only | An aircraft with broken or unpowered instruments may take off at night or in fog, with a warning | Off |
| Aircraft and sling loads | Crews feel thin air when oxygen fails at height | Everyone aboard a high-flying aircraft whose oxygen is shot out is short of breath for a few hours | Off |
| Aircraft and sling loads | Rough landings | An aircraft landing blind, or with its flight controls shot about, may come down hard and damage its landing gear; a working autopilot halves the risk. Never harms the crew | On |
| Aircraft and sling loads | Sights and weather affect an aircraft's aim | A damaged or basic sight misses more; so does every aircraft at night without a night sight, and in rain, fog or snow, with a working radar giving back half | On |
| Aircraft and sling loads | Stock helicopters fly over the map | The Cherokee, the Louie and the Mosquito take off, fly about, hover, circle and land on the map as the Osprey does, and leave and arrive flying; their pictures were drawn for flying straight off the map, so a hover may look a little odd. Off: they fly as their authors made them | Off |
| Aircraft and sling loads | Airdrop landings can hurt and damage | People landing by parachute are hurt at the real rate (about 5 in 1,000 jumps, more at night, in wind, with heavy kit, on first jumps and when dropped low); cargo and vehicles now and then land hard. Off: everything lands unharmed, still carried by the wind | On |
| Aircraft and sling loads | Vehicles may drop with their crew inside | A vehicle dropped by parachute keeps its crew inside, as Soviet airborne troops did in 1973 and 1976; they are hurt far more often than a jumper. High risk. Off: a vehicle with people inside stays in the hold | Off |
| Aircraft and sling loads | Packers' skill decides whether canopies fail | Parachutes and drop rigs carry the quality of the person who packed them, and a badly packed one can fail: a jumper lands roughly on the reserve, cargo or a vehicle lands hard. Off: canopies never fail | Off |
| Aircraft and sling loads | Aircraft may pull vehicles out low over a strip (LAPES) | An aircraft built for it (the C-27J) flies a few feet over a clear strip and an extraction chute pulls a vehicle out of its ramp to slide to a stop: no drift, a harder landing, and now and then the aircraft strikes the ground. Off: the order is not offered | On |
| Aircraft and sling loads | Other authors' aircraft use D'thorga airdrops (the C-17) | The C-17 Globemaster III drops with real parachutes, wind and landings in place of its own airdrop. Any fault while it flies a drop on D'thorga's flight is D'thorga's. Off: it works exactly as its author made it | Off |
| Aircraft and sling loads | The Dead Man's Switch - Expedition's air defence sees aircraft (shown with that mod) | Its radars track aircraft in flight on the world map and may fire; a launch towards a defence asks first | On |
| Aircraft and sling loads | Hot days lift less / A ground crew hooks the load up / Weather can ground a lift / Slung loads fly slower and burn more / Emergency release and drops | Each sling rule on its own | All on |
| Steam and electric | Realistic feed water | Off: a water stop draws a tenth of the real tender | Off |
| Steam and electric | Boiler explosions | A boiler without a fusible plug explodes when run dry | Off |
| Steam and electric | Battery fires | An overheated traction battery catches fire | Off |
| Steam and electric | The Lightning runs on batteries | D'thorga Vehicles' Lightning becomes electric | Off |
| Dangers and raids | Flammable tank loads explode | A destroyed tank of something flammable explodes, more violently the fuller; off, it only spills | On |
| Dangers and raids | Ammunition aboard can cook off in a fire | With Combat Extended's ammunition: a burning vehicle's ammunition can explode, and a tank's turret can be blown off | On |
| Dangers and raids | Other factions' vehicles burn as yours do | A fire on a raider's or a trader's vehicle wears its parts as on yours. Off: only their ammunition cooks off | On |
| Dangers and raids | Fire underneath burns fireproof vehicles' tyres and hoses | A fire beneath a vehicle built never to catch fire (tanks, trains) still burns its tyres or tracks, belts and hoses | On |
| Dangers and raids | Your crews get out of a burning vehicle | Once it has stopped, and never in the air; off, they stay aboard until ordered out | On |
| Dangers and raids | Leaking fuel feeds a vehicle fire | A high risk: a burning vehicle's leaking fuel catches as a pool of fire beneath it, which spreads and reaches people and vehicles near | Off |
| Dangers and raids | Burnt tyres can light again | Tyres smoulder about two game hours after a fire is put out and may catch again, unless it rains | On |
| Dangers and raids | Burning tyres can burst | A high risk: a burning tyre may burst, hurting anyone within about two cells | Off |
| Dangers and raids | Breaking glass cuts the people behind it | When a cab's glass breaks, the people behind it take small cuts, now and then an eye | On |
| Dangers and raids | Vehicles' glass cracks and breaks | A closed cab's glass breaks from hits, blasts, fire and a strike; gone, the cab is open to the weather until re-glazed with panes from the glass chain | On |
| Dangers and raids | Beaten armour throws spall inside | With Combat Extended: a round that gets through a vehicle's rifle-proof or heavier plate breaks the inside loose, which can wound the crew behind it and hole parts inside; a spall liner catches most of it | On |
| Dangers and raids | Also on other mods' vehicles | Spall on other mods' tanks and armoured cars too, whoever owns them; off, only D'thorga vehicles | On |
| Dangers and raids | Heavy hits shake spall loose | A shell bursting against the plate, or a round that nearly got through, knocks one piece loose | On |
| Dangers and raids | Raiders may arrive by aircraft | Raiders of an industrial or better faction may come in aboard one of our aircraft (the Osprey), which puts them down near the colony and leaves; shot down or with its crew lost on the ground it is wrecked, never yours. Off: raids never arrive that way | On |
| Dangers and raids | Enemy vehicles may carry fittings | Other factions' vehicles may come with fittings and a tune, kept if captured | On |
| Repairs, service and fittings | Destroyed parts need replacing | A part at 0% needs supplies and a mechanic before repair | On |
| Repairs, service and fittings | Replacing an engine or transmission costs / Replacing any other part costs | The share of the part's materials a replacement takes | 100% / 50% |
| Repairs, service and fittings | Field fixes | Patch leaks and shorts, and re-fit a thrown track or wrecked wheel, away from a base | On |
| Repairs, service and fittings | Low oil calls for a service | A service falls due once oil is below 60% | On |
| Repairs, service and fittings | A failing turbocharger can damage the engine | A turbo that fails with the engine running takes 15% of the engine's health once | On |
| Repairs, service and fittings | This colony: services take raw materials instead of kits | Saved with the colony. On: services take steel, components and chemfuel instead of kits | Off (on for a colony from before kits) |
| Repairs, service and fittings (dyno bay trips) | Vehicles are taken to a dyno bay for repair by themselves | A land vehicle with work that sends it is driven to a dyno bay, repaired and driven home | On |
| Repairs, service and fittings (dyno bay trips) | What sends a vehicle to a dyno bay | Bay-only work alone; broken, or bay-only work; any repair or service due | Broken, or bay-only work |
| Repairs, service and fittings (dyno bay trips) | A vehicle that cannot drive is towed to the dyno bay | A vehicle with the recovery winch fetches it and tows it into the bay's reach | On |
| Repairs, service and fittings (dyno bay trips) | Light vehicles that cannot drive are pushed to the dyno bay by hand | With no tow vehicle free, colonists who haul push it there, six at most | On |
| Repairs, service and fittings | Tunes, changes of grade and heavy overhauls need a dyno bay | Tunes, grade changes, head machining and turbine overhauls wait for a powered dyno bay in reach | On |
| Repairs, service and fittings | A tank needs upgrades for compartments | Off: every liquid tank has three compartments free | On |
| Repairs, service and fittings | Time to fit and tune / Materials to fit and tune | Quick, shorter, standard or long; light, standard or heavy | Standard / Standard |
| Repairs, service and fittings | Bigger vehicles cost more to fit and tune | Scales fitting costs by weight | On |
| Cabs, camps and caravans | Passengers feel the cab | Moods about a warm or cold cab | On |
| Cabs, camps and caravans | Every closed cab has climate control | Gives climate control to every closed cab, not only modern ones | Off |
| Cabs, camps and caravans | Walk-in cabins | Colonists can walk into a vehicle's cabin (the Osprey's) to wait, rest or tend the wounded, on the ground or in flight | On |
| Cabs, camps and caravans | Hotboxing | A rider whose drug policy allows a smoked drug may light up aboard; in a closed cab the others share the high, and a high driver drives slower | On |
| Cabs, camps and caravans | People use parked vehicles | Colonists go to parked vehicles by themselves: to sit with the radio, for a hotbox session, to warm up or nap in the cab, and to eat and drink from its stores when the map has nothing else. Each vehicle's "Open to the colony" switch keeps one for the road | On |
| Cabs, camps and caravans | Visitors may flatten the battery | On, a visitor's radio plays on until the battery is flat; off, they turn it off at barely enough to start | Off |
| Cabs, camps and caravans | Field kitchens cook in a caravan | Once a night, while a caravan rests, its cooks make simple meals from raw food at a field kitchen it carries, up to twenty a cook | On |
| Cabs, camps and caravans | Trailers can dump waste on the ground | A shower trailer's waste tank has a dump valve that runs its waste onto the ground as Dubs Bad Hygiene's sewage; off, the button is hidden (Dubs Bad Hygiene) | On |
| Cabs, camps and caravans | Night heater at camp | A vehicle whose night heater switch is on keeps its cab and engine warm at a caravan's camp, and for someone asleep in its cab bunk on a map, for fuel | Off |
| Cabs, camps and caravans | Run-down house batteries freeze | A lead-acid house battery left run down in deep cold freezes, with a warning first, and is lost after about half a day; off, it never freezes | On |
| Cabs, camps and caravans | Drive through the night | Vehicle caravans keep going at night while every vehicle has a rested driver, swapping tired drivers for rested passengers; journeys take about half the time. Each caravan also has its own switch | Off |
| Cabs, camps and caravans | Choose how to meet an ambush | A driving convoy that is ambushed may stop and fight, drive through or turn back; each way out is a chance that rises with speed and armour, and a failed one is the ambush. Off: always fight | On |
| Cabs, camps and caravans | Trailers on wrecked gear crawl | A vehicle towing a trailer on a wrecked axle or tyres keeps a quarter of its speed, and the damaged gear wears further | On |
| Cabs, camps and caravans | Insects in test holes | A drill rig's test hole on the road can break into insect tunnels, as the game's deep drills can; on, a test hole brings up 4 portions, off 3 | Off |
| Cabs, camps and caravans | Heavy vehicles bog on soft ground | A vehicle over 12 tonnes laden sinks on swampy or very wet flat tiles and sticks in time; a winch, a tow, waiting or 10 stone chunks free it | On |
| Remote control | Crewless supply runs need radio reach | With the radio framework: a supply truck with nobody aboard must stay in reach of home all the way, or only to set off and at stops | All the way |
| Remote control | A crewless supply truck out of reach | It holds where it is, carries on, or turns back to its last stop | Holds |
| Cabs, camps and caravans | Warn before a parked vehicle is lost | Days of warning; 0 turns it off | 3 days |
| Contracts | How often settlements offer contracts | Settlements hire your vehicles for deliveries and work on site; the figure is beside the game's trade requests, in quarters from never to 300% | 100% |
| Contracts | Enemies may wait at contract sites | The place where a broken-down vehicle or a hurt traveller waits may hold the game's rescue threats, where violent quests are allowed. People can die there | Off |
| Remote control | Vehicles follow a leader, run loops and fly rounds by themselves | A vehicle with the route kit follows a crewed leader (four at most) or runs between two stops, with nobody aboard; an aircraft flies a round of places with its pilot aboard. Off: none does, and one doing so stops | On |
| Remote control | Mechanitors can drive vehicles | With Biotech: a vehicle with the mech control link, linked to a mechanitor, is driven by them through their bandwidth (1, 2 or 3 by size). Off: none is, and one driven so stops | On |
| Remote control | Remote guns fire on their own | A gun that no seat works, on a vehicle with the remote control kit, fires by itself without a station's picture. Off: only while a station has the picture | Off |
| Remote control | Remote control of vehicles by radio | A vehicle with the radio's control kit can be driven from a radio station with nobody aboard, and does what you chose when its link is lost. Off: none is, and one driving stops | On |
| Remote control | Vehicles driven from afar keep to radio cover | A vehicle driven from a station prefers a way its stations reach, going round ground where it would lose its link. Off: it takes the way it would with a driver | On |
| Display, sound and stories | How much to show | Plain, Operator or Engineer | Plain |
| Display, sound and stories | Keep Vehicle Framework's own Health tab | Keeps their list instead of ours | Off |
| Display, sound and stories | Show the fleet button on the bottom bar | The fleet board | On |
| Display, sound and stories | Power points have the larger socket | Looks only: which picture a working truck's power point uses | Off |
| Display, sound and stories | Vehicle system sounds on other mods' vehicles too | The vehicle systems' sounds (a tyre bursting, an engine knocking, the warning chime) play on any vehicle running them. Off: only D'thorga's own vehicles | On |
| Display, sound and stories | Vehicle messages nobody asked for | Every message; Only troubles (breakdowns, fires, damage, losses and threats); None: letters only. Answers to your own orders and letters always show | Every message |
| Display, sound and stories | Letters for troubled journeys | When a vehicle comes back to a map after a journey on which it broke down, was stranded or someone aboard was hurt, a letter says how it went. Every journey goes in the vehicle's logbook either way | On |
| Display, sound and stories | Exhaust in closed rooms | An engine or generator running indoors fills the room with carbon monoxide; in a closed single garage it is dangerous within about an hour and a half. With D'thorga Medical, breathing it poisons people | Off |
| Display, sound and stories | Destroyed vehicles leave a hull | A vehicle destroyed on a map leaves its hull, which gives salvaged vehicle parts worth a quarter of the vehicle | On |
| Display, sound and stories | Lost convoys stay where they stopped | When a convoy's last colonist dies on the road, its vehicles and cargo stay on the world map for a caravan to come back for | On |
| Display, sound and stories | Depots weather | In frost or a very wet climate, a depot's vehicles wear a little each day and its loose fuel cans slowly leak | On |
| Display, sound and stories | Stored fuel goes stale | Fuel kept in a depot more than about three months goes stale, a hundredth a day; fresh fuel freshens it | On |
| Display, sound and stories | Hostile neighbours find depots | A depot within six tiles of a hostile settlement is found and emptied, sooner the closer it is | Off |
| Removing | Remove the added parts from this colony's vehicles | The button to press before removing the mod | |

Per-vehicle figures (battery, alternator, engine heat, empty mass and more) are in **Vehicle Framework's own settings**, each
vehicle's page.

## More

- The player guide: [getting started](../../docs/player-guide/getting-started.md) and the five vehicle chapters, from [vehicle
  systems](../../docs/player-guide/vehicle-systems.md), each with what is tested in game.
- [The changelog](CHANGELOG.md): what changed in each version, with each version's checks.
- For modders: [vehicle systems for modders](../../docs/vehicle-systems-modding.md) (the vehicle systems' Defs, loadouts and
  schema) and [D'thorga Framework for modders](../../docs/framework-modding.md) (gases, oxygen, the health layer, care choices,
  protective-gear lines, parts bills and the shared helpers); the other mods' pages are [mod by
  mod](../../docs/modding/README.md).

Community-created content. Portions of the materials used to create this content/mod are trademarks and/or copyrighted works of
Ludeon Studios Inc. All rights reserved by Ludeon. This content/mod is not official and is not endorsed by Ludeon.
