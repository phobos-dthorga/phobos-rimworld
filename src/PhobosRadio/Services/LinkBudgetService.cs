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
    /// tiles (the greater of free space and plane earth since 0.22.0), both directions as on a map, and nothing past the radio horizon of the
    /// two antennas, each lifted by its tile's ground (Propagation.SiteHeightMetres). No walls or local noise between tiles: a quiet rural
    /// floor at each end (simplifications, labelled in the radio's records). Since Radio 0.41.0 the high ground between the two tiles costs
    /// signal inside the horizon (<paramref name="terrainFigure"/>, from <see cref="WorldTerrain.Between"/>; nothing by default).
    /// <b>A contact between two tiles goes through the overload that takes the tiles</b>, which reads the ground between them; this one is
    /// the arithmetic, for a caller that has read the ground already (the world map's picture).
    /// </summary>
    public static LinkEvaluation? EvaluateWorld(RadioEndpoint a, RadioEndpoint b, double distanceMetres, RadioRulesDef rules,
        double siteAMetres = 0, double siteBMetres = 0, ModeDef? mode = null, double terrainFigure = double.NegativeInfinity)
    {
        // Radio 0.22.0: each end's height above the plain is its aerial and its tile's ground (hilltops), for the horizon and plane earth.
        var heightA = a.AntennaHeightMetres + siteAMetres;
        var heightB = b.AntennaHeightMetres + siteBMetres;
        // Radio 0.40.0: a link may be worked in another mode than the radio's own (Morse, when both ends have a key).
        mode ??= a.Mode;
        var floorAtA = NoiseFloor.TotalDbm(mode.bandwidthHz, a.NoiseFigureDb, double.NegativeInfinity);
        var floorAtB = NoiseFloor.TotalDbm(mode.bandwidthHz, b.NoiseFigureDb, double.NegativeInfinity);
        var overHorizon = distanceMetres > Propagation.RadioHorizonMetres(heightA, heightB);
        var planeEarth = Propagation.PlaneEarthPathLossDb(distanceMetres, heightA, heightB);
        LinkEvaluation? best = null;
        foreach (var band in a.Bands)
        {
            if (!b.Bands.Contains(band)) continue;
            // Radio 0.22.0 (the owner's card: plane-earth fading): the greater of free space and plane earth.
            var pathLoss = System.Math.Max(PathLossDb(distanceMetres, band), planeEarth);
            // Radio 0.41.0: inside the horizon, the high ground on the way costs what its edge costs on this band.
            var obstruction = overHorizon ? rules.overHorizonLossDb : Physics.TerrainRule.LossDb(terrainFigure, band.centreMHz);
            var forward = new LinkDirection(Inputs(a.Side(band), b.Side(band), pathLoss, obstruction, floorAtB, mode));
            var reverse = new LinkDirection(Inputs(b.Side(band), a.Side(band), pathLoss, obstruction, floorAtA, mode));
            var budget = new LinkBudget(forward, reverse, rules.MarginBands);
            if (best == null || budget.MarginDb > best.MarginDb)
                best = new LinkEvaluation(a, b, band, mode, budget, distanceMetres, double.NegativeInfinity, double.NegativeInfinity);
        }
        return best;
    }

    /// <summary>
    /// A link between two ends on two world tiles, the way every contact between tiles is decided (Radio 0.41.0): the distance between the
    /// tiles at the rules' metres a tile, each aerial lifted by its site, and the high ground on the way between them
    /// (<see cref="WorldTerrain"/>). <paramref name="blocking"/> says what stood in the way, for the caller's line to the player.
    /// </summary>
    public static LinkEvaluation? EvaluateWorld(RadioEndpoint a, RimWorld.Planet.PlanetTile tileA, double siteAMetres, RadioEndpoint b,
        RimWorld.Planet.PlanetTile tileB, double siteBMetres, RadioRulesDef rules, out WorldTerrain.Blocking blocking, ModeDef? mode = null)
    {
        double metres = Find.WorldGrid.ApproxDistanceInTiles(tileA, tileB) * rules.metresPerWorldTile;
        blocking = WorldTerrain.Between(tileA, tileB, a.AntennaHeightMetres + siteAMetres, b.AntennaHeightMetres + siteBMetres, metres, rules);
        return EvaluateWorld(a, b, metres, rules, siteAMetres, siteBMetres, mode, blocking.Figure);
    }

    /// <summary>The same between two relay nodes, each with its tile and its site.</summary>
    public static LinkEvaluation? EvaluateWorld(RelayNode a, RelayNode b, RadioRulesDef rules, ModeDef? mode = null) =>
        EvaluateWorld(a.End, a.Tile, a.SiteMetres, b.End, b.Tile, b.SiteMetres, rules, out _, mode);

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
