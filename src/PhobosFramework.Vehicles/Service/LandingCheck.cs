using System;
using HarmonyLib;
using Phobos.Framework.Rules;
using Phobos.Framework.WorldMap;
using RimWorld;
using RimWorld.Planet;
using SmashTools.Targeting;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Whether an aircraft can hover onto the ground at the end of a world-map journey, in the air of the place it lands
/// (Framework 0.140.0; docs/development/vehicle-altitude-and-turbines.md, "The landing check"). The lift check before launch reads the
/// air of the place it leaves; a journey to a higher or hotter place, or one on which an engine was lost, could land beyond its engines
/// without a word. The player's hover-limit setting decides what follows: refuse (the default) means a rolling landing, as a helicopter
/// comes in when it cannot hover and as the Harrier takes its runway; allow at a cost runs the engines hard; warn only says so. Judged
/// only for aircraft whose lift is checked at launch (Vehicle Framework's own helicopters and pods), with the full engine model on.
/// </summary>
public static class LandingCheck
{
    public enum Outcome
    {
        /// <summary>It can hover there, or the check does not apply.</summary>
        Fine,
        /// <summary>It comes in rolling: the undercarriage takes wear (refuse).</summary>
        RunOn,
        /// <summary>It lands vertically with its engines past their limits (allow at a cost).</summary>
        Strained,
        /// <summary>It lands vertically and the player is told (warn only, or figures only).</summary>
        Warned,
    }

    /// <summary>The share of one undercarriage part's full health a rolling landing costs (authored, 18 Nov 2026: a firm landing on
    /// unprepared ground, not a crash; about one in twenty such landings' worth of a part).</summary>
    public const float RunOnWearShare = 0.05f;

    /// <summary>The engine model that judges this aircraft's landing, or null when none does.</summary>
    static Powertrain? Judge(VehiclePawn vehicle)
    {
        if (VehicleSystemsMod.Settings.drivetrainMode != DrivetrainMode.Full) return null;
        Powertrain? train = CompPhobosVehicleSystems.Of(vehicle)?.Powertrain;
        return train is { ChecksLiftAtLaunch: true } ? train : null;
    }

    /// <summary>The hover margin on landing at this tile (1 is just enough), at the weight now less the fuel still to burn on the way;
    /// null when the check does not apply.</summary>
    public static double? MarginAt(VehiclePawn vehicle, PlanetTile tile, double fuelStillToBurn = 0)
    {
        Powertrain? train = Judge(vehicle);
        if (train == null || !tile.Valid || Find.WorldGrid == null) return null;
        double mass = Math.Max(1, train.MassKg() - Math.Max(0, fuelStillToBurn) * train.FuelKgPerUnit);
        float ambient = GenTemperature.GetTemperatureAtTile(tile);
        double pressure = VehicleSystemsMod.Settings.thinAir && train.BreathesAir ? AirRule.PressureRatio(TileAir.ElevationMetres(tile)) : 1;
        (bool dry, bool wet, double margin) = train.HoverCheck(mass, ambient, pressure);
        return dry || wet ? Math.Max(1, margin) : margin;
    }

    /// <summary>What landing at this tile would come to under the player's setting.</summary>
    public static Outcome At(VehiclePawn vehicle, PlanetTile tile, double fuelStillToBurn = 0) =>
        MarginAt(vehicle, tile, fuelStillToBurn) is { } margin && margin < 1 ? Beyond() : Outcome.Fine;

    /// <summary>What the player's setting makes of a landing the engines cannot hold.</summary>
    static Outcome Beyond()
    {
        VehicleSystemsSettings settings = VehicleSystemsMod.Settings;
        if (settings.showOnly || settings.hoverLimit == VehicleSystemsSettings.HoverLimit.WarnOnly) return Outcome.Warned;
        return settings.hoverLimit == VehicleSystemsSettings.HoverLimit.AllowWithDamage ? Outcome.Strained : Outcome.RunOn;
    }

    /// <summary>Whether this arrival puts the aircraft on the ground (a map or a caravan); not a strafing run, nor a crash already
    /// under way.</summary>
    public static bool Lands(IArrivalAction? action) =>
        action is ArrivalAction_LoadMap or ArrivalAction_LandToCaravan || action is ArrivalAction_LandInMap and not ArrivalAction_CrashInMap;

    /// <summary>The journey's last tile is reached and the aircraft is about to come down (the flight path's patch): judges the landing
    /// in that tile's air and applies the outcome. The player's aircraft only; nothing else flies these journeys.</summary>
    public static void OnArrival(AerialVehicleInFlight flight, PlanetTile tile)
    {
        VehiclePawn? vehicle = flight.Vehicle;
        if (vehicle == null || vehicle.Faction != Faction.OfPlayer) return;
        Apply(vehicle, At(vehicle, tile), flight);
    }

