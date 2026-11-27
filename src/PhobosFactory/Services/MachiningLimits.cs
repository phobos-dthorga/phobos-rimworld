using System.Collections.Generic;
using Phobos.Factory.Rules;
using PipeSystem;
using VanillaFurnitureExpandedFactory;
using Verse;

namespace Phobos.Factory;

/// <summary>
/// Lets VFE Factory's machining bay fill its output hopper with a whole stack of anything that stacks (Factory Additions 0.16.0; the owner's
/// report of 28 Nov 2026: hand-held radios backing the factory up). VFE makes one bill per Machining-table item from a template whose output
/// limit is 1 for all of them (its MachiningProcessTemplateDef), so the bay waited with two in the hopper whatever the item's stack limit.
/// <see cref="OutputRule.MachiningLimit"/> gives the new limit; guns and everything else that never stacks keep VFE's. Runs once at load, after
/// VFE has made its bills (<see cref="DefGeneratorPatch"/>); running again changes nothing.
/// </summary>
public static class MachiningLimits
{
    public static void Raise()
    {
        var templates = new List<string>();
        foreach (var template in DefDatabase<MachiningProcessTemplateDef>.AllDefsListForReading) templates.Add(template.defName);
        if (templates.Count == 0) return;
        int raised = 0;
        foreach (var bill in DefDatabase<ProcessDef>.AllDefsListForReading)
        {
            if (!FromTemplate(bill.defName, templates) || bill.results is not { Count: 1 } || bill.results[0].thing is not { } item) continue;
            // A bill whose item is made of a material never merges into a stack already there, so its limit is never read that way.
            if (!bill.onlyGrabAndOutputToFactoryHoppers || bill.maxOutputCount <= 0f || bill.useFirstIngredientAsOutputStuff) continue;
            float limit = OutputRule.MachiningLimit(bill.maxOutputCount, item.stackLimit, bill.results[0].count);
            if (limit <= bill.maxOutputCount) continue;
            // A hot reload gives VFE's bill its own limit again, with our mark still on it.
            if (!bill.HasModExtension<RaisedOutputLimitExtension>())
            {
                bill.modExtensions ??= new List<DefModExtension>();
                bill.modExtensions.Add(new RaisedOutputLimitExtension { vfeLimit = bill.maxOutputCount });
            }
            bill.maxOutputCount = limit;
            raised++;
        }
        if (raised > 0) Log.Message($"[D'thorga Factory Additions] The machining bay now fills a whole stack of {raised} items that stack.");
    }

    /// <summary>VFE names each bill its template's defName followed by the item's.</summary>
    private static bool FromTemplate(string defName, List<string> templates)
    {
        foreach (var template in templates)
            if (defName.StartsWith(template, System.StringComparison.Ordinal)) return true;
        return false;
    }
}
