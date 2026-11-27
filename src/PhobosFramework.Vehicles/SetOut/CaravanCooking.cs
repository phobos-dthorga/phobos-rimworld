using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Vehicles;
using Vehicles.World;
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
    /// <summary>The share of a cook's meals the kettle alone gives while the caravan drives through the night (authored: half).</summary>
    public float onTheMoveShare = 0.5f;
    /// <summary>How long a meal halt holds a caravan that drives through the night, in ticks (authored: an hour).</summary>
    public int mealHaltTicks = GenDate.TicksPerHour;
    /// <summary>
    /// The water a meal takes from the caravan's tanks, in litres (authored: the kettle's 10 L for a batch of ten). Asked only where
    /// something gives water a meaning (CaravanWater has a source: Dubs Bad Hygiene).
    /// </summary>
    public float waterPerMeal = 1f;
    /// <summary>
    /// The stove runs on the vehicle's own fuel (Framework 0.366.0, a galley fitting): a trawler's diesel range, or an aircraft's galley ovens on
    /// the generators its engines or auxiliary power unit turn. Its fuel per meal comes from the tank of the vehicle it is fitted to, never
    /// from burners or the packs.
    /// </summary>
    public bool fromVehicleTank;
}

/// <summary>
/// A kitchen a caravan carries (Framework 0.219.0): a field kitchen on wheels, driven or towed, or a kitchen box packed in the cargo. What its
/// cooks may make is its own Def's (PhobosCaravanCookingExtension, on the vehicle's Def or the box's), and its burners are what it holds.
/// </summary>
public sealed class CaravanKitchen
{
    public readonly Thing Source;
    public readonly PhobosCaravanCookingExtension Rules;
    public readonly List<CompRefuelable> Burners;
    /// <summary>A kitchen box rather than a field kitchen: the field kitchen's own seen-in-play notes are not written for it.</summary>
    public readonly bool IsBox;
    /// <summary>A galley fitted to this vehicle (Framework 0.366.0), whose stove burns the vehicle's own fuel; null for any other kitchen.</summary>
    public readonly VehiclePawn? Galley;

    public CaravanKitchen(Thing source, PhobosCaravanCookingExtension rules, List<CompRefuelable> burners, bool isBox, VehiclePawn? galley = null)
    {
        Source = source;
        Rules = rules;
        Burners = burners;
        IsBox = isBox;
        Galley = galley;
    }
}

/// <summary>
/// Caravan cooking (Framework 0.121.0; docs/development/vehicle-field-kitchen.md, owner's choice of 13 Nov 2026, a setting on by default): once a
/// night, while a caravan rests, its cooks turn raw food from its packs into simple meals at a field kitchen it carries, on its own wheels or
/// behind a hitch. Each meal takes what the game's simple meal takes and burns the kitchen's fuel, or chemfuel and wood from the packs.
/// The cooks also reach the cargo of whatever the caravan tows and a packed cold box (Framework 0.216.0).
/// Nothing patches the game's caravan food: meals simply join the packs, and the game feeds people as it always does.
/// A caravan that drives through the night (Framework 0.214.0) has no night's rest, so since Framework 0.216.0 (the owner, 22 Nov 2026) its
/// kettle simmers on the move, tended by riders who are not driving, for a share of the meals; or, where the player chose meal halts for that
/// caravan, it stops about an hour to cook as at camp and drives on (CaravanCookRule says which).
/// </summary>
public class PhobosCaravanCooking : WorldComponent
{
    const int CheckEveryTicks = GenDate.TicksPerHour;
    /// <summary>A caravan cooks once a rest: not again until this long after it last did.</summary>
    const int TicksBetweenNights = GenDate.TicksPerDay / 2;

    /// <summary>How long a look for a caravan's kitchen is kept for its buttons (buttons read caches, never compute).</summary>
    const int KitchenLookTicks = 250;

