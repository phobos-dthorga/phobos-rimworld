# D'thorga Emergency Dosing changelog

Maintained from 5 November 2026. Draft entries record preparation, not Steam publication; nothing is published yet. Not yet
tested in game unless an entry says the owner tested it.

## [Unreleased]

## [0.1.1] - 2026-11-05 - Draft

Found by reading the load line 0.1.0 printed in the owner's game, then auditing the mod against the game's code. Not yet tested
in game. New refusal messages are agent-authored and held for the owner's review.

### Fixed

- Epinephrine would have been given for pain: its small relief was enough to index it, and doctors carry it for cardiac arrest.
  A drug must now take away at least 30% of the pain and score above zero to count at all, read from its effects, so relief as a
  side effect no longer qualifies. The index on the owner's mod list falls from 40 items.
- Recreational drugs outranked medical ones once their setting was on: go-juice and heroin score higher than morphine because
  they cost no consciousness. They are now a last resort, after medical painkillers and sedatives.
- Two different painkillers could stack, because only the item's own last dose was checked. A medical painkiller still working
  now blocks every other painkiller. A sedative may still follow in extreme pain.
- The load line listed recreational drugs as if they might be given. It now names medical and recreational relief apart and
  says the second is never given while the setting is off.
- The right-click gave no reason when the care setting or a mental state blocked a patient, and offered nothing at all when the
  cached patient list was empty or up to 250 ticks old. It now checks directly and names the reason, for patients in need only.
- A doctor with no working hands could take the job; it now needs Manipulation, as tending and feeding do. Babies are no longer
  dosed. An ingestible that is not a drug was always refused by the game's drug check; it is now simply taken.
- Path searches ran for every indexed item on every check. The rules now run first and reach is tested only for an item the rules
  allow.
- Wording: 60% is not the game's severe band, which starts at 40%; and 80% is described as extreme pain.

### Compatibility and limits

- Game-free rules pass 12 unit tests; the stacking check, the right-click reasons and the reach order live in game-facing code
  and have no unit test. Not yet tested in game.

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

- Effects a mod applies from its own code rather than its XML are invisible: the bleed increase of More Injuries' morphine (a
  linked condition) is not seen.
- A pawn up and working in pain is not dosed. Mechs do not dose. Search and Rescue's and More Injuries' doctors are not
  interrupted: a reserved patient is skipped.
- Game-free rules pass unit tests. Nothing is tested in game.
