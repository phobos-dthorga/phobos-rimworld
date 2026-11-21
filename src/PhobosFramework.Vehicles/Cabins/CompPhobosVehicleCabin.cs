using System.Collections.Generic;
using System.Linq;
using Phobos.Framework;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>One of a cabin's layouts: used when every fitting it names is fitted (none: the cabin as built).</summary>
public class CabinLayoutSpec
{
    public List<PhobosVehicleFittingDef> whenFitted = new();
    /// <summary>Abilities the vehicle's fittings must grant, by name (Framework 0.217.0): "DeckTent" for a deck tent's inside.</summary>
    public List<string> whenGranted = new();
    public List<string> rows = new();

    [Unsaved] CabinPlan? plan;

    public CabinPlan Plan => plan ??= CabinLayoutRule.Parse(rows);
}

/// <summary>A vehicle's walk-in cabin, on its Def (Framework, vehicle cabins, 22 Nov 2026; docs/development/vehicle-cabins.md).</summary>
public class CompProperties_PhobosVehicleCabin : CompProperties
{
    /// <summary>The layouts, most particular first; the first whose fittings are all fitted is used. The last should name none.</summary>
    public List<CabinLayoutSpec> layouts = new();
    /// <summary>
    /// The cabin is a tent on the vehicle's deck (Framework 0.217.0): it exists only while a layout's conditions hold (there is no cabin "as
    /// built"), its air follows the outside, it does not share the passenger seats' limit, and danger outside wakes and empties it.
    /// </summary>
    public bool deckTent;

    public CompProperties_PhobosVehicleCabin() => compClass = typeof(CompPhobosVehicleCabin);

    public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
    {
        foreach (string error in base.ConfigErrors(parentDef)) yield return error;
        if (layouts.Count == 0) yield return "a cabin with no layout";
        if (!deckTent && layouts.Count > 0 && layouts[layouts.Count - 1].whenFitted.Count > 0) yield return "the last cabin layout should name no fittings, as the cabin as built";
        for (int i = 0; i < layouts.Count; i++)
        {
            foreach (string problem in layouts[i].Plan.Problems) yield return $"cabin layout {i + 1} {problem}";
        }
    }
}

/// <summary>
/// The cabin itself: a small pocket map made when the first person goes in and removed once nobody is inside and the vehicle stands on a map.
/// Saves only the map it made. Everything it does is <see cref="VehicleCabins"/>'s.
/// </summary>
public class CompPhobosVehicleCabin : ThingComp
{
    Map? cabin;

    public CompProperties_PhobosVehicleCabin Props => (CompProperties_PhobosVehicleCabin)props;
    public VehiclePawn Vehicle => (VehiclePawn)parent;

    /// <summary>The cabin's map while it is open, else null.</summary>
    public Map? Cabin => cabin is { Disposed: false } ? cabin : null;

    internal void SetCabin(Map? map)
    {
        DeckTents.Note(this, cabin, isOpen: false);
        cabin = map;
        DeckTents.Note(this, cabin, isOpen: true);
    }

    static bool Holds(CabinLayoutSpec layout, VehicleFittings? fittings) =>
        layout.whenFitted.All(def => def != null && fittings != null && fittings.Has(def)) && layout.whenGranted.All(ability => fittings != null && fittings.Grants(ability));

    /// <summary>Whether the vehicle has a cabin to offer now: always for a cabin as built; for a deck tent, only while its tent is fitted.</summary>
    public bool HasLayout
    {
        get
        {
            if (!Props.deckTent) return Props.layouts.Count > 0;
            VehicleFittings? fittings = CompPhobosVehicleSystems.Of(Vehicle)?.Fittings;
            return Props.layouts.Any(layout => Holds(layout, fittings));
        }
    }

    /// <summary>The layout the cabin takes now, by what is fitted.</summary>
    public CabinLayoutSpec Layout
    {
        get
        {
            VehicleFittings? fittings = CompPhobosVehicleSystems.Of(Vehicle)?.Fittings;
            foreach (CabinLayoutSpec layout in Props.layouts)
            {
                if (Holds(layout, fittings)) return layout;
            }
            return Props.layouts[Props.layouts.Count - 1];
        }
    }

    /// <summary>Back on a map (landed from a flight, or loaded): the cabin's way out leads here now. Changes no thing on any map.</summary>
    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (Cabin?.Parent is PocketMapParent pocket && parent.Map != null) pocket.sourceMap = parent.Map;
    }

    public override void PostDestroy(DestroyMode mode, Map? previousMap)
    {
        base.PostDestroy(mode, previousMap);
        VehicleCabins.Evacuate(this, previousMap, parent.Position);
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_References.Look(ref cabin, "PhobosVehicleCabin_Map");
        // An open deck tent is told to the temperature answer again after a load.
        if (Scribe.mode == LoadSaveMode.PostLoadInit) DeckTents.Note(this, Cabin, isOpen: true);
    }
}

[DefOf]
public static class VehicleCabinDefOf
{
    public static MapGeneratorDef PhobosVehicleCabinMap = null!;
    public static JobDef PhobosVehicleSystems_EnterCabin = null!;
    public static JobDef PhobosVehicleSystems_LeaveCabin = null!;
    public static ThingDef PhobosCabinSeat = null!;
    public static ThingDef PhobosCabinLitter = null!;
    public static ThingDef PhobosCabinBed = null!;
    public static ThingDef PhobosCabinTank = null!;
    public static ThingDef PhobosCabinWayOut = null!;

    static VehicleCabinDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(VehicleCabinDefOf));
}
