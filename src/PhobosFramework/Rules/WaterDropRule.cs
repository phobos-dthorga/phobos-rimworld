using System;

namespace Phobos.Framework.Rules;

/// <summary>The stages of a helicopter's water drop run, in the order they are flown.</summary>
public enum WaterRunStage
{
    Climb,
    ToWater,
    Dip,
    ToFire,
    Pour,
    Home,
    Descend,
    Done,
}

/// <summary>How long each stage of a water drop run lasts, in ticks (WaterDropRule.Plan).</summary>
public readonly struct WaterRunPlan
{
    public readonly int Climb, ToWater, Dip, ToFire, Pour, Home, Descend;

    public WaterRunPlan(int climb, int toWater, int dip, int toFire, int pour, int home, int descend)
    {
        Climb = Math.Max(0, climb);
        ToWater = Math.Max(0, toWater);
        Dip = Math.Max(0, dip);
        ToFire = Math.Max(0, toFire);
        Pour = Math.Max(0, pour);
        Home = Math.Max(0, home);
        Descend = Math.Max(0, descend);
    }

    public int Total => Climb + ToWater + Dip + ToFire + Pour + Home + Descend;

    /// <summary>The tick the bucket empties over the fire: the end of the pour.</summary>
    public int PourEnds => Climb + ToWater + Dip + ToFire + Pour;
}

/// <summary>Where a water drop run is at a moment: the helicopter's place over the ground, its height as a share of full height (0 on the
/// ground, 1 at cruising height), and the stage.</summary>
public readonly struct WaterRunPoint
{
    public readonly double X, Z, Height;
    public readonly WaterRunStage Stage;

    public WaterRunPoint(double x, double z, double height, WaterRunStage stage)
    {
        X = x;
        Z = z;
        Height = height;
        Stage = stage;
    }
}

/// <summary>
/// A helicopter's water bucket, game-free (Framework 0.101.0, batch 3's signatures; docs/development/vehicle-upgrades-round-two.md). The
/// helicopter flies a circuit: it climbs where it stood, flies to open water, dips the bucket, flies to the fire, empties the bucket over
/// it, flies back and comes down. Every figure is authored and comes from the rules Def; the water a bucket holds is capped by what the
/// hook lifts today (a litre of water taken as a kilogram, the bucket's own weight left out: a simplification).
/// </summary>
public static class WaterDropRule
{
    /// <summary>The litres carried: the bucket's size, or what the hook lifts now if that is less.</summary>
    public static double Litres(double bucketLitres, double hookCapacityKilograms) =>
        Math.Max(0, Math.Min(bucketLitres, hookCapacityKilograms));

    /// <summary>The radius in cells the water puts out: the circle whose cells get <paramref name="litresPerCell"/> each.</summary>
    public static double Radius(double litres, double litresPerCell)
    {
        if (litres <= 0 || litresPerCell <= 0) return 0;
        return Math.Sqrt(litres / litresPerCell / Math.PI);
    }

    /// <summary>The ticks to fly a distance in cells at a speed in cells a tick, at least one for any distance.</summary>
    public static int FlightTicks(double cells, double cellsPerTick)
    {
        if (cells <= 0) return 0;
        if (cellsPerTick <= 0) return int.MaxValue / 8;
        return Math.Max(1, (int)Math.Ceiling(cells / cellsPerTick));
    }

    /// <summary>The run's stages for a circuit from the pad (0, 0) to the water, the fire and back, at a speed in cells a tick.</summary>
    public static WaterRunPlan Plan(double waterX, double waterZ, double fireX, double fireZ, double cellsPerTick, int climbTicks, int dipTicks, int pourTicks)
    {
        double toWater = Math.Sqrt(waterX * waterX + waterZ * waterZ);
        double toFire = Distance(waterX, waterZ, fireX, fireZ);
        double home = Math.Sqrt(fireX * fireX + fireZ * fireZ);
        return new WaterRunPlan(climbTicks, FlightTicks(toWater, cellsPerTick), dipTicks, FlightTicks(toFire, cellsPerTick), pourTicks,
            FlightTicks(home, cellsPerTick), climbTicks);
    }

    /// <summary>
    /// Where the helicopter is <paramref name="tick"/> ticks into a run, relative to the pad (0, 0). It climbs and comes down over the pad,
    /// holds still over the water and the fire, and flies straight between them; a leg eases in and out so it never jumps.
    /// </summary>
    public static WaterRunPoint At(WaterRunPlan plan, int tick, double waterX, double waterZ, double fireX, double fireZ)
    {
        int t = Math.Max(0, tick);
        if (t < plan.Climb) return new WaterRunPoint(0, 0, Ease((double)t / plan.Climb), WaterRunStage.Climb);
        t -= plan.Climb;
        if (t < plan.ToWater) return Leg(0, 0, waterX, waterZ, (double)t / plan.ToWater, WaterRunStage.ToWater);
        t -= plan.ToWater;
        if (t < plan.Dip) return new WaterRunPoint(waterX, waterZ, 1, WaterRunStage.Dip);
        t -= plan.Dip;
        if (t < plan.ToFire) return Leg(waterX, waterZ, fireX, fireZ, (double)t / plan.ToFire, WaterRunStage.ToFire);
        t -= plan.ToFire;
        if (t < plan.Pour) return new WaterRunPoint(fireX, fireZ, 1, WaterRunStage.Pour);
        t -= plan.Pour;
        if (t < plan.Home) return Leg(fireX, fireZ, 0, 0, (double)t / plan.Home, WaterRunStage.Home);
        t -= plan.Home;
        if (t < plan.Descend) return new WaterRunPoint(0, 0, 1 - Ease((double)t / plan.Descend), WaterRunStage.Descend);
        return new WaterRunPoint(0, 0, 0, WaterRunStage.Done);
    }

    /// <summary>Whether the bucket is full at this stage: from the end of the dip to the end of the pour.</summary>
    public static bool BucketFull(WaterRunStage stage) => stage is WaterRunStage.ToFire or WaterRunStage.Pour;

    static WaterRunPoint Leg(double fromX, double fromZ, double toX, double toZ, double share, WaterRunStage stage)
    {
        double s = Ease(share);
        return new WaterRunPoint(fromX + (toX - fromX) * s, fromZ + (toZ - fromZ) * s, 1, stage);
    }

    static double Ease(double share)
    {
        double s = Math.Min(1, Math.Max(0, share));
        return s * s * (3 - 2 * s);
    }

    static double Distance(double ax, double az, double bx, double bz)
    {
        double dx = bx - ax, dz = bz - az;
        return Math.Sqrt(dx * dx + dz * dz);
    }
}
