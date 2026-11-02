using HarmonyLib;
using Verse;

namespace Phobos.VehiclePatches;

/// <summary>
/// Runs in the Mod constructor, not a static constructor: mods are constructed before any static constructor runs, so
/// this is in place before RimPacts and the Harrier's flight framework patch anything.
/// </summary>
public class VehiclePatchesMod : Mod
{
    /// <summary>The Harmony ID is the packageId; never change it once the mod is published.</summary>
    public const string HarmonyId = "PhobosDthorga.VehiclePatches";

    public VehiclePatchesMod(ModContentPack content) : base(content)
    {
        RimPactsArrivalFilter.Watch(new Harmony(HarmonyId));
    }
}
