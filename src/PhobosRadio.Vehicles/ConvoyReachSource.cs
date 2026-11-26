using System.Collections.Generic;
using System.Linq;
using Phobos.Radio.Physics;
using Phobos.Radio.Services;
using Phobos.VehicleSystems;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.Radio.Vehicles;

/// <summary>
/// A convoy's reach home on the world map (Radio 0.21.0, the first step of phase 2; D'thorga Framework's EM33; the owner's cards of 26 Nov
/// 2026), through relays since Radio 0.22.0. The convoy's best fitted vehicle radio, its antenna lifted by a Datum's mast when one is up, to
/// each powered station on each of the colony's home maps, directly or through relays (<see cref="RelayNodes"/>), each hop through the one
/// link budget (<see cref="LinkBudgetService.EvaluateWorld"/>), the chain as good as its worst hop.
/// <para>Cost: asked only when D'thorga Framework asks (a convoy's line, an ambush, an airdrop's targets, a supply run's look), at most once
/// a tenth of a game hour for each convoy, and then one budget a pair of nodes, for a handful of nodes and three hops. No map is walked;
/// nothing runs on its own.</para>
/// </summary>
public sealed class ConvoyReachSource : IConvoyReachSource
{
    /// <summary>What a relay mast costs to put up, from the convoy's cargo (agent's figures).</summary>
    public const int MastSteel = 60;
    public const int MastComponents = 4;

    /// <summary>The convoy's best fitted vehicle radio and its end, its aerial at the mast when one is up; null with no radio aboard.</summary>
    public static RelayNode? ConvoyEnd(Caravan convoy, float mastMetres)
    {
        VehiclePawn? carrier = null;
        CompRadio? radio = null;
        foreach (VehiclePawn vehicle in (convoy as VehicleCaravan)?.VehiclesListForReading ?? new List<VehiclePawn>())
        {
            if (vehicle.GetComp<CompRadio>() is not { } fitted || !VehicleRadios.Fitted(vehicle)) continue;
            if (radio == null || fitted.Props.transmitPowerWatts > radio.Props.transmitPowerWatts)
            {
                carrier = vehicle;
                radio = fitted;
            }
        }
        if (carrier == null || radio == null) return null;
        float height = mastMetres > radio.Props.antennaHeightMetres ? mastMetres : radio.Props.antennaHeightMetres;
        var end = new RadioEndpoint(carrier, IntVec3.Zero, radio, radio.Props.bands, height, radio.Props.antennaGainDbi, new List<FeedSegment>(), carrier.LabelShort);
        return new RelayNode(end, convoy.Tile, convoy.LabelCap, convoy, Repeater: VehicleRadios.Repeats(carrier), SiteMetres: RelayNode.SiteOf(convoy.Tile));
    }

    /// <summary>
    /// How much a convoy's call to one colony's stations alone has to spare, through any relay, or null when it does not reach (Radio
    /// 0.24.1; the owner's card of 27 Nov 2026, "Make it work": another colony's station with the repeater upgrade relays toward this one).
    /// Its relay places are booked under the convoy and that colony together, apart from the convoy's own reach home. Asked only when the
    /// colony the convoy reaches first could not help (advice by radio), so with one colony it never runs.
    /// </summary>
    public static double? MarginTo(Caravan convoy, Map colony)
    {
        if (ConvoyEnd(convoy, ConvoyRadio.Mast(convoy)) is not { } start) return null;
        List<RelayNode> targets = HomeEnds(colony);
        if (targets.Count == 0) return null;
        return RelayNodes.BestChain(start, targets, RelayNodes.All().ToList(), leaseHolder: (convoy, colony)) is { } found ? found.chain.WorstMarginDb : null;
    }

    /// <summary>Each powered station's ends on each home map, or on one map only, as targets.</summary>
    static List<RelayNode> HomeEnds(Map? only = null)
    {
        var ends = new List<RelayNode>();
        foreach (Map map in Find.Maps.Where(m => m.IsPlayerHome && (only == null || m == only)))
        {
            foreach (CompRadio station in RadioNet.For(map).Stations)
            {
                if (station.parent.TryGetComp<CompPowerTrader>() is { PowerOn: false }) continue;
                foreach (RadioEndpoint end in RadioNet.StationEndpoints(station, RadioMod.Settings.metresPerCell))
                    ends.Add(new RelayNode(end, map.Tile, map.Parent.LabelCap, map, SiteMetres: RelayNode.SiteOf(map.Tile)));
            }
        }
        return ends;
    }

