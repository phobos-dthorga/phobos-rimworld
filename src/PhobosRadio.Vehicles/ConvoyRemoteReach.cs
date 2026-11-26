using System.Linq;
using Phobos.Radio.Services;
using Phobos.VehicleSystems;
using RimWorld.Planet;
using Verse;

namespace Phobos.Radio.Vehicles;

/// <summary>
/// Where a person in a convoy reaches home by radio (Radio 0.23.0, for advice by radio): the convoy's reach home, as D'thorga Framework keeps
/// it (asked at most once a tenth of a game hour), and the home map it reaches. Null when the convoy is out of touch or has no radio.
/// </summary>
public sealed class ConvoyRemoteReach : IRemoteReachSource
{
    public RemoteHome? Home(Pawn pawn)
    {
        if (pawn.GetCaravan() is not { } convoy || ConvoyRadio.Of(convoy) is not { InTouch: true } reach || reach.HomeTile < 0) return null;
        Map? home = Find.Maps.FirstOrDefault(map => map.Tile == reach.HomeTile);
        return home == null ? null : new RemoteHome(reach.MarginDb, home);
    }
}
