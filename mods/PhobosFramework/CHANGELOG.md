# D'thorga Framework changelog

Maintained from 4 November 2026. Draft entries record preparation, not Steam publication; nothing is published yet. Not yet
tested in game unless an entry says the owner tested it.

## [Unreleased]

## [0.8.1] - 2026-11-07 - Draft

Found in the owner's log of 7 November 2026: 7,662 errors in one session. Not yet tested in game.

### Fixed

- The frame-work queue: when a loaded game empties the queue, a job whose map has gone could throw while being named in the log, which left the queue full and threw again on every frame. Every log line now names a job through a label that cannot throw, and the queue is emptied even when a drop fails.

## [0.8.0] - 2026-11-07 - Draft

Made with D'thorga Medical 0.7.0, its first user. No change in play on its own. Not yet tested in game.

### Added

- For modders: a per-person choice's icon, its menu and its label with the current option, from the choices registry, so every place that offers the choice draws the same thing. The pawn's own button now uses them.
- For modders: a condition oxygen helps can say how much faster it heals on supplied oxygen, and a treatment how much better than a mask it is; the treatment calls one shared method to heal. Permanent conditions never heal.
- For modders: reading another mod's setting by name, from its static settings holder, even when a source generator hides the field inside a longer name.

## [0.7.1] - 2026-11-07 - Draft

Owner's report: many settings seemed to have gone missing since the merge. **Tested in game: the owner reports every setting back (7 November 2026).**

### Fixed

- Long settings windows (D'thorga Medical's, and D'thorga Patches' since 0.8.0) lost their later settings off-screen. Past its first height the game's settings list starts a new column to the right, which in a scrolling page sits out of sight and makes the page measure itself shorter, so more settings disappeared each moment. The page now keeps one column and scrolls. No setting was lost: every choice was saved and carried over, and shows again with this fix.

## [0.7.0] - 2026-11-07 - Draft

Made with D'thorga Patches 0.8.0, its first user. No change in play on its own. Not yet tested in game.

### Added

- For modders: a work queue that runs a long job a slice per drawn frame, paused or not, within a time budget, so the game keeps responding. A job says how to start, how to take one step and when it is done; identical jobs queued behind one another can fold into one.
- For modders: a hold on a map's room and region rebuild while a slice destroys many things, released and rebuilt once at the end of the slice.
- For modders: a cost warning under a setting, in amber, and a labelled button that opens a menu of choices.

### Save compatibility

- Adds one game component that saves nothing. A save made with it logs one line about a missing class if the framework is later removed.

## [0.6.0] - 2026-11-07 - Draft

Made with the merge of seven D'thorga mods into two (owner's decision, 7 November 2026), as the pieces both merged mods needed. No change in play. Not yet tested in game.

### Added

- For modders: a one-time settings import. A mod that changes its folder or its Mod class reads the settings file its old self left, once, so a player's choices carry over. D'thorga Medical and D'thorga Patches use it.
- For modders: section headings and a scrolling page for settings windows longer than the screen. D'thorga Medical's one window, with a section per part, uses them.
- For modders: a load line can name the part of a mod it reports, for a mod made of several parts that logs one line per part.

### Compatibility and limits

- The shared build rules now stop any project's assembly being copied beside another mod's. A copy of this framework's assembly had shipped inside D'thorga Medical Equipment since 0.5.0.

## [0.5.0] - 2026-11-07 - Draft

Not yet tested in game.

### Added

- A Dubs Bad Hygiene tie-in, loaded only while that mod is active: the one shared way the D'thorga mods draw water from its plumbing,
  from a pipe under a building or against its side. Moved from D'thorga Factory Additions, whose machines and D'thorga Medical
  Equipment's oxygen electrolyser now both use it.

### Compatibility and limits

- Loads after Dubs Bad Hygiene when both are present. Without it nothing changes and nothing is logged.

## [0.4.2] - 2026-11-07 - Draft

No change in play. Not yet tested in game.

### Added

- For modders: one shared check of who counts as a patient (one of the colony's people with a flesh body, and lying down: downed or
  in bed), so each treatment mod stops writing its own. A mod's own version had let a vehicle be checked as a patient.

## [0.4.1] - 2026-11-07 - Draft

A fix from the owner's game. **The owner reports the error gone in game (7 November 2026, on 0.4.1).** The surgery bars from 0.4.0
are not yet tested in game.

### Fixed

