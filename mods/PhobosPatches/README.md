# D'thorga Patches

Fixes and patches for other authors' mods, each switched on only while that mod is active. It copies nothing from them. It covers
**Harrier VTOL Attack Aircraft** and **Close Air Support: A-10 Warthog & Su-25 Frogfoot** by potemkinvillager, **UH-60M Black Hawk** by z3nt3, **InterRim Ballistic Missile** by Psionic Zephyr, **RimPacts**, **Visit
Settlements** by Nina.Goblin, **Vehicle Framework**, **Vanilla Expanded Framework**'s outposts, **Vanilla Factions Expanded -
Classical**, **Spaceship Turret Hull** by MK and **Combat Extended** (with **Simple sidearms** by PeteTimesSix), and RimThunder's heavy
casting at The Dead Man's Switch's rotary forger.

One of House D'thorga's mods (also spelt dthorga). It replaces two earlier mods, merged into it on 7 November 2026: **D'thorga
Vehicle Patches** and **D'thorga Missile Patches** ([why](../../docs/development/mod-consolidation.md)).

Version 0.34.0, built 22 November 2026 for House D'thorga. **Partly tested in game:** the Vehicle Framework fires part (owner,
10 November 2026: the stuck fires went out) and one missile strike on an earlier version (owner, 7 November 2026). Everything else is
not yet tested in game.

**How to play with it is in the player guide's [Patches chapter](../../docs/player-guide/patches.md).** Each version's changes are in
the [changelog](CHANGELOG.md).

## Read this first (gotchas)

- **Needs Harmony and D'thorga Framework 0.184.0 or later.** **Load it after
  the mods it patches**: the About file asks for this, so RimSort does it for you. **Switch it on yourself**: nothing else requires
  it. It does nothing without the mods it patches.
