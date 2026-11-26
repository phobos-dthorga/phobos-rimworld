using System.Collections.Generic;
using System.Linq;
using Phobos.Radio.Physics;
using Phobos.Radio.Services;
using Phobos.VehicleSystems;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.Radio.Vehicles;

/// <summary>
/// A convoy's reach home on the world map (Radio 0.21.0, the first step of phase 2; D'thorga Framework's EM33; the owner's cards of 26 Nov
/// 2026). The convoy's best fitted vehicle radio, its antenna lifted by a Datum's mast when one is up, against each powered station on each of
/// the colony's home maps, through the one link budget (<see cref="LinkBudgetService.EvaluateWorld"/>). The best link decides.
/// <para>Cost: asked only when D'thorga Framework asks (a convoy's line, an ambush, an airdrop's targets), at most once a tenth of a game hour
/// for each convoy, and then one budget a station a band. No map is walked; nothing runs on its own.</para>
/// </summary>
public sealed class ConvoyReachSource : IConvoyReachSource
{
    public ConvoyReach? Reach(Caravan convoy, float mastMetres)
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
        var rules = RadioRulesDef.Current;
        float height = mastMetres > radio.Props.antennaHeightMetres ? mastMetres : radio.Props.antennaHeightMetres;
        var convoyEnd = new RadioEndpoint(carrier, IntVec3.Zero, radio, radio.Props.bands, height, radio.Props.antennaGainDbi,
            new List<FeedSegment>(), carrier.LabelShort);
        ConvoyReach? best = null;
        foreach (Map map in Find.Maps.Where(m => m.IsPlayerHome))
        {
            double metres = Find.WorldGrid.ApproxDistanceInTiles(convoy.Tile, map.Tile) * rules.metresPerWorldTile;
            foreach (CompRadio station in RadioNet.For(map).Stations)
            {
                if (station.parent.TryGetComp<CompPowerTrader>() is { PowerOn: false }) continue;
                foreach (RadioEndpoint homeEnd in RadioNet.StationEndpoints(station, RadioMod.Settings.metresPerCell))
                {
                    if (LinkBudgetService.EvaluateWorld(convoyEnd, homeEnd, metres, rules) is not { } link) continue;
                    double horizon = Propagation.RadioHorizonMetres(convoyEnd.AntennaHeightMetres, homeEnd.AntennaHeightMetres);
                    var reach = new ConvoyReach(link.InTouch, metres / 1000.0, horizon / 1000.0, link.MarginDb,
                        Line(link.InTouch, metres, horizon, link.MarginDb, map.Parent.LabelCap));
                    if (best == null || reach.InTouch && !best.Value.InTouch || reach.InTouch == best.Value.InTouch && reach.MarginDb > best.Value.MarginDb) best = reach;
                }
            }
        }
        return best ?? new ConvoyReach(false, 0, 0, double.NegativeInfinity, "PhobosRadioReach_NoStation".Translate().Resolve());
    }

    static string Line(bool inTouch, double metres, double horizon, double margin, string home) =>
        (inTouch ? "PhobosRadioReach_InTouch" : metres > horizon ? "PhobosRadioReach_OverHorizon" : "PhobosRadioReach_TooWeak")
            .Translate(home.Named("HOME"), (metres / 1000.0).ToString("0").Named("KM"), (horizon / 1000.0).ToString("0").Named("HORIZON"),
                margin.ToString("0").Named("MARGIN")).Resolve();
}
