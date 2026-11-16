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
public class Building_PhobosFieldStove : Building_WorkTable, ISetOutLoad
{
    const int WorkingWindowTicks = 300;
    int lastUsedTick = -99999;
    readonly Phobos.Framework.Sounds.SustainedSound burners = new();

    public bool CookingNow => Find.TickManager.TicksGame - lastUsedTick <= WorkingWindowTicks;

    public float WattsNow => CookingNow ? def.GetModExtension<PhobosSetOutPowerExtension>()?.wattsWorking ?? 0f : 0f;

    PhobosFieldStoveSoundsExtension? Sounds => def.GetModExtension<PhobosFieldStoveSoundsExtension>();

    public override void UsedThisTick()
    {
        base.UsedThisTick();
        if (!CookingNow)
        {
            // A batch starting: the lid's clank once, and the burners until the cook stops (owner's picks, 17 Nov 2026).
            Sounds?.startSound?.PlayOneShot(new TargetInfo(Position, Map));
            burners.Set(Sounds?.workingSound, true, this);
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
}

/// <summary>A recipe offered only while its table's trailer has an upgrade fitted; the bill menu says which.</summary>
public class RecipeWorker_PhobosNeedsNode : RecipeWorker
{
    public override AcceptanceReport AvailableReport(Thing thing, BodyPartRecord? part = null)
    {
        if (recipe.GetModExtension<PhobosRecipeNodeExtension>() is not { node.Length: > 0 } needs) return base.AvailableReport(thing, part);
        if (SetOuts.Owner(thing) is { } owner && VehicleUpgrades.Unlocked(owner.Vehicle, needs.node)) return base.AvailableReport(thing, part);
        return new AcceptanceReport("PhobosSetOut_RecipeNeedsNode".Translate().Resolve());
    }

    public override bool AvailableOnNow(Thing thing, BodyPartRecord? part = null) => AvailableReport(thing, part).Accepted;
}

/// <summary>On a field range's Def: at start-up it takes every recipe of the game's cooking tables, so other mods' stove recipes work on it.</summary>
public class PhobosCopyStoveRecipesExtension : DefModExtension
{
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
            foreach (RecipeDef recipe in recipes)
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
