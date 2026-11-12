namespace Phobos.Framework.Rules;

/// <summary>The tipper's rules (docs/development/vehicle-tipper-hauler.md): which cells the load may land on, and how many things come off in a stretch of time.</summary>
public static class TipRule
{
    /// <summary>Whether a cell, as an offset from the cell behind the tailgate, is in the half-disc the load lands on: within the radius, and not forward of the tail.</summary>
    public static bool InFan(int dx, int dz, int rearX, int rearZ, float radius) => dx * dx + dz * dz <= radius * radius && dx * rearX + dz * rearZ >= 0;

    /// <summary>Adds this stretch's allowance to what was left over and takes out the whole things it comes to. A slow tip of under one thing a step still gets there.</summary>
    public static int WholeThings(ref float allowance, float thingsPerSecond, float seconds)
    {
        allowance += thingsPerSecond * seconds;
        int whole = (int)allowance;
        allowance -= whole;
        return whole;
    }
}

/// <summary>The tipper's loading grab (Framework 0.101.0; docs/development/vehicle-upgrades-round-two.md): how much of a stack one lift takes.</summary>
public static class GrabRule
{
    /// <summary>
    /// How many of a stack one lift takes: the whole stack, or as many as the lift's rated load and the room left aboard allow, whichever is
    /// fewest. Nothing when a single one is too heavy for the lift or the room. A massless thing takes the whole stack.
    /// </summary>
    public static int CountForLift(int stackCount, double unitKilograms, double liftKilograms, double freeKilograms)
    {
        if (stackCount <= 0) return 0;
        if (unitKilograms <= 0) return stackCount;
        double limit = System.Math.Min(liftKilograms, freeKilograms);
        if (limit <= 0) return 0;
        return (int)System.Math.Min(stackCount, System.Math.Floor(limit / unitKilograms + 1e-9));
    }
}
