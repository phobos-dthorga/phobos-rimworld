using System.Linq;
using System.Text;
using Phobos.Framework.Interface;
using UnityEngine;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A diagnosis in words, coloured as the tabs colour it (Framework 0.78.0, P1 step one): section titles in the game's heading colour, a
/// part's health by its share, a reading's stage and a fault by how bad it is. The report letter's body: what the letter archive and other
/// mods read, and what the letter shows if the vehicle's Def is gone. Plain words only in the Plain display mode.
/// </summary>
public static class DiagnosisText
{
    const string Indent = "  ";

    public static Color ToneColour(ReportTone tone) => tone switch
    {
        ReportTone.Good => PhobosWidgets.Good,
        ReportTone.Caution => PhobosWidgets.Caution,
        ReportTone.Bad => PhobosWidgets.Bad,
        ReportTone.Off => PhobosWidgets.Off,
        _ => Color.white,
    };

    public static string Colour(string text, ReportTone tone) => tone == ReportTone.None ? text : text.Colorize(ToneColour(tone));

    static string Title(string text) => text.Colorize(ColoredText.TipSectionTitleColor);

    /// <summary>A part's state: its health as a figure or a word, in its health's colour.</summary>
    public static string PartState(DiagnosisPart part, bool figures) =>
        (figures ? part.share.ToStringPercent() : HealthWords.Of(part.share)).Colorize(PhobosWidgets.HealthColour(part.share));

    public static string Body(DiagnosisReport report, bool figures)
    {
        var text = new StringBuilder("PhobosVehicleSystems_DiagnoseIntro".Translate(report.mechanic.Named("MECHANIC"), report.vehicleLabel.Named("VEHICLE")).Resolve());
        if (figures && report.ratedKw > 0f)
        {
            text.Append("\n\n").Append("PhobosVehicleSystems_DiagnosePower".Translate(report.ratedKw.ToString("0").Named("KW")).Resolve());
        }
        foreach (string tune in report.tunes) text.Append("\n").Append("PhobosVehicleSystems_DiagnoseTune".Translate(tune.Named("TUNE")).Resolve());

        text.Append("\n\n").Append(Title("PhobosVehicleSystems_DiagnoseParts".Translate().Resolve()));
        foreach (DiagnosisPart part in report.parts)
        {
            text.Append("\n").Append(Indent).Append(part.label).Append(": ").Append(PartState(part, figures));
            if (part.grade != null) text.Append(", ").Append(part.grade);
            if (part.fitting != null) text.Append(", ").Append(part.fitting);
        }

        text.Append("\n\n").Append(Title("PhobosVehicleSystems_DiagnoseReadings".Translate().Resolve()));
        foreach (IGrouping<string, DiagnosisReading> system in report.readings.Where(r => figures || r.stage != null).GroupBy(r => r.system))
        {
            text.Append("\n").Append(Indent).Append(Title(system.Key));
            foreach (DiagnosisReading reading in system)
            {
                text.Append("\n").Append(Indent).Append(Indent).Append(reading.label).Append(": ");
                string said = figures ? reading.figure + (reading.stage != null ? " (" + reading.stage + ")" : "") : reading.stage!;
                text.Append(Colour(said, reading.tone));
            }
        }

        if (report.faults.Count > 0)
        {
            text.Append("\n\n").Append(Title("PhobosVehicleSystems_DiagnoseConditions".Translate().Resolve()));
            foreach (DiagnosisFault fault in report.faults)
            {
                text.Append("\n").Append(Indent).Append(Colour(fault.label, fault.tone)).Append(" (").Append(fault.part).Append(")");
                if (fault.stage != null) text.Append(": ").Append(fault.stage);
                if (figures) text.Append(", ").Append(fault.severity.ToStringPercent());
            }
        }
        return text.ToString();
    }
}