    Dictionary<int, int> lastCooked = new();
    /// <summary>Caravans the player chose meal halts for, by ID. Saved.</summary>
    HashSet<int> mealHalts = new();
    /// <summary>The kitchen each caravan carries, as last looked. Derived, never saved.</summary>
    readonly Dictionary<int, (int tick, PhobosCaravanCookingExtension? kitchen)> kitchens = new();

    public static PhobosCaravanCooking? Current => Find.World?.GetComponent<PhobosCaravanCooking>();

    /// <summary>Whether this caravan stops for a meal while it drives through the night, instead of simmering on the move.</summary>
    public bool WantsHalts(Caravan caravan) => mealHalts.Contains(caravan.ID);

    public void ToggleHalts(Caravan caravan)
    {
        if (!mealHalts.Remove(caravan.ID)) mealHalts.Add(caravan.ID);
    }

    /// <summary>The rules of the kitchen this caravan carries, or null, looked for at most every few seconds: for its buttons.</summary>
    public PhobosCaravanCookingExtension? KitchenOf(Caravan caravan)
    {
        int now = Find.TickManager.TicksGame;
        if (kitchens.TryGetValue(caravan.ID, out (int tick, PhobosCaravanCookingExtension? kitchen) seen) && now - seen.tick < KitchenLookTicks && now >= seen.tick) return seen.kitchen;
        PhobosCaravanCookingExtension? kitchen = Kitchen(caravan)?.Rules;
        kitchens[caravan.ID] = (now, kitchen);
        return kitchen;
    }

    public PhobosCaravanCooking(World world) : base(world)
    {
    }

    public override void WorldComponentTick()
    {
        base.WorldComponentTick();
        int now = Find.TickManager.TicksGame;
        if (now % CheckEveryTicks != 0 || !VehicleSystemsMod.Settings.caravanCooking) return;
        NightDriving? driving = NightDriving.Enabled ? NightDriving.Current : null;
        foreach (Caravan caravan in Find.WorldObjects.Caravans)
        {
            if (caravan.Faction != Faction.OfPlayer) continue;
            bool resting = caravan.NightResting;
            CaravanCookRule.Way way = CaravanCookRule.WayFor(resting, !resting && driving?.DrivesOn(caravan) == true, WantsHalts(caravan));
            if (way == CaravanCookRule.Way.None) continue;
            if (lastCooked.TryGetValue(caravan.ID, out int last) && now - last < TicksBetweenNights) continue;
            if (Kitchen(caravan) is not { } kitchen) continue;
            lastCooked[caravan.ID] = now;
            int meals = Cook(caravan, kitchen, way);
            // A meal halt holds the caravan only when there was a meal to stop for.
            if (way == CaravanCookRule.Way.Halt && meals > 0) driving?.Halt(caravan, kitchen.Rules.mealHaltTicks);
        }
    }

    /// <summary>
    /// The kitchen the caravan cooks at: a field kitchen it carries first, a vehicle in it or one anywhere along a line of tows; else a kitchen
    /// box packed in its cargo, in tow or in anyone's packs (Framework 0.219.0; the owner, 22 Nov 2026: with a Billy there too, the Billy cooks).
    /// </summary>
    static CaravanKitchen? Kitchen(Caravan caravan)
    {
        foreach (Pawn pawn in caravan.PawnsListForReading)
        {
            if (pawn is VehiclePawn vehicle && vehicle.def.GetModExtension<PhobosCaravanCookingExtension>() is { } rules) return OnWheels(vehicle, rules);
        }
        foreach (VehiclePawn towed in Hitches.InTow(caravan))
        {
            if (towed.def.GetModExtension<PhobosCaravanCookingExtension>() is { } rules) return OnWheels(towed, rules);
        }
        // Framework 0.366.0 (the vehicle standard's batch 4): a galley fitted to a vehicle of the caravan, after a field kitchen and before a box.
        foreach (Pawn pawn in caravan.PawnsListForReading)
        {
            if (pawn is VehiclePawn vehicle && GalleyOf(vehicle) is { } rules) return new CaravanKitchen(vehicle, rules, new List<CompRefuelable>(), isBox: false, galley: vehicle);
        }
        foreach (Thing thing in Larder(caravan).things)
        {
            if (thing is MinifiedThing { InnerThing: { } inner } && inner.def.GetModExtension<PhobosCaravanCookingExtension>() is { } rules)
            {
                var burners = new List<CompRefuelable>();
                if (inner.TryGetComp<CompRefuelable>() is { } burner) burners.Add(burner);
                return new CaravanKitchen(inner, rules, burners, isBox: true);
            }
        }
        return null;
    }

