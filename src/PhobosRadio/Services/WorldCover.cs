using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Phobos.Framework.WorldMap;
using Phobos.Radio.Physics;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Phobos.Radio.Services;

/// <summary>
/// A place on the world map with a working radio, as the cover picture and the reach ring read it: its ends (each with its tile and its
/// site's height), whether it is the one that calls (a convoy), and which of its ends have a Morse key.
/// </summary>
public sealed class WorldCoverSite
{
    public readonly WorldObject Place;
    public readonly PlanetTile Tile;
    public readonly List<RelayNode> Ends;
    /// <summary>A convoy: it calls and the far end answers, so a link to a relay is worked in its mode.</summary>
    public readonly bool Caller;
    /// <summary>Whether a relay stands here, which passes on speech only: no Morse.</summary>
    public readonly bool Relay;

    public WorldCoverSite(WorldObject place, List<RelayNode> ends, bool caller = false, bool relay = false)
    {
        Place = place;
        Tile = place.Tile;
        Ends = ends;
        Caller = caller;
        Relay = relay;
    }

    /// <summary>Whether this end's station has a Morse key (a base station's upgrade).</summary>
    public static bool HasKey(RelayNode end) => end.End.Thing != null && RadioUpgrades.Figure(end.End.Thing, RadioUpgrades.MorseKey, 0f) >= 1f;

    /// <summary>The highest aerial here over its tile's plain: aerial and site together.</summary>
    public double HighestMetres()
    {
        double highest = 0;
        foreach (RelayNode end in Ends) highest = System.Math.Max(highest, end.End.AntennaHeightMetres + end.SiteMetres);
        return highest;
    }
}

/// <summary>Why a far-end radio cannot be drawn for a place.</summary>
public enum WorldCoverRefusal
{
    None,
    /// <summary>Morse, with no key at any station of the place.</summary>
    NoMorseKey,
    /// <summary>Morse, at a relay: a relay passes on speech only.</summary>
    RelayPassesSpeechOnly,
}

/// <summary>Something that knows a kind of place with a radio: the vehicles tie-in, for a convoy.</summary>
public interface IWorldCoverSiteSource
{
    /// <summary>Whether this place may have a radio: asked each frame it is selected, so it must cost almost nothing.</summary>
    bool MayHaveRadio(WorldObject place);

    /// <summary>The place's working radio ends, or null: asked now and then, and when the picture's menu opens.</summary>
    WorldCoverSite? SiteOf(WorldObject place);
}

/// <summary>
/// The cover picture on the world map (Radio 0.42.0; the owner's cards of 27 Nov 2026: painted tiles for one site, and buttons to switch
/// the far-end radio; docs/development/radio-world-coverage.md). With a place of the player's that has a radio selected on the world map
/// and a far-end radio picked, every tile within the horizon is given the signal a radio of that kind standing there would have to spare
/// with the place: the same losses a contact between tiles uses (<see cref="LinkBudgetService.WorldLosses"/>), the same ground on the way
/// (<see cref="TerrainRule.Worst"/> over the very tiles <see cref="TileAir.WalkLeg"/> would walk from that tile to the place), so the
/// picture cannot disagree with a call made from there.
/// <para>
/// <b>Performance is the mod's first concern.</b> Nothing runs with the picture off, the world map not shown, or no such place selected.
/// While on, tiles are worked out outward from the place for at most <see cref="RadioRulesDef.worldCoverBudgetMsPerFrame"/> of real time
/// a frame. Each tile's step toward the place is found once and kept, so a tile's way back is a few reads a tile of distance, never a
/// search. It works again only when the place, its radios or the far-end radio change (looked at every <see cref="LookFrames"/> frames).
/// Four arrays the size of the planet's layer are held while the picture is on, and let go when it is switched off. Nothing is saved.
/// </para>
/// </summary>
public static class WorldCover
{
    /// <summary>How often, in frames, the selected place's radios are read again for a change.</summary>
    public const int LookFrames = 60;
    /// <summary>How often, in tiles, the frame's time budget is checked.</summary>
    const int TilesPerClockCheck = 16;
    /// <summary>How often the picture is redrawn while it is still being worked out, in seconds of real time, so it is seen to grow.</summary>
    const float RedrawSeconds = 1f;
    /// <summary>The far end a ring measures to from a relay: a home station's mast on level ground (authored: the mast antenna's height).</summary>
    public const float RingHomeMastMetres = 10f;
    /// <summary>The far end a ring measures to from a colony when no vehicle radio is defined: a whip on a cab (authored).</summary>
    public const float RingWhipMetres = 3f;

