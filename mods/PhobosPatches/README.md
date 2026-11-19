# D'thorga Patches

Fixes and patches for other authors' mods, each switched on only while that mod is active. It copies nothing from them. It covers
**Harrier VTOL Attack Aircraft** by potemkinvillager, **InterRim Ballistic Missile** by Psionic Zephyr, **RimPacts**, **Visit
Settlements** by Nina.Goblin, **Vehicle Framework**, **Vanilla Expanded Framework**'s outposts, **Vanilla Factions Expanded -
Classical**, **Spaceship Turret Hull** by MK and **Combat Extended**, and RimThunder's heavy casting at The Dead Man's Switch's rotary
forger.

One of House D'thorga's mods (also spelt dthorga). It replaces two earlier mods, merged into it on 7 November 2026: **D'thorga
Vehicle Patches** and **D'thorga Missile Patches** ([why](../../docs/development/mod-consolidation.md)).

Version 0.25.0, built 19 November 2026 for House D'thorga. **Partly tested in game:** the Vehicle Framework fires part (owner,
10 November 2026: the stuck fires went out) and one missile strike on an earlier version (owner, 7 November 2026). Everything else is
not yet tested in game.

**How to play with it is in the player guide's [Patches chapter](../../docs/player-guide/patches.md).** Each version's changes are in
the [changelog](CHANGELOG.md).

## Read this first (gotchas)

- **Needs Harmony and D'thorga Framework 0.97.0 or later** (0.106.0 or later for the Harrier's flight-control rule). **Load it after
  the mods it patches**: the About file asks for this, so RimSort does it for you. **Switch it on yourself**: nothing else requires
  it. It does nothing without the mods it patches.
