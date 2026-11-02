# D'thorga Vehicle Patches changelog

Maintained from 2 November 2026. Draft entries record preparation, not Steam publication; nothing is published
yet. Not yet tested in game unless an entry says the owner tested it.

## [Unreleased]

## [0.2.0] - 2026-11-03 - Draft

Owner's request. Not yet tested in game.

### Added

- The Harrier's five bombs (Mk. 82 and the four Mk. 84s) can now be made at The Dead Man's Switch's ammunition fabricator, as well as at
  the machining table. A colonist loads the machine and later collects the bomb; the machine works alone in between, at twice the speed
  of a colonist at the table. Costs are unchanged.

### Dependencies

- None added. It switches on only while the fabricator exists, which The Dead Man's Switch provides only with Combat Extended.

### Save compatibility

- Safe to add. Before removing this mod, delete any bomb bills you added at a fabricator, because the fabricator will no longer list those recipes.

### Compatibility and limits

- The fabricator drops a finished bomb beside itself, whatever the bill's storage setting says, and haulers take it from there.
- If a colonist is interrupted while loading, the machine drops what it was given, and the job starts again. A chemfuel-heavy bomb takes
  many trips, so this is more likely than with ordinary ammo. Nothing is lost.

## [0.1.0] - 2026-11-02 - Draft

First version, for Harrier VTOL Attack Aircraft by potemkinvillager. Owner's request. Not yet tested in game.

### Fixed

- The Harrier's flight framework failing to load fully while RimPacts is active. RimPacts patches a Vehicle Framework method in a way
  that breaks the next patch of it, so more than 50 framework patches never loaded, among them boarding, refuelling and landing. An
  ordinary patch now takes the place of RimPacts' one and calls its filter, so RimPacts' landing rule still works.
- The log warning Parsed 2.5 as int from the jet's fuel tank. The game already used 2, so only the warning goes.

### Balance

- The jet's build cost: 400 steel, 120 plasteel, 30 components, 1 RimThunder turbine or 3 engine blocks, 4 exhausts, 4 suspensions,
  4 wheels, 3 fuel tanks, 1 transmission, 2 alternators, 2 batteries and 6 wiring. It was 450 steel, 150 plasteel, 25 components,
  6 advanced components and 1 engine block. Authored; the design record compares value and mass with the Hind and Mongoose.
- With RimThunder the blueprint also needs turbocharged research, so it can't appear before its engine can be made.
- Construction skill 10, up from 8, as for the Hind and Mongoose.
- Drop Tanks upgrade: 2 fuel tanks, 20 steel and 2 components, in place of 60 steel and 2 components.
- Under Combat Extended, bomb ingredients now come much closer to the bombs' real weights. The Mk. 82 and incendiary Mk. 84 stop short on
  purpose (owner's choice), at one shared ratio of about 0.23 FSX or prometheum and 3.5 chemfuel per kg of real fill. Without RimThunder,
  cases cost steel in place of heavy castings. Selling prices now follow from the ingredients.
- The texts and figures are agent-written, held for the owner's review.

### Dependencies

- None required. It does nothing without the Harrier. One small assembly, built for RimWorld 1.6 and loaded on 1.7 too, loads only while
  the Harrier is active. It references the installed Vehicle Framework and copies nothing.

### Save compatibility

- Safe to add. A jet already built keeps working, and only new builds pay the new cost. Bombs already made are unchanged.
- Safe to remove. Costs return to the Harrier's own, and with RimPacts active its framework fails to load fully again.

### Compatibility and limits

- A second fault in the framework could have been hidden behind this one, because it stopped loading at the first. Only running the game
  will show it.
- Not checked: whether a shot-down Harrier raider from VTOL Raids leaves a share of the new, richer cost.
- If RimPacts stops using a generic patch, the fix finds nothing to do. If RimPacts renames what it reads, it logs a warning and changes nothing.
