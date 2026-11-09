using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using PipeSystem;
using RimWorld;
using Verse;

namespace Phobos.Factory;

/// <summary>
/// Checks each bill once, on its first tick after a load or after it is added, and rebuilds it if its recipe has changed and it has
/// not started (its counted ingredients, if any, go back beside the machine). A bill already running keeps its load; it is rebuilt
/// when it resets for the next batch (<see cref="BillRepairResetPatch"/>). A prefix that never skips the original.
/// </summary>
[HarmonyPatch(typeof(Process), nameof(Process.Tick))]
public static class BillRepairTickPatch
{
    static readonly ConditionalWeakTable<Process, object> Checked = new();
    static readonly object Marker = new();

    public static void Prefix(Process __instance)
    {
        if (Checked.TryGetValue(__instance, out _)) return;
        Checked.Add(__instance, Marker);
        if (!BillRepair.IsStale(__instance) || !__instance.MissingIngredients) return;
        BillRepair.Rebuild(__instance);
        BillRepairReport.Rebuilt(__instance);
    }
}

/// <summary>Before a machine sets up its bills, any whose recipe no longer exists comes off it: nothing can read such a bill, including the window that would cancel it.</summary>
[HarmonyPatch(typeof(CompAdvancedResourceProcessor), nameof(CompAdvancedResourceProcessor.PostSpawnSetup))]
public static class GhostBillsPatch
{
    public static void Prefix(CompAdvancedResourceProcessor __instance) => BillRepair.ClearGhostBills(__instance);
}

/// <summary>A running bill with a changed recipe is rebuilt the moment it finishes and resets, when it holds nothing.</summary>
[HarmonyPatch(typeof(Process), nameof(Process.ResetProcess))]
public static class BillRepairResetPatch
{
    public static void Postfix(Process __instance)
    {
        if (!BillRepair.IsStale(__instance)) return;
        BillRepair.Rebuild(__instance);
        BillRepairReport.Rebuilt(__instance);
    }
}

/// <summary>
/// An item on an input cell that the bill's owners cannot take (the recipe asks for it, the saved bill does not, or a category
/// matches nothing) is skipped instead of throwing, so the items behind it on the cell are still read. Skipping the original is
/// the point: the original has nothing to do with no owner, and would throw. The bill itself is rebuilt by the patches above.
/// </summary>
[HarmonyPatch(typeof(AdvancedProcessorsManager), nameof(AdvancedProcessorsManager.AddIngredient))]
public static class AddIngredientGuardPatch
{
    public static bool Prefix(CompAdvancedResourceProcessor comp, Thing thing)
    {
        Process? process = comp?.Process;
        if (process == null || thing?.def == null) return false;
        if (process.GetOwnerFor(thing.def) != null) return true;
        List<ThingCategoryDef>? categories = thing.def.thingCategories;
        return categories != null && process.GetOwnerForCategory(categories) != null;
    }
}

/// <summary>
/// Pipe System's hopper scan marks the owner it was called for as "being filled" whenever it takes any item, and only the owner the
/// item went to is unmarked afterwards; with several ingredients on one cell the checked owner can stay marked for good, and a
/// marked owner is left out of what pawns are asked to bring. The scan is only run for bills that feed from hoppers, which pawns
/// never deliver to, so clearing the mark on an owner still short can take nothing from a pawn.
/// </summary>
[HarmonyPatch(typeof(Process), nameof(Process.CheckInputSlots))]
public static class BeingFilledClearPatch
{
    static readonly AccessTools.FieldRef<ThingAndResourceOwner, bool> BeingFilled = AccessTools.FieldRefAccess<ThingAndResourceOwner, bool>("beingFilled");

    public static void Postfix(ThingAndResourceOwner ingredientsOwner)
    {
        if (ingredientsOwner.Require && ingredientsOwner.BeingFilled) BeingFilled(ingredientsOwner) = false;
    }
}

/// <summary>Tells the player, in the game's words, that a bill was brought up to its recipe; the log keeps the detail.</summary>
public static class BillRepairReport
{
    /// <summary>Sets down what the removed bills held, and says so. Runs after the map has loaded, when things can be placed.</summary>
    public static void GhostsCleared(ThingWithComps machine, int count, List<(ThingDef def, int count)> held)
    {
        Log.Message("[D'thorga Factory Additions] Removed " + count + " bill(s) with no recipe from " + machine.LabelShort + ", holding " + held.Count + " kind(s) of ingredient.");
        if (!machine.Spawned) return;
        foreach ((ThingDef def, int amount) in held)
        {
            Thing item = ThingMaker.MakeThing(def, def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null);
            item.stackCount = amount;
            GenPlace.TryPlaceThing(item, machine.Position, machine.Map, ThingPlaceMode.Near);
        }
        Messages.Message("PhobosFactory_GhostBillsCleared".Translate(count.Named("COUNT"), machine.LabelShort.Named("MACHINE")), machine, MessageTypeDefOf.CautionInput, historical: false);
    }

    public static void Rebuilt(Process process)
    {
        Thing? machine = process.advancedProcessor?.parent;
        string bill = BillRepair.LabelOf(process);
        Log.Message("[D'thorga Factory Additions] Rebuilt the bill " + bill + (machine != null ? " at " + machine.Position : "")
            + ": its saved ingredients no longer matched the recipe. Anything it had counted was set down beside the machine.");
        if (machine == null || !machine.Spawned) return;
        Messages.Message("PhobosFactory_BillRebuilt".Translate(bill.Named("BILL"), machine.LabelShort.Named("MACHINE")), machine, MessageTypeDefOf.NeutralEvent, historical: false);
    }
}
