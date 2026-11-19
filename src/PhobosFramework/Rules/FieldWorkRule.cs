using System;

namespace Phobos.Framework.Rules;

/// <summary>
/// The figures of an implement working a field (docs/development/farming-batch-two.md): the pull it needs through the ground, the seed a
/// cell takes, and what grain gives as seed once cleaned. Game-free, so any mod's plough, cultivator or drill shares it. The figures that
/// go in come from the implement's Def; the sources and the agent's scale are in the design record.
/// </summary>
public static class FieldWorkRule
{
    /// <summary>
    /// An implement's draught in newtons: its draught per metre of working width (Iowa State University's tables, in kN a metre) times its
    /// width in cells times the metres a cell stands for (the agent's scale). Nothing for a lifted implement or a nonsense figure.
    /// </summary>
    public static double DraughtNewtons(bool lowered, double kilonewtonsPerMetre, int widthCells, double metresPerCell)
    {
        if (!lowered || kilonewtonsPerMetre <= 0 || widthCells <= 0 || metresPerCell <= 0) return 0;
        return kilonewtonsPerMetre * 1000.0 * widthCells * metresPerCell;
    }

    /// <summary>
    /// The seed a cell takes, in kilograms: a share of what the cell's crop would yield by weight (about 3 % for wheat in the record's
    /// sources). Nothing for a crop that yields nothing.
    /// </summary>
    public static double SeedKilogramsPerCell(int harvestUnits, double kilogramsPerUnit, double seedShare)
    {
        if (harvestUnits <= 0 || kilogramsPerUnit <= 0 || seedShare <= 0) return 0;
        return harvestUnits * kilogramsPerUnit * seedShare;
    }

    /// <summary>
    /// How many whole units of seed a drill draws for this cell, carrying the fraction left over to the next: the seed a cell takes is
    /// usually a few grams, far less than one unit, so units are drawn only as the owed weight passes one. Returns the units now and the
    /// weight still owed.
    /// </summary>
    public static (int Units, double Owed) DrawSeed(double owedKilograms, double cellKilograms, double kilogramsPerUnit)
    {
        double owed = Math.Max(0, owedKilograms) + Math.Max(0, cellKilograms);
        if (kilogramsPerUnit <= 0) return (0, 0);
        int units = (int)Math.Floor(owed / kilogramsPerUnit + 1e-9);
        return (units, Math.Max(0, owed - units * kilogramsPerUnit));
    }

    /// <summary>
    /// How many units of seed a load of grain gives once cleaned: the loss share (chaff, broken and light grain) taken off, rounded down.
    /// The agent's way of the owner's "kept back and cleaned" (docs/development/farming-batch-two.md, "Seed").
    /// </summary>
    public static int SeedFromGrain(int grainUnits, double cleaningLoss)
    {
        if (grainUnits <= 0) return 0;
        double kept = 1 - Math.Clamp(cleaningLoss, 0, 1);
        return (int)Math.Floor(grainUnits * kept + 1e-9);
    }
}
