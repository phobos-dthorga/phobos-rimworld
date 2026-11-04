# D'thorga Burns and Smoke changelog

Maintained from 4 November 2026. Draft entries record preparation, not Steam publication; nothing is published yet. Not yet
tested in game unless an entry says the owner tested it.

## [Unreleased]

## [0.2.0] - 2026-11-04 - Draft

The owner's first look at 0.1.0 in game (an incendiary raid on 7 Septober 5512: pawns with limbs, a jaw and a head burned
off, burns on head, neck, torso and shoulders) found every one of them at "burns (minor)" or with no burns condition at all,
and never in burn shock. The mod ran without errors; it was counting almost nothing. Not yet tested in game.

### Fixed

- Skin shares: the mod subtracted child parts from a game figure that already leaves them out, so the torso, neck, head and
  both shoulders counted as zero skin and only arms, legs and feet ever added up. Shares now come from the including-children
  coverage and sum to 1 (torso 0.405, each leg 0.126, each arm 0.080, head 0.028), as the design record intended.
- Parts burned off counted for nothing, and the burns on them vanished with them, so the worst-burned pawns showed the least.
  A part burned off now counts as its whole skin burned while the stump is fresh.
- The load line threw at startup through the framework (D'thorga Framework 0.1.1 fixes it).
- The burns condition said burn shock starts past a fifth of the body; the rule is 15%. The wording now says about a sixth.

### Changed

- Burn shock always shows its compensated stage once 15% of the body is burned, even for a young pawn whose Baux score is
  below the no-death mark, so the fluids job is visible; the death clocks are still Baux-driven.

### Balance

- A tended stump fades out of the burned fraction over 15 days (authored).

### Dependencies

- D'thorga Framework 0.1.1 or later.

### Compatibility and limits

- Burn amputations still do not bleed (the game's own rule for burns). Dead pawns are not recomputed; a corpse keeps the
  condition it died with.
- Game-free rules pass 18 unit tests. Nothing is tested in game.

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
- Built twice: against the 1.7.4998 reference on .NET 10, which leads, and against the 1.6 reference; the game loads its own
  version's folder (1.7/Assemblies or 1.6/Assemblies).

### Save compatibility

- Safe to add mid-game: the conditions are derived from the burns a pawn already has. To remove, press the settings button,
  save, then switch the mod off.

### Compatibility and limits

- With More Injuries: its fire-inhalation worker is removed from its worker list (owner's choice), carbon monoxide and a
  closing airway feed its hypoxia and brain damage through its own XML handlers, and the tracheotomy waits for Emergency
  Medicine. With Organ Failure, a closed airway runs into its respiratory-failure chain instead of killing outright.
- Humanlike pawns only in this version. Combat Extended's burn from incendiary ammunition counts as a burn.
- Game-free rules pass 14 unit tests. Nothing is tested in game.
