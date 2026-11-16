using System.Collections.Generic;
using Phobos.Framework.Defs;
using Phobos.Framework.Vehicles.Rules;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Mobility kills (Framework 0.115.0; owner, 16 and 17 Nov 2026; docs/development/vehicle-knocked-out-systems.md, M1 and M2): a thrown
/// track stops a tracked vehicle, and a vehicle that cannot move says why. Vehicle Framework averages the parts that feed speed, so a tank
/// with a track gone drove on at about three quarters; its own "priority" flag cannot do this, as it averages those parts too (a dead engine
/// beside two good tracks would give a third). Applied through our speed stat part, read only when Vehicle Framework recalculates the stat
/// (after damage, a repair or its rare scan), on every faction's vehicle with our systems. Nothing is saved.
/// </summary>
public static class Mobility
{
    /// <summary>Vehicle Framework's own line: a vehicle at or under this speed cannot move, and is undrafted (`VehiclePawn.CanMove`).</summary>
    public const float StoppedAtOrBelow = 0.1f;

    /// <summary>At start-up: which of the design's parts are tracks. Speed is read by name: this runs while Defs are still resolved.</summary>
    public static void Apply(VehicleDef def, CompProperties_PhobosVehicleSystems props)
    {
        PhobosKnockOutRulesDef rules = PhobosRulesDef.Current<PhobosKnockOutRulesDef>();
        if (!rules.thrownTracks) return;
        VehicleStatDef? speed = DefDatabase<VehicleStatDef>.GetNamedSilentFail("MoveSpeed");
        if (speed == null) return;
        foreach (VehicleComponentProperties part in def.components)
        {
            if (MobilityRule.IsTrack(part.key, part.categories?.Contains(speed) == true, rules.trackKeySuffixes)) props.trackKeys.Add(part.key);
        }
    }

    /// <summary>The first thrown track, or null.</summary>
    public static VehicleComponent? ThrownTrack(VehiclePawn vehicle, CompProperties_PhobosVehicleSystems props)
    {
        if (props.trackKeys.Count == 0 || vehicle.statHandler == null) return null;
        float thrown = PhobosRulesDef.Current<PhobosKnockOutRulesDef>().trackThrownAtOrBelow;
        foreach (string key in props.trackKeys)
        {
            VehicleComponent? track = vehicle.statHandler.GetComponent(key);
            if (track != null && MobilityRule.Thrown(track.HealthPercent, thrown)) return track;
        }
        return null;
    }

    /// <summary>Whether damage has stopped the vehicle: its speed is at Vehicle Framework's line.</summary>
    public static bool Stopped(VehiclePawn vehicle) =>
        vehicle.statHandler != null && vehicle.GetStatValue(VehicleStatDefOf.MoveSpeed) <= StoppedAtOrBelow;

    /// <summary>The part to name as why the vehicle cannot move, or null.</summary>
    public static VehicleComponent? Cause(VehiclePawn vehicle, CompProperties_PhobosVehicleSystems props)
    {
        var candidates = new List<VehicleComponent>();
        var parts = new List<SpeedPart>();
        foreach (VehicleComponent part in vehicle.statHandler.components)
        {
            if (part.props.categories == null || !part.props.categories.Contains(VehicleStatDefOf.MoveSpeed)) continue;
            candidates.Add(part);
            bool track = props.trackKeys.Contains(part.props.key);
            parts.Add(new SpeedPart(track ? part.HealthPercent : part.Efficiency, track, part.props.priorityStatEfficiency));
        }
        int index = MobilityRule.Cause(parts, PhobosRulesDef.Current<PhobosKnockOutRulesDef>().trackThrownAtOrBelow);
        return index < 0 ? null : candidates[index];
    }

    /// <summary>"Cannot move: left track destroyed." for a vehicle damage has stopped; null otherwise.</summary>
    public static string? Line(VehiclePawn vehicle, CompProperties_PhobosVehicleSystems props)
    {
        if (!Stopped(vehicle)) return null;
        VehicleComponent? cause = Cause(vehicle, props);
        if (cause == null) return "PhobosVehicleSystems_InspectCannotMove".Translate().Resolve();
        string part = cause.props.label ?? cause.props.key;
        return (cause.Health <= 0
            ? "PhobosVehicleSystems_InspectCannotMoveDestroyed".Translate(part.Named("PART"))
            : "PhobosVehicleSystems_InspectCannotMoveDamaged".Translate(part.Named("PART"))).Resolve();
    }
}
