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
        foreach (var band in a.Bands)
        {
            if (!b.Bands.Contains(band)) continue;
            var pathLoss = Propagation.FreeSpacePathLossDb(distanceMetres, band.centreMHz);
            var obstruction = ObstructionService.LossDb(map, a, b, band, rules);
            var forward = new LinkDirection(Inputs(a, b, pathLoss, obstruction, floorAtB, mode));
            var reverse = new LinkDirection(Inputs(b, a, pathLoss, obstruction, floorAtA, mode));
            var budget = new LinkBudget(forward, reverse, bands);
            if (best == null || budget.MarginDb > best.MarginDb)
            {
                best = new LinkEvaluation(a, b, band, mode, budget, distanceMetres, noiseAtA, noiseAtB);
            }
        }
        return best;
    }

    private static LinkInputs Inputs(RadioEndpoint tx, RadioEndpoint rx, double pathLossDb, double obstructionDb,
        double noiseFloorAtRxDbm, ModeDef mode) =>
        new(
            TransmitPowerDbm: Decibels.FromWatts(tx.TransmitPowerWatts),
            TransmitAntennaGainDbi: tx.AntennaGainDbi,
            TransmitFeedLossDb: tx.FeedLossDb,
            PathLossDb: pathLossDb,
            ObstructionLossDb: obstructionDb,
            ReceiveAntennaGainDbi: rx.AntennaGainDbi,
            ReceiveFeedLossDb: rx.FeedLossDb,
            NoiseFloorDbm: noiseFloorAtRxDbm,
            RequiredSnrDb: mode.requiredSnrDb);
}
