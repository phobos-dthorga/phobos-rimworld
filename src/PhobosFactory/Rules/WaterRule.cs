using System;

namespace Phobos.Factory.Rules;

/// <summary>
/// How much piped water a factory bill uses, and what that water does to a batch. A bill names its litres for
/// the whole batch; each step of work draws its share, so a paused batch has used only what it has worked for.
/// Overclocking shortens the batch (Pipe System scales the total ticks), so the water per batch stays the same.
/// No game types: this file is compiled into the unit tests too.
/// </summary>
public static class WaterRule
{
    /// <summary>The litres for one step of work on a batch.</summary>
    public static float LitresForTicks(float litresPerBatch, int ticks, int ticksPerBatch)
    {
        if (litresPerBatch <= 0f || ticks <= 0 || ticksPerBatch <= 0) return 0f;
        return litresPerBatch * Math.Min(ticks, ticksPerBatch) / ticksPerBatch;
    }

    /// <summary>
    /// The chance a finished batch carries food poisoning: the chance each litre brought, averaged over the
    /// litres used, so a batch that met contaminated water only near its end is less likely to be spoiled.
    /// </summary>
    public static float BatchPoisonChance(float riskLitres, float litres)
    {
        if (litres <= 0f || riskLitres <= 0f) return 0f;
        return Math.Min(riskLitres / litres, 1f);
    }
}
