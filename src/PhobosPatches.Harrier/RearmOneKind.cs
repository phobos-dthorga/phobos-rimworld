using System;
using System.Reflection;
using HarmonyLib;
using Phobos.Framework.Rules;
using RimWorld;
using Vehicles;
using Verse;
using Verse.AI;

namespace Phobos.VehiclePatches;

/// <summary>
/// One kind of ammunition to a magazine while the Harrier rearms (owner, 9 Nov 2026: "absolutely everybody comes out", and the counter goes up, down, up, down). Read in its framework's WorkGiver_RearmVehicle and
/// GroundRearm: with no kind chosen, a colonist fetches any kind in stock; a different kind arriving throws the whole magazine on the ground before loading; and each colonist only counts others carrying the same kind.
/// With HE and HEAT both in store, the two kept evicting each other, each eviction asked for a whole new magazine, and that called out everyone. Choosing a kind on the turret still swaps it as before.
/// The author is active, so this should go if their mod fixes it. Every member of theirs is reached by name: this assembly does not reference theirs.
/// </summary>
internal static class RearmOneKind
{
    const string RearmJob = "FWA_RearmVehicle";

    internal static readonly Func<Pawn, VehicleTurret, Predicate<ThingDef>, int, Thing?>? FindAmmo = MakeFindAmmo();

    static Func<Pawn, VehicleTurret, Predicate<ThingDef>, int, Thing?>? MakeFindAmmo()
    {
        MethodInfo? method = AccessTools.Method("FixedWingAircraftFramework.WorkGiver_RearmVehicle:FindAmmo");
        return method == null ? null : AccessTools.MethodDelegate<Func<Pawn, VehicleTurret, Predicate<ThingDef>, int, Thing?>>(method);
    }

    internal static ThingDef? KindToKeep(VehicleTurret turret) =>
        RearmRule.KindToKeep(turret.loadedAmmo, turret.shellCount, turret.loadedAmmo != null && (turret.loadConfig == null || turret.loadConfig.IsEnabled(turret.loadedAmmo)));

    /// <summary>The kind another colonist is already bringing to this turret, if any.</summary>
    internal static ThingDef? OnItsWay(Pawn self, VehicleTurret turret)
    {
        VehiclePawn vehicle = turret.vehicle;
        foreach (Pawn other in vehicle.Map.mapPawns.SpawnedPawnsInFaction(self.Faction))
        {
            Job? job = other.CurJob;
            if (other == self || job == null || job.def.defName != RearmJob || job.targetB.Thing != vehicle) continue;
            ThingDef? kind = job.targetA.Thing?.def;
            if (kind != null && turret.def.ammunition.Allows(kind)) return kind;
        }
        return null;
    }
}

/// <summary>A postfix on the framework's choice of ammunition when no kind is chosen: fetch what the magazine holds or what is already on its way, or nothing. Nothing lets its loop move on to the next weapon.</summary>
[HarmonyPatch]
public static class RearmOneKindFallbackPatch
{
    static bool Prepare() => TargetMethod() != null && RearmOneKind.FindAmmo != null;

    static MethodBase? TargetMethod() => AccessTools.Method("FixedWingAircraftFramework.WorkGiver_RearmVehicle:FindFallback");

    public static void Postfix(ref Thing? __result, Pawn pawn, VehicleTurret turret, int minStack)
    {
        if (__result == null) return;
        ThingDef? kind = RearmRule.KindToBring(RearmOneKind.KindToKeep(turret), RearmOneKind.OnItsWay(pawn, turret));
        if (kind == null || __result.def == kind) return;
        __result = RearmOneKind.FindAmmo!(pawn, turret, def => def == kind, minStack);
    }
}

/// <summary>
/// A prefix on loading: a different kind arriving does not throw out what is loaded. It skips the original only in that case, because the original's first act is to empty the magazine (recorded reason).
/// Answering 0 makes the framework's job set the load down, and it is hauled back to storage. A kind the player chose goes through as before.
/// </summary>
[HarmonyPatch]
public static class RearmOneKindLoadPatch
{
    static bool Prepare() => TargetMethod() != null;

    static MethodBase? TargetMethod() => AccessTools.Method("FixedWingAircraftFramework.GroundRearm:LoadDirect");

    public static bool Prefix(ref int __result, VehicleTurret turret, Thing ammo)
    {
        if (RearmRule.MayLoad(RearmOneKind.KindToKeep(turret), ammo.def, turret.savedAmmoType)) return true;
        __result = 0;
        return false;
    }
}
