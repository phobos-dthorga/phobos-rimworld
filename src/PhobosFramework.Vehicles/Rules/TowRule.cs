using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>What a tow comes to: the whole speed, a share of it, a crawl, or not at all.</summary>
public enum TowOutcome
{
    FullSpeed,
    Slowed,
    Crawls,
    TooHeavy,
}

/// <summary>The answer to "can this pull that?": the outcome, the share of its speed the vehicle keeps, what holds it back, and the whole
/// train's mass.</summary>
public readonly struct TowVerdict
{
    public readonly TowOutcome Outcome;
    public readonly double SpeedShare;
    public readonly DriveLimit Limit;
    public readonly double TrainKg;

    public TowVerdict(TowOutcome outcome, double speedShare, DriveLimit limit, double trainKg)
    {
        Outcome = outcome;
        SpeedShare = speedShare;
        Limit = limit;
        TrainKg = trainKg;
    }
}

/// <summary>
/// "Can this pull that?" before a tow begins (Framework 0.104.0, slice 3 of docs/development/vehicle-hitch-design.md), game-free. The
/// answer is the tow the drivetrain model would give at the vehicle's design speed on the ground it stands on, on level ground and not
/// accelerating: the same <see cref="DrivetrainModel.Evaluate"/> the drive itself uses, so the forecast and the drive never disagree on
/// the same ground. A vehicle without the full model gets <see cref="HitchRule.PlainCap"/> instead; a recovery tow over its weight ratio
/// (<see cref="RecoveryRule.CanTow"/>) is refused. A forecast, labelled as one: hills, softer going and a heavier load on the way change it.
/// </summary>
public static class TowRule
{
    /// <summary>A share of the speed at or above this is the whole speed (the drive's own caps are smoothed to about this).</summary>
    public const double FullShare = 0.995;

    /// <summary>The verdict for a share of the speed: at the crawl floor it crawls, below the whole speed it is slowed.</summary>
    public static TowVerdict FromShare(double share, DriveLimit limit, double trainKg, double floor)
    {
        share = Math.Max(0, Math.Min(1, share));
        if (share >= FullShare) return new TowVerdict(TowOutcome.FullSpeed, 1, DriveLimit.None, trainKg);
        TowOutcome outcome = share <= floor + 1e-6 ? TowOutcome.Crawls : TowOutcome.Slowed;
        return new TowVerdict(outcome, share, limit == DriveLimit.None ? DriveLimit.Power : limit, trainKg);
    }

    /// <summary>A tow without the drivetrain model: the plain rule's share (a gameplay simplification, as the tow itself uses).</summary>
    public static TowVerdict Plain(double tractorKg, double towedKg, double floor) =>
        FromShare(HitchRule.PlainCap(tractorKg, towedKg, floor), DriveLimit.Power, Math.Max(0, tractorKg) + Math.Max(0, towedKg), floor);

    /// <summary>A recovery tow over the weight ratio: refused.</summary>
    public static TowVerdict TooHeavy(double tractorKg, double towedKg) =>
        new(TowOutcome.TooHeavy, 0, DriveLimit.None, Math.Max(0, tractorKg) + Math.Max(0, towedKg));

    /// <summary>
    /// The drivetrain model's forecast: the speed share the vehicle holds at its design speed with this much behind it, on ground of this
    /// rolling resistance and grip, level and steady.
    /// </summary>
    /// <param name="drivenWorking">The driven wheels' working share (a damaged axle grips less).</param>
    /// <param name="usableGears">Gears the gearbox can use now; 0 for all.</param>
    public static TowVerdict Modelled(DrivetrainSpec spec, double tractorKg, double towedKg, double rolling, double grip, double drivenWorking,
        int usableGears)
    {
        var conditions = new DriveConditions
        {
            EngineRunning = true,
            Speed = Math.Max(0.1, spec.DesignSpeed),
            MassKg = Math.Max(0, tractorKg),
            TowedMassKg = Math.Max(0, towedKg),
            RollingResistance = Math.Max(0, rolling),
            Grip = Math.Max(0, grip),
            UsableGears = usableGears,
            ClutchCapacity = 1,
            DrivenWheelsWorking = drivenWorking,
        };
        DriveResult result = DrivetrainModel.Evaluate(spec, conditions);
        return FromShare(result.Cap, result.Limit, conditions.MassKg + conditions.TowedMassKg, DrivetrainModel.CrawlFloor);
    }
}
