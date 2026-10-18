using System.Text;
using Phobos.Radio.Physics;
using Phobos.Radio.Services;
using Verse;

namespace Phobos.Radio.UI;

/// <summary>Turns a link into words at the chosen depth. Presentation only; nothing here decides.</summary>
public static class LinkText
{
    public static string Bars(LinkOutcome outcome) => outcome switch
    {
        LinkOutcome.Clean => "[||||]",
        LinkOutcome.Workable => "[||| ]",
        LinkOutcome.Marginal => "[||  ]",
        _ => "[    ]",
    };

    public static string OutcomeLabel(LinkOutcome outcome) => ("PhobosRadio_Outcome" + outcome).Translate();

    /// <summary>The one-line reason for the Plain mode: the biggest loss, or noise when the floor is raised.</summary>
    public static string PlainReason(LinkEvaluation link)
    {
        var limiting = link.Budget.Limiting;
        var noiseAtReceiver = link.Budget.ReverseLimits ? link.LocalNoiseAtADbm : link.LocalNoiseAtBDbm;
        var thermal = NoiseFloor.ThermalDbm(link.Mode.bandwidthHz, (link.Budget.ReverseLimits ? link.A : link.B).NoiseFigureDb);
        if (!double.IsNegativeInfinity(noiseAtReceiver) && limiting.Inputs.NoiseFloorDbm - thermal > 6.0)
        {
            return "PhobosRadio_ReasonNoise".Translate();
        }
        return limiting.LargestLoss().Key switch
        {
            LinkDirection.KeyObstruction => "PhobosRadio_ReasonObstruction".Translate(),
            LinkDirection.KeyTransmitFeed => "PhobosRadio_ReasonTransmitFeed".Translate(),
            LinkDirection.KeyReceiveFeed => "PhobosRadio_ReasonReceiveFeed".Translate(),
            _ => "PhobosRadio_ReasonPath".Translate(),
        };
    }

    /// <summary>Operator depth: band, SNR, margin and which direction limits.</summary>
    public static string OperatorSummary(LinkEvaluation link)
    {
        var limiting = link.Budget.Limiting;
        var direction = link.Budget.ReverseLimits ? "PhobosRadio_DirectionStationToHandheld" : "PhobosRadio_DirectionHandheldToStation";
        return "PhobosRadio_OperatorSummary".Translate(link.Band.LabelCap, limiting.SnrDb.ToString("0.0"),
            limiting.MarginDb.ToString("0.0"), direction.Translate(), link.DistanceMetres.ToString("0"));
    }

    /// <summary>Engineer depth: the whole sheet, both directions.</summary>
    public static string EngineerSheet(LinkEvaluation link)
    {
        var sb = new StringBuilder();
        sb.AppendLine("PhobosRadio_SheetHeader".Translate(link.Band.LabelCap, link.Band.centreMHz.ToString("0.0"), link.Mode.LabelCap));
        sb.AppendLine("PhobosRadio_SheetAntennas".Translate(link.A.AntennaLabel, link.B.AntennaLabel));
        AppendDirection(sb, "PhobosRadio_DirectionHandheldToStation".Translate(), link.Budget.Forward, !link.Budget.ReverseLimits);
        AppendDirection(sb, "PhobosRadio_DirectionStationToHandheld".Translate(), link.Budget.Reverse, link.Budget.ReverseLimits);
        return sb.ToString().TrimEnd();
    }

    private static void AppendDirection(StringBuilder sb, string title, LinkDirection direction, bool limits)
    {
        sb.AppendLine();
        sb.AppendLine(limits ? "PhobosRadio_SheetDirectionLimiting".Translate(title) : title);
        foreach (var term in direction.Terms)
        {
            var value = term.IsLevel ? $"{term.Value:0.0} dBm" : $"{(term.Value >= 0 ? "+" : "")}{term.Value:0.0} dB";
            sb.AppendLine($"  {term.Key.Translate()}: {value}");
        }
    }

    public static string Describe(LinkEvaluation? link, DisplayMode mode)
    {
        if (link == null) return "PhobosRadio_NoStation".Translate();
        var head = Bars(link.Outcome) + " " + OutcomeLabel(link.Outcome);
        return mode switch
        {
            DisplayMode.Engineer => head + "\n" + OperatorSummary(link) + "\n\n" + EngineerSheet(link),
            DisplayMode.Operator => head + "\n" + OperatorSummary(link),
            _ => head + (link.InTouch ? "" : "\n" + "PhobosRadio_PlainReason".Translate(PlainReason(link))),
        };
    }
}
