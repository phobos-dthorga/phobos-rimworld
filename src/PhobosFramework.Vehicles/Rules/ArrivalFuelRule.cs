using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// How full another faction's ground vehicle arrives, game-free (Framework 0.192.0; docs/development/vehicle-emergent-play.md, EM5; the
/// owner's choice of 20 Nov 2026: fuel by distance, with a floor). A full tank less the fuel for the trip from that faction's nearest
/// settlement, by Vehicle Framework's own figures: a world tile takes round(60 / speed) x 340 ticks to cross
/// (<c>VehicleCaravanTicksPerMoveUtility.MoveSpeedToTileSpeed</c>), and the tank burns its rate x 1/60,000 a tick
/// (<c>CompFueledTravel.ConsumptionRatePerTickRaw</c>, times its world multiplier between tiles, which the caller folds into the rate). Never below <see cref="FloorShare"/> of the tank (authored), so a raid from far
/// away still arrives able to move; distance shows when its tank is holed, as a fuller tank leaks for longer. The trip's road, terrain
/// and rests are left out, a simplification: fuel burns only while a convoy moves, and the ticks per tile are Vehicle Framework's base.
/// </summary>
public static class ArrivalFuelRule
{
    /// <summary>The least share of its tank a vehicle arrives with (authored, the owner's "fuel floor").</summary>
    public const double FloorShare = 0.2;

    /// <summary>Vehicle Framework's conversion from a map speed to ticks per world tile.</summary>
    public const double TicksPerTileFactor = 340;

    /// <summary>Vehicle Framework's fuel per tick for a rate of 1.</summary>
    public const double FuelPerTickPerRate = 1.0 / 60000;

    /// <summary>Ticks to cross one world tile at this map speed (cells a second times the world speed factor); 0 for one that cannot move.</summary>
    public static double TicksPerTile(double moveSpeed) => moveSpeed <= 0 ? 0 : Math.Round(60 / moveSpeed) * TicksPerTileFactor;    // Unity rounds halves to even, as Math.Round does

    /// <summary>The fuel a trip of so many tiles burns.</summary>
    public static double TripFuel(double tiles, double rate, double moveSpeed) =>
        Math.Max(0, tiles) * Math.Max(0, rate) * FuelPerTickPerRate * TicksPerTile(moveSpeed);

    /// <summary>The fuel it arrives with: a full tank less the trip, never below the floor nor above the tank.</summary>
    public static double Arrival(double capacity, double tripFuel, double floorShare = FloorShare)
    {
        if (capacity <= 0) return 0;
        double floor = capacity * Math.Max(0, Math.Min(1, floorShare));
        return Math.Max(floor, Math.Min(capacity, capacity - Math.Max(0, tripFuel)));
    }
}
