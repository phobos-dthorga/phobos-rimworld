using System.Collections.Generic;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// Driving through the night and resting on the road (Framework 0.214.0; docs/development/vehicle-accessories.md, B2 and B3): who is fit to
/// drive, who takes over, and who gets the bunk. Game-free and tested; the thresholds are the rules Def's, authored (the record's 0.3 and 0.6).
/// </summary>
public static class NightDriveRule
{
    /// <summary>Whether a driver with this much rest (0 to 1) may drive on. A person with no need of rest always may.</summary>
    public static bool Fit(double? rest, double fitRest) => rest == null || rest.Value > fitRest;

    /// <summary>
    /// The rider who takes over from a tired driver: one who can drive and is rested above <paramref name="reliefRest"/>, the most rested first
    /// (the first listed among equals). -1 when there is none.
    /// </summary>
    public static int Relief(IReadOnlyList<(double? rest, bool canDrive)> riders, double reliefRest)
    {
        int best = -1;
        double bestRest = double.MinValue;
        for (int index = 0; index < riders.Count; index++)
        {
            (double? rest, bool canDrive) = riders[index];
            if (!canDrive) continue;
            double level = rest ?? 1.0;
            if (level <= reliefRest || level <= bestRest) continue;
            best = index;
            bestRest = level;
        }
        return best;
    }

    /// <summary>
    /// Who sleeps in the bunk while the vehicle moves: the most tired rider who is not driving or crewing, not already in a bed, and below
    /// <paramref name="wantsBelow"/>. -1 when nobody wants it.
    /// </summary>
    public static int BunkFor(IReadOnlyList<(double? rest, bool crew, bool inBed)> riders, double wantsBelow)
    {
        int best = -1;
        double lowest = double.MaxValue;
        for (int index = 0; index < riders.Count; index++)
        {
            (double? rest, bool crew, bool inBed) = riders[index];
            if (crew || inBed || rest == null) continue;
            if (rest.Value >= wantsBelow || rest.Value >= lowest) continue;
            best = index;
            lowest = rest.Value;
        }
        return best;
    }

    /// <summary>
    /// Whether a caravan is expected to drive through the night, for its arrival time (Framework 0.228.0): every vehicle has its driving
    /// seats filled and at least one rider beside them who can take the wheel. A vehicle with no relief stops the caravan once its driver
    /// tires, so the estimate then keeps the nights at rest. An estimate only: a relief too tired to take over still stops it.
    /// </summary>
    public static bool ExpectsToDriveThrough(IReadOnlyList<(bool driversAboard, int reliefs)> vehicles)
    {
        if (vehicles.Count == 0) return false;
        for (int index = 0; index < vehicles.Count; index++)
        {
            (bool driversAboard, int reliefs) = vehicles[index];
            if (!driversAboard || reliefs < 1) return false;
        }
        return true;
    }

    /// <summary>Whether a bunk takes a passenger seat's place: only in a cab of more than two seats (the record's B2).</summary>
    public static bool BunkTakesSeat(int allSeats) => allSeats > 2;
}