    /// <summary>Applies a landing's outcome: wear and a message, or nothing when it could hover.</summary>
    public static void Apply(VehiclePawn vehicle, Outcome outcome, GlobalTargetInfo where)
    {
        if (outcome == Outcome.Fine) return;
        CompPhobosVehicleSystems? comp = CompPhobosVehicleSystems.Of(vehicle);
        string key;
        switch (outcome)
        {
            case Outcome.RunOn:
                comp?.WearLanding(RunOnWearShare);
                key = "PhobosVehicleSystems_MsgLandRunOn";
                break;
            case Outcome.Strained:
                if (comp != null) HoverLimits.Strain(comp);
                key = "PhobosVehicleSystems_MsgLandStrained";
                break;
            default:
                key = "PhobosVehicleSystems_MsgLandBeyond";
                break;
        }
        Messages.Message(key.Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve(), where.IsValid ? new LookTargets(where) : LookTargets.Invalid,
            MessageTypeDefOf.CautionInput);
        Seen.Note("aircraft.landing-check", vehicle, variant: OutcomeName(outcome));
    }

    /// <summary>The outcome as the test list names its cases.</summary>
    public static string OutcomeName(Outcome outcome) => outcome switch
    {
        Outcome.RunOn => "run-on",
        Outcome.Strained => "strained",
        Outcome.Warned => "warned",
        _ => "fine",
    };

    /// <summary>Why an aircraft will not set down on this tile for fuel, or null: under the refuse setting a refuelling stop is never made
    /// where it could not hover, since it can fly on to better air. Under the other settings it sets down and the outcome applies.</summary>
    public static string? StopRefusal(VehiclePawn vehicle, PlanetTile tile, out Outcome outcome)
    {
        outcome = At(vehicle, tile);
        return outcome == Outcome.RunOn ? "PhobosVehicleSystems_FlightRefuelNoLandingThinAir".Translate().Resolve() : null;
    }

    /// <summary>The line the destination picker adds for a place the aircraft could not hover onto, or null.</summary>
    public static string? DestinationWarning(VehiclePawn vehicle, PlanetTile tile, double fuelCost)
    {
        if (MarginAt(vehicle, tile, fuelCost) is not { } margin || margin >= 1) return null;
        string key = Beyond() switch
        {
            Outcome.RunOn => "PhobosVehicleSystems_LandWarnRunOn",
            Outcome.Strained => "PhobosVehicleSystems_LandWarnStrained",
            _ => "PhobosVehicleSystems_LandWarnBeyond",
        };
        return key.Translate(vehicle.LabelShort.Named("VEHICLE"), ((float)margin).ToStringPercent().Named("SHARE")).Resolve();
    }
}

/// <summary>
/// Judges the landing as a journey reaches its last tile (Framework 0.140.0): Vehicle Framework's FlightPath.ConsumeNode takes the last
/// node and calls the arrival action, which lands the aircraft on a map or as a caravan. A prefix that only reads and, where the engines
/// cannot hold a hover, wears the aircraft and says so; the landing itself is Vehicle Framework's, unchanged. Circling and recon are left
/// alone.
/// </summary>
[HarmonyPatch(typeof(FlightPath), nameof(FlightPath.ConsumeNode))]
public static class Patch_FlightPath_ConsumeNode_LandingCheck
{
    public static void Prefix(FlightPath __instance, AerialVehicleInFlight ___aerialVehicle)
    {
        try
        {
            if (__instance.Count != 1 || __instance.Circling || ___aerialVehicle == null || ___aerialVehicle.recon) return;
            if (!LandingCheck.Lands(__instance.ArrivalAction)) return;
            LandingCheck.OnArrival(___aerialVehicle, __instance.First.Tile);
        }
        catch (Exception e)
        {
            // Never stand between an aircraft and its landing.
            VehicleSystemsMod.Logger.ErrorOnce("Landing check failed; the aircraft lands as Vehicle Framework has it: " + e);
        }
    }
}

/// <summary>Says in the destination picker when the aircraft could not hover onto the place under the mouse at its weight on arrival
/// (Framework 0.140.0). Runs after the cargo-reach postfix, so it adds to whatever that left.</summary>
[HarmonyPatch(typeof(FuelTargetUpdater), nameof(FuelTargetUpdater.ValidateTargets))]
[HarmonyPriority(Priority.Low)]
public static class Patch_FuelTargetUpdater_ValidateTargets_LandingCheck
{
    public static void Postfix(FuelTargetUpdater __instance, VehiclePawn ___vehicle, ref TargetValidation __result)
    {
        if (!__result.isValid || ___vehicle == null || ___vehicle.Faction != Faction.OfPlayer) return;
        try
        {
            if (FlightCargoReach.MouseTile is not { Valid: true } tile) return;
            string? warning = LandingCheck.DestinationWarning(___vehicle, tile, FlightCargoReach.FuelCost(__instance));
            if (warning == null) return;
            string? before = __result.Tooltip;
            __result = __result with { Tooltip = before.NullOrEmpty() ? warning : before + "\n" + warning };
        }
        catch (Exception e)
        {
            VehicleSystemsMod.Logger.ErrorOnce("Destination landing warning failed: " + e);
        }
    }
}
