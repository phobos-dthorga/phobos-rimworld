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
    /// <summary>Jamming at each end's receiver input on this link's band and mode, in dBm, or −∞ (Radio 0.45.0, EW1).</summary>
    public double JammingAtADbm { get; }
    public double JammingAtBDbm { get; }

    public LinkEvaluation(RadioEndpoint a, RadioEndpoint b, BandDef band, ModeDef mode, LinkBudget budget,
        double distanceMetres, double localNoiseAtADbm, double localNoiseAtBDbm,
        double jammingAtADbm = double.NegativeInfinity, double jammingAtBDbm = double.NegativeInfinity)
    {
        JammingAtADbm = jammingAtADbm;
        JammingAtBDbm = jammingAtBDbm;
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

    /// <summary>Jamming at the receiver of the direction that limits this link, in dBm, or −∞.</summary>
    public double JammingAtLimitingReceiverDbm => Budget.ReverseLimits ? JammingAtADbm : JammingAtBDbm;

    /// <summary>
    /// Whether jamming is what holds this link back (Radio 0.45.0, EW1): at the limiting direction's receiver it raises the floor by more
    /// than the rules' figure over what that receiver hears anyway. The plain reason, the lost-link letter and the notes ask this one place.
    /// </summary>
    public bool JammingLimits
    {
        get
        {
            var jamming = JammingAtLimitingReceiverDbm;
            if (double.IsNegativeInfinity(jamming)) return false;
            var receiver = Budget.ReverseLimits ? A : B;
            var local = Budget.ReverseLimits ? LocalNoiseAtADbm : LocalNoiseAtBDbm;
            var without = NoiseFloor.TotalDbm(Mode.bandwidthHz, receiver.NoiseFigureDb, local);
            return Budget.Limiting.Inputs.NoiseFloorDbm - without > RadioRulesDef.Current.jammingRaisesFloorDb;
        }
    }
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
            var sideA = a.Side(band);
            var sideB = b.Side(band);
            // Radio 0.45.0 (EW1): jamming on this band, through each receiver's own antenna and cable.
            var jamAtA = JammingAtInputDbm(noise.JammingDbm(a.Cell, metresPerCell, mode.bandwidthHz, band), sideA);
            var jamAtB = JammingAtInputDbm(noise.JammingDbm(b.Cell, metresPerCell, mode.bandwidthHz, band), sideB);
            var forward = new LinkDirection(Inputs(sideA, sideB, pathLoss, obstruction, NoiseFloor.WithJammingDbm(floorAtB, jamAtB), mode));
            var reverse = new LinkDirection(Inputs(sideB, sideA, pathLoss, obstruction, NoiseFloor.WithJammingDbm(floorAtA, jamAtA), mode));
            var budget = new LinkBudget(forward, reverse, bands);
            if (best == null || budget.MarginDb > best.MarginDb)
            {
                best = new LinkEvaluation(a, b, band, mode, budget, distanceMetres, noiseAtA, noiseAtB, jamAtA, jamAtB);
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
        => EvaluateWorld(a, b, distanceMetres, rules, siteAMetres, siteBMetres, mode, terrainFigure, null, null);

    /// <summary>
    /// The arithmetic, with the two tiles when known (Radio 0.62.0): an HF band between tiles is weighed only with them, as its loss is the
    /// ground wave over their grounds at HF's own scale and its floor takes the atmosphere's noise at each receiver
    /// (<see cref="HfService"/>; no horizon and no hills, a smooth Earth). Without the tiles an HF band is left out.
    /// </summary>
    static LinkEvaluation? EvaluateWorld(RadioEndpoint a, RadioEndpoint b, double distanceMetres, RadioRulesDef rules, double siteAMetres,
        double siteBMetres, ModeDef? mode, double terrainFigure, RimWorld.Planet.PlanetTile? tileA, RimWorld.Planet.PlanetTile? tileB)
    {
        // Radio 0.22.0: each end's height above the plain is its aerial and its tile's ground (hilltops), for the horizon and plane earth.
        var heightA = a.AntennaHeightMetres + siteAMetres;
        var heightB = b.AntennaHeightMetres + siteBMetres;
        // Radio 0.40.0: a link may be worked in another mode than the radio's own (Morse, when both ends have a key).
        mode ??= a.Mode;
        var floorAtA = NoiseFloor.TotalDbm(mode.bandwidthHz, a.NoiseFigureDb, double.NegativeInfinity);
        var floorAtB = NoiseFloor.TotalDbm(mode.bandwidthHz, b.NoiseFigureDb, double.NegativeInfinity);
        LinkEvaluation? best = null;
        foreach (var band in a.Bands)
        {
            if (!b.Bands.Contains(band)) continue;
            double pathLoss, obstruction, floorA = floorAtA, floorB = floorAtB, metres = distanceMetres;
            if (band.path == BandPath.Hf)
            {
                if (tileA is not { } ta || tileB is not { } tb) continue;
                metres = distanceMetres / rules.metresPerWorldTile * rules.hfMetresPerWorldTile;
                if (HfService.GroundWaveDb(band, ta, tb, metres / 1000.0, a.AntennaHeightMetres, b.AntennaHeightMetres, a.Horizontal, b.Horizontal)
                    is not { } groundWave) continue;
                pathLoss = groundWave;
                obstruction = 0;
                floorA = HfService.FloorWithAtmosphereDbm(floorAtA, band, ta, mode.bandwidthHz);
                floorB = HfService.FloorWithAtmosphereDbm(floorAtB, band, tb, mode.bandwidthHz);
            }
            else WorldLosses(distanceMetres, band, heightA, heightB, terrainFigure, rules, out pathLoss, out obstruction);
            var forward = new LinkDirection(Inputs(a.Side(band), b.Side(band), pathLoss, obstruction, floorB, mode));
            var reverse = new LinkDirection(Inputs(b.Side(band), a.Side(band), pathLoss, obstruction, floorA, mode));
            var budget = new LinkBudget(forward, reverse, rules.MarginBands);
            if (best == null || budget.MarginDb > best.MarginDb)
                best = new LinkEvaluation(a, b, band, mode, budget, metres, double.NegativeInfinity, double.NegativeInfinity);
        }
        if (best != null && best.Band.path == BandPath.Hf && best.InTouch && Seen.Wants("radio.hf-ground-wave", a.Thing, best.Band.defName))
            Seen.Note("radio.hf-ground-wave", a.Thing, best.Band.label + ", " + (best.DistanceMetres / 1000.0).ToString("0") + " km, margin "
                + best.MarginDb.ToString("0.0") + " dB", best.Band.defName);
        return best;
    }

    /// <summary>
    /// What distance and the ground cost a link between two tiles on one band, in dB: the sums every contact between tiles and the
    /// world map's picture share, so the picture cannot disagree with a contact (Radio 0.42.0). The path loss is the greater of free space
    /// and plane earth (Radio 0.22.0, the owner's card: plane-earth fading). What stands in the way is everything past the horizon of the
    /// two aerials, and inside it what the high ground's edge costs on this band (Radio 0.41.0). Heights are aerial and site together.
    /// </summary>
    public static void WorldLosses(double distanceMetres, BandDef band, double heightAMetres, double heightBMetres, double terrainFigure,
        RadioRulesDef rules, out double pathLossDb, out double obstructionDb)
    {
        pathLossDb = System.Math.Max(PathLossDb(distanceMetres, band), Propagation.PlaneEarthPathLossDb(distanceMetres, heightAMetres, heightBMetres));
        obstructionDb = distanceMetres > Propagation.RadioHorizonMetres(heightAMetres, heightBMetres)
            ? rules.overHorizonLossDb
            : TerrainRule.LossDb(terrainFigure, band.centreMHz);
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
        return EvaluateWorld(a, b, metres, rules, siteAMetres, siteBMetres, mode, blocking.Figure, tileA, tileB);
    }

    /// <summary>The same between two relay nodes, each with its tile and its site.</summary>
    public static LinkEvaluation? EvaluateWorld(RelayNode a, RelayNode b, RadioRulesDef rules, ModeDef? mode = null) =>
        EvaluateWorld(a.End, a.Tile, a.SiteMetres, b.End, b.Tile, b.SiteMetres, rules, out _, mode);

    /// <summary>
    /// Jamming at a receiver's input from what reaches its antenna (Radio 0.45.0, EW1): through its antenna's gain, less its cable and
    /// anything only in its receive path (amplifiers switched out of the line). The coverage picture uses the same, so it cannot disagree.
    /// </summary>
    public static double JammingAtInputDbm(double atAntennaDbm, in EndSide receiver) =>
        Jamming.AtInputDbm(atAntennaDbm, receiver.AntennaGainDbi, receiver.FeedLossDb + receiver.ReceiveOnlyLossDb);

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