    /// <summary>The fittings that are galleys (a fitting Def carrying cooking rules), read once: Defs do not change while the game runs.</summary>
    static List<PhobosVehicleFittingDef>? galleys;

    /// <summary>What a galley fitting grants (Fittings.xml, PhobosFitting_Galley): it counts only while its bay is not ruined.</summary>
    public const string GalleyGrant = "Galley";

    /// <summary>The cooking rules of a galley fitted to this vehicle and counting (its bay not ruined), or null.</summary>
    static PhobosCaravanCookingExtension? GalleyOf(VehiclePawn vehicle)
    {
        galleys ??= DefDatabase<PhobosVehicleFittingDef>.AllDefsListForReading.Where(d => d.HasModExtension<PhobosCaravanCookingExtension>()).ToList();
        if (galleys.Count == 0 || CompPhobosVehicleSystems.Of(vehicle) is not { } comp || !comp.Fittings.Grants(GalleyGrant)) return null;
        foreach (PhobosVehicleFittingDef def in galleys)
        {
            if (comp.Fittings.Has(def)) return def.GetModExtension<PhobosCaravanCookingExtension>();
        }
        return null;
    }

    static CaravanKitchen OnWheels(VehiclePawn vehicle, PhobosCaravanCookingExtension rules) =>
        new(vehicle, rules, vehicle.GetComp<CompPhobosSetOut>()?.PackedPieces.Select(p => p.TryGetComp<CompRefuelable>()).OfType<CompRefuelable>().ToList() ?? new List<CompRefuelable>(), isBox: false);

    /// <summary>
    /// Everything the cooks can reach (Framework 0.216.0; the owner, 22 Nov 2026): the caravan's packs, the cargo of every vehicle it tows, and
    /// what a packed cold box holds. A towed trailer rides in its hitch, so the game's packs never held its cargo, and food loaded into the
    /// kitchen itself was out of its own cooks' reach. Second: how many of those stacks are in tow, for the seen-in-play note.
    /// </summary>
    static (List<Thing> things, int inTow) Larder(Caravan caravan)
    {
        var things = new List<Thing>(CaravanInventoryUtility.AllInventoryItems(caravan));
        int packs = things.Count;
        foreach (Pawn pawn in caravan.PawnsListForReading)
        {
            if (pawn is VehiclePawn vehicle) AddColdBoxes(vehicle, things);
        }
        foreach (VehiclePawn towed in Hitches.InTow(caravan))
        {
            if (towed.inventory?.innerContainer is { } cargo) things.AddRange(cargo);
            AddColdBoxes(towed, things);
        }
        return (things, things.Count - packs);
    }

    static void AddColdBoxes(VehiclePawn vehicle, List<Thing> things)
    {
        if (vehicle.GetComp<CompPhobosSetOut>() is not { } setOut) return;
        foreach (Thing piece in setOut.PackedPieces)
        {
            if (piece is Building_PhobosColdBox box) things.AddRange(box.GetDirectlyHeldThings());
        }
    }

    /// <summary>The people who drive or crew a vehicle of this caravan now: they tend no kettle on the move.</summary>
    static HashSet<Pawn> Crews(Caravan caravan)
    {
        var crews = new HashSet<Pawn>();
        if (caravan is not VehicleCaravan vehicles) return crews;
        foreach (VehiclePawn vehicle in vehicles.VehiclesListForReading)
        {
            foreach (Pawn pawn in vehicle.AllPawnsAboard)
            {
                if (Berths.IsCrew(vehicle, pawn)) crews.Add(pawn);
            }
        }
        return crews;
    }

