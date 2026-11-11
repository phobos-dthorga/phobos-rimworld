using System.Collections.Generic;
using System.Linq;
using Phobos.Framework;
using Phobos.Framework.Interface;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A diagnosis as a report (Framework 0.78.0; docs/development/vehicle-presentation-design.md, P1 step two, P6's last-day graph and P7a's
/// damage sheet): on the left the vehicle's picture with its hurt parts tinted, who looked and when, its power and tunes; on the right the
/// parts worst first, the readings by system, the faults and, for those who read figures, the last day of its gauges. Hovering a part's row
/// outlines its cells on the picture, and hovering the picture lights the part's row. Draws the saved finding only; its buttons delegate to
/// the letter.
/// </summary>
public class Dialog_PhobosDiagnosis : Window
{
    const float LeftWidth = 320f;
    const float Gap = 12f;
    const float Row = 24f;
    const float SystemRow = 22f;
    const float GraphHeight = 150f;
    const float IconSize = 18f;
    const float ButtonHeight = 32f;

    /// <summary>Line colours on the graphs, in turn (authored: told apart on the game's dark background).</summary>
    static readonly Color[] LineColours =
    {
        new(0.95f, 0.55f, 0.25f), new(0.40f, 0.72f, 0.98f), new(0.95f, 0.85f, 0.35f), new(0.55f, 0.85f, 0.45f), new(0.80f, 0.55f, 0.95f), new(0.85f, 0.85f, 0.85f),
    };

    readonly Letter_PhobosReport letter;
    readonly DiagnosisReport report;
    readonly List<DamageSheet.SheetPart> sheetParts;
    readonly List<PhobosWidgets.GraphSeries> temperatures = new();
    readonly List<PhobosWidgets.GraphSeries> levels = new();
    readonly List<CurveMark> temperatureMarks = new();
    readonly List<CurveMark> levelMarks = new();
    readonly float temperatureMin;
    readonly float temperatureMax;
    readonly string date;
    Vector2 scroll;
    float viewHeight = 400f;
    /// <summary>The readings by system, with each system's worst tone as its card's colour: put together once, the report does not change.</summary>
    readonly List<(string system, List<DiagnosisReading> readings, Color colour)> systemCards = new();
    Color? partsColour;
    Color? faultsColour;
    List<string>? leftLines;
    bool leftFigures;
    string? litPart;
    string? hoveredOnSheet;
    /// <summary>The part clicked on the sheet: its row stays marked and is scrolled into view (Framework 0.84.0). A second click clears it.</summary>
    string? picked;
    bool scrollToPicked;

    public Dialog_PhobosDiagnosis(Letter_PhobosReport letter, DiagnosisReport report)
    {
        this.letter = letter;
        this.report = report;
        doCloseX = true;
        draggable = true;
        closeOnClickedOutside = true;
        absorbInputAroundWindow = false;
        sheetParts = report.parts.Select(p => new DamageSheet.SheetPart(p.key, p.label, p.share)).ToList();
        date = GenDate.DateFullStringAt(report.ticksAbs, report.longLat);

        // The graphs are built once: temperatures in the player's unit, on a scale padded to the nearest ten around what was recorded.
        int temperatureLine = 0, levelLine = 0;
        float low = float.MaxValue, high = float.MinValue;
        foreach (DiagnosisSeries series in report.series)
        {
            if (series.Samples.Count < 2) continue;
            if (series.temperature)
            {
                List<float> shown = series.Samples.Select(c => GenTemperature.CelsiusTo(c, Prefs.TemperatureMode)).ToList();
                low = Mathf.Min(low, shown.Min());
                high = Mathf.Max(high, shown.Max());
                temperatures.Add(new PhobosWidgets.GraphSeries(shown, series.label, LineColours[temperatureLine++ % LineColours.Length]));
            }
            else
            {
                levels.Add(new PhobosWidgets.GraphSeries(series.Samples, series.label, LineColours[levelLine++ % LineColours.Length]));
            }
        }
        foreach (IGrouping<string, DiagnosisReading> group in report.readings.GroupBy(r => r.system))
        {
            List<DiagnosisReading> readings = group.ToList();
            Color colour = readings.Any(r => r.tone == ReportTone.Bad) ? PhobosWidgets.Bad : readings.Any(r => r.tone == ReportTone.Caution) ? PhobosWidgets.Caution : PhobosWidgets.Good;
            systemCards.Add((group.Key, readings, colour));
        }
        if (report.parts.Count > 0) partsColour = PhobosWidgets.HealthColour(report.parts.Min(part => part.share));
        if (report.faults.Count > 0) faultsColour = report.faults.Any(f => f.tone == ReportTone.Bad) ? PhobosWidgets.Bad : PhobosWidgets.Caution;
        foreach (DiagnosisMark mark in report.marks)
        {
            (mark.temperature ? temperatureMarks : levelMarks).Add(new CurveMark(-mark.hoursAgo, mark.message, DiagnosisText.ToneColour(mark.tone)));
        }
        temperatureMin = temperatures.Count > 0 ? Mathf.Floor(low / 10f) * 10f - 10f : 0f;
        temperatureMax = temperatures.Count > 0 ? Mathf.Ceil(high / 10f) * 10f + 10f : 100f;
    }

    public override Vector2 InitialSize => new(900f, 620f);

    static bool Figures => VehicleSystemsMod.Settings.displayMode != DisplayMode.Plain;

    public override void DoWindowContents(Rect inRect)
    {
        // Guarded: a throw is logged once and the window rests, where the game would log it every frame (UiGuard).
        if (UiGuard.Resting(GuardKey)) { UiGuard.Fallback(inRect); return; }
        try { Contents(inRect); }
        catch (System.Exception e) { UiGuard.Report(GuardKey, e); }
    }

    const string GuardKey = "The diagnosis report";

    void Contents(Rect inRect)
    {
        using (new TextBlock(GameFont.Medium))
        {
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width - 30f, 34f), letter.Label);
        }
        float top = inRect.y + 40f;
        var left = new Rect(inRect.x, top, LeftWidth, inRect.yMax - top);
        var right = new Rect(left.xMax + Gap, top, inRect.width - LeftWidth - Gap, inRect.yMax - top);
        DrawLeft(left);
        DrawRight(right);
    }

    void DrawLeft(Rect rect)
    {
        var sheet = new Rect(rect.x, rect.y, rect.width, rect.width);
        hoveredOnSheet = DamageSheet.Draw(sheet, report.vehicleDef!, sheetParts, litPart ?? picked);
        if (Widgets.ButtonInvisible(sheet, doMouseoverSound: false))
        {
            picked = hoveredOnSheet == picked ? null : hoveredOnSheet;
            scrollToPicked = picked != null;
        }
        float y = sheet.yMax + 8f;

        // The report does not change, so its lines are put together once for each display mode, not every frame.
        if (leftLines == null || leftFigures != Figures)
        {
            leftFigures = Figures;
            leftLines = new List<string>
            {
                "PhobosVehicleSystems_ReportLookedOver".Translate(report.mechanic.Named("MECHANIC"), date.Named("DATE")).Resolve(),
            };
            if (Figures && report.ratedKw > 0f) leftLines.Add("PhobosVehicleSystems_DiagnosePower".Translate(report.ratedKw.ToString("0").Named("KW")).Resolve());
            foreach (string tune in report.tunes) leftLines.Add("PhobosVehicleSystems_DiagnoseTune".Translate(tune.Named("TUNE")).Resolve());
        }
        List<string> lines = leftLines;
        float buttons = rect.yMax - ButtonHeight;
        using (new TextBlock(GameFont.Small))
        {
            foreach (string line in lines)
            {
                float height = Text.CalcHeight(line, rect.width);
                if (y + height > buttons - 4f) break;
                Widgets.Label(new Rect(rect.x, y, rect.width, height), line);
                y += height + 2f;
            }
        }

        float half = (rect.width - 6f) / 2f;
        var jump = new Rect(rect.x, buttons, half, ButtonHeight);
        if (letter.CanJump)
        {
            if (Widgets.ButtonText(jump, "PhobosVehicleSystems_ReportJumpTo".Translate()))
            {
                letter.JumpTo();
                Close();
            }
        }
        else
        {
            // Disabled, with its reason: the vehicle is gone, or on no map the camera can reach.
            Widgets.ButtonText(jump, "PhobosVehicleSystems_ReportJumpTo".Translate(), active: false);
            TooltipHandler.TipRegion(jump, "PhobosVehicleSystems_ReportCannotJump".Translate(report.vehicleLabel.Named("VEHICLE")));
        }
        if (Widgets.ButtonText(new Rect(jump.xMax + 6f, buttons, half, ButtonHeight), "PhobosVehicleSystems_ReportClose".Translate()))
        {
            letter.Dismiss();
            Close();
        }
        TooltipHandler.TipRegion(new Rect(jump.xMax + 6f, buttons, half, ButtonHeight), "PhobosVehicleSystems_ReportCloseDesc".Translate());
    }

    void DrawRight(Rect outer)
    {
        bool figures = Figures;
        var view = new Rect(0f, 0f, outer.width - 18f, viewHeight);
        Widgets.BeginScrollView(outer, ref scroll, view);
        float y = 0f;
        string? lit = null;

        float partsTop = y;
        PhobosWidgets.CardHeader(ref y, 0f, view.width, "PhobosVehicleSystems_DiagnoseParts".Translate(), partsColour);
        foreach (DiagnosisPart part in report.parts)
        {
            var row = new Rect(0f, y, view.width, Row);
            if (part.key == picked) Widgets.DrawHighlightSelected(row);
            if (part.key == hoveredOnSheet) Widgets.DrawHighlight(row);
            else Widgets.DrawHighlightIfMouseover(row);
            if (scrollToPicked && part.key == picked)
            {
                scroll.y = Mathf.Max(0f, y - Row * 2f);
                scrollToPicked = false;
            }
            if (Mouse.IsOver(row)) lit = part.key;
            float labelWidth = view.width * 0.34f;
            using (new TextBlock(GameFont.Small, TextAnchor.MiddleLeft)) Widgets.Label(new Rect(4f, y, labelWidth - 4f, Row), part.label);
            var gauge = new Rect(labelWidth, y + 4f, view.width * 0.30f, Row - 8f);
            PhobosWidgets.Gauge(gauge, part.share, PhobosWidgets.HealthColour(part.share), figure: figures ? part.share.ToStringPercent() : HealthWords.Of(part.share));
            string notes = string.Join(", ", new[] { part.grade, part.fitting }.Where(s => s != null));
            if (notes.Length > 0)
            {
                using (new TextBlock(GameFont.Tiny, TextAnchor.MiddleLeft)) Widgets.Label(new Rect(gauge.xMax + 8f, y, view.width - gauge.xMax - 8f, Row), notes);
            }
            y += Row;
        }
        PhobosWidgets.CardEnd(partsTop, ref y, 0f, view.width);

        // A card a system, as the Systems tab has (Framework 0.94.0).
        foreach ((string system, List<DiagnosisReading> readings, Color systemColour) in systemCards)
        {
            float systemTop = y;
            PhobosWidgets.CardHeader(ref y, 0f, view.width, system, systemColour);
            foreach (DiagnosisReading reading in readings)
            {
                var row = new Rect(0f, y, view.width, Row);
                Widgets.DrawHighlightIfMouseover(row);
                Color colour = reading.tone == ReportTone.None ? PhobosWidgets.Good : DiagnosisText.ToneColour(reading.tone);
                if (SystemIcons.IfExists(reading.telltale) is { } icon)
                {
                    GUI.color = reading.tone is ReportTone.Caution or ReportTone.Bad ? colour : PhobosWidgets.Off;
                    GUI.DrawTexture(new Rect(4f, y + (Row - IconSize) / 2f, IconSize, IconSize), icon);
                    GUI.color = Color.white;
                }
                float labelX = 4f + IconSize + 6f;
                float labelWidth = view.width * 0.34f - labelX;
                using (new TextBlock(GameFont.Small, TextAnchor.MiddleLeft)) Widgets.Label(new Rect(labelX, y, labelWidth, Row), reading.label);
                var gauge = new Rect(view.width * 0.34f, y + 4f, view.width * 0.30f, Row - 8f);
                PhobosWidgets.Gauge(gauge, reading.fill, colour, figure: figures ? reading.figure : null);
                if (reading.stage != null)
                {
                    using (new TextBlock(GameFont.Small, TextAnchor.MiddleLeft))
                    {
                        Widgets.Label(new Rect(gauge.xMax + 8f, y, view.width - gauge.xMax - 8f, Row), DiagnosisText.Colour(reading.stage, reading.tone));
                    }
                }
                y += Row;
            }
            PhobosWidgets.CardEnd(systemTop, ref y, 0f, view.width);
        }

        if (report.faults.Count > 0)
        {
            float faultsTop = y;
            PhobosWidgets.CardHeader(ref y, 0f, view.width, "PhobosVehicleSystems_DiagnoseConditions".Translate(), faultsColour);
            foreach (DiagnosisFault fault in report.faults)
            {
                var row = new Rect(0f, y, view.width, Row);
                Widgets.DrawHighlightIfMouseover(row);
                string said = DiagnosisText.Colour(fault.label, fault.tone) + " (" + fault.part + ")" + (fault.stage != null ? ": " + fault.stage : "");
                float labelWidth = figures ? view.width * 0.64f : view.width;
                using (new TextBlock(GameFont.Small, TextAnchor.MiddleLeft)) Widgets.Label(new Rect(4f, y, labelWidth - 4f, Row), said);
                if (figures)
                {
                    PhobosWidgets.Gauge(new Rect(labelWidth, y + 4f, view.width * 0.30f, Row - 8f), fault.severity, DiagnosisText.ToneColour(fault.tone), figure: fault.severity.ToStringPercent());
                }
                y += Row;
            }
            PhobosWidgets.CardEnd(faultsTop, ref y, 0f, view.width);
        }

        // The last day of the gauges, for those who read figures: how the vehicle was treated, not only where it stands.
        if (figures && (temperatures.Count > 0 || levels.Count > 0))
        {
            float graphsTop = y;
            PhobosWidgets.CardHeader(ref y, 0f, view.width, "PhobosVehicleSystems_ReportLastDay".Translate(QuantityHistory.Hours.Named("HOURS")), null);
            if (temperatures.Count > 0)
            {
                using (new TextBlock(GameFont.Tiny)) Widgets.Label(new Rect(6f, y, view.width - 12f, 18f), "PhobosVehicleSystems_ReportTemperatures".Translate(TemperatureUnit().Named("UNIT")));
                y += 18f;
                PhobosWidgets.HistoryGraphs(new Rect(4f, y, view.width - 8f, GraphHeight), temperatures, temperatureMin, temperatureMax, QuantityHistory.Hours, QuantityHistory.Capacity, TemperatureUnit(), temperatureMarks);
                y += GraphHeight + 8f;
            }
            if (levels.Count > 0)
            {
                using (new TextBlock(GameFont.Tiny)) Widgets.Label(new Rect(6f, y, view.width - 12f, 18f), "PhobosVehicleSystems_ReportLevels".Translate());
                y += 18f;
                PhobosWidgets.HistoryGraphs(new Rect(4f, y, view.width - 8f, GraphHeight), levels, 0f, 100f, QuantityHistory.Hours, QuantityHistory.Capacity, "PhobosVehicleSystems_UnitPercent".Translate(), levelMarks);
                y += GraphHeight + 8f;
            }
            PhobosWidgets.CardEnd(graphsTop, ref y, 0f, view.width);
        }

        Widgets.EndScrollView();
        viewHeight = Mathf.Max(y, 10f);
        litPart = lit;
    }

    static string TemperatureUnit() => Prefs.TemperatureMode switch
    {
        TemperatureDisplayMode.Fahrenheit => "PhobosVehicleSystems_UnitFahrenheit".Translate(),
        TemperatureDisplayMode.Kelvin => "PhobosVehicleSystems_UnitKelvin".Translate(),
        _ => "PhobosVehicleSystems_UnitCelsius".Translate(),
    };
}
