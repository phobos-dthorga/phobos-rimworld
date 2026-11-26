using System.Linq;
using Phobos.Radio.Physics;
using Verse;

namespace Phobos.Radio.Services;

/// <summary>A link between two endpoints: the budget, and the band and mode it was computed for.</summary>
public sealed class LinkEvaluation
{
    public RadioEndpoint A { get; }
    public RadioEndpoint B { get; }
    public BandDef Band { get; }
    public ModeDef Mode { get; }
    public LinkBudget Budget { get; }
    public double DistanceMetres { get; }
    public double LocalNoiseAtADbm { get; }
    public double LocalNoiseAtBDbm { get; }

    public LinkEvaluation(RadioEndpoint a, RadioEndpoint b, BandDef band, ModeDef mode, LinkBudget budget,
        double distanceMetres, double localNoiseAtADbm, double localNoiseAtBDbm)
    {
        A = a;
        B = b;
        Band = band;
        Mode = mode;
        Budget = budget;
        DistanceMetres = distanceMetres;
        LocalNoiseAtADbm = localNoiseAtADbm;
        LocalNoiseAtBDbm = localNoiseAtBDbm;
    }

    public LinkOutcome Outcome => Budget.Outcome;
    public double MarginDb => Budget.MarginDb;
    public bool InTouch => Outcome >= LinkOutcome.Workable;
}

/// <summary>
/// The single source of truth: every contact is decided here, in both directions, on the best band the
/// two ends share. The UI only shows what this returns.
/// </summary>
public static class LinkBudgetService
{
    public static LinkEvaluation? Evaluate(Map map, RadioEndpoint a, RadioEndpoint b, NoiseService noise, RadioRulesDef rules,
        float metresPerCell)
    {
        var mode = a.Mode;
        var distanceMetres = a.Cell.DistanceTo(b.Cell) * metresPerCell;
        var noiseAtA = noise.LocalNoiseDbm(a.Cell, metresPerCell);
        var noiseAtB = noise.LocalNoiseDbm(b.Cell, metresPerCell);
        var floorAtA = NoiseFloor.TotalDbm(mode.bandwidthHz, a.NoiseFigureDb, noiseAtA);
        var floorAtB = NoiseFloor.TotalDbm(mode.bandwidthHz, b.NoiseFigureDb, noiseAtB);
        var bands = rules.MarginBands;

        LinkEvaluation? best = null;
        var crossings = ObstructionService.Walk(map, a.Cell, a.AntennaHeightMetres, b.Cell, b.AntennaHeightMetres, rules);
        foreach (var band in a.Bands)
        {
            if (!b.Bands.Contains(band)) continue;
            var pathLoss = PathLossDb(distanceMetres, band);
            var obstruction = crossings.LossDb(band);
            var forward = new LinkDirection(Inputs(a.Side(band), b.Side(band), pathLoss, obstruction, floorAtB, mode));
            var reverse = new LinkDirection(Inputs(b.Side(band), a.Side(band), pathLoss, obstruction, floorAtA, mode));
            var budget = new LinkBudget(forward, reverse, bands);
            if (best == null || budget.MarginDb > best.MarginDb)
            {
                best = new LinkEvaluation(a, b, band, mode, budget, distanceMetres, noiseAtA, noiseAtB);
            }
        }
        return best;
    }

    /// <summary>
    /// A link between two ends on different world tiles (Radio 0.21.0, phase 2's first step; EM33): free space over the distance between the
    /// tiles, both directions as on a map, and nothing past the radio horizon of the two antennas. No walls, terrain or tile heights are read
    /// between tiles yet, and no local noise: a quiet rural floor at each end (simplifications, labelled in the radio's records).
    /// </summary>
    public static LinkEvaluation? EvaluateWorld(RadioEndpoint a, RadioEndpoint b, double distanceMetres, RadioRulesDef rules)
    {
        var mode = a.Mode;
        var floorAtA = NoiseFloor.TotalDbm(mode.bandwidthHz, a.NoiseFigureDb, double.NegativeInfinity);
        var floorAtB = NoiseFloor.TotalDbm(mode.bandwidthHz, b.NoiseFigureDb, double.NegativeInfinity);
        var overHorizon = distanceMetres > Propagation.RadioHorizonMetres(a.AntennaHeightMetres, b.AntennaHeightMetres);
        LinkEvaluation? best = null;
        foreach (var band in a.Bands)
        {
            if (!b.Bands.Contains(band)) continue;
            var pathLoss = PathLossDb(distanceMetres, band);
            var obstruction = overHorizon ? rules.overHorizonLossDb : 0.0;
            var forward = new LinkDirection(Inputs(a.Side(band), b.Side(band), pathLoss, obstruction, floorAtB, mode));
            var reverse = new LinkDirection(Inputs(b.Side(band), a.Side(band), pathLoss, obstruction, floorAtA, mode));
            var budget = new LinkBudget(forward, reverse, rules.MarginBands);
            if (best == null || budget.MarginDb > best.MarginDb)
                best = new LinkEvaluation(a, b, band, mode, budget, distanceMetres, double.NegativeInfinity, double.NegativeInfinity);
        }
        return best;
    }

    /// <summary>Distance on a band: free space, the only path model on one map.</summary>
    public static double PathLossDb(double distanceMetres, BandDef band) =>
        Propagation.FreeSpacePathLossDb(distanceMetres, band.centreMHz);

    /// <summary>
    /// One direction's inputs, from the transmitting side and the receiving side. Every contact and every coverage cell builds its
    /// inputs here, so the overlay and the link sheet cannot drift apart (Radio 0.2.0).
    /// </summary>
    public static LinkInputs Inputs(in EndSide tx, in EndSide rx, double pathLossDb, double obstructionDb, double noiseFloorAtRxDbm,
        ModeDef mode) =>
        new(
            TransmitPowerDbm: tx.TransmitPowerDbm,
            TransmitAntennaGainDbi: tx.AntennaGainDbi,
            TransmitFeedLossDb: tx.FeedLossDb,
            PathLossDb: pathLossDb,
            ObstructionLossDb: obstructionDb,
            ReceiveAntennaGainDbi: rx.AntennaGainDbi,
            ReceiveFeedLossDb: rx.FeedLossDb + rx.ReceiveOnlyLossDb,
            NoiseFloorDbm: noiseFloorAtRxDbm,
            RequiredSnrDb: mode.requiredSnrDb);
}

/// <summary>
/// What one end brings to a link on one band: its power, its antenna's gain, its cable's loss both ways, and what costs it only on
/// receive (the amplifiers in its run, switched out of the line; Radio 0.5.0).
/// </summary>
public readonly record struct EndSide(double TransmitPowerDbm, double AntennaGainDbi, double FeedLossDb, double ReceiveOnlyLossDb = 0);
