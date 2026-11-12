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
- [In-game test pass (8 Nov 2026)](in-game-test-pass.md): the short list of checks for the owner's next session, three that
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
  owner's open questions; then what D'thorga Framework 0.11.0 built (8 Nov 2026), the owner's answers and a test list. Not yet
  tested in game.
- [Gas turbines: design (8 Nov 2026)](vehicle-turbine-design.md): turboshaft helicopters, the Harrier's vectored-thrust engine
  with water injection, and the tank turbine; flight phases as the aircraft load signal, the vertical-landing hover margin,
  hot and hung starts from the electrical system, twin engines, and what to confirm in the flight frameworks first; then what was
  built on 8 Nov 2026 (Framework 0.11.0, Vehicles 0.2.0, Patches 0.14.0), what the decompiles said, the owner's answers and a
  test list. Not yet tested in game.
- [Vehicle simulation references (8 Nov 2026)](vehicle-references.md): the study library for the three vehicle designs (books,
  papers, open code and game documentation), each with its terms, what to take from it and whether it was read.
- [Power and lift: design (8 Nov 2026)](vehicle-power-and-lift-design.md): any number of engines, helicopters' rotor drives
  (the Huey's and the Mangusta's transmission limits), thrust-lifted pods losing lift with each thruster, a lift-off check for
  Vehicle Framework's own aircraft, and a diesel-electric with traction motors fitted to a US Army manual's short-time ratings;
  built in D'thorga Framework 0.12.0 and D'thorga Vehicles 0.3.0. Not yet tested in game.
