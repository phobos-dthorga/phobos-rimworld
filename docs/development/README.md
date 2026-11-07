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
  ventilator to the steriliser, what each would do in play and what it rests on. Nothing built.
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

## RimWorld

- [The owner's mod list](owner-mod-list.md): every mod the owner has on, checked against each mod's own rules,
  with the official Vanilla Expanded collection and suggestions for what to add next. Kept current by
  `scripts/refresh-mod-list.py`.
- [RimWorld modding reference (18 Oct 2026)](rimworld-modding-reference.md): versions, the Waylith engine and
  1.7, package IDs, `About.xml`, load folders, C# build targets, paths, and Ludeon's EULA on mods, with sources.
