# D'thorga Vehicles

Vehicles that keep running only while they are looked after. It adds working vehicles of its own, the **Talus** tipper truck,
**Ullage** bulk tanker, **Kelly** drill rig, **Datum** survey truck, **Busbar** generator set and **Fathom** seabed crawler, with
the **Pintle** trailers, the **Billy** field kitchen and the **V-22 Osprey** tiltrotor, and gives the vehicles of other mods you
already drive the systems that match what they really are, on top of the general systems D'thorga Framework gives every vehicle.

One of House D'thorga's mods (also spelt dthorga). **How to play it is in the player guide**: [working
vehicles](../../docs/player-guide/working-vehicles.md) and [submarines](../../docs/player-guide/submarines.md).

Version 0.117.1, built 25 November 2026 for House D'thorga. **Partly tested in game:** the survey truck and its boom (owner,
9 November 2026), the Busbar's size and look (owner, 13 November 2026), the tanker's electric pump, its upgrade tree and its load
counting as weight, the Talus gathering by itself, a truck with nobody aboard calling for a driver, the trucks' upgrade trees, and
the fuel bowser and flatbed sitting on their drawbars. Everything else is not yet tested in game.

## Read this first (gotchas)

- **Partly tested in game.** Besides the survey truck and the Busbar's size and look, the owner has passed the tanker's electric
  pump, its upgrade tree and its load counting as weight, the Talus gathering by itself, a truck with nobody aboard calling for a
  driver, the trucks' upgrade trees, and the fuel bowser and flatbed sitting on their drawbars. The list, with dates and what
  passed only in part, ends the guide's [Working vehicles chapter](../../docs/player-guide/working-vehicles.md#tested-in-game).
  **Everything else is not yet tested in game.**
- **Needs Vehicle Framework and D'thorga Framework 0.169.0 or later.** Load it after both; its About file asks for that order, so
  RimSort sorts it for you. Vanilla Expanded Framework is optional: it brings the garages.
- **RimWorld 1.7 only.**
- **Settings to decide before a colony**, all D'thorga Framework's: **Parts for building** (needs a restart), and **The Lightning
  runs on batteries** (off: it runs on chemfuel; needs a restart). Where RimThunder or Vanilla Vehicles Expanded is installed, our
  vehicles ask for their parts unless you choose our own.
- **Safe to add to a running colony.**
- **Safe to remove only once none of our own vehicles is left.** Scrap every tipper, tanker, drill rig, survey truck, generator
  set, seabed crawler, trailer and Osprey, or they go with the mod. **Pack up a set-up generator set first**, or its cable end is
  left behind; **take every vehicle out of every garage**; and **empty and deconstruct every bulk bunker and loading chute**, or
  their goods are left on the ground. Other mods' vehicles go back to the general systems. Every mod's steps are in [Removing a
  mod](../../docs/player-guide/getting-started.md#removing-a-mod).
- **To set up, someone must be aboard and the engine running**; a vehicle that is set up cannot drive until you press **Pack up**.
  **If its engine stops, the work stops** until someone aboard starts it again.
- **A trailer is set up and packed up by a colonist with construction work**, and **a set-up trailer cannot be hitched**. **The
  water bowser and the shower trailer exist only with Dubs Bad Hygiene** (not in its lite mode), and the paste bowser only with
  Vanilla Nutrient Paste Expanded.
- **Heavy trucks with air brakes must idle a few minutes** after standing for hours before they will drive; the move order says
  so.
- **The five service trucks are armoured like real civilian trucks**, thin sheet metal that stops nothing aimed at it: **rifle
  fire hurts them more without Combat Extended than it used to.** Armour kits help, but **a kit's weight is carried as cargo**, so
  the engine works harder, and on the Talus it takes cargo room.
- **Haulers fill a bulk bunker with chunks lying about the map**, as any stockpile that takes chunks: lower its priority or untick
  chunks to keep room for a truck. **A loading chute starts set to take nothing**: tick what it should load.
- **The Talus never gathers from storage, nor anything forbidden: forbid what you want left.**
- **A laden helicopter or pod may refuse to lift off** on a hot day or with engines out; the launch button says why. A D'thorga
  Framework setting can allow it at a cost to the engine, or only warn.
- **The Fathom seabed crawler is set aside for now**: off the build menu, its research tab shown only in a colony that has already
  researched some of it. A boat already built keeps working.
- **Every upgrade picture's place on its vehicle is a first placement**, to be adjusted once seen in game. The loading chute's
  picture is a stand-in.

## What it adds

How each vehicle works, its buttons, upgrades and armour, are in the player guide's [Working vehicles
chapter](../../docs/player-guide/working-vehicles.md), with the field kitchen and shower trailer in
[Camping](../../docs/player-guide/camping.md).

