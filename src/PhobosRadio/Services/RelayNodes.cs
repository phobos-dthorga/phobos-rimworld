using System.Collections.Generic;
using System.Linq;
using Phobos.Radio.Physics;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

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
    /// so another colony's station with a repeater relays toward it. <paramref name="leaseHolder"/> (Radio 0.24.1) is whose places these
    /// are, the caller's own by default: a second search for the same convoy aimed at one colony books under its own name, so it neither
    /// takes over nor lets go of the places the convoy's own reach home holds. A convoy calling home and one colony at once holds two calls.
    /// </summary>
    public static (RelayRule.Chain chain, List<RelayNode> nodes)? BestChain(RelayNode from, IReadOnlyList<RelayNode> targets, IEnumerable<RelayNode> relays,
        object? leaseHolder = null)
    {
        var rules = RadioRulesDef.Current;
        int now = Find.TickManager.TicksGame;
        int kind = KindOf(from);
        object holder = leaseHolder ?? from.Owner;
        RelayLedger leases = Ledger;
        var full = new List<RelayNode>();
        var nodes = new List<RelayNode> { from };
        foreach (RelayNode relay in relays)
        {
            if (relay.Owner == from.Owner) continue;
            if (leases.HasRoom(relay.Owner, holder, kind, Shapes, relay.ChannelsOrRules, now)) nodes.Add(relay);
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
        // The busy line is the convoy's own reach home's; a search aimed at one colony leaves it alone.
        bool own = Equals(holder, from.Owner);
        if (own) busy.Remove(from.Owner);
        if (!chain.Found)
        {
            leases.Release(holder);
            if (own && full.Count > 0)
            {
                busy[from.Owner] = full.Select(r => r.Label).Distinct().ToCommaList(useAnd: true);
                if (Seen.Wants("radio.relay-busy", from.End.Thing)) Seen.Note("radio.relay-busy", from.End.Thing, busy[from.Owner]);
            }
            return null;
        }
        var held = chain.Path.Skip(1).Take(chain.Relays).Select(i => nodes[i]).ToList();
        // A repeater station on a map that this call starts to pass through now, not one it held already (Radio 0.30.0, S166).
        var starting = held.Where(r => r.Owner is Thing { Spawned: true } && !leases.Holds(r.Owner, holder, now)).Select(r => (Thing)r.Owner).ToList();
        leases.Hold(holder, kind, held.Select(r => r.Owner), Shapes, owner => held.First(r => r.Owner == owner).ChannelsOrRules, now, now + rules.relayLeaseTicks);
        // Its courtesy tone and squelch tail there, heard by whoever stands near it (the owner's card of 27 Nov 2026, "At the repeater on its map").
        foreach (Thing station in starting)
        {
            RadioSoundDefOf.PhobosRadio_RepeaterCourtesy.PlayOneShot(new TargetInfo(station.Position, station.Map));
            Seen.Note("radio.repeater-courtesy", station);
        }
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
            if (thing is WorldObject_PhobosRelayMast { Destroyed: false, Works: true } mast && mast.Radio is { } radio)
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
    /// <summary>How sound the mast is, 1 new (Radio 0.37.0): it wears in the weather while the losses setting is on.</summary>
    float condition = 1f;
    /// <summary>Components a convoy spends to service it (authored).</summary>
    public const int ServiceComponents = 1;
    const int LookTicks = 2500;

    /// <summary>Whether it passes calls on: always, unless the losses setting is on and it is worn out.</summary>
    public bool Works => !RelayMastLosses.On || RelayMastRule.Works(condition);

    /// <summary>
    /// Once a game hour, with the losses setting on (Radio 0.37.0; the owner's cards of 27 Nov 2026, "Raids and wear", off by default): it
    /// wears a little, and raiders from a hostile settlement in reach may knock it down (<see cref="RelayMastRule"/>). Cost: a modulo each
    /// tick; once an hour, a walk over the world's settlements.
    /// </summary>
    protected override void Tick()
    {
        base.Tick();
        if (!RelayMastLosses.On || Destroyed || (Find.TickManager.TicksGame + ID) % LookTicks != 0) return;
        bool worked = RelayMastRule.Works(condition);
        condition = (float)RelayMastRule.Worn(condition, LookTicks / (double)GenDate.TicksPerDay);
        if (worked && !RelayMastRule.Works(condition))
        {
            Messages.Message("PhobosRadio_RelayWornOut".Translate(LabelCap.Named("MAST")), this, MessageTypeDefOf.NegativeEvent);
            if (Seen.Wants("radio.relay-mast-lost", null, "worn")) Seen.Note("radio.relay-mast-lost", null, LabelCap, "worn");
        }
        double nearest = double.MaxValue;
        Faction? raiders = null;
        foreach (Settlement town in Find.WorldObjects.Settlements)
        {
            if (town.Faction == null || !town.Faction.HostileTo(Faction.OfPlayer) || town.Tile.Layer != Tile.Layer) continue;
            double tiles = Find.WorldGrid.ApproxDistanceInTiles(Tile, town.Tile);
            if (tiles >= nearest) continue;
            nearest = tiles;
            raiders = town.Faction;
        }
        if (raiders == null || !Rand.MTBEventOccurs((float)RelayMastRule.RaidMeanDays(nearest), GenDate.TicksPerDay, LookTicks)) return;
        Find.LetterStack.ReceiveLetter("PhobosRadio_RelayRaidedLabel".Translate(), "PhobosRadio_RelayRaided".Translate(LabelCap.Named("MAST"),
            raiders.Name.Named("FACTION")), LetterDefOf.NegativeEvent, new GlobalTargetInfo(Tile));
        if (Seen.Wants("radio.relay-mast-lost", null, "raided")) Seen.Note("radio.relay-mast-lost", null, LabelCap, "raided");
        Destroy();
    }

    /// <summary>Why this convoy cannot service the mast now, or null.</summary>
    public string? ServiceRefusal(Caravan caravan)
    {
        if (condition >= 0.999f) return "PhobosRadio_RelayServiceNotNeeded".Translate().Resolve();
        int have = CaravanInventoryUtility.AllInventoryItems(caravan).Where(t => t.def == ThingDefOf.ComponentIndustrial).Sum(t => t.stackCount);
        return have < ServiceComponents ? "PhobosRadio_RelayServiceShort".Translate(ServiceComponents.Named("COUNT")).Resolve() : null;
    }

    /// <summary>A convoy on its tile services it: a component spent, the mast as new.</summary>
    public void Service(Caravan caravan)
    {
        if (ServiceRefusal(caravan) is { } why)
        {
            Messages.Message(why, caravan, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        int left = ServiceComponents;
        foreach (Thing thing in CaravanInventoryUtility.AllInventoryItems(caravan).Where(t => t.def == ThingDefOf.ComponentIndustrial).ToList())
        {
            if (left <= 0) break;
            int take = Mathf.Min(left, thing.stackCount);
            thing.SplitOff(take).Destroy();
            left -= take;
        }
        condition = 1f;
        if (Seen.Wants("radio.relay-mast-lost", null, "serviced")) Seen.Note("radio.relay-mast-lost", null, LabelCap, "serviced");
        Messages.Message("PhobosRadio_RelayServiced".Translate(LabelCap.Named("MAST")), this, MessageTypeDefOf.TaskCompletion, historical: false);
    }

    public override IEnumerable<Gizmo> GetCaravanGizmos(Caravan caravan)
    {
        foreach (Gizmo gizmo in base.GetCaravanGizmos(caravan)) yield return gizmo;
        if (!RelayMastLosses.On || caravan.Faction != Faction.OfPlayer) yield break;
        var service = new Command_Action
        {
            defaultLabel = "PhobosRadio_RelayServiceGizmo".Translate(),
            defaultDesc = "PhobosRadio_RelayServiceGizmoDesc".Translate(ServiceComponents.Named("COUNT")),
            icon = ContentFinder<Texture2D>.Get("Phobos/Radio/World/RelayMast", reportFailure: false) ?? BaseContent.BadTex,
            action = () => Service(caravan),
        };
        if (ServiceRefusal(caravan) is { } refused) service.Disable(refused);
        yield return service;
    }

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
        if (RelayMastLosses.On)
            line += "\n" + (Works ? "PhobosRadio_RelayCondition" : "PhobosRadio_RelayConditionWornOut").Translate(condition.ToStringPercent().Named("CONDITION")).Resolve();
        if (RelayNodes.UseLine(this, RadioRulesDef.Current.repeaterChannels) is { } use) line += "\n" + use;
        return text.NullOrEmpty() ? line : text + "\n" + line;
    }

    public ThingOwner GetDirectlyHeldThings() => Held;

    public void GetChildHolders(List<IThingHolder> outChildren) => ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref held, "PhobosRadio_RelayHeld", this);
        Scribe_Values.Look(ref condition, "PhobosRadio_RelayCondition", 1f);
    }
}

[DefOf]
public static class RelayDefOf
{
    public static WorldObjectDef PhobosRadio_RelayMast = null!;
    public static ThingDef PhobosRadio_RelayUnit = null!;

    static RelayDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(RelayDefOf));
}
