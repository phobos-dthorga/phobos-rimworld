using System.Collections.Generic;
using Vehicles;

namespace Phobos.VehicleSystems;

/// <summary>
/// Which generator set's service lead runs to which vehicle (Framework 0.101.0; docs/development/vehicle-upgrades-round-two.md), so a vehicle's
/// shore-power check can ask in one look. Derived, never saved: each set saves its own lead and notes it here when it spawns. An entry is
/// believed only while its set still names that vehicle, so a stale one (a set gone, a game left) answers nothing.
/// </summary>
public static class ServiceLeads
{
    static readonly Dictionary<VehiclePawn, CompPhobosGeneratorSet> bySource = new();

    public static void Note(VehiclePawn target, CompPhobosGeneratorSet source) => bySource[target] = source;

    public static void Forget(VehiclePawn target, CompPhobosGeneratorSet source)
    {
        if (bySource.TryGetValue(target, out CompPhobosGeneratorSet? held) && held == source) bySource.Remove(target);
    }

    /// <summary>The set whose lead runs to this vehicle now, or null.</summary>
    public static CompPhobosGeneratorSet? SourceFor(VehiclePawn target)
    {
        if (!bySource.TryGetValue(target, out CompPhobosGeneratorSet? source)) return null;
        if (source.LeadTo == target && source.Vehicle is { Destroyed: false }) return source;
        bySource.Remove(target);
        return null;
    }

    /// <summary>Whether a set's lead is charging this vehicle now: it counts as on shore power.</summary>
    public static bool Feeding(VehiclePawn target) => bySource.Count > 0 && SourceFor(target)?.LeadLive == true;
}
