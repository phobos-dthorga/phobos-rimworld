using HarmonyLib;
using Phobos.Framework;
using Phobos.Shared;
using UnityEngine;
using Verse;

namespace Phobos.Farming;

/// <summary>D'thorga Farming's player preferences (docs/development/farming-batch-one.md, "Settings"). Saved keys are stable: never rename.</summary>
public class FarmingSettings : ModSettings
{
    /// <summary>Whether a growing zone newly marked for machines lets colonists sow and harvest it by hand too. Off: machines only.</summary>
    public bool handsMayHelpOnNew;
    /// <summary>Whether a machine's driver can fail a harvest as a colonist by hand can, by the same roll on their harvesting skill.</summary>
    public bool driverCanFail = true;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref handsMayHelpOnNew, "handsMayHelpOnNew");
        Scribe_Values.Look(ref driverCanFail, "driverCanFail", true);
    }
}

/// <summary>
/// D'thorga Farming (docs/development/farming-machines.md, farming-batch-one.md): machine-worked paddocks and the machines that work them.
/// Batch one: paddocks, the combine harvester with its grain fronts, the chaser bin, the field bin and the silo. One settings window,
/// one load line, one Harmony ID.
/// </summary>
public class FarmingMod : SettingsBookMod
{
    public const string PackageId = "PhobosDthorga.Farming";
    /// <summary>The Harmony ID is the packageId; never change it once the mod is published.</summary>
    public const string HarmonyId = PackageId;
    public const string ModVersion = "0.4.1";
    public const string LogPrefix = "[D'thorga Farming]";

    public static readonly Phobos.Logging.PhobosLog Logger = Framework.Logging.Logs.For("D'thorga Farming");

    public static FarmingSettings Settings { get; private set; } = new();

    static readonly SettingsBook SettingsPages = SettingsBook.For(PackageId, () => "PhobosFarming_SettingsCategory".Translate());
    protected override SettingsBook Book => SettingsPages;

    public FarmingMod(ModContentPack content) : base(content)
    {
        SettingsPages.Join(this).Add(new SettingsPage("general", 0f, () => "PhobosFarming_PageGeneral".Translate(), DrawGeneral));
        Settings = GetSettings<FarmingSettings>();
        HarmonyPatchApplier.ApplyEach(new Harmony(HarmonyId), typeof(FarmingMod).Assembly, LogPrefix);
        LongEventHandler.ExecuteWhenFinished(() => new LoadReport(LogPrefix, ModVersion)
            .On("paddocks").On("combine harvester").On("bulk bins").Log());
    }

    static void DrawGeneral(SettingsList list)
    {
        FarmingSettings s = Settings;
        list.Section("PhobosFarming_SectionPaddocks".Translate());
        list.Check("PhobosFarming_SettingHandsMayHelp".Translate(), ref s.handsMayHelpOnNew, false, "PhobosFarming_SettingHandsMayHelpDesc".Translate());
        list.Section("PhobosFarming_SectionHarvest".Translate());
        list.Check("PhobosFarming_SettingDriverCanFail".Translate(), ref s.driverCanFail, true, "PhobosFarming_SettingDriverCanFailDesc".Translate());
    }
}
