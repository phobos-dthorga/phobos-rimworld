using HarmonyLib;
using PipeSystem;
using RimWorld;
using Verse;

namespace Phobos.Factory.Hygiene;

/// <summary>
/// Pauses a batch while its machine has no water. Pipe System has no way for a bill to wait on something outside
/// its own pipes: the one pause it offers, a process's suspended flag, is private, saved, and belongs to the player's
/// own suspend button. Skipping the tick is the reason for a prefix; the batch keeps its ingredients and progress and
/// carries on when water returns. Runs every 100 ticks per working machine, and leaves any bill without a water use alone.
/// </summary>
[HarmonyPatch(typeof(Process), nameof(Process.Tick), new[] { typeof(int) })]
public static class ProcessTickPatch
{
    public static bool Prefix(Process __instance, int ticks) => FactoryWater.MayTick(__instance, ticks);
}

/// <summary>
/// Adds the batch's chance of food poisoning to each finished item, in the private step where Pipe System gives a
/// new item its ingredients and quality. That runs before the item merges into a stack, so the game's own merge
/// blends the poisoning by count. The output guard also calls this on a throwaway item, which is harmless.
/// </summary>
[HarmonyPatch(typeof(Process), "ApplyIngredientsAndQuality", new[] { typeof(Thing) })]
public static class ApplyIngredientsPatch
{
    public static void Postfix(Process __instance, Thing outThing) => FactoryWater.ApplyDirtyWater(__instance, outThing);
}

/// <summary>Forgets a batch's water once it ends, so a contaminated batch doesn't count against the next one.</summary>
[HarmonyPatch(typeof(Process), nameof(Process.ResetProcess), new[] { typeof(bool) })]
public static class ResetProcessPatch
{
    public static void Postfix(Process __instance) => FactoryWater.BatchEnded(__instance);
}

/// <summary>
/// Names <see cref="FactoryWater.ContaminatedWater"/> in the game's "food poisoning" message. Without this the game
/// prints the bare number, which is what a player sees for food already poisoned if they remove this mod.
/// </summary>
[HarmonyPatch(typeof(FoodPoisonCauseExtension), nameof(FoodPoisonCauseExtension.ToStringHuman), new[] { typeof(FoodPoisonCause) })]
public static class FoodPoisonCausePatch
{
    public static void Postfix(FoodPoisonCause cause, ref string __result)
    {
        if (cause == FactoryWater.ContaminatedWater) __result = "PhobosFactory_FoodPoisonCause_ContaminatedWater".Translate();
    }
}

/// <summary>Applies this assembly's patches, and says on each water-using bill how much it needs.</summary>
[StaticConstructorOnStartup]
public static class HygieneSetup
{
    static HygieneSetup()
    {
        FactoryMod.ApplyPatches(new Harmony(FactoryMod.HarmonyId), typeof(HygieneSetup).Assembly);
        foreach (var def in DefDatabase<ProcessDef>.AllDefsListForReading)
        {
            var use = def.GetModExtension<WaterUseExtension>();
            if (use == null) continue;
            def.description += "\n\n" + "PhobosFactory_WaterBillNote".Translate(use.litresPerBatch.ToString("0.#").Named("LITRES"));
        }
    }
}
