# D'thorga Radio Framework changelog

Maintained from 19 October 2026. Draft entries record preparation, not Steam publication; nothing is
published yet. Not yet tested in game unless an entry says the owner tested it.

## [Unreleased]

### Changed

- **The art and sound records no longer ship in the mod's folder** (the owner's choice of 23 Nov 2026, in the docs rewrite).
  The notes saying how each picture and sound was made moved to the repository's assets folder, so installing the mod no longer
  copies them into the game's Mods folder; the next install leaves the old copies in its backup. Nothing a player sees changes.
- **The exporters' records no longer ship in the mod's folder either** (the owner's word of 23 Nov 2026). The notes saying
  what each picture was cut from moved beside the art records, in the repository's assets folder; nothing a player sees
  changes.
- **The README is slimmer**: what the mod needs, its real gotchas, what it adds with its figures, its settings and links; the
  notes for modders, the dev actions and the checks done are on the repository's modders' page for the radio. The Workshop
  draft now says it needs D'thorga Framework, as it has since 0.4.0, and names the upgrades. Nothing in play changes.

## [0.8.0] - 2026-11-24 - Draft

Channels by kind: the station counts each use of its channels apart

### Added

- **The channel ledger counts by kind**, as D'thorga Mechanised Specialist Expansion's BVR channels do (the owner's go of 24 November
  2026). Under its channels line a station lists each kind in use: "Local voice: 1 in use by 3 radios; room for 2 more". Each kind
  fills channels of its own, with its own mode setting how many one use takes and how many radios share them. Local voice is the
  only kind for now; other mods, and our coming remote control of vehicles, add kinds in XML. Not yet tested in game: row 478.

### Changed

- **The channels line says how many are free** instead of how many radios are served of how many could be; each kind's line now
  carries those counts.

### Compatibility and limits

- Safe with a running save: nothing new is saved. No new settings.

## [0.7.0] - 2026-11-24 - Draft

Channels: a station serves as many radios as its channels hold

### Added

- **Stations have channels, 25 kHz each, as on a trunked radio system** (the owner's go of 24 November 2026): the desk has one, the
  terminal four. Radios share a channel by taking turns, five to a channel for voice. A colonist in touch whose stations are all full
  reads "Radio: net busy" and gets no "in touch" mood; once served, a radio keeps its station. The station's text, button and Net tab
  count the channels. Not yet tested in game: rows 474 and 475.
- **The desk takes a second transceiver**, an upgrade for a second channel. Not yet tested in game: row 476.

### Changed

- **The waterfall puts each radio on the channel it really holds**, with the channels named under it, and the lamp and needle light
  for each served radio's turn. Not yet tested in game: row 477.

### Compatibility and limits

- Safe with a running save: nothing new is saved. No new settings.

## [0.6.0] - 2026-11-24 - Draft

The radio comes alive: a step chart, lamps and needles, a mast light, the coax glowing, and a waterfall

### Added

- **The link-budget window draws the budget as steps** (the owner's go of 24 November 2026): the power sent, each gain a step up and
  each loss a step down, to what arrives, with the noise and what the mode needs as lines. Operator shows the direction that
  decides; Engineer both. Not yet tested in game: row 468.
- **The desk and the amplifiers show when they send**: a red lamp lights and the meter's needle rises while the station is in use,
  for the fifth of the time voice is keyed; an amplifier's needle shows its output against its rating. Not yet tested in game:
  row 469.
- **The mast has a red warning light that flashes at night.** Not yet tested in game: row 470.
- **The coax glows with the signal it carries** while its station or an antenna is selected: bright at the station, dimmer along
  the run as the cable loses it, bright again past a powered amplifier. Not yet tested in game: row 471.
- **The radio terminal has a Waterfall tab**, as a software radio shows its band: each colonist's radio a streak as bright as it
  arrives, sending about a fifth of the time, over the noise the station hears, with the line voice needs. Each radio has its own
  spot on the band only to tell them apart; the radios have no channels yet. Not yet tested in game: row 472.
- **A new setting, Show the waterfall on the radio desk** (off by default, the owner's choice of 24 November 2026), gives the desk
  the Waterfall tab too. Not yet tested in game: row 473.

## [0.5.0] - 2026-11-24 - Draft

The radio terminal, linear amplifiers and a full-size ground-plane

### Added

- **The radio terminal**, a second station (the owner's go-ahead of 24 November 2026): a computer driving a software-defined radio, 10 W
  of its own with a very quiet receiver and a weak stub antenna. Its computer is itself a source of radio noise, so its antennas
  want to be a few metres away. Research software radio. Not yet tested in game: row 464.
- **Two linear amplifiers for the coax**: one up to 100 W, a heavy one up to 400 W. Each counts only for antennas whose cable passes
  through it, draws power and warms its room while its station is sending, and costs a little on receive. Not yet tested in game:
  row 465.
- **A full-size ground-plane**, about 2 dB better than the compact one for a little more steel; the taller pole fits both. Not yet
  tested in game: row 466.
- **A new setting, Radio terminals make radio noise** (on by default), to treat the terminal as quiet. Not yet tested in game: row 467.

### Changed

- **The whip is now called the compact ground-plane**, beside the new full-size one. Its figures are unchanged.

### Compatibility and limits

- Safe with a running save: nothing new is saved beyond the setting. Where the research screen puts the three new projects is not
  yet seen in game (row 464).

## [0.4.0] - 2026-11-24 - Draft

Upgrades for the radio desk and the antennas (experimental)

### Added

- **The radio desk, the whip and the mast take upgrades** (the owner's word of 24 November 2026, experimental): a receive
  preamplifier for the desk, a taller pole for the whip, a mast extension and a longer collinear for the mast. A builder fits each one
  from the building's Upgrades button. Not yet tested in game: rows 461 and 462.

### Changed

- **New pictures for the radio desk, the whip and the mast**, the owner's picks of 24 November 2026: a desk transceiver with its
  microphone, power supply, meter and logbook; a ground-plane whip with four radials; a guyed lattice mast with a white collinear on
  top. The antennas are drawn as the game draws trees, from above and a little in front, so they are drawn larger than their one
  cell, their guy wires and radials reaching past it. Not yet tested in game: row 463.

### Dependencies

- **Now needs D'thorga Framework**, for its building upgrades. Load it after D'thorga Framework.

### Compatibility and limits

- Safe with a running save: existing stations and antennas gain the Upgrades button. No new settings.

## [0.3.0] - 2026-11-24 - Draft

A line from each colonist's radio to its antenna, and a side view of the path

### Added

- **Selecting a colonist who wears a hand-held draws a line to the antenna their radio uses** (the owner's go-ahead of 24 November
  2026): coloured by the signal, thick where walls count, with a mark on each wall, door and patch of rock it meets. Operator adds
  the signal to spare, Engineer what each crossing cost. Not yet tested in game: row 459.
- **The link-budget window shows the path from the side**: the antenna and the colonist at their heights, wall height, the walls
  and rock between and the line over them. Not yet tested in game: row 460.

### Compatibility and limits

- Safe with a running save: nothing new is saved. No new settings.

## [0.2.0] - 2026-11-24 - Draft

Radio coverage painted on the map, and noise that fades out

### Added

- **Selecting the base station or an antenna paints its radio coverage on the map** (the owner's choice of 23 November 2026):
  brighter where a hand-held would have more signal to spare, dark red where it has no contact. It uses the same rules as a real
  contact, costs nothing until something is selected, and works only a little each frame. Not yet tested in game: row 456.

### Changed

- **Electrical noise now fades smoothly to nothing at the edge of its reach** instead of stopping dead (the owner's choice of 23
  November 2026). Links near the edge of a generator's reach get slightly better. Not yet tested in game: row 457.
- **The README no longer says the radio framework is on the back burner**: the owner reopened it on 23 November 2026, with
  performance as its first concern. No change in play.

### Compatibility and limits

- Safe with a running save: nothing new is saved. No new settings.

## [0.1.9] - 2026-11-21 - Draft

Radio's own Architect tab, and art records cleaned

### Changed

- **The radio base station, antennas and coax feedline are in their own Radio tab, inside the Architect's D'thorga tab** (the owner's choice of 21 November 2026, though radio is on the back burner: a tab only, nothing else changed). Without D'thorga Framework the Radio tab sits on the bar by itself. They were in the game's Misc tab.

### Compatibility and limits

- A layout change only. Safe with a running save. Not yet tested in game: row 301 of the test pass.

### Fixed

- **Art records no longer hold a path from the developer's PC.** The export records beside some textures named the full folder they were made in; they now name the file within the project. Nothing a player sees changes.

### Changed

- The README says the radio framework is on the back burner since 9 November 2026, and its dated checks note that RimWorld 1.6 was retired. No change in play.
- The README no longer repeats the player guide: the play test and how to read the signal are in the guide's Radio chapter. No change in play.
- The coax feedline atlas and its blueprint atlas have asset records: drawn in code on 19 October 2026, and meant to stay drawn by script. No change in play.

## [0.1.8] - 2026-11-13 - Draft

A clean background on the base station

### Fixed

- White left between the base station's parts is gone, and the signal button's edge is slightly cleaner. A fix to existing pictures only; the radio framework stays on hold.

### Save compatibility

- Safe to add to a running game: only pictures change.

### Compatibility and limits

- Not yet tested in game. Each picture was checked by eye on dark ground before and after, and the owner approved the set (13 Nov 2026).

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