    public ConvoyReach? Reach(Caravan convoy, float mastMetres)
    {
        if (ConvoyEnd(convoy, mastMetres) is not { } start) return null;
        List<RelayNode> homes = HomeEnds();
        if (homes.Count == 0) return new ConvoyReach(false, 0, 0, double.NegativeInfinity, "PhobosRadioReach_NoStation".Translate().Resolve());
        var rules = RadioRulesDef.Current;
        if (RelayNodes.BestChain(start, homes, RelayNodes.All().ToList()) is { } found)
        {
            RelayNode home = found.nodes[found.chain.Path[found.chain.Path.Count - 1]];
            double metres = Find.WorldGrid.ApproxDistanceInTiles(convoy.Tile, home.Tile) * rules.metresPerWorldTile;
            double horizon = Propagation.RadioHorizonMetres(start.End.AntennaHeightMetres + start.SiteMetres, home.End.AntennaHeightMetres + home.SiteMetres);
            string line = found.chain.Relays == 0
                ? "PhobosRadioReach_InTouch".Translate(home.Label.Named("HOME"), (metres / 1000.0).ToString("0").Named("KM"),
                    (horizon / 1000.0).ToString("0").Named("HORIZON"), found.chain.WorstMarginDb.ToString("0").Named("MARGIN")).Resolve()
                : "PhobosRadioReach_InTouchRelayed".Translate(home.Label.Named("HOME"),
                    found.chain.Path.Skip(1).Take(found.chain.Relays).Select(i => found.nodes[i].Label).ToCommaList(useAnd: true).Named("RELAYS"),
                    (metres / 1000.0).ToString("0").Named("KM"), found.chain.WorstMarginDb.ToString("0").Named("MARGIN")).Resolve();
            if (found.chain.Relays > 0 && Seen.Wants("radio.relay-chain", start.End.Thing))
                Seen.Note("radio.relay-chain", start.End.Thing, found.chain.Relays + " relays, " + found.chain.WorstMarginDb.ToString("0") + " dB");
            return new ConvoyReach(true, metres / 1000.0, horizon / 1000.0, found.chain.WorstMarginDb, line, home.Tile);
        }
        // Out of reach: said against the nearest home, directly.
        RelayNode nearest = homes.OrderBy(h => Find.WorldGrid.ApproxDistanceInTiles(convoy.Tile, h.Tile)).First();
        double far = Find.WorldGrid.ApproxDistanceInTiles(convoy.Tile, nearest.Tile) * rules.metresPerWorldTile;
        double reachable = Propagation.RadioHorizonMetres(start.End.AntennaHeightMetres + start.SiteMetres, nearest.End.AntennaHeightMetres + nearest.SiteMetres);
        double margin = LinkBudgetService.EvaluateWorld(start.End, nearest.End, far, rules, start.SiteMetres, nearest.SiteMetres)?.MarginDb ?? double.NegativeInfinity;
        string why = (far > reachable ? "PhobosRadioReach_OverHorizon" : "PhobosRadioReach_TooWeak").Translate(nearest.Label.Named("HOME"),
            (far / 1000.0).ToString("0").Named("KM"), (reachable / 1000.0).ToString("0").Named("HORIZON"), margin.ToString("0").Named("MARGIN")).Resolve();
        // Radio 0.24.0: a relay that would have carried it, full of other calls.
        if (RelayNodes.BusyFor(convoy) is { } busy) why += " " + "PhobosRadioReach_RelayBusy".Translate(busy.Named("RELAYS")).Resolve();
        return new ConvoyReach(false, far / 1000.0, reachable / 1000.0, margin, why);
    }

    // ---- Putting up a relay mast (Radio 0.22.0) ----

