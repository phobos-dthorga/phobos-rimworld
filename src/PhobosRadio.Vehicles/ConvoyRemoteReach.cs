using System.Collections.Generic;
using System.Linq;
using Phobos.Radio.Services;
using Phobos.VehicleSystems;
using RimWorld.Planet;
using Verse;

namespace Phobos.Radio.Vehicles;

/// <summary>
/// Where a person in a convoy reaches home by radio (Radio 0.23.0, for advice by radio): first the colony the convoy's reach home finds, as
/// D'thorga Framework keeps it (asked at most once a tenth of a game hour); then, since Radio 0.24.1, each other colony of the player's,
/// asked only when the caller goes on looking (a lazy list), through any relay, another colony's repeater station among them
/// (<see cref="ConvoyReachSource.MarginTo"/>). Nothing when the convoy has no radio or reaches no colony.
/// </summary>
public sealed class ConvoyRemoteReach : IRemoteReachSource
{
    public IEnumerable<RemoteHome> Homes(Pawn pawn)
    {
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
}