    static readonly List<IWorldCoverSiteSource> sources = new() { new CoreSites() };
    static readonly List<PlanetTile> neighbours = new();
    static readonly List<double> profile = new();
    static readonly Queue<int> queue = new();
    static readonly List<int> painted = new();
    static readonly List<Combo> combos = new();
    static readonly Stopwatch watch = new();

    static Game? game;
    static RadioCoverKindDef? kind;

    // The place selected, read now and then.
    static WorldObject? place;
    static WorldCoverSite? site;
    static int lookedFrame = -LookFrames;
    static int ringTiles;

    // The pass: what it is for, and how far it has got.
    static int signature;
    static PlanetLayer? layer;
    static int siteId = -1;
    static Vector3 siteCentre;
    static int radiusTiles;
    static int generation;
    static int[] seen = System.Array.Empty<int>();
    static int[] nextGeneration = System.Array.Empty<int>();
    static int[] next = System.Array.Empty<int>();
    static float[] margins = System.Array.Empty<float>();
    static bool working;
    static bool hasPass;
    static int done;
    static int reached;
    static float lastRedraw;

    public static void Register(IWorldCoverSiteSource source)
    {
        if (!sources.Contains(source)) sources.Add(source);
    }

    /// <summary>The far-end radio the picture is drawn for, or null with the picture off. It starts off in every game.</summary>
    public static RadioCoverKindDef? Kind
    {
        get
        {
            Fresh();
            return kind;
        }
    }

    /// <summary>Raised each time the picture has changed: the draw layer redraws when it differs from what it drew.</summary>
    public static int Version { get; private set; }

    /// <summary>The tiles painted so far, by id on <see cref="Layer"/>, and each one's signal to spare in dB (below 0: no contact).</summary>
    public static IReadOnlyList<int> Painted => painted;
    public static float MarginOf(int tileId) => margins[tileId];
    public static PlanetLayer? Layer => layer;

    /// <summary>Whether there is a picture to draw on this layer of the planet now.</summary>
    public static bool Showing(PlanetLayer planetLayer) => kind != null && hasPass && layer == planetLayer && game == Current.Game;

    /// <summary>Picks the far-end radio, or switches the picture off with null. Presentation only: it changes nothing in the game.</summary>
    public static void Pick(RadioCoverKindDef? picked)
    {
        Fresh();
        if (kind == picked) return;
        kind = picked;
        signature = 0;
        lookedFrame = -LookFrames;
        if (picked == null) LetGo();
    }

    /// <summary>Whether a place may have a radio, cheaply: what a button asks each frame.</summary>
    public static bool MayHaveRadio(WorldObject place)
    {
        foreach (IWorldCoverSiteSource source in sources)
            if (source.MayHaveRadio(place)) return true;
        return false;
    }

    /// <summary>A place's working radio ends, or null.</summary>
    public static WorldCoverSite? SiteOf(WorldObject place)
    {
        foreach (IWorldCoverSiteSource source in sources)
            if (source.MayHaveRadio(place) && source.SiteOf(place) is { Ends.Count: > 0 } found) return found;
        return null;
    }

