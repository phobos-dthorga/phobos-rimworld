using Phobos.Framework.Vehicles.Rules;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>The compass point from one cell to another, in words, for any sensor that tells the player where something was heard or found.</summary>
public static class CompassText
{
    public static string From(IntVec3 from, IntVec3 to) => ListeningRule.BearingOf(to.x - from.x, to.z - from.z) switch
    {
        Bearing.North => "PhobosListening_North".Translate().Resolve(),
        Bearing.NorthEast => "PhobosListening_NorthEast".Translate().Resolve(),
        Bearing.East => "PhobosListening_East".Translate().Resolve(),
        Bearing.SouthEast => "PhobosListening_SouthEast".Translate().Resolve(),
        Bearing.South => "PhobosListening_South".Translate().Resolve(),
        Bearing.SouthWest => "PhobosListening_SouthWest".Translate().Resolve(),
        Bearing.West => "PhobosListening_West".Translate().Resolve(),
        _ => "PhobosListening_NorthWest".Translate().Resolve(),
    };
}
