using System;
using System.Reflection;
using HarmonyLib;
using Phobos.Framework;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.CombatExtendedPatches.KeepGuns;

/// <summary>
/// Puts a colonist's own gun back in their hands when they are left holding a grenade, a satchel charge or nothing (Patches 0.26.0;
/// docs/development/soldiers-keep-their-guns.md). It sits in the colonists' think tree before Simple Sidearms' quick-switch and Combat
/// Extended's loadouts, which would otherwise put a grenade from the pack in empty hands, and runs only when a person looks for something to do,
/// never every tick. In order, it takes: a gun in the pack that can fire; the gun they let fall when downed (fetched even if forbidden, as the
/// game's own job does); then, with its setting on, another gun of the same kind lying unforbidden on the map with rounds in it. Drafted people
/// are left to the player's orders. Holding a gun, it only notes the gun and keeps it as Simple Sidearms' preferred one.
/// </summary>
public class JobGiver_PhobosKeepOwnGun : ThinkNode_JobGiver
{
    protected override Job? TryGiveJob(Pawn pawn)
    {
        if (!CombatExtendedPatchesMod.Settings.keepGuns || !KeepGunsRule.IsOurSoldier(pawn) || pawn.Drafted || pawn.Downed) return null;
        ThingWithComps? primary = pawn.equipment?.Primary;
        if (primary != null && SingleUseWeapons.IsGun(primary.def))
        {
            KeepGunsRule.NoteHeld(pawn, primary);
            return null;
        }
        bool holdsSingleUse = primary != null && SingleUseWeapons.IsSingleUse(primary.def);
        // A melee weapon or anything else that is not a weapon used up when fired is the player's business.
        if (primary != null && !holdsSingleUse) return null;
        if (!KeepGunsRule.CanTakeUpArms(pawn)) return null;
        if (OwnGuns.For(pawn, make: false) is not { } own) return null;
        int now = Find.TickManager.TicksGame;
        if (now < own.BackOffUntil) return null;
        // A single-use weapon the player forced through Simple Sidearms stays where it is.
        if (holdsSingleUse && SidearmsMemory.Forced(pawn) is { } forced && SingleUseWeapons.IsSingleUse(forced)) return null;

        // 1. A gun that can fire, already in the pack: in hand at once, as Simple Sidearms and Combat Extended switch weapons.
        if (holdsSingleUse && KeepGunsRule.BestPackedGun(pawn, own) is { } packed)
        {
            if (KeepGunsRule.SwapFromPack(pawn, packed))
            {
                KeepGunsRule.Recovered(pawn, own, packed, "pack", now);
                KeepGunsRule.NoteHeld(pawn, packed);
            }
            return null;
        }

        // 2. The gun they let fall when downed, wherever it lies on this map, even forbidden (as the game's own job fetches it).
        if (KeepGunsRule.DroppedGun(pawn, own) is { } dropped)
        {
            KeepGunsRule.Recovered(pawn, own, dropped, "dropped", now);
            Job job = JobMaker.MakeJob(JobDefOf.Equip, dropped);
            job.ignoreForbidden = true;
            return job;
        }

        // 3. Their own is lost: another of the same kind, unforbidden and loaded, from the map (setting). Empty hands alone are not enough,
        // since the player may have disarmed them on purpose: they must hold a single-use weapon, or have lost the gun they dropped when downed.
        if (CombatExtendedPatchesMod.Settings.fetchSameKind && (holdsSingleUse || own.DroppedId == OwnGun.Lost)
            && own.DroppedId < 0 && KeepGunsRule.SameKindNearby(pawn, own) is { } same)
        {
            KeepGunsRule.Recovered(pawn, own, same, "same-kind", now);
            return JobMaker.MakeJob(JobDefOf.Equip, same);
        }
        return null;
    }
}

