using System.Linq;
using System.Text;
using Phobos.Framework;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A diagnosis at a tuning rig (S1 of docs/development/vehicle-crosslinks-round-two.md; owner, 10 Nov 2026): the player asks for one, a
/// mechanic works at the vehicle within reach of a working rig, and a letter reports everything the gauges do not show: every part's
/// state, grade and fitting, the tunes, every system's readings and every condition. In the Plain display mode it says it in words; in
/// the others, with the figures. Owned by the systems comp; only the request is saved.
/// </summary>
public sealed class VehicleDiagnostics
{
    readonly CompPhobosVehicleSystems comp;
    bool requested;

    public VehicleDiagnostics(CompPhobosVehicleSystems comp)
    {
        this.comp = comp;
    }

    public void ExposeData() => Scribe_Values.Look(ref requested, "PhobosVehicleSystems_DiagnoseRequested", false);

    public bool Requested => requested;

    public void SetRequested(bool on) => requested = on;

    /// <summary>What a requested diagnosis waits for, or null when a mechanic can do it now.</summary>
    public string? WaitingFor() => comp.Fittings.NearRig() ? null : "PhobosVehicleSystems_DiagnoseNeedsRig".Translate(comp.Vehicle.LabelShort.Named("VEHICLE")).Resolve();

    /// <summary>Asked for, and a mechanic could do it now.</summary>
    public bool Ready => requested && WaitingFor() == null;

    /// <summary>The mechanic has finished: the report goes out as a letter, and the request is done.</summary>
    public bool Complete(Pawn mechanic)
    {
        if (!requested) return false;
        requested = false;
        Find.LetterStack.ReceiveLetter("PhobosVehicleSystems_DiagnoseLetterLabel".Translate(comp.Vehicle.LabelShort.Named("VEHICLE")),
            Report(mechanic), LetterDefOf.NeutralEvent, comp.Vehicle);
        return true;
    }

    string Report(Pawn mechanic)
    {
        bool figures = VehicleSystemsMod.Settings.displayMode != DisplayMode.Plain;
        VehiclePawn vehicle = comp.Vehicle;
        var text = new StringBuilder("PhobosVehicleSystems_DiagnoseIntro".Translate(mechanic.LabelShort.Named("MECHANIC"), vehicle.LabelShort.Named("VEHICLE")).Resolve());
        if (figures && comp.Powertrain is { } train && train.Spec.RatedPowerKw > 0)
        {
            text.Append("\n\n").Append("PhobosVehicleSystems_DiagnosePower".Translate(train.Spec.RatedPowerKw.ToString("0").Named("KW")).Resolve());
        }
        foreach (VehicleTuneAxis axis in new[] { VehicleTuneAxis.Engine, VehicleTuneAxis.Gearing })
        {
            if (comp.Fittings.Current(axis) is { isDefault: false } tune) text.Append("\n").Append("PhobosVehicleSystems_DiagnoseTune".Translate(tune.label.Named("TUNE")).Resolve());
        }
        text.Append("\n\n").Append("PhobosVehicleSystems_DiagnoseParts".Translate().Resolve());
        foreach (VehicleComponent part in vehicle.statHandler.components.OrderBy(p => p.HealthPercent))
        {
            string state = figures ? part.HealthPercent.ToStringPercent() : HealthWords.Of(part.HealthPercent);
            text.Append("\n  ").Append((part.props.label ?? part.props.key).CapitalizeFirst()).Append(": ").Append(state);
            PhobosVehiclePartGradeDef grade = comp.Fittings.GradeOf(part.props.key);
            if (!grade.isDefault) text.Append(", ").Append(grade.label);
            if (comp.Fittings.FittedOn(part.props.key) is { } fitting) text.Append(", ").Append(fitting.label);
        }
        text.Append("\n\n").Append("PhobosVehicleSystems_DiagnoseReadings".Translate().Resolve());
        foreach (SystemState state in comp.States)
        {
            if (!comp.IsEnabled(state)) continue;
            for (int q = 0; q < state.values.Length; q++)
            {
                QuantitySpec quantity = state.Def.quantities[q];
                string? stage = state.CurrentStage(q)?.label;
                if (!figures && stage == null) continue;
                text.Append("\n  ").Append(quantity.label.CapitalizeFirst()).Append(": ");
                text.Append(figures ? InspectText.Format(quantity, state.values[q]) + (stage != null ? " (" + stage + ")" : "") : stage);
            }
        }
        if (comp.Conditions.All.Any())
        {
            text.Append("\n\n").Append("PhobosVehicleSystems_DiagnoseConditions".Translate().Resolve());
            foreach (VehicleCondition condition in comp.Conditions.All)
            {
                VehicleComponent? part = vehicle.statHandler.GetComponent(condition.partKey);
                text.Append("\n  ").Append(condition.def!.LabelCap.Resolve()).Append(" (").Append(part?.props.label ?? condition.partKey).Append(")");
                if (condition.CurrentStage?.label is { } stage) text.Append(": ").Append(stage);
                if (figures) text.Append(", ").Append(condition.severity.ToStringPercent());
            }
        }
        return text.ToString();
    }
}
