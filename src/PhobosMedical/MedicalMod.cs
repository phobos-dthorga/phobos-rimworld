using System.Collections.Generic;
using HarmonyLib;
using Phobos.BurnsAndSmoke;
using Phobos.Dosing;
using Phobos.Framework;
using Phobos.Framework.Uninstall;
using Phobos.MedicalSaveBridge;
using Phobos.Shared;
using Phobos.Triage;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.Medical;

/// <summary>
/// Every part's player preferences, saved in one file. Each part keeps its own settings class and its own saved keys, written flat
/// into the one file, so the keys the five former mods used carry over unchanged. Balance lives in each part's rules Def.
/// </summary>
public class MedicalSettings : ModSettings
{
    public BurnsAndSmokeSettings burns = new();
    public DosingSettings dosing = new();
    public TriageSettings triage = new();
    public MedicalEquipmentSettings equipment = new();
    public SaveBridgeSettings bridge = new();
    public Phobos.Radiation.RadiationSettings radiation = new();

    public override void ExposeData()
    {
        base.ExposeData();
        burns.ExposeData();
        dosing.ExposeData();
        triage.ExposeData();
        equipment.ExposeData();
        bridge.ExposeData();
        radiation.ExposeData();
    }
}

/// <summary>
/// D'thorga Medical: burns and smoke, emergency dosing, triage, medical equipment and the save bridge, merged into one mod on
/// 7 November 2026 from five (docs/development/mod-consolidation.md). Each part keeps its namespace, classes, Defs and saved keys,
/// so saves made with the five mods load with this one. One settings window with a section per part, one load line per part, one
/// Harmony ID.
/// </summary>
public class MedicalMod : SettingsBookMod
{
    public const string PackageId = "PhobosDthorga.Medical";
    /// <summary>The Harmony ID is the packageId; never change it once the mod is published.</summary>
    public const string HarmonyId = PackageId;
    public const string ModVersion = "0.31.0";
    public const string LogPrefix = "[D'thorga Medical]";

    /// <summary>The five mods this one replaced: package ID, name, settings folder and Mod class (for the one-time import).</summary>
    static readonly (string PackageId, string Name, string Folder, string ModClass)[] Former =
    {
        ("PhobosDthorga.BurnsAndSmoke", "D'thorga Burns and Smoke", "PhobosBurnsAndSmoke", "BurnsAndSmokeMod"),
        ("PhobosDthorga.Dosing", "D'thorga Emergency Dosing", "PhobosDosing", "DosingMod"),
        ("PhobosDthorga.Triage", "D'thorga Triage", "PhobosTriage", "TriageMod"),
        ("PhobosDthorga.MedicalEquipment", "D'thorga Medical Equipment", "PhobosMedicalEquipment", "MedicalEquipmentMod"),
        ("PhobosDthorga.MedicalSaveBridge", "D'thorga Medical Save Bridge", "PhobosMedicalSaveBridge", "MedicalSaveBridgeMod"),
    };

    public static MedicalSettings Settings { get; private set; } = new();
    /// <summary>This mod's content pack, for the uninstall sweep (it removes every condition whose Def belongs to it).</summary>
    public static ModContentPack? Pack { get; private set; }
    /// <summary>The one display mode every part follows (it was one setting per mod until 7 November 2026).</summary>
    public static DisplayMode DisplayMode => Settings.equipment.displayMode;

    readonly List<string> imported = new();

    public MedicalMod(ModContentPack content) : base(content)
    {
        Pack = content;
        AddSettingsPages();
        bool fresh = !SettingsImport.HasOwnFile(this);
        Settings = GetSettings<MedicalSettings>();
        if (fresh) ImportFormerSettings();
        Phobos.Framework.Patching.PatchFlags.Register(BurnsAndSmokeMod.ScarringFlag,
            () => Settings.burns.causticGas && Settings.burns.causticEyes && Settings.burns.causticScarring);

        var harmony = new Harmony(HarmonyId);
        HarmonyPatchApplier.ApplyEach(harmony, typeof(MedicalMod).Assembly, LogPrefix);
        MedicalSaveBridgeMod.Start(harmony);
        MedicalEquipmentMod.Start();
        Phobos.Radiation.RadiationPart.Start(harmony);
        Protection.ProtectionPart.Start();
        LongEventHandler.ExecuteWhenFinished(LogLoaded);
    }

    /// <summary>Writes the one settings file. Parts call this rather than writing their own settings object, which has no Mod.</summary>
    public static void WriteAll() => LoadedModManager.GetMod<MedicalMod>()?.WriteSettings();

