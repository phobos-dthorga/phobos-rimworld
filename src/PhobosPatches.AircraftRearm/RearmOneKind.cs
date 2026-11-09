using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Phobos.Framework;
using Phobos.Framework.Rules;
using RimWorld;
using Vehicles;
using Verse;
using Verse.AI;

namespace Phobos.AircraftRearmPatches;

/// <summary>
/// One kind of ammunition to a magazine while an aircraft rearms (owner, 9 Nov 2026: on landing "absolutely everybody comes out" and the counter goes up, down, up, down). The Harrier, the Hind and the Mongoose share one author's
/// rearm code, each with its own copy under its own namespace. Read in WorkGiver_RearmVehicle and GroundRearm: with no kind chosen, a colonist fetches any kind in stock; a different kind arriving throws the whole magazine on the ground
/// before loading; and each colonist only counts others carrying the same kind. With two kinds in store the two kept evicting each other, each eviction asked for a whole new magazine, and that called out everyone. Choosing a kind on the
/// weapon still swaps it as before. The author is active, so this should go if their mods fix it. Every member of theirs is reached by name: this assembly references none of them.
/// </summary>
public static class RearmOneKind
{
    sealed record Aircraft(string PackageId, string Label, string Namespace, string JobDefName);

    static readonly Aircraft[] Aircrafts =
    {
        new("potemkinvillager.av8bharrier", "the Harrier", "FixedWingAircraftFramework", "FWA_RearmVehicle"),
        new("potemkinvillager.mi24hindhelicopter", "the Hind", "Mi24HindHelicopter", "Hind_RearmVehicle"),
        new("potemkinvillager.mongooseattackhelicopter", "the Mongoose", "MongooseAttackHelicopter", "Mongoose_RearmVehicle"),
    };

    static readonly Dictionary<Type, Func<Pawn, VehicleTurret, Predicate<ThingDef>, int, Thing?>> findAmmo = new();
    static readonly HashSet<string> rearmJobs = new();

    public static void Apply(Harmony harmony, LoadReport report)
    {
        foreach (Aircraft aircraft in Aircrafts)
        {
            if (!ModsConfig.IsActive(aircraft.PackageId)) continue;
            string feature = "rearming " + aircraft.Label + " keeps to one kind of ammunition";
            Type? giver = AccessTools.TypeByName(aircraft.Namespace + ".WorkGiver_RearmVehicle");
            Type? ground = AccessTools.TypeByName(aircraft.Namespace + ".GroundRearm");
            MethodInfo? fallback = giver == null ? null : AccessTools.Method(giver, "FindFallback");
            MethodInfo? find = giver == null ? null : AccessTools.Method(giver, "FindAmmo");
            MethodInfo? load = ground == null ? null : AccessTools.Method(ground, "LoadDirect");
            if (giver == null || fallback == null || find == null || load == null)
            {
                report.Off(feature, "its code has changed");
                continue;
            }
            findAmmo[giver] = AccessTools.MethodDelegate<Func<Pawn, VehicleTurret, Predicate<ThingDef>, int, Thing?>>(find);
            rearmJobs.Add(aircraft.JobDefName);
            harmony.Patch(fallback, postfix: new HarmonyMethod(typeof(RearmOneKind), nameof(FallbackPostfix)));
            harmony.Patch(load, prefix: new HarmonyMethod(typeof(RearmOneKind), nameof(LoadPrefix)));
            report.On(feature);
        }
    }

    static ThingDef? KindToKeep(VehicleTurret turret) =>
        RearmRule.KindToKeep(turret.loadedAmmo, turret.shellCount, turret.loadedAmmo != null && (turret.loadConfig == null || turret.loadConfig.IsEnabled(turret.loadedAmmo)));

    /// <summary>The kind another colonist is already bringing to this weapon, if any.</summary>
    static ThingDef? OnItsWay(Pawn self, VehicleTurret turret)
    {
        VehiclePawn vehicle = turret.vehicle;
        foreach (Pawn other in vehicle.Map.mapPawns.SpawnedPawnsInFaction(self.Faction))
        {
            Job? job = other.CurJob;
            if (other == self || job == null || !rearmJobs.Contains(job.def.defName) || job.targetB.Thing != vehicle) continue;
            ThingDef? kind = job.targetA.Thing?.def;
            if (kind != null && turret.def.ammunition.Allows(kind)) return kind;
        }
        return null;
    }

    /// <summary>A postfix on the choice of ammunition when no kind is chosen: fetch what the magazine holds or what is already on its way, or nothing. Nothing lets its loop move on to the next weapon.</summary>
    public static void FallbackPostfix(ref Thing? __result, MethodBase __originalMethod, Pawn pawn, VehicleTurret turret, int minStack)
    {
        if (__result == null) return;
        ThingDef? kind = RearmRule.KindToBring(KindToKeep(turret), OnItsWay(pawn, turret));
        if (kind == null || __result.def == kind) return;
        __result = findAmmo[__originalMethod.DeclaringType!](pawn, turret, def => def == kind, minStack);
    }

    /// <summary>
    /// A prefix on loading: a different kind arriving does not throw out what is loaded. It skips the original only in that case, because the original's first act is to empty the magazine (recorded reason).
    /// Answering 0 makes the rearm job set the load down, and it is hauled back to storage. A kind the player chose goes through as before.
    /// </summary>
    public static bool LoadPrefix(ref int __result, VehicleTurret turret, Thing ammo)
    {
        if (RearmRule.MayLoad(KindToKeep(turret), ammo.def, turret.savedAmmoType)) return true;
        __result = 0;
        return false;
    }
}
