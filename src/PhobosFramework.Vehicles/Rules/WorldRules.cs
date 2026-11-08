using System;
using System.Collections.Generic;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>How the load-dependent fuel setting corrects Vehicle Framework's own fuel use (docs/development/vehicle-world-map-design.md). Game-free.</summary>
public static class FuelRules
{
    /// <summary>
    /// Fuel to burn (positive) or give back (negative) per hour beyond what Vehicle Framework burns on its own: VF's hourly rate
    /// times the share it burns now (1 moving, 0.5 idling on a map, 0 standing on the world map) against the share the engine model
    /// says it should burn, normalised so a nominal cruise matches the vehicle author's rate.
    /// </summary>
    public static double ExtraPerHour(double vfPerHour, double vfShare, double modelShare) => vfPerHour * (modelShare - vfShare);

    /// <summary>Vehicle Framework's world-map fuel rate per hour from its per-tick figure.</summary>
    public static double WorldPerHour(double perTick, int ticksPerHour) => perTick * ticksPerHour;
}

/// <summary>How a caravan's vehicles' speed caps combine (the drivetrain's power and traction limits, on the world map). Game-free.</summary>
public static class CaravanRules
{
    /// <summary>The caravan moves as its slowest vehicle: the smallest cap, 1 when no vehicle is capped, never below the crawl floor.</summary>
    public static double CombineCaps(IEnumerable<double> caps)
    {
        double worst = 1;
        foreach (double cap in caps)
        {
            if (cap < worst) worst = cap;
        }
        return Math.Max(DrivetrainModel.CrawlFloor, worst);
    }

    /// <summary>Ticks per move after the cap: more ticks for a slower caravan (a cap of 0.5 doubles them).</summary>
    public static int TicksPerMove(int ticks, double cap) => cap >= 1 ? ticks : (int)Math.Ceiling(ticks / Math.Max(DrivetrainModel.CrawlFloor, cap));
}