    /// <summary>Why this far-end radio cannot be drawn for a place, or <see cref="WorldCoverRefusal.None"/> when it can.</summary>
    public static WorldCoverRefusal Refusal(WorldCoverSite site, RadioCoverKindDef kind)
    {
        if (!kind.needsKey) return WorldCoverRefusal.None;
        if (site.Relay) return WorldCoverRefusal.RelayPassesSpeechOnly;
        foreach (RelayNode end in site.Ends)
            if (WorldCoverSite.HasKey(end)) return WorldCoverRefusal.None;
        return WorldCoverRefusal.NoMorseKey;
    }

    /// <summary>The place the picture and the ring are for now, or null.</summary>
    public static WorldCoverSite? Site => site;

    /// <summary>How far the selected place's ring reaches, in tiles; 0 with none.</summary>
    public static int RingTiles => site == null ? 0 : ringTiles;

    /// <summary>Whether the picture is still being worked out, and how far along it is, from 0 to 1.</summary>
    public static bool Working => working;
    public static float Progress => !working ? 1f : Mathf.Clamp01(done / (float)System.Math.Max(1, System.Math.Min(layer?.TilesCount ?? 1, CoverRule.TilesWithin(radiusTiles))));

    /// <summary>How many tiles the finished picture found a contact on (workable or better).</summary>
    public static int Reached => reached;

    /// <summary>
    /// Called each frame the world map is shown, with the first thing selected there (or null). Reads the place's radios now and then,
    /// starts the picture again when what it is drawn from has changed, and works a little.
    /// </summary>
    public static void Update(WorldObject? selected)
    {
        Fresh();
        int frame = RealTime.frameCount;
        if (selected != place || frame - lookedFrame >= LookFrames)
        {
            place = selected;
            lookedFrame = frame;
            site = selected != null && selected.Faction is { IsPlayer: true } or null && MayHaveRadio(selected) ? SiteOf(selected) : null;
            ringTiles = site == null ? 0 : RingFor(site);
            if (kind != null) Refresh();
        }
        if (kind == null || !working) return;
        Work(RadioRulesDef.Current.worldCoverBudgetMsPerFrame);
    }

    /// <summary>The world map is not shown: nothing is worked out. The picture is kept for when it is shown again.</summary>
    public static void Hidden()
    {
        place = null;
        site = null;
        lookedFrame = -LookFrames;
    }

    /// <summary>A new game, or another one loaded: nothing of the last one's is kept, and the picture starts off.</summary>
    static void Fresh()
    {
        if (game == Current.Game) return;
        game = Current.Game;
        kind = null;
        place = null;
        site = null;
        LetGo();
    }

    static void LetGo()
    {
        seen = System.Array.Empty<int>();
        nextGeneration = System.Array.Empty<int>();
        next = System.Array.Empty<int>();
        margins = System.Array.Empty<float>();
        queue.Clear();
        painted.Clear();
        combos.Clear();
        working = false;
        hasPass = false;
        layer = null;
        siteId = -1;
        signature = 0;
        Version++;
    }

    /// <summary>A ring at the radio horizon over level ground: from a relay to a home station's mast, from a colony to a vehicle's whip.</summary>
    static int RingFor(WorldCoverSite site)
    {
        double far = site.Relay || site.Caller ? RingHomeMastMetres
            : DefDatabase<RadioFittingDef>.GetNamedSilentFail("PhobosRadio_FittingVehicleControl")?.radio?.antennaHeightMetres ?? RingWhipMetres;
        double metres = Propagation.RadioHorizonMetres(site.HighestMetres(), far);
        return Mathf.Clamp(Mathf.RoundToInt((float)(metres / RadioRulesDef.Current.metresPerWorldTile)), 1, 200);
    }

    /// <summary>Starts the picture again when the place, its radios, the far-end radio or the rules it is drawn from have changed.</summary>
    static void Refresh()
    {
        if (kind == null) return;
        if (site == null || !site.Tile.Valid || site.Tile.LayerDef == null || site.Tile.LayerDef.isSpace)
        {
            // Nothing with a radio is selected: the last picture stays up until another place is, so a look elsewhere does not lose it.
            return;
        }
        var rules = RadioRulesDef.Current;
        int now = SignatureOf(site, kind, rules);
        if (now == signature && hasPass) return;
        signature = now;
        Start(site, kind, rules);
    }

