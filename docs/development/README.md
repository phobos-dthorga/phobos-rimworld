# Development and research

Contributor instructions, research, designs and records. Dated records keep their original scope; they don't
promise current behaviour. For using the mods, start at the [documentation index](../README.md).

## Start here

- [Project direction](project-direction.md): intent, the owner's game, decisions with who made them, and open
  checks.
- [Building, testing and deploying](building.md): every C# project in one table, the checks and what runs on GitHub,
  where things go, the C# toolchain (RimWorld 1.7 leads; 1.6 kept as the minimum) and deploying to the game.
- [Writing READMEs and in-game text](player-language.md)
- [The medical mods together](../medical-mods.md): the player's view of which medical mod does what, with a flow diagram.
- [In-game test pass (9 Nov 2026)](in-game-test-pass.md): the short list of checks for the owner's next session, three that
  matter and four if play brings them up, with what passes for each.
- [Outstanding work (from 7 Nov 2026)](outstanding-work.md): every open item, the owner's decision on it and its status; kept
  current by every change.
- [Mod consolidation (7 Nov 2026)](mod-consolidation.md): why eleven mods became six (five medical mods into D'thorga
  Medical, two patch mods into D'thorga Patches), who decided what, how identities and settings were kept, and what came of it.
- Which mod needs which: the diagram in the [project README](../../README.md#the-mods).

## The radio framework

- [D'thorga Radio Framework: design (snapshot, 18 Oct 2026)](radio-framework-design.md): the owner's design,
  from the living doc on claude.ai, which stays the master copy.
- [Radio reference checks (18 Oct 2026)](radio-reference-checks.md): each figure in the design's Reference
  table, checked by calculation or against its primary source, including the Australian licence limits by mode.
- [Map scale research (19 Oct 2026)](map-scale-research.md): what a cell and a world tile measure, from the
  game's own constants, and why Phase 1 uses 1 m per cell.

## Factory additions

- [VFE Factory additions: research and decisions (28 Oct 2026)](vfe-factory-additions.md): how a factory bill
  works, why the stock neutroamine bill already took human meat, the values in play, decisions and ideas;
  the drug-lab recipe scan (86 recipes, 44 ported in 0.3.0), the output-cap rule and the offline checker;
  why bionics had no factory bills, the fabrication bay's bill generator and the machining bay's lost-item bug;
  the output guard for every machine (0.5.1); the storage switch that lets belts take from any storage
  (0.6.0), with a survey of the owner's storage buildings; and later the stuck-belt fix, the synthread and soup bills, piped
  water with Dubs Bad Hygiene (0.10.0), the per-building belt switch on hoppers (0.11.0) and oversized batches in parts (0.11.1).
- [VFE Factory conveyors and hoppers (28 Oct 2026)](vfe-factory-conveyors.md): what a belt may pull from (only
  factory hoppers, or storage flagged with Pipe System's extension; since 0.6.0 also switched-on storage), where
  it pushes, what the flag does not change, and the recipe for flagging another mod's storage.

## D'thorga Patches (vehicle and missile patches)

- [Vehicle patches: research and decisions (2 Nov 2026)](vehicle-patches.md): why the Harrier's flight framework fails beside
  RimPacts (a Harmony patch in a generic class breaks the next patch of that method), its build cost and bomb costs against
  the owner's other vehicles, bombs and rockets at the ammunition fabricator, heavy castings at the rotary forger, keeping a
  sortie's map open, Spaceship Turret Hull under Combat Extended, 1.7 readiness, and bug reports for the mods' authors.

- [Missile patches: research and decisions (7 Nov 2026)](missile-patches.md): why a strategic warhead from InterRim Ballistic
  Missile hung the owner's game at detonation (one frame for the whole map, and a room rebuild after every fallen wall), how
  D'thorga Missile Patches spreads the blast over frames and holds the rebuild, who chose what, the owner's test protocol, and a
  bug report for the mod's author.
- [Strategic strike: design and decisions (7 Nov 2026)](strategic-strike.md): D'thorga Patches' own model for the extinction-class
  warhead after Dubs Rimatomics' nuke: fireball, blast and heat zones, burns, the pulse, Rimatomics' fallout, who chose what.

## D'thorga Vehicles (vehicle systems and utility vehicles)

- [Vehicle systems: design (8 Nov 2026)](vehicle-systems-design.md): the owner's generic, schema-driven service for real
  vehicle systems (electrical, coolant and oil first) on D'thorga Framework's Vehicle-Framework-optional part, retrofitted to
  other authors' vehicles through loadout Defs anyone can write; the schema shape, the service, settings, the first two
  systems, the roadmap and who chose what; then what was built in D'thorga Framework 0.9.0 (phases A to C, 8 Nov 2026), how it
  differs, and the owner's test list. Not yet tested in game.
- [Vehicle drivetrain and engine speed: design (8 Nov 2026)](vehicle-drivetrain-design.md): rpm and a semi-realistic
  drivetrain worked backwards from the speed Vehicle Framework already gives (gear, load, fuel power, heat), a speed cap only
  where power or traction runs out, the two clocks, archetypes chosen by effect, parts and damage, a worked example and the
  owner's open questions; then what D'thorga Framework 0.11.0 built (9 Nov 2026), the owner's answers and a test list. Not yet
  tested in game.
- [Gas turbines: design (8 Nov 2026)](vehicle-turbine-design.md): turboshaft helicopters, the Harrier's vectored-thrust engine
  with water injection, and the tank turbine; flight phases as the aircraft load signal, the vertical-landing hover margin,
  hot and hung starts from the electrical system, twin engines, and what to confirm in the flight frameworks first; then what was
  built on 9 Nov 2026 (Framework 0.11.0, Vehicles 0.2.0, Patches 0.14.0), what the decompiles said, the owner's answers and a
  test list. Not yet tested in game.
- [Vehicle simulation references (8 Nov 2026)](vehicle-references.md): the study library for the three vehicle designs (books,
  papers, open code and game documentation), each with its terms, what to take from it and whether it was read.
- [Power and lift: design (9 Nov 2026)](vehicle-power-and-lift-design.md): any number of engines, helicopters' rotor drives
  (the Huey's and the Mangusta's transmission limits), thrust-lifted pods losing lift with each thruster, a lift-off check for
  Vehicle Framework's own aircraft, and a diesel-electric with traction motors fitted to a US Army manual's short-time ratings;
  built in D'thorga Framework 0.12.0 and D'thorga Vehicles 0.3.0. Not yet tested in game.
- [Real figures for named vehicles: design (9 Nov 2026)](vehicle-real-figures-design.md): the L-ATV first, then the MaxxPro,
  Humvee, Land Cruiser J70, PLS (Roaring Tiger's "HEMTT"), M113, M48H, M10, Bradley, Abrams, T-54, BMP-1s and Wiesel with
  their engines' published power, weights and gearboxes; D'thorga Vehicles 0.4.0. Not yet tested in game.
- [Steam boilers: design (9 Nov 2026)](vehicle-steam-design.md): raising steam over hours, safety valves, the water glass and
  injector, water stops, and the fusible plug that melts if the crown sheet is uncovered, for Trains of the Rim's Frisco 4-8-4
  and Tommy 0-6-0T; D'thorga Framework 0.13.0 and D'thorga Vehicles 0.5.0. Not yet tested in game.
- [The world map and the cab: design (9 Nov 2026)](vehicle-world-map-design.md): what the vehicle systems did in caravans
  and what was skipped or wrong, a closed cab's own temperature through the game's held-temperature lookup, the night heater,
  caravan speed caps and refusals, the jump-start kit and its two settings; D'thorga Framework 0.16.0 and D'thorga Vehicles
  0.7.0. Not yet tested in game.
- [A shared logging framework: research and direction (9 Nov 2026)](logging-framework-design.md): why every mod's log dies at
  10,000 messages a session (one mod's storm killed the owner's), the 1.7 pipeline from Verse.Log through Waylith to Player.log and
  back, what other mods' loggers do, and the direction: a bounded revive without Harmony, our own log categories with budgets, a
  shared logger in a sibling repository, a schema and reader across games, and no Rust inside the game. Nothing built yet.
- [A pawn-like health model for vehicles: research and direction (9 Nov 2026)](vehicle-health-design.md): what the game does
  for a pawn and Vehicle Framework for a vehicle (it switches the pawn health stack off), why a condition layer on Vehicle
  Framework's parts beats reviving that stack, the performance budget and measurement protocol, the NPC lite tier, and the
  four-slice roadmap from the Health tab to repair depth. Not yet tested in game.
- [Electric vehicles: design (9 Nov 2026)](vehicle-electric-design.md): traction battery temperature and wear, the electric drive,
  charging that follows the battery, and the Lightning as a test van behind a setting; Framework 0.20.0, Vehicles 0.9.0. Not yet
  tested in game.
- [Kerb mass check (9 Nov 2026)](vehicle-kerb-mass-check.md): the 12 kg per Vehicle Framework Mass rule against every active
  vehicle; authored masses for Vanilla Vehicles Expanded, tunable empty mass; Framework 0.19.0, Vehicles 0.8.0. Not yet tested in game.
- [Vehicle follow-ups: design (9 Nov 2026)](vehicle-crew-and-ground-design.md): booster packs with starts, the world map's ground
  from each biome's Defs, cab moods by comfort range, and the RimHUD value; D'thorga Framework 0.18.0. Not yet tested in game.
- [The vehicle tabs: design (9 Nov 2026)](vehicle-tabs-design.md): the Dashboard and Systems inspector tabs, how they hook into
  Vehicle Framework's Def resolution, the saved day of history behind the graphs, the widgets that went to the Framework core,
  and what RimHUD does with them; D'thorga Framework 0.17.0. Not yet tested in game.
- [Vehicle figures: research (9 Nov 2026)](vehicle-figures-research.md): sourced engine, transmission and weight figures for the
  L-ATV, MaxxPro, Humvee, PLS, M113, Bradley, M48H, M10, T-54, BMP-1, Wiesel, Land Cruiser J70, the Abrams' turbine and five
  helicopters, with what was not found.
- [Steam and diesel-electric: research (9 Nov 2026)](steam-and-diesel-electric-research.md): the Frisco 4500 class and small
  tank engines, safety valves, crown sheets and fusible plugs, steam tables, fuels and efficiency, diesel-electric shunters'
  short-time ratings, and engine-out precedents for thrust-lifted craft.
- [Utility vehicles: design (8 Nov 2026)](utility-vehicles-design.md): the new content mod and why it starts as a stand-in,
  the parts model and stats, the upgrade-tree and parts-by-provider principles (9 Nov 2026), the extraction-chain family in full
  (survey truck, drill rig, tipper hauler, generator set, and the bulk tanker that is liquid or gas by upgrade),
  the catalogue of other utility vehicles, the Vehicle Framework 1.7 gate and the art plan; the stand-in mod (0.10.0) is built, no
  vehicle of ours is.
- [Vehicle Framework mechanics: research (8 Nov 2026)](vehicle-framework-mechanics.md): Vehicle Framework's 1.7 status and
  authors, the parts and health system, ticking, events, fuel and electric rules, its per-vehicle settings, how parts and
  comps retrofit onto other mods' designs and what saves hold, the game's temperature and breakdown hooks, Vanilla
  Vehicles Expanded's powertrain data, prior art and art requirements; read from source, not observed in play.
- [Art services for vehicle sprites: research (8 Nov 2026)](vehicle-art-services.md): pay-per-image APIs against
  subscriptions and PixelLab for Vehicle Framework's three-facing bodies and masks, output terms and ownership traps,
  the rotate-then-edit pipeline and the cost of four vehicles; prices as read that day.

## Framework

- [D'thorga Framework: research and decisions (4 Nov 2026)](phobos-framework.md): the owner's rules (existing effects first,
  own gases when they earn it, additive only, a manual override), what already carries a gas in the game and in Combat
  Extended, the room-gas engine copied from the game's temperature design, carbon monoxide as the first own gas, the shared
  helpers, supplied oxygen (0.2.0), the health layer and per-person care choices the medical mods share (0.3.0), and the owner's
  test protocol.

## D'thorga Medical

Five mods until 7 November 2026; each record below keeps its own mod's name and scope.

### Medical save bridge

- [Medical save bridge: research and decisions (3 Nov 2026)](medical-save-bridge.md): why adding More Injuries to a save that
  runs Medical System Expansion 2 wiped wounds and revived corpses (parts are saved by index, and More Injuries appends eight),
  the game's own conversion hooks the bridge sits on, who chose what, and the owner's test protocol.

### Burns and smoke

- [Burns and smoke: research and decisions (4 Nov 2026)](burns-and-smoke.md): what an incendiary raid left in the save (112
  plain burns and nothing else), how the game, Combat Extended and More Injuries handle fire and smoke, the medical sources
  behind the burned fraction, burn shock, airway burns, lung injury and carbon monoxide, fire heat in the open (0.5.0), who chose
  what, and the test protocol.

### Caustic gas

- [Caustic gas: research and decisions (5 Nov 2026)](caustic-gas.md): what the game's tox gas really does (read from the code), the owner's
  slow-acting default and fast-acting setting, three exposures for eyes, lungs and skin, the conditions proposed and reused, who chose
  what, and a test protocol for when it is built. A game design with authored figures; nothing is built.

### Medical equipment

- [Medical equipment: research and decisions (5 Nov 2026)](medical-equipment.md): bedside oxygen on the pattern of the vitals
  monitor and IV stand, the burn unit bed, what is reused from Vanilla Gravship Expanded, why Organ Failure's ventilator cannot
  appear in the owner's game, and the test protocol.
- [Medical equipment, second set: design (7 Nov 2026)](medical-equipment-2.md): the eight pieces the owner chose, from the
  ventilator to the steriliser, what each does in play and what it rests on. All eight built in D'thorga Medical 0.7.0, not yet tested in game.
- [Workstation environmental effects: design (7 Nov 2026)](workstation-environmental-effects.md): the owner's idea of workstations
  giving off gases, found by what they do and switched on a kind at a time in a running colony; nothing built.
- [Vaccines and prevention for healthy pawns: design (7 Nov 2026)](vaccines-and-prevention.md): what already prevents disease
  (penoxycyline, Biological Warfare's vaccines), doctors giving it by themselves, and a head-start vaccine of our own; nothing built.
- [Ventilation and hydrogen: design (7 Nov 2026)](ventilation-and-hydrogen.md): the electrolyser's hydrogen as a flammable room
  gas, and the extractor fan, passive vent, fume hood and hydrogen detector. Nothing built.

### Emergency dosing

- [Emergency dosing: research and decisions (5 Nov 2026)](emergency-dosing.md): why no doctor ever gives a painkiller or
  sedative on their own (More Injuries' drugs are drafted-only right-click options, Search and Rescue's doctors are undrafted
  by rule), the owner's effects-not-names rule and the index it produced, the need, safety and choice rules, self-medication,
  the disease-fighting drugs and the bleeding hold (0.3.0), two upstream faults for the owner to report, and the test protocol.

### Triage

- [Triage: research and decisions (6 Nov 2026)](triage.md): what Search and Rescue and More Injuries already do and what neither
  does (queue surgery), how the game creates, orders and forgets a bill, the owner's choices on authority and defaults, the
  by-effect classing of conditions, the choice of operation, the tag and score, the shared health layer in the framework, and
  the test protocol.

## Water tap

- [Water tap: research and decisions (4 Nov 2026)](water-tap.md): whether anyone had built one (one near miss), how Dubs Bad Hygiene handles bottles and water quality, why a
  split stack of untreated water came out clean and how the mod fixes that, and who chose what.

## Artwork

- [Artwork direction (19 Oct 2026)](artwork-direction.md): why PixelLab does not suit RimWorld, the three
  routes (code-drawn, local Flux.1-schnell through ComfyUI, a paid plan last), licences and records.
- [Art backlog (6 Nov 2026)](art-backlog.md): every texture and preview the mods ship or still need, which suit the local image
  model, and what must happen before a run.
- [Art automation and medical equipment: design (4 Nov 2026)](art-automation-and-medical-equipment.md): where all the current work
  stands, a hands-off overnight art pipeline, what makes training material, vanilla sprite sizes and the EULA, the medical
  equipment your mods already have, what they lack, and the open questions.
- [View-angle models: research record (7 Nov 2026)](view-angle-models.md): why generated assets come out at odd angles, which
  published models can lock or move the camera, their licences and memory needs on the owner's card, and the trial order.
- [Paid image generation: rules and lessons from Phobos Ostranauts (9 Nov 2026)](paid-image-generation-rules.md): the twelve
  rules for every picture bought from the OpenAI image API, where each came from in the Ostranauts records (projection
  failures, pilots before families, reference roles, what a provider ignores, records, cost discipline), what differs for
  Vehicle Framework bodies, the prompt skeleton, the pilot flow, the review checklist and what the engine enforces.

## Analysis tooling

- [Analysis tooling: proposals (7 Nov 2026)](analysis-tooling.md): eight deterministic tools for logs, saves, mod Defs,
  assemblies and the Workshop, so sessions read summaries instead of 300 MB saves; the language of each, and why they live in
  the shared phobos-modkit repository.

## RimWorld

- [The owner's mod list](owner-mod-list.md): every mod the owner has on, checked against each mod's own rules,
  with the official Vanilla Expanded collection and suggestions for what to add next. Kept current by
  `scripts/refresh-mod-list.py`, which also caches the collection in `vanilla-expanded-collection.json`.
- [RimWorld modding reference (18 Oct 2026)](rimworld-modding-reference.md): versions, the Waylith engine and
  1.7, package IDs, `About.xml`, load folders, C# build targets, paths, and Ludeon's EULA on mods, with sources.
- [RimWorld 1.7 readiness (24 Oct 2026)](rimworld-1.7-readiness.md): which of the owner's mods declare 1.7,
  which frameworks hold the rest back, what players report broken, and how to try 1.7 on a copy.
