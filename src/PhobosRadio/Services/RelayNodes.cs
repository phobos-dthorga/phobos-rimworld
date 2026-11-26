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
/// mast always), the height its tile's ground lifts it by (its source supplies it: Propagation.SiteHeightMetres), and since Radio 0.24.0
/// how many channels it has to relay on (0: the rules' <see cref="RadioRulesDef.repeaterChannels"/>).
/// </summary>
public sealed record RelayNode(RadioEndpoint End, PlanetTile Tile, string Label, object Owner, bool Repeater = false, double SiteMetres = 0, int Channels = 0)
{
    /// <summary>The height a tile's ground lifts a node on it by.</summary>
    public static double SiteOf(PlanetTile tile) => Propagation.SiteHeightMetres((int)Find.WorldGrid[tile].hilliness);

    /// <summary>The channels it relays on.</summary>
    public int ChannelsOrRules => Channels > 0 ? Channels : RadioRulesDef.Current.repeaterChannels;
}

/// <summary>Something that offers relays: the vehicles tie-in (halted convoys, parked vehicles), relay masts, home stations with a
/// repeater, relay posts.</summary>
public interface IRelaySource
{
    IEnumerable<RelayNode> Nodes();
}

/// <summary>
/// The relays on the world map, from every registered source (Radio 0.22.0; radio-autonomy's relay design). A relay is a hop, never a
/// second system: every hop goes through <see cref="LinkBudgetService.EvaluateWorld"/>, and <see cref="RelayRule"/> picks the chain whose
/// worst hop has the most to spare. Since Radio 0.24.0 a relayed call takes channels at each relay it passes through (<see cref="RelayLedger"/>):
/// a relay with no room for the caller's kind is left out of its search, and the chain found holds its places until the caller stops asking.
/// <para>Cost: the ledger is a few lists, asked once a relay a search; nothing runs on its own and nothing is saved.</para>
/// </summary>
public static class RelayNodes
{
    static readonly List<IRelaySource> sources = new() { new RelayMastSource(), new HomeStationRelaySource(), new RelayPostSource() };
    static readonly RelayLedger ledger = new();
    static readonly Dictionary<object, string> busy = new();
    static Game? ledgerGame;
    static KindShape[]? shapes;

    public static void Register(IRelaySource source)
    {
        if (!sources.Contains(source)) sources.Add(source);
    }

    /// <summary>Every relay now that may pass a call on.</summary>
    public static IEnumerable<RelayNode> All() => sources.SelectMany(s => s.Nodes()).Where(CanRelay);

    /// <summary>The ledger of this game: started afresh when a game is loaded or begun, since nothing in it is saved.</summary>
    static RelayLedger Ledger
    {
        get
        {
            if (ledgerGame != Current.Game)
            {
                ledger.Clear();
                busy.Clear();
                ledgerGame = Current.Game;
            }
            return ledger;
        }
    }

    /// <summary>Each channel kind's shape, as a station's ledger counts it.</summary>
    static KindShape[] Shapes
    {
        get
        {
            if (shapes != null) return shapes;
            var rules = RadioRulesDef.Current;
            var kinds = ChannelKindDef.InOrder;
            shapes = new KindShape[kinds.Count];
            for (var k = 0; k < kinds.Count; k++)
                shapes[k] = new KindShape(Channels.PerUse(kinds[k].Mode.bandwidthHz, rules.channelWidthHz), Channels.RadiosPerUse(kinds[k].Mode.dutyCycle), kinds[k].preempts);
            return shapes;
        }
    }

    static int KindOf(RelayNode node) => (node.End.Thing?.TryGetComp<CompRadio>()?.Kind ?? ChannelKindDef.LocalVoice).LedgerIndex;

    /// <summary>How many calls pass through the relay this owns now, and the channels they hold there.</summary>
    public static (int Calls, int Held) Use(object owner) => Ledger.Use(owner, Shapes, Find.TickManager.TicksGame);

    /// <summary>A relay's line for its inspect pane: the calls through it and its channels in use, or null with none.</summary>
    public static string? UseLine(object owner, int channels)
    {
        var (calls, held) = Use(owner);
        if (calls == 0) return null;
        return (held >= channels ? "PhobosRadio_RelayUseFull" : "PhobosRadio_RelayUse").Translate(calls.Named("CALLS"), held.Named("HELD"), channels.Named("CHANNELS")).Resolve();
    }

    /// <summary>The relays that would have carried this caller's last call but had no room, as a list for the player, or null.</summary>
    public static string? BusyFor(object caller) => busy.TryGetValue(caller, out var names) && ledgerGame == Current.Game ? names : null;

