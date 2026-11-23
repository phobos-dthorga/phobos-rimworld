using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.Sound;

namespace Phobos.VehicleSystems;

/// <summary>
/// A field range or kettle (Framework 0.121.0; docs/development/vehicle-field-kitchen.md): the game's own work table, burning its fuel through
/// the game's refuelling, that remembers when it last cooked so the trailer's power point draws for it only then.
/// </summary>
/// <summary>
/// A part of a field stove that can hold its bills back (Framework 0.122.0: the kettle's water with Dubs Bad Hygiene). The stove asks it whether it
/// may work, and tells it when a batch starts.
/// </summary>
public interface IFieldStoveGate
{
    /// <summary>Why the stove cannot start or carry on a bill now, or null.</summary>
    string? StoveRefusal();

    /// <summary>A batch has started at the stove.</summary>
    void BatchStarted();
}

public class Building_PhobosFieldStove : Building_WorkTable, ISetOutLoad, IBillGiver
{
    const int WorkingWindowTicks = 300;
    int lastUsedTick = -99999;
    readonly Phobos.Framework.Sounds.SustainedSound burners = new();

    public bool CookingNow => Find.TickManager.TicksGame - lastUsedTick <= WorkingWindowTicks;

    public float WattsNow => CookingNow ? def.GetModExtension<PhobosSetOutPowerExtension>()?.wattsWorking ?? 0f : 0f;

    PhobosFieldStoveSoundsExtension? Sounds => def.GetModExtension<PhobosFieldStoveSoundsExtension>();

    /// <summary>Why a part holds its bills back, or null.</summary>
    public string? GateRefusal()
    {
        foreach (ThingComp comp in AllComps)
        {
            if (comp is IFieldStoveGate gate && gate.StoveRefusal() is { } refusal) return refusal;
        }
        return null;
    }

    // The game asks a work table through this interface whether its bills may run, both before a cook starts and while one works (JobDriver_DoBill's
    // fail check), so re-implementing it here holds the stove's bills back when a part says so, with no Harmony.
    bool IBillGiver.CurrentlyUsableForBills() => CurrentlyUsableForBills() && GateRefusal() == null;

    bool IBillGiver.UsableForBillsAfterFueling() => UsableForBillsAfterFueling() && GateRefusal() == null;

    public override string GetInspectString()
    {
        string text = base.GetInspectString();
        if (GateRefusal() is not { } refusal) return text;
        return text.NullOrEmpty() ? refusal : text + "\n" + refusal;
    }

    public override void UsedThisTick()
    {
        base.UsedThisTick();
        if (!CookingNow)
        {
            // A batch starting: the lid's clank once, and the burners until the cook stops (owner's picks, 17 Nov 2026).
            Sounds?.startSound?.PlayOneShot(new TargetInfo(Position, Map));
            burners.Set(Sounds?.workingSound, true, this);
            foreach (ThingComp comp in AllComps)
            {
                if (comp is IFieldStoveGate gate) gate.BatchStarted();
            }
            Seen.Note("trailers.field-stove-cooks", this, SetOuts.Powered(this) ? "powered" : "unpowered");
        }
        lastUsedTick = Find.TickManager.TicksGame;
    }

    public override void TickRare()
    {
        base.TickRare();
        if (!CookingNow) burners.End();
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        burners.End();
        base.DeSpawn(mode);
    }
}

/// <summary>On a field stove's Def: its burners' loop while cooking, and a one-off as a batch starts (the kettle's lid).</summary>
public class PhobosFieldStoveSoundsExtension : DefModExtension
{
    public SoundDef? workingSound;
    public SoundDef? startSound;
}

/// <summary>
/// A set-out piece works faster while its trailer is plugged in (Billy's range and kettle, x1.25 authored): added to the game's work-speed stat
/// for work tables by XML, so the bill's speed, its explanation and every other mod's reading of the stat agree.
/// </summary>
public class StatPart_PhobosSetOutPower : StatPart
{
    static float Factor(StatRequest req) =>
        req.Thing is { Spawned: true } thing && thing.def.GetModExtension<PhobosSetOutPowerExtension>() is { } power && power.speedWhenPowered != 1f && SetOuts.Powered(thing)
            ? power.speedWhenPowered
            : 1f;

    public override void TransformValue(StatRequest req, ref float val) => val *= Factor(req);

    public override string? ExplanationPart(StatRequest req)
    {
        float factor = Factor(req);
        return factor == 1f ? null : "PhobosSetOut_StatPowered".Translate() + ": x" + factor.ToStringPercent();
    }
}

