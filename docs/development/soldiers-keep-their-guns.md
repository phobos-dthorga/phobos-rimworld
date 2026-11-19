# Soldiers keep their guns (design record, 19 Nov 2026)

**Status (19 Nov 2026):** built in D'thorga Patches 0.26.0 (its Combat Extended part) with D'thorga Framework 0.174.0's
SingleUseWeapons rule. Not yet tested in game. Not built: a fallback to any gun when a colonist's own kind is unknown (an option below).

The owner reported that many soldiers had only grenades and satchel charges equipped as their firearms, and asked first for the reason,
then for a possible fix with settings, before their decision (19 Nov 2026). Everything in "What we found" was read from the save and
from the mods' code; nothing was reproduced in a game.

## Read this first

- **The cause is three systems meeting, none of them broken on its own**: the game drops a downed colonist's gun, Simple Sidearms
  puts a grenade from the pack in the empty hands and then remembers it as their favourite, and Combat Extended's ad-hoc loadout counts
  whatever is held as the weapon. The game's own "go back for your gun" memory is wiped as soon as anything is equipped.
- **The fix is ours, not a change to either mod**: a think-tree node that sends a colonist back to their own gun, a note of that gun
  that survives the wipe, and one call to Simple Sidearms' public API. No Harmony on Combat Extended or Simple Sidearms.
- **Eight colonists in the owner's save have no gun anywhere.** Nothing knows which gun was theirs, so each needs one handed to them
  once (right-click a gun, Equip); after that the fix keeps it.
- **Agent defaults, for the owner to revise:** all three settings on; "same kind" never means a different gun; a recovery that undoes
  itself three times in an hour stops for a day.

## What we found

### In the save (PG15, 19 Nov 2026, `scripts/query-save.py`)

- Nine colonists held a frag grenade (eight) or a satchel charge (one) as their weapon. **Eight of them had no gun in their pack or
  hands**; one (Carlotta) had an M250 in her pack with no ammunition for it.
- **Every one was on the CE loadout "Soldier #1"** or one inheriting from it ("Military Doctor #1"), which lists 3 frag grenades, 1
  satchel charge, medicine, food and drugs, but **no gun**. All the loadouts are ad hoc ("keep whatever gun they hold").
- **Simple Sidearms remembered the grenade as their preferred ranged weapon** (DefaultRangedWeapon) and nothing else, read from the
  raw save for those nine.
- It is old: **PG22 (6 Nov 2026) already had eight colonists holding frag grenades**; Autosave-7 (19 Nov) had eleven.

### In the code (read 19 Nov 2026)

1. **Downed, the game drops the gun, forbidden**, and remembers it (`Pawn_HealthTracker.MakeDowned` calls
   `DropAndForbidEverything(keepInventoryAndEquipmentIfInBed: true, rememberPrimary: true)`; `DropAllEquipment` sets
   `mindState.droppedWeapon`). Its colonist think tree has a job for it, `JobGiver_PickupDroppedWeapon` with `ignoreForbidden`.
2. **That memory is wiped by any equip**: `Pawn_EquipmentTracker.AddEquipment` and the first toil of `JobDriver_Equip` both clear
   `droppedWeapon` (RimWorld 1.7 decompile).
3. **Simple Sidearms gets there first.** Its `JobGiver_QuicklySwitchWeapons` sits ahead of the game's pick-up job in the colonist
   tree (patched Humanlike tree, lines 302 and 336) and, with empty hands, equips the best ranged weapon in the pack by its average
   damage per second (`WeaponAssingment.equipBestWeaponFromInventoryByPreference`, `GettersFilters.findBestRangedWeapon`): the grenade.
   Its "Skip dangerous weapons" setting (off in the owner's game) filters only incendiaries and building destroyers; CE's frag
   grenade is neither, so it would not have helped with frags (it would with the satchel charge).
4. **Combat Extended's loadout equips a missing item with empty hands.** `JobGiver_UpdateLoadout.TryGiveJob` (CE source, lines
   368-395, "Equip gun if unarmed or current gun is not in loadout") gives the game's Equip job, not a pocket job, for any wieldable
   item it fetches while the hands are empty, and grenades are wieldable. Simple Sidearms watches that job
   (`JobDriver_Equip_MakeNewToils_Patches.MemoriseWeaponAboutToBeEquipped` → `InformOfAddedPrimary`) and records the grenade as the
   preferred ranged weapon, forgetting the gun (`InformOfDroppedSidearm(current, intentional: true)` when anything replaces it).
