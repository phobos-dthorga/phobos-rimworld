# D'thorga Triage changelog

Maintained from 6 November 2026. Draft entries record preparation, not Steam publication; nothing is published yet. Not yet
tested in game unless an entry says the owner tested it.

## [Unreleased]

## [0.2.0] - 2026-11-07 - Draft

Operations wait while something holds the patient from surgery. Not yet tested in game.

### Added

- A patient that another mod says can't be operated on for now (D'thorga Medical Equipment 0.4.1's hyperbaric chamber is the
  first) is held: nothing new is queued for them, a bill already there stays, and doctors taking the worst case first skip them.
  Their inspect line and one message per condition say the operation must wait and why, in the words of whatever holds them,
  such as moving them to a bed. A held patient is tagged by their clock, never expectant (agent choice, 7 November 2026).
- A Workshop preview picture: a generated background the owner picked (7 November 2026), with the name added by script. No change in play.

### Dependencies

- D'thorga Framework 0.4.0 or later, for its surgery holds.

### Compatibility and limits

- Closes the limit D'thorga Medical Equipment 0.4.0 recorded: Triage queued an operation for a patient in the chamber and said it
  was queued while no doctor could start it. The game's own operate giver may still look at a held patient; Medical Equipment's
  rule refuses the job.

## [0.1.2] - 2026-11-07 - Draft

The care-choice button gets its own icon. Not yet tested in game.

### Changed

- The "Doctors may operate" button on a patient shows a scalpel across a red cross, in place of the game's industrial medicine
  icon (generated art, the owner's pick, 7 November 2026; it passed the near-copy check, and the record beside the texture says how
  it was made). If the texture is ever missing, the button falls back to the medicine icon, never the red error cross.

### Dependencies

- D'thorga Framework 0.3.1 or later, unchanged: the icon goes through its existing icon setting.

## [0.1.1] - 2026-11-06 - Draft

Fixes found in a documentation review the same day, before any play. Not yet tested in game.

### Fixed

- A dying patient whose condition no operation removes (carbon monoxide poisoning, burn shock, a widespread burn) was tagged
  expectant, and a message said nobody could operate. Such a condition is now tagged by its clock like any other, nothing is
  queued and no message is sent: oxygen, tending and time are its treatment, not a bill.
- "For any repair" did the same as "to save life or a limb". It now also queues an operation for a condition that neither kills
  nor cripples, when one removes it.
- Drug addictions and gene-driven chemical dependencies are left out entirely: never operated on, tagged or named in a message
  (owner, 6 November 2026, after first-start messages about psychite dependency on 0.1.0). Found by what the game says the
  condition is, an addiction or a chemical dependency, never by name; a rules Def flag turns it off.
- The rules Def's urgentHours was never read. It is now the score's time scale: within a tag, a patient with that many hours
  left gets half the urgency of one with none (24 hours, as before).

### Save compatibility

- Nothing saved changes. The map component the mod adds is saved by name, so removing the mod prints one harmless line per map
  on the next load, besides the one about its game component; 0.1.0's notes said only the latter.

## [0.1.0] - 2026-11-06 - Draft

The owner's ask of 6 November 2026: doctors should take over life-saving surgery on their own when given the agency, with
settings, and a triage score should decide which bills are done first. Not yet tested in game.

### Added

- Doctors queue the operation a killing condition needs, by what the condition does and what each recipe removes, never by
  name: the operation most likely to succeed among those the colony can do, with a skilled doctor free, the ingredients in
  stock, the care level allowing the medicine, and a chance of death on the table within the setting. Bills the doctors queue
  are withdrawn once the condition is gone; the player's bills are never touched.
- A button on each colonist, slave, prisoner and guest: how far the doctors may go without orders (never, to save life, to save
  life or a limb, for any repair). Defaults by group in the settings: life-saving for colonists, slaves and prisoners, never for
  guests.
- Triage tags (immediate, urgent, delayed, minor, expectant) on the inspect line, from how soon each condition kills and how
  far the vital functions have fallen, and a copy of the game's operate work giver that takes the worst case first.
- Messages when an operation is queued and, once per person and condition, when nobody can do a needed one and why.
- Settings: on or off, the defaults by group, the highest chance of death accepted, rare tools, violations on other factions'
  people, messages, the order of work, the display mode.
- Dev actions: log the operations indexed, log the triage of selected pawns, run the planner now.

### Dependencies

- D'thorga Framework 0.3.0 or later (its health layer and patient comp). No Harmony.

### Save compatibility

- Saves only the list of bills it queued, by reference. Safe to add to a running colony; safe to remove, the bills it queued
  staying as ordinary bills, with one line about a missing game component on the next load.

### Compatibility and limits

- Operations are found through recipes that set removesHediff or carry this mod's extension; a recipe whose worker removes a
  condition another way is not seen until its author or a patch adds the extension.
- Field care (tending, tourniquets, transfusions, CPR, airways) is left to Search and Rescue and More Injuries; this mod only
  queues operations and orders surgical work. Search and Rescue's rescue lanes and reservations are respected through the
  game's own reservation checks.
- A condition's hours to death are an estimate: a death clock's mean, the climb to a lethal severity at today's rate, or the
  game's bleed-out figure, whichever is soonest.
- The doctors' order of work is read from the 1.7 code; the 1.6 job giver's body could not be read and is assumed the same.
