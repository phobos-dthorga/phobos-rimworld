using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using Phobos.Framework.Debug;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Dev-mode shortcuts for testing fittings, families, tunes and grades (owner, 12 Nov 2026: "Fit now"). Each goes through the same order
/// and completion the mechanic's job uses (VehicleFittings.Order* then Complete), so what it changes is what play would change; only the
/// materials, the haulers, the rig and the mechanic's hours are skipped. Never shown outside dev mode.
/// </summary>
public static class FitNowDebugActions
{
    static IEnumerable<CompPhobosVehicleSystems> Selected() =>
        Find.Selector.SelectedObjects.OfType<VehiclePawn>().Select(v => v.GetComp<CompPhobosVehicleSystems>()).Where(c => c != null && c.Usable);

    /// <summary>Fits one chosen family, fitting, tune or grade on every selected vehicle at once.</summary>
    [DebugAction(DebugCategory.Name, "Vehicle systems: fit now (no materials or work)", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void FitNow()
    {
        CompPhobosVehicleSystems? first = Selected().FirstOrDefault();
        if (first == null)
        {
            Messages.Message("Select one of our vehicles first.", MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        VehicleFittings fittings = first.Fittings;
        var choices = new List<FloatMenuOption>();
        foreach (PhobosVehicleFamilyDef family in fittings.Families())
        {
            PhobosVehicleFamilyDef captured = family;
            choices.Add(DebugChoices.Choice("Family: " + family.LabelCap, () => ForEach(comp => comp.Fittings.OrderFamily(captured))));
        }
        foreach (VehicleComponent part in fittings.ListedParts())
        {
            VehicleComponent capturedPart = part;
            List<PhobosVehicleFittingDef> options = fittings.OptionsFor(part).ToList();
            if (options.Count == 0) continue;
            choices.Add(DebugChoices.Submenu("Part: " + part.props.label.CapitalizeFirst(), () => options.Select(def =>
                DebugChoices.Choice(def.LabelCap, () => ForEach(comp => OrderOnSamePart(comp, capturedPart.props.key, def)))), "Nothing fits this part."));
        }
        foreach (VehicleTuneAxis axis in new[] { VehicleTuneAxis.Engine, VehicleTuneAxis.Gearing })
        {
            if (!fittings.CanTune(axis)) continue;
            VehicleTuneAxis capturedAxis = axis;
            choices.Add(DebugChoices.Submenu("Tune: " + axis, () => VehicleFittings.TunesOf(capturedAxis).Select(tune =>
                DebugChoices.Choice(tune.LabelCap, () => ForEach(comp => comp.Fittings.OrderTune(tune)))), "No tunes for this axis."));
        }
        DebugChoices.Open(choices, "Nothing can be fitted to this vehicle.");
    }

    /// <summary>Finishes every piece of fitting work the selected vehicles are waiting on, as if a mechanic had done it.</summary>
    [DebugAction(DebugCategory.Name, "Vehicle systems: finish waiting fitting work now", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void FinishWaiting()
    {
        int done = 0;
        foreach (CompPhobosVehicleSystems comp in Selected().ToList()) done += Finish(comp);
        Messages.Message(done == 0 ? "No fitting work was waiting on the selected vehicles." : $"Finished {done} piece(s) of fitting work.",
            done == 0 ? MessageTypeDefOf.RejectInput : MessageTypeDefOf.TaskCompletion, historical: false);
    }

    /// <summary>Orders on each selected vehicle, then finishes everything waiting on it.</summary>
    static void ForEach(System.Action<CompPhobosVehicleSystems> order)
    {
        int done = 0;
        foreach (CompPhobosVehicleSystems comp in Selected().ToList())
        {
            order(comp);
            done += Finish(comp);
        }
        if (done == 0) Messages.Message("Nothing was fitted: the order was refused (its message says why) or it is already fitted.", MessageTypeDefOf.RejectInput, historical: false);
    }

    /// <summary>The same part on another selected vehicle of the same design, found by its key.</summary>
    static void OrderOnSamePart(CompPhobosVehicleSystems comp, string partKey, PhobosVehicleFittingDef def)
    {
        VehicleComponent? part = comp.Fittings.ListedParts().FirstOrDefault(p => p.props.key == partKey);
        if (part != null) comp.Fittings.OrderFitting(part, def);
    }

    static int Finish(CompPhobosVehicleSystems comp)
    {
        Pawn worker = comp.Vehicle.Map?.mapPawns.FreeColonistsSpawned.FirstOrDefault() ?? comp.Vehicle;
        int done = 0;
        foreach (FitOrder order in comp.Fittings.Orders.ToList())
        {
            Messages.Message(comp.Fittings.Complete(order, worker), comp.Vehicle, MessageTypeDefOf.TaskCompletion, historical: false);
            done++;
        }
        return done;
    }
}
