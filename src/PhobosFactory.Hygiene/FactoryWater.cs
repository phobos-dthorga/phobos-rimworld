using Phobos.Factory.Rules;
using PipeSystem;
using RimWorld;
using Verse;

namespace Phobos.Factory.Hygiene;

/// <summary>The rules for factory bills that use piped water. The patches and the machine's comp call these.</summary>
public static class FactoryWater
{
    /// <summary>
    /// The food poisoning cause for dirty factory water. The game's list has no such cause, and every one it has
    /// would blame the wrong thing. A number well past the game's five; the game saves a cause by its value.
    /// <see cref="FoodPoisonCausePatch"/> gives it its name.
    /// </summary>
    public const FoodPoisonCause ContaminatedWater = (FoodPoisonCause)101;

    /// <summary>Pipe System holds a finished batch at this many ticks while it waits to place the output.</summary>
    private const int WaitingForOutputTicks = 1;

    /// <summary>Dubs Bad Hygiene's lite mode has no plumbing, so nothing could ever supply the water.</summary>
    public static bool Required => FactoryMod.Settings.waterRequired && !Phobos.Framework.Hygiene.PlumbingFeed.LiteMode;

    /// <summary>
    /// Whether this tick of a process may go ahead, drawing its water if it uses any. Water goes only to a batch
    /// actually being worked, the same conditions Process.Tick uses to count down: not while it gathers ingredients
    /// or waits to place its output, and not where the process can't run at all.
    /// </summary>
    public static bool MayTick(Process process, int ticks)
    {
        var use = process.Def.GetModExtension<WaterUseExtension>();
        if (use == null || !Required) return true;
        if (process.MissingIngredients || process.RuinedByTemp || process.tickLeft <= WaitingForOutputTicks) return true;
        if (!process.Def.worksInSpace && ProcessUtility.InSpace(process.advancedProcessor)) return true;
        var water = process.advancedProcessor.parent.GetComp<CompFactoryWater>();
        if (water == null) return true;
        float litres = WaterRule.LitresForTicks(use.litresPerBatch, ticks, process.ticksOrQualityTicks);
        if (water.TryDraw(litres, use)) return true;
        // Skipping the tick stops the countdown, the effects and the sound's upkeep, but the sound itself plays until
        // it is ended. Its next working tick starts it again. A sound that isn't playing ignores this.
        process.Notify_StopWorkingSound();
        return false;
    }

    /// <summary>
    /// Gives a finished item its chance of food poisoning from the batch's water, on the game's own comp, so the
    /// game blends it when stacks merge and rolls it when someone eats. Items that can't carry poisoning ignore it.
    /// </summary>
    public static void ApplyDirtyWater(Process process, Thing product)
    {
        if (!Required || !FactoryMod.Settings.dirtyWaterPoisons) return;
        var water = process.advancedProcessor.parent.GetComp<CompFactoryWater>();
        if (water == null) return;
        float chance = water.BatchPoisonChance;
        if (chance <= 0f) return;
        var poison = product.TryGetComp<CompFoodPoisonable>();
        if (poison != null && Rand.Chance(chance)) poison.SetPoisoned(ContaminatedWater);
    }

    /// <summary>A batch has ended or been cancelled, so its water no longer counts towards the next one.</summary>
    public static void BatchEnded(Process process) =>
        process.advancedProcessor.parent.GetComp<CompFactoryWater>()?.ClearBatch();
}
