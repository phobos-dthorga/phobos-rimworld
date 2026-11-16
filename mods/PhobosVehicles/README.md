# D'thorga Vehicles

Vehicles that keep running only while they are looked after. It adds working vehicles of its own, the **Talus** tipper truck,
**Ullage** bulk tanker, **Kelly** drill rig, **Datum** survey truck, **Busbar** generator set and **Fathom** seabed crawler, with
three **Pintle** trailers, and gives the vehicles of other mods you already drive the systems that match what they really are, on
top of the general systems D'thorga Framework gives every vehicle.

One of House D'thorga's mods (also spelt dthorga). **How to play it is in the player guide**:
[working vehicles](../../docs/player-guide/working-vehicles.md) and [submarines](../../docs/player-guide/submarines.md).

Version 0.58.0, built 17 November 2026 for House D'thorga. **Not yet tested in game**, except the survey truck and its boom (owner,
9 November 2026) and the Busbar's size and look (owner, 13 November 2026).

## Read this first (gotchas)

- **Needs Vehicle Framework and D'thorga Framework 0.116.0 or later**, on **RimWorld 1.7 only**. Load it after both; the About
  file asks for that, so RimSort does it for you. Vanilla Expanded Framework is optional: it brings the garages.
- **Safe to add to a running colony.** **Safe to remove only once none of our own vehicles is left**: scrap every tipper, tanker,
  drill rig, survey truck, generator set, seabed crawler and trailer first, or they go with the mod. **Pack up a set-up generator set
  first**, or its cable end is left behind, and **take every vehicle out of every garage**. Other mods' vehicles go back to the
  general systems.
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
Harrier, Hind, Mongoose, Cherokee and M1A2s, real engines and weights on military vehicles, boilers on steam locomotives, cargo hooks
on three helicopters, climate control on modern vehicles. The full list is in
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
