using System.Collections.Generic;
using System.Linq;
using Phobos.Factory.Rules;
using PipeSystem;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.Factory;

/// <summary>
/// Makes the bills each <see cref="BenchBillTemplateDef"/> asks for, at load, the way VFE Factory makes its
/// machining bay bills: ingredients from the item's cost, work from its recipe times the template's
/// multiplier, research from its recipe plus the template's. Runs once, from <see cref="DefGeneratorPatch"/>.
/// </summary>
public static class BenchBillGenerator
{
    /// <summary>VFE Factory's own stand-in for an item whose recipe gives no work.</summary>
    private const float FallbackWork = 3200f;
    /// <summary>The game names the recipe it makes from a recipeMaker "Make_" + the item's defName.</summary>
    private const string RecipePrefix = "Make_";

    public static void GenerateAll(bool hotReload)
    {
        // Items some bill already makes (VFE Factory's machining and tailoring bills, hand-written bills, an
        // earlier template's) are not made twice.
        var alreadyMade = new HashSet<ThingDef>();
        foreach (var process in DefDatabase<ProcessDef>.AllDefsListForReading)
        {
            if (process.results == null || process.HasModExtension<GeneratedBillExtension>()) continue;
            foreach (var result in process.results)
                if (result.thing != null) alreadyMade.Add(result.thing);
        }
        var report = new List<string>();
        foreach (var template in DefDatabase<BenchBillTemplateDef>.AllDefsListForReading)
            report.Add(Generate(template, alreadyMade, hotReload));
        if (report.Count > 0) Log.Message("[D'thorga Factory Additions] " + string.Join(" ", report));
    }

    private static string Generate(BenchBillTemplateDef template, HashSet<ThingDef> alreadyMade, bool hotReload)
    {
        var machine = template.machine;
        var processor = machine?.GetCompProperties<CompProperties_AdvancedResourceProcessor>();
        if (machine == null || processor == null) return $"{template.defName}: no machine, no bills.";
        processor.processes ??= new List<ProcessDef>();
        var piped = PipedThing(machine);
        var bills = new List<(ThingDef item, ProcessDef bill)>();
        var buildings = new List<string>();
        var mixedStuff = new List<string>();
        foreach (var item in DefDatabase<ThingDef>.AllDefsListForReading)
        {
            var users = item.recipeMaker?.recipeUsers;
            if (users == null || !users.Any(template.sourceBenches.Contains)) continue;
            if (template.excludedThings.Contains(item) || alreadyMade.Contains(item)) continue;
            if (item.category != ThingCategory.Item)
            {
                buildings.Add(item.label);
                continue;
            }
            var ingredients = Ingredients(item, out var stuff);
            if (ingredients == null)
            {
                mixedStuff.Add(item.label);
                continue;
            }
            bills.Add((item, Bill(template, item, ingredients, stuff, piped, hotReload)));
            alreadyMade.Add(item);
        }

        // Group the menu by kind (the item's first category), then by name; keep every label unique, because
        // the menu filter in ProcessesOptionsPatch finds a bill's option by its label.
        bills.Sort((a, b) =>
        {
            int byKind = string.Compare(a.item.FirstThingCategory?.label, b.item.FirstThingCategory?.label, System.StringComparison.Ordinal);
            return byKind != 0 ? byKind : string.Compare(a.item.label, b.item.label, System.StringComparison.Ordinal);
        });
        var labels = new HashSet<string>(processor.processes
            .Where(p => p != null && !p.HasModExtension<GeneratedBillExtension>()).Select(p => p.labelOverride));
        int priority = template.firstPriorityInBillList;
        foreach (var (item, bill) in bills)
        {
            if (!labels.Add(bill.labelOverride))
            {
                bill.labelOverride += $" ({item.defName})";
                bill.label = bill.labelOverride;
                labels.Add(bill.labelOverride);
            }
            bill.priorityInBillList = priority++;
            DefGenerator.AddImpliedDef(bill, hotReload);
            if (!processor.processes.Contains(bill)) processor.processes.Add(bill);
        }

        string benches = string.Join(", ", template.sourceBenches.Select(b => b.label));
        string text = $"{machine.label}: {bills.Count} bills from the {benches}.";
        if (buildings.Count > 0) text += $" Not automated, buildings: {string.Join(", ", buildings)}.";
        if (mixedStuff.Count > 0) text += $" Not automated, made of a material plus parts: {string.Join(", ", mixedStuff)}.";
        return text;
    }

    /// <summary>
    /// The bill's ingredients, or null when Pipe System cannot make the item right. It gives a finished item
    /// the material of the last ingredient it took, so an item made of a material plus parts could come out
    /// made of components; those stay at the bench. A material maps to one ingredient as in VFE Factory:
    /// the only material that fits, else steel for metal, any textile for fabric, any leather for leather.
    /// </summary>
    private static List<ProcessDef.Ingredient>? Ingredients(ThingDef item, out ThingDef? stuff)
    {
        stuff = null;
        var ingredients = new List<ProcessDef.Ingredient>();
        if (item.MadeFromStuff)
        {
            if (!item.costList.NullOrEmpty()) return null;
            var stuffIngredient = StuffIngredient(item);
            if (stuffIngredient == null) return null;
            stuff = stuffIngredient.thing;
            ingredients.Add(stuffIngredient);
        }
        if (item.costList != null)
        {
            foreach (var cost in item.costList)
                ingredients.Add(new ProcessDef.Ingredient { thing = cost.thingDef, countNeeded = cost.count });
        }
        return ingredients.Count > 0 ? ingredients : null;
    }

