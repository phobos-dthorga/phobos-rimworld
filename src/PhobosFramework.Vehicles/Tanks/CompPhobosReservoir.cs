using System.Collections.Generic;
using Phobos.Framework.Rules;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;
using Verse.Sound;

namespace Phobos.VehicleSystems;

/// <summary>
/// A small store of one tank load in a building (Framework 0.273.0; docs/development/vehicle-nutrient-paste.md, NP4): the Billy's paste tap
/// keeps its own paste in one. A tank of ours set to pump out fills it while it stands within the tank's hose reach (Port_Reservoirs), carrying
/// what the load was made from; the building draws on it. It spoils by the load's own days at the building's temperature, as a tank does.
/// Other modders give any building one in XML with its contents and size.
/// </summary>
public class CompProperties_PhobosReservoir : CompProperties
{
    /// <summary>The only load it holds.</summary>
    public PhobosTankContentsDef? contents;
    /// <summary>How many units it holds (authored).</summary>
    public float capacity = 100f;
    /// <summary>A set-out piece's trailer upgrade that makes it bigger (the Billy's bigger paste tank), and how many it then holds.</summary>
    public string? node;
    public float capacityWithNode;
    /// <summary>How fast its spoilage clock runs (1 is a plain tank).</summary>
    public float spoilRate = 1f;

    public CompProperties_PhobosReservoir() => compClass = typeof(CompPhobosReservoir);

    /// <summary>What it holds on this trailer: more while the trailer has the upgrade named (null: no trailer, the plain size).</summary>
    public float CapacityOn(VehiclePawn? trailer) =>
        node.NullOrEmpty() || capacityWithNode <= 0f || trailer == null || !VehicleUpgrades.Unlocked(trailer, node!) ? capacity : capacityWithNode;
}

public class CompPhobosReservoir : ThingComp
{
    float amount;
    float spoilHours;
    List<ThingDef> ingredients = new();

    const float HoursPerRareTick = GenTicks.TickRareInterval / (float)GenDate.TicksPerHour;

    public CompProperties_PhobosReservoir Props => (CompProperties_PhobosReservoir)props;
    public float Amount => amount;
    /// <summary>What it holds now: more while the trailer that set it out, or holds it packed, has the upgrade its Props name.</summary>
    public float Capacity => Props.CapacityOn(SetOuts.Owner(parent)?.Vehicle);
    public float Space => Mathf.Max(0f, Capacity - amount);
    public IReadOnlyList<ThingDef> Ingredients => ingredients;

    /// <summary>Whether it takes this load.</summary>
    public bool Holds(PhobosTankContentsDef? contents) => contents != null && contents == Props.contents;

    /// <summary>Puts up to <paramref name="units"/> in, with what they were made from and how old they are (in spoilage hours); returns how
    /// many went in. The older load sets the clock (fault-read 25 Nov 2026, B7).</summary>
    public float Add(float units, IEnumerable<ThingDef>? madeFrom, float ageHours = 0f)
    {
        float added = Mathf.Min(units, Space);
        if (added <= 0f) return 0f;
        spoilHours = amount <= 0f ? ageHours : Mathf.Max(spoilHours, ageHours);
        amount += added;
        if (madeFrom != null)
        {
            foreach (ThingDef def in madeFrom)
            {
                if (def != null && !ingredients.Contains(def)) ingredients.Add(def);
            }
        }
        return added;
    }

    /// <summary>Takes <paramref name="units"/> out when it has them; false and nothing taken otherwise.</summary>
    public bool Take(float units)
    {
        if (amount < units) return false;
        amount -= units;
        // Less than a serving left can never be taken: the dregs go (fault-read 25 Nov 2026, B7).
        if (amount < 1f) Empty();
        return true;
    }

    /// <summary>A meal of <paramref name="item"/>, unplaced, carrying what the load was made from. Takes nothing out.</summary>
    public Thing MealOf(ThingDef item)
    {
        Thing meal = ThingMaker.MakeThing(item);
        if (meal.TryGetComp<CompIngredients>() is { } made)
        {
            foreach (ThingDef ingredient in ingredients) made.RegisterIngredient(ingredient);
        }
        return meal;
    }

    /// <summary>The meals it could still make, for a caravan's days of food (Framework 0.347.0: a packed paste tap feeds a caravan); none
    /// when its load is no food.</summary>
    public ThingDefCount? EdibleMeals()
    {
        if (Props.contents?.EdibleItem is not { } edible) return null;
        int meals = Mathf.FloorToInt(amount / edible.units);
        return meals > 0 ? new ThingDefCount(edible.item, meals) : null;
    }