    static int SignatureOf(WorldCoverSite site, RadioCoverKindDef kind, RadioRulesDef rules)
    {
        int hash = Gen.HashCombineInt(kind.shortHash, site.Tile.tileId);
        hash = Gen.HashCombineInt(hash, site.Place.ID);
        hash = Gen.HashCombineInt(hash, Mathf.RoundToInt(rules.metresPerWorldTile));
        hash = Gen.HashCombineInt(hash, rules.terrainBlocksBetweenTiles ? 1 : 0);
        hash = Gen.HashCombineInt(hash, Mathf.RoundToInt(kind.AerialMetres * 10f));
        foreach (RelayNode end in site.Ends)
        {
            hash = Gen.HashCombineInt(hash, Mathf.RoundToInt((end.End.AntennaHeightMetres + (float)end.SiteMetres) * 10f));
            hash = Gen.HashCombineInt(hash, Mathf.RoundToInt(end.End.TransmitPowerWatts * 10f));
            hash = Gen.HashCombineInt(hash, Mathf.RoundToInt(end.End.AntennaGainDbi * 10f));
            hash = Gen.HashCombineInt(hash, Mathf.RoundToInt(end.End.NoiseFigureDb * 10f));
            hash = Gen.HashCombineInt(hash, WorldCoverSite.HasKey(end) ? 1 : 0);
            foreach (BandDef band in end.End.Bands)
                hash = Gen.HashCombineInt(hash, Gen.HashCombineInt(band.shortHash, Mathf.RoundToInt(end.End.FeedLossDb(band) * 10f)));
        }
        return hash == 0 ? 1 : hash;
    }

    static void Start(WorldCoverSite site, RadioCoverKindDef kind, RadioRulesDef rules)
    {
        layer = site.Tile.Layer;
        int tiles = layer.TilesCount;
        if (seen.Length != tiles)
        {
            seen = new int[tiles];
            nextGeneration = new int[tiles];
            next = new int[tiles];
            margins = new float[tiles];
            generation = 0;
        }
        generation++;
        siteId = site.Tile.tileId;
        siteCentre = Find.WorldGrid.GetTileCenter(site.Tile);
        BuildCombos(site, kind);
        radiusTiles = CoverRule.RadiusTiles(site.HighestMetres(), kind.AerialMetres, Propagation.SiteHeightMetres((int)Hilliness.Mountainous),
            rules.metresPerWorldTile, rules.worldCoverMaxTiles);
        queue.Clear();
        painted.Clear();
        done = 0;
        reached = 0;
        hasPass = true;
        working = combos.Count > 0;
        if (working)
        {
            seen[siteId] = generation;
            queue.Enqueue(siteId);
        }
        lastRedraw = RealTime.LastRealTime;
        Version++;
    }

    /// <summary>
    /// Each of the place's ends with the far-end radio on each band they share, as fixed for the picture. The mode is the caller's: the
    /// far radio's own (or the kind's, for Morse) when it calls, and a convoy's when the far end is a relay it calls through.
    /// </summary>
    static void BuildCombos(WorldCoverSite site, RadioCoverKindDef kind)
    {
        combos.Clear();
        CompProperties_Radio? far = kind.Radio;
        if (far == null || Refusal(site, kind) != WorldCoverRefusal.None) return;
        var farSide = new EndSide(Decibels.FromWatts(far.transmitPowerWatts), far.antennaGainDbi, 0);
        foreach (RelayNode end in site.Ends)
        {
            if (kind.needsKey && !WorldCoverSite.HasKey(end)) continue;
            ModeDef? mode = kind.mode ?? (!kind.caller && site.Caller ? end.End.Mode : far.mode);
            if (mode == null) continue;
            double floorAtFar = NoiseFloor.TotalDbm(mode.bandwidthHz, far.noiseFigureDb, double.NegativeInfinity);
            double floorAtSite = NoiseFloor.TotalDbm(mode.bandwidthHz, end.End.NoiseFigureDb, double.NegativeInfinity);
            foreach (BandDef band in far.bands)
            {
                if (!end.End.Bands.Contains(band)) continue;
                combos.Add(new Combo(band, mode, farSide, end.End.Side(band), kind.AerialMetres, end.End.AntennaHeightMetres + end.SiteMetres,
                    floorAtFar, floorAtSite));
            }
        }
    }

