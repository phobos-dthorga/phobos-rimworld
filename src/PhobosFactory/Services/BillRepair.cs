using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using PipeSystem;
using RimWorld;
using Verse;

namespace Phobos.Factory;

/// <summary>
/// A factory bill saved before its recipe changed. Pipe System builds a bill's ingredient owners once, when the bill is added
/// (Process.Setup), and saves them; it never compares them with the recipe again. After a recipe change the bill keeps asking for
/// the old ingredients, and the hopper scan matches cell items against the new recipe but puts them into the old owners: an item
/// the old bill never wanted finds no owner and throws, which ends the scan before the items behind it on the cell are read (read in
/// Pipe System 1.7, 7 Nov 2026; the owner's save of 7 Nov held bills from two recipe changes). This compares the two lists and
/// rebuilds the owners from the recipe, setting any counted ingredients down beside the machine, as deleting the bill would.
/// </summary>
/// <summary>A bill taken off a machine because its recipe does not exist now, kept so that it can come back.</summary>
public sealed class SetAsideBill : IExposable
{
    public string recipe = "";
    public BillRepeatModeDef? repeatMode;
    public int targetCount = 1;
    public bool forceQuality;
    public QualityCategory qualityToForce = QualityCategory.Normal;
    public QualityCategory qualityToOutput = QualityCategory.Normal;

    public static SetAsideBill From(string recipe, Process process) => new()
    {
        recipe = recipe,
        repeatMode = process.repeatMode,
        targetCount = process.targetCount,
        forceQuality = process.forceQualityOut,
        qualityToForce = process.qualityToForce,
        qualityToOutput = process.qualityToOutput,
    };

    public void AddTo(ProcessStack stack, ProcessDef def, ThingWithComps machine)
    {
        stack.AddProcess(def, machine, repeatMode ?? BillRepeatModeDefOf.RepeatCount, targetCount, qualityToOutput);
        Process added = stack.Processes[stack.Processes.Count - 1];
        added.forceQualityOut = forceQuality;
        added.qualityToForce = qualityToForce;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref recipe, "recipe", "");
        Scribe_Defs.Look(ref repeatMode, "repeatMode");
        Scribe_Values.Look(ref targetCount, "targetCount", 1);
        Scribe_Values.Look(ref forceQuality, "forceQuality");
        Scribe_Values.Look(ref qualityToForce, "qualityToForce", QualityCategory.Normal);
        Scribe_Values.Look(ref qualityToOutput, "qualityToOutput", QualityCategory.Normal);
    }
}

public static class BillRepair
{
    static readonly FieldInfo? TargetCount = AccessTools.Field(typeof(Process), "targetCount");
    static readonly FieldInfo? IngredientResources = AccessTools.Field(typeof(Process), "ingredientsCompResources");
    static readonly FieldInfo? CurrentProcess = AccessTools.Field(typeof(ProcessStack), "currentProcess");

    static readonly ConditionalWeakTable<Process, string> RecipeNames = new();
    static readonly ConditionalWeakTable<ProcessStack, List<SetAsideBill>> SetAside = new();

    /// <summary>On load, a bill whose recipe did not resolve has lost it by the time anything can read it, so its name is taken from the save while it is still there.</summary>
    public static void NoteMissingRecipe(Process process)
    {
        if (Scribe.mode != LoadSaveMode.LoadingVars || process.Def != null) return;
        string? name = Scribe.loader.curXmlParent?["process"]?.InnerText;
        if (!name.NullOrEmpty() && name != "null") RecipeNames.AddOrUpdate(process, name!);
    }

    /// <summary>Saves a machine's set-aside bills beside its bills. Nothing is written for a machine with none, and if this mod is removed the game ignores the entry.</summary>
    public static void ExposeSetAside(ProcessStack stack)
    {
        SetAside.TryGetValue(stack, out List<SetAsideBill>? bills);
        if (Scribe.mode == LoadSaveMode.Saving && (bills == null || bills.Count == 0)) return;
        Scribe_Collections.Look(ref bills, "PhobosSetAsideBills", LookMode.Deep);
        if (bills != null) SetAside.AddOrUpdate(stack, bills);
    }

