using System;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Where vehicles' water comes from: coolant top-ups and the Harrier's injection water. By default water is free, as the base game
/// treats it (owner, 8 Nov 2026). A tie-in may register a source instead: D'thorga Framework's Dubs Bad Hygiene part does, drawing
/// from plumbing beside the parked vehicle, so with Dubs Bad Hygiene active a vehicle needs plumbing to take on water.
/// </summary>
public static class VehicleWater
{
    /// <summary>A source: can it give the litres now (with the in-world reason when not), and take them.</summary>
    public interface ISource
    {
        bool CanDraw(VehiclePawn vehicle, float litres, out string? reason);
        bool TryDraw(VehiclePawn vehicle, float litres);
    }

    static ISource? source;

    /// <summary>The name of the registered source, for the load line.</summary>
    public static string SourceName { get; private set; } = "free (no water source registered)";

    /// <summary>Whether anything provides water; without a source, water is free or a service's fallback supplies stand in.</summary>
    public static bool HasSource => source != null;

    public static void Register(ISource waterSource, string name)
    {
        source = waterSource;
        SourceName = name;
    }

    public static bool CanDraw(VehiclePawn vehicle, float litres, out string? reason)
    {
        reason = null;
        if (source == null || litres <= 0f) return true;
        try
        {
            return source.CanDraw(vehicle, litres, out reason);
        }
        catch (Exception error)
        {
            Log.ErrorOnce(VehicleSystemsMod.LogPrefix + " The vehicle water source failed; water is free until restart. " + error, 0x5048_5741);
            source = null;
            return true;
        }
    }

    public static bool TryDraw(VehiclePawn vehicle, float litres)
    {
        if (source == null || litres <= 0f) return true;
        try
        {
            return source.TryDraw(vehicle, litres);
        }
        catch (Exception)
        {
            source = null;
            return true;
        }
    }
}
