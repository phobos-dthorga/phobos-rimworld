using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// Descents on the world map and what holds a vehicle back on them (Framework 0.250.x; docs/development/world-elevation-and-descents.md; the
/// owner's choices of 23 Nov 2026: hills go up and down with the net slope between tiles on top, brakes that heat and fade, and four
/// retarders: the engine's friction and loads with the fuel cut off, an exhaust brake, a compression brake on heavy diesels, and
/// regeneration). Game-free; the drivetrain model works the overrun itself (DrivetrainModel.Overrun) from what this gives it.
/// </summary>
public static class DescentRule
{
    /// <summary>The distance between two tiles' middles, metres: a tile is about 30 km across (the owner's decision of 18 Nov 2026).</summary>
    public const double TileMetres = 30000;

    /// <summary>A diesel this powerful or more has a compression brake built in unless its drivetrain says otherwise (authored: engines of
    /// 10 litres and more need one, FleetOwner, 2002; about 250 kW is the smallest such engine).</summary>
    public const double CompressionBrakeFromKw = 250;

    /// <summary>The steepest net slope taken from two tiles' heights (authored: no road between tiles is steeper over 30 km).</summary>
    public const double MaxNetSlope = 0.08;

    /// <summary>
    /// The grade a caravan meets now on a leg (a share: 0.05 is 5 %, negative downhill): a hilly tile climbs for the first half of the leg
    /// and descends for the second at its grade (authored: half and half, so the country's hills cost on the way up and give back on the way
    /// down), with the net slope between the two tiles on top throughout.
    /// </summary>
    /// <param name="hillGrade">The tile's grade by its hilliness and its way (WorldGroundRules.Grade), never negative.</param>
    /// <param name="legProgress">How far across the leg the caravan is, 0 to 1.</param>
    /// <param name="netSlope">The height gained over the leg's length (negative when it falls).</param>
    public static double LegGrade(double hillGrade, double legProgress, double netSlope)
    {
        double hill = Math.Max(0, hillGrade);
        return (Clamp01(legProgress) < 0.5 ? hill : -hill) + netSlope;
    }

    /// <summary>The share of a leg behind a caravan: what it has paid of the leg's cost, 0 to 1 (0 with no cost known).</summary>
    public static double LegProgress(double costLeft, double costTotal) => costTotal > 0 ? Clamp01(1 - costLeft / costTotal) : 0;

    /// <summary>The net slope from one tile's height to the next's over the distance between their middles, held within
    /// <see cref="MaxNetSlope"/>. 0 with no distance.</summary>
    public static double NetSlope(double fromMetres, double toMetres, double tileMetres)
    {
        if (tileMetres <= 0) return 0;
        double slope = (toMetres - fromMetres) / tileMetres;
        return Math.Max(-MaxNetSlope, Math.Min(MaxNetSlope, slope));
    }

    // ---- What holds a vehicle back ----

    /// <summary>An exhaust brake's power at the engine's rated speed, as a share of its rated power (the Wyoming report: "about half the
    /// retarding power of engine brakes").</summary>
    public const double ExhaustBrakeShare = 0.5;

    /// <summary>
    /// The power a retarder absorbs at the engine's rated speed, kW: a compression brake's share of rated power (about all of it, from the
    /// Wyoming report's measured 502 hp on a 450 to 500 hp engine), or an exhaust brake's half, whichever is fitted and larger.
    /// </summary>
    public static double RetarderKw(double ratedKw, double compressionBrakeShare, bool exhaustBrake) =>
        Math.Max(0, ratedKw) * Math.Max(Math.Max(0, compressionBrakeShare), exhaustBrake ? ExhaustBrakeShare : 0);

    /// <summary>A retarder's power at an engine speed: in proportion to speed, up to a tenth over rated, where it is strongest (Heavy Duty
    /// Trucking, 2023, citing Jacobs: "about half" at 1,200 rpm, best "about 10% above the engine's rated rpm").</summary>
    public static double RetarderKwAt(double retarderKwAtRated, double rpm, double ratedRpm) =>
        ratedRpm <= 0 ? 0 : Math.Max(0, retarderKwAtRated) * Math.Min(1.1, Math.Max(0, rpm) / ratedRpm);

    /// <summary>What an electric drive can put back on a descent, as a share of its rated power (authored, approximate).</summary>
    public const double RegenShareOfRated = 0.7;

    /// <summary>The share of the power regenerated that reaches the pack, after motor, inverter and charging losses (authored).</summary>
    public const double RegenEfficiency = 0.7;

    /// <summary>A diesel-electric's dynamic brake, as a share of its rated power, turned to heat in its resistors (authored).</summary>
    public const double DynamicBrakeShare = 1.0;

    // ---- Brakes that heat and fade ----

    /// <summary>
    /// The brakes' heat capacity per tonne of the vehicle, kJ per degree C (authored from the iron in a heavy truck's brakes: about 500 kg on a
    /// 36 t truck at 0.46 kJ per kg per degree, so about 6.4 per tonne).
    /// </summary>
    public const double HeatCapacityPerTonne = 6.4;

    /// <summary>The brake temperature fade begins at, degrees C: the Grade Severity Rating System's 500 degrees F, "based on the fade
    /// temperatures of brake linings" (the Wyoming report).</summary>
    public const double FadeStarts = 260;

    /// <summary>The temperature by which the brakes have faded to their worst (authored).</summary>
    public const double FadeWorst = 450;

    /// <summary>The most a fade takes from the brakes' hold (authored: they never fail outright).</summary>
    public const double FadeMostLoss = 0.7;

    /// <summary>
    /// How fast the service brakes warm, degrees C an hour, for the power going into them: the power over their heat capacity, which grows
    /// with the vehicle's mass (<see cref="HeatCapacityPerTonne"/>).
    /// </summary>
    public static double HeatPerHour(double brakeKw, double massKg)
    {
        if (brakeKw <= 0 || massKg <= 0) return 0;
        double capacityKjPerDegree = HeatCapacityPerTonne * massKg / 1000.0;
        return brakeKw * 3600.0 / capacityKjPerDegree;
    }

    /// <summary>How much of their hold the brakes have lost to heat, 0 to <see cref="FadeMostLoss"/>: none up to <see cref="FadeStarts"/>,
    /// rising evenly to the most at <see cref="FadeWorst"/>.</summary>
    public static double FadeLoss(double temperatureC)
    {
        if (temperatureC <= FadeStarts) return 0;
        double share = Math.Min(1, (temperatureC - FadeStarts) / (FadeWorst - FadeStarts));
        return FadeMostLoss * share;
    }

    static double Clamp01(double x) => x < 0 ? 0 : x > 1 ? 1 : x;
}