    private static ProcessDef.Ingredient? StuffIngredient(ThingDef item)
    {
        var fitting = DefDatabase<ThingDef>.AllDefsListForReading.Where(t => t.IsStuff && t.stuffProps.CanMake(item)).Take(2).ToList();
        var categories = item.stuffCategories;
        if (fitting.Count == 1) return new ProcessDef.Ingredient { thing = fitting[0], countNeeded = item.costStuffCount };
        if (categories.Contains(StuffCategoryDefOf.Metallic))
            return new ProcessDef.Ingredient { thing = ThingDefOf.Steel, countNeeded = item.costStuffCount };
        if (categories.Contains(StuffCategoryDefOf.Fabric))
            return new ProcessDef.Ingredient { thingCategory = ThingCategoryDefOf.Textiles, countNeeded = item.costStuffCount };
        if (categories.Contains(StuffCategoryDefOf.Leathery))
            return new ProcessDef.Ingredient { thingCategory = ThingCategoryDefOf.Leathers, countNeeded = item.costStuffCount };
        return null;
    }

    private static ProcessDef Bill(BenchBillTemplateDef template, ThingDef item, List<ProcessDef.Ingredient> ingredients,
        ThingDef? stuff, ThingDef? piped, bool hotReload)
    {
        string defName = $"{template.defName}_{item.defName}";
        var bill = hotReload ? DefDatabase<ProcessDef>.GetNamed(defName, errorOnFail: false) ?? new ProcessDef() : new ProcessDef();
        var maker = item.recipeMaker;
        int count = Mathf.Max(1, maker.productCount);
        string name = template.billLabel.Formatted(item.label).Resolve() + (count > 1 ? $" x{count}" : "");

        bill.defName = defName;
        bill.label = name;
        bill.labelOverride = name.CapitalizeFirst();
        bill.description = template.billDescription.Formatted(item.label).Resolve();
        bill.descriptionHyperlinks = new List<DefHyperlink> { new(item) };
        bill.modContentPack = template.modContentPack;
        bill.ingredients = ingredients;
        bill.results = new List<ProcessDef.Result> { new() { thing = item, count = count } };
        bill.ticks = Mathf.Max(1, Mathf.RoundToInt(Work(item, stuff) * template.workMultiplier));
        bill.isFactoryProcess = true;
        bill.autoGrabFromHoppers = true;
        bill.autoInputSlots = template.autoInputSlots;
        bill.autoExtract = true;
        bill.onlyGrabAndOutputToFactoryHoppers = true;
        bill.spawnOnInteractionCell = template.spawnOnInteractionCell;
        bill.disallowMixing = true;
        bill.useFirstIngredientAsOutputStuff = item.MadeFromStuff;
        // A made-from-stuff item is placed as a new thing, never added to a stack, so only stacking outputs
        // need the safe limit; the template's limit still makes the machine wait instead of dropping items
        // around itself when the output cell is full.
        bill.maxOutputCount = bill.useFirstIngredientAsOutputStuff
            ? template.maxOutputCount
            : OutputCapRule.Clamp(template.maxOutputCount, item.stackLimit, count);
        bill.considerBuildingCompResource = piped != null && ingredients.Any(i => i.thing == piped);
        bill.sustainerDef = template.sustainerDef;
        bill.sustainerWhenWorking = template.sustainerDef != null;
        bill.effecterDef = template.effecterDef;
        bill.effecterWhenWorking = template.effecterDef != null;
        bill.researchPrerequisites = Research(template, maker);
        bill.modExtensions = new List<DefModExtension>
        {
            new GeneratedBillExtension
            {
                sourceRecipe = DefDatabase<RecipeDef>.GetNamedSilentFail(RecipePrefix + item.defName),
                template = template,
            },
        };
        return bill;
    }

    /// <summary>The bench work for one item: the recipe's own work if it sets one, else the item's work to make.</summary>
    private static float Work(ThingDef item, ThingDef? stuff)
    {
        float work = item.recipeMaker.workAmount >= 0 ? item.recipeMaker.workAmount : item.GetStatValueAbstract(StatDefOf.WorkToMake, stuff);
        return work > 0f ? work : FallbackWork;
    }

    private static List<ResearchProjectDef> Research(BenchBillTemplateDef template, RecipeMakerProperties maker)
    {
        var research = new List<ResearchProjectDef>(template.researchPrerequisites);
        if (maker.researchPrerequisite != null) research.Add(maker.researchPrerequisite);
        if (maker.researchPrerequisites != null) research.AddRange(maker.researchPrerequisites.Where(r => r != null));
        return research.Distinct().ToList();
    }

    /// <summary>
    /// The item the machine's own pipe connection supplies, if any. Pipe System lets a bill marked
    /// considerBuildingCompResource draw an ingredient straight from the building's first pipe network
    /// when the ingredient is that network's refuelling item (chemfuel, with Vanilla Chemfuel Expanded).
    /// </summary>
    private static ThingDef? PipedThing(ThingDef machine)
    {
        var resource = machine.comps?.OfType<CompProperties_Resource>().FirstOrDefault();
        return resource?.pipeNet?.linkToRefuelables?.FirstOrDefault()?.thing;
    }
}
