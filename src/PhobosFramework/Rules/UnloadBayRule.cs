using System.Collections.Generic;

namespace Phobos.Framework.Rules;

/// <summary>
/// One storage place marked "Vehicles may unload here", as a vehicle choosing where to unload sees it (Framework 0.175.0;
/// docs/development/vehicle-gathering-runs.md, "Marked bays and Deliver"). <see cref="PassedOver"/> is a place already found full or out of
/// reach on this trip.
/// </summary>
public readonly record struct BayCandidate(int DistanceSquared, bool TakesSomething, bool HasRoom, bool PassedOver);

/// <summary>Why no marked bay could be chosen.</summary>
public enum BayShortfall
{
    None,
    /// <summary>Nothing on the map is marked for vehicles to unload at.</summary>
    NoneMarked,
    /// <summary>Places are marked, but none takes anything aboard.</summary>
    NoneTakes,
    /// <summary>Some take what is aboard, but each is full or could not be reached.</summary>
    NoneWithRoom,
}

/// <summary>
/// The rules of unloading at marked bays (Framework 0.175.0): which bay a vehicle makes for, when it moves on to the next, how a delivery
/// ends and whether it drives back. Game-free, so any vehicle with an unloading worker shares them.
/// </summary>
public static class UnloadBayRule
{
    /// <summary>The nearest bay that takes something aboard, has room for it and has not been passed over on this trip; -1 for none.</summary>
    public static int Nearest(IReadOnlyList<BayCandidate> bays)
    {
        int best = -1;
        for (int i = 0; i < bays.Count; i++)
        {
            BayCandidate bay = bays[i];
            if (!bay.TakesSomething || !bay.HasRoom || bay.PassedOver) continue;
            if (best < 0 || bay.DistanceSquared < bays[best].DistanceSquared) best = i;
        }
        return best;
    }

    /// <summary>Why <see cref="Nearest"/> found none: nothing marked, nothing that takes the load, or all full or out of reach.</summary>
    public static BayShortfall WhyNone(IReadOnlyList<BayCandidate> bays)
    {
        if (bays.Count == 0) return BayShortfall.NoneMarked;
        for (int i = 0; i < bays.Count; i++)
        {
            if (bays[i].TakesSomething) return BayShortfall.NoneWithRoom;
        }
        return BayShortfall.NoneTakes;
    }

    /// <summary>
    /// Whether a tip that has ended at one bay looks for another: when it emptied what that bay takes, or the bay filled. A fault or the
    /// player's stop ends the trip.
    /// </summary>
    public static bool LooksForAnother(UnloadEnd end) => end is UnloadEnd.Emptied or UnloadEnd.PlaceFull;

    /// <summary>How a delivery ends once no bay is left to try: delivered when nothing for the bays is aboard, else no bay for the rest.</summary>
    public static GatherRunEnd DeliveryEnd(bool anythingLeftForBays) => anythingLeftForBays ? GatherRunEnd.NoBay : GatherRunEnd.Delivered;

    /// <summary>
    /// Whether a finished delivery drives back to where it started (owner, 19 Nov 2026: the player chooses, per vehicle): when chosen, when it
    /// is somewhere else, and only after it delivered or ran out of bays. A fault, another order, enemies or the player's stop leave it where it is.
    /// </summary>
    public static bool DrivesBack(bool chosen, bool awayFromStart, GatherRunEnd end) =>
        chosen && awayFromStart && end is GatherRunEnd.Delivered or GatherRunEnd.NoBay;
}
