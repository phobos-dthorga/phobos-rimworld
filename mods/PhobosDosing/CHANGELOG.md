# D'thorga Emergency Dosing changelog

Maintained from 5 November 2026. Draft entries record preparation, not Steam publication; nothing is published yet. Not yet
tested in game unless an entry says the owner tested it.

## [Unreleased]

## [0.1.0] - 2026-11-05 - Draft

The owner's request after finding that no doctor ever gives a painkiller or sedative on their own, Search and Rescue or not.
Not yet tested in game. Settings text and refusal messages are agent-authored and held for the owner's review.

### Added

- Doctor work "give pain relief": a resting patient (down or in bed) in severe pain gets the painkiller with the strongest
  relief net of side effects, from the doctor's inventory first, then the nearest stack; past the pain-shock band a sedative
  when nothing lighter can be given. The patient takes it through the game's own toils, so the item's effects apply exactly
  as its mod wrote them.
- An index of every pain-relieving ingestible, built from the Defs at startup (owner's rule: effects, not names): pain,
  consciousness, breathing and bleeding from the outcome condition's stage at the dose, addictiveness and category from the
  drug comp. Any mod's drugs qualify; a mod can opt an item out with PhobosDosingExtension.
- Safety: no second dose while the first still works, none during an overdose, none that reaches a dangerous stage of its
  own condition, none whose offsets leave consciousness or breathing under 15%, none to a patient under "no medicine",
  recreational drugs only by setting, and the game's own willingness check (teetotalers, precepts) unless overridden for
  medical drugs.
- Settings: on or off, the minimum medical care level, recreational drugs for pain, drugs for patients who refuse them, the
  display mode. Two dev actions: log the index, log the decision for selected pawns.
- Every refusal says why, in the right-click menu.

### Dependencies

- D'thorga Framework 0.1.1 or later (required). No other mod. Built twice: against the 1.7.4998 reference on .NET 10, which
  leads, and against the 1.6 reference.

### Save compatibility

- Nothing is saved. Safe to add or remove at any time.

### Compatibility and limits

- Effects a mod applies from its own code rather than its XML are invisible: More Injuries' epinephrine (its benefit is in
  its code) and the bleed increase of its morphine (a linked condition) are not seen. Epinephrine stays with More Injuries
  and Search and Rescue, which already give it.
- A pawn up and working in pain is not dosed. Mechs do not dose. Search and Rescue's and More Injuries' doctors are not
  interrupted: a reserved patient is skipped.
- Game-free rules pass unit tests. Nothing is tested in game.
