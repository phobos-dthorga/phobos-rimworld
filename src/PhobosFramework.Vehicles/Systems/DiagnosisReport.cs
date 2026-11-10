using System.Collections.Generic;
using Phobos.Framework.Persistence;
using Phobos.Framework.Rules;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>How a report row reads: nothing to say, sound, worth a look, or bad; the window and the letter colour it the same.</summary>
public enum ReportTone
{
    None,
    Good,
    Caution,
    Bad,
    Off,
}

public static class ReportTones
{
    /// <summary>A stage's tone, by the dashboard's rule: none without a stage, good when it is not notable, bad when it refuses or harms.</summary>
    public static ReportTone OfStage(StageSpec? stage) => stage == null ? ReportTone.None : !stage.notable ? ReportTone.Good : stage.Severe ? ReportTone.Bad : ReportTone.Caution;
}

/// <summary>One part as it was found: its key (to light its cells on the damage sheet), its label, its share of health, grade and fitting.</summary>
public sealed class DiagnosisPart : IExposable
{
    public string key = "";
    public string label = "";
    public float share;
    public string? grade;
    public string? fitting;

    public void ExposeData()
    {
        Scribe_Values.Look(ref key, "key", "");
        Scribe_Values.Look(ref label, "label", "");
        Scribe_Values.Look(ref share, "share");
        Scribe_Values.Look(ref grade, "grade");
        Scribe_Values.Look(ref fitting, "fitting");
    }
}

/// <summary>One reading as it was found: its system, label, figure, stage and tone, a gauge's fill, and the telltale picture where it has
/// one.</summary>
public sealed class DiagnosisReading : IExposable
{
    public string system = "";
    public string label = "";
    public string figure = "";
    public string? stage;
    public ReportTone tone;
    public float fill;
    public string? telltale;

    public void ExposeData()
    {
        Scribe_Values.Look(ref system, "system", "");
        Scribe_Values.Look(ref label, "label", "");
        Scribe_Values.Look(ref figure, "figure", "");
        Scribe_Values.Look(ref stage, "stage");
        Scribe_Values.Look(ref tone, "tone");
        Scribe_Values.Look(ref fill, "fill");
        Scribe_Values.Look(ref telltale, "telltale");
    }
}

/// <summary>One fault as it was found: the condition, its part, its stage, its tone and its severity.</summary>
public sealed class DiagnosisFault : IExposable
{
    public string label = "";
    public string part = "";
    public string? stage;
    public ReportTone tone;
    public float severity;

    public void ExposeData()
    {
        Scribe_Values.Look(ref label, "label", "");
        Scribe_Values.Look(ref part, "part", "");
        Scribe_Values.Look(ref stage, "stage");
        Scribe_Values.Look(ref tone, "tone");
        Scribe_Values.Look(ref severity, "severity");
    }
}

/// <summary>
/// One line of the last day on a graph of temperatures (degrees, as recorded) or of levels (percent of the gauge, what is left for a
/// quantity that counts what is used). Saved in the history's compact form between its limits.
/// </summary>
public sealed class DiagnosisSeries : IExposable
{
    public string label = "";
    public bool temperature;
    public float min;
    public float max = 100f;
    public int capacity = QuantityHistory.Capacity;
    public HistoryRing? ring;
    [Unsaved] List<float>? samples;

    /// <summary>The samples, oldest first, read out of the ring once.</summary>
    public IReadOnlyList<float> Samples
    {
        get
        {
            if (samples == null)
            {
                samples = new List<float>();
                if (ring != null)
                {
                    for (int i = 0; i < ring.Count; i++) samples.Add(ring[i]);
                }
            }
            return samples;
        }
    }

    public void ExposeData()
    {
        // The limits and capacity come first: loading the ring needs them.
        Scribe_Values.Look(ref label, "label", "");
        Scribe_Values.Look(ref temperature, "temperature");
        Scribe_Values.Look(ref min, "min");
        Scribe_Values.Look(ref max, "max", 100f);
        Scribe_Values.Look(ref capacity, "capacity", QuantityHistory.Capacity);
        ScribeHistory.Look(ref ring, "samples", capacity, min, max);
    }
}

/// <summary>
/// A diagnosis as it was on the day (Framework 0.78.0; docs/development/vehicle-presentation-design.md, P1): everything the letter and the
/// report window show, taken once when the mechanic finishes and saved with the letter, so opening it again later shows what was true
/// then. Plain data only: the window draws it and decides nothing.
/// </summary>
public sealed class DiagnosisReport : IExposable
{
    public VehicleDef? vehicleDef;
    public string vehicleLabel = "";
    public string mechanic = "";
    public long ticksAbs;
    public Vector2 longLat;
    /// <summary>Below 0 when the vehicle has no engine model.</summary>
    public float ratedKw = -1f;
    public List<string> tunes = new();
    public List<DiagnosisPart> parts = new();
    public List<DiagnosisReading> readings = new();
    public List<DiagnosisFault> faults = new();
    public List<DiagnosisSeries> series = new();

    public void ExposeData()
    {
        Scribe_Defs.Look(ref vehicleDef, "vehicleDef");
        Scribe_Values.Look(ref vehicleLabel, "vehicleLabel", "");
        Scribe_Values.Look(ref mechanic, "mechanic", "");
        Scribe_Values.Look(ref ticksAbs, "ticksAbs");
        Scribe_Values.Look(ref longLat, "longLat");
        Scribe_Values.Look(ref ratedKw, "ratedKw", -1f);
        Scribe_Collections.Look(ref tunes, "tunes", LookMode.Value);
        Scribe_Collections.Look(ref parts, "parts", LookMode.Deep);
        Scribe_Collections.Look(ref readings, "readings", LookMode.Deep);
        Scribe_Collections.Look(ref faults, "faults", LookMode.Deep);
        Scribe_Collections.Look(ref series, "series", LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            tunes ??= new List<string>();
            parts ??= new List<DiagnosisPart>();
            readings ??= new List<DiagnosisReading>();
            faults ??= new List<DiagnosisFault>();
            series ??= new List<DiagnosisSeries>();
        }
    }
}
