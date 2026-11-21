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


    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref sortieOverUnjoinedBattle, "PhobosVehiclePatches_SortieOverUnjoinedBattle", false);
    }
}

public class VehiclePatchesMod : SettingsBookMod
{
    public static VehiclePatchesSettings Settings { get; private set; } = new();

    /// <summary>The Harmony ID is the packageId; never change it once the mod is published.</summary>
    public const string HarmonyId = "PhobosDthorga.Patches";
    public const string ModVersion = "0.33.0";
    public const string LogPrefix = "[D'thorga Patches]";
    public static readonly Phobos.Logging.PhobosLog Logger = Phobos.Framework.Logging.Logs.For("D'thorga Patches", "Harrier");
    /// <summary>The temporary disassembly trace (DisassemblyTrace): written while this log is ticked under D'thorga Framework's Detailed logs.
    /// It had a tick of its own, on by default, until 20 Nov 2026 (owner's choice: fault-chasing detail belongs behind Detailed logs).</summary>
    public static readonly Phobos.Logging.PhobosLog TraceLog = Phobos.Framework.Logging.Logs.For("D'thorga Patches", "Harrier disassembly");

    public VehiclePatchesMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<VehiclePatchesSettings>();
        AddSettingsPages();
        var harmony = new Harmony(HarmonyId);
        RimPactsArrivalFilter.Watch(harmony);
        AirborneCrewKeepsMap.Apply(harmony);
        // The attribute patches: the sortie gate's reason and the flight-control rule. A saved Harrier's new parts are created by
        // D'thorga Framework's vehicle systems (0.10.0), which load whenever Vehicle Framework does.
        HarmonyPatchApplier.ApplyEach(harmony, typeof(VehiclePatchesMod).Assembly, LogPrefix);
        // Temporary: the disassembly trace's patch on the fixed-wing framework's own Destroy, found by name once every assembly is in.
        LongEventHandler.ExecuteWhenFinished(() => DisassemblyTrace.Apply(harmony));
        // The salvo button on a map (Patches 0.21.0): D'thorga Framework shows it on the vehicles registered here, once the Harrier's types are in.
        LongEventHandler.ExecuteWhenFinished(HarrierSalvo.Register);
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
        if (TraceLog.DebugEnabled) report.On("a temporary trace of each step when a vehicle is taken apart, to find why the Harrier cannot be");
        else report.Off("a temporary trace of vehicle disassembly", "its detailed log is not ticked");
        report.Log();
    }


    static readonly SettingsBook SettingsPages = SettingsBook.For("PhobosDthorga.Patches", () => "PhobosPatches_SettingsBook".Translate());
    static readonly VehiclePatchesSettings Defaults = new();
    protected override SettingsBook Book => SettingsPages;

    void AddSettingsPages() =>
        SettingsPages.Join(this).Add(new SettingsPage("harrier", 20f, () => "PhobosVehiclePatches_Page".Translate(), DrawSettings));

    static void DrawSettings(SettingsList list)
    {
        VehiclePatchesSettings s = Settings, d = Defaults;
        list.Section("PhobosVehiclePatches_SectionSorties".Translate());
        list.Check("PhobosVehiclePatches_SettingOverflight".Translate(), ref s.sortieOverUnjoinedBattle, d.sortieOverUnjoinedBattle,
            "PhobosVehiclePatches_SettingOverflightDesc".Translate());
        list.Warning(s.sortieOverUnjoinedBattle ? "PhobosVehiclePatches_OverflightWarning".Translate().ToString() : null);
        list.Section("PhobosVehiclePatches_SectionDiagnostics".Translate());
        list.Note("PhobosVehiclePatches_TraceMoved".Translate());
    }
}