    /// <summary>Cooks the night's meals the given way, and says what happened. Returns the meals made.</summary>
    /// <summary>Whether a vehicle in the caravan carries a closed tent (a room its camp would pitch) to cook in at camp.</summary>
    static bool ClosedTent(Caravan caravan)
    {
        foreach (Pawn pawn in caravan.PawnsListForReading)
        {
            if (pawn is VehiclePawn vehicle && vehicle.GetComp<CompPhobosCamp>() is { } camp && camp.Fit.Rooms > 0) return true;
        }
        return false;
    }

    static int Cook(Caravan caravan, CaravanKitchen at, CaravanCookRule.Way way)
    {
        PhobosCaravanCookingExtension rules = at.Rules;
        Thing kitchen = at.Source;
        RecipeDef? recipe = rules.recipe;
        ThingDef? mealDef = rules.meal;
        if (recipe == null || mealDef == null || recipe.ingredients.Count == 0) return 0;
        NamedArgument caravanName = caravan.Label.Named("CARAVAN");
        bool simmer = way == CaravanCookRule.Way.Simmer;
        HashSet<Pawn>? crews = simmer ? Crews(caravan) : null;

        // Whoever the recipe's own work type and speed allow: the game's simple meal names cooking and cooking speed.
        List<Pawn> cooks = caravan.PawnsListForReading
            .Where(p => p.RaceProps.Humanlike && !p.Downed && !p.Dead && p.skills != null
                && (recipe.requiredGiverWorkType == null || !p.WorkTypeIsDisabled(recipe.requiredGiverWorkType))
                && (recipe.workSpeedStat == null || p.GetStatValue(recipe.workSpeedStat) > 0f)
                && (crews == null || !crews.Contains(p)))
            .OrderByDescending(p => p.skills.GetSkill(SkillDefOf.Cooking).Level).ToList();
        if (cooks.Count == 0)
        {
            string nobody = simmer ? "PhobosCaravanCooking_NoRider".Translate(caravanName).Resolve() : "PhobosCaravanCooking_NoCook".Translate(caravanName).Resolve();
            Tell(nobody, caravan, MessageTypeDefOf.NeutralEvent);
            return 0;
        }

        float perMeal = recipe.ingredients[0].GetBaseCount();
        (List<Thing> larder, int inTow) = Larder(caravan);
        List<Thing> raw = larder.Where(t => Allowed(recipe, t)).ToList();
        float nutrition = raw.Sum(t => t.GetStatValue(StatDefOf.Nutrition) * t.stackCount);
        List<CompRefuelable> burners = at.Burners;
        // Framework 0.366.0: a galley's stove burns its vehicle's own fuel, never the packs'.
        CompFueledTravel? tank = rules.fromVehicleTank ? at.Galley?.CompFueledTravel : null;
        List<Thing> carriedFuel = rules.fromVehicleTank ? new List<Thing>() : larder
            .Where(t => burners.Count > 0 ? burners[0].Props.fuelFilter.Allows(t) : t.def == ThingDefOf.Chemfuel || t.def == ThingDefOf.WoodLog).ToList();
        float fuel = rules.fromVehicleTank ? tank?.Fuel ?? 0f : burners.Sum(b => b.Fuel) + carriedFuel.Sum(t => t.stackCount);

        // On the move the kettle alone works, for a share of each cook's meals; at camp or a halt, the whole kitchen.
        int perCook = CaravanCookRule.PerCook(rules.mealsPerCook, simmer ? rules.onTheMoveShare : 1.0);
        // The kettle's water, where water means anything (Framework 0.216.0): on a map each batch of ten takes 10 L, so on the road each meal takes one.
        float waterPerMeal = CaravanWater.HasSource ? rules.waterPerMeal : 0f;
        float water = waterPerMeal > 0f ? CaravanWater.Litres(caravan) : 0f;
        int meals = CaravanCookRule.Meals(cooks.Count, perCook, nutrition, perMeal, fuel, rules.fuelPerMeal, water, waterPerMeal);
        if (meals <= 0)
        {
            string why = nutrition < perMeal ? "PhobosCaravanCooking_NoFood".Translate(caravanName).Resolve()
                : waterPerMeal > 0f && water < waterPerMeal ? "PhobosCaravanCooking_NoWater".Translate(caravanName).Resolve()
                : "PhobosCaravanCooking_NoFuel".Translate(caravanName).Resolve();
            Tell(why, caravan, MessageTypeDefOf.NeutralEvent);
            return 0;
        }
        if (waterPerMeal > 0f)
        {
            float drawn = CaravanWater.Take(caravan, meals * waterPerMeal, kitchenFirst: true);
            if (Seen.Wants("trailers.caravan-kettle-water", kitchen)) Seen.Note("trailers.caravan-kettle-water", kitchen, drawn.ToString("0.#") + " L");
        }

        List<ThingDef> madeFrom = TakeNutrition(raw, meals * perMeal);
        if (tank != null) tank.ConsumeFuel(meals * rules.fuelPerMeal);
        else BurnFuel(burners, carriedFuel, meals * rules.fuelPerMeal);

        // At camp in a closed tent (a walled awning, a rear tent, a camper), not under the sky: no outdoor chance (Framework 0.292.0).
        float place = (float)CaravanCookRule.PlaceChance(way, ClosedTent(caravan), rules.outdoorPoisonChance);
        int poisoned = 0, left = meals;
        foreach (Pawn cook in cooks)
        {
            int byThisCook = Mathf.Min(left, perCook);
            if (byThisCook <= 0) break;
            left -= byThisCook;
            float chance = Mathf.Clamp01(cook.GetStatValue(StatDefOf.FoodPoisonChance) + place);
            for (int i = 0; i < byThisCook; i++)
            {
                if (Rand.Chance(chance)) poisoned++;
            }
            cook.skills.Learn(SkillDefOf.Cooking, byThisCook * rules.experiencePerMeal);
        }

        Give(caravan, mealDef, meals - poisoned, null, madeFrom);
        Give(caravan, mealDef, poisoned, FoodPoisonCause.IncompetentCook, madeFrom);
        NamedArgument count = meals.Named("COUNT"), meal = mealDef.label.Named("MEAL");
        string said = way switch
        {
            CaravanCookRule.Way.Simmer => "PhobosCaravanCooking_Simmered".Translate(caravanName, count, meal).Resolve(),
            CaravanCookRule.Way.Halt => "PhobosCaravanCooking_Halted".Translate(caravanName, count, meal, rules.mealHaltTicks.ToStringTicksToPeriod().Named("TIME")).Resolve(),
            _ => "PhobosCaravanCooking_Cooked".Translate(caravanName, count, meal).Resolve(),
        };
        Tell(said, caravan, MessageTypeDefOf.PositiveEvent);
        string how = way switch
        {
            CaravanCookRule.Way.Simmer => "PhobosLog_RoadMealsSimmer".Translate().Resolve(),
            CaravanCookRule.Way.Halt => "PhobosLog_RoadMealsHalt".Translate().Resolve(),
            _ => "PhobosLog_RoadMealsCamp".Translate().Resolve(),
        };
        VehicleStory.Convoy(caravan, VehicleStoryDefOf.PhobosLog_RoadMeals, detail: how, value: meals);
        if (at.IsBox)
        {
            if (Seen.Wants("camp.kitchen-box-caravan", kitchen, way.ToString())) Seen.Note("camp.kitchen-box-caravan", kitchen, meals + " meals, " + poisoned + " spoilt", way.ToString());
            return meals;
        }
        if (at.Galley != null)
        {
            // Framework 0.366.0 (test row 745): a galley fitted to a transport aircraft or a trawler cooked for its caravan.
            if (Seen.Wants("vehicles.galley-caravan", kitchen, way.ToString()))
                Seen.Note("vehicles.galley-caravan", kitchen, meals + " meals, " + poisoned + " spoilt", way.ToString(),
                    new[] { new KeyValuePair<string, double>("meals", meals), new KeyValuePair<string, double>("fuel", meals * rules.fuelPerMeal) });
            return meals;
        }
        if (inTow > 0 && Seen.Wants("trailers.caravan-larder", kitchen)) Seen.Note("trailers.caravan-larder", kitchen, inTow + " stacks in tow within the cooks' reach");
        switch (way)
        {
            case CaravanCookRule.Way.Simmer:
                if (Seen.Wants("trailers.caravan-simmer", kitchen)) Seen.Note("trailers.caravan-simmer", kitchen, meals + " meals, " + poisoned + " spoilt");
                break;
            case CaravanCookRule.Way.Halt:
                if (Seen.Wants("trailers.caravan-meal-halt", kitchen)) Seen.Note("trailers.caravan-meal-halt", kitchen, meals + " meals, " + poisoned + " spoilt");
                break;
            default:
                if (Seen.Wants("trailers.caravan-cooking", kitchen)) Seen.Note("trailers.caravan-cooking", kitchen, meals + " meals, " + poisoned + " spoilt");
                break;
        }
        return meals;
    }

