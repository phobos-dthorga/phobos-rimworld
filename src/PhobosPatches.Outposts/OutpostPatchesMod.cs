using HarmonyLib;
using Phobos.Framework;
using Phobos.Shared;
using UnityEngine;
using Verse;

namespace Phobos.OutpostPatches;

public class OutpostPatchesSettings : ModSettings
{
    /// <summary>The founding button refuses a caravan that holds a vehicle. Off is the outposts code's original behaviour.</summary>
    public bool refuseConvoyFounding = true;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref refuseConvoyFounding, "PhobosOutpostPatches_RefuseConvoyFounding", true);
    }
}

/// <summary>
/// D'thorga Patches' outposts part (from 0.15.0), switched on only while Vanilla Expanded Framework and Vehicle Framework are both active
/// (this assembly lives in a load folder that needs both). The setting is read when the dialog opens, so changing it needs no restart.
/// </summary>
public class OutpostPatchesMod : SettingsBookMod
{
    /// <summary>The Harmony ID is the packageId, shared by every part; never change it once the mod is published.</summary>
    public const string PackageId = "PhobosDthorga.Patches";
    public const string ModVersion = "0.33.0";
    public const string LogPrefix = "[D'thorga Patches]";

    public static OutpostPatchesSettings Settings { get; private set; } = new();

    public OutpostPatchesMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<OutpostPatchesSettings>();
        AddSettingsPages();
        HarmonyPatchApplier.ApplyEach(new Harmony(PackageId), typeof(OutpostPatchesMod).Assembly, LogPrefix);
        LongEventHandler.ExecuteWhenFinished(LogLoaded);
    }

    static void LogLoaded()
    {
        var report = new LoadReport(LogPrefix, ModVersion, "Outposts");
        const string feature = "a caravan with vehicles cannot found an outpost through the normal button";
        if (Settings.refuseConvoyFounding) report.On(feature);
        else report.Off(feature, "its setting is off, as the player chose");
        report.Log();
    }


    static readonly SettingsBook SettingsPages = SettingsBook.For("PhobosDthorga.Patches", () => "PhobosPatches_SettingsBook".Translate());
    static readonly OutpostPatchesSettings Defaults = new();
    protected override SettingsBook Book => SettingsPages;

    void AddSettingsPages() =>
        SettingsPages.Join(this).Add(new SettingsPage("outposts", 50f, () => "PhobosOutpostPatches_Page".Translate(), DrawSettings));

    static void DrawSettings(SettingsList list)
    {
        OutpostPatchesSettings s = Settings, d = Defaults;
        list.Section("PhobosOutpostPatches_SectionConvoys".Translate());
        list.Note("PhobosOutpostPatches_WhyThisExists".Translate());
        list.Check("PhobosOutpostPatches_SettingRefuse".Translate(), ref s.refuseConvoyFounding, d.refuseConvoyFounding, "PhobosOutpostPatches_SettingRefuseDesc".Translate());
        list.Warning(s.refuseConvoyFounding ? null : "PhobosOutpostPatches_OriginalBehaviourWarning".Translate().ToString());
    }
}
