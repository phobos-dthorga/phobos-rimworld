using Phobos.Radio.Physics;
using Phobos.Radio.Services;
using UnityEngine;
using Verse;

namespace Phobos.Radio.UI;

/// <summary>
/// The step chart of a link budget (Radio 0.6.0, the radio visuals record's RV5), drawn in the link-budget window at Operator and
/// Engineer depth: the transmitter's power on the left, each gain a step up and each loss a step down, to what arrives; then the
/// noise floor and what the mode needs above it as lines, the gap between what arrives and that line being the signal to spare.
/// Operator shows the direction that decides; Engineer both, side by side. Presentation only, from the sheet's own terms; it costs
/// nothing while the window is shut.
/// </summary>
public static class BudgetSteps
{
    public const float Height = 150f;
    private const float LeftMargin = 54f;
    private const float Pad = 8f;

    private static readonly Color Background = new(0.05f, 0.06f, 0.08f);
    private static readonly Color Gain = new(0.44f, 0.83f, 0.88f);
    private static readonly Color Loss = new(0.94f, 0.45f, 0.23f);
    private static readonly Color Floor = new(0.58f, 0.63f, 0.70f);
    private static readonly Color Needed = new(0.79f, 0.63f, 1f);

    public static void Draw(Rect rect, LinkEvaluation link, DisplayMode depth)
    {
        if (depth == DisplayMode.Plain) return;
        Widgets.DrawBoxSolid(rect, Background);
        var budget = link.Budget;
        if (depth == DisplayMode.Engineer)
        {
            var half = rect.width / 2f;
            DrawDirection(new Rect(rect.x, rect.y, half - 2f, rect.height), budget.Forward, !budget.ReverseLimits, link,
                "PhobosRadio_DirectionHandheldToStation".Translate(), depth);
            DrawDirection(new Rect(rect.x + half + 2f, rect.y, half - 2f, rect.height), budget.Reverse, budget.ReverseLimits, link,
                "PhobosRadio_DirectionStationToHandheld".Translate(), depth);
        }
        else
        {
            var title = budget.ReverseLimits ? "PhobosRadio_DirectionStationToHandheld" : "PhobosRadio_DirectionHandheldToStation";
            DrawDirection(rect, budget.Limiting, true, link, title.Translate(), depth);
        }
    }

    private static void DrawDirection(Rect rect, LinkDirection direction, bool limits, LinkEvaluation link, string title,
        DisplayMode depth)
    {
        var inputs = direction.Inputs;
        var floor = inputs.NoiseFloorDbm;
        var needed = floor + inputs.RequiredSnrDb;
        // The running level after each term, from the power sent to what arrives.
        var levels = new double[]
        {
            inputs.TransmitPowerDbm,
            inputs.TransmitPowerDbm + inputs.TransmitAntennaGainDbi,
            inputs.TransmitPowerDbm + inputs.TransmitAntennaGainDbi - inputs.TransmitFeedLossDb,
            inputs.TransmitPowerDbm + inputs.TransmitAntennaGainDbi - inputs.TransmitFeedLossDb - inputs.PathLossDb,
            inputs.TransmitPowerDbm + inputs.TransmitAntennaGainDbi - inputs.TransmitFeedLossDb - inputs.PathLossDb - inputs.ObstructionLossDb,
            inputs.TransmitPowerDbm + inputs.TransmitAntennaGainDbi - inputs.TransmitFeedLossDb - inputs.PathLossDb - inputs.ObstructionLossDb
                + inputs.ReceiveAntennaGainDbi,
            direction.ReceivedDbm,
        };
        var top = Mathf.Max((float)levels[0], (float)levels[1]) + 6f;
        var bottom = Mathf.Min((float)floor, (float)direction.ReceivedDbm) - 6f;
        var plot = new Rect(rect.x + LeftMargin, rect.y + 20f, rect.width - LeftMargin - Pad, rect.height - 28f);
        float Y(double dbm) => plot.y + (float)((top - dbm) / Mathf.Max(1f, top - bottom)) * plot.height;
        var stepWidth = plot.width / levels.Length;

        Text.Font = GameFont.Tiny;
        GUI.color = limits ? Needed : Floor;
        Text.Anchor = TextAnchor.UpperLeft;
        Widgets.Label(new Rect(rect.x + 4f, rect.y + 2f, rect.width - 8f, 18f),
            limits ? "PhobosRadio_SheetDirectionLimiting".Translate(title.Named("DIRECTION")).ToString() : title);
        GUI.color = Color.white;

        // The floor, and what the mode needs above it.
        Widgets.DrawLine(new Vector2(plot.x, Y(floor)), new Vector2(plot.xMax, Y(floor)), Floor, 1f);
        Widgets.DrawLine(new Vector2(plot.x, Y(needed)), new Vector2(plot.xMax, Y(needed)), Needed, 1f);
        Text.Anchor = TextAnchor.MiddleRight;
        GUI.color = Floor;
        Widgets.Label(new Rect(rect.x, Y(floor) - 8f, LeftMargin - 4f, 16f), "PhobosRadio_StepsFloor".Translate());
        GUI.color = Needed;
        Widgets.Label(new Rect(rect.x, Y(needed) - 8f, LeftMargin - 4f, 16f), "PhobosRadio_StepsNeeded".Translate());
        GUI.color = Color.white;

        // The steps: a level held across each term's width, a riser between them.
        for (var i = 0; i < levels.Length; i++)
        {
            var x0 = plot.x + i * stepWidth;
            var y = Y(levels[i]);
            var colour = i == 0 ? Gain : levels[i] >= levels[i - 1] ? Gain : Loss;
            Widgets.DrawLine(new Vector2(x0, y), new Vector2(x0 + stepWidth, y), colour, 2f);
            if (i > 0) Widgets.DrawLine(new Vector2(x0, Y(levels[i - 1])), new Vector2(x0, y), colour, 1f);
        }

        // The signal to spare: the gap from what arrives down to what the mode needs.
        var arrive = Y(direction.ReceivedDbm);
        var margin = direction.MarginDb;
        var gapX = plot.xMax - 4f;
        Widgets.DrawLine(new Vector2(gapX, arrive), new Vector2(gapX, Y(needed)), RadioPathView.OutcomeColor(link.Budget.Outcome), 3f);
        Text.Anchor = TextAnchor.LowerRight;
        GUI.color = RadioPathView.OutcomeColor(link.Budget.Outcome);
        Widgets.Label(new Rect(plot.x, Mathf.Min(arrive, Y(needed)) - 18f, plot.width - 8f, 16f),
            "PhobosRadio_StepsMargin".Translate(margin.ToString("0").Named("MARGIN")));
        GUI.color = Color.white;

        if (depth == DisplayMode.Engineer)
        {
            // Engineer: the level at the start and at the end.
            Text.Anchor = TextAnchor.LowerLeft;
            GUI.color = Gain;
            Widgets.Label(new Rect(plot.x + 2f, Y(levels[0]) - 16f, stepWidth * 2f, 16f), "PhobosRadio_BudgetLevelDbm".Translate(levels[0].ToString("0").Named("LEVEL")));
            GUI.color = Floor;
            Widgets.Label(new Rect(plot.x + stepWidth * (levels.Length - 2), arrive + 14f, stepWidth * 2f, 16f), "PhobosRadio_BudgetLevelDbm".Translate(direction.ReceivedDbm.ToString("0").Named("LEVEL")));
            GUI.color = Color.white;
        }
        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Small;
    }
}
