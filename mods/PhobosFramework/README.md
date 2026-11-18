# D'thorga Framework

The shared library the D'thorga mods build on. On its own it changes little you will notice: it tells other mods what a colonist is
breathing, makes fires fill roofed rooms with the game's own smoke when Combat Extended's smoke is not running, carries carbon
monoxide from fires, and holds the per-person care choices D'thorga Medical offers. **With Vehicle Framework it gives every vehicle
real systems**: a battery, an engine that runs hot, oil, tyres and brakes that wear, and parts a mechanic services.

One of House D'thorga's mods (also spelt dthorga).

Version 0.144.0, built 19 November 2026 for House D'thorga. **Partly tested in game:** the owner has seen the gas layer and supplied
oxygen working (5 November 2026) and a handful of vehicle features ([the list](../../docs/player-guide/vehicle-care.md#what-has-been-tested-in-game)).
**Everything else, the health layer and most of the vehicle systems included, is not yet tested in game.**

## Read this first (gotchas)

- **Load it before any D'thorga mod that needs it**, and **after The Dead Man's Switch - Expedition, Rimefeller and Vanilla Expanded
  Framework** where you have them; the About file asks for both, so RimSort does it for you. Loaded before one of them, its tie-in with
  that mod is rejected with a red error. It needs no other mod.
  **RimWorld 1.7 only.** Its vehicle systems switch on only with **Vehicle Framework**; garages and pipe lines need **Vanilla Expanded
  Framework** as well.
- **The vehicle systems change every Vehicle Framework vehicle you have, from any mod.** To narrow that, set **Which vehicles** in
  **D'thorga Framework: vehicle systems** **before you load a colony**: it needs a restart. Switching vehicles off later is safe.
- **Safe to add to a running colony.** It saves the carbon monoxide in rooms and the care choices you set; a save without it loads
  with no gas.
- **Before removing it** (after the mods that depend on it): if vehicles used the systems, load the colony, press **Remove the added
  parts from this colony's vehicles** in the vehicle systems settings, save, then remove the mod, or saved vehicles may fail to load.
  First also **empty every garage**, **recover every sling wreck**, **let a water drop run land and unload every litter** (switch off
  **Take casualties aboard** first, or carriers keep bringing more), and **take
  any extra fuel tank off**: whatever is in them goes with the mod. Jump-start kits and booster packs vanish with an error line each;
  sell or destroy them for a clean log. A line per missing map component on the next load is harmless.
- **Settings to decide before a colony** (the table below has them all): **Which vehicles** and **Parts for building** need a restart;
  **Drivetrain shown, speeds unchanged** keeps every vehicle at its author's speed; the aircraft lift-off rule; and, for steam
  locomotives, **Boiler explosions** and **Realistic feed water**.
- **Build a vehicle parts bench early** (research **Vehicle parts**, Architect, **motor pool** tab): our parts, kits and booster packs
  are made only there. **Tick the Vehicle parts storage category in a stockpile**, or the bench overshoots "Do until you have X".
- **Make a jump-start kit or a booster pack before you need one**, and carry one in every caravan.
- **The headlights switch starts off.** Left on with the engine off, lights and heaters run the battery flat.
- **Keep the engine running in a long fight or a long winch pull**: powered turrets and the recovery winch draw on the battery.
- **Carbon monoxide ignores gas masks.** Only sealed headgear with its own air (Combat Extended's power and recon helmets) keeps it
  out. It lingers in a shut room for hours after a fire: open a door or a vent.
- **Without Combat Extended, burning rooms fill with the game's smoke**, which spoils aim and sight. With it, its **smoke effects**
  setting decides: on, CE's smoke is what colonists breathe; off, the game's smoke takes over. Gas does not travel with a gravship.
- **With Dubs Bad Hygiene, a coolant top-up and the Harrier's injection water need a plumbing pipe with water** beside the parked
  vehicle. Without it, the top-up takes chemfuel and the Harrier's water is free.
- **The pipes and conduits our things lay beneath them are hidden**: look for them in the plumbing and power overlays.
- **Every storage building now has a "Vehicles may load from here" switch, off to start** (0.125.0). Only with it on can a truck's
  loading grab lift goods out of that storage. **A tipper now fills storage behind it first**: whatever a storage building in the tip
  area would take goes in until it is full, and the rest of those things stay aboard rather than spilling beside it.
  **The drill rig and the tanker's item port fill storage beside them too** (0.127.0), and **a loading chute** (D'thorga Vehicles)
  drops what it holds into a vehicle parked across its spout.
- **A truck with a loading grab can gather by itself** (0.143.0; the Talus in D'thorga Vehicles): **Gather** sends it about the map,
  with someone aboard to drive, to lift loose things you chose with **Gather what**, a stockpile's filter. **It never takes from
  storage, nor anything forbidden: forbid what you want left.** **Since 0.144.0 it can unload by itself**: choose a bunker or stockpile
  under **Unload at** and, when full, it backs up there tail first (a bulk bunker from its open end), tips what it gathered and goes back
  out. A lift takes the same kind lying together, about 20 granite chunks. It stops when nothing is left, when enemies come, when its
  unloading place is full, or at any other order. Not yet tested in game.
- **Vehicle accessories** (0.129.0): a **12-volt fridge**, a **house battery** and a **power outlet**, each made as a kit at the
  vehicle parts bench (vehicle electrics research) and fitted to its own new part on every land vehicle. **Without a house battery the
  fridge and outlet run the starting battery down with the engine off**; fit one to keep the engine's battery for starting. Taking an
  accessory off gives its kit back.
- **With The Dead Man's Switch - Expedition, aircraft can be shot at on the world map** (0.119.0): a defended settlement's radars track
  your aircraft and may fire. A hit damages it; it comes down at a crash site only if it can no longer fly. **Make countermeasure
  cartridges at the parts bench and load them into an aircraft's cargo** before sending it near a defended place, and keep its avionics
  repaired: its warning receiver says when it is tracked, and its dispensers fire the cartridges. A launch towards a defence asks you to
  confirm. To keep the old behaviour, untick **The Dead Man's Switch - Expedition's air defence sees aircraft**.
- **The game's log is turned back on when it stops itself** at 10,000 messages, at most 25 times a session. Switch off **Keep the
  game's log going after its message limit** to keep the game's own limit.

## What it does

- **Room gases**: reads the game's and Combat Extended's gases for other mods, adds the game's smoke to burning roofed rooms when CE's
  smoke is not running, and carries carbon monoxide, which drifts through open doors and vents. What it does to colonists is D'thorga
  Medical's: [burns and smoke](../../docs/player-guide/medical.md#burns-and-smoke) and
  [carbon monoxide](../../docs/player-guide/medical.md#carbon-monoxide).
- **The health layer**: who the doctors look after and how urgent each condition is, and a button per care choice on each person
  ([triage](../../docs/player-guide/medical.md#triage)). How D'thorga Medical's parts share the work is in
  [the medical mods together](../../docs/medical-mods.md).
- **A D'thorga research tab** for vehicle parts and workshops; an existing colony gets a project free once if it had already done what
  it replaces ([research](../../docs/player-guide/getting-started.md#research)).
- **Sounds**: one **D'thorga sounds volume** for every D'thorga mod, engines that follow the drive, and the vehicle systems' own
  sounds. All were made with generative AI on the owner's PC and picked by the owner
  ([how](../../docs/development/sound-generation.md)).

### Vehicle systems (with Vehicle Framework)

Every vehicle gets systems that fit what it is, by what it runs on and whether it drives, floats or flies. The player guide's
[looking after vehicles](../../docs/player-guide/vehicle-care.md) is the full guide:

- **Batteries, engines, coolant, oil, tyres, brakes and services**:
  [what wears out and what fixes it](../../docs/player-guide/vehicle-care.md#what-wears-out-and-what-fixes-it),
  [jump-starting](../../docs/player-guide/vehicle-care.md#jump-starting).
- **The Dashboard, Health and Systems tabs**: [the three tabs](../../docs/player-guide/vehicle-care.md#the-three-tabs).
- **Damage, conditions and knocked-out weapons and tracks**: [damage and repairs](../../docs/player-guide/vehicle-care.md#damage-and-repairs).
- **The workshop** (parts bench, inspection pit, dyno bay, depot bay) and parts by role: [the workshop](../../docs/player-guide/vehicle-care.md#the-workshop).
- **Fittings, tunes, grades and families**: [fittings, tunes and grades](../../docs/player-guide/vehicle-care.md#fittings-tunes-and-grades).
- **Cabs, headlights, the driver, weather and rivers**: [cabs and comfort](../../docs/player-guide/vehicle-care.md#cabs-and-comfort),
  [headlights](../../docs/player-guide/vehicle-care.md#headlights),
  [the driver, the weather and rivers](../../docs/player-guide/vehicle-care.md#the-driver-the-weather-and-rivers).
- **Caravans, fuel reach, the fleet button and garages**: [on the world map](../../docs/player-guide/vehicle-care.md#on-the-world-map).
- **Aircraft checks, avionics, sling loads, water drops and the air ambulance**:
  [aircraft and sling loads](../../docs/player-guide/vehicle-care.md#aircraft-and-sling-loads).
- **Gathering runs** (a truck with a loading grab fetching loose things by itself): [the Talus](../../docs/player-guide/working-vehicles.md#talus-tipper-truck).
- **Towing and the recovery winch**: [towing a trailer](../../docs/player-guide/vehicle-care.md#towing-a-trailer),
  [recovering a stuck vehicle](../../docs/player-guide/vehicle-care.md#recovering-a-stuck-vehicle).
- **Other factions' vehicles and claiming them**: [other factions' vehicles](../../docs/player-guide/vehicle-care.md#other-factions-vehicles).
- **Electric vehicles, steam and diesel-electric locomotives, boats and aircraft**: [special engines](../../docs/player-guide/vehicle-care.md#special-engines).
- **Diving boats, listening arrays, sonar and laboratories aboard**: [submarines](../../docs/player-guide/submarines.md).

## Settings

Under **Options > Mod settings**. RimWorld saves only what you change.

### D'thorga Framework

| Setting | What it does | Default |
| --- | --- | --- |
| Where gases come from | Automatic: the game's and other mods' gases where they exist, the framework's own where nothing else carries one. Or always the framework's own, or never (mods that need one fall back to a simpler estimate) | Automatic |
| D'thorga sounds volume | Every D'thorga mod's sounds, from 0% (silent) to 200%; at 100% each matches a comparable game sound. Changes at once | 100% |
| Vehicle system sounds on other mods' vehicles too | The vehicle systems' sounds (a tyre bursting, an engine knocking, the warning chime) play on any vehicle running them. Off: only D'thorga's own vehicles | On |
| Parts for building | Whose parts a build of ours asks for: **Other mods' parts where installed**, or **Always our own parts**. **Needs a restart.** Repairs accept either | Other mods' parts where installed |
| How full to fill them (under the one above) | Its own fill level (as the machine's own fill order), at least half, at least three-quarters, or full. Never less than its own level | Its own fill level |
| Keep machines and mechanoids topped up | Colonists doing hauling work refill machines that hold a store of material and only offer a right-click fill order, such as a drone platform's steel, and a mechanitor refills mechanoids that carry one, as they repair them (named by D'thorga Patches). Off: by hand only | On |
| Show gases at the mouse (dev mode) | A label at the mouse names every gas in that cell's room | Off |
| Keep the game's log going after its message limit | Turns the game's log back on when it stops at 10,000 messages, at most 25 times a session, with a line naming the message that filled it | On |
| Write a colony mood file at each save | Writes every colonist's, slave's and prisoner's thoughts to `PhobosSnapshots/ColonyMood.xml` in the RimWorld user folder, for the mood report in D'thorga Companion and phobos-modkit. Changes nothing in play | On |
| Write every Def with all patches applied at the next start (dev mode) | For mod developers: one file, `PhobosPatchedDefs/PatchedDefs.xml`, then it turns itself off | Off |
| Verbose logging: everything, and Verbose: one tick per log | Extra detail in the game's log for chasing a fault, for everything or one part at a time; capped so it cannot flood the log | Off |
| Note features as they are seen working (for testing) | The first time each feature is seen working in a session, writes one line to `PhobosSnapshots/SeenInPlay.jsonl` in the RimWorld user folder and one to the log, for the Tests page in D'thorga Companion. A line says the feature ran without an error, never that it looked right. Changes nothing in play | Off |
| Say when a listed test happens (dev mode only) | With the notes above and dev mode on, a message in the corner the first time in a session that something on the test list happens. Needs the test rows our install script writes into the installed mod; a Workshop copy has none and says nothing | Off |

### D'thorga Framework: vehicle systems (with Vehicle Framework)

| Section | Setting | What it does | Default |
| --- | --- | --- | --- |
| Which vehicles | Every vehicle with a fitting design / Only D'thorga vehicles; a tick per vehicle mod and per kind | Which vehicles get systems. **Needs a restart**; turning vehicles off is safe | Every vehicle, every mod and kind |
| Systems | A tick per system (battery, cooling, oil, driver and the rest) | Switch one system off everywhere | All on |
| Systems | Figures only, no consequences | Everything shows; nothing happens to the vehicle | Off |
| Systems | Fuel thickens in deep cold | Below about -8 °C chemfuel gels and the engine will not start | Off |
| Systems | Flammable tank loads explode | A destroyed tank of something flammable explodes, more violently the fuller; off, it only spills | On |
| Systems | A tank needs upgrades for compartments | Off: every liquid tank has three compartments free | On |
| Engines and drivetrains | Drivetrain off / Drivetrain shown, speeds unchanged / Full drivetrain | Whether engine power and grip slow a vehicle (down to a crawl, never below 15% of its speed) | Full |
| Engines and drivetrains | Fuel use follows engine load | Laden climbs burn more, empty road runs less, idling about half, and whatever the engine drives costs fuel. At an ordinary cruise the author's rate. Off: the author's flat rate, and working vehicles' own fixed rates | On (since 0.131.0) |
| Engines and drivetrains | Aircraft fuel follows the engines | An aircraft burns its author's rate flying about or cruising in good order; a hover or a climb burns more, a worn engine and what is switched on a little more, and idling on the ground with a pilot aboard burns fuel too. Off: the authors' flat rates | On (new in 0.132.0) |
| Engines and drivetrains | Thin air at height | The higher the country, the less power an engine without a turbocharger and a gas turbine have, the more a helicopter needs to hover, and the less a radiator cools. Your own vehicles with the full drivetrain model only. Off: everywhere is sea level | On (new in 0.132.0) |
| Engines and drivetrains | Cruising height between places | Each stretch of a flight between places is flown at a height of its own: over the highest ground on the way, lower where the engines cannot climb at the aircraft's weight or someone aboard has no oxygen. A jet flown low is slower and thirstier; a stretch over ground too high to climb is refused before launch. Your own aircraft with cruising figures only. Off: every flight is flown as its author made it | On (new in 0.141.0) |
| Engines and drivetrains | Night heater at camp | A vehicle whose night heater switch is on keeps its cab and engine warm at a caravan's camp, for fuel | Off |
| Engines and drivetrains | Low oil calls for a service | A service falls due once oil is below 60% | On |
| Engines and drivetrains | Accessories load the engine | What the engine drives besides the wheels takes its power: the alternator, the radiator fan when hot, the air conditioner, the air brakes' compressor, a tanker's pump. Less power for driving, a little more heat, and more fuel with load-following fuel | On |
| Engines and drivetrains | Rough ground wears tyres and suspension | Up to three times the tyre wear off the roads | On |
| Engines and drivetrains | Headlights light the ground | Switched-on headlights light the ground at night; off if many vehicles slow the game | On |
| Engines and drivetrains | Rain, snow and ice take grip | Wet and frozen ground takes grip on hills, in mud and when towing | On |
| Engines and drivetrains | Ammunition aboard can cook off in a fire | With Combat Extended's ammunition: a burning vehicle's ammunition can explode, and a tank's turret can be blown off | On |
| Engines and drivetrains | This colony: services take raw materials instead of kits | Saved with the colony. On: services take steel, components and chemfuel instead of kits | Off (on for a colony from before kits) |
| Engines and drivetrains | Passengers feel the cab | Moods about a warm or cold cab | On |
| Engines and drivetrains | Every closed cab has climate control | Gives climate control to every closed cab, not only modern ones | Off |
| Engines and drivetrains | When an aircraft is too heavy or too hot to lift off or hover onto its pad | Refuse it (use the runway, or lighten it) / allow it, at a cost to the engine / allow it, with a warning only | Refuse it |
| Engines and drivetrains | Blind take-offs: warn only | An aircraft with broken or unpowered instruments may take off at night or in fog, with a warning | Off |
| Engines and drivetrains | Crews feel thin air when oxygen fails at height | Everyone aboard a high-flying aircraft whose oxygen is shot out is short of breath for a few hours | Off |
| Engines and drivetrains | The Dead Man's Switch - Expedition's air defence sees aircraft (shown with that mod) | Its radars track aircraft in flight on the world map and may fire; a launch towards a defence asks first | On |
| Steam locomotives | Realistic feed water | Off: a water stop draws a tenth of the real tender | Off |
| Steam locomotives | Boiler explosions | A boiler without a fusible plug explodes when run dry | Off |
| Electric vehicles | Battery fires | An overheated traction battery catches fire | Off |
| Electric vehicles | The Lightning runs on batteries (needs a restart) | D'thorga Vehicles' Lightning becomes electric | Off |
| Repairs | Destroyed parts need replacing | A part at 0% needs supplies and a mechanic before repair | On |
| Repairs | Replacing an engine or transmission costs / Replacing any other part costs | The share of the part's materials a replacement takes | 100% / 50% |
| Repairs | Field fixes | Patch leaks and shorts, and re-fit a thrown track or wrecked wheel, away from a base | On |
| Repairs | Tunes, changes of grade and heavy overhauls need a dyno bay | Tunes, grade changes, head machining and turbine overhauls wait for a powered dyno bay in reach | On |
| Repairs | Time to fit and tune / Materials to fit and tune | Quick, shorter, standard or long; light, standard or heavy | Standard / Standard |
| Repairs | Bigger vehicles cost more to fit and tune | Scales fitting costs by weight | On |
| Repairs | Power points have the larger socket | Looks only: which picture a working truck's power point uses | Off |
| Repairs | Enemy vehicles may carry fittings | Other factions' vehicles may come with fittings and a tune, kept if captured | On |
| Sling loads | Hot days lift less / A ground crew hooks the load up / Weather can ground a lift / Slung loads fly slower and burn more / Emergency release and drops | Each sling rule on its own | All on |
| Towing | Vehicles without the drivetrain model can tow | Lets other vehicles tow on a plain rule | Off |
| Jump-starts | Jump-start leads must be crafted | A jump-start needs a jump-start kit | On |
| Jump-starts | Every vehicle carries a booster pack | No kit or pack needed | Off |
| Your fleet | Show the fleet button on the bottom bar | The fleet board | On |
| Your fleet | Warn before a parked vehicle is lost | Days of warning; 0 turns it off | 3 days |
| What vehicles show | How much to show | Plain, Operator or Engineer | Plain |
| What vehicles show | Keep Vehicle Framework's own Health tab (needs a restart) | Keeps their list instead of ours | Off |
| Removing vehicle systems from a colony | Remove the added parts from this colony's vehicles | The button to press before removing the mod | |

Per-vehicle figures (battery, alternator, engine heat, empty mass and more) are in **Vehicle Framework's own settings**, each vehicle's page.

## For modders

The vehicle systems' Defs, loadouts and schema are in [vehicle systems for modders](../../docs/vehicle-systems-modding.md); gases,
oxygen, the health layer, care choices, protective-gear lines, parts bills and the shared helpers are in
[D'thorga Framework for modders](../../docs/framework-modding.md).

## Checks done

Every version's checks, and what the owner has tested in game, are in [the changelog](CHANGELOG.md); the latest entry is 0.118.0
(17 November 2026), not yet tested in game. The vehicle features the owner has tested are listed in the player guide's
[what has been tested in game](../../docs/player-guide/vehicle-care.md#what-has-been-tested-in-game).
