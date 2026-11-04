# D'thorga Vehicle Patches changelog

Maintained from 2 November 2026. Draft entries record preparation, not Steam publication; nothing is published
yet. Not yet tested in game unless an entry says the owner tested it.

## [Unreleased]

## [0.3.2] - 2026-11-04 - Draft

A native RimWorld 1.7 build, which now leads (owner, 4 November 2026); the 1.6 build stays as the minimum backwards support
until 1.6 is retired. No behaviour change. Not yet tested in game on either version since this build.

### Changed

- The assembly is built twice, against the 1.7.4998 reference on .NET 10 with Harmony 3 and against the 1.6 reference as before;
  the game loads its own version's folder (Mods/HarrierVtol/1.7 or Mods/HarrierVtol/1.6). Until now 1.7 loaded the 1.6 build.

### Compatibility and limits

- Vehicle Framework ships only 1.6, so both builds reference that assembly, as the game itself loads it on 1.7. Harmony 3 accepted the patches at build time; whether they still apply in play is unchecked.

## [0.3.1] - 2026-11-03 - Draft

Owner's request: a review of the Harrier's code for sortie, RimWorld 1.7 and Combat Extended faults. Not yet tested in game.

### Fixed

- A sortie's map could close under the jet. The game removes a site's map once no colonist is left on it, and its check can't see a
  pilot inside a vehicle. Vehicle Framework and the Harrier's flight framework each cover part of that gap, but the framework covers
  only the game's own kinds of site. Over a Real Ruins site, a RimMarket convoy battle or any other mod's site with its own rule, the map
  could be removed with the jet still in the air. Now any vehicle of yours with a colonist aboard keeps the map open, in the air or on
  the ground, whichever mod made the map.

### Compatibility and limits

- The fix lives in the Harrier's folder, so it runs only while the Harrier is active, but it covers every Vehicle Framework vehicle of
  yours, the Hind and Mongoose included.
- Checked but not changed: every game, Vehicle Framework, Harmony and Unity member the Harrier's three assemblies call exists in
  RimWorld 1.7 (by name and parameter count), and the Combat Extended members its code reaches by name exist in the current CE build.
  The review's record is in docs/development/vehicle-patches.md.

## [0.3.0] - 2026-11-03 - Draft

Owner's request. Not yet tested in game.

### Added

- RimThunder's heavy casting (its in-game name is heavy compoment) can now be made at the rotary forger from The Dead Man's Switch - Expedition, as
  well as at the electric smelter and assembly platform. It costs the same 100 steel and still gives a lump of steel slag.

### Dependencies

- None added. It switches on only while RimThunder is active and the forger exists.

### Save compatibility

- Safe to add. Before removing this mod, delete any heavy casting bills you added at a forger, because the forger may no longer list that recipe.

### Compatibility and limits

- The forger saves no colonist time on this recipe. Loading and collecting take a colonist about 600 ticks in all, against about 500 at the smelter, because the recipe is so short.
- Without RimThunder's own parts module, which Aoba.Motorization.Engine replaces, the recipe isn't there and this does nothing.

## [0.2.0] - 2026-11-03 - Draft

Owner's request. Not yet tested in game.

### Added

- The Harrier's five bombs (Mk. 82 and the four Mk. 84s) and its three 57 mm rockets (HE, HEAT and AP) can now be made at The Dead Man's
  Switch's ammunition fabricator, as well as at the machining table. A colonist loads the machine and later collects the result; the machine
  works alone in between, at twice the speed of a colonist at the table. Costs and research are unchanged.
- The Hind and Mongoose use the same three rocket recipes, so their rockets gain this too.

### Dependencies

- None added. It switches on only while the fabricator exists, which The Dead Man's Switch provides only with Combat Extended.

### Save compatibility

- Safe to add. Before removing this mod, delete any bomb or rocket bills you added at a fabricator, because the fabricator may no longer list those recipes.

### Compatibility and limits

- The fabricator drops what it makes beside itself, whatever the bill's storage setting says, and haulers take it from there.
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
