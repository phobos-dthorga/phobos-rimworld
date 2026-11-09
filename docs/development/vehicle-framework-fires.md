# Fires on vehicles that never go out (10 Nov 2026)

The owner's Harrier flew home from a sortie on fire, crashed short of home, and three game days later still listed "Burning (fire size 170)" dozens of times while its parts
were repaired to full. The owner asked for a fix ("Please implement a fix"). This records what the saves and the code show, what is still unknown, the fix in D'thorga Patches 0.16.6,
and a draft report for Vehicle Framework's author, whose bug it is.

## Read this first

- **37 fires are attached to one jet.** The game keeps one fire per thing and always works on "the first": a second fire is never put out by foam, rain, a colonist or the
  vehicle leaving the map. Vehicle Framework's own foam code reads only the first fire too (`VehicleStatHandler.TryExtinguishFire`, in the 1.6 build and in `develop`).
- **All 37 are on the wrong map.** They sit on the enemy base's map at (37, 5), where the jet was hit; the jet sits on the crash site's map at (37, 5). The jet's attachment
  comp moves them to its own coordinates every tick, on a map it is not on.
- **They tick but never change.** Between two saves 5,134 ticks apart their tick counters advanced by 5,130, yet every one stayed at size 1.70132506. A burning fire grows
  towards 1.75 or shrinks and dies; these do neither, and nothing in `Player.log` says why.
- **The jet is not being harmed**: its parts rose from 72% to 100% by ordinary repair while "burning". The cost is that it reads as burning, so Vehicle Framework never
  auto-refuels it and our service jobs leave it alone unless ordered. It sat at 0 of 250 fuel for three days.
- **Not ours.** No Phobos code touches fire attachment; the parts rose through repair; the save bridge was inert for that jet.

## What the evidence shows, in order

| Fact | Source |
| --- | --- |
| 37 `Fire` things with `parent` = the jet, spawned in three bursts at ticks 49,436,403 (10), 49,437,406 (20) and 49,437,426 (7) | `PG5` and `PG5_temp`, read with `query-save.py` and a read-only script on frozen copies |
| All on map index 1 (the faction base, 325 by 325), at (37, 5); the jet on map index 2 (an encounter map, 75 by 75), at (37, 5) | `query-save.py things`, `summary` |
| Size 1.70132506 on every fire in both saves; `ticksSinceSpawn` 30,671 then 35,801 | the saves |
| The game attaches a fire only if the thing has none (`FireUtility.TryAttachFire`: `!t.HasAttachment(Fire)`); Combat Extended's flame, fuel and fire-start paths all go through it | the 1.7 game source; Combat Extended's decompile, in our words |
| A thing entering a container has its first fire put out (`ThingOwner`, `ShouldAutoExtinguishInnerThings`); a vehicle launching enters an `AerialVehicleInFlight` container, so one of 37 would go and 36 stay | the game source |
| Vehicle Framework's despawn does nothing about attached fires; its foam shrinks the first fire only | `VehiclePawn.DeSpawn`, `VehicleStatHandler.TryExtinguishFire`, 1.6 build and `develop` (31 Oct 2026) |
| No active mod patches `CompAttachBase`, `AttachmentUtility` or `Pawn.TakeDamage`; Combat Extended and Field Tents patch `Fire.TickInterval` for spread and smoke | `query-patches.py` |

**Still unknown, stated plainly:** how 37 got past the one-fire gate in bursts of 10 within a tick, and what holds their size still. The crash and the map change are the
likeliest ingredients (inference). The fix below does not depend on either answer.

## The fix: D'thorga Patches 0.16.6, Vehicle Framework part

Two Harmony patches on `VehiclePawn`, calling one service, `VehicleFires`:

- **Leaving a map puts every fire out** (prefix on `DeSpawn`, while the fires are still on the map), as the game does for a thing entering a container. A vehicle that
  drives or flies off a map arrives without fire.
- **Every rare tick, surplus fires go** (postfix on `TickRare`): any fire not on the vehicle's map, and every fire after the first that is. `AttachedFireRule.Surplus`
  decides, game-free, with tests. The player's vehicles get a message saying how many were put out.

**Lets the player:** refuel and service a vehicle that came back from a fight. **Stops:** nothing a real fire does; one fire on a vehicle burns, spreads and is fought as before.
**Changes:** a vehicle no longer carries a fire off a map, so the fire stays where it was.

The rule that another author's bug is theirs to fix applies, and the owner chose to patch it; Vehicle Framework's project is active (`develop` pushed 31 Oct 2026). **Remove this part**
when Vehicle Framework puts fires out on despawn and keeps one per vehicle (watch `VehiclePawn.DeSpawn` and `TryExtinguishFire` in `develop`, and item J3 in `outstanding-work.md`):
delete `src/PhobosPatches.VehicleFramework/` and `mods/PhobosPatches/Mods/VehicleFramework/`, its two `LoadFolders.xml` lines, its two constants entries, the tests file and its
`Compile Include`, the README's lines, and take a patch-number version.

Cost: `GetComp<CompAttachBase>()` once per vehicle rare tick, from the game's comp cache; the list is empty on nearly every vehicle. **Tested in game by the owner, 10 Nov 2026: the fix works.**

## Draft report for Vehicle Framework's author (the owner posts; agents never post)

> **Fires attached to a vehicle are never put out, and multiply.** RimWorld 1.7.5000, Vehicle Framework 1.6.2144 (the `develop` source of 31 Oct 2026 looks the same here).
> A Harrier (Harrier VTOL Attack Aircraft, with Combat Extended) hit by flamethrowers over an enemy base ended with 37 `Fire` things attached (three bursts within about
> 1,000 ticks). It flew off and crashed on another map; the fires stayed spawned on the base map, still parented to the jet, moved to the jet's coordinates every tick by
> `CompAttachBase`. They never change size (1.70132506 across two game hours of ticking). The vehicle reads as burning for ever, so `CompFueledTravel` never auto-refuels
> it. `VehicleStatHandler.TryExtinguishFire` and the game's container auto-extinguish both act on the first fire only, so the rest can never go. Suggested: put attached
> fires out in `VehiclePawn.DeSpawn`, and treat a second attached fire as a fault. Save with the 37 fires available on request.

## Who chose what

- **Owner (10 Nov 2026):** that the fire bug is Vehicle Framework's and predates our mods; to implement a fix.
- **Agent:** the reading of the saves and code, the two-patch shape, the rule, the message, and the removal conditions.

## Sources

`PG5.rws` and `PG5_temp.rws`, read from frozen copies and never written; the game's decompiled source (`Fire`, `AttachableThing`, `CompAttachBase`, `AttachmentUtility`,
`FireUtility`, `ThingOwner`); Vehicle Framework's 1.6 decompile and its public `develop` branch (MIT), summarised, not copied; Combat Extended's and Field Tents' decompiles for
their fire patches, in our words; `python scripts/query-save.py`, `query-patches.py`, `query-defs.py patched Harrier_Jet`.
