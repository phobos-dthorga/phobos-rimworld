using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// Recovery, game-free (Framework 0.103.0, the recovery family of docs/development/vehicle-upgrade-families.md, built as one piece with the
/// hitch's recovery tow). A winch pulls a stuck vehicle towards the vehicle it is fitted to; whether it can is a balance of forces, in
/// kilograms-force, the way recovery crews reckon it:
/// <list type="bullet">
/// <item>What it takes: the stuck vehicle's weight times a resistance. A vehicle that rolls resists by its ground's rolling resistance
/// (<see cref="DrivetrainModel.RollingResistanceForPathCost"/>); one whose wheels will not turn is dragged, and resists by its ground's grip,
/// which is the friction it slides against. Soft ground adds suction, up to <c>mireAdd</c> of the weight on the softest going, in step with
/// how soft the ground is (the same softness <see cref="TreadRules.SoftGroundFactor"/> uses). Authored: real recovery manuals use tables of
/// such factors; ours are the drivetrain's own ground curves plus one authored suction figure, a gameplay simplification.</item>
/// <item>What the winch can give: its rated pull, but never more than the winching vehicle can hold on its brakes, which is its own weight
/// times its ground's grip. No ground anchors and no snatch blocks (a simplification).</item>
/// </list>
/// A recovery tow is judged more simply: the towed vehicle may weigh at most a set multiple of the vehicle towing it (authored); the
/// drivetrain model then decides how slowly the pair moves.
/// </summary>
public static class RecoveryRule
{
    /// <summary>How soft the ground is, 0 on firm going to 1 on the softest, from its grip.</summary>
    public static double Softness(double groundGrip) =>
        Math.Max(0, Math.Min(1, (TreadRules.FirmGrip - groundGrip) / (TreadRules.FirmGrip - TreadRules.SoftestGrip)));

    /// <summary>The share of a stuck vehicle's weight a pull must overcome on this ground.</summary>
    /// <param name="rolling">The ground's rolling resistance.</param>
    /// <param name="groundGrip">The ground's grip, also its sliding friction.</param>
    /// <param name="rolls">Whether the stuck vehicle's wheels or tracks turn; false drags it.</param>
    /// <param name="mireAdd">The suction soft ground adds at its softest, as a share of the weight (authored).</param>
    public static double Resistance(double rolling, double groundGrip, bool rolls, double mireAdd) =>
        Math.Max(0, rolls ? rolling : groundGrip) + Math.Max(0, mireAdd) * Softness(groundGrip);

    /// <summary>The pull it takes to move a stuck vehicle of this weight, in kilograms-force.</summary>
    public static double Required(double stuckKg, double resistance) => Math.Max(0, stuckKg) * Math.Max(0, resistance);

    /// <summary>The pull the winch can give, in kilograms-force: its rating, or what the winching vehicle holds on its brakes, the lower.</summary>
    public static double Available(double ratedKgf, double anchorKg, double anchorGrip) =>
        Math.Max(0, Math.Min(ratedKgf, Math.Max(0, anchorKg) * Math.Max(0, anchorGrip)));

    public static bool CanPull(double requiredKgf, double availableKgf) => availableKgf > 0 && requiredKgf <= availableKgf;

    /// <summary>
    /// Ticks for the winch to pull the stuck vehicle one cell: the base pace for a light pull, slowing to twice it at the winch's limit
    /// (a winch's line speed falls as its load rises; the shape is authored).
    /// </summary>
    public static int TicksPerCell(int baseTicks, double requiredKgf, double availableKgf)
    {
        if (availableKgf <= 0) return int.MaxValue;
        double load = Math.Max(0, Math.Min(1, requiredKgf / availableKgf));
        return (int)Math.Ceiling(Math.Max(1, baseTicks) * (1 + load));
    }

    /// <summary>The one-cell step from a stuck vehicle's cell towards the winching vehicle's, in each axis -1, 0 or 1; (0, 0) when there.</summary>
    public static (int Dx, int Dz) Step(int fromX, int fromZ, int toX, int toZ)
    {
        int dx = Math.Sign(toX - fromX), dz = Math.Sign(toZ - fromZ);
        // Keep the line straight: move along the minor axis only while it lags the major one by at least half a step.
        int adx = Math.Abs(toX - fromX), adz = Math.Abs(toZ - fromZ);
        if (adx > 2 * adz) dz = 0;
        else if (adz > 2 * adx) dx = 0;
        return (dx, dz);
    }

    /// <summary>Whether a vehicle this heavy may be towed by one this heavy on the recovery tow.</summary>
    public static bool CanTow(double tractorKg, double towedKg, double maxRatio) =>
        tractorKg > 0 && towedKg >= 0 && towedKg <= tractorKg * Math.Max(0, maxRatio);
}
