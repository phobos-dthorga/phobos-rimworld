using System.Collections.Generic;

namespace Phobos.Radio.Physics;

/// <summary>What the margin buys. Thresholds come from the rules Def; see <see cref="MarginBands"/>.</summary>
public enum LinkOutcome
{
    NoContact,
    Marginal,
    Workable,
    Clean,
}

/// <summary>The margin thresholds, in dB, above which each outcome starts. Starting values: 0, 6, 15.</summary>
public readonly record struct MarginBands(double MarginalDb, double WorkableDb, double CleanDb)
{
    public static readonly MarginBands Default = new(0.0, 6.0, 15.0);

    public LinkOutcome Classify(double marginDb)
    {
        if (marginDb >= CleanDb) return LinkOutcome.Clean;
        if (marginDb >= WorkableDb) return LinkOutcome.Workable;
        if (marginDb >= MarginalDb) return LinkOutcome.Marginal;
        return LinkOutcome.NoContact;
    }
}

/// <summary>One line of the sheet: a translation key and its value in dB (or dBm for levels).</summary>
public readonly record struct LinkTerm(string Key, double Value, bool IsLevel = false);

/// <summary>
/// Everything one direction of a link needs, already in dB. The game-side service fills this from
/// Defs, comps and the map; the arithmetic lives here so it can be unit-tested without the game.
/// </summary>
public readonly record struct LinkInputs(
    double TransmitPowerDbm,
    double TransmitAntennaGainDbi,
    double TransmitFeedLossDb,
    double PathLossDb,
    double ObstructionLossDb,
    double ReceiveAntennaGainDbi,
    double ReceiveFeedLossDb,
    double NoiseFloorDbm,
    double RequiredSnrDb);

/// <summary>One direction of a link, as a sheet: the ordered terms, what arrives, and the margin.</summary>
public sealed class LinkDirection
{
    public const string KeyTransmitPower = "PhobosRadio_TermTransmitPower";
    public const string KeyTransmitAntenna = "PhobosRadio_TermTransmitAntenna";
    public const string KeyTransmitFeed = "PhobosRadio_TermTransmitFeed";
    public const string KeyPath = "PhobosRadio_TermPath";
    public const string KeyObstruction = "PhobosRadio_TermObstruction";
    public const string KeyReceiveAntenna = "PhobosRadio_TermReceiveAntenna";
    public const string KeyReceiveFeed = "PhobosRadio_TermReceiveFeed";
    public const string KeyReceived = "PhobosRadio_TermReceived";
    public const string KeyNoiseFloor = "PhobosRadio_TermNoiseFloor";
    public const string KeyRequiredSnr = "PhobosRadio_TermRequiredSnr";
    public const string KeyMargin = "PhobosRadio_TermMargin";

    public LinkInputs Inputs { get; }
    public IReadOnlyList<LinkTerm> Terms { get; }
    public double ReceivedDbm { get; }
    public double SnrDb { get; }
    public double MarginDb { get; }

    public LinkDirection(LinkInputs inputs)
    {
        Inputs = inputs;
        ReceivedDbm = inputs.TransmitPowerDbm + inputs.TransmitAntennaGainDbi - inputs.TransmitFeedLossDb
                      - inputs.PathLossDb - inputs.ObstructionLossDb
                      + inputs.ReceiveAntennaGainDbi - inputs.ReceiveFeedLossDb;
        SnrDb = ReceivedDbm - inputs.NoiseFloorDbm;
        MarginDb = SnrDb - inputs.RequiredSnrDb;
        Terms = new[]
        {
            new LinkTerm(KeyTransmitPower, inputs.TransmitPowerDbm, IsLevel: true),
            new LinkTerm(KeyTransmitAntenna, inputs.TransmitAntennaGainDbi),
            new LinkTerm(KeyTransmitFeed, -inputs.TransmitFeedLossDb),
            new LinkTerm(KeyPath, -inputs.PathLossDb),
            new LinkTerm(KeyObstruction, -inputs.ObstructionLossDb),
            new LinkTerm(KeyReceiveAntenna, inputs.ReceiveAntennaGainDbi),
            new LinkTerm(KeyReceiveFeed, -inputs.ReceiveFeedLossDb),
            new LinkTerm(KeyReceived, ReceivedDbm, IsLevel: true),
            new LinkTerm(KeyNoiseFloor, inputs.NoiseFloorDbm, IsLevel: true),
            new LinkTerm(KeyRequiredSnr, -inputs.RequiredSnrDb),
            new LinkTerm(KeyMargin, MarginDb),
        };
    }

    /// <summary>The loss term that costs the most: the Plain mode's one-line reason.</summary>
    public LinkTerm LargestLoss()
    {
        LinkTerm worst = default;
        var found = false;
        foreach (var term in Terms)
        {
            if (term.IsLevel || term.Key == KeyRequiredSnr || term.Key == KeyMargin) continue;
            if (!found || term.Value < worst.Value) { worst = term; found = true; }
        }
        return worst;
    }
}

/// <summary>A two-way link: the worse direction decides, and the sheet says which one that was.</summary>
public sealed class LinkBudget
{
    public LinkDirection Forward { get; }
    public LinkDirection Reverse { get; }
    public bool ReverseLimits { get; }
    public LinkDirection Limiting => ReverseLimits ? Reverse : Forward;
    public double MarginDb => Limiting.MarginDb;
    public LinkOutcome Outcome { get; }

    public LinkBudget(LinkDirection forward, LinkDirection reverse, MarginBands bands)
    {
        Forward = forward;
        Reverse = reverse;
        ReverseLimits = reverse.MarginDb < forward.MarginDb;
        Outcome = bands.Classify(MarginDb);
    }
}
