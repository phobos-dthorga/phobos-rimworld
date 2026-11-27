using System.Collections.Generic;
using HarmonyLib;
using Phobos.Framework.Persistence;
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
/// Other modders give any building one in XML with its contents and size, and the building must tick rarely (tickerType Rare).
/// <para>On any building (the owner's card of 28 Nov 2026, "Reservoirs"; before, only a trailer's set-out pieces filled from a line, only the
/// paste tap drew from one, and only the paste tap showed its upgrade picture): standing on its own, it lays its load's pipe-net pipe beneath
/// it and fills from a run with storage, as a set-out piece does through its trailer (with Vanilla Expanded Framework); a building whose own
/// refuelable burns the load's item is topped up from it; and <see cref="PhobosNodeGraphicExtension"/> swaps the picture of any set-out piece
/// with the comp. A paste tap serves its meals from it (Building_PhobosPasteTap); a modder's own building class calls Take, TakeMeal or
/// MealOf. Cost: one rare-tick look a store, a few cell lookups.</para>
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

    public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
    {
        foreach (string error in base.ConfigErrors(parentDef)) yield return error;
        if (contents == null) yield return "a reservoir names no contents";
        if (capacity <= 0f) yield return "a reservoir's capacity must be above 0";
        // Its spoiling, filling from a line, refuelling and picture all run on the rare tick.
        if (parentDef.tickerType != TickerType.Rare) yield return "a reservoir's building must tick rarely (tickerType Rare)";
    }

    /// <summary>What it holds on this trailer: more while the trailer has the upgrade named (null: no trailer, the plain size).</summary>
    public float CapacityOn(VehiclePawn? trailer) =>
        node.NullOrEmpty() || capacityWithNode <= 0f || trailer == null || !VehicleUpgrades.Unlocked(trailer, node!) ? capacity : capacityWithNode;
}

public class CompPhobosReservoir : ThingComp
{
    float amount;
    float spoilHours;
    List<ThingDef> ingredients = new();
    /// <summary>The load's pipe laid beneath a store that stands on its own (saved, so it goes with the building).</summary>
    PipeUnderlay underlay = new();
    /// <summary>Whether its pipe was laid since it last spawned: laid once a spawn, as a building's plumbing is. Derived.</summary>
    bool laid;
    /// <summary>The building's own refuelable, read once a spawn. Derived.</summary>
    CompRefuelable? refuelable;
    /// <summary>The upgrade picture it last put in the thing's place, for the swap (PhobosNodeGraphicExtension). Derived.</summary>
    Graphic? upgraded;

    /// <summary>The pipe tie-in (PhobosFramework.Vehicles.Pipes, loaded with Vanilla Expanded Framework); null without it.</summary>
    static IReservoirPipes? pipes;

    /// <summary>Called once by the pipe tie-in's static constructor.</summary>
    public static void UsePipes(IReservoirPipes link) => pipes = link;

