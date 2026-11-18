using System;
using Phobos.Framework.Rules;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// What a vehicle picks things up with where it stands, for a gathering run (Framework 0.143.0; docs/development/vehicle-gathering-runs.md).
/// The Talus's loading grab is the first; a fork-lift, a magnet or a hose would be others. The run decides what is wanted and where to stand;
/// the worker decides what it can lift, how fast, and when it must stop, and says why.
/// </summary>
public interface IVehicleGatherWorker
{
    /// <summary>Whether the vehicle has it now (the grab fitted). Without it a run's buttons are not shown.</summary>
    bool CanGatherAtAll { get; }

    /// <summary>How many cells beyond the body's edge it reaches.</summary>
    int GatherReachCells { get; }

    /// <summary>Whether a cell is within its reach from where the vehicle stands now.</summary>
    bool InGatherReach(IntVec3 cell);

    /// <summary>Its own limits: whether it could pick this thing up at all, whatever the player chose (an item, not too heavy for it).</summary>
    bool CouldGather(Thing thing);

    /// <summary>How many kilograms more the cargo takes.</summary>
    float GatherRoomKilograms { get; }

    /// <summary>
    /// Starts picking up what <paramref name="wanted"/> allows within reach, quietly: the run reports. <see cref="GatherWorkEnd.None"/> when it
    /// has started; otherwise why not, with what to tell the player in <paramref name="refusal"/>.
    /// </summary>
    GatherWorkEnd StartGathering(Predicate<Thing> wanted, out string? refusal);

    /// <summary>Hands the wanted test back after a load: it is not saved. The worker takes nothing until it has it.</summary>
    void ResumeGathering(Predicate<Thing> wanted);

    /// <summary>Whether it is picking things up for a run now.</summary>
    bool Gathering { get; }

    /// <summary>Stops at once, quietly.</summary>
    void StopGathering();

    /// <summary>Why it last stopped.</summary>
    GatherWorkEnd LastGatherEnd { get; }

    /// <summary>What it said when it last stopped, for a run cut short; null if nothing.</summary>
    string? LastGatherReason { get; }

    /// <summary>What it picked up since it last started: loads, and their mass.</summary>
    int GatheredLoads { get; }

    float GatheredKilograms { get; }
}
