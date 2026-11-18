using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>On a field kitchen's VehicleDef: what its cooks can do in a resting caravan (figures authored, docs/development/vehicle-field-kitchen.md).</summary>
public class PhobosCaravanCookingExtension : DefModExtension
{
    public int mealsPerCook = 20;
    public float fuelPerMeal = 0.5f;
    public ThingDef? meal;
    /// <summary>The recipe whose ingredients and filters a meal takes: the game's simple meal.</summary>
    public RecipeDef? recipe;
    /// <summary>The flat chance a meal is spoilt, cooked under the sky with no tent (the game's own figure for cooking outdoors).</summary>
    public float outdoorPoisonChance = 0.02f;
    /// <summary>Cooking experience a meal gives its cook (authored).</summary>
    public float experiencePerMeal = 20f;
}

/// <summary>
/// Caravan cooking (Framework 0.121.0; docs/development/vehicle-field-kitchen.md, owner's choice of 13 Nov 2026, a setting on by default): once a
/// night, while a caravan rests, its cooks turn raw food from its packs into simple meals at a field kitchen it carries, on its own wheels or
/// behind a hitch. Each meal takes what the game's simple meal takes and burns the kitchen's fuel, or chemfuel and wood from the packs.
/// Nothing patches the game's caravan food: meals simply join the packs, and the game feeds people as it always does.
/// </summary>
public class PhobosCaravanCooking : WorldComponent
{
    const int CheckEveryTicks = GenDate.TicksPerHour;
    /// <summary>A caravan cooks once a rest: not again until this long after it last did.</summary>
    const int TicksBetweenNights = GenDate.TicksPerDay / 2;

    Dictionary<int, int> lastCooked = new();

    public PhobosCaravanCooking(World world) : base(world)
    {
    }

    public override void WorldComponentTick()
    {
        base.WorldComponentTick();
        int now = Find.TickManager.TicksGame;
        if (now % CheckEveryTicks != 0 || !VehicleSystemsMod.Settings.caravanCooking) return;
        foreach (Caravan caravan in Find.WorldObjects.Caravans)
        {
            if (caravan.Faction != Faction.OfPlayer || !caravan.NightResting) continue;
            if (lastCooked.TryGetValue(caravan.ID, out int last) && now - last < TicksBetweenNights) continue;
            if (Kitchen(caravan) is not { } kitchen) continue;
            lastCooked[caravan.ID] = now;
            Cook(caravan, kitchen);
        }
    }

    /// <summary>The first field kitchen the caravan carries: a vehicle in it, or one riding a hitch.</summary>
    static VehiclePawn? Kitchen(Caravan caravan)
    {
        foreach (Pawn pawn in caravan.PawnsListForReading)
        {
            if (pawn is not VehiclePawn vehicle) continue;
            if (vehicle.def.HasModExtension<PhobosCaravanCookingExtension>()) return vehicle;
            if (vehicle.GetComp<CompPhobosHitch>()?.GetDirectlyHeldThings() is { } towed)
            {
                foreach (Thing thing in towed)
                {
                    if (thing is VehiclePawn trailer && trailer.def.HasModExtension<PhobosCaravanCookingExtension>()) return trailer;
                }
            }
        }
        return null;
    }

