# D'thorga Medical Save Bridge changelog

Maintained from 3 November 2026. Draft entries record preparation, not Steam publication; nothing is published
yet. Not yet tested in game unless an entry says the owner tested it.

## [Unreleased]

### Changed

- The description now carries the disclaimer in the wording Ludeon's EULA requires. It said only that the mod is not official and not endorsed. No behaviour change.

### Compatibility and limits

- README: says plainly that the 0.1.2 build is not yet tested in game, and gains a Checks done section and a diagram of one load.

## [0.1.2] - 2026-11-04 - Draft

A native RimWorld 1.7 build, which now leads (owner, 4 November 2026); the 1.6 build stays as the minimum backwards support
until 1.6 is retired. No behaviour change. Not yet tested in game on either version since this build.

### Changed

- The assembly is built twice, against the 1.7.4998 reference on .NET 10 with Harmony 3 and against the 1.6 reference as before;
  the game loads its own version's folder (1.7/Assemblies or 1.6/Assemblies). Until now 1.7 loaded the 1.6 build.

### Compatibility and limits

- Harmony 3 on 1.7 accepted every patch at build time; whether each still applies in play is unchecked.

## [0.1.1] - 2026-11-03 - Draft

Follows the owner's first load in game, which showed the one gap below. **Tested in game by the owner on 3 November 2026**:
the same copy of the live colony loaded again with 0.1.1, and the load report added 92 hediffs upgraded from the game's Hediff
to HediffWithComps, MSE2 re-installed nothing, and the error list was identical to the 0.1.0 load. The owner then saved and reloaded: the bridge logged INERT, every failure
pattern stayed at zero and no new error group appeared. A new colony is not yet tested in game.

### Fixed

- Blood loss and hypothermia carried over from the save keep their severity. More Injuries re-classes those two vanilla hediff
  defs to the game's HediffWithComps so they can carry its comps, and 0.1.0 only upgraded classes that came from a newly added
  mod, so it left them to Medical System Expansion 2, which re-makes such hediffs from scratch: in the owner's load 79 blood-loss
  and 8 hypothermia hediffs came back fresh, their severity gone. The bridge now upgrades any carried-over hediff whose def's class
  has since become a subclass of the saved one, whichever mod the class belongs to.

### Compatibility and limits

- Without MSE2, 0.1.0 left those hediffs on the plain class, without More Injuries' comps, until they healed. Now they load as
  More Injuries expects on either setup.

## [0.1.0] - 2026-11-03 - Draft

Owner's request, from a handoff investigating why More Injuries (Continued) broke the PG17 colony when added to a save that
runs Medical System Expansion 2.

**Tested in game by the owner on 3 November 2026**, first load only: a copy of the live colony (an autosave of PG17, RimWorld
1.7.4995, about 330 mods, Combat Extended, MSE2) loaded with More Injuries, both its add-ons and No More Lethal Damage Threshold
switched on in the same step. The log said ACTIVE with Human (8 new parts), 3,811 parts re-numbered, 1,687 injuries and 1,071
missing parts upgraded, 517 wipes prevented, no hediff on a part newer than the save, no added part off its recipe's parts, and
none of the failure patterns; the error list matched the control run without More Injuries. The owner reported the colony
playing well, with More Injuries' own migration dialogs and new surgeries in place. Save-and-reload and the new-colony check
are still to run.

### Added

- The bridge itself: on the first load of a save written without More Injuries (or any other mod that adds body parts), every
  saved body part reference is re-numbered from the old part list to the new one, so wounds, missing parts, prosthetics, bionics,
  MSE2 modules and surgery bills keep the part they had. Nothing is written to disk until the player saves.
- Old wounds and missing parts are created as More Injuries' own classes on that load, carrying the bleeding state a live wound
  would, so Medical System Expansion 2 has nothing to re-install and no lost limb throws an error.
- More Injuries' first-load clean-up, which removes every wound and missing part from every pawn and sets dead pawns back to
  Mobile, is held off on that load by clearing its flag as it is set.
- A load report in the log: the ACTIVE or INERT decision and why, counts of what was done, then a check of every pawn, dead or
  alive, for hediffs sitting on parts the save never had and for added parts sitting off the parts their recipes allow.
- Two dev-mode actions under D'thorga: log body part indices (each part's old number, or the mod it is new from), and check every
  pawn's parts.

### Dependencies

- Harmony. Load after More Injuries and Medical System Expansion 2; neither is required, and the bridge references neither
  assembly. Without More Injuries it only re-numbers parts for whatever other part-adding mod the save lacked.

### Save compatibility

- Safe to add; it saves nothing. Safe to remove at any time. After one save with More Injuries it is inert for that colony.

### Compatibility and limits

- A mod that moves or deletes an existing body part, rather than adding one, is beyond the re-numbering rule.
- A newly added mod that inserts records of vanilla part defs (a second kidney, say) is not seen as new, because the bridge tells
  new parts by the mod that defines the part def.
- More Injuries' add-ons (Organ Failure, Medieval Solutions) were not studied. Keep them off for the first load.
- The bleeding-state defaults written for upgraded wounds were read from More Injuries 1.6.0.2. A later version that renames them
  loads such wounds with its own defaults instead, as it would without the bridge.
- Built for RimWorld 1.6, loaded on 1.7 too. The hooks it uses exist in both; a missing one switches that part off with a log line.
