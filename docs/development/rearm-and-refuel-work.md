# Rearm and refuel: a work type of its own

**Status (27 Nov 2026):** built in D'thorga Patches 0.46.0 (the work type, its jobs, the uca) and D'thorga Framework 0.354.0 (starting
priorities for a work type new to a save). Not yet tested in game (test row 703).

## Why

The owner's report, 27 Nov 2026: turrets, vehicles and aircraft "used to be readily rearmed, but for quite some time now, that hasn't
been so", and refuelling too. Read from the owner's save PG1 of that day:

- **Every rearm, reload and refuel job in the game is Hauling work**: the game's turrets and fuelled buildings, Combat Extended's
  turrets and ammo containers, Vehicle Framework's refuelling and turret loading, Vanilla Expanded Framework's second fuel, and every
  jet's and helicopter's rearm and bomb-part loading.
- **Hauling is ranked 300 of the game's built-in order**, below every work type but cleaning and research. Where a colonist's
  priorities are equal, the game takes work types in that order, and **44 of the colony's 52 colonists had everything at one
  priority** (858 of 888 marks). With full bill queues, they almost never reached Hauling.
- **The colony's drones and mechs did almost no hauling**: of about 115, three had Hauling on (two shaft workers and an escort
  lifter). The 16 ucas cannot haul at all.

Nothing was broken: the jobs were starved. Within Hauling they sit near the top (80 to 150), so a hauler who reached Hauling did them
first.

## What it is

- **A work type, Rearm and refuel** (`PhobosRearmRefuel`, D'thorga Patches' `Defs/RearmAndRefuel_Core.xml`), at **1160** in the built-in
  order: just above basic work (1150), below childcare and urgent hauling (agent choice). Its pawn label is Armourer. It carries
  Hauling's work tags, so whoever cannot haul by background cannot do it either.
- **Every rearm, reload and refuel job joins it a second time**, each from the folder of the mod it belongs to and only while that mod
  is active, with the mod's own class: the same way Framework 0.302.1 offered Vehicle Framework's upgrade materials under
  Construction. Hauling keeps every job, so haulers still help.

| Job | Class | Folder | Order in the work type |
| --- | --- | --- | --- |
| Reload turrets (Combat Extended) | `CombatExtended.WorkGiver_ReloadTurret` | `Mods/CombatExtended` | 100 |
| Reload ammo containers (Combat Extended) | `CombatExtended.WorkGiver_ReloadAutoLoader` | `Mods/CombatExtended` | 99 |
| Rearm turrets (the game) | `WorkGiver_Refuel_Turret` | root | 98 |
| Refuel ammunition (Vanilla Expanded Framework) | `VEF.Buildings.WorkGiver_RefuelSecondary` | root, `MayRequire` | 97 |
| Load vehicle turrets (Vehicle Framework) | `Vehicles.WorkGiver_RefuelVehicleTurret` | `Mods/VehicleFramework` | 90 |
| Rearm the Harrier, A-10 and Su-25, Prowler, Hind, Mongoose | each mod's `WorkGiver_RearmVehicle` | each mod's folder | 85 |
| Load bomb and missile parts (Harrier, A-10, Prowler) | each mod's bomb or missile parts giver | each mod's folder | 84 |
| Refuel vehicles (Vehicle Framework) | `Vehicles.WorkGiver_RefuelVehicle` | `Mods/VehicleFramework` | 80 |
| Refuel buildings (the game) | `WorkGiver_Refuel` | root | 70 |

- **The uca may do it** (the owner's choice, 27 Nov 2026: "Work type, Ucas too"). A mech does only the work types its race lists, so
  `Patches/RearmAndRefuel_Uca.xml` adds the work type to `DMS_Drone_Uca`'s list, matched by the Def's name so nothing happens without
  The Dead Man's Switch. Its description is a drone for "maintaining equipment".
- **Starting priorities in a save already under way** (D'thorga Framework 0.354.0, `Work/WorkTypeSeeding.cs`, rule
  `WorkTypeSeedRule`). A work type added mid-game starts off for everyone, and switching it on for 52 colonists by hand is a chore
  for anyone, more so with sore wrists. So the first time a save runs with it, each pawn of the player's starts it at the priority it
  has for **Hauling**, or, if it cannot haul at all, for **Construction** (the uca). A colonist whose Hauling the player switched off
  keeps it off. It happens once per save; later colonists and drones are set up by the game as for any work type (agent choice). The
  service is generic: any WorkTypeDef carrying `PhobosWorkTypeSeedExtension` gets it.

## The four questions

- **What it lets the player do**: keep guns loaded and tanks full with chosen armourers and drones, without lifting Hauling over every
  other job; give the colony's idle ucas the work.
- **What it stops**: nothing. Haulers still do every job.
- **How play changes**: rearming and refuelling happen soon after they are needed, before cooking and crafting at equal priority.
  Someone set to Rearm and refuel at a high priority stops other work to do it, as a firefighter does.
- **What outside notices**: the turrets, vehicles and aircraft themselves (loaded, fuelled); the work tab; each armourer's time.

## Risk and setting

**Risk: none** to things or people (agent's grade, for the owner to confirm): it only adds a second way to the same jobs. So **no
setting**; the player turns the work type down or off per pawn in the work tab, as any other.

## Limits and what is not done

- **Other authors' loaders left out** (agent choice): The Dead Man's Switch's missile launcher and AXF-12 loaders, and the wildcard
  ammo gantry. Each is one more WorkGiverDef if wanted.
- **Only the uca** among drones and mechs gets the work type, by the owner's choice; another race is one more patch line.
- **Untested**: whether every one of these classes behaves under a second Def is checked by reading their code only (no Hauling-only
  or humans-only check in any of them); the game will tell.
- **Another game**: the starting-priorities service is RimWorld's (work types are its engine); nothing here is generic beyond it.