    static void Work(float budgetMs)
    {
        if (layer == null) { working = false; return; }
        var rules = RadioRulesDef.Current;
        WorldGrid grid = Find.WorldGrid;
        var siteTile = new PlanetTile(siteId, layer);
        watch.Restart();
        int sinceCheck = 0;
        while (queue.Count > 0)
        {
            int id = queue.Dequeue();
            var tile = new PlanetTile(id, layer);
            // Its neighbours once: the step toward the place (kept), and the tiles to look at next.
            next[id] = TileAir.StepToward(grid, tile, siteCentre, neighbours).tileId;
            nextGeneration[id] = generation;
            foreach (PlanetTile neighbour in neighbours)
            {
                int other = neighbour.tileId;
                if (seen[other] == generation) continue;
                seen[other] = generation;
                if (grid.ApproxDistanceInTiles(siteTile, neighbour) <= radiusTiles) queue.Enqueue(other);
            }
            float margin = Compute(id, tile, siteTile, grid, rules);
            margins[id] = margin;
            if (!float.IsNaN(margin))
            {
                painted.Add(id);
                if (margin >= rules.workableDb) reached++;
            }
            done++;
            if (++sinceCheck < TilesPerClockCheck) continue;
            sinceCheck = 0;
            if (watch.Elapsed.TotalMilliseconds >= budgetMs) break;
        }
        if (queue.Count == 0)
        {
            working = false;
            Version++;
            if (Seen.Wants("radio.world-cover", null, kind?.defName))
                Seen.Note("radio.world-cover", null, reached + " tiles in reach of " + painted.Count + " inside the horizon, " + done + " looked at", kind?.defName);
        }
        else if (RealTime.LastRealTime - lastRedraw >= RedrawSeconds)
        {
            lastRedraw = RealTime.LastRealTime;
            Version++;
        }
    }

    /// <summary>The step from a tile toward the place, found once a pass and kept.</summary>
    static int NextOf(int id, WorldGrid grid)
    {
        if (nextGeneration[id] == generation) return next[id];
        next[id] = TileAir.StepToward(grid, new PlanetTile(id, layer!), siteCentre, neighbours).tileId;
        nextGeneration[id] = generation;
        return next[id];
    }