    static void Cook(Caravan caravan, VehiclePawn kitchen)
    {
        PhobosCaravanCookingExtension rules = kitchen.def.GetModExtension<PhobosCaravanCookingExtension>();
        RecipeDef? recipe = rules.recipe;
        ThingDef? mealDef = rules.meal;
        if (recipe == null || mealDef == null || recipe.ingredients.Count == 0) return;
        NamedArgument caravanName = caravan.Label.Named("CARAVAN");

        // Whoever the recipe's own work type and speed allow: the game's simple meal names cooking and cooking speed.
        List<Pawn> cooks = caravan.PawnsListForReading
            .Where(p => p.RaceProps.Humanlike && !p.Downed && !p.Dead && p.skills != null
                && (recipe.requiredGiverWorkType == null || !p.WorkTypeIsDisabled(recipe.requiredGiverWorkType))
                && (recipe.workSpeedStat == null || p.GetStatValue(recipe.workSpeedStat) > 0f))
            .OrderByDescending(p => p.skills.GetSkill(SkillDefOf.Cooking).Level).ToList();
        if (cooks.Count == 0)
        {
            Tell("PhobosCaravanCooking_NoCook".Translate(caravanName).Resolve(), caravan, MessageTypeDefOf.NeutralEvent);
            return;
        }

        float perMeal = recipe.ingredients[0].GetBaseCount();
        List<Thing> raw = CaravanInventoryUtility.AllInventoryItems(caravan).Where(t => Allowed(recipe, t)).ToList();
        float nutrition = raw.Sum(t => t.GetStatValue(StatDefOf.Nutrition) * t.stackCount);
        List<CompRefuelable> burners = kitchen.GetComp<CompPhobosSetOut>()?.PackedPieces.Select(p => p.TryGetComp<CompRefuelable>()).OfType<CompRefuelable>().ToList() ?? new List<CompRefuelable>();
        List<Thing> carriedFuel = CaravanInventoryUtility.AllInventoryItems(caravan)
            .Where(t => burners.Count > 0 ? burners[0].Props.fuelFilter.Allows(t) : t.def == ThingDefOf.Chemfuel || t.def == ThingDefOf.WoodLog).ToList();
        float fuel = burners.Sum(b => b.Fuel) + carriedFuel.Sum(t => t.stackCount);

        int meals = Mathf.Min(cooks.Count * rules.mealsPerCook, Mathf.FloorToInt(nutrition / perMeal + 0.0001f), rules.fuelPerMeal > 0f ? Mathf.FloorToInt(fuel / rules.fuelPerMeal) : int.MaxValue);
        if (meals <= 0)
        {
            Tell((nutrition < perMeal ? "PhobosCaravanCooking_NoFood" : "PhobosCaravanCooking_NoFuel").Translate(caravanName).Resolve(), caravan, MessageTypeDefOf.NeutralEvent);
            return;
        }

        TakeNutrition(raw, meals * perMeal);
        BurnFuel(burners, carriedFuel, meals * rules.fuelPerMeal);

        int poisoned = 0, left = meals;
        foreach (Pawn cook in cooks)
        {
            int byThisCook = Mathf.Min(left, rules.mealsPerCook);
            if (byThisCook <= 0) break;
            left -= byThisCook;
            float chance = Mathf.Clamp01(cook.GetStatValue(StatDefOf.FoodPoisonChance) + rules.outdoorPoisonChance);
            for (int i = 0; i < byThisCook; i++)
            {
                if (Rand.Chance(chance)) poisoned++;
            }
            cook.skills.Learn(SkillDefOf.Cooking, byThisCook * rules.experiencePerMeal);
        }

        Give(caravan, mealDef, meals - poisoned, null);
        Give(caravan, mealDef, poisoned, FoodPoisonCause.IncompetentCook);
        Tell("PhobosCaravanCooking_Cooked".Translate(caravanName, meals.Named("COUNT"), mealDef.label.Named("MEAL")).Resolve(), caravan, MessageTypeDefOf.PositiveEvent);
        if (Seen.Wants("trailers.caravan-cooking", kitchen)) Seen.Note("trailers.caravan-cooking", kitchen, meals + " meals, " + poisoned + " spoilt");
    }

    static bool Allowed(RecipeDef recipe, Thing thing) =>
        thing.def.IsNutritionGivingIngestible && recipe.fixedIngredientFilter.Allows(thing) && (recipe.defaultIngredientFilter?.Allows(thing) ?? true)
        && recipe.ingredients[0].filter.Allows(thing);

    /// <summary>Takes this much nutrition from the raw food, the most plentiful first, splitting stacks as the game's cooks would.</summary>
    static void TakeNutrition(List<Thing> raw, float wanted)
    {
        foreach (Thing thing in raw.OrderByDescending(t => t.stackCount))
        {
            if (wanted <= 0.0001f) return;
            float each = thing.GetStatValue(StatDefOf.Nutrition);
            if (each <= 0f) continue;
            int count = Mathf.Min(thing.stackCount, Mathf.CeilToInt(wanted / each - 0.0001f));
            wanted -= count * each;
            thing.SplitOff(count).Destroy();
        }
    }

    static void BurnFuel(List<CompRefuelable> burners, List<Thing> carried, float wanted)
    {
        foreach (CompRefuelable burner in burners)
        {
            float take = Mathf.Min(burner.Fuel, wanted);
            if (take <= 0f) continue;
            burner.ConsumeFuel(take);
            wanted -= take;
        }
        foreach (Thing thing in carried)
        {
            if (wanted <= 0.0001f) return;
            int count = Mathf.Min(thing.stackCount, Mathf.CeilToInt(wanted - 0.0001f));
            wanted -= count;
            thing.SplitOff(count).Destroy();
        }
    }

    static void Give(Caravan caravan, ThingDef mealDef, int count, FoodPoisonCause? poison)
    {
        while (count > 0)
        {
            int stack = Mathf.Min(count, mealDef.stackLimit);
            count -= stack;
            Thing meals = ThingMaker.MakeThing(mealDef);
            meals.stackCount = stack;
            if (poison is { } cause) meals.TryGetComp<CompFoodPoisonable>()?.SetPoisoned(cause);
            CaravanInventoryUtility.GiveThing(caravan, meals);
        }
    }

    static void Tell(string text, Caravan caravan, MessageTypeDef type) => Messages.Message(text, caravan, type, historical: false);

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref lastCooked, "PhobosCaravanCooking_LastCooked", LookMode.Value, LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit) lastCooked ??= new Dictionary<int, int>();
    }
}