/// <summary>The rules behind <see cref="JobGiver_PhobosKeepOwnGun"/>, kept apart from the think node so the drop hook shares them.</summary>
public static class KeepGunsRule
{
    /// <summary>Three recoveries within this time mean something keeps undoing them (authored: one in-game hour).</summary>
    const int LoopWindowTicks = GenDate.TicksPerHour;
    const int LoopLimit = 3;
    /// <summary>How long to leave a person alone after that (authored: one in-game day).</summary>
    const int BackOffTicks = GenDate.TicksPerDay;

    const string AmmoUserType = "CombatExtended.CompAmmoUser";
    static bool lookedForAmmo;
    static Type? ammoUser;
    static PropertyInfo? canFire;

    /// <summary>A person of the player's own faction who carries weapons: not a prisoner, not an animal or a machine.</summary>
    public static bool IsOurSoldier(Pawn pawn) =>
        pawn.Faction is { IsPlayer: true } && pawn.RaceProps?.Humanlike == true && !pawn.IsPrisoner && pawn.equipment != null && pawn.inventory != null;

    /// <summary>Able and willing to hold a gun: not incapable of violence, and with hands that work.</summary>
    public static bool CanTakeUpArms(Pawn pawn) =>
        !pawn.WorkTagIsDisabled(WorkTags.Violent) && pawn.health?.capacities != null && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation);

    /// <summary>Notes the gun in hand as this person's own, and keeps it as Simple Sidearms' preferred ranged weapon when that was a grenade.</summary>
    public static void NoteHeld(Pawn pawn, ThingWithComps gun)
    {
        OwnGun? own = OwnGuns.For(pawn, make: true);
        if (own != null && (own.GunDef != gun.def.defName || own.StuffDef != gun.Stuff?.defName || own.DroppedId != OwnGun.None))
        {
            own.GunDef = gun.def.defName;
            own.StuffDef = gun.Stuff?.defName;
            own.DroppedId = OwnGun.None;
            OwnGuns.Save();
        }
        if (CombatExtendedPatchesMod.Settings.fixSidearms && SidearmsMemory.KeepAsPreferred(pawn, gun))
        {
            if (Seen.Wants("patches.keep-guns.sidearms-preference", pawn)) Seen.Note("patches.keep-guns.sidearms-preference", pawn, gun.LabelShort);
            if (CombatExtendedPatchesMod.Logger.DebugEnabled)
                CombatExtendedPatchesMod.Logger.Debug(pawn.LabelShort + "'s " + gun.LabelShort + " is their preferred ranged weapon in Simple Sidearms again, in place of a grenade or nothing.");
        }
    }

    /// <summary>The best gun in the pack that can fire now: their own kind first, then the dearest.</summary>
    public static ThingWithComps? BestPackedGun(Pawn pawn, OwnGun own)
    {
        ThingWithComps? best = null;
        float bestScore = float.MinValue;
        foreach (Thing thing in pawn.inventory.innerContainer)
        {
            if (thing is not ThingWithComps gun || !SingleUseWeapons.IsGun(gun.def) || !CanFire(gun) || !EquipmentUtility.CanEquip(gun, pawn)) continue;
            float score = gun.MarketValue + (gun.def.defName == own.GunDef ? 1_000_000f : 0f);
            if (score > bestScore)
            {
                best = gun;
                bestScore = score;
            }
        }
        return best;
    }

    /// <summary>The single-use weapon in hand goes into the pack and the gun comes out of it. False, with nothing changed, if either step fails.</summary>
    public static bool SwapFromPack(Pawn pawn, ThingWithComps gun)
    {
        ThingOwner pack = pawn.inventory.innerContainer;
        ThingWithComps? held = pawn.equipment.Primary;
        if (held != null && !pawn.equipment.TryTransferEquipmentToContainer(held, pack)) return false;
        if (!pack.Remove(gun))
        {
            if (held != null && pack.Remove(held)) pawn.equipment.AddEquipment(held);
            return false;
        }
        pawn.equipment.AddEquipment(gun);
        return true;
    }

    /// <summary>The gun this person let fall when downed, while it lies on their map where they can reach it; forgotten once it is gone.</summary>
    public static ThingWithComps? DroppedGun(Pawn pawn, OwnGun own)
    {
        if (own.DroppedId < 0) return null;
        ThingDef? def = own.Def;
        ThingWithComps? found = null;
        if (def != null && pawn.Map != null)
        {
            foreach (Thing thing in pawn.Map.listerThings.ThingsOfDef(def))
            {
                if (thing.thingIDNumber == own.DroppedId)
                {
                    found = thing as ThingWithComps;
                    break;
                }
            }
        }
        if (found == null || found.Destroyed)
        {
            // Not on this map, or destroyed: counted as lost, so the same-kind step can take over.
            if (pawn.Map != null)
            {
                own.DroppedId = OwnGun.Lost;
                OwnGuns.Save();
            }
            return null;
        }
        if (!found.Spawned || found.IsBurning() || !EquipmentUtility.CanEquip(found, pawn)) return null;
        if (!pawn.CanReserveAndReach(found, PathEndMode.ClosestTouch, Danger.Deadly)) return null;
        return found;
    }

    /// <summary>The nearest unforbidden gun of this person's own kind on the map that has rounds in it (or needs none).</summary>
    public static ThingWithComps? SameKindNearby(Pawn pawn, OwnGun own)
    {
        ThingDef? def = own.Def;
        if (def == null || pawn.Map == null) return null;
        Thing? found = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(def), PathEndMode.ClosestTouch,
            TraverseParms.For(pawn), 9999f,
            t => t is ThingWithComps gun && !gun.IsForbidden(pawn) && !gun.IsBurning() && CanFire(gun) && EquipmentUtility.CanEquip(gun, pawn)
                 && pawn.CanReserve(gun));
        return found as ThingWithComps;
    }

    /// <summary>Counts a recovery, backs off for a day when they keep coming undone, and notes it for the test list.</summary>
    public static void Recovered(Pawn pawn, OwnGun own, Thing gun, string how, int now)
    {
        if (now - own.FirstRecoveryTick > LoopWindowTicks)
        {
            own.FirstRecoveryTick = now;
            own.Recoveries = 0;
        }
        own.Recoveries++;
        if (own.Recoveries >= LoopLimit)
        {
            own.BackOffUntil = now + BackOffTicks;
            own.Recoveries = 0;
            CombatExtendedPatchesMod.Logger.Info(pawn.LabelShort + " has been given " + gun.LabelShort + " back " + LoopLimit
                + " times within an hour and something keeps taking it away; leaving them be for a day.");
        }
        OwnGuns.Save();
        if (Seen.Wants("patches.keep-guns.recovered", pawn, how)) Seen.Note("patches.keep-guns.recovered", pawn, gun.LabelShort, how);
        if (CombatExtendedPatchesMod.Logger.DebugEnabled)
            CombatExtendedPatchesMod.Logger.Debug(pawn.LabelShort + " is taking up " + gun.LabelShort + " again (" + how + ").");
    }

    /// <summary>Whether a gun can fire now: Combat Extended's own answer where it has one (rounds loaded, or its ammunition in the holder's
    /// pack, or none needed), true otherwise.</summary>
    static bool CanFire(ThingWithComps gun)
    {
        if (!lookedForAmmo)
        {
            lookedForAmmo = true;
            ammoUser = AccessTools.TypeByName(AmmoUserType);
            if (ammoUser != null) canFire = AccessTools.Property(ammoUser, "HasAndUsesAmmoOrMagazine");
        }
        if (ammoUser == null || canFire == null) return true;
        foreach (ThingComp comp in gun.AllComps)
        {
            if (!ammoUser.IsInstanceOfType(comp)) continue;
            try
            {
                return canFire.GetValue(comp) is true;
            }
            catch (Exception)
            {
                return true;
            }
        }
        return true;
    }
}