    /// <summary>Draws one meal a person may eat now, carrying what the load was made from, and returns it unplaced (null with none). The
    /// caller hands it to the game's own eating.</summary>
    public Thing? TakeMeal(Pawn forPawn)
    {
        if (Props.contents?.EdibleItem is not { } edible || amount < edible.units) return null;
        Thing meal = MealOf(edible.item);
        if (!RimWorld.Planet.CaravanPawnsNeedsUtility.CanEatForNutritionNow(meal, forPawn) || !Take(edible.units))
        {
            meal.Destroy();
            return null;
        }
        return meal;
    }

    void Empty()
    {
        amount = 0f;
        spoilHours = 0f;
        ingredients.Clear();
    }

    public override void CompTickRare()
    {
        base.CompTickRare();
        // Packed in its trailer it ages too (fault-read 25 Nov 2026, B7): the trailer's packed pieces are ticked for it (HeldGoods), at the
        // temperature where it is held, which the game's AmbientTemperature finds through its holders, on a map or on the road.
        if (amount <= 0f || Props.contents is not { spoilsAfterDays: > 0f } contents || parent.Destroyed) return;
        spoilHours = TankRule.NextSpoilHours(spoilHours, parent.AmbientTemperature, HoursPerRareTick, Props.spoilRate);
        if (!TankRule.Spoiled(spoilHours, contents.spoilsAfterDays)) return;
        Messages.Message("PhobosReservoir_Spoiled".Translate(parent.LabelShort.Named("THING"), contents.label.Named("LOAD")).Resolve(), parent,
            MessageTypeDefOf.NegativeEvent);
        Empty();
    }

    public override string CompInspectStringExtra() => Props.contents == null ? ""
        : "PhobosReservoir_Inspect".Translate(Props.contents.LabelCap.Named("LOAD"), Mathf.FloorToInt(amount).Named("AMOUNT"),
            Mathf.FloorToInt(Capacity).Named("CAPACITY"), Props.contents.unitLabel.Named("UNITS")).Resolve();

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref amount, "PhobosReservoirAmount");
        Scribe_Values.Look(ref spoilHours, "PhobosReservoirSpoilHours");
        Scribe_Collections.Look(ref ingredients, "PhobosReservoirIngredients", LookMode.Def);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            ingredients ??= new List<ThingDef>();
            ingredients.RemoveAll(def => def == null);
        }
    }
}

/// <summary>
/// The Billy's paste tap (Framework 0.273.0; vehicle-nutrient-paste.md, NP4): the game's own paste dispenser, so colonists, prisoners and
/// the game's food search use it as one, served from its own paste store rather than hoppers. Each meal is one unit, carrying what the paste
/// was made from. It is pumped by hand, so it needs no power (the Billy's rule: power is a bonus, never a need); the game's dispenser reads a
/// power comp, so it carries one of no draw, kept on.
/// </summary>
public class Building_PhobosPasteTap : Building_NutrientPasteDispenser
{
    CompPhobosReservoir? Store => GetComp<CompPhobosReservoir>();
    Graphic? upgradedGraphic;

    /// <summary>Its own picture while the trailer that set it out has the upgrade its extension names (the bigger paste tank).</summary>
    public override Graphic Graphic
    {
        get
        {
            if (def.GetModExtension<PhobosNodeGraphicExtension>() is not { graphicData: { } data } look || look.node.NullOrEmpty()
                || SetOuts.Owner(this) is not { } owner || !VehicleUpgrades.Unlocked(owner.Vehicle, look.node)) return base.Graphic;
            return upgradedGraphic ??= data.GraphicColoredFor(this);
        }
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        if (powerComp != null) powerComp.PowerOn = true;
    }

    /// <summary>A power net it touches may switch it off; a hand pump never is.</summary>
    public override void TickRare()
    {
        base.TickRare();
        if (powerComp is { PowerOn: false }) powerComp.PowerOn = true;
    }

    public override bool HasEnoughFeedstockInHoppers() => Store is { Amount: >= 1f };

    /// <summary>It has no hoppers: its paste comes in through a tank's hose.</summary>
    public override Building? AdjacentReachableHopper(Pawn reacher) => null;

    public override Thing? FindFeedInAnyHopper() => null;

    public override Thing? TryDispenseFood()
    {
        if (!CanDispenseNow || Store is not { } store || !store.Take(1f)) return null;
        def.building.soundDispense?.PlayOneShot(new TargetInfo(Position, Map));
        Thing meal = store.MealOf(DispensableDef);
        if (Seen.Wants("vehicles.paste-tap-served", this)) Seen.Note("vehicles.paste-tap-served", this, meal.LabelShort);
        return meal;
    }
}

/// <summary>On a set-out piece: another picture while the trailer that set it out has an upgrade (Framework 0.273.0: the Billy's bigger paste tank).</summary>
public class PhobosNodeGraphicExtension : DefModExtension
{
    public string node = "";
    public GraphicData? graphicData;
}
