using HarmonyLib;
using Phobos.Shared;
using Phobos.Framework;
using UnityEngine;
using Verse;

namespace Phobos.VehiclePatches;

/// <summary>
/// D'thorga Patches' Harrier part (D'thorga Vehicle Patches until 7 November 2026; the namespace keeps the old name). Runs in the
/// Mod constructor, not a static constructor: mods are constructed before any static constructor runs, so this is in place
/// before RimPacts and the Harrier's flight framework patch anything.
/// </summary>
/// <summary>Player preferences for the Harrier part. The saved key never changes.</summary>
public class VehiclePatchesSettings : ModSettings
{
    /// <summary>Aircraft may fly a sortie over a RimPacts battle the player has not joined (RimPacts' own rule says no).</summary>
    public bool sortieOverUnjoinedBattle;

    /// <summary>TEMPORARY (0.18.0): log each step when a vehicle is taken apart, to find why the Harrier cannot be (DisassemblyTrace).</summary>
    public bool traceDisassembly = true;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref sortieOverUnjoinedBattle, "PhobosVehiclePatches_SortieOverUnjoinedBattle", false);
        Scribe_Values.Look(ref traceDisassembly, "PhobosVehiclePatches_TraceDisassembly", true);
    }
}

public class VehiclePatchesMod : Mod
{
    public static VehiclePatchesSettings Settings { get; private set; } = new();

    /// <summary>The Harmony ID is the packageId; never change it once the mod is published.</summary>
    public const string HarmonyId = "PhobosDthorga.Patches";
    public const string ModVersion = "0.19.1";
    public const string LogPrefix = "[D'thorga Patches]";
    public static readonly Phobos.Logging.PhobosLog Logger = Phobos.Framework.Logging.Logs.For("D'thorga Patches", "Harrier");

    public VehiclePatchesMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<VehiclePatchesSettings>();
        var harmony = new Harmony(HarmonyId);
        RimPactsArrivalFilter.Watch(harmony);
        AirborneCrewKeepsMap.Apply(harmony);
        // The attribute patches: the sortie gate's reason and the flight-control rule. A saved Harrier's new parts are created by
        // D'thorga Framework's vehicle systems (0.10.0), which load whenever Vehicle Framework does.
        HarmonyPatchApplier.ApplyEach(harmony, typeof(VehiclePatchesMod).Assembly, LogPrefix);
        // Temporary: the disassembly trace's patch on the fixed-wing framework's own Destroy, found by name once every assembly is in.
        LongEventHandler.ExecuteWhenFinished(() => DisassemblyTrace.Apply(harmony));
        LongEventHandler.ExecuteWhenFinished(LogLoaded);
    }

    static void LogLoaded()
    {
        var report = new LoadReport(LogPrefix, ModVersion, "Harrier VTOL");
        report.On("sortie maps kept open under the jet");
        report.On("older saves' bomb cases carried over to their new names");
        if (OptionalMods.IsActive("wowgag.RimPacts")) report.On("RimPacts' landing rule through a plain postfix" + (Settings.sortieOverUnjoinedBattle ? ", sorties exempt (setting)" : "") + ", with its reason shown");
        else report.Off("RimPacts' landing rule through a plain postfix", "RimPacts is not active");
        report.On("the Harrier's fuller parts list (D'thorga Framework's flight controls rule grounds it when they are too damaged)");
        report.On("pad landings checked against the engine's hover limit (D'thorga Framework's setting: " + Phobos.VehicleSystems.VehicleSystemsMod.Settings.hoverLimit + ")");
        if (Settings.traceDisassembly) report.On("a temporary trace of each step when a vehicle is taken apart, to find why the Harrier cannot be");
        else report.Off("a temporary trace of vehicle disassembly", "switched off in the settings");
        report.Log();
    }

    public override string SettingsCategory() => "PhobosVehiclePatches_SettingsCategory".Translate();

    public override void DoSettingsWindowContents(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        SettingsUi.Section(listing, "PhobosVehiclePatches_SectionSorties".Translate());
        listing.CheckboxLabeled("PhobosVehiclePatches_SettingOverflight".Translate(), ref Settings.sortieOverUnjoinedBattle, "PhobosVehiclePatches_SettingOverflightDesc".Translate());
        if (Settings.sortieOverUnjoinedBattle) SettingsUi.Warning(listing, "PhobosVehiclePatches_OverflightWarning".Translate());
        SettingsUi.Section(listing, "PhobosVehiclePatches_SectionDiagnostics".Translate());
        listing.CheckboxLabeled("PhobosVehiclePatches_SettingTraceDisassembly".Translate(), ref Settings.traceDisassembly, "PhobosVehiclePatches_SettingTraceDisassemblyDesc".Translate());
        listing.End();
    }
}
