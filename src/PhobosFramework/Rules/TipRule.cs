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
