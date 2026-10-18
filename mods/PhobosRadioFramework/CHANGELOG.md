# D'thorga Radio Framework changelog

Maintained from 19 October 2026. Draft entries record preparation, not Steam publication; nothing is
published yet. Not yet tested in game unless an entry says the owner tested it.

## [Unreleased]

### Added

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
