using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// How much fuel a parked vehicle takes from a fuel line in one look (owner, 14 Nov 2026: "vehicles refuel from a chemfuel pipe"),
/// game-free. A pipe net counts in its own units; the net's Def says how many of them make one fuel item (Vanilla Expanded Framework's
/// <c>linkToRefuelables</c> ratio: Vanilla Chemfuel Expanded's chemfuel line is 1 unit to 1 chemfuel). The pump rate is authored.
/// </summary>
public static class PipeRefuelRule
{
    /// <summary>The fuel moved this look, in the vehicle's fuel: the least of what the tank still takes, what the pump moves in the
    /// hours since the last look, and what the net holds (its units over the ratio). Never negative.</summary>
    public static double FuelThisLook(double fuel, double capacity, double hours, double pumpPerHour, double netStored, double unitsPerFuel)
    {
        if (unitsPerFuel <= 0) return 0;
        double room = Math.Max(0, capacity - fuel);
        double pumped = Math.Max(0, hours) * Math.Max(0, pumpPerHour);
        double held = Math.Max(0, netStored) / unitsPerFuel;
        return Math.Min(room, Math.Min(pumped, held));
    }

    /// <summary>The net's units drawn for that much fuel.</summary>
    public static double UnitsFor(double fuel, double unitsPerFuel) => Math.Max(0, fuel) * Math.Max(0, unitsPerFuel);

    /// <summary>The hours a look covers: since the last look, never more than <paramref name="most"/> (a vehicle parked for days before the
    /// line was laid is not filled in one gulp), and nothing on the first look after parking.</summary>
    public static double Hours(int lastTick, int nowTick, int ticksPerHour, double most)
    {
        if (lastTick < 0 || nowTick <= lastTick || ticksPerHour <= 0) return 0;
        return Math.Min(Math.Max(0, most), (nowTick - lastTick) / (double)ticksPerHour);
    }
}
