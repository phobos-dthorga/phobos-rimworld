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

    /// <summary>The in-world reason for a refused pad landing.</summary>
    public static string RefusalText(VehiclePawn vehicle) =>
        "PhobosVehicleSystems_MsgHoverRefused".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve();
}
