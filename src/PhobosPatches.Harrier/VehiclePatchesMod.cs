using HarmonyLib;
using Phobos.Framework;
using Verse;

namespace Phobos.VehiclePatches;

/// <summary>
/// D'thorga Patches' Harrier part (D'thorga Vehicle Patches until 7 November 2026; the namespace keeps the old name). Runs in the
/// Mod constructor, not a static constructor: mods are constructed before any static constructor runs, so this is in place
/// before RimPacts and the Harrier's flight framework patch anything.
/// </summary>
public class VehiclePatchesMod : Mod
{
    /// <summary>The Harmony ID is the packageId; never change it once the mod is published.</summary>
    public const string HarmonyId = "PhobosDthorga.Patches";
    public const string ModVersion = "0.7.0";
    public const string LogPrefix = "[D'thorga Patches]";

    public VehiclePatchesMod(ModContentPack content) : base(content)
    {
        var harmony = new Harmony(HarmonyId);
        RimPactsArrivalFilter.Watch(harmony);
        AirborneCrewKeepsMap.Apply(harmony);
        LongEventHandler.ExecuteWhenFinished(LogLoaded);
    }

    static void LogLoaded()
    {
        var report = new LoadReport(LogPrefix, ModVersion, "Harrier VTOL");
        report.On("sortie maps kept open under the jet");
        if (OptionalMods.IsActive("wowgag.RimPacts")) report.On("RimPacts' landing rule through a plain postfix");
        else report.Off("RimPacts' landing rule through a plain postfix", "RimPacts is not active");
        report.Log();
    }
}