    /// <summary>
    /// Whether this node may pass a call on between tiles: the one place relaying is gated (the owner's card of 26 Nov 2026, "Two kinds, by
    /// physics": between tiles it needs a repeater). Its source declares one, or its radio relays by itself.
    /// </summary>
    public static bool CanRelay(RelayNode node) => node.Repeater || node.End.Thing?.TryGetComp<CompRadio>()?.Props.relays == true;

    /// <summary>
    /// The best chain from one end to any of the targets, through the relays, on the world map: the hops' distances at the rules' metres
    /// a tile, each evaluated once, the chain at most the rules' hops. Null when nothing reaches. A relay with no room for the caller (the
    /// owner of <paramref name="from"/>) is left out; the chain found holds a place at each of its relays for the rules' lease, and a caller
    /// that finds none lets go of its places. A target is any end the caller names: home's stations for "reach home", or one colony's alone,
    /// so another colony's station with a repeater relays toward it.
    /// </summary>
    public static (RelayRule.Chain chain, List<RelayNode> nodes)? BestChain(RelayNode from, IReadOnlyList<RelayNode> targets, IEnumerable<RelayNode> relays)
    {
        var rules = RadioRulesDef.Current;
        int now = Find.TickManager.TicksGame;
        int kind = KindOf(from);
        RelayLedger leases = Ledger;
        var full = new List<RelayNode>();
        var nodes = new List<RelayNode> { from };
        foreach (RelayNode relay in relays)
        {
            if (relay.Owner == from.Owner) continue;
            if (leases.HasRoom(relay.Owner, from.Owner, kind, Shapes, relay.ChannelsOrRules, now)) nodes.Add(relay);
            else full.Add(relay);
        }
        int firstTarget = nodes.Count;
        nodes.AddRange(targets);
        double? Hop(int a, int b)
        {
            double metres = Find.WorldGrid.ApproxDistanceInTiles(nodes[a].Tile, nodes[b].Tile) * rules.metresPerWorldTile;
            return LinkBudgetService.EvaluateWorld(nodes[a].End, nodes[b].End, metres, rules, nodes[a].SiteMetres, nodes[b].SiteMetres) is { InTouch: true } link
                ? link.MarginDb : null;
        }
        RelayRule.Chain chain = RelayRule.Best(nodes.Count, 0, i => i >= firstTarget, i => i < firstTarget, Hop, rules.maxRelayHops);
        busy.Remove(from.Owner);
        if (!chain.Found)
        {
            leases.Release(from.Owner);
            if (full.Count > 0)
            {
                busy[from.Owner] = full.Select(r => r.Label).Distinct().ToCommaList(useAnd: true);
                if (Seen.Wants("radio.relay-busy", from.End.Thing)) Seen.Note("radio.relay-busy", from.End.Thing, busy[from.Owner]);
            }
            return null;
        }
        var held = chain.Path.Skip(1).Take(chain.Relays).Select(i => nodes[i]).ToList();
        leases.Hold(from.Owner, kind, held.Select(r => r.Owner), Shapes, owner => held.First(r => r.Owner == owner).ChannelsOrRules, now, now + rules.relayLeaseTicks);
        foreach (RelayNode relay in held)
        {
            if (relay.Owner is Outpost_PhobosRelayPost && Seen.Wants("radio.relay-post", from.End.Thing)) Seen.Note("radio.relay-post", from.End.Thing, relay.Label);
            else if (relay.Owner is Thing { Spawned: true } && Seen.Wants("radio.relay-station", from.End.Thing)) Seen.Note("radio.relay-station", from.End.Thing, relay.Label);
        }
        return (chain, nodes);
    }
}

/// <summary>
/// The colony's own stations as relays between tiles (Radio 0.24.0; the owner's cards of 26 and 27 Nov 2026: "home station" among the four
/// relay sites, and "Station upgrade" for how one becomes a relay): each powered station on a home map with the repeater upgrade fitted.
/// It passes on only a call aimed at another colony, since a call that reaches any colony is home already.
/// </summary>
public sealed class HomeStationRelaySource : IRelaySource
{
    public IEnumerable<RelayNode> Nodes()
    {
        if (Find.Maps == null) yield break;
        foreach (Map map in Find.Maps)
        {
            if (!map.IsPlayerHome) continue;
            foreach (CompRadio station in RadioNet.For(map).Stations)
            {
                if (RadioUpgrades.Figure(station.parent, RadioUpgrades.Repeater, 0f) < 1f) continue;
                if (station.parent.TryGetComp<CompPowerTrader>() is { PowerOn: false }) continue;
                foreach (RadioEndpoint end in RadioNet.StationEndpoints(station, RadioMod.Settings.metresPerCell))
                    yield return new RelayNode(end, map.Tile, map.Parent.LabelCap, station.parent, Repeater: true, SiteMetres: RelayNode.SiteOf(map.Tile));
            }
        }
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
        if (RelayNodes.UseLine(this, RadioRulesDef.Current.repeaterChannels) is { } use) line += "\n" + use;
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
