using System;
using System.Collections.Generic;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// The mechanitor level of working without a crew (Framework 0.282.0; docs/development/autonomous-vehicles.md, step 7b; the owner's choices
/// of 18 and 25 Nov 2026): a vehicle's cost in a mechanitor's bandwidth, and which vehicles keep control when it runs short. Game-free and
/// tested.
/// </summary>
public static class MechLinkRule
{
    /// <summary>
    /// A vehicle's bandwidth by size (the owner's card of 25 Nov 2026: 1 light, 2 medium, 3 heavy or tracked, from its loadout's class). A
    /// heavy or tracked loadout says so; light and medium share one class of loadout, so they are told apart by the vehicle's empty mass
    /// [A: <paramref name="lightMaxKg"/>]. With no mass known, medium.
    /// </summary>
    public static int Cost(bool heavyOrTracked, double kerbKg, double lightMaxKg)
    {
        if (heavyOrTracked) return 3;
        if (double.IsNaN(kerbKg) || kerbKg <= 0) return 2;
        return kerbKg <= lightMaxKg ? 1 : 2;
    }

    /// <summary>
    /// How many of a mechanitor's vehicles keep control, oldest first (the owner, 18 Nov 2026: added to the used bandwidth, so vehicles lose
    /// control first, newest first, and mechanoids keep priority). The mechanoids' own use comes off the total before any vehicle.
    /// </summary>
    public static int Kept(int totalBandwidth, int mechanoidsUse, IReadOnlyList<int> costsOldestFirst)
    {
        var left = totalBandwidth - Math.Max(0, mechanoidsUse);
        var kept = 0;
        foreach (var cost in costsOldestFirst)
        {
            if (cost > left) break;
            left -= cost;
            kept++;
        }
        return kept;
    }
}
