using System.Collections.Generic;
using Phobos.Radio.Physics;
using RimWorld.Planet;
using Verse;

namespace Phobos.Radio.Services;

/// <summary>
/// One hop between two world tiles, laid out for the link sheet and its side view (Radio 0.48.0; the owner's picks of 27 Nov 2026: the
/// path profile, and naming the ground in the way; docs/development/radio-world-coverage.md): the link as a contact decides it
/// (<see cref="LinkBudgetService.EvaluateWorld(RadioEndpoint, PlanetTile, double, RadioEndpoint, PlanetTile, double, RadioRulesDef, out WorldTerrain.Blocking, ModeDef?)"/>),
/// the ground of the tiles on the way as that contact read it, and which of them decides.
/// <para>Cost: built only while a link sheet is open, when the chain it shows changes: one link and two walks between the tiles.</para>
/// </summary>
public sealed class WorldLinkPath
{
    public readonly RelayNode A;
    public readonly RelayNode B;
    /// <summary>The link, or null when the two radios share no band.</summary>
    public readonly LinkEvaluation? Link;
    public readonly WorldTerrain.Blocking Blocking;
    public readonly double Metres;
    /// <summary>Each end's aerial over its tile's plain: aerial and site together.</summary>
    public readonly double HeightAMetres;
    public readonly double HeightBMetres;
    public readonly double HorizonMetres;
    /// <summary>The ground of the tiles between the two ends, in order from A, taken as evenly spaced; empty for neighbours.</summary>
    public readonly List<double> Ground = new();
    /// <summary>Which entry of <see cref="Ground"/> decides, or -1 when nothing stands high enough to matter.</summary>
    public readonly int EdgeIndex = -1;
    /// <summary>What the deciding ground costs on the link's band, in dB; 0 with none, or past the horizon, where it no longer matters.</summary>
    public readonly double TerrainLossDb;
    /// <summary>How far the deciding ground is from A, in metres.</summary>
    public readonly double EdgeFromAMetres;

    public bool OverHorizon => Metres > HorizonMetres;
    public LinkOutcome Outcome => Link?.Outcome ?? LinkOutcome.NoContact;

    WorldLinkPath(RelayNode a, RelayNode b, RadioRulesDef rules, ModeDef? mode)
    {
        A = a;
        B = b;
        HeightAMetres = a.End.AntennaHeightMetres + a.SiteMetres;
        HeightBMetres = b.End.AntennaHeightMetres + b.SiteMetres;
        HorizonMetres = Propagation.RadioHorizonMetres(HeightAMetres, HeightBMetres);
        Metres = Find.WorldGrid.ApproxDistanceInTiles(a.Tile, b.Tile) * rules.metresPerWorldTile;
        Link = LinkBudgetService.EvaluateWorld(a.End, a.Tile, a.SiteMetres, b.End, b.Tile, b.SiteMetres, rules, out Blocking, mode);
        if (!WorldTerrain.Profile(a.Tile, b.Tile, Ground)) return;
        if (!rules.terrainBlocksBetweenTiles) return;
        TerrainRule.Edge edge = TerrainRule.Worst(Ground, Metres, HeightAMetres, HeightBMetres);
        if (!edge.Any) return;
        EdgeIndex = edge.Index;
        EdgeFromAMetres = Metres * (edge.Index + 1) / (Ground.Count + 1);
        if (Link != null && !OverHorizon) TerrainLossDb = Blocking.LossDb(Link.Band);
    }

    public static WorldLinkPath For(RelayNode a, RelayNode b, RadioRulesDef rules, ModeDef? mode = null) => new(a, b, rules, mode);

    /// <summary>Whether the ground in the way is what loses this link: inside the horizon, not in touch, and in touch without the ground.</summary>
    public bool GroundLosesIt(RadioRulesDef rules) =>
        Link != null && !Link.InTouch && !OverHorizon && TerrainLossDb > 0 && Link.MarginDb + TerrainLossDb >= rules.workableDb;
}