- **Each part loads only while its own mods are active** (an `IfModActive` folder per patched mod): the Harrier part with the Harrier
  (its bomb changes also need Combat Extended, and the fabricator bills The Dead Man's Switch's fabricator); rearming with the Harrier,
  Hind, Mongoose or Close Air Support; the Close Air Support part with that mod; the Black Hawk part with the UH-60M Black Hawk; the forger bill with RimThunder and the forger from The Dead Man's Switch - Expedition; the turret hulls with
  Spaceship Turret Hull and Combat Extended together; the ammo guard and "soldiers keep their guns" with Combat Extended (that one needs
  D'thorga Framework 0.174.0 or later); the missile part with InterRim Ballistic
  Missile; one part each for RimPacts, Visit Settlements, Vehicle Framework and Vanilla Factions Expanded - Classical (that one needs
  D'thorga Framework 0.168.0 or later); the outposts part with Vanilla Expanded Framework and Vehicle Framework together; the mech recharger part with Biotech (D'thorga
  Framework 0.198.0 or later). A player running none of them sees nothing.
- **With Biotech, mech rechargers drawing real power is off by default.** Before turning it on, check your grid: a tunneler or war
  queen charging draws about 1,000 W or more, and a shortage switches things off at random.
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
  stop there**: each attempt runs the first half of the removal again. **Its log trace is off unless you ask for it** (0.27.0): tick
  **D'thorga Patches: Harrier disassembly** on D'thorga Framework's **Detailed logs** settings page first.
- **Harrier flight controls:** with avionics, hydraulics or wiring below about 35% the jet cannot take off until repaired. On a hot
  day a heavy Harrier lands on a runway instead of hovering onto its pad, by **D'thorga Framework's** setting "When an aircraft is too
  heavy or too hot to hover onto its pad".
- **Harrier dodge and chaff (0.21.0):** the jet dodges as its author set while sound; shot-up flight controls or a dead warning receiver
  make it easier to hit. Keep **countermeasure cartridges** (D'thorga Framework's parts bench) in storage; haulers load them into its dispensers while it is
  parked, for its **Chaff and flares** button in the air.
- **UH-60M Black Hawk (0.31.0): built from real parts.** **Finish or cancel a Black Hawk frame before installing**: a frame keeps what was
  delivered and then asks for the new list. A Black Hawk already built keeps working.
- **Close Air Support (0.29.0): Realistic handling is off by default.** On (the **Close Air Support** page of the settings), the A-10 is
  much slower and loses more speed with an engine out, and the Su-25 a little slower, as the real aircraft are beside a Harrier; it takes
  effect at the next start. A jet you already built keeps working and gains its new parts at full health; only new builds pay the
  new cost. **Before removing it**, delete any bomb bills you added for its jets at an ammunition fabricator.
- **Rearming keeps to one kind of ammunition** (Harrier, Hind, Mongoose, A-10, Su-25). To change kind, choose it on the weapon.
- **With Combat Extended, allow the two bomb cases in machining bay hoppers** that used to take heavy castings, and **rebuild turret
  hulls built before**, while the ship is landed.
- **With Combat Extended, soldiers go back to their own guns** (0.26.0, not yet tested in game) instead of fighting on with grenades and
  satchel charges after being downed. **A colonist who already has no gun anywhere needs one handed to them once** (select them,
  right-click a gun, Equip): from then on they keep it. **With Simple Sidearms too, a colonist never fetches a sidearm their loadout would
  only put down again** (0.29.1): you are told who and which weapon, to take the extra copy off their sidearms. Four settings on the
  **Combat Extended** page of **D'thorga Patches**' settings; safe to add to and remove from a running save (what it remembers is kept
  by D'thorga Framework).
- **With RimPacts, a world war attack must win within 3 days of accepting it**, or control of about a dozen border towns passes to
  the enemy. That is RimPacts' rule; the quest shows the countdown. **Strike and attack early.**
- **With Visit Settlements, pawns already lost to a closed map stay lost**: load a save from before.
- **With Vanilla Factions Expanded - Classical, a save made during a scorpion raid now loads with the raid working** (temporary). **If
  you saved again after such a save loaded broken, before 0.25.0, load the save made before that**: the raid's orders are gone from it.
- **Keep Prison Labor up to date**: the guard for its first 1.7 build was removed in 0.19.2, as Prison Labor 1.7.1 fixed the crash.

## What it does

Each part in a line or three; the guide chapter has the figures, tables and how to use each.

### Harrier VTOL Attack Aircraft

- **A realistic build cost**, from real parts by role (0.30.0): a turbine (RimThunder's where installed, ours otherwise), four nozzles,
  landing gear, fuel tanks, electrics, hydraulics and avionics, and Construction 10 ([building it](../../docs/player-guide/patches.md#building-it)).
  Drop Tanks take real fuel tanks. **Finish or cancel a Harrier frame before installing 0.30.0**: a frame keeps what was delivered.
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
- **Soldiers keep their guns**: a colonist left holding a grenade, a satchel charge or nothing after being downed takes their own gun
  up again, and Simple Sidearms keeps the gun, not a grenade, as the weapon it hands back
  ([soldiers keep their guns](../../docs/player-guide/patches.md#soldiers-keep-their-guns)).
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

### Biotech

A setting, off by default: mech rechargers draw power by the size of the mech on them, charging slows over the last fifth of the
battery, a charging recharger warms its room, and a mech's information card shows its recharger draw
([mech rechargers](../../docs/player-guide/patches.md#mech-rechargers-with-real-power-biotech)).

### Vanilla Factions Expanded - Classical

A save made during a scorpion raid loads with the raid still working (temporary)
([Classical](../../docs/player-guide/patches.md#vanilla-factions-expanded---classical)).

### Vehicle Framework and outposts

Fires a vehicle could never put out are put out; repairs and upgrades show a progress bar (temporary); a vehicle saved while one of
its weapons is firing a burst keeps itself, its crew and its cargo in the save, where before it vanished with everyone aboard on
loading (temporary; a save already made that way cannot be mended); a caravan with vehicles cannot found an outpost through "create outpost"
([outposts](../../docs/player-guide/patches.md#outposts-and-caravans-with-vehicles),
[Vehicle Framework](../../docs/player-guide/patches.md#vehicle-framework-and-combat-extended)).

**Is it working?** The parts that patch another mod's code write a load line naming what is on; where that code has changed, the line
says which part is off and why.

## Settings

In **Options, Mod settings, D'thorga Patches**: one entry since 0.27.0 (it was four), with a page for each patched mod down the left;
a page shows only while its mod is active. **Type in the search box to find a setting on any page**, tick **Changed only** to see just
what you have changed (each marked with a dot), and use **Reset this page to defaults** at the bottom of a page. RimWorld saves only
what you change; no restart is needed. A choice that costs speed shows its cost in amber under it.

### IRBM: strikes, and IRBM: after-effects (with InterRim Ballistic Missile)

The first page holds the strike itself and the game's speed, from the top of the table to the wall fragments; the second what a
strike leaves behind, from the fireball onwards.

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
| Vehicles in the strike (with D'thorga Framework's vehicles) | Vehicles in the fireball are gone with everyone aboard; further out the blast wrecks or batters them by their weight, heat burns tyres and hoses, a running engine takes the pulse, and the people aboard are thrown about and take the dose the hull lets through. Off: a vehicle takes one blow to one spot, which armour usually shrugs off | On |
| Close-in weapon systems shoot down Combat Extended shells (only with Combat Extended) | Off: they leave CE's mortar and artillery shells alone | On |

### Harrier jump jet

| Setting | What it does | Default |
| --- | --- | --- |
| Sorties may fly over a battle you have not joined | On: aircraft may fly a sortie onto a RimPacts battle you have not joined, and RimPacts then counts the crew as your forces there | Off |

The temporary disassembly trace had a tick here, on by default, until 0.27.0. It is now **D'thorga Patches: Harrier disassembly** on
D'thorga Framework's **Detailed logs** page, off until ticked.

### Mechanoids (with Biotech)

| Setting | What it does | Default |
| --- | --- | --- |
| Mech rechargers draw power by the size of the mech | On: a recharger draws about 285 W per point of the mech's body size while it charges (a light mech 200 W, a tunneler about 1,000 W), charging slows over the last fifth, a sixth of the power warms the room, and an empty recharger draws a twentieth of its listed power. Off: the game's own flat figure | Off |
| Mech battery size | Standard, Large (three times the draw) or Huge (ten times); charging takes the same time at each. Only matters with the setting above on | Standard |

### Outposts

| Setting | What it does | Default |
| --- | --- | --- |
| Refuse to found an outpost from a caravan with vehicles (recommended) | Off: the "create outpost" window works as Vanilla Expanded Framework wrote it, and may lose the vehicles and their crews | On |

### Combat Extended

| Setting | What it does | Default |
| --- | --- | --- |
| Colonists go back to their own guns (recommended) | A colonist holding a grenade or a satchel charge, or nothing after being downed, takes up a gun from their pack that can fire, else the gun they dropped when downed (even if forbidden). Drafted colonists and melee weapons are left alone. Off: Combat Extended and the game as written | On |
| When their own gun is gone, take another of the same kind | Their dropped gun destroyed or left behind, or a grenade in hand and no gun at all: the nearest unforbidden gun of the kind they last held, with rounds in it. Never a different kind | On |
| Simple Sidearms keeps a colonist's gun, not a grenade, as the weapon it hands back (recommended) | With Simple sidearms: the gun in hand replaces a grenade, or nothing, as the ranged weapon Simple Sidearms keeps putting back. A different gun you chose, or a weapon you forced, is left alone | On |

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