- [Real figures for named vehicles: design (8 Nov 2026)](vehicle-real-figures-design.md): the L-ATV first, then the MaxxPro,
  Humvee, Land Cruiser J70, PLS (Roaring Tiger's "HEMTT"), M113, M48H, M10, Bradley, Abrams, T-54, BMP-1s and Wiesel with
  their engines' published power, weights and gearboxes; D'thorga Vehicles 0.4.0. Not yet tested in game.
- [Steam boilers: design (8 Nov 2026)](vehicle-steam-design.md): raising steam over hours, safety valves, the water glass and
  injector, water stops, and the fusible plug that melts if the crown sheet is uncovered, for Trains of the Rim's Frisco 4-8-4
  and Tommy 0-6-0T; D'thorga Framework 0.13.0 and D'thorga Vehicles 0.5.0. Not yet tested in game.
- [The world map and the cab: design (8 Nov 2026)](vehicle-world-map-design.md): what the vehicle systems did in caravans
  and what was skipped or wrong, a closed cab's own temperature through the game's held-temperature lookup, the night heater,
  caravan speed caps and refusals, the jump-start kit and its two settings; D'thorga Framework 0.16.0 and D'thorga Vehicles
  0.7.0. Not yet tested in game.
- [A shared logging framework: research and direction (8 Nov 2026)](logging-framework-design.md): why every mod's log dies at
  10,000 messages a session (one mod's storm killed the owner's), the 1.7 pipeline from Verse.Log through Waylith to Player.log and
  back, what other mods' loggers do, and the direction: a bounded revive without Harmony, our own log categories with budgets, a
  shared logger in a sibling repository, a schema and reader across games, and no Rust inside the game. Nothing built yet.
- [A pawn-like health model for vehicles: research and direction (8 Nov 2026)](vehicle-health-design.md): what the game does
  for a pawn and Vehicle Framework for a vehicle (it switches the pawn health stack off), why a condition layer on Vehicle
  Framework's parts beats reviving that stack, the performance budget and measurement protocol, the NPC lite tier, and the
  four-slice roadmap from the Health tab to repair depth. Not yet tested in game.
- [Electric vehicles: design (8 Nov 2026)](vehicle-electric-design.md): traction battery temperature and wear, the electric drive,
  charging that follows the battery, and the Lightning as a test van behind a setting; Framework 0.20.0, Vehicles 0.9.0. Not yet
  tested in game.
- [Kerb mass check (8 Nov 2026)](vehicle-kerb-mass-check.md): the 12 kg per Vehicle Framework Mass rule against every active
  vehicle; authored masses for Vanilla Vehicles Expanded, tunable empty mass; Framework 0.19.0, Vehicles 0.8.0. Not yet tested in game.
- [Vehicle follow-ups: design (8 Nov 2026)](vehicle-crew-and-ground-design.md): booster packs with starts, the world map's ground
  from each biome's Defs, cab moods by comfort range, and the RimHUD value; D'thorga Framework 0.18.0. Not yet tested in game.
- [The vehicle tabs: design (8 Nov 2026)](vehicle-tabs-design.md): the Dashboard and Systems inspector tabs, how they hook into
  Vehicle Framework's Def resolution, the saved day of history behind the graphs, the widgets that went to the Framework core,
  and what RimHUD does with them; D'thorga Framework 0.17.0. Not yet tested in game.
- [Vehicle tuning and upgrades: what it would do to the player (9 Nov 2026)](vehicle-tuning-and-upgrades.md): fittings and engine tunes for
  any vehicle; a proposal with six questions for the owner, nothing built.
- [Vehicle upgrades, round two (11 Nov 2026)](vehicle-upgrades-round-two.md): which of the owner's 75 vehicles have an upgrade tree
  and whose it is (the Cherokee has one, from VVE - Upgrades Extended; 21 have none), and a signature feature proposed for each
  thin one; a proposal with four questions for the owner, nothing built.
- [Vehicle upgrade families (12 Nov 2026)](vehicle-upgrade-families.md): five families of fittings shared across many vehicles
  (recovery, long range, cold weather, work lights, all-terrain tyres), chosen by effects and loadout roles, beside other authors'
  trees; a proposal with four questions for the owner, nothing built.
- [Trailers that follow their tractor (11 Nov 2026)](vehicle-trailer-research.md): how RimThunder - Core hitches, draws and turns
  a trailer (read from code), what Turbo's library and RimArrow add, the limits, and a hitch of our own with a smoother turn;
  research with three questions for the owner, nothing built.
- [The hitch and the generic trailer: design (11 Nov 2026)](vehicle-hitch-design.md): a hitch of our own beside the sling, a
  trailer any vehicle can tow with the load going through the drivetrain model, the smooth turn from one follow rule, and the
  check that Vehicle Framework draws bodies and overlays at any angle; four decisions for the owner, nothing built.
- [Other factions' vehicles: an audit of the vehicle code (11 Nov 2026)](vehicle-other-factions-audit.md): every check of the
  player's faction read against the owner's rule; three small faults, the lite tier leaving raiders' vehicles untouched by the
  drivetrain, and three decisions for the owner. Nothing changed.
- [Code audit: saves, cost per tick and missing things (11 Nov 2026)](code-audit-2026-11-11.md): a read of all three across our
  mods (radio left out); one save fault fixed in Framework 0.96.1, a proposal for optional parts' saved data, and a three-step test
  list for the owner.
- [A setup wizard on first load: the idea (9 Nov 2026)](first-load-setup-wizard.md): the owner's idea, kept for later; what it would cover,
  when to raise it and what to settle first.
- [Vehicle health, slice 3: what it does to the player (8 Nov 2026, built 9 Nov 2026)](vehicle-health-consequences.md): the danger clock,
  destroyed parts needing supplies, serious conditions, field fixes, and our own parts framework.
- [Vehicle system crosslinks: research and record (9 Nov 2026)](vehicle-system-crosslinks.md): the 22 systems form a star round
  the engine state, with five quantity links between them; eight proposed crosslinks (night heater warming the engine, the cab heater off the
  coolant, a worn engine burning oil, fire cooking the systems, and more), each answered for itself, with an order and the owner's answers
  (10 Nov 2026). Links 1, 2 and 8 built in Framework 0.67.0, link 3 in 0.68.0, link 4 in 0.69.0, links 7, 5 and 6 and a block heater in 0.70.0: all eight built, not yet tested in game.
- [Vehicle crosslinks, round two: deeper chains, more fittings, and what the stations should do (10 Nov 2026)](vehicle-crosslinks-round-two.md):
  the graph after 0.70.0 and its holes (hydraulics nobody reads, no drive belt, flat brake wear, tread that never loses grip, a rig that only
  gates); eight deeper links, ten fittings and a boost axis, the rig doing diagnostics and better fits, consumables as items, four things argued
  against, an order and the owner's questions. T1 to T3, S1, S2 and an inspection pit built in Framework 0.73.0 and Vehicles 0.33.0 (not yet
  tested in game); S4, service kits as items, in Framework 0.74.0.
- [How our vehicle features look: research and record (11 Nov 2026)](vehicle-presentation-design.md): everything we show is text and
  flat bars; what the game gives free (letters that open windows, the health card's row shape, the curve drawer, a vehicle's picture in a
  rect); the diagnosis as a report window, the fleet board, fittings icons, one dial, graphs (already recorded, free while closed), the
  damage sheet and the world-map range as the strikingly visual high-benefit pieces, three art batches with costs, what is argued
  against, and the owner's questions. Nothing built.
- [Parts bills: what a built thing is made of, by role (11 Nov 2026)](parts-bills.md): a generic core service (Framework 0.95.0): a
  building or a vehicle lists its roles, and each is filled by another mod's part where installed and suited, ours otherwise; the Def
  types with an example for a placed building, every vehicle's bill, and what a later chemical industry mod reuses. Not yet tested in game.
- [Sling loads: a helicopter carries a vehicle under it (11 Nov 2026)](vehicle-sling-design.md): the owner's own airlift with our
  flavour; a cargo-hook fitting on any helicopter, real masses against sourced hook ratings with hot-day derating, a ground crew, weather
  limits, a slower thirstier slung flight and an emergency release, each a setting; three slices; nothing built yet.
- [Crash record: stack overflow on 9 Nov 2026](crash-2026-11-09-stack-overflow.md): a runaway call loop the log never mentioned, what the dump proved and could not,
  three suspects, the other errors in that log, and how to name the culprit.
- [Fires on vehicles that never go out (10 Nov 2026)](vehicle-framework-fires.md): 37 fires on one jet, left on the wrong map and never changing; what the saves and
  the code show, what is still unknown, the two-patch fix in D'thorga Patches 0.16.6, and a draft report for Vehicle Framework's author.
- [Patches for other authors' faults (register, 10 Nov 2026)](temporary-patches.md): every patch that exists because of a fault in another mod, marked temporary or
  permanent, with what would make it unnecessary, how to check, and whether the author has been told.
- [The mobile generator set: design (9 Nov 2026, built 9 Nov 2026)](vehicle-generator-set.md): see the entry under the vehicle family; built with stand-in pictures. Not yet tested in game.
- [Home depot bays: design (9 Nov 2026)](vehicle-depot-bays.md): a vehicle's home bay, its Go to bay button, self-refuelling and faster service; crews and convoys left to Crew Presets. Not yet tested in game.
- [Animating our vehicles: research (9 Nov 2026)](vehicle-animation-research.md): what Vehicle Framework and the game can move, why one still image a
  moving part beats frame sheets and custom shaders, and where motion would benefit each of our five vehicles before more art is made.
- [Vehicle stations and the motor pool tab: design (9 Nov 2026)](vehicle-stations.md): the parts bench, the tuning rig that tunes and refits need,
  and our own Architect tab, so parts stop getting lost in long lists. Not yet tested in game.
- [The vehicle garage and fleet tools: design (8 Nov 2026)](vehicle-garage-design.md): a staffed outpost that holds and slowly
  repairs vehicles, what the outposts code allows and where it fails for vehicles, and the owner's four fleet tools. Not yet tested in game.
- [Radiation control line (10 Nov 2026)](radiation-control-line.md): a sign or painted area behind which colonists suit up for Rimatomics' radiation, built on D'thorga Framework's generic protective-gear lines, with the owner's choices. Not yet tested in game.
- [The D'thorga research tree (10 Nov 2026)](research-tree.md): the D'thorga research tab, its projects, costs and soft tie-ins, the one-time grant for existing colonies, and enforcing research on our vehicles' upgrades. Not yet tested in game.
- [The Seabed Crawler: design (10 Nov 2026)](vehicle-seabed-crawler.md): the owner's first real submarine and research vessel, its eleven upgrades with their own research and crafted parts,
  sonar, radar, the research rules, soft tie-ins and the chemistry. The first slices are built; not yet tested in game.
- [The Seabed Crawler's picture: research and art record (11 Nov 2026)](vehicle-seabed-crawler-art.md): NR-1 from above and what we keep of it,
  the 4 x 9 boat on a 1024 x 2048 canvas, the six visible upgrade nodes placed before the first picture, the prompt, the costs and the review.
  Nothing bought before the owner's go.
- [The submersibles framework: design (9 Nov 2026)](submarine-framework.md): a dive as a state of ours, the reserve, what is hidden and what still reaches a hull,
  why each patch exists, the limits of the first slice and how to test it. Built in Framework 0.58.0 with a dev-only test hull. Not yet tested in game.
- [Striking another map: lessons from DMS Expedition's long-range cannon (11 Nov 2026)](cross-map-strikes.md): why the owner's long-range
  shells vanished at the border under overhead mountain, the other silent ways the game ends a projectile, and proposed rules for our
  ballistic submarines and other cross-map weapons (launch straight up, never spend a shot that cannot leave, design for enemy launches).
  Research and proposal; nothing built.
- [Unique military vehicles and submarines: research (9 Nov 2026)](military-vehicles-research.md): twenty-odd real vehicles no
  Vehicle Framework mod covers, checked against the 277 items of the three Workshop collections; five shortlisted (recovery,
  decontamination, shower, landing craft, snow carrier) and five submarines (cargo, chemical, seabed crawler, carrier, polar),
  with sources and open questions. Research and record only.
- [Vehicle figures: research (8 Nov 2026)](vehicle-figures-research.md): sourced engine, transmission and weight figures for the
  L-ATV, MaxxPro, Humvee, PLS, M113, Bradley, M48H, M10, T-54, BMP-1, Wiesel, Land Cruiser J70, the Abrams' turbine and five
  helicopters, with what was not found.
- [Steam and diesel-electric: research (8 Nov 2026)](steam-and-diesel-electric-research.md): the Frisco 4500 class and small
  tank engines, safety valves, crown sheets and fusible plugs, steam tables, fuels and efficiency, diesel-electric shunters'
  short-time ratings, and engine-out precedents for thrust-lifted craft.
- [Utility vehicles: design (8 Nov 2026)](utility-vehicles-design.md): the new content mod and why it starts as a stand-in,
  the parts model and stats, the upgrade-tree and parts-by-provider principles (8 Nov 2026), the extraction-chain family in full
  (survey truck, drill rig, tipper hauler, generator set, and the bulk tanker that is liquid or gas by upgrade),
  the catalogue of other utility vehicles, the Vehicle Framework 1.7 gate and the art plan; the tanker is built (not yet tested in
  game), the other four have records of their own, below.
- [The survey truck: design (9 Nov 2026)](vehicle-survey-truck.md): the ground-penetrating scanner on wheels, with a long-range mode,
  a powered array and a battery by upgrade; uranium in its cost; deepchem found, no gas. Research and record only.
- [The mobile drill rig: design (9 Nov 2026)](vehicle-drill-rig.md): drills the game's deep resource grid at its rates, deepchem to the
  tanker beside it, an oil-well kit with Rimefeller, electric drilling on the camp's power. Research and record only.
- [The tipper hauler: design (9 Nov 2026)](vehicle-tipper-hauler.md): a high-capacity body and one tip job; the chunk arithmetic; a
  spreader for Rails and Roads of the Rim. Research and record only.
- [The mobile generator set: design (9 Nov 2026)](vehicle-generator-set.md): the family's diesel-electric vehicle; parked, it feeds the
  grid through a stub building; the camp's power rule for every vehicle of ours. Research and record only.
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
- [Colony mood file: design record (10 Nov 2026)](mood-snapshot.md): at each save, D'thorga Framework writes every colonist's,
  slave's and prisoner's thoughts as the game works them out, so phobos-modkit's mood report can name what a save leaves out
  (rooms, pain, beliefs); the file's shape and how the report matches it to its save. Built in Framework 0.64.0, not yet tested in game.

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
- [Paid image generation: rules and lessons from Phobos Ostranauts (8 Nov 2026)](paid-image-generation-rules.md): the twelve
  rules for every picture bought from the OpenAI image API, where each came from in the Ostranauts records (projection
  failures, pilots before families, reference roles, what a provider ignores, records, cost discipline), what differs for
  Vehicle Framework bodies, the prompt skeleton, the pilot flow, the review checklist and what the engine enforces.
- [Sound generation: research and pipeline (10 Nov 2026)](sound-generation.md): local sound effects with Stable Audio 3 Small
  SFX through ComfyUI, the models compared and their licences, ffmpeg clean-up, and a listening page with spectrograms,
  measured flags and a lowered copy for ears that lose the high frequencies; the toolkits that measure the game's own sounds and ship
  ours; the first four sounds, shipped 10 Nov 2026 (not yet tested in game).
- [Sound catalogue: every sound we need, now and planned (10 Nov 2026)](sound-catalogue.md): an audit of all our vehicles, machines,
  devices and systems against what GTek's L-ATV does with Vehicle Framework's events; about 130 sounds with IDs, which are shared and
  why, which need only XML, an order of work and six questions for the owner. Nothing built beyond the first four.

## Analysis tooling

- [Analysis tooling: proposals (7 Nov 2026)](analysis-tooling.md): eight deterministic tools for logs, saves, mod Defs,
  assemblies and the Workshop, so sessions read summaries instead of 300 MB saves; the language of each, and why they live in
  the shared phobos-modkit repository.
- [Desktop front end for the Phobos tools: research and direction (9 Nov 2026)](tools-desktop-research.md): a program for
  the person at the screen over every tool, built for humans (or dragons) while agents keep the command line; the owner's
  decisions (Rust and SQLite, Republic Observatory's layout and feel under House D'thorga, a new sibling repository), what
  Observatory and OnAir WyrmGrid teach, every question the tools answer today, the kit changes it needs, and the planet
  globe as the first slice. Research and record only; nothing built.

## RimWorld

- [The owner's mod list](owner-mod-list.md): every mod the owner has on, checked against each mod's own rules,
  with the official Vanilla Expanded collection and suggestions for what to add next. Kept current by
  `scripts/refresh-mod-list.py`, which also caches the collection in `vanilla-expanded-collection.json`.
- [RimWorld modding reference (18 Oct 2026)](rimworld-modding-reference.md): versions, the Waylith engine and
  1.7, package IDs, `About.xml`, load folders, C# build targets, paths, and Ludeon's EULA on mods, with sources.
- [RimWorld 1.7 readiness (24 Oct 2026)](rimworld-1.7-readiness.md): which of the owner's mods declare 1.7,
  which frameworks hold the rest back, what players report broken, and how to try 1.7 on a copy.
