using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Whether an aircraft's engine can hold it in a hover now, for frameworks that land on a pad (the turbine design's hover margin).
/// The player's setting decides what follows (owner, 9 Nov 2026): refuse the pad and use the runway (D'thorga Patches gates the
/// Harrier's pad landings through this), allow it with the engine run past its limits, or warn only. A tie-in asks; this decides.
/// </summary>
public static class HoverLimits
{
    /// <summary>True when the engine model judges hovers for this vehicle and cannot hold one now, dry or with water.</summary>
    public static bool Beyond(VehiclePawn vehicle)
    {
        if (VehicleSystemsMod.Settings.drivetrainMode != DrivetrainMode.Full) return false;
        Powertrain? train = CompPhobosVehicleSystems.Of(vehicle)?.Powertrain;
        if (train == null || !train.JudgesHover) return false;
        (bool dry, bool wet, double _) = train.HoverCheck();
        return !dry && !wet;
    }

    /// <summary>True when the player chose to refuse the pad and this hover is beyond the engine (never with figures only).</summary>
    public static bool RefusesPad(VehiclePawn vehicle) =>
        VehicleSystemsMod.Settings.hoverLimit == VehicleSystemsSettings.HoverLimit.RefusePadUseRunway && !VehicleSystemsMod.Settings.showOnly && Beyond(vehicle);

    /// <summary>True when lift-off through Vehicle Framework's own launch is checked, the player chose to refuse, and the engines
    /// cannot lift the aircraft now (never with figures only).</summary>
    public static bool RefusesLaunch(VehiclePawn vehicle)
    {
        if (VehicleSystemsMod.Settings.hoverLimit != VehicleSystemsSettings.HoverLimit.RefusePadUseRunway || VehicleSystemsMod.Settings.showOnly) return false;
        Powertrain? train = CompPhobosVehicleSystems.Of(vehicle)?.Powertrain;
        return train != null && train.ChecksLiftAtLaunch && Beyond(vehicle);
    }

    /// <summary>The in-world reason for a refused lift-off.</summary>
    public static string LaunchRefusalText(VehiclePawn vehicle) =>
        "PhobosVehicleSystems_RefuseLiftOff".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve();

    /// <summary>Lift-off began. Beyond the engines, "allow at a cost" wears the turbines' hot section (or the engines without one) and
    /// says so; "warn only" says so. Inside the engines, nothing happens.</summary>
    public static void OnLaunch(VehiclePawn vehicle)
    {
        CompPhobosVehicleSystems? comp = CompPhobosVehicleSystems.Of(vehicle);
        if (comp?.Powertrain == null || !comp.Powertrain.ChecksLiftAtLaunch || !Beyond(vehicle)) return;
        if (VehicleSystemsMod.Settings.hoverLimit == VehicleSystemsSettings.HoverLimit.WarnOnly)
        {
            Messages.Message("PhobosVehicleSystems_MsgLiftBeyond".Translate(vehicle.LabelShort.Named("VEHICLE")), vehicle, MessageTypeDefOf.CautionInput);
            return;
        }
        if (!VehicleSystemsMod.Settings.showOnly)
        {
            if (comp.TryGetQuantity("hotSectionLife", out _)) comp.AddToQuantity("hotSectionLife", LaunchStrainLife);
            else comp.WearRole("Engine", LaunchStrainWear);
        }
        Messages.Message("PhobosVehicleSystems_MsgLiftStrained".Translate(vehicle.LabelShort.Named("VEHICLE")), vehicle, MessageTypeDefOf.CautionInput);
    }

    /// <summary>Hot-section life one over-limit lift-off uses (authored: about five hours past the continuous limit's worst).</summary>
    public const float LaunchStrainLife = 0.1f;
    /// <summary>Engine health one over-limit lift-off costs without a turbine system (authored).</summary>
    public const float LaunchStrainWear = 8f;

    /// <summary>The in-world reason for a refused pad landing.</summary>
    public static string RefusalText(VehiclePawn vehicle) =>
        "PhobosVehicleSystems_MsgHoverRefused".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve();
}
