using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// A vehicle with no brakes cannot stop where it is told (Framework 0.188.0; docs/development/vehicle-emergent-play.md, EM29(b); the
/// owner's choice of 20 Nov 2026: it overruns and hits things). It rolls on past its stopping place by its speed and its weight, held
/// back by the engine and the ground alone, and what it strikes takes a blow that grows with both. Every figure is authored: a believable
/// ballpark, not a braking model.
/// </summary>
public static class OverrunRule
{
    /// <summary>Seconds a vehicle of the reference weight rolls on before the engine and the ground stop it.</summary>
    public const double SecondsRolling = 0.6;
    /// <summary>The weight the rolling time is given for; a heavier vehicle rolls further, by the square root of the ratio.</summary>
    public const double ReferenceKilograms = 5000;
    /// <summary>The furthest any vehicle rolls on, cells.</summary>
    public const int MaxCells = 8;
    /// <summary>Damage for each tonne at each cell a second.</summary>
    public const double DamagePerTonneSpeed = 4;
    public const double MinDamage = 5, MaxDamage = 400;

    /// <summary>How many cells it rolls past its stopping place: none when it was not moving, else at least one.</summary>
    public static int Cells(double cellsPerSecond, double kilograms)
    {
        if (cellsPerSecond <= 0) return 0;
        double weight = Math.Sqrt(Math.Max(0.25, kilograms / ReferenceKilograms));
        return (int)Math.Min(MaxCells, Math.Max(1, Math.Round(cellsPerSecond * SecondsRolling * weight)));
    }

    /// <summary>The blow it deals to what it strikes; the vehicle takes half itself.</summary>
    public static double ImpactDamage(double cellsPerSecond, double kilograms) =>
        Math.Min(MaxDamage, Math.Max(MinDamage, cellsPerSecond * Math.Max(0, kilograms) / 1000 * DamagePerTonneSpeed));

    /// <summary>Ticks between one cell and the next while it rolls on: its own pace, never slower than a crawl nor faster than a blur.</summary>
    public static int TicksPerCell(double cellsPerSecond) => (int)Math.Min(20, Math.Max(3, Math.Round(60 / Math.Max(0.1, cellsPerSecond))));
}
