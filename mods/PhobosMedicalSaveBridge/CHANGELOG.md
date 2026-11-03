# D'thorga Medical Save Bridge changelog

Maintained from 3 November 2026. Draft entries record preparation, not Steam publication; nothing is published
yet. Not yet tested in game unless an entry says the owner tested it.

## [Unreleased]

## [0.1.0] - 2026-11-03 - Draft

Owner's request, from a handoff investigating why More Injuries (Continued) broke the PG17 colony when added to a save that
runs Medical System Expansion 2. Not yet tested in game.

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
