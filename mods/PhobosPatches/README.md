# D'thorga Patches

Fixes and patches for other authors' mods, each switched on only while that mod is active. It copies nothing from them. It covers
**Harrier VTOL Attack Aircraft** and **Close Air Support: A-10 Warthog & Su-25 Frogfoot** by potemkinvillager, **UH-60M Black
Hawk** and **C-17 Globemaster III** by z3nt3, **InterRim Ballistic Missile** by Psionic Zephyr, **RimPacts**, **Visit
Settlements** by Nina.Goblin, **Vehicle Framework**, **Vanilla Expanded Framework**'s outposts, **Vanilla Factions Expanded -
Classical**, **Nice Research Tab** by Andromeda, **Spaceship Turret Hull** by MK and **Combat Extended** (with **Simple sidearms**
by PeteTimesSix), and RimThunder's heavy casting at The Dead Man's Switch's rotary forger. It also covers the **EA-6B Prowler**'s
munitions, **Biotech**'s mech rechargers and **The Dead Man's Switch**'s drone platforms.

One of House D'thorga's mods (also spelt dthorga). It replaces two earlier mods, merged into it on 7 November 2026: **D'thorga
Vehicle Patches** and **D'thorga Missile Patches** ([why](../../docs/development/mod-consolidation.md)).

Version 0.49.0, built 28 November 2026 for House D'thorga. **Partly tested in game:** the Vehicle Framework fires part (owner,
10 November 2026: the stuck fires went out), one missile strike on an earlier version (owner, 7 November 2026), the Prison Labor
guard, drone platforms kept topped up and drones called home in time, soldiers going back to their own guns, sidearms that loop
being stopped, mech rechargers drawing real power, and the Nice Research Tab guard. Everything else is not yet tested in game.

**How to play with it is in the player guide's [Patches chapter](../../docs/player-guide/patches.md).** Each version's changes are
in the [changelog](CHANGELOG.md).

## Read this first (gotchas)

