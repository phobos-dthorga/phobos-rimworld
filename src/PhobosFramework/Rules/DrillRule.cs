namespace Phobos.Framework.Rules;

/// <summary>The drill rig's rules (docs/development/vehicle-drill-rig.md): a rig faster than the game's deep drill meets insects at the game's odds per portion of ore, not per day.</summary>
public static class DrillRule
{
    /// <summary>The mean time between hives, in days, for a rig working this many times faster than a deep drill. The game's figure divided by the rate: the same risk per portion.</summary>
    public static float InsectMtbDays(float baseMtbDays, float rateFactor) => rateFactor <= 0f ? baseMtbDays : baseMtbDays / rateFactor;

    /// <summary>The rate on stone once the ore is gone: the drilling rate times the bit's bedrock factor.</summary>
    public static float StoneRate(float drillRate, float bedrockFactor) => drillRate * bedrockFactor;
}