    public IEnumerable<Gizmo> CaravanGizmos(Caravan convoy)
    {
        if (ConvoyEnd(convoy, 0f) == null) yield break;
        var build = new Command_Action
        {
            defaultLabel = "PhobosRadio_RelayBuildGizmo".Translate(),
            defaultDesc = "PhobosRadio_RelayBuildGizmoDesc".Translate(10.Named("METRES"), MastSteel.Named("STEEL"), MastComponents.Named("COMPONENTS")),
            icon = ContentFinder<Texture2D>.Get("Phobos/Radio/Things/Mast", reportFailure: false) ?? BaseContent.BadTex,
            action = () => BuildMast(convoy),
        };
        if (MastRefusal(convoy) is { } refused) build.Disable(refused);
        yield return build;
    }

    static List<Thing> Carried(Caravan convoy, ThingDef def) => Contracts.Carried(convoy).Where(t => t.def == def).ToList();

    public static string? MastRefusal(Caravan convoy)
    {
        if (RoadWork.Moving(convoy)) return "PhobosRadio_RelayRefuseMoving".Translate().Resolve();
        if (WorldObject_PhobosRelayMast.At(convoy.Tile)) return "PhobosRadio_RelayRefuseHere".Translate().Resolve();
        int steel = Carried(convoy, ThingDefOf.Steel).Sum(t => t.stackCount);
        int components = Carried(convoy, ThingDefOf.ComponentIndustrial).Sum(t => t.stackCount);
        if (steel < MastSteel || components < MastComponents)
            return "PhobosRadio_RelayRefuseShort".Translate(MastSteel.Named("STEEL"), MastComponents.Named("COMPONENTS"), steel.Named("HAVESTEEL"),
                components.Named("HAVECOMPONENTS")).Resolve();
        return null;
    }

    static void Take(Caravan convoy, ThingDef def, int count)
    {
        foreach (Thing thing in Carried(convoy, def))
        {
            if (count <= 0) break;
            int take = Mathf.Min(count, thing.stackCount);
            thing.SplitOff(take).Destroy(DestroyMode.Vanish);
            count -= take;
        }
    }

    public static void BuildMast(Caravan convoy)
    {
        if (MastRefusal(convoy) is { } refused)
        {
            Messages.Message(refused, convoy, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        Take(convoy, ThingDefOf.Steel, MastSteel);
        Take(convoy, ThingDefOf.ComponentIndustrial, MastComponents);
        if (convoy is VehicleCaravan vehicles) vehicles.RecacheVehicles();
        WorldObject_PhobosRelayMast mast = WorldObject_PhobosRelayMast.Make(convoy.Tile);
        Seen.Note("radio.relay-mast", (convoy as VehicleCaravan)?.VehiclesListForReading.FirstOrDefault(), convoy.LabelCap);
        Messages.Message("PhobosRadio_RelayBuilt".Translate(convoy.LabelCap.Named("CONVOY")), mast, MessageTypeDefOf.TaskCompletion, historical: false);
    }
}

/// <summary>
/// The vehicles that relay (Radio 0.22.0; the owner's cards of 26 Nov 2026: "Parked radio vehicles", and a repeater between tiles): a convoy
/// of the player's standing still on the world map, its aerial on a Datum's mast when one is up, and a radio vehicle left at a depot, each
/// relaying only with the repeater kit fitted and powered. A moving convoy relays nothing.
/// </summary>
public sealed class VehicleRelaySource : IRelaySource
{
    public IEnumerable<RelayNode> Nodes()
    {
        if (Find.WorldObjects == null) yield break;
        foreach (WorldObject place in Find.WorldObjects.AllWorldObjects)
        {
            if (place is VehicleCaravan convoy && convoy.Faction == Faction.OfPlayer && !RoadWork.Moving(convoy))
            {
                if (ConvoyReachSource.ConvoyEnd(convoy, ConvoyRadio.Mast(convoy)) is { } end) yield return end;
            }
            else if (place is PhobosSlingWreck { IsDepot: true } depot)
            {
                foreach (VehiclePawn vehicle in depot.Vehicles)
                {
                    if (vehicle.Faction != Faction.OfPlayer || vehicle.GetComp<CompRadio>() is not { } radio || !VehicleRadios.Fitted(vehicle)) continue;
                    yield return new RelayNode(RadioEndpoint.BuiltIn(vehicle, IntVec3.Zero, radio), depot.Tile, vehicle.LabelShort, vehicle,
                        Repeater: VehicleRadios.Repeats(vehicle), SiteMetres: RelayNode.SiteOf(depot.Tile));
                }
            }
        }
    }
}