5. **From then on it holds.** CE's ad-hoc loadout adds a virtual slot for whatever is in hand (`Loadout.GetSlotsFor`, commit
   06d8607bb, May 2025), so the grenade counts as the weapon; Simple Sidearms keeps swapping the grenade back in. Nothing ever looks
   for a gun.
6. **A gun that runs dry** is put away by CE for the next viable weapon in the pack (`CompInventory.SwitchToNextViableWeapon`), a
   grenade if that is all there is: Carlotta's case.

Simple Sidearms' author has not updated it for 1.7 (last update 13 Aug 2025, no uploader reply in its last 50 comments,
`workshop-research.py report`, 19 Nov 2026), so by AGENTS.md it counts as abandoned; we still avoid patching it and use its public
`InformOfAddedPrimary` instead. Combat Extended is active and its loadout behaviour is by design; we leave it alone.

## What the behaviour answers for

- **What it lets the player do:** keep soldiers armed through fights where they go down, without checking every colonist's hands
  after each battle and re-equipping them one by one.
- **What it stops them doing:** nothing they do on purpose. Drafted colonists are left to orders; melee weapons and a weapon forced in
  Simple Sidearms are left alone; empty hands alone (a deliberate disarm) never send anyone to fetch a gun of the same kind.
- **How it changes the way they play:** grenades and satchel charges go back to being what the loadout meant, things in the pack to
  throw, and the gun in hand is the one the colonist was given. A downed soldier walks back to their gun after recovering, as the game
  intended, even across a battlefield, even if it was forbidden.

## How it works

- **SingleUseWeapons** (D'thorga Framework core): a ranged weapon is single-use when one of its verbs is the game's `Verb_ShootOneUse`
  or CE's `CombatExtended.Verb_ShootCEOneUse` (found by name). Effects, not names. Everything else ranged is a gun.
- **OwnGuns** (Patches, Combat Extended part): per colonist, the kind of gun last held, the thing ID of the gun dropped when downed (or
  "lost"), and a back-off. One line of text each in D'thorga Framework's part store, so removing the mod leaves no missing-class error.
- **DroppedWhenDownedPatch**: a postfix on `Pawn_EquipmentTracker.DropAllEquipment` that reads the game's own `droppedWeapon` when it
  has just been set and notes it. Reads only.
- **JobGiver_PhobosKeepOwnGun**, inserted before `JobGiver_DropUnusedInventory` in the colonists' branch of the Humanlike tree (so
  before Simple Sidearms' node, whichever loads first):
  - Holding a gun: note it, and if Simple Sidearms' preferred ranged weapon is a single-use one or none, call its
    `InformOfAddedPrimary` with the gun.
  - Holding a single-use weapon: (1) a gun from the pack that CE says can fire (`CompAmmoUser.HasAndUsesAmmoOrMagazine`), swapped in at
    once as Simple Sidearms and CE swap; (2) the dropped gun, by Equip job, ignoring forbidden, if reachable; (3) setting: the nearest
    unforbidden gun of the same Def, loaded.
  - Empty hands: (2), then (3) only if the dropped gun is known lost.
  - Three recoveries within an in-game hour: a one-day back-off and a log line (authored figures).
- Settings under **D'thorga Patches: Combat Extended**: Colonists go back to their own guns; When their own gun is gone, take another
  of the same kind; Simple Sidearms keeps a colonist's gun, not a grenade, as the weapon it hands back. All on.

## Options not built (for the owner)

- **Any gun when their own kind is unknown**: would re-arm the eight unarmed soldiers by itself, but needs a rule for which gun (CE
  ammunition on hand, quality, value). Handing each a gun once is quicker and keeps the choice the owner's.
- **Pocketing grenades in CE's loadout instead of equipping them** (a postfix on `JobGiver_UpdateLoadout`): would stop the grenade
  reaching the hands at all, but changes an active author's design; not done. A report to CE's team could be drafted if wanted.
- **Fetching ammunition for a gun in the pack that has run dry** (Carlotta): CE's ad-hoc loadout fetches ammunition only for the
  weapon in hand.
- **Adding a gun slot to the "Soldier #1" loadout** is the owner's own fix in CE, with no code; it pins every soldier to one kind.
