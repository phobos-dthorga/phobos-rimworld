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

    /// <summary>
    /// A station's channels as the ledger counted them (Radio 0.7.0): in use, width and free, then a line for each kind in use, as
    /// the BVR system's tooltip counts its kinds (0.8.0). With nothing served, the local voice net's line says how many it has room for.
    /// </summary>
    public static string ChannelsLine(StationChannels channels)
    {
        var sb = new StringBuilder("PhobosRadio_StationChannels".Translate(channels.InUse.Named("INUSE"), channels.Channels.Named("CHANNELS"),
            (RadioRulesDef.Current.channelWidthHz / 1000f).ToString("0.#").Named("WIDTH"), channels.Free.Named("FREE")));
        var any = false;
        foreach (var kind in channels.Kinds)
        {
            if (kind.Served == 0) continue;
            any = true;
            AppendKind(sb, kind);
        }
        if (!any && channels.Channels > 0) AppendKind(sb, channels.Of(ChannelKindDef.LocalVoice));
        return sb.ToString();
    }

    private static void AppendKind(StringBuilder sb, KindLedger kind)
    {
        sb.AppendLine();
        sb.Append("PhobosRadio_StationChannelKind".Translate(kind.Kind.LabelCap.Named("KIND"), kind.Held.Named("HELD"),
            kind.Served.Named("SERVED"), kind.Room.Named("ROOM")));
    }

    /// <summary>The one-line reason for the Plain mode: jamming when it holds the link back (Radio 0.45.0), the biggest loss, or noise when the floor is raised.</summary>
    public static string PlainReason(LinkEvaluation link)
    {
        if (link.JammingLimits) return "PhobosRadio_ReasonJamming".Translate();
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
        return "PhobosRadio_OperatorSummary".Translate(link.Band.LabelCap.ToString().Named("BAND"), limiting.SnrDb.ToString("0.0").Named("SNR"),
            limiting.MarginDb.ToString("0.0").Named("MARGIN"), direction.Translate().ToString().Named("DIRECTION"), link.DistanceMetres.ToString("0").Named("DISTANCE"));
    }

    /// <summary>Engineer depth: the whole sheet, both directions.</summary>
    public static string EngineerSheet(LinkEvaluation link)
    {
        var sb = new StringBuilder();
        sb.AppendLine("PhobosRadio_SheetHeader".Translate(link.Band.LabelCap.ToString().Named("BAND"), link.Band.centreMHz.ToString("0.0").Named("FREQUENCY"), link.Mode.LabelCap.ToString().Named("MODE")));
        sb.AppendLine("PhobosRadio_SheetAntennas".Translate(link.A.AntennaLabel.ToString().Named("FIRST"), link.B.AntennaLabel.ToString().Named("SECOND")));
        AppendDirection(sb, "PhobosRadio_DirectionHandheldToStation".Translate(), link.Budget.Forward, !link.Budget.ReverseLimits);
        AppendDirection(sb, "PhobosRadio_DirectionStationToHandheld".Translate(), link.Budget.Reverse, link.Budget.ReverseLimits);
        // Radio 0.45.0 (EW1): jamming is already in each direction's floor above; this line says how much of it is jamming.
        if (!double.IsNegativeInfinity(link.JammingAtADbm) || !double.IsNegativeInfinity(link.JammingAtBDbm))
        {
            sb.AppendLine();
            sb.AppendLine("PhobosRadio_SheetJamming".Translate(JammingText(link.JammingAtBDbm).Named("ATSECOND"), JammingText(link.JammingAtADbm).Named("ATFIRST")));
        }
        return sb.ToString().TrimEnd();
    }

    private static string JammingText(double dbm) => double.IsNegativeInfinity(dbm) ? "-" : $"{dbm:0.0} dBm";

    private static void AppendDirection(StringBuilder sb, string title, LinkDirection direction, bool limits)
    {
        sb.AppendLine();
        sb.AppendLine(limits ? "PhobosRadio_SheetDirectionLimiting".Translate(title.Named("DIRECTION")) : title);
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
            _ => head + (link.InTouch ? "" : "\n" + "PhobosRadio_PlainReason".Translate(PlainReason(link).ToString().Named("REASON"))),
        };
    }
}
