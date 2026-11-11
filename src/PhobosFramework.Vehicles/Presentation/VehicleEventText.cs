using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// What a vehicle event says and how it is coloured on a graph (Framework 0.84.0): the game's own graph marks, a dot above the graph with
/// the event and how long ago as its tooltip. Reads the log; changes nothing.
/// </summary>
public static class VehicleEventText
{
    /// <summary>Whether an event belongs on the graph of this system's quantity: a stage on its own quantity's graph, anything else on every
    /// graph of its system.</summary>
    public static bool Concerns(VehicleEvent e, string system, string quantity) =>
        e.system == system && (e.kind != VehicleEventKind.Stage || e.detail == quantity);

    public static string Message(VehicleEvent e)
    {
        PhobosVehicleSystemDef? system = DefDatabase<PhobosVehicleSystemDef>.GetNamedSilentFail(e.system);
        switch (e.kind)
        {
            case VehicleEventKind.Stage:
                QuantitySpec? quantity = system?.quantities.Find(q => q.name == e.detail);
                string stage = quantity != null && e.stage >= 0 && e.stage < quantity.stages.Count ? quantity.stages[e.stage].label : "?";
                return "PhobosVehicleSystems_MarkStage".Translate((quantity?.label ?? e.detail).CapitalizeFirst().Named("QUANTITY"), stage.Named("STAGE")).Resolve();
            case VehicleEventKind.Service:
                return "PhobosVehicleSystems_MarkService".Translate((system?.service?.label ?? e.system).CapitalizeFirst().Named("SERVICE")).Resolve();
            case VehicleEventKind.Fault:
                PhobosVehicleConditionDef? condition = DefDatabase<PhobosVehicleConditionDef>.GetNamedSilentFail(e.detail);
                return "PhobosVehicleSystems_MarkFault".Translate((condition?.label ?? e.detail).Named("FAULT")).Resolve();
            default:
                return "PhobosVehicleSystems_MarkJumpStart".Translate().Resolve();
        }
    }

    public static ReportTone Tone(VehicleEvent e)
    {
        switch (e.kind)
        {
            case VehicleEventKind.Stage:
                QuantitySpec? quantity = DefDatabase<PhobosVehicleSystemDef>.GetNamedSilentFail(e.system)?.quantities.Find(q => q.name == e.detail);
                return quantity != null && e.stage >= 0 && e.stage < quantity.stages.Count ? ReportTones.OfStage(quantity.stages[e.stage]) : ReportTone.Caution;
            case VehicleEventKind.Fault:
                return ReportTone.Caution;
            default:
                return ReportTone.Good;
        }
    }

    /// <summary>The event and how long before <paramref name="now"/> it was, for a mark's tooltip.</summary>
    public static string WithWhen(string message, int ticksAgo) =>
        "PhobosVehicleSystems_MarkWhen".Translate(message.Named("EVENT"), Mathf.Max(0, ticksAgo).ToStringTicksToPeriod().Named("AGO")).Resolve();

    /// <summary>The marks for one quantity's graph over its last hours, or null when there are none (the graph then keeps its full height).</summary>
    public static List<CurveMark>? Marks(CompPhobosVehicleSystems comp, string system, string quantity, int hours)
    {
        List<CurveMark>? marks = null;
        int now = Find.TickManager.TicksGame;
        foreach (VehicleEvent e in comp.Events.Events)
        {
            float hoursAgo = (now - e.tick) / (float)GenDate.TicksPerHour;
            if (hoursAgo > hours || !Concerns(e, system, quantity)) continue;
            (marks ??= new List<CurveMark>()).Add(new CurveMark(-hoursAgo, WithWhen(Message(e), now - e.tick), DiagnosisText.ToneColour(Tone(e))));
        }
        return marks;
    }
}
