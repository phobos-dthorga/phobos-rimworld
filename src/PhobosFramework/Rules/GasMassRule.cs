using System;

namespace Phobos.Framework.Rules;

/// <summary>
/// Real amounts of a gas into the framework's room gases, game-free (Framework 0.187.0; docs/development/vehicle-emergent-play.md, EM20).
/// The room engine counts gas in "cells": one cell's worth at full concentration, spread over a room of so many cells. A source that knows
/// its grams (an engine's carbon monoxide) is turned into cells through the gas's molar mass and the concentration in ppm that the
/// engine's "full" stands for: grams make milligrams per cubic metre in one cell's volume, which make ppm at 25 C and 1 atmosphere
/// (24.45 litres a mole), which make a share of full. A gameplay simplification: the room mixes at once, as the engine already assumes.
/// </summary>
public static class GasMassRule
{
    /// <summary>Litres a mole of any gas fills at 25 C and one atmosphere: the usual figure for turning mg/m3 into ppm.</summary>
    public const double MolarVolumeLitres = 24.45;

    /// <summary>The air in one map cell: a cell is a metre square (docs/development/vehicle-drivetrain-design.md) under a ceiling about
    /// 3 m high. Authored.</summary>
    public const double CubicMetresPerCell = 3;

    /// <summary>mg/m3 that one ppm of a gas of this molar mass is: M / 24.45.</summary>
    public static double MilligramsPerCubicMetrePerPpm(double molarMassGramsPerMole) => Math.Max(0, molarMassGramsPerMole) / MolarVolumeLitres;

    /// <summary>The room engine's cells of gas that this many grams make: 0 for any figure that is not positive.</summary>
    public static double CellsFromGrams(double grams, double molarMassGramsPerMole, double ppmAtFullConcentration, double cubicMetresPerCell = CubicMetresPerCell)
    {
        if (grams <= 0 || molarMassGramsPerMole <= 0 || ppmAtFullConcentration <= 0 || cubicMetresPerCell <= 0) return 0;
        double ppmInOneCell = grams * 1000 / cubicMetresPerCell / MilligramsPerCubicMetrePerPpm(molarMassGramsPerMole);
        return ppmInOneCell / ppmAtFullConcentration;
    }
}
