# D'thorga Emergency Dosing changelog

Maintained from 5 November 2026. Draft entries record preparation, not Steam publication; nothing is published yet. Not yet
tested in game unless an entry says the owner tested it.

## [Unreleased]

### Compatibility and limits

- README brought up to date: partly tested in game, Framework 0.3.0 or later, slaves are dosed too, the setting's current name,
  and how it works beside D'thorga Triage.
- The owner reports doctors' dosing working in game on 0.3.0, from one case with penicillin and ketamine. Self-medication and the bleeding hold are not yet tested in game.
- A drug that gives its condition through its own mod's custom ingestion code is not read by the index. Vanilla Races Expanded's antibiotics are one, so doctors do not give them
  on their own.

## [0.3.1] - 2026-11-06 - Draft

No change in play. Not yet tested in game.

### Changed

- Who counts as the player's patient, the care-level test, the severity-rate sum and the danger reader now come from D'thorga
  Framework's health layer, which D'thorga Triage shares; the patient cache is the framework's. Same answers as before.

### Dependencies

- D'thorga Framework 0.3.0 or later.

## [0.3.0] - 2026-11-05 - Draft

Two additions the owner approved on 5 November 2026. Not yet tested in game. New text is agent-authored and held for the owner's review.

### Added

- Doctors give drugs that help fight disease: a patient down or in bed with a disease that can kill them, when it is winning the race against their immunity, gets a medical drug
  that adds at least 10% to immunity gain speed. Drugs are found by their effects, as painkillers are. The race is read from the game's own severity and immunity rates, and a
  patient already winning gets nothing, so stock lasts. Only doctors give these. A setting switches it off.
- A bleeding hold: a patient bleeding at 30% a day or more waits for a painkiller until that is under control, because some painkillers raise bleeding in ways this mod cannot
  see. A sedative in extreme pain and a drug for fighting disease do not wait. It applies to self-medication too.
- A refusal message for the hold, and a dev action listing the drugs for fighting disease.

### Changed

- The doctor's job now reads giving medication rather than pain relief, in the work giver, the job report and the main setting.

### Balance

- The 30% hold, the 10% least gain and the one day of margin in the race are authored, all in the rules Def.

### Compatibility and limits

- Medical drugs only: no teas or other drinks, and nothing that gives a disease, such as Biological Warfare's malaria pills. Drugs that prevent a disease, such as vaccines and
  penoxycyline, are not included: those are for the healthy, and penoxycyline can already be scheduled in a drug policy.
- Real antibiotics kill bacteria and do nothing for a virus. The game's immunity stat is a stand-in, so these drugs help against any disease, which is a gameplay simplification.
- Effects a mod applies through other means, such as stats by severity, are not read.
- Game-free rules pass 23 unit tests. The reading of a patient's disease and rates is game-facing and has no unit test. Not yet tested in game.

## [0.2.0] - 2026-11-05 - Draft

The owner's idea: someone above a pain threshold would seek out a painkiller on their own. Not yet tested in game. Settings text is
agent-authored and held for the owner's review.

### Added

- Self-medication: an adult colonist who is up and about at 40% pain or more goes and takes the best painkiller their drug policy allows, for
  joy or on a schedule, through the game's own ingest job. Medical drugs come before recreational ones. The doctors' safety rules apply,
  with a higher floor so a dose cannot drop a pawn on their feet (consciousness and breathing stay at 40% or more), and any painkiller still
  working blocks the next.
- Chemical interest and fascination lower the threshold to 30% and 20% and ignore the policy, as they do for joy in the game. Teetotalers and
  children never self-medicate, and sedatives are never taken by choice.
- A setting to switch it off, on by default, and a setting to let guests do the same, off by default, for mods such as Hospitality. The game gives
  guests no drug policy, so they take medical painkillers only.
- A dev action that logs what a selected pawn would take and why.

### Balance

- 40% pain is the owner's choice; the two trait offsets and the 40% capacity floor are authored.

### Compatibility and limits

- A pawn in bed or under medical rest, which includes anyone with a tended wound still healing, is left to the doctors, who step in from 60%.
- Addiction and overdose follow from the drug taken, by design. Check drug policies before enabling this on an existing colony.
- Uses a think-tree hook, no Harmony. Nothing is saved. Removing the mod while a pawn is fetching a drug is not tested.
- Game-free rules pass 17 unit tests. The eligibility checks and the item search are game-facing and have no unit test. Not yet tested in game.

## [0.1.3] - 2026-11-05 - Draft

No change in play. Not yet tested in game.

### Changed

- Refusal messages use a named placeholder for the drug, so translators can see what it means.
- The share of the bleed weight that counts while a patient is not bleeding moved into the rules Def, at the same value.

## [0.1.2] - 2026-11-05 - Draft

### Changed

- Two refusal messages spoke of settings rather than the world, under the owner's rule of 5 November 2026 that text a player reads
  in game stays in the game world. No behaviour change. The new wording is agent-authored and held for the owner's review. Not yet
  tested in game.
- The description now carries the disclaimer in the wording Ludeon's EULA requires. It said only that the mod is not official and not endorsed. No behaviour change.

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
