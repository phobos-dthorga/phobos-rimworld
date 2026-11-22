# D'thorga Vehicles

Vehicles that keep running only while they are looked after. It adds working vehicles of its own, the **Talus** tipper truck,
**Ullage** bulk tanker, **Kelly** drill rig, **Datum** survey truck, **Busbar** generator set and **Fathom** seabed crawler, with
three **Pintle** trailers and the **V-22 Osprey** tiltrotor, and gives the vehicles of other mods you already drive the systems that match what they really are, on
top of the general systems D'thorga Framework gives every vehicle.

One of House D'thorga's mods (also spelt dthorga). **How to play it is in the player guide**:
[working vehicles](../../docs/player-guide/working-vehicles.md) and [submarines](../../docs/player-guide/submarines.md).

Version 0.106.3, built 23 November 2026 for House D'thorga. **Not yet tested in game**, except the survey truck and its boom (owner,
9 November 2026) and the Busbar's size and look (owner, 13 November 2026).

## Read this first (gotchas)

- **The Fathom seabed crawler is set aside for now** (0.88.0): off the build menu, its upgrade parts made at no bench, and its
  research tab shown only in a colony that has already researched some of it. A boat already built keeps working.
- **Needs Vehicle Framework and D'thorga Framework 0.169.0 or later**, on **RimWorld 1.7 only**. Load it after both; the About
  file asks for that, so RimSort does it for you. Vanilla Expanded Framework is optional: it brings the garages.
- **Safe to add to a running colony.** **Safe to remove only once none of our own vehicles is left**: scrap every tipper, tanker,
  drill rig, survey truck, generator set, seabed crawler, trailer and Osprey first, or they go with the mod. **Pack up a set-up generator set
  first**, or its cable end is left behind, **take every vehicle out of every garage**, and **empty and deconstruct every bulk
  bunker and loading chute** (their goods are left on the ground otherwise). Other mods' vehicles go back to the general systems.
- **The bulk bunker** (tipper research, motor pool tab; 0.63.0) holds one full tipper load, 96 chunks. **Haulers fill it with chunks
  lying about the map**, as any stockpile that takes chunks: lower its priority or untick chunks to keep room for a truck. Back a tipper
  up to its open end and tip; what does not fit stays aboard. Belts, paste dispensers and The Dead Man's Switch's automatic benches can
  draw from it.
- **The five service trucks are armoured part by part** (0.76.0): every part is skin, thin sheet metal that stops nothing aimed at
  it, as on a real civilian truck. **Rifle fire hurts them more than before without Combat Extended** (0.10 against the old 0.40);
  with it each part is 2 mm of steel where the whole truck read as 0.4 mm, and shots strike them across their full width. The
  Health tab names each part's armour. **Armour kits** (0.80.0), after **truck armour** research: engine and tank guards (rifle-proof,
  300 kg), run-flat tyres (120 kg) and EMP hardening (60 kg). **A kit's weight is carried as cargo**: the engine works harder on every
  truck, and on the Talus it also takes cargo room. **Drawn kits** (0.86.0): an armoured cab, heavy cab plates (after **heavy truck
  armour**), a ram bar, and body plates on the Talus, Ullage and Kelly; all show on the truck. **With its armoured cab the Talus may keep
  gathering while enemies are about** (Gather what, starts on stop). **With Combat Extended, a spall liner** (0.92.0) over the armoured
  cab: four liner panels from the parts bench line it (200 kg), it catches most of the spall a round through the cab throws at the crew
  (D'thorga Framework's spall), wears as it does, and is re-lined only with panels. Not yet tested in game.
- **The Talus gathers by itself once its loading grab is fitted** (0.67.0): **Gather what** chooses what, as for a stockpile (chunks
  and raw resources to start), and **Gather** sends it about the map with someone aboard to drive. **It never takes from storage, nor
  anything forbidden: forbid what you want left.** **Choose a bulk bunker or a stockpile under Unload at** and, each time it is full,
  it backs up there tail first (a bunker from its open end), tips what it gathered, spare parts excepted, and goes back out (0.68.0).
  Each lift takes the same kind lying together, about 20 granite chunks. It stops when nothing is left, when enemies come, when its
  unloading place is full or at any other order. Not yet tested in game.
