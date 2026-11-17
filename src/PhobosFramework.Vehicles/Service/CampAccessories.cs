using System.Collections.Generic;
using Phobos.Framework.Environment;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The power outlet (Framework 0.129.0; docs/development/vehicle-accessories.md, Y1): a parked vehicle as a small source on the game's own power
/// net, through the hidden hook-up conduit it already lays beneath itself. What it gives is what the camp's devices ask beyond the net's other
/// sources, so it never fills a colony's batteries; the vehicle's electrics pay for it in amps (CampRule.OutletAmps), and the engine, idling,
/// pays for those in fuel by the rule already in place.
/// </summary>
public static class CampOutlet
{
    /// <summary>The ability the outlet fitting grants, and its load's name.</summary>
    public const string Grant = "PowerOutlet";
    public const string LoadName = "campOutlet";

    /// <summary>
    /// The watts the camp's devices on the meter's net ask beyond its other sources: every consumer switched on or wanting to be (a lamp waiting
    /// for power is counted, or a fresh camp would never light), less what other producers give. Batteries are left out.
    /// </summary>
    public static double CampAsks(CompPowerTrader meter)
    {
        PowerNet? net = meter.PowerNet;
        if (net == null) return 0;
        double draw = 0, others = 0;
        foreach (CompPowerTrader comp in net.powerComps)
        {
            if (comp == meter) continue;
            if (comp.PowerOutput < 0f)
            {
                if (comp.PowerOn || (FlickUtility.WantsToBeOn(comp.parent) && !comp.parent.IsBrokenDown())) draw -= comp.PowerOutput;
            }
            else if (comp.PowerOn) others += comp.PowerOutput;
        }
        return System.Math.Max(0, draw - others);
    }
}

/// <summary>
/// The 12 V fridge (Framework 0.129.0; docs/development/vehicle-accessories.md, Y7): while it runs it keeps the cargo food closest to spoiling cold,
/// up to its load (the owner's choice, 17 Nov 2026: automatic, no dialog). The food stays ordinary cargo; only the temperature the game is told
/// for it changes, through the held-temperature registry, which the game asks before the weather.
/// </summary>
[StaticConstructorOnStartup]
public static class CampFridge
{
    /// <summary>The ability the fridge fitting grants, and its load's name.</summary>
    public const string Grant = "Fridge";
    public const string LoadName = "campFridge";

    static readonly List<Thing> candidates = new List<Thing>();
    static readonly List<(double, double)> figures = new List<(double, double)>();

    static CampFridge() => HeldTemperature.Register<Pawn_InventoryTracker>((tracker, thing) =>
        tracker.pawn is VehiclePawn vehicle && thing != null ? vehicle.GetComp<CompPhobosVehicleSystems>()?.ChilledTemperature(thing) : null);

    /// <summary>Fills the set with the cargo food the fridge keeps cold now (CampRule.FridgeTakes).</summary>
    public static void Choose(VehiclePawn vehicle, float kilograms, HashSet<Thing> into)
    {
        ThingOwner? cargo = vehicle.inventory?.innerContainer;
        if (cargo == null) return;
        candidates.Clear();
        figures.Clear();
        for (int i = 0; i < cargo.Count; i++)
        {
            Thing thing = cargo[i];
            if (!thing.def.IsIngestible || thing.TryGetComp<CompRottable>() is not { } rot || rot.Stage != RotStage.Fresh) continue;
            candidates.Add(thing);
            // Hours left at full rot speed, whatever it is kept at: ranked by the temperature it is kept at now, a chilled stack would last longer,
            // drop out of the fridge at the next look and come back at the one after.
            double hoursLeft = (rot.PropsRot.TicksToRotStart - rot.RotProgress) / GenDate.TicksPerHour;
            figures.Add((hoursLeft, thing.GetStatValue(StatDefOf.Mass) * thing.stackCount));
        }
        foreach (int index in CampRule.FridgeTakes(figures, kilograms)) into.Add(candidates[index]);
        candidates.Clear();
        figures.Clear();
    }
}
