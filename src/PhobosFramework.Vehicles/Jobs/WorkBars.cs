using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Phobos.VehicleSystems;

/// <summary>
/// The game's work bar on our vehicle jobs (owner, 12 Nov 2026: "there needs to be progress bars in many areas, where appropriate").
/// Vehicle Framework's JobDriver_WorkVehicle draws its bar only when the job has no effecter, and its effecter defaults to the vehicle's
/// repair effect, which every vehicle has, so the bar never shows (read from Vehicle Framework 1.6, JobDriver_WorkVehicle.MakeNewToils,
/// 12 Nov 2026). This adds the bar to the work toil as well, keeping the sparks. Presentation only.
/// </summary>
public static class WorkBars
{
    /// <summary>Passes the base driver's toils through, adding the bar to its work toil (the one that ticks) when the base left it off.</summary>
    public static IEnumerable<Toil> WithBar(IEnumerable<Toil> toils, bool baseHasEffecter, Func<float> progress)
    {
        foreach (Toil toil in toils)
        {
            if (baseHasEffecter && toil.tickAction != null) toil.WithProgressBar(TargetIndex.A, progress, false, -0.5f);
            yield return toil;
        }
    }
}
