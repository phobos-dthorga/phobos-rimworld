using System.Linq;
using HarmonyLib;
using Rimefeller;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems.Oil;

/// <summary>Patches this assembly: it loads only with Rimefeller active.</summary>
public class VehiclesRimefellerMod : Mod
{
    public VehiclesRimefellerMod(ModContentPack content) : base(content) => new Harmony("PhobosDthorga.Framework").PatchAll(typeof(VehiclesRimefellerMod).Assembly);
}

/// <summary>Counts a set-up survey truck as the powered deep scanner a deep well needs, so oil can be prospected in the field. Rimefeller asks only about colony buildings. A postfix; only ever turns false to true.</summary>
[HarmonyPatch(typeof(PlaceWorker_DeepWellHead), nameof(PlaceWorker_DeepWellHead.HasScanner))]
public static class SurveyTruckCountsForDeepWells
{
    public static void Postfix(ref bool __result)
    {
        if (!__result && Find.CurrentMap != null) __result = CompPhobosSurveyTruck.AnySetUp(Find.CurrentMap);
    }
}

/// <summary>
/// Sinks a Rimefeller oil well at the rig's hole (docs/development/vehicle-drill-rig.md): the derrick's own steel and work, read from its Def, by the same stat its supervisor uses, and its own gates (a console for shallow oil, a scanner for deep).
/// It leaves the well head Rimefeller's derrick would make and pumps nothing: a derrick built over the head and Rimefeller's pipes do that, so its pressure and quakes stay Rimefeller's.
/// </summary>
public class RigJob_OilWell : RigSideJob
{
    public ThingDef? shallowDerrick;
    public ThingDef? deepDerrick;

    static MapComponent_Rimefeller? Oil(Map map) => map.GetComponent<MapComponent_Rimefeller>();

    /// <summary>Whether the hole is over oil, which layer, and whether a head is already there.</summary>
    static bool OverOil(CompPhobosDrillRig rig, out bool deep)
    {
        deep = false;
        Map map = rig.Vehicle.Map;
        MapComponent_Rimefeller? oil = Oil(map);
        if (oil == null) return false;
        IntVec3 hole = rig.Hole;
        if (!hole.InBounds(map) || hole.GetThingList(map).OfType<Building_WellHead>().Any()) return false;
        if (oil.OilGrid.GetFieldAt(hole) != null) return true;
        deep = true;
        return oil.DeepOilGrid.GetFieldAt(hole) != null;
    }

    public override bool Wanted(CompPhobosDrillRig rig) => rig.Vehicle.GetStatValue(RigDefOf.Phobos_WellKit) > 0f && OverOil(rig, out _);

    static bool HasConsole(Map map) => Oil(map)?.Consoles.Any(x => x.WorkingNow) == true;

    static bool HasScanner(Map map) => map.deepResourceGrid.AnyActiveDeepScannersOnMap();

    CompProperties_OilDerrick? Derrick(bool deep) => (deep ? deepDerrick : shallowDerrick)?.GetCompProperties<CompProperties_OilDerrick>();

    public override string? WhyNot(CompPhobosDrillRig rig)
    {
        OverOil(rig, out bool deep);
        Map map = rig.Vehicle.Map;
        if (!deep && !HasConsole(map)) return "PhobosVehicleSystems_WellNoConsole".Translate().Resolve();
        if (deep && !HasScanner(map)) return "PhobosVehicleSystems_WellNoScanner".Translate().Resolve();
        return rig.Vehicle.inventory.innerContainer.TotalStackCountOfDef(ThingDefOf.Steel) > 0 ? null : "PhobosVehicleSystems_WellNoSteel".Translate().Resolve();
    }

    public override void Worked(CompPhobosDrillRig rig, Pawn worker, int delta)
    {
        OverOil(rig, out bool deep);
        CompProperties_OilDerrick? derrick = Derrick(deep);
        if (derrick == null) return;
        rig.SideProgress += worker.GetStatValue(StatDefOf.MiningSpeed) * delta;
        if (rig.SideProgress < derrick.TicksPerSteel) return;
        rig.SideProgress = 0f;
        ThingOwner cargo = rig.Vehicle.inventory.innerContainer;
        Thing? steel = cargo.FirstOrDefault(t => t.def == ThingDefOf.Steel);
        if (steel == null) return;
        steel.SplitOff(1).Destroy();
        rig.SideSteps++;
        if (rig.SideSteps >= derrick.SteelCost) Finish(rig, deep);
    }

    static void Finish(CompPhobosDrillRig rig, bool deep)
    {
        rig.SideSteps = 0;
        var well = (Building_WellHead)ThingMaker.MakeThing(deep ? DubDef.DeepOilWellHead : DubDef.OilWellHead);
        well.Deep = deep;
        GenPlace.TryPlaceThing(well, rig.Hole, rig.Vehicle.Map, ThingPlaceMode.Direct);
        rig.Tell("PhobosVehicleSystems_WellDone".Translate(rig.Vehicle.LabelShort.Named("VEHICLE")).Resolve(), MessageTypeDefOf.TaskCompletion);
        rig.Reread();
    }

    /// <summary>The steel placed, and the piece in hand, against the steel the well needs.</summary>
    public override float? Progress(CompPhobosDrillRig rig)
    {
        OverOil(rig, out bool deep);
        CompProperties_OilDerrick? derrick = Derrick(deep);
        if (derrick == null || derrick.SteelCost <= 0) return null;
        float piece = derrick.TicksPerSteel > 0 ? rig.SideProgress / derrick.TicksPerSteel : 0f;
        return (rig.SideSteps + piece) / derrick.SteelCost;
    }

    public override string? InspectLine(CompPhobosDrillRig rig)
    {
        OverOil(rig, out bool deep);
        CompProperties_OilDerrick? derrick = Derrick(deep);
        return derrick == null ? null : "PhobosVehicleSystems_WellProgress".Translate(rig.SideSteps.Named("STEPS"), derrick.SteelCost.Named("TOTAL")).Resolve();
    }
}