    /// <summary>
    /// The signal a far-end radio standing on this tile would have to spare with the place, in dB, the best over the place's ends and the
    /// bands they share; NaN where the tile is past every horizon or nothing can stand on it. At the place itself, the picture's full scale.
    /// </summary>
    static float Compute(int id, PlanetTile tile, PlanetTile siteTile, WorldGrid grid, RadioRulesDef rules)
    {
        if (id == siteId) return rules.worldCoverFullScaleDb;
        Hilliness hilliness = layer![id].hilliness;
        if (hilliness == Hilliness.Impassable) return float.NaN;
        double farSite = Propagation.SiteHeightMetres((int)hilliness);
        double metres = grid.ApproxDistanceInTiles(tile, siteTile) * rules.metresPerWorldTile;
        // The ground on the way, over the tiles a walk from here to the place takes: both ends' own tiles left out, as a contact's are.
        profile.Clear();
        if (rules.terrainBlocksBetweenTiles)
        {
            int steps = 0;
            int at = NextOf(id, grid);
            int last = id;
            while (at != siteId)
            {
                if (at == last || ++steps > TileAir.MaxWalkTiles)
                {
                    // The walk does not arrive: a contact reads no ground then, and neither does the picture.
                    profile.Clear();
                    break;
                }
                profile.Add(TerrainRule.GroundHeightMetres((int)layer[at].hilliness));
                last = at;
                at = NextOf(at, grid);
            }
        }
        double best = double.NaN;
        double figure = double.NegativeInfinity;
        double figureFar = double.NaN, figureSite = double.NaN;
        foreach (Combo combo in combos)
        {
            double heightFar = combo.FarAerialMetres + farSite;
            if (metres > Propagation.RadioHorizonMetres(heightFar, combo.SiteHeightMetres)) continue;
            if (heightFar != figureFar || combo.SiteHeightMetres != figureSite)
            {
                figureFar = heightFar;
                figureSite = combo.SiteHeightMetres;
                figure = profile.Count == 0 ? double.NegativeInfinity : TerrainRule.Worst(profile, metres, heightFar, combo.SiteHeightMetres).Figure;
            }
            LinkBudgetService.WorldLosses(metres, combo.Band, heightFar, combo.SiteHeightMetres, figure, rules, out double pathLoss, out double obstruction);
            var forward = LinkBudgetService.Inputs(combo.FarSide, combo.SiteSide, pathLoss, obstruction, combo.FloorAtSiteDbm, combo.Mode);
            var reverse = LinkBudgetService.Inputs(combo.SiteSide, combo.FarSide, pathLoss, obstruction, combo.FloorAtFarDbm, combo.Mode);
            double margin = LinkMath.TwoWayMarginDb(forward, reverse);
            if (double.IsNaN(best) || margin > best) best = margin;
        }
        return (float)best;
    }

    /// <summary>One of the place's ends with the far-end radio on one band: what is fixed for the picture.</summary>
    readonly struct Combo
    {
        public readonly BandDef Band;
        public readonly ModeDef Mode;
        public readonly EndSide FarSide;
        public readonly EndSide SiteSide;
        public readonly double FarAerialMetres;
        public readonly double SiteHeightMetres;
        public readonly double FloorAtFarDbm;
        public readonly double FloorAtSiteDbm;

        public Combo(BandDef band, ModeDef mode, EndSide farSide, EndSide siteSide, double farAerialMetres, double siteHeightMetres,
            double floorAtFarDbm, double floorAtSiteDbm)
        {
            Band = band;
            Mode = mode;
            FarSide = farSide;
            SiteSide = siteSide;
            FarAerialMetres = farAerialMetres;
            SiteHeightMetres = siteHeightMetres;
            FloorAtFarDbm = floorAtFarDbm;
            FloorAtSiteDbm = floorAtSiteDbm;
        }
    }

    /// <summary>The places the radio's core knows: a colony of the player's with a station, a relay mast, a relay post.</summary>
    sealed class CoreSites : IWorldCoverSiteSource
    {
        public bool MayHaveRadio(WorldObject place) => place switch
        {
            WorldObject_PhobosRelayMast => true,
            Outpost_PhobosRelayPost post => post.Faction is { IsPlayer: true },
            MapParent { HasMap: true } home => home.Map.IsPlayerHome && RadioNet.For(home.Map).Stations.Count > 0,
            _ => false,
        };

        public WorldCoverSite? SiteOf(WorldObject place) => place switch
        {
            WorldObject_PhobosRelayMast mast => mast.Node() is { } node ? new WorldCoverSite(place, new List<RelayNode> { node }, relay: true) : null,
            Outpost_PhobosRelayPost post => post.Node() is { } node ? new WorldCoverSite(place, new List<RelayNode> { node }, relay: true) : null,
            MapParent { HasMap: true } home => new WorldCoverSite(place, FactionDoctors.StationEnds(home.Map)),
            _ => null,
        };
    }
}
