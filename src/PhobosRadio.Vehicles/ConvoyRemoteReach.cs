using System.Collections.Generic;
using System.Linq;
using Phobos.Radio.Services;
using Phobos.VehicleSystems;
using RimWorld.Planet;
using Vehicles;
using Verse;

namespace Phobos.Radio.Vehicles;

/// <summary>
/// Where a person away from home reaches home by radio (Radio 0.23.0, for advice by radio). In a convoy: first the colony the convoy's reach
/// home finds, as D'thorga Framework keeps it (asked at most once a tenth of a game hour); then, since Radio 0.24.1, each other colony of the
/// player's, asked only when the caller goes on looking (a lazy list), through any relay, another colony's repeater station among them
/// (<see cref="ConvoyReachSource.MarginTo"/>). Since Radio 0.25.0, on a map no station serves, through a vehicle there with the dual-band
/// radio (<see cref="CrossBand"/>): the colony its chain reaches first, then the others, each link the worse of the hand-held's hop and the
/// vehicle's chain. Nothing when there is no way home.
/// </summary>
public sealed class ConvoyRemoteReach : IRemoteReachSource
{
    public IEnumerable<RemoteHome> Homes(Pawn pawn)
    {
        if (pawn.Spawned)
        {
            foreach (RemoteHome home in ThroughGateway(pawn)) yield return home;
            yield break;
        }
        if (pawn.GetCaravan() is not { } convoy) yield break;
        // Out of reach of every colony, no search aimed at one could reach it either: the same relays, fewer targets.
        if (ConvoyRadio.Of(convoy) is not { InTouch: true } reach || reach.HomeTile < 0) yield break;
        Map? first = Find.Maps.FirstOrDefault(map => map.Tile == reach.HomeTile);
        if (first != null) yield return new RemoteHome(reach.MarginDb, first);
        foreach (Map colony in Find.Maps.Where(map => map.IsPlayerHome && map != first).ToList())
        {
            if (ConvoyReachSource.MarginTo(convoy, colony) is { } margin) yield return new RemoteHome(margin, colony);
        }
    }

    /// <summary>A map where a dual-band vehicle stands is worth looking at for advice, though no station is there.</summary>
    public bool Serves(Map map) => !map.IsPlayerHome && CrossBand.AnyOn(map);

    /// <summary>
    /// A person's end between world tiles, for a call that searches its own chain (radio-elmer's faction doctors): a convoy's radio, or the
    /// dual-band vehicle a spawned colonist's hand-held reaches on a map no station serves; null with neither.
    /// </summary>
    public static RelayNode? WorldEndOf(Pawn pawn) =>
        pawn.Spawned ? CrossBand.For(pawn)?.WorldEnd
        : pawn.GetCaravan() is { } convoy ? ConvoyReachSource.ConvoyEnd(convoy, ConvoyRadio.Mast(convoy)) : null;

    /// <summary>Radio 0.26.0: the person's end between tiles for another faction's doctor, with the hand-held's hop to a cross-band vehicle.</summary>
    public (RelayNode End, double HopMarginDb)? WorldEnd(Pawn pawn)
    {
        if (pawn.Spawned) return CrossBand.For(pawn) is { } gateway ? (gateway.WorldEnd, gateway.MapMarginDb) : null;
        return WorldEndOf(pawn) is { } end ? (end, double.PositiveInfinity) : null;
    }

    /// <summary>The first colony each gateway vehicle reaches, kept a tenth of a game hour, as a convoy's reach is: a call's look asks often.</summary>
    static readonly Dictionary<VehiclePawn, (int tick, (Map Home, double MarginDb)? first)> firstKept = new();
    const int KeepTicks = 250;

    static (Map Home, double MarginDb)? FirstHome(CrossBand.Gateway gateway)
    {
        int now = Find.TickManager.TicksGame;
        if (firstKept.TryGetValue(gateway.Vehicle, out var held) && now - held.tick >= 0 && now - held.tick < KeepTicks) return held.first;
        if (firstKept.Count > 64) firstKept.Clear();
        var first = ConvoyReachSource.ChainHome(gateway.WorldEnd, null, gateway.Vehicle);
        firstKept[gateway.Vehicle] = (now, first);
        return first;
    }

    static IEnumerable<RemoteHome> ThroughGateway(Pawn pawn)
    {
        if (CrossBand.For(pawn) is not { } gateway) yield break;
        if (FirstHome(gateway) is not { } first) yield break;
        yield return new RemoteHome(System.Math.Min(gateway.MapMarginDb, first.MarginDb), first.Home);
        foreach (Map colony in Find.Maps.Where(map => map.IsPlayerHome && map != first.Home).ToList())
        {
            if (ConvoyReachSource.ChainHome(gateway.WorldEnd, colony, (gateway.Vehicle, colony)) is { } other)
                yield return new RemoteHome(System.Math.Min(gateway.MapMarginDb, other.MarginDb), colony);
        }
    }
}
