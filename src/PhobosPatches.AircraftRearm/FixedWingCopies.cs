using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;

namespace Phobos.AircraftRearmPatches;

/// <summary>
/// Where each jet mod keeps its fixed-wing flight code (Patches 0.45.1). Until 26 Nov 2026 the Harrier, Close Air Support and the Prowler
/// shared one framework, FixedWingAircraftFramework, with one rearm job, FWA_RearmVehicle. That day's updates gave each mod its own copy
/// under its own namespace, with its own rearm job; read from their new builds the same day, the copies are the same code apart from
/// those names and their message keys. With two or three of the mods active there are now two or three copies, so each fix here patches
/// every copy it finds, and a copy whose code changed is reported off by itself.
/// </summary>
internal static class FixedWingCopies
{
    internal sealed record Copy(string PackageId, string Label, string Namespace, string RearmJob)
    {
        /// <summary>One of this copy's types by its short name, or null when the mod is not loaded or renamed it.</summary>
        public Type? Type(string name) => AccessTools.TypeByName(Namespace + "." + name);
    }

    internal static readonly Copy[] All =
    {
        new("potemkinvillager.av8bharrier", "the Harrier", "AV8BHarrier.Flight", "HFW_RearmVehicle"),
        new("potemkinvillager.CloseAirSupport", "the A-10 and Su-25", "CloseAirSupport.Flight", "CAS_RearmVehicle"),
        new("potemkinvillager.ea6bprowler", "the Prowler", "EA6BProwler.Flight", "PRW_RearmVehicle"),
    };

    /// <summary>The copies of the jet mods that are active.</summary>
    internal static IEnumerable<Copy> Active() => All.Where(copy => ModsConfig.IsActive(copy.PackageId));

    internal static bool AnyActive => Active().Any();
}
