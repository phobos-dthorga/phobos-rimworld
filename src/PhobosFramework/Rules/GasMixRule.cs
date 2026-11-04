using System;

namespace Phobos.Framework.Rules;

/// <summary>
/// The maths of the framework's own room gases, copied from how the game moves room temperature: a door or vent moves two
/// rooms toward their shared mean, an open roof lets gas out, and a gas fades on its own. Concentrations are fractions of a
/// full room, 0 to 1; an "amount" is concentration times cells, so exchange conserves it. Every constant comes from the
/// caller (the rules Def), so this stays game-free and testable.
/// </summary>
public static class GasMixRule
{
    /// <summary>Ticks between passes of the engine. The game equalises temperature on its own cadences; we scale to this one.</summary>
    public const int PassTicks = 60;

    /// <summary>
    /// The transfer coefficient of an opening, in cells of gas per pass, from the game's door and vent figures: a rate applied
    /// every <paramref name="intervalTicks"/>, scaled to one pass. The game's open door is rate 1 every 34 ticks, a closed door
    /// rate 1 every 375 ticks, a vent rate 14 every 250 ticks (Core, BuildingProperties and Building_Vent, read 4 Nov 2026).
    /// </summary>
    public static float OpeningCoefficient(float rate, int intervalTicks)
    {
        if (rate <= 0f || intervalTicks <= 0) return 0f;
        return rate * PassTicks / intervalTicks;
    }

    /// <summary>
    /// Moves gas between two rooms through one opening. Returns the amount (in cells of gas) that moved from b to a; apply it as
    /// a gain of amount / cellsA to a and a loss of amount / cellsB to b. Never overshoots the shared mean, so repeated
    /// openings converge instead of oscillating.
    /// </summary>
    public static float Exchange(float concentrationA, int cellsA, float concentrationB, int cellsB, float coefficient)
    {
        if (cellsA <= 0 || cellsB <= 0 || coefficient <= 0f) return 0f;
        float difference = concentrationB - concentrationA;
        if (difference == 0f) return 0f;
        float wanted = coefficient * difference;
        // The amount that would put both rooms at the amount-weighted mean.
        float toMean = difference * cellsA * (float)cellsB / (cellsA + cellsB);
        return Math.Abs(wanted) < Math.Abs(toMean) ? wanted : toMean;
    }

    /// <summary>Gas lost through an opening to the outdoors, as a fraction of the room's concentration.</summary>
    public static float VentFraction(int cells, float coefficient)
    {
        if (cells <= 0 || coefficient <= 0f) return 0f;
        float fraction = coefficient / cells;
        return fraction > 1f ? 1f : fraction;
    }

    /// <summary>What remains after one pass of fading with the given half-life in ticks (0 or less means no fading).</summary>
    public static float DecayFactor(float halfLifeTicks)
    {
        if (halfLifeTicks <= 0f) return 1f;
        return (float)Math.Pow(0.5, PassTicks / halfLifeTicks);
    }

    /// <summary>
    /// What remains after one pass of venting through gaps in the roof: the open fraction of the room's cells fades at the
    /// open-roof half-life, the rest is held.
    /// </summary>
    public static float OpenRoofFactor(int openRoofCells, int cells, float openRoofHalfLifeTicks)
    {
        if (cells <= 0 || openRoofCells <= 0) return 1f;
        float openFraction = openRoofCells / (float)cells;
        if (openFraction > 1f) openFraction = 1f;
        return 1f - openFraction * (1f - DecayFactor(openRoofHalfLifeTicks));
    }

    /// <summary>
    /// The concentration a room gets when it is rebuilt from the cells it now covers: the amount those cells held, spread over
    /// the whole room. Cells that held nothing dilute it; a room split in two keeps each half's value.
    /// </summary>
    public static float Rebuilt(float amountInCoveredCells, int roomCells)
    {
        if (roomCells <= 0) return 0f;
        float concentration = amountInCoveredCells / roomCells;
        return concentration < 0f ? 0f : concentration;
    }

    public static float Clamp(float concentration, float max)
    {
        if (concentration < 0f) return 0f;
        return concentration > max ? max : concentration;
    }
}
