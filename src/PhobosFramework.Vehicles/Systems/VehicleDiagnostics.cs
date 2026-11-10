using System.Linq;
using Phobos.Framework;
using Phobos.Framework.Rules;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A diagnosis at a tuning rig (S1 of docs/development/vehicle-crosslinks-round-two.md; owner, 10 Nov 2026): the player asks for one, a
/// mechanic works at the vehicle within reach of a working rig, and a letter reports everything the gauges do not show: every part's
/// state, grade and fitting, the tunes, every system's readings, every condition and the last day of the gauges. The finding is taken
/// once as a <see cref="DiagnosisReport"/> and saved with the letter, which opens as a report window (0.78.0,
/// docs/development/vehicle-presentation-design.md, P1). Owned by the systems comp; only the request is saved here.
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

    /// <summary>The mechanic has finished: the finding goes out as a report letter, and the request is done.</summary>
    public bool Complete(Pawn mechanic)
    {
        if (!requested) return false;
        requested = false;
        DiagnosisReport report = Take(mechanic);
        Letter_PhobosReport.Send(report, "PhobosVehicleSystems_DiagnoseLetterLabel".Translate(comp.Vehicle.LabelShort.Named("VEHICLE")),
            DiagnosisText.Body(report, VehicleSystemsMod.Settings.displayMode != DisplayMode.Plain), comp.Vehicle);
        return true;
    }

    /// <summary>
    /// Everything the mechanic found, as it is now. Readings: every shown one, and a hidden one (a heater flag, the fuel-gel switch, the
    /// ground under the wheels) only when it has a stage to name (0.77.1: the owner's first report showed "Fuel: 0%" for the gel switch).
    /// The last day: every dashboard gauge's history, temperatures on one graph and levels (as the gauge reads them) on the other.
    /// </summary>
    public DiagnosisReport Take(Pawn mechanic)
    {
        VehiclePawn vehicle = comp.Vehicle;
        var report = new DiagnosisReport
        {
            vehicleDef = vehicle.VehicleDef,
            vehicleLabel = vehicle.LabelShort,
            mechanic = mechanic.LabelShort,
            ticksAbs = Find.TickManager.TicksAbs,
            longLat = vehicle.Tile.Valid ? Find.WorldGrid.LongLatOf(vehicle.Tile) : default,
        };
        if (comp.Powertrain is { } train && train.Spec.RatedPowerKw > 0) report.ratedKw = (float)train.Spec.RatedPowerKw;
        foreach (VehicleTuneAxis axis in new[] { VehicleTuneAxis.Engine, VehicleTuneAxis.Gearing })
        {
            if (comp.Fittings.Current(axis) is { isDefault: false } tune) report.tunes.Add(tune.label);
        }
        foreach (VehicleComponent part in vehicle.statHandler.components.OrderBy(p => p.HealthPercent))
        {
            PhobosVehiclePartGradeDef grade = comp.Fittings.GradeOf(part.props.key);
            report.parts.Add(new DiagnosisPart
            {
                key = part.props.key,
                label = (part.props.label ?? part.props.key).CapitalizeFirst(),
                share = part.HealthPercent,
                grade = grade.isDefault ? null : grade.label,
                fitting = comp.Fittings.FittedOn(part.props.key)?.label,
            });
        }
        foreach (SystemState state in comp.States)
        {
            if (!comp.IsEnabled(state)) continue;
            string system = state.Def.label.NullOrEmpty() ? state.Def.defName : state.Def.label.CapitalizeFirst();
            for (int q = 0; q < state.values.Length; q++)
            {
                QuantitySpec quantity = state.Def.quantities[q];
                StageSpec? stage = state.CurrentStage(q);
                if (stage == null && quantity.hidden) continue;
                GaugeReading gauge = GaugeReading.For(state, q);
                report.readings.Add(new DiagnosisReading
                {
                    system = system,
                    label = gauge.Label,
                    figure = gauge.Figure,
                    stage = stage?.label,
                    tone = ReportTones.OfStage(stage),
                    fill = gauge.Fill,
                    telltale = quantity.telltale,
                });
                if (quantity.showOnDashboard && comp.History.For(state, q) is { Count: >= 2 } ring) report.series.Add(Series(state, q, ring, gauge.Label));
            }
        }
        foreach (VehicleCondition condition in comp.Conditions.All)
        {
            VehicleComponent? part = vehicle.statHandler.GetComponent(condition.partKey);
            StageSpec? stage = condition.CurrentStage;
            report.faults.Add(new DiagnosisFault
            {
                label = condition.def!.LabelCap.Resolve(),
                part = part?.props.label ?? condition.partKey,
                stage = stage?.label,
                tone = stage?.Severe == true ? ReportTone.Bad : ReportTone.Caution,
                severity = condition.severity,
            });
        }
        return report;
    }

    /// <summary>A gauge's last day: a temperature as recorded, anything else as the share of its gauge, what is left where the gauge shows that.</summary>
    static DiagnosisSeries Series(SystemState state, int q, HistoryRing ring, string label)
    {
        QuantitySpec quantity = state.Def.quantities[q];
        bool temperature = quantity.unit == QuantityUnit.Celsius;
        bool remaining = !quantity.remainingLabel.NullOrEmpty();
        float min = (float)state.min[q], max = (float)state.max[q];
        var copy = new HistoryRing(ring.Capacity);
        for (int i = 0; i < ring.Count; i++)
        {
            float value = ring[i];
            if (!temperature)
            {
                float share = UnityEngine.Mathf.Clamp01((value - min) / UnityEngine.Mathf.Max(1e-6f, max - min));
                value = 100f * (remaining ? 1f - share : share);
            }
            copy.Push(value);
        }
        return new DiagnosisSeries
        {
            label = label,
            temperature = temperature,
            min = temperature ? quantity.min : 0f,
            max = temperature ? quantity.max : 100f,
            capacity = ring.Capacity,
            ring = copy,
        };
    }
}