| Vehicle | Size (cells) | What it does |
| --- | --- | --- |
| [Datum survey truck](../../docs/player-guide/working-vehicles.md#datum-survey-truck) | 3 x 7 | Finds ore anywhere you can drive, ten times quicker than the game's scanner for a first find |
| [Kelly drill rig](../../docs/player-guide/working-vehicles.md#kelly-drill-rig) | 3 x 10 | Drills a found deposit at ten to thirty times a deep drill |
| [Talus tipper truck](../../docs/player-guide/working-vehicles.md#talus-tipper-truck) | 3 x 8 | Hauls 2,400 kg and tips it in one go; with its loading grab it gathers what you choose and delivers it to storage you mark |
| [Ullage bulk tanker](../../docs/player-guide/working-vehicles.md#ullage-bulk-tanker) | 3 x 10 | Carries and pumps fluids and gases, and buys them in bulk at settlements |
| [Busbar generator set](../../docs/player-guide/working-vehicles.md#busbar-generator-set) | 3 x 8 | A 2,000 W power station you can drive away; a hybrid with its battery buffer |
| [Fathom seabed crawler](../../docs/player-guide/submarines.md) | 4 x 9 | A small research submarine; set aside for now |
| [Pintle flatbed trailer](../../docs/player-guide/working-vehicles.md#pintle-flatbed-trailer) | 3 x 7 | 5,000 kg of cargo behind any land vehicle |
| [Pintle water, fuel and paste bowsers](../../docs/player-guide/working-vehicles.md#pintle-water-and-fuel-bowsers) | 3 x 8 | 2,500 litres of water, chemfuel or nutrient paste, left at a camp |
| [Billy field kitchen](../../docs/player-guide/working-vehicles.md#billy-field-kitchen) | 3 x 8 | A kitchen tent at camp; cooks in a resting caravan |
| [Pintle shower trailer](../../docs/player-guide/working-vehicles.md#pintle-shower-trailer) | 3 x 8 | Showers and toilets at camp |
| [V-22 Osprey](../../docs/player-guide/working-vehicles.md#v-22-osprey) | 5 x 9 | A tiltrotor with two pilots, a crew chief and 24 seats: lifts off like a helicopter, crosses the world like an aeroplane |
| [X-1 submarine test hull](../../docs/player-guide/submarines.md#the-x-1-test-hull) | 4 x 9 | Dev mode only, for trying out diving; cannot be built |

- **Each vehicle has its own research on the D'thorga tab**, after **vehicle parts**, and is built from parts in the Architect's
  **motor pool** tab. Its upgrades need the research they name, and cost what D'thorga Framework's fittings and tunes do.
- **The bulk bunker and the loading chute** (motor pool tab, after the tipper's research), for trucks to tip into and load from.
- **A remote control kit for the five service trucks**, with D'thorga Radio Framework: a colonist at a radio station drives
  the truck with nobody aboard ([the guide](../../docs/player-guide/vehicles-on-the-road.md#driven-from-a-station)).
- **Other mods' vehicles get their real counterparts' systems and figures**: air brakes on the Big Rig and HEMTT, gas turbines on
  the aircraft and helicopters, real engines and weights on military vehicles, boilers on steam locomotives, cargo hooks on
  helicopters, climate control on modern vehicles. The full list is in [the
  guide](../../docs/player-guide/working-vehicles.md#other-mods-vehicles); every figure's source is written beside it in the
  loadout files under `Mods/`.
- **Sounds**: every vehicle here has its own engine, doors and work sounds, under D'thorga Framework's **D'thorga sounds volume**.
  The sounds and pictures were made with generative AI and picked by the owner ([how the sounds were
  made](../../docs/development/sound-generation.md)).

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
| When an aircraft is too heavy or too hot to lift off, hover onto its pad or land at a journey's end | Refuse it: use the runway, or lighten it | Allow it at a cost to the engine, or warn only |
| The Lightning runs on batteries | Off | The Lightning charges from the grid instead of taking chemfuel. Needs a restart |
| D'thorga sounds volume | 100% | Every sound here, down to silent |

Every other vehicle setting is in [D'thorga Framework's README](../PhobosFramework/README.md#settings).

## More

- The player guide: [working vehicles](../../docs/player-guide/working-vehicles.md), [camping](../../docs/player-guide/camping.md)
  and [submarines](../../docs/player-guide/submarines.md), each with what is tested in game.
- [The changelog](CHANGELOG.md): what changed in each version, with each version's checks.
- [For modders](../../docs/modding/vehicles.md): loadouts for your own vehicles, and where the designs are.

Community-created content. Portions of the materials used to create this content/mod are trademarks and/or copyrighted works of
Ludeon Studios Inc. All rights reserved by Ludeon. This content/mod is not official and is not endorsed by Ludeon.