    static bool Allowed(RecipeDef recipe, Thing thing) =>
        thing.def.IsNutritionGivingIngestible && recipe.fixedIngredientFilter.Allows(thing) && (recipe.defaultIngredientFilter?.Allows(thing) ?? true)
        && recipe.ingredients[0].filter.Allows(thing);

    /// <summary>Takes this much nutrition from the raw food, the most plentiful first, splitting stacks as the game's cooks would. Returns
    /// what it took, for the meals' ingredients (fault-read 25 Nov 2026, B8: precepts read them).</summary>
    static List<ThingDef> TakeNutrition(List<Thing> raw, float wanted)
    {
        var used = new List<ThingDef>();
        foreach (Thing thing in raw.OrderByDescending(t => t.stackCount))
        {
            if (wanted <= 0.0001f) break;
            float each = thing.GetStatValue(StatDefOf.Nutrition);
            if (each <= 0f) continue;
            int count = Mathf.Min(thing.stackCount, Mathf.CeilToInt(wanted / each - 0.0001f));
            wanted -= count * each;
            if (count > 0 && !used.Contains(thing.def)) used.Add(thing.def);
            thing.SplitOff(count).Destroy();
        }
        return used;
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

    static void Give(Caravan caravan, ThingDef mealDef, int count, FoodPoisonCause? poison, List<ThingDef> madeFrom)
    {
        while (count > 0)
        {
            int stack = Mathf.Min(count, mealDef.stackLimit);
            count -= stack;
            Thing meals = ThingMaker.MakeThing(mealDef);
            meals.stackCount = stack;
            if (poison is { } cause) meals.TryGetComp<CompFoodPoisonable>()?.SetPoisoned(cause);
            // What they were cooked from, as a stove's meals record it (the game keeps the first three).
            if (meals.TryGetComp<CompIngredients>() is { } ingredients)
            {
                foreach (ThingDef food in madeFrom) ingredients.RegisterIngredient(food);
            }
            CaravanInventoryUtility.GiveThing(caravan, meals);
        }
    }

    static void Tell(string text, Caravan caravan, MessageTypeDef type) => Messages.Message(text, caravan, type, historical: false);

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref lastCooked, "PhobosCaravanCooking_LastCooked", LookMode.Value, LookMode.Value);
        Scribe_Collections.Look(ref mealHalts, "PhobosCaravanCooking_MealHalts", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            lastCooked ??= new Dictionary<int, int>();
            mealHalts ??= new HashSet<int>();
        }
    }
}
