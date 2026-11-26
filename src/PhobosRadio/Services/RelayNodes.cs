using System.Collections.Generic;
using System.Linq;
using Phobos.Radio.Physics;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Phobos.Radio.Services;

/// <summary>
/// One place a call can be passed on from, on the world map: its radio end, its tile, a label for the player, what owns it (a convoy, a
/// vehicle, a mast), so a convoy never relays its own call, whether its source declares a repeater on it (a vehicle's repeater kit; a
/// mast always), and the height its tile's ground lifts it by (its source supplies it: Propagation.SiteHeightMetres).
/// </summary>
public sealed record RelayNode(RadioEndpoint End, PlanetTile Tile, string Label, object Owner, bool Repeater = false, double SiteMetres = 0)
{
    /// <summary>The height a tile's ground lifts a node on it by.</summary>
    public static double SiteOf(PlanetTile tile) => Propagation.SiteHeightMetres((int)Find.WorldGrid[tile].hilliness);
}

/// <summary>Something that offers relays: the vehicles tie-in (halted convoys, parked vehicles), relay masts, later outposts and stations.</summary>
public interface IRelaySource
{
    IEnumerable<RelayNode> Nodes();
}

/// <summary>
/// The relays on the world map, from every registered source (Radio 0.22.0; radio-autonomy's relay design). A relay is a hop, never a
/// second system: every hop goes through <see cref="LinkBudgetService.EvaluateWorld"/>, and <see cref="RelayRule"/> picks the chain whose
/// worst hop has the most to spare. Relayed calls take no channels at a relay yet (the design leans two: a limit of this first slice).
/// </summary>
public static class RelayNodes
{
    static readonly List<IRelaySource> sources = new() { new RelayMastSource() };

    public static void Register(IRelaySource source)
    {
        if (!sources.Contains(source)) sources.Add(source);
    }

    /// <summary>Every relay now that may pass a call on.</summary>
    public static IEnumerable<RelayNode> All() => sources.SelectMany(s => s.Nodes()).Where(CanRelay);

    /// <summary>
    /// Whether this node may pass a call on between tiles: the one place relaying is gated (the owner's card of 26 Nov 2026, "Two kinds, by
    /// physics": between tiles it needs a repeater). Its source declares one, or its radio relays by itself.
    /// </summary>
    public static bool CanRelay(RelayNode node) => node.Repeater || node.End.Thing?.TryGetComp<CompRadio>()?.Props.relays == true;

    /// <summary>
    /// The best chain from one end to any of the targets, through the relays, on the world map: the hops' distances at the rules' metres
    /// a tile, each evaluated once, the chain at most the rules' hops. Null when nothing reaches.
    /// </summary>
    public static (RelayRule.Chain chain, List<RelayNode> nodes)? BestChain(RelayNode from, IReadOnlyList<RelayNode> targets, IEnumerable<RelayNode> relays)
    {
        var rules = RadioRulesDef.Current;
        var nodes = new List<RelayNode> { from };
        nodes.AddRange(relays.Where(r => r.Owner != from.Owner));
        int firstTarget = nodes.Count;
        nodes.AddRange(targets);
        double? Hop(int a, int b)
        {
            double metres = Find.WorldGrid.ApproxDistanceInTiles(nodes[a].Tile, nodes[b].Tile) * rules.metresPerWorldTile;
            return LinkBudgetService.EvaluateWorld(nodes[a].End, nodes[b].End, metres, rules, nodes[a].SiteMetres, nodes[b].SiteMetres) is { InTouch: true } link
                ? link.MarginDb : null;
        }
        RelayRule.Chain chain = RelayRule.Best(nodes.Count, 0, i => i >= firstTarget, i => i < firstTarget, Hop, rules.maxRelayHops);
        return chain.Found ? (chain, nodes) : null;
    }
}

/// <summary>The relay masts standing on the world map, as relays.</summary>
public sealed class RelayMastSource : IRelaySource
{
    public IEnumerable<RelayNode> Nodes()
    {
        if (Find.WorldObjects == null) yield break;
        foreach (WorldObject thing in Find.WorldObjects.AllWorldObjects)
        {
            if (thing is WorldObject_PhobosRelayMast { Destroyed: false } mast && mast.Radio is { } radio)
                yield return new RelayNode(RadioEndpoint.BuiltIn(radio.parent, IntVec3.Zero, radio), mast.Tile, mast.LabelCap, mast, Repeater: true,
                    SiteMetres: RelayNode.SiteOf(mast.Tile));
        }
    }
}

/// <summary>
/// A relay mast standing on a world tile (Radio 0.22.0; the owner's card of 26 Nov 2026: "A relay mast built on a tile"): a small solar
/// mast with a relay radio, built by a convoy at a halt, passing calls on until it is taken down. Its radio is a thing it holds (never on a
/// map), so a hop to it goes through the one link budget like any other. No power, weather or wear: a simplification of this first slice.
/// </summary>
public class WorldObject_PhobosRelayMast : WorldObject, IThingHolder
{
    ThingOwner<Thing>? held;

    ThingOwner<Thing> Held => held ??= new ThingOwner<Thing>(this, oneStackOnly: true);

    /// <summary>The mast's relay radio, or null.</summary>
    public CompRadio? Radio => Held.FirstOrDefault()?.TryGetComp<CompRadio>();

    public static WorldObject_PhobosRelayMast Make(PlanetTile tile)
    {
        var mast = (WorldObject_PhobosRelayMast)WorldObjectMaker.MakeWorldObject(RelayDefOf.PhobosRadio_RelayMast);
        mast.Tile = tile;
        mast.Held.TryAdd(ThingMaker.MakeThing(RelayDefOf.PhobosRadio_RelayUnit));
        Find.WorldObjects.Add(mast);
        return mast;
    }

    /// <summary>Whether a relay mast stands on this tile already.</summary>
    public static bool At(PlanetTile tile) => Find.WorldObjects.ObjectsAt(tile).Any(o => o is WorldObject_PhobosRelayMast { Destroyed: false });

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos()) yield return gizmo;
        yield return new Command_Action
        {
            defaultLabel = "PhobosRadio_RelayTakeDown".Translate(),
            defaultDesc = "PhobosRadio_RelayTakeDownDesc".Translate(),
            icon = ContentFinder<Texture2D>.Get("UI/Designators/Deconstruct", reportFailure: false) ?? BaseContent.BadTex,
            action = () =>
            {
                Messages.Message("PhobosRadio_RelayTakenDown".Translate(LabelCap.Named("MAST")), MessageTypeDefOf.NeutralEvent, historical: false);
                Destroy();
            },
        };
    }

    public override string GetInspectString()
    {
        string text = base.GetInspectString();
        string line = "PhobosRadio_RelayInspect".Translate((Radio?.Props.antennaHeightMetres ?? 0f).ToString("0").Named("METRES")).Resolve();
        return text.NullOrEmpty() ? line : text + "\n" + line;
    }

    public ThingOwner GetDirectlyHeldThings() => Held;

    public void GetChildHolders(List<IThingHolder> outChildren) => ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref held, "PhobosRadio_RelayHeld", this);
    }
}

[DefOf]
public static class RelayDefOf
{
    public static WorldObjectDef PhobosRadio_RelayMast = null!;
    public static ThingDef PhobosRadio_RelayUnit = null!;

    static RelayDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(RelayDefOf));
}
