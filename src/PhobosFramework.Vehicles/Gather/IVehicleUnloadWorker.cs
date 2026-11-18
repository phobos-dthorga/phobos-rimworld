using System;
using Phobos.Framework.Rules;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// What a vehicle puts its load down with, over its tail, for a gathering run that unloads at a chosen place (Framework 0.144.0;
/// docs/development/vehicle-gathering-runs.md). The Talus's tip is the first: the run backs the truck up to the place, tail first, and the
/// tip fills storage there before the ground. The run decides what goes and where to stand; the worker puts it down and says why it stopped.
/// </summary>
public interface IVehicleUnloadWorker
{
    /// <summary>Whether it can unload now at all.</summary>
    bool CanUnloadAtAll { get; }

    /// <summary>
    /// Starts putting down what <paramref name="which"/> allows, quietly: the run reports. <see cref="UnloadEnd.None"/> when it has started;
    /// otherwise why not (nothing aboard to put down, no room, a fault), with what to tell the player in <paramref name="refusal"/>.
    /// </summary>
    UnloadEnd StartUnloading(Predicate<Thing> which, out string? refusal);

    /// <summary>Hands the test back after a load: it is not saved. The worker puts nothing down until it has it.</summary>
    void ResumeUnloading(Predicate<Thing> which);

    /// <summary>Whether it is putting things down for a run now.</summary>
    bool Unloading { get; }

    /// <summary>Stops at once, quietly.</summary>
    void StopUnloading();

    /// <summary>Why it last stopped.</summary>
    UnloadEnd LastUnloadEnd { get; }

    /// <summary>What it said when it last stopped, for a run cut short; null if nothing.</summary>
    string? LastUnloadReason { get; }

    /// <summary>How many things it put down since it last started.</summary>
    int UnloadedCount { get; }
}

/// <summary>
/// Which side of a storage building a vehicle backs up to when it unloads there (Framework 0.144.0). Without it, any side. The bulk bunker
/// opens behind: its walls are on the other three.
/// </summary>
public class PhobosBackUpSideExtension : DefModExtension
{
    public BackUpSide side = BackUpSide.Back;
}