    /// <summary>The first start after the merge: each part reads the settings file its former mod left, once.</summary>
    void ImportFormerSettings()
    {
        if (SettingsImport.ImportInto(Settings.burns, Former[0].Folder, Former[0].ModClass)) imported.Add(Former[0].Name);
        if (SettingsImport.ImportInto(Settings.dosing, Former[1].Folder, Former[1].ModClass)) imported.Add(Former[1].Name);
        if (SettingsImport.ImportInto(Settings.triage, Former[2].Folder, Former[2].ModClass)) imported.Add(Former[2].Name);
        if (SettingsImport.ImportInto(Settings.equipment, Former[3].Folder, Former[3].ModClass)) imported.Add(Former[3].Name);
        if (imported.Count > 0) LongEventHandler.ExecuteWhenFinished(WriteSettings);
    }

    void LogLoaded()
    {
        foreach (var former in Former)
        {
            if (ModsConfig.IsActive(former.PackageId))
            {
                Log.Error(LogPrefix + " " + former.Name + " is switched on beside D'thorga Medical, which replaces it. Switch it off in the mod list: "
                    + "running both loads the same parts twice.");
            }
        }
        if (imported.Count > 0) Log.Message(LogPrefix + " Settings carried over from " + string.Join(", ", imported) + ".");
        BurnsAndSmokeMod.Report(Part("burns and smoke")).Log();
        DosingMod.Report(Part("emergency dosing")).Log();
        TriageMod.Report(Part("triage")).Log();
        MedicalEquipmentMod.Report(Part("equipment")).Log();
        Protection.ProtectionPart.Report(Part("protective gear")).Log();
        Phobos.Radiation.RadiationPart.Report(Part("radiation")).Log();
    }

    static LoadReport Part(string name) => new(LogPrefix, ModVersion, name);

    static readonly SettingsBook SettingsPages = SettingsBook.For(PackageId, () => "PhobosMedical_SettingsCategory".Translate());
    protected override SettingsBook Book => SettingsPages;

    /// <summary>A page per part (owner's choice of 20 Nov 2026: pages with search, docs/development/settings-pages.md).</summary>
    void AddSettingsPages()
    {
        SettingsPages.Join(this)
            .Add(new SettingsPage("general", 0f, () => "PhobosMedical_PageGeneral".Translate(), DrawGeneral))
            .Add(new SettingsPage("burns", 10f, () => "PhobosMedical_SectionBurns".Translate(), BurnsAndSmokeMod.DrawSettings))
            .Add(new SettingsPage("caustic", 20f, () => "PhobosMedical_PageCaustic".Translate(), BurnsAndSmokeMod.DrawCausticSettings))
            .Add(new SettingsPage("dosing", 30f, () => "PhobosMedical_SectionDosing".Translate(), DosingMod.DrawSettings))
            .Add(new SettingsPage("triage", 40f, () => "PhobosMedical_SectionTriage".Translate(), TriageMod.DrawSettings))
            .Add(new SettingsPage("radiation", 45f, () => "PhobosMedical_PageRadiation".Translate(), Phobos.Radiation.RadiationPart.DrawSettings))
            .Add(new SettingsPage("bridge", 50f, () => "PhobosMedical_SectionSaveBridge".Translate(), MedicalSaveBridgeMod.DrawSettings))
            .Add(new SettingsPage("removing", 60f, () => "PhobosMedical_SectionRemoving".Translate(), DrawRemoving));
    }

    static void DrawGeneral(SettingsList list)
    {
        SettingsUi.DisplayModeRow(list, ref Settings.equipment.displayMode);
        list.Section("PhobosMedical_SectionEquipment".Translate());
        MedicalEquipmentMod.DrawSettings(list);
    }

    static void DrawRemoving(SettingsList list)
    {
        list.Note("PhobosMedical_SettingRemoveAllDesc".Translate());
        if (list.Button("PhobosMedical_SettingRemoveAll".Translate(), "PhobosMedical_SettingRemoveAllDesc".Translate())) RemoveEveryCondition();
    }

    /// <summary>The one uninstall button: every condition whose Def belongs to this mod, from every pawn, alive or dead.</summary>
    static void RemoveEveryCondition()
    {
        if (Current.Game == null || Pack == null)
        {
            Messages.Message("PhobosMedical_RemoveNeedsGame".Translate(), MessageTypeDefOf.RejectInput, false);
            return;
        }
        int removed = CleanupService.RemoveHediffsOf(Pack);
        Messages.Message("PhobosMedical_RemoveDone".Translate(removed.Named("COUNT")), MessageTypeDefOf.TaskCompletion, false);
    }
}
