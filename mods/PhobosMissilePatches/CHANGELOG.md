# D'thorga Missile Patches changelog

Maintained from 7 November 2026. Draft entries record preparation, not Steam publication; nothing is published
yet. Not yet tested in game unless an entry says the owner tested it.

## [Unreleased]

## [0.1.0] - 2026-11-07 - Draft

Owner's request: a strategic warhead from InterRim Ballistic Missile hung the game at detonation until Windows closed it. Read from the owner's log and the mods' code; not yet tested in game.

### Added

- The mod: fixes for InterRim Ballistic Missile, loaded only while it is active, built on D'thorga Framework.
- A strategic warhead's blast is spread over frames: the struck map is crossed a slice per frame from the centre outward, within a time budget, so the game keeps responding. Everything the warhead would have destroyed is still destroyed, by the missile mod's own rules.
- During each slice the game's room and region rebuild is held and run once at the slice's end, instead of after every fallen wall. That rebuild, triggered by placing each building's leavings, is what made the one-frame blast grow slower with every building.
- A message when a blast runs longer than two seconds, and one when it has spent itself.
- Settings: the time per frame (2 to 50 ms, default 12), a switch for the whole fix, and a switch for holding the room rebuild.
- Dev actions under the D'thorga category: strike the current map as an extinction-class warhead (destroys everything there), log the blast queue, drop queued blasts. Each finished blast logs one line with counts, seconds, frames and swallowed errors.

### Fixed

- The game no longer stops responding for minutes when a strategic missile lands on a large map. In the owner's game of 7 November 2026 the one-frame blast was still running after two minutes when Windows closed the game as hung.
- Things already destroyed when the blast reaches them are skipped instead of logging a red error each.

### Save compatibility

- Safe to add and to remove; nothing is saved. A blast still in progress when the game is saved is forgotten on load, so what it had not reached survives.

### Compatibility and limits

- The fires, terrain changes and cloud the warhead makes still happen in the frame the screen reaches white, as the missile mod does them.
- Needs Harmony and D'thorga Framework. The missile mod ships only a 1.6 build; this mod is built for 1.7 and 1.6.
- The missile mod's Workshop description could not be read (region-blocked), so its licence is not recorded; nothing of it ships here.
