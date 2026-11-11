# D'thorga Radio Framework changelog

Maintained from 19 October 2026. Draft entries record preparation, not Steam publication; nothing is
published yet. Not yet tested in game unless an entry says the owner tested it.

## [Unreleased]

## [0.1.7] - 2026-11-12 - Draft

The base station has its own picture, chosen by the owner on 12 November 2026. **Not yet tested in game.**

### Changed

- **A new picture for the base station**: a radio set with a screen and a hand microphone on a coiled lead, in place of the flat placeholder. Nothing changes in play. The whip antenna keeps its stand-in until work on the radio resumes (owner, 12 November 2026).
- The mod's description and README now carry the spelling dthorga beside D'thorga, so a search without the apostrophe finds it. The description says it is community-created by House D'thorga.

## [0.1.6] - 2026-11-09 - Draft

### Balance

- UHF's easier time now applies only to walls and built roofs, which it gets through by way of openings. Rock and natural roofs cost UHF the same as VHF, which also makes the siting preview's figure for them right on either band (owner's decision). **Not yet tested in game.**

## [0.1.5] - 2026-11-09 - Draft

### Fixed

- A base station with no power no longer makes contacts: hand-helds said in touch, and colonists kept the mood, with the station off. A hand-held now says the station has no power. **Not yet tested in game.**
- The false No storage connected alert on every feedline is gone, and coax no longer reads as storing zero decibels. **Not yet tested in game.**
- A generator out of fuel no longer raises the radio noise floor. **Not yet tested in game.**
- A new coax route, or a change to the scale of a cell, now takes effect at once instead of at the next reload. **Not yet tested in game.**
- A port with no cable says so. Clicking a station's gizmo opens its Net tab instead of deselecting it. A slave's radio no longer claims there is no station. The rock obstruction patch now takes effect. The antenna line names the strongest antenna, and a mast on the coax before anyone carries a radio. **Not yet tested in game.**
- Loading a game no longer keeps the previous game's map or pipe networks in memory. **Not yet tested in game.**

### Changed

- Dates in this mod's records that were written a day ahead now read 8 November 2026. Text only; nothing in game changes.

## [0.1.4] - 2026-11-09 - Draft

### Fixed

- Loading a game within a session no longer keeps the previous game's pipe networks, and through them its maps, in memory. **Not yet tested in game.**

## [0.1.3] - 2026-11-08 - Draft

No change in play on 1.7. Not yet tested in game.

### Compatibility and limits

- RimWorld 1.7 only: the 1.6 build, its load folder and 1.6 in the About file are gone (owner, 8 November 2026: 1.6 is a dying version). Players on 1.6 keep the last version that had it.
- The About file now lists 1.7, so the mod list no longer calls it outdated on 1.7.

### Changed

- Generated art the owner picked (7 November 2026) in place of five placeholders: the hand-held radio, the mast (a guyed lattice tower), the signal and feedline icons, and the whip antenna, which uses a thin lattice mast as a stand-in until a whip is picked. The mast and whip are drawn at their real shape, tall and narrow, instead of a square. Each passed the near-copy check; the record beside each texture says how it was made. The base station and the feedline keep their placeholders.
- The Workshop preview is a generated background the owner picked, with the current name added by script.

### Fixed

- The assembly's own version said 0.1.1 since 0.1.2; it now matches. No change in play.

### Compatibility and limits

- README brought up to date: the supported versions (a build for 1.7 and 1.6; About.xml still lists 1.6 only), no Harmony
  needed, the settings' range and the mood switch, the one-step rebuild, and a diagram of what decides a contact.

## [0.1.2] - 2026-11-05 - Draft

No change in play. Not yet tested in game.

### Changed

- Messages use named placeholders, so translators can see what each one means.
- The description now carries the disclaimer in the wording Ludeon's EULA requires. It said only that the mod is not official and not endorsed. No behaviour change.

## [0.1.1] - 2026-11-04 - Draft

A native RimWorld 1.7 build, which now leads (owner, 4 November 2026); the 1.6 build stays as the minimum backwards support
until 1.6 is retired. No behaviour change. Not yet tested in game on either version since this build.

### Changed

- The assembly is built twice, against the 1.7.4998 reference on .NET 10 and against the 1.6 reference as before;
  the game loads its own version's folder (1.7/Assemblies or 1.6/Assemblies). Until now 1.7 loaded the 1.6 build.

### Compatibility and limits

- The 1.7 build references Vanilla Expanded Framework's own 1.7 PipeSystem assembly, the 1.6 build its 1.6 one.

## [0.1.0] - 2026-10-19 - Draft

Phase 1 of the design: local VHF and UHF on one map. Built and deployed to the owner's game on
19 October 2026; the owner's play test is what decides whether the 6 dB rule feels right.

### Added

- The link-budget window (click a radio's gizmo, or a row of the station's Net tab): opens at your
  display mode and expands one level deeper on request, following the pawn while open.
- The station's Net tab: every worn hand-held against this station, with its outcome, the reason or
  the Operator summary, and which station is each pawn's best.
- The siting preview while placing an antenna or the station: antenna height, the roof over the cell
  and what it costs, and the colony noise there. Warns under thick roof; never blocks.
- Dev-mode actions under "Phobos Radio": log the selected pawns' full sheets, log the noise at a cell,
  recompute the net now. All read-only.
- Coax feedline on Vanilla Expanded Framework's pipe network: lay it like power conduit from the base
  station to an antenna (2 steel per cell, under walls if need be). The station walks the cable to each
  antenna and charges the run's loss: 8 dB per 100 m on VHF, rising with the square root of frequency.
- Whip antenna (3 m, no gain, 15 steel) and mast antenna (10 m, 3 dBi, 60 steel + 1 component). The
  station uses whichever antenna on its coax gives the best link, or its built-in whip; the inspect text
  names the one in use. Ports say whether coax is connected and what is on it.
- Local radio: the research project (after Microelectronics Basics), two hand-held radios (VHF and
  UHF, utility apparel on the belt slot, 5 W) and the radio base station (2 x 2, 60 W idle, 25 W
  transmitter with a built-in whip; draws more power and warms the room while in use).
- The link budget decides every contact in both directions, on the best band both ends share: free-space
  loss by distance, walls and doors on the straight line (counted only where that line is below 2.5 m,
  so a tall antenna sees over them), natural rock per cell, the roof over each antenna (overhead
  mountain is near-total), and the colony's electrical noise at the receiver.
- "In touch with home": a small mood thought for a free colonist whose worn hand-held has a workable
  link to a base station (+2), or a faint one (+1). Off by a setting if you prefer a pure readout.
- A signal-bars gizmo on every worn hand-held and on the station, with a Plain, Operator or Engineer
  description by the display-mode setting; clicking shows the full sheet.
- Band, mode and rules Defs, and two Def extensions other mods can use: obstruction loss on materials
  (patched onto the vanilla stuffs and natural rock) and noise on buildings (patched onto the vanilla
  generators and the battery).
- Mod settings: display mode, metres per cell, the thought on or off.
- Placeholder sprites for the hand-held, the station and the gizmo icon, drawn by a script.
- The mod skeleton: About.xml (packageId PhobosDthorga.RadioFramework, RimWorld 1.6), load folders for
  1.6 and 1.7 (1.7 borrows the 1.6 build), a placeholder preview image, this changelog and the README.
- The game-free physics core (decibels, free-space path loss, noise floor, coax loss, ray clearance,
  the link-budget sheet and the margin outcomes), with unit tests that run without the game.
- The build project (RimWorld 1.6, .NET Framework 4.7.2, PolySharp) and scripts/deploy.py, which copies
  the mod into the game's Mods folder with RimWorld closed.

### Balance

- Every figure is an authored starting value unless its Def comment names a source: 12 dB for FM voice
  (the SINAD convention), 0.6 transmitter efficiency, wall losses of 6 (wood) to 30 (plasteel) dB per
  crossing, 30 dB per cell of rock, roof losses of 6/20/60 dB, generators at -60 dBm at 1 m.

### Compatibility and limits

- A hand-held takes the belt slot, so a colonist cannot wear one with a shield belt.
- Only free colonists count for "in touch"; slaves, prisoners, guests and caravans do not. Downed
  colonists do. A radio in a pawn's inventory does nothing; it must be worn.
- Requires Vanilla Expanded Framework (declared in About.xml). Nothing from VEF ships with this mod.
- Coax water-ingress degradation (in the design) is not built yet.
- The coax sprite is a placeholder atlas; if its tiles look rotated, that is the atlas, not the net.
- Safe to add to a running save. Remove it only after deconstructing the station and dropping every
  hand-held; expect one harmless "could not find class" error on the next load.
- Metres per cell: decide it before starting a colony; changing it mid-save changes every distance.
- Not yet tested in game.
