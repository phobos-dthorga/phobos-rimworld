using System;

namespace Phobos.Framework.Rules;

/// <summary>
/// A hybrid's battery drive, game-free (Framework 0.104.0; owner, 12 Nov 2026; docs/development/vehicle-generator-set.md, "The hybrid"):
/// a vehicle with a battery buffer can creep on it with its engine off, quietly and slowly, until the buffer is spent, and its engine fills
/// the buffer again while it runs. Charge is in watt-days, the buffer's own unit (GeneratorRule). Every figure passed in is authored.
/// </summary>
public static class BatteryDriveRule
{
    /// <summary>How many cells the buffer's charge carries the vehicle, at <paramref name="wattDaysPerCell"/> a cell.</summary>
    public static float CellsLeft(float charge, float wattDaysPerCell) =>
        wattDaysPerCell <= 0f ? float.PositiveInfinity : Math.Max(0f, charge) / wattDaysPerCell;

    /// <summary>The charge left after moving <paramref name="cells"/> on the battery; never below zero.</summary>
    public static float Drain(float charge, float cells, float wattDaysPerCell) =>
        Math.Max(0f, Math.Max(0f, charge) - Math.Max(0f, cells) * Math.Max(0f, wattDaysPerCell));

    /// <summary>The charge after the engine has run for <paramref name="days"/>, putting <paramref name="watts"/> into it; never above the
    /// buffer's capacity.</summary>
    public static float Charge(float charge, float capacity, float watts, float days)
    {
        float top = Math.Max(0f, capacity);
        return Math.Min(top, Math.Max(0f, charge) + Math.Max(0f, watts) * Math.Max(0f, days));
    }

    /// <summary>Whether there is enough charge left to move at all: at least one cell's worth.</summary>
    public static bool CanCreep(float charge, float wattDaysPerCell) => charge > 0f && charge >= wattDaysPerCell;
}