- **Partly tested in game.** Besides the stuck vehicle fires and one missile strike on an earlier version (the version line
  above), the owner has passed the Prison Labor guard, drone platforms kept topped up and drones called home in time, soldiers
  going back to their own guns, sidearms that loop being stopped, mech rechargers drawing real power, and the Nice Research Tab
  guard. The list, with dates and what passed only in part, ends the guide's [Patches
  chapter](../../docs/player-guide/patches.md#tested-in-game). **Everything else is not yet tested in game.**
- **Needs Harmony and D'thorga Framework 0.184.0 or later.** **Load it after the mods it patches**: its About file asks for that
  order, so RimSort sorts it for you. **Switch it on yourself**: nothing else requires it.
- **RimWorld 1.7 only.**
- **Each part loads only while its own mods are active**, so it does nothing without them, and a player running none of them sees
  nothing. Which mods each part needs is in the guide's [mod-by-mod
  table](../../docs/player-guide/patches.md#what-it-changes-mod-by-mod).
- **It replaces D'thorga Vehicle Patches and D'thorga Missile Patches: switch both off and this one on, in the same change.**
  RimSort warns if one is left on; your save needs nothing, and the missile settings carry over by themselves.
- **It adds a Rearm and refuel column to the work tab**, switched on for your colony the first time you load (each colonist at their
  Hauling priority; ucas at their Construction priority). Someone set high on it leaves other work to rearm or refuel: pick a few
  armourers.
- **Settings to decide before a colony:**
  - **With InterRim Ballistic Missile, the blast settings**, before your first strike. Choices that cost speed say so in amber.
  - **With Biotech, Mech rechargers draw power by the size of the mech** (off). Before turning it on, check your grid: a tunneler
    or war queen charging draws about 1,000 W or more, and a shortage switches things off at random.
  - **With Close Air Support, Realistic handling** (off). On, the A-10 is much slower and loses more speed with an engine out, and
    the Su-25 a little slower; it takes effect at the next start.
- **Safe to add to a running colony.** A Harrier, A-10 or Su-25 you already built keeps working and gains its new parts at full
  health, and a Black Hawk or C-17 already built keeps working; only new builds pay the new cost. Bombs already made stay as they
  are. **Finish or cancel a Harrier, Black Hawk or C-17 frame before installing**: a frame keeps what was delivered and then asks
  for the new list.
- **Before removing it:** delete any bomb or rocket bills you added at an ammunition fabricator and any heavy casting bills at a
  rotary forger; **with Combat Extended, use up or sell your bomb cases, bomb fills and missile seeker heads**, delete their
  bills, and **deconstruct turret hulls built under this mod** (not yet tested either way). Without it the Harrier's costs go back
  to its own, and with RimPacts its flight framework fails to load fully again. Every mod's steps are in [Removing a
  mod](../../docs/player-guide/getting-started.md#removing-a-mod).
- **After a missile strike, wait for the message that the blast has spent itself before saving**: a blast still running when you
  save is forgotten on load.
- **The Harrier cannot be taken apart yet. Try one disassembly, on a copy of your save, and stop there**: each attempt runs the
  first half of the removal again. Its log trace is off unless you tick **D'thorga Patches: Harrier disassembly** on D'thorga
  Framework's **Detailed logs** page first.
- **A Harrier with avionics, hydraulics or wiring below about 35% cannot take off** until repaired, and on a hot day a heavy
  Harrier lands on a runway instead of hovering onto its pad (a D'thorga Framework setting). Keep **countermeasure cartridges** in
  storage for its **Chaff and flares** button.
- **Rearming keeps to one kind of ammunition** (Harrier, Hind, Mongoose, A-10, Su-25). To change kind, choose it on the weapon.
- **With Combat Extended:** allow the two bomb cases in machining bay hoppers that used to take heavy castings, and **rebuild
  turret hulls built before**, while the ship is landed. **A colonist who already has no gun anywhere needs one handed to them
  once** to go back to their own gun after being downed.
- **With RimPacts, a world war attack must win within 3 days of accepting it**, or about a dozen border towns pass to the enemy.
  That is RimPacts' rule; the quest shows the countdown. **Strike and attack early.**
- **With Visit Settlements, pawns already lost to a closed map stay lost**: load a save from before.
- **With Vanilla Factions Expanded - Classical, if you saved again after a scorpion-raid save loaded broken, before this fix, load
  the save made before that**: the raid's orders are gone from the later one.
- **Keep Prison Labor up to date**: 1.7.1 fixed the crash our old guard covered.

## What it adds

One line each; the guide's [Patches chapter](../../docs/player-guide/patches.md) has the figures, tables and how to use each.

- **Rearm and refuel**, a work type of its own: rearming, reloading and refuelling turrets, vehicles, aircraft and buildings, ranked
  just above basic work instead of near the bottom with hauling; with The Dead Man's Switch, its ucas may do it too.
- **Harrier VTOL Attack Aircraft**: built from real parts, fourteen parts with flight controls that ground it when damaged, loads
  fully beside RimPacts, its sortie's map stays open, and a temporary disassembly trace.
- **UH-60M Black Hawk, C-17 Globemaster III, Close Air Support's A-10 and Su-25**: built from real parts by role.
- **With Combat Extended**: bombs, missiles and jamming pods that cost what they weigh, each bomb on a steel bomb case; bombs and
  rockets at The Dead Man's Switch's ammunition fabricator; soldiers who go back to their own guns, and sidearms that stop looping
  with Simple Sidearms; a temporary guard for ammo sets that would stop Combat Extended's ammo setup.
- **RimThunder with The Dead Man's Switch - Expedition**: heavy castings 12 at a time at the rotary forger.
- **The Dead Man's Switch's drone platforms**: refilled with steel by colonists, drones called home while they can still get
  there, and their wrecks carried back for their steel.
- **Spaceship Turret Hull, with Combat Extended**: the armed hulls load and fire Combat Extended ammunition.
- **InterRim Ballistic Missile**: a strategic blast spread over moments instead of freezing the game, shelter under overhead
  mountain, and the extinction-class warhead's fireball, blast and heat zones; a radiation dose from its other nuclear and
  antimatter warheads (with D'thorga Medical's radiation model); with Combat Extended, its close-in weapon systems shoot down
  Combat Extended's shells.
- **The Dead Man's Switch - Legion**: its nuclear strike's radiation by distance and shelter, a setting (with D'thorga Medical's radiation
  model).
- **Antigrain blasts and the nuclear boiler** (the game, Combat Extended, VFE Deserters, Vanilla Gravship Expanded 2, 长征工业合作社's
  power system): a radiation dose at the flash, with D'thorga Medical's radiation model.
- **RimPacts**: allies' assaults leave empty vehicles alone (temporary), and face a struck town's garrison from before the strike.
- **Visit Settlements**: a visited settlement's map stays open while your people can still act there, and its food drops work.
- **Biotech**, a setting: mech rechargers draw power by the size of the mech.
- **Vanilla Factions Expanded - Classical**: a save made during a scorpion raid loads with the raid working (temporary).
- **Nice Research Tab**: a blank entry can no longer stop the research tab opening (temporary).
- **Vehicle Framework**: fires a vehicle could never put out are put out, repairs and upgrades show a progress bar, and a vehicle
  saved mid-burst keeps itself, its crew and its cargo (temporary). **With Vanilla Expanded Framework**, a caravan with vehicles
  can't found an outpost and lose them.

**Is it working?** The parts that patch another mod's code write a load line naming what is on; where that code has changed, the
line says which part is off and why.

## Settings

In **Options > Mod settings > D'thorga Patches**, with a page for each patched mod down the left; a page shows only while its mod
is active. **Type in the search box to find a setting on any page**, tick **Changed only** to see just what you have changed (each
marked with a dot), and use **Reset this page to defaults** at the bottom of a page. RimWorld saves only what you change; no
restart is needed. A choice that costs speed shows its cost in amber under it.

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
| Shadow and shelter from the strike | Anyone under a roof is spared burns and flash blindness; the dose is cut by what stood over them (a sixth behind stone, almost none in rock); flash blindness is worse at night; your people's doses come on a letter. Off: everyone not under rock takes burns, blindness and the whole dose | On |
| Vehicles in the strike (with D'thorga Framework's vehicles) | Vehicles in the fireball are gone with everyone aboard; further out the blast wrecks or batters them by their weight, heat burns tyres and hoses, a running engine takes the pulse, and the people aboard are thrown about and take the dose the hull lets through. Off: a vehicle takes one blow to one spot, which armour usually shrugs off | On |
| Close-in weapon systems shoot down Combat Extended shells (only with Combat Extended) | Off: they leave CE's mortar and artillery shells alone | On |
| CIWS fires at enemy aircraft | Close-in weapon systems also fire at enemy aircraft flying over the map, such as one making a parachute drop, damaging its parts; never at your own. Off: they fire only at shells, missiles and drop pods | On |

### Harrier jump jet

| Setting | What it does | Default |
| --- | --- | --- |
| Sorties may fly over a battle you have not joined | On: aircraft may fly a sortie onto a RimPacts battle you have not joined, and RimPacts then counts the crew as your forces there | Off |

The Harrier's disassembly trace is not here: it is **D'thorga Patches: Harrier disassembly** on D'thorga Framework's **Detailed
logs** page, off until ticked.

### Close Air Support, and EA-6B Prowler

| Setting | What it does | Default |
| --- | --- | --- |
| Realistic handling (Close Air Support) | On: the A-10 and the Su-25 fly at speeds set by the real aircraft's, against the Harrier's: the A-10 much slower, and losing more speed with an engine out; the Su-25 a little slower. Take-offs, landings, turns and attack runs are unchanged. Off: the speeds the Close Air Support author set | Off |
| Realistic handling (EA-6B Prowler) | On: the Prowler flies at speeds set by the real EA-6B's. Its author already gives it the real figures, so this changes nothing yet; it holds them if a later update changes them | Off |

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
| A colonist never fetches a sidearm their loadout would only put down again (recommended) | With Simple sidearms: a colonist no longer fetches an extra copy of a weapon their loadout would drop and haul away again, over and over. You are told once which colonist and weapon. Off: Simple Sidearms' and Combat Extended's own behaviour | On |

### DMS Legion (with The Dead Man's Switch - Legion)

| Setting | What it does | Default |
| --- | --- | --- |
| Legion's nuclear strike: dose by distance and shelter | Each person takes a radiation dose by how far they were and what covered them, so people deep in rock can live; a letter names your people dosed. Needs D'thorga Medical's radiation model. Off: Legion's own radiation, which kills everyone on the map within seconds | On |

## More

- [The player guide's Patches chapter](../../docs/player-guide/patches.md): every part mod by mod, and what is tested in game.
- [The changelog](CHANGELOG.md): what changed in each version, with the two former mods' histories.
- [For modders](../../docs/modding/patches.md): the design records behind each part, the register of temporary patches, and the
  checks done.

Community-created content. Portions of the materials used to create this content/mod are trademarks and/or copyrighted works of
Ludeon Studios Inc. All rights reserved by Ludeon. This content/mod is not official and is not endorsed by Ludeon.