    /// <summary>The pipe laid beneath it while it stands on its own, for the pipe tie-in.</summary>
    public PipeUnderlay Underlay => underlay;

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

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        laid = false;
        refuelable = parent.TryGetComp<CompRefuelable>();
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        // Its own pipe goes with it, unless it is being replaced where it stands (a set-out piece never lays one: its trailer does).
        if (mode != DestroyMode.WillReplace) underlay.Clear();
        base.PostDeSpawn(map, mode);
    }

    /// <summary>
    /// Its rare look while it stands (the owner's card of 28 Nov 2026): fills from its load's line when it stands on its own (a set-out piece,
    /// standing or packed, is filled through its trailer, as before), tops up its building's refuelable, and shows its upgrade picture.
    /// </summary>
    void Look()
    {
        if (pipes != null && SetOuts.Owner(parent) == null)
        {
            pipes.Look(this, lay: !laid);
            laid = true;
        }
        TopUpRefuelable();
        UpdateLook();
    }

    /// <summary>The load's item port: what one item of it is, and how many units make one (chemfuel, a paste meal).</summary>
    Port_Items? ItemPort
    {
        get
        {
            if (Props.contents == null) return null;
            foreach (TankPort port in Props.contents.ports)
            {
                if (port is Port_Items { thing: not null, unitsPerItem: > 0f } items) return items;
            }
            return null;
        }
    }

    /// <summary>
    /// Fills the building's own refuelable from the store when it burns the load's item (the owner's card of 28 Nov 2026: any building draws
    /// on it in XML; a chemfuel store beside a generator). Whole items only, up to the refuelable's target, and never while the player has
    /// switched its refuelling off.
    /// </summary>
    void TopUpRefuelable()
    {
        if (refuelable == null || amount < 1f || refuelable.IsFull || !refuelable.allowAutoRefuel || ItemPort is not { } port
            || !refuelable.Props.fuelFilter.Allows(port.thing!)) return;
        int wanted = refuelable.GetFuelCountToFullyRefuel();
        int items = Mathf.Min(wanted, Mathf.FloorToInt(amount / port.unitsPerItem));
        // A refuelable filled all at once (atomicFueling) takes a full load or none.
        if (items <= 0 || (refuelable.Props.atomicFueling && items < wanted) || !Take(items * port.unitsPerItem)) return;
        refuelable.Refuel((float)items);
    }

    /// <summary>The thing's own picture field, so any building class that does not draw its own (Building_PhobosPasteTap does) shows the
    /// upgrade picture without a patch on the game's drawing. Null if the game renames it: the picture then stays the plain one.</summary>
    static readonly AccessTools.FieldRef<Thing, Graphic?>? GraphicField =
        AccessTools.Field(typeof(Thing), "graphicInt") is { } field ? AccessTools.FieldRefAccess<Thing, Graphic?>(field) : null;

    static readonly Dictionary<System.Type, bool> drawsOwnPicture = new();

    static bool DrawsOwnPicture(System.Type type)
    {
        if (drawsOwnPicture.TryGetValue(type, out bool own)) return own;
        System.Reflection.MethodInfo? getter = type.GetProperty(nameof(Thing.Graphic))?.GetGetMethod();
        return drawsOwnPicture[type] = getter != null && getter.DeclaringType != typeof(Thing);
    }

    /// <summary>Puts the upgrade picture in the thing's place while its trailer has the node, and the plain one back when not; the map redraws
    /// it only when it changes. Called on the rare look and when its trailer sets out.</summary>
    internal void UpdateLook()
    {
        if (!parent.Spawned || GraphicField == null || parent.def.GetModExtension<PhobosNodeGraphicExtension>() == null
            || DrawsOwnPicture(parent.GetType())) return;
        Graphic? wanted = PhobosNodeGraphicExtension.UpgradedFor(parent, ref upgraded);
        Graphic? current = GraphicField(parent);
        // Unchanged: the upgrade already shown, or the plain one (null until first drawn, or the def's own).
        if (wanted != null ? current == wanted : current == null || current != upgraded) return;
        GraphicField(parent) = wanted;
        parent.DirtyMapMesh(parent.Map);
    }

    public override void CompTickRare()
    {
        base.CompTickRare();
        if (parent.Spawned) Look();
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
        Scribe_Deep.Look(ref underlay, "PhobosReservoirUnderlay");
        underlay ??= new PipeUnderlay();
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

    /// <summary>Its own picture while the trailer that set it out has the upgrade its extension names (the bigger paste tank), read live as
    /// before; any other building with the comp has it put in place by the comp's look.</summary>
    public override Graphic Graphic => PhobosNodeGraphicExtension.UpgradedFor(this, ref upgradedGraphic) ?? base.Graphic;

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

/// <summary>On a set-out piece: another picture while the trailer that set it out, or holds it packed, has an upgrade (Framework 0.273.0: the
/// Billy's bigger paste tank). Read by the paste tap's own picture and, for any other building carrying a <see cref="CompPhobosReservoir"/>, put
/// in place by that comp (the owner's card of 28 Nov 2026). A building standing on its own has no trailer, so it keeps its plain picture.</summary>
public class PhobosNodeGraphicExtension : DefModExtension
{
    public string node = "";
    public GraphicData? graphicData;

    /// <summary>The picture this piece shows now: the extension's while its trailer has the node, or null for the plain one.</summary>
    public static Graphic? UpgradedFor(Thing piece, ref Graphic? cache)
    {
        if (piece.def.GetModExtension<PhobosNodeGraphicExtension>() is not { graphicData: { } data } look || look.node.NullOrEmpty()
            || SetOuts.Owner(piece) is not { } owner || !VehicleUpgrades.Unlocked(owner.Vehicle, look.node)) return null;
        return cache ??= data.GraphicColoredFor(piece);
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (node.NullOrEmpty()) yield return "PhobosNodeGraphicExtension names no node";
        if (graphicData == null) yield return "PhobosNodeGraphicExtension has no graphicData";
    }
}

/// <summary>
/// The pipe tie-in's hand in a store standing on its own (the owner's card of 28 Nov 2026, "Reservoirs"): lays its load's net pipe beneath
/// it, once a spawn, and fills it from a run of that net with storage at each rare look. Set by PhobosFramework.Vehicles.Pipes.
/// </summary>
public interface IReservoirPipes
{
    void Look(CompPhobosReservoir store, bool lay);
}