    /// <summary>
    /// Sets aside any bill whose recipe does not exist now. Such a bill has no recipe at all, and everything that reads it throws: the machine's tick, its drawing every frame
    /// and the bills window, so the player cannot even cancel it. A recipe can be missing for one session only (a mod switched off, or one failing at startup), so the bill's
    /// settings are kept, saved with the machine, and it comes back when the recipe does. What it held is set down beside the machine once the map has loaded, and the player is told.
    /// </summary>
    public static void ClearGhostBills(CompAdvancedResourceProcessor comp)
    {
        ProcessStack stack = comp.ProcessStack;
        List<Process>? ghosts = stack?.Processes?.Where(process => process?.Def == null).ToList();
        if (ghosts == null || ghosts.Count == 0) return;
        if (CurrentProcess?.GetValue(stack) is Process { Def: null }) stack!.Notify_ProcessEnded();
        if (!SetAside.TryGetValue(stack!, out List<SetAsideBill>? kept))
        {
            kept = new List<SetAsideBill>();
            SetAside.Add(stack!, kept);
        }
        var held = new List<(ThingDef def, int count)>();
        foreach (Process ghost in ghosts)
        {
            stack!.Processes.Remove(ghost);
            if (ghost == null) continue;
            if (RecipeNames.TryGetValue(ghost, out string? recipe)) kept.Add(SetAsideBill.From(recipe, ghost));
            if (ghost.IngredientsOwners == null) continue;
            foreach (ThingAndResourceOwner owner in ghost.IngredientsOwners)
            {
                ThingDef? def = owner.ThingDef ?? owner.lastThingStored;
                if (def != null && owner.Count > 0) held.Add((def, owner.Count));
            }
        }
        ThingWithComps machine = comp.parent;
        int count = ghosts.Count;
        LongEventHandler.ExecuteWhenFinished(() => BillRepairReport.GhostsCleared(machine, count, held));
    }

    /// <summary>Puts back any set-aside bill whose recipe exists again, once the map has loaded.</summary>
    public static void RestoreSetAside(CompAdvancedResourceProcessor comp)
    {
        ProcessStack stack = comp.ProcessStack;
        if (stack == null || !SetAside.TryGetValue(stack, out List<SetAsideBill>? kept) || kept.Count == 0) return;
        if (!kept.Any(bill => DefDatabase<ProcessDef>.GetNamedSilentFail(bill.recipe) != null)) return;
        ThingWithComps machine = comp.parent;
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            if (!machine.Spawned) return;
            int restored = 0;
            for (int i = kept.Count - 1; i >= 0; i--)
            {
                ProcessDef? def = DefDatabase<ProcessDef>.GetNamedSilentFail(kept[i].recipe);
                if (def == null) continue;
                kept[i].AddTo(stack, def, machine);
                kept.RemoveAt(i);
                restored++;
            }
            if (restored > 0) BillRepairReport.Restored(machine, restored);
        });
    }

    /// <summary>Whether the bill's saved owners no longer match its recipe, by ingredient, category, pipe and count.</summary>
    public static bool IsStale(Process process)
    {
        var owners = process.IngredientsOwners;
        var ingredients = process.Def?.ingredients;
        if (owners == null || ingredients == null) return false;
        if (owners.Count != ingredients.Count) return true;
        for (int i = 0; i < owners.Count; i++)
        {
            ThingAndResourceOwner owner = owners[i];
            ProcessDef.Ingredient ingredient = ingredients[i];
            if (owner.ThingDef != ingredient.thing || owner.ThingCategoryDef != ingredient.thingCategory || owner.PipeNetDef != ingredient.pipeNet) return true;
            if (!ingredient.nutritionGetter && owner.Count + owner.Required != (int)ingredient.countNeeded) return true;
        }
        return false;
    }

    /// <summary>
    /// Rebuilds the owners from the recipe. Counted ingredients go back beside the machine (Pipe System's own reset), the player's
    /// target count is kept (Setup resets it to 1), and the bill then fills from its hoppers as a new one would.
    /// </summary>
    public static void Rebuild(Process process)
    {
        object? target = TargetCount?.GetValue(process);
        PadResources(process);
        process.ResetOwners(false);
        process.Setup();
        if (target != null) TargetCount!.SetValue(process, target);
    }

    /// <summary>Pipe System's reset reads this list by the owners' positions, and builds it from the recipe: a recipe that lost an ingredient
    /// has more saved owners than entries, and the reset would throw. An empty entry means no pipe, so the ingredient is set down by the machine.</summary>
    static void PadResources(Process process)
    {
        if (IngredientResources?.GetValue(process) is not List<CompResource> resources) return;
        int owners = process.IngredientsOwners?.Count ?? 0;
        while (resources.Count < owners) resources.Add(null!);
    }

    public static string LabelOf(Process process) => process.Def?.label ?? process.Def?.defName ?? "bill";
}