- **The Talus delivers by itself** (with D'thorga Framework 0.175.0): **Deliver** takes whatever it carries, spare parts excepted, to
  the nearest storage marked **"Vehicles may unload here"** that takes it and has room, and moves on to the next when one is full;
  **Drive back after delivering** (on to start) brings it back to where it started. **The bulk bunker starts marked**; mark any other
  storage or stockpile with its own switch. Gathering can unload at **any marked bay** the same way. Not yet tested in game.
- **The loading chute** (tipper research, motor pool tab; 0.64.0) drops what lands in it into a vehicle parked across its spout.
  **It starts set to take nothing: tick what it should load.** With no vehicle there it holds up to four stacks and the belt behind it
  waits. **Its picture is a stand-in** until generated art is picked.
- **The V-22 Osprey** (tiltrotor aircraft research, after turbine engines; motor pool tab; 0.69.0) is our first aircraft: two pilots,
  a crew chief, 24 seats and 5,600 kg, taking off straight up and crossing the world at 17.5. Its rotors spin when drafted and its nacelles tip
  forward as it climbs away (0.70.0). **Its upgrades** (0.72.0): a second cargo hook, an armour kit, an infrared jammer, a terrain radar and
  an inlet barrier, each with its own research; its stretchers, ferry tanks, cargo hook and cabin oxygen are the usual fittings. **Its
  guns** (0.74.0): a ramp gun, 7.62 mm or .50, worked by the crew chief, and a belly minigun that fires by itself while it has power,
  each with real cartridges under Combat Extended. **Armoured part by part** (0.78.0): skin, with splinter armour on the engines and gearboxes; the armour kit makes the cockpit and cabins
  rifle-proof. **With Combat Extended, a spall liner** (0.94.0) over it: six liner panels, 150 kg, catching the spall a round through
  those plates throws at the crew and passengers. Not yet tested in game.
- **The general systems come from D'thorga Framework**, not this mod: battery and alternator, coolant and oil, tyres or tracks,
  brakes, gearbox, wear and service ([looking after vehicles](../../docs/player-guide/vehicle-care.md)).
- **Our vehicles are built from parts.** Where RimThunder or Vanilla Vehicles Expanded is installed, a build asks for their parts;
  otherwise for ours, made at the **vehicle parts bench**. Decide **Parts for building** in D'thorga Framework's settings before you
  start (it needs a restart).
- **Each vehicle has its own research on the D'thorga tab** (the Crawler on **D'thorga: Submarines**), after **vehicle parts**, and is
  built from the Architect's **motor pool** tab. Its upgrades need the research they name.
- **Upgrades cost what D'thorga Framework's fittings and tunes do**: several hours' work and much more steel and components, more on a
  heavier vehicle, under **Time to fit and tune** and **Materials to fit and tune**. Taking one off refunds a share.
- **To set up, someone must be aboard and the engine running**; a vehicle that is set up cannot drive until you press **Pack up**.
  **If its engine stops, the work stops** until someone aboard starts it again.
- **A trailer is set up and packed up by a colonist with construction work**, and **a set-up trailer cannot be hitched**. **The water
  bowser exists only with Dubs Bad Hygiene**, and not in its lite mode.