- Selecting a dead colonist's corpse threw an error and showed it in the inspect pane instead of the corpse's details (seen in the
  owner's game on 0.3.1, 7 November 2026). The per-person care comp had also reached the corpse; it now shows nothing there, and is
  added only to things that are pawns. The error was in display code only: it could not hurt or kill anyone.

## [0.4.0] - 2026-11-07 - Draft

Not yet tested in game.

### Added

- Surgery bars for modders: a mod registers what stops surgeons operating on a patient for now, with the reason in the game's words,
  and a mod that queues operations asks whether a patient is barred and why. D'thorga Medical Equipment registers its hyperbaric
  chamber; D'thorga Triage is to read it. Nothing is saved.
- A Workshop preview picture: a generated background the owner picked (7 November 2026), with the name added by script. No change in play.

## [0.3.1] - 2026-11-06 - Draft

### Fixed

- The care-choice button (D'thorga Triage's "Doctors may operate") showed the game's red error cross: it asked for a texture the
  game does not have. It now shows the game's industrial medicine icon, as the medical care menu does. Seen in game by the owner,
  6 November 2026; the fix is not yet tested in game.

- The load line said "0 care choices registered": it is written before other mods register theirs. It now names only the comp.

### Added

- A care choice can name its own icon (iconPath on PatientPreference); a missing one falls back to the medicine icon.

### Compatibility and limits

- The owner reports the gas layer and supplied oxygen working in game through Burns and Smoke and Medical Equipment, 5 November
  2026. The health layer is not yet tested in game.
- README corrected: removal prints a line per missing map component (up to three per map), and the care choices are saved too.

## [0.3.0] - 2026-11-06 - Draft

The health layer the medical mods share, built for D'thorga Triage and adopted by Emergency Dosing 0.3.1 and Medical Equipment
0.2.2. Not yet tested in game.

### Added

- A health layer for mods whose doctors act on their own: who the doctors look after and under what care (colonist, slave,
  prisoner, guest), how dangerous a condition is and how long a patient has (from the game's own life-threatening stages, death
  clocks, lethal severities and bleeding), which doctors could operate and how well, a per-map patient cache, and a lapsing
  treatment condition. Emergency Dosing and Medical Equipment now read these from here instead of their own copies.
- Per-person care choices: every humanlike pawn carries a small comp that saves the choices D'thorga medical mods register, and
  shows a button per choice on colonists, slaves, prisoners and guests. On its own the framework registers none.
- Dev action: Framework: log care choices of selected pawns.

### Save compatibility

- The comp saves only the choices a player sets, as plain fields on the pawn; a save loads without the framework with no line
  about them. Nothing else new is saved.

## [0.2.2] - 2026-11-05 - Draft

No change in play. Not yet tested in game.

### Changed

- Moved into the gas rules Def, at the same values: the resistance that counts as sealed air without Combat Extended, and how enclosed
  a roofed cell is in a room that counts as outdoors.

## [0.2.1] - 2026-11-05 - Draft

### Fixed

- With Combat Extended, a gas mask stopped carbon monoxide. The sealed-headgear test read the smoke sensitivity stat, and
  Combat Extended's gas masks lower it to 0 with the same offset its power and recon helmets use. The figure is now rebuilt
  from the worn items that carry no gas-mask tag, so a filter mask passes carbon monoxide as the README always said and a
  sealed helmet still stops it. Found by reading the owner's save PG19 of 5 November 2026 against the code; not yet tested in game.

## [0.2.0] - 2026-11-05 - Draft

### Added

- Supplied oxygen, for medical mods: one condition marks a pawn as breathing supplied oxygen, and another marks a condition as one
  oxygen helps, so a mod can ask about either without depending on the mod behind it. It is a treatment, not a gas in the room.
  Not yet tested in game.

### Changed

- The description now carries the disclaimer in the wording Ludeon's EULA requires. It said only that the mod is not official and not endorsed. No behaviour change.

## [0.1.1] - 2026-11-04 - Draft

### Fixed

- The load report threw at startup ("Could not execute post-long-event action") because the Combat Extended smoke check read
  the tick manager before any game existed, and took D'thorga Burns and Smoke's load line down with it. The check now reads
  the setting directly outside a game. Seen in the owner's Player.log on 4 November 2026; the fix is not yet tested in game.

## [0.1.0] - 2026-11-04 - Draft

The owner's shared framework, started for D'thorga Burns and Smoke and recorded as a future idea on 22 October 2026. Not yet
tested in game.

### Added

- The gas layer. One way for a mod to read what a pawn breathes: smoke from Combat Extended's own smoke inhalation condition
  while that runs (its masks, sleep and breathing capacity included), otherwise the game's own smoke at the cell through the
  pawn's toxic environment resistance or a gas-mask tag; tox gas, rot stink and deadlife dust from the game's grid.
- Fires fill roofed rooms with the game's own smoke whenever Combat Extended's smoke is not in use, so burning buildings are
  smoky without it, with the game's own spread through open doors, slower fading under a roof, and its aim and sight penalties.
- The framework's own additive room gases: one mixture per room that exists only while something adds it, exchanged through
  open and closed doors and switched-on vents at the game's own door and vent rates, vented through roof gaps and doors to
  the outdoors, fading at each gas's half-life, emptied by vacuum, kept across wall changes through the cells it covered, and
  saved as cells. No air, oxygen or pressure model.
- Carbon monoxide as the first own gas: fires add it to their roofed room; it ignores filter masks and lingers for hours.
- The owner's rule as a setting, "Where gases come from": Automatic reuses existing gases and effects and uses the
  framework's own where nothing else carries one; Always own and Never override it. A gas Def can pin itself to own.
- An XML-only emitter comp, a dev-mode overlay and two dev actions.
- Shared helpers: optional mods and Defs by name, the one-line load report, the Plain, Operator and Engineer display mode and
  its settings radio, a rules-Def base whose first instance wins, the D'thorga debug category, and a cleanup service that
  removes a mod's conditions from every pawn before the mod is switched off.

### Dependencies

- None. Combat Extended is reached by name only (its setting, condition and stat names); no other mod's assembly is referenced.
- Built twice: against the 1.7.4998 reference on .NET 10, which leads, and against the 1.6 reference; the game loads its own
  version's folder (1.7/Assemblies or 1.6/Assemblies).

### Save compatibility

- Safe to add mid-game. Saves hold the room gas as cells; a save without the framework loads with no gas. Safe to remove once
  the mods that depend on it are gone; the next load prints one harmless line about a missing map component.

### Compatibility and limits

- Gas does not travel with a gravship. Rooms are found through regions and vents through a room's adjacent things on 1.7;
  not yet confirmed in play. Humanoid Alien Races and other flesh races use the same giver sets and get the same gases.
- Game-free rules pass 17 unit tests. Nothing is tested in game.
