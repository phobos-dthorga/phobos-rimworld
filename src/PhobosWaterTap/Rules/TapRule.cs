using System;

namespace Phobos.WaterTap.Rules;

/// <summary>What a tap is doing, or the one thing it is waiting for. Inspect text keys are built from these names.</summary>
public enum TapState
{
    SwitchedOff,
    Full,
    StockLimit,
    Blocked,
    NoWaterTower,
    Contaminated,
    Dry,
    DifferentWater,
    Filling,
}

/// <summary>
/// What a tap can see at this moment. <see cref="MapStock"/> counts the bottles lying on the map, this tap's included.
/// <see cref="WaterStored"/> is in Dubs Bad Hygiene's water units.
/// </summary>
public readonly record struct TapReading(
    int Held,
    int Target,
    int MapStock,
    int StockLimit,
    bool Blocked,
    bool HeldIsDifferentWater,
    bool HasWaterTower,
    bool Contaminated,
    float WaterStored,
    float WaterPerBottle);

/// <summary>The tap's rules, free of the game so they can be tested without it.</summary>
public static class TapRule
{
    /// <summary>The colony limit a map starts with: bottles on the map at which its taps stop filling. Authored.</summary>
    public const int DefaultStockLimit = 50;

    /// <summary>How far one press of the colony limit commands moves it.</summary>
    public const int StockLimitStep = 10;

    /// <summary>The highest colony limit, which keeps the commands' range meaningful. Authored.</summary>
    public const int MaxStockLimit = 1000;

    /// <summary>
    /// What the tap should do. The order is the order a player would fix things in: a tap with no need to fill has no
    /// problem to report, whether it's set to 0, full, or the map has enough, and water matters only once a bottle has
    /// somewhere to go.
    /// </summary>
    public static TapState Decide(TapReading reading)
    {
        if (reading.Target <= 0) return TapState.SwitchedOff;
        if (reading.Held >= reading.Target) return TapState.Full;
        if (reading.MapStock >= reading.StockLimit) return TapState.StockLimit;
        if (reading.Blocked) return TapState.Blocked;
        if (!reading.HasWaterTower) return TapState.NoWaterTower;
        if (reading.Contaminated) return TapState.Contaminated;
        if (reading.WaterStored < reading.WaterPerBottle) return TapState.Dry;
        if (reading.HeldIsDifferentWater) return TapState.DifferentWater;
        return TapState.Filling;
    }

    /// <summary>
    /// How many bottles one fill makes: never past the tap's target, never past the colony limit, and never more than the
    /// water stored can fill.
    /// </summary>
    public static int BottlesToMake(TapReading reading, int bottlesPerFill)
    {
        if (Decide(reading) != TapState.Filling) return 0;
        int affordable = (int)Math.Floor(reading.WaterStored / reading.WaterPerBottle);
        int room = Math.Min(reading.Target - reading.Held, reading.StockLimit - reading.MapStock);
        return Math.Min(Math.Min(room, bottlesPerFill), affordable);
    }

    /// <summary>A target the tap can hold: nothing below 0 (off), nothing above one stack on its one cell.</summary>
    public static int ClampTarget(int target, int stackLimit) => Math.Max(0, Math.Min(target, stackLimit));

    /// <summary>A colony limit within range. At 0 every tap on the map stops filling.</summary>
    public static int ClampStockLimit(int limit) => Math.Max(0, Math.Min(limit, MaxStockLimit));
}