/// <summary>On a recipe: offered only while the trailer that set its table out has this upgrade fitted (Billy's big kettle).</summary>
public class PhobosRecipeNodeExtension : DefModExtension
{
    public string node = "";
    /// <summary>Offered at any table no trailer set out, a home stove (Framework 0.273.0, the fine paste bake): the upgrade gates it on a
    /// trailer only.</summary>
    public bool anywhereElse;
}

/// <summary>A recipe offered only while its table's trailer has an upgrade fitted; the bill menu says which.</summary>
public class RecipeWorker_PhobosNeedsNode : RecipeWorker
{
    public override AcceptanceReport AvailableReport(Thing thing, BodyPartRecord? part = null)
    {
        if (recipe.GetModExtension<PhobosRecipeNodeExtension>() is not { node.Length: > 0 } needs) return base.AvailableReport(thing, part);
        CompPhobosSetOut? owner = SetOuts.Owner(thing);
        if (owner == null && needs.anywhereElse) return base.AvailableReport(thing, part);
        if (owner != null && VehicleUpgrades.Unlocked(owner.Vehicle, needs.node)) return base.AvailableReport(thing, part);
        return new AcceptanceReport("PhobosSetOut_RecipeNeedsNode".Translate().Resolve());
    }

    public override bool AvailableOnNow(Thing thing, BodyPartRecord? part = null) => AvailableReport(thing, part).Accepted;
}

/// <summary>On a field range's Def: at start-up it takes every recipe of the game's cooking tables, so other mods' stove recipes work on it.</summary>
public class PhobosCopyStoveRecipesExtension : DefModExtension
{
    /// <summary>
    /// Benches whose recipes it takes as well (Framework 0.219.0, the kitchen box: Vanilla Brewing Expanded's espresso machine, for its teas and
    /// coffees; the owner, 22 Nov 2026). Named in XML with MayRequire, so a bench that is not loaded is simply left out.
    /// </summary>
    public List<ThingDef> alsoRecipesOf = new();
}

/// <summary>
/// Gives each field range every recipe of the tables the game's cooking work giver lists (found by that list, never by name), at start-up,
/// once every Def is loaded. A recipe a table lists for itself stays its own; the range is only added to who may cook it.
/// </summary>
[StaticConstructorOnStartup]
public static class FieldStoveRecipes
{
    static FieldStoveRecipes()
    {
        List<ThingDef> ranges = DefDatabase<ThingDef>.AllDefsListForReading.Where(d => d.HasModExtension<PhobosCopyStoveRecipesExtension>()).ToList();
        if (ranges.Count == 0) return;
        WorkGiverDef? cook = DefDatabase<WorkGiverDef>.GetNamedSilentFail("DoBillsCook");
        List<ThingDef> stoves = cook?.fixedBillGiverDefs?.Where(d => !ranges.Contains(d) && !d.HasModExtension<PhobosSetOutPowerExtension>()).ToList() ?? new List<ThingDef>();
        var recipes = new HashSet<RecipeDef>(stoves.SelectMany(s => s.AllRecipes));
        foreach (ThingDef range in ranges)
        {
            int added = 0;
            var its = new HashSet<RecipeDef>(recipes);
            foreach (ThingDef bench in range.GetModExtension<PhobosCopyStoveRecipesExtension>().alsoRecipesOf)
            {
                if (bench != null) its.UnionWith(bench.AllRecipes);
            }
            foreach (RecipeDef recipe in its)
            {
                recipe.recipeUsers ??= new List<ThingDef>();
                if (recipe.recipeUsers.Contains(range) || range.recipes?.Contains(recipe) == true) continue;
                recipe.recipeUsers.Add(range);
                added++;
            }
            // The range's list of recipes is cached the first time it is read: forget it so the next read sees what was added.
            AccessTools.Field(typeof(ThingDef), "allRecipesCached")?.SetValue(range, null);
            VehicleSystemsMod.Logger.Debug(range.defName + " cooks " + added + " recipes taken from " + stoves.Count + " cooking tables.");
        }
    }
}

/// <summary>A set-out lamp lights only while its trailer is plugged in; the trailer tells it when that changes.</summary>
public class CompProperties_PhobosSetOutLight : CompProperties
{
    public CompProperties_PhobosSetOutLight() => compClass = typeof(CompPhobosSetOutLight);
}

public class CompPhobosSetOutLight : ThingComp, IThingGlower
{
    bool? lastPowered;

    public bool ShouldBeLitNow() => SetOuts.Powered(parent);

    public override void CompTickRare()
    {
        base.CompTickRare();
        bool powered = ShouldBeLitNow();
        if (lastPowered == powered || !parent.Spawned) return;
        lastPowered = powered;
        parent.GetComp<CompGlower>()?.UpdateLit(parent.Map);
    }
}