- **Each part loads only while its own mods are active** (an `IfModActive` folder per patched mod): the Harrier part with the Harrier
  (its bomb changes also need Combat Extended, and the fabricator bills The Dead Man's Switch's fabricator); rearming with the Harrier,
  Hind or Mongoose; the forger bill with RimThunder and the forger from The Dead Man's Switch - Expedition; the turret hulls with
  Spaceship Turret Hull and Combat Extended together; the ammo guard with Combat Extended; the missile part with InterRim Ballistic
  Missile; one part each for RimPacts, Visit Settlements, Vehicle Framework and Vanilla Factions Expanded - Classical (that one needs
  D'thorga Framework 0.168.0 or later); the outposts part with Vanilla Expanded Framework and Vehicle Framework together. A player running none of them sees nothing.
- **RimWorld 1.7 only.** It builds against Vehicle Framework's own 1.7 build (14 November 2026).
- **Switching from D'thorga Vehicle Patches or D'thorga Missile Patches: switch both off and this one on, in the same change.** The
  About file lists them as incompatible, so RimSort warns you. Your save needs nothing; the missile settings carry over by themselves.
- **Safe to add to a running save.** A Harrier you already built keeps working and gains its new parts at full health; only new builds
  pay the new cost. Bombs already made stay as they are.
- **Before removing it:** delete any bomb or rocket bills you added at an ammunition fabricator and any heavy casting bills at a rotary
  forger; **with Combat Extended, use up or sell your bomb cases and bomb fills**, delete their bills, and **deconstruct turret hulls built under this
  mod** (not yet tested either way). Without it the Harrier's costs go back to its own, and with RimPacts its flight framework fails
  to load fully again. Removing IRBM or RimPacts later is clean once the save has been loaded with 0.16.8 or later.
- **Decide before a missile strike:** the blast settings below. Choices that cost speed say so in amber. **After a strike, wait for the
  message that the blast has spent itself before saving**: a blast still running when you save is forgotten on load.
- **Harrier disassembly (temporary trace):** the Harrier cannot be taken apart yet. **Try one disassembly, on a copy of your save, and
  stop there**: each attempt runs the first half of the removal again. Its log trace is a setting, on by default.
- **Harrier flight controls:** with avionics, hydraulics or wiring below about 35% the jet cannot take off until repaired. On a hot
  day a heavy Harrier lands on a runway instead of hovering onto its pad, by **D'thorga Framework's** setting "When an aircraft is too
  heavy or too hot to hover onto its pad".
- **Harrier dodge and chaff (0.21.0):** the jet dodges as its author set while sound; shot-up flight controls or a dead warning receiver
  make it easier to hit. Load **countermeasure cartridges** (D'thorga Framework's parts bench) into its cargo for its **Chaff and flares**
  button in the air.
- **Rearming keeps to one kind of ammunition** (Harrier, Hind, Mongoose). To change kind, choose it on the weapon.
- **With Combat Extended, allow the two bomb cases in machining bay hoppers** that used to take heavy castings, and **rebuild turret
  hulls built before**, while the ship is landed.
- **With RimPacts, a world war attack must win within 3 days of accepting it**, or control of about a dozen border towns passes to
  the enemy. That is RimPacts' rule; the quest shows the countdown. **Strike and attack early.**
- **With Visit Settlements, pawns already lost to a closed map stay lost**: load a save from before.
- **With Vanilla Factions Expanded - Classical, a save made during a scorpion raid now loads with the raid working** (temporary). **If
  you saved again after such a save loaded broken, before 0.25.0, load the save made before that**: the raid's orders are gone from it.
- **Keep Prison Labor up to date**: the guard for its first 1.7 build was removed in 0.19.2, as Prison Labor 1.7.1 fixed the crash.

## What it does

Each part in a line or three; the guide chapter has the figures, tables and how to use each.

### Harrier VTOL Attack Aircraft

- **A realistic build cost**: RimThunder's turbine (or 3 engine blocks), four nozzles, landing gear, fuel tanks, wiring and more, and
  Construction 10 ([building it](../../docs/player-guide/patches.md#building-it)). Drop Tanks take real fuel tanks.
- **Fourteen parts** in place of four, with flight controls that ground the jet when damaged
  ([parts](../../docs/player-guide/patches.md#its-parts-and-flight-controls)).
- **Loads fully beside RimPacts**: a RimPacts patch had stopped more than 50 of the jet's framework patches loading.
- **A sortie's map stays open under the jet**, whichever mod made it, and a sortie onto a RimPacts battle you have not joined says
  why it is refused ([sorties](../../docs/player-guide/patches.md#sorties)).
- **Rearming keeps to one kind** ([rearming](../../docs/player-guide/patches.md#rearming)); **too heavy to hover** follows the
  Framework's setting ([hovering](../../docs/player-guide/patches.md#hovering-onto-its-pad)).
- **A temporary disassembly trace** ([taking it apart](../../docs/player-guide/patches.md#taking-it-apart-temporary)).

### Combat Extended, The Dead Man's Switch and RimThunder

- **Bombs cost what they weigh**, each built on a stackable steel **bomb case** of its own
  ([bombs](../../docs/player-guide/patches.md#bombs-under-combat-extended)).
- **Bombs and rockets at the ammunition fabricator**
  ([fabricator](../../docs/player-guide/patches.md#bombs-and-rockets-at-the-ammunition-fabricator)).
- **Heavy castings 12 at a time at the rotary forger**
  ([forger](../../docs/player-guide/patches.md#heavy-castings-at-the-rotary-forger)).
- **A temporary guard** drops ammo set entries that would stop Combat Extended's whole ammo setup.
- **Colonists refill The Dead Man's Switch's drone platforms with steel by themselves**
  ([drone platforms](../../docs/player-guide/patches.md#drone-platforms)).

### Spaceship Turret Hull

All seven armed hulls load and fire Combat Extended ammunition like its own turrets
([turret hulls](../../docs/player-guide/patches.md#turret-hulls-under-combat-extended)).

### InterRim Ballistic Missile

- **A strategic blast is spread over moments** instead of freezing the game, and ends in seconds; overhead mountain shelters what is
  under it; no flood of "Area revealed" letters ([a strategic blast](../../docs/player-guide/patches.md#a-strategic-blast)).
- **The extinction-class warhead strikes with a fireball, a blast zone and a heat zone**, with Dubs Rimatomics' fallout where it is
  active ([the warhead](../../docs/player-guide/patches.md#the-extinction-class-warhead)).
- **With Combat Extended, its close-in weapon systems shoot down CE's mortar and artillery shells.**
- Dev actions and limits: [messages, limits and dev actions](../../docs/player-guide/patches.md#messages-limits-and-dev-actions).

### RimPacts

An ally's assault leaves empty vehicles alone (temporary); after your missile strike it faces the town's garrison from before the
strike; a world war quest shows its deadline ([RimPacts](../../docs/player-guide/patches.md#rimpacts)).

### Visit Settlements

A visited settlement's map stays open while your people can still act there or a battle is on, and its food drops no longer fail on
every tick ([Visit Settlements](../../docs/player-guide/patches.md#visit-settlements)).

### Vanilla Factions Expanded - Classical

A save made during a scorpion raid loads with the raid still working (temporary)
([Classical](../../docs/player-guide/patches.md#vanilla-factions-expanded---classical)).

### Vehicle Framework and outposts

Fires a vehicle could never put out are put out; repairs and upgrades show a progress bar (temporary); a caravan with vehicles cannot
found an outpost through "create outpost"
([outposts](../../docs/player-guide/patches.md#outposts-and-caravans-with-vehicles),
[Vehicle Framework](../../docs/player-guide/patches.md#vehicle-framework-and-combat-extended)).

**Is it working?** The parts that patch another mod's code write a load line naming what is on; where that code has changed, the line
says which part is off and why.

## Settings

In **Options, Mod settings**. RimWorld saves only what you change; no restart is needed.

### D'thorga Patches (with InterRim Ballistic Missile)

A choice that costs speed shows its cost in amber under it.

| Setting | What it does | Default |
| --- | --- | --- |
| Spread a strategic blast over several moments | Off: the missile mod's own one-frame blast, which can freeze the game for minutes | On |
| Time each moment may spend on the blast | 2 to 50 ms; above 25 ms the game visibly stutters while a blast runs | 12 ms |
| Take up to 40 ms a moment while the screen is still white | A slower moment costs nothing you can see while the screen is white | On |
| What shelters people and things from a blast | Any overhead mountain; only thick overhead mountain (the missile mod's rule); nothing. The last two sweep mountain interiors too, several times slower | Any overhead mountain |
| Leave rubble and salvage where buildings stood | On: many times slower | Off |
| The dead | Vaporised, or left where they fell | Vaporised |
| Rebuild rooms once per moment during a blast | Switch it off only if a blast leaves rooms behaving oddly, and report it | On |
| Tell me when a blast takes a while, and when it ends | A message after two seconds, and one when it has spent itself | On |
| No "Area revealed" letters while a blast runs | Also silences the warning about mechanoids found inside an opened pocket | On |
| Wall fragments from a strategic blast (only with Combat Extended) | None during a blast; a tenth as many (several times slower); as many as in a fight (a blast on a town can take tens of minutes) | None during a blast |
| Fireball, blast and heat by distance from ground zero | Off: no burns, wilting, ash or pulse, and the warhead's fires light a fifth of every cell in one moment | On |
| Full strike on every settlement in range | On: each map in range takes a full strike of its own | Off |
| Fires beyond the blast | None, sparse, moderate, firestorm; moderate and above mean hundreds to thousands of fires at once | Moderate |
| Fallout and radiation | Radioactive fallout where it can, otherwise nuclear winter; nuclear winter only; nothing | Radioactive fallout where it can, otherwise nuclear winter |
| Fallout clouds drifting from the strike (with Rimatomics) | 0 to 6 | 6 |
| Radiation sickness and flash blindness for survivors (with Rimatomics) | For those who live through the strike in the open | On |
| Ground pollution near the centre (with Biotech) | The scoured ground at the centre, and patches around it | On |
| Electromagnetic pulse | Powered buildings and mechanoids in the open stop for a few hours | On |
| Close-in weapon systems shoot down Combat Extended shells (only with Combat Extended) | Off: they leave CE's mortar and artillery shells alone | On |

### D'thorga Patches: Harrier

| Setting | What it does | Default |
| --- | --- | --- |
| Sorties may fly over a battle you have not joined | On: aircraft may fly a sortie onto a RimPacts battle you have not joined, and RimPacts then counts the crew as your forces there | Off |
| Log each step when a vehicle is taken apart (temporary) | One log line per step of a vehicle's disassembly, to find why the Harrier cannot be taken apart; changes nothing in play | On |

### D'thorga Patches: outposts

| Setting | What it does | Default |
| --- | --- | --- |
| Refuse to found an outpost from a caravan with vehicles (recommended) | Off: the "create outpost" window works as Vanilla Expanded Framework wrote it, and may lose the vehicles and their crews | On |

## For modders

- [Vehicle patches: research and decisions](../../docs/development/vehicle-patches.md): the Harrier, bombs, fabricator, forger and
  turret hulls, with the code paths and the source files.
- [Missile patches: research and decisions](../../docs/development/missile-patches.md) and
  [strategic strike](../../docs/development/missile-patches.md#strategic-strike-design-and-decisions-7-nov-2026): the blast and its queue.
- [RimPacts patches](../../docs/development/rimpacts-patches.md): the Harrier's load fix, sorties over battles, struck towns' assaults.
- [Visit Settlements patches](../../docs/development/visit-settlements-patches.md): the battle map that closed, and the food drops.
- [The register of temporary patches](../../docs/development/temporary-patches.md): what would make each temporary part unnecessary.

## Checks done

Every part was read in the patched mods' code and files and checked offline (builds, and `scripts/check-factory-patches.py` for
the recipe patches); each version's checks are in the [changelog](CHANGELOG.md), and the dated list the README kept until 17 November
2026 is in [Vehicle patches](../../docs/development/vehicle-patches.md#checks-moved-from-the-readme-17-nov-2026). **Tested in game by
the owner:** the stuck vehicle fires (10 November 2026) and one missile strike with the first missile fix (7 November 2026). **Not yet
tested in game:** everything else.
