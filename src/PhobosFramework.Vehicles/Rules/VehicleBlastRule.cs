using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>What a large blast does to a vehicle as a whole.</summary>
public enum VehicleBlastFate
{
    /// <summary>Too light a blast, or too heavy a vehicle, for anything to show.</summary>
    Untouched,
    /// <summary>Thrown about and dented: each part loses a share of its health, the outer ones most.</summary>
    Battered,
    /// <summary>Rolled, torn open and wrecked: the vehicle is destroyed and leaves its hull.</summary>
    Wrecked,
}

/// <summary>The figures a large blast on a vehicle is worked with, from the rules Def (docs/development/nuclear-strike-effects.md).</summary>
public readonly struct VehicleBlastFigures
{
    /// <summary>A vehicle of this many tonnes is wrecked by a blast of strength 1 (the fireball's edge); heavier ones stand longer.</summary>
    public readonly double referenceTonnes;
    /// <summary>Lighter vehicles count as this many tonnes, so a bicycle's weight does not make the figure run away.</summary>
    public readonly double lightestTonnes;
    /// <summary>Below this severity nothing shows.</summary>
    public readonly double least;
    /// <summary>The share of its health an outer part loses at severity 1, and an inner part.</summary>
    public readonly double outerShare;
    public readonly double innerShare;
    /// <summary>Each part's loss varies by up to this share either way, so a battered vehicle is not dented evenly.</summary>
    public readonly double spread;
    /// <summary>Soft parts (tyres, belts, hoses) start to burn at this heat (1 at the fireball's edge), and lose up to this share.</summary>
    public readonly double softFrom;
    public readonly double softShare;
    /// <summary>Blunt harm to each person aboard at severity 1, the most severity it counts, and the largest single blow.</summary>
    public readonly double crewHarm;
    public readonly double crewMostSeverity;
    public readonly double crewBlowMost;
    /// <summary>The share of the outside radiation dose that reaches people aboard: an open vehicle, then closed ones by weight.</summary>
    public readonly double hullOpen;
    public readonly double hullLight;
    public readonly double hullMedium;
    public readonly double hullHeavy;
    public readonly double mediumTonnes;
    public readonly double heavyTonnes;

    public VehicleBlastFigures(double referenceTonnes, double lightestTonnes, double least, double outerShare, double innerShare, double spread,
        double softFrom, double softShare, double crewHarm, double crewMostSeverity, double crewBlowMost, double hullOpen, double hullLight,
        double hullMedium, double hullHeavy, double mediumTonnes, double heavyTonnes)
    {
        this.referenceTonnes = referenceTonnes;
        this.lightestTonnes = lightestTonnes;
        this.least = least;
        this.outerShare = outerShare;
        this.innerShare = innerShare;
        this.spread = spread;
        this.softFrom = softFrom;
        this.softShare = softShare;
        this.crewHarm = crewHarm;
        this.crewMostSeverity = crewMostSeverity;
        this.crewBlowMost = crewBlowMost;
        this.hullOpen = hullOpen;
        this.hullLight = hullLight;
        this.hullMedium = hullMedium;
        this.hullHeavy = hullHeavy;
        this.mediumTonnes = mediumTonnes;
        this.heavyTonnes = heavyTonnes;
    }

    /// <summary>The design's figures (docs/development/nuclear-strike-effects.md), for tests and as the Def's defaults. All authored.</summary>
    public static VehicleBlastFigures Design => new(20, 0.5, 0.02, 0.8, 0.2, 0.5, 0.3, 1.0, 25, 2, 10, 1.0, 0.9, 0.7, 0.35, 15, 40);
}

/// <summary>
/// A large blast on a vehicle (D'thorga Framework 0.218.0; owner, 22 Nov 2026, the nuclear strike record's NK2, NK3, NK8 and NK9),
/// game-free. The real thing, in the record's sources: vehicles are damaged mainly by the blast wind throwing and rolling them, not by
/// the squeeze, so weight and distance decide their fate and armour thickness barely enters (Glasstone and Dolan, 1977, 5.86; the
/// Operation Teapot tank tests, WT-1123). Heat takes rubber and canvas at two to four times the exposure that burns bare skin. Armour
/// stops little of the dose at detonation: a medium tank passes about a third (Davidson, 1979). Every figure here is authored, a gameplay
/// choice shaped by those ratios, never a measurement.
/// </summary>
public static class VehicleBlastRule
{
    /// <summary>
    /// How hard the blast hits this vehicle: the blast's strength (1 at the fireball's edge) times the square root of the reference weight
    /// over the vehicle's, so a vehicle four times heavier stands a blast twice as strong.
    /// </summary>
    public static double Severity(double wind, double tonnes, VehicleBlastFigures f)
    {
        if (wind <= 0 || f.referenceTonnes <= 0) return 0;
        double weight = Math.Max(tonnes, Math.Max(f.lightestTonnes, 0.001));
        return wind * Math.Sqrt(f.referenceTonnes / weight);
    }

    public static VehicleBlastFate Fate(double severity, VehicleBlastFigures f) =>
        severity >= 1 ? VehicleBlastFate.Wrecked : severity < f.least ? VehicleBlastFate.Untouched : VehicleBlastFate.Battered;

    /// <summary>The share of its health one part loses; <paramref name="roll"/> is a random number from 0 to 1 for the spread.</summary>
    public static double PartShare(double severity, bool outer, double roll, VehicleBlastFigures f)
    {
        if (severity <= 0) return 0;
        double share = Math.Min(severity, 1) * (outer ? f.outerShare : f.innerShare) * (1 + f.spread * (2 * Clamp01(roll) - 1));
        return Clamp01(share);
    }

    /// <summary>The share of its health a soft part loses to the heat (1 at the fireball's edge); nothing below where rubber starts to burn.</summary>
    public static double SoftShare(double heat, VehicleBlastFigures f)
    {
        if (heat <= f.softFrom || f.softFrom >= 1) return 0;
        return Clamp01((heat - f.softFrom) / (1 - f.softFrom)) * Clamp01(f.softShare);
    }

    /// <summary>The blunt harm done to each person aboard as the vehicle is thrown about, wrecked or not.</summary>
    public static double CrewHarm(double severity, VehicleBlastFigures f) =>
        severity < f.least ? 0 : Math.Min(severity, f.crewMostSeverity) * f.crewHarm;

    /// <summary>How many blows that harm is split into, none larger than the most for one blow.</summary>
    public static int Blows(double harm, VehicleBlastFigures f) =>
        harm <= 0 ? 0 : f.crewBlowMost <= 0 ? 1 : (int)Math.Ceiling(harm / f.crewBlowMost);

    /// <summary>The share of the outside radiation dose that reaches people aboard.</summary>
    public static double HullShare(bool closedCab, double tonnes, VehicleBlastFigures f) =>
        !closedCab ? f.hullOpen : tonnes >= f.heavyTonnes ? f.hullHeavy : tonnes >= f.mediumTonnes ? f.hullMedium : f.hullLight;

    static double Clamp01(double value) => value < 0 ? 0 : value > 1 ? 1 : value;
}
