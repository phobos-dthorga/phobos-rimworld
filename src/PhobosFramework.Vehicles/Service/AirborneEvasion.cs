using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Phobos.Framework;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A dodge of our own for an aircraft whose own mod gives it none (Framework 0.203.0; owner, 21 Nov 2026, for Close Air Support's A-10 and
/// Su-25: "build a dodge"; docs/development/close-air-support-jets.md). Its loadout's avionics name the chance a shot misses while it is in the
/// air on a map (<see cref="AvionicsFit.mapEvasion"/>); <see cref="AirCombat.Evasion"/> scales that by its flight controls and warning receiver
/// and adds a salvo's bonus, exactly as for the Harrier's own figure. An aircraft whose own mod has a dodge (the Harrier, the Hind, the
/// Mongoose) carries no mapEvasion, so no shot is dodged twice. Shots fired over the top (mortars) are left alone, as are its own.
/// Works for any faction's aircraft with a loadout that sets the figure: raiders' jets dodge the player's shots the same way.
/// </summary>
public static class AirborneEvasion
{
    /// <summary>The cells a missed shot is sent to lie this far beyond the aircraft's footprint.</summary>
    const int MissRing = 2;

    /// <summary>The chance this shot at the vehicle misses because it dodged: 0 on the ground, for its own shots, or without the figure.</summary>
    public static float ChanceFor(VehiclePawn vehicle, Thing? launcher)
    {
        if (launcher == vehicle || !vehicle.Spawned || vehicle.Destroyed) return 0f;
        AvionicsFit? fit = AirCombat.Systems(vehicle)?.Props.loadout?.avionics;
        bool dodges = fit is { mapEvasion: > 0f };
        // A self-protection jamming pod (Framework 0.226.0) fools an automated turret's aim, on any aircraft, its own dodge or not.
        float pod = EcmPod.ChanceAgainst(vehicle, launcher);
        if (!dodges && pod <= 0f) return 0f;
        if (!OnMapFlight.Airborne(vehicle)) return 0f;
        float dodge = dodges ? AirCombat.Evasion(vehicle, Mathf.Clamp01(fit!.mapEvasion)) : 0f;
        return 1f - (1f - dodge) * (1f - pod);
    }

    /// <summary>A cell just beyond the footprint for a dodged shot to fly at, on the far side from where it came where one is on the map, so
    /// it passes the aircraft by (it may still hit whatever stands there, as any miss may).</summary>
    public static bool TryMissCell(VehiclePawn vehicle, Vector3 origin, out IntVec3 cell)
    {
        Map map = vehicle.Map;
        float centre = (vehicle.Position.ToVector3Shifted() - origin).MagnitudeHorizontalSquared();
        var beyond = new List<IntVec3>();
        var near = new List<IntVec3>();
        foreach (IntVec3 edge in vehicle.OccupiedRect().ExpandedBy(MissRing).EdgeCells)
        {
            if (!edge.InBounds(map)) continue;
            ((edge.ToVector3Shifted() - origin).MagnitudeHorizontalSquared() >= centre ? beyond : near).Add(edge);
        }
        return (beyond.Count > 0 ? beyond : near).TryRandomElement(out cell);
    }

    /// <summary>Notes a dodge for the test list.</summary>
    public static void Noted(VehiclePawn vehicle) => Seen.Note("aircraft.own-evasion", vehicle);
}

/// <summary>
/// The dodge for the game's own shots: a shot launched as a hit on an aircraft with our dodge is, by its chance, aimed instead at a cell just
/// beyond it. A prefix that changes only where the shot is aimed and never skips the launch; at low priority, after the aircraft mods' own
/// stray-hit patches (Close Air Support's turns a miss into the airframe into a hit), so those hits can be dodged too.
/// </summary>
[HarmonyPatch(typeof(Projectile), nameof(Projectile.Launch),
    new[] { typeof(Thing), typeof(Vector3), typeof(LocalTargetInfo), typeof(LocalTargetInfo), typeof(ProjectileHitFlags), typeof(bool), typeof(Thing), typeof(ThingDef) })]
[HarmonyPriority(Priority.Low)]
public static class AirborneEvasionLaunchPatch
{
    public static void Prefix(Projectile __instance, Thing launcher, Vector3 origin, ref LocalTargetInfo usedTarget)
    {
        if (usedTarget.Thing is not VehiclePawn vehicle || __instance.def?.projectile is not { flyOverhead: false }) return;
        float chance = AirborneEvasion.ChanceFor(vehicle, launcher);
        if (chance <= 0f || !Rand.Chance(chance) || !AirborneEvasion.TryMissCell(vehicle, origin, out IntVec3 cell)) return;
        usedTarget = cell;
        AirborneEvasion.Noted(vehicle);
    }
}

/// <summary>
/// The dodge under Combat Extended, whose shots are its own projectiles and find what they hit by testing each thing on their path: by its
/// chance, rolled once for each shot and aircraft so the answer holds along the whole path, the shot passes through. A postfix that only ever
/// turns a hit into a pass. Reached by name, as no core assembly references Combat Extended; read from its source at
/// ProjectileCE.cs:1302 (CanCollideWith(Thing thing, out float dist), and its public launcher field at :149).
/// </summary>
[HarmonyPatch]
public static class AirborneEvasionCombatExtendedPatch
{
    static FieldInfo? launcherField;

    static bool Prepare() => TargetMethod() != null && launcherField != null;

    static MethodBase? TargetMethod()
    {
        MethodInfo? method = AccessTools.Method("CombatExtended.ProjectileCE:CanCollideWith", new[] { typeof(Thing), typeof(float).MakeByRefType() });
        if (method != null) launcherField ??= AccessTools.Field("CombatExtended.ProjectileCE:launcher");
        return method;
    }

    public static void Postfix(object __instance, Thing thing, ref bool __result)
    {
        if (!__result || thing is not VehiclePawn vehicle || __instance is not Thing shot || shot.def?.projectile is not { flyOverhead: false }) return;
        float chance = AirborneEvasion.ChanceFor(vehicle, launcherField?.GetValue(shot) as Thing);
        if (chance <= 0f || !Rand.ChanceSeeded(chance, Gen.HashCombineInt(shot.thingIDNumber, vehicle.thingIDNumber))) return;
        __result = false;
        AirborneEvasion.Noted(vehicle);
    }
}
