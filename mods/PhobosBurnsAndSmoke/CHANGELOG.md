# D'thorga Burns and Smoke changelog

Maintained from 4 November 2026. Draft entries record preparation, not Steam publication; nothing is published yet. Not yet
tested in game unless an entry says the owner tested it.

## [Unreleased]

## [0.1.0] - 2026-11-04 - Draft

The owner's request after an incendiary raid showed burns and smoke doing almost nothing beside More Injuries' wounds. Not
yet tested in game. Every condition's labels and descriptions are agent-authored and held for the owner's review.

### Added

- Burned body fraction: every burn on the skin, weighted by the part's share of the skin from the game's body coverage,
  recomputed every 300 ticks. Stages bring pain, cold sensitivity, hunger, rest loss and strain on the heart.
- Burn shock from 15% burned, heading for a target set by the revised Baux score (age plus percent burned plus 17 with an
  inhalation injury), lowered by tended burns and bed rest; tendable, so tending it stands in for fluids. Death clocks at the
  severe and terminal stages.
- Airway burns after burning or burns to the head and neck: swelling rises to a peak at 8 hours, holds to 24 and resolves by
  48; the top stage stops breathing. A tracheotomy surgery removes it and leaves a tracheostomy for five days.
- Smoke read from D'thorga Framework (Combat Extended's own inhalation while its smoke runs, otherwise the game's smoke):
  lasting lung damage that invites pneumonia, and an acute smoke condition when Combat Extended's is not running.
- Carbon monoxide from the framework's room gas, which fires make in closed rooms; it clears with a half-life of 5.3 hours
  under a roof and 3 in the open, and ignores filter masks. Without the framework's gas it is inferred from smoke in an
  enclosed space.
- Settings: a toggle per condition, the display mode, the Combat Extended acute-smoke option, and an uninstall button.
- Dev actions: log a pawn's burn load and shock readout, start an airway burn, remove every condition.

### Dependencies

- D'thorga Framework 0.1.0 (required). More Injuries, More Injuries: Organ Failure and Combat Extended optional.

### Save compatibility

- Safe to add mid-game: the conditions are derived from the burns a pawn already has. To remove, press the settings button,
  save, then switch the mod off.

### Compatibility and limits

- With More Injuries: its fire-inhalation worker is removed from its worker list (owner's choice), carbon monoxide and a
  closing airway feed its hypoxia and brain damage through its own XML handlers, and the tracheotomy waits for Emergency
  Medicine. With Organ Failure, a closed airway runs into its respiratory-failure chain instead of killing outright.
- Humanlike pawns only in this version. Combat Extended's burn from incendiary ammunition counts as a burn.
- Game-free rules pass 14 unit tests. Nothing is tested in game.