- **Heavy trucks with air brakes must idle a few minutes** after standing for hours before they will drive; the move order says so.
- **A laden helicopter or pod may refuse to lift off** on a hot day or with engines out, and a heavy Harrier lands on a runway instead
  of its pad (with D'thorga Patches); the launch button says why. D'thorga Framework's setting **When an aircraft is too heavy or too hot
  to lift off or hover onto its pad** can allow it at a cost to the engine, or only warn.
- **The Lightning runs on chemfuel unless you turn on The Lightning runs on batteries** in D'thorga Framework's settings (needs a
  restart).
- **Every upgrade picture's place on its vehicle is a first placement**, to be adjusted once seen in game.

## The vehicles

| Vehicle | Size (cells) | What it does |
| --- | --- | --- |
| [Datum survey truck](../../docs/player-guide/working-vehicles.md#datum-survey-truck) | 3 x 7 | Finds ore anywhere you can drive, ten times quicker than the game's scanner for a first find |
| [Kelly drill rig](../../docs/player-guide/working-vehicles.md#kelly-drill-rig) | 3 x 10 | Drills a found deposit at ten to thirty times a deep drill |
| [Talus tipper truck](../../docs/player-guide/working-vehicles.md#talus-tipper-truck) | 3 x 8 | Hauls 2,400 kg and tips it in one go; a loading grab picks up loose stacks |
| [Ullage bulk tanker](../../docs/player-guide/working-vehicles.md#ullage-bulk-tanker) | 3 x 10 | Carries and pumps fluids and gases, and buys them in bulk at settlements |
| [Busbar generator set](../../docs/player-guide/working-vehicles.md#busbar-generator-set) | 3 x 8 | A 2,000 W power station you can drive away; a hybrid with its battery buffer |
| [Fathom seabed crawler](../../docs/player-guide/submarines.md) | 4 x 9 | A small research submarine that dives, listens, pings and carries a laboratory |
| [Pintle flatbed trailer](../../docs/player-guide/working-vehicles.md#pintle-flatbed-trailer) | 3 x 7 | 5,000 kg of cargo behind any land vehicle |
| [Pintle water bowser and fuel bowser](../../docs/player-guide/working-vehicles.md#pintle-water-and-fuel-bowsers) | 3 x 8 | 2,500 litres of water or chemfuel, left at a camp |
| [X-1 submarine test hull](../../docs/player-guide/submarines.md#the-x-1-test-hull) | 4 x 9 | Dev mode only, for trying out diving; cannot be built |

**Other mods' vehicles** get their real counterparts' systems and figures: air brakes on the Big Rig and HEMTT, gas turbines on the
Harrier, the A-10 and Su-25, Hind, Mongoose, Cherokee, RimArrow's Ka-27PS and Mi-38 and the M1A2s, real engines and weights on military vehicles, boilers
on steam locomotives, cargo hooks on five helicopters, climate control on modern vehicles. The full list is in
[the guide](../../docs/player-guide/working-vehicles.md#other-mods-vehicles); every figure's source is written beside it in the
loadout files under `Mods/`.

**Sounds and pictures**: every vehicle here has its own engine, doors and work sounds, under D'thorga Framework's **D'thorga sounds
volume** setting. The sounds and pictures were made with generative AI and picked by the owner, with a record of
how each was made beside it ([sounds](../../docs/development/sound-generation.md)).

## Settings

This mod has no settings of its own. Its vehicles use **D'thorga Framework's** vehicle settings, among them:

| Setting (D'thorga Framework) | Starts as | What it does here |
| --- | --- | --- |
| Parts for building | Other mods' parts where installed | Which parts our vehicles ask for. Needs a restart |
| Time to fit and tune / Materials to fit and tune | Standard / Standard | What every upgrade on our vehicles costs |
| Flammable tank loads explode | On | Off, a destroyed tank only spills its load |
| A tank needs upgrades for compartments | On | Off, every liquid tank has its compartments free |
| Fuel thickens in deep cold | Off | On, chemfuel and deepchem pump slowly in deep cold |
| Power points have the larger socket | Off | Changes only the picture of the drill rig's and survey truck's power point |
| Vehicles without the drivetrain model can tow | Off | Lets them pull a Pintle trailer |
| When an aircraft is too heavy or too hot to lift off or hover onto its pad | Refuse it | Allow it at a cost to the engine, or warn only |
| The Lightning runs on batteries | Off | The Lightning charges from the grid instead of taking chemfuel. Needs a restart |
| D'thorga sounds volume | 100% | Every sound here, down to silent |

## For modders

A loadout is an XML file that names a vehicle and the systems and figures it gets. Write your own for any vehicle, in your own mod:
[vehicle systems for modders](../../docs/vehicle-systems-modding.md). The design records start at
[the utility vehicles design](../../docs/development/utility-vehicles-design.md).

## Checks done

Each version's checks, and what it leaves untested, are in [the changelog](CHANGELOG.md). Everything apart from the owner's two tests
above is read from the mods' files and checked offline, and is **not yet tested in game**.
