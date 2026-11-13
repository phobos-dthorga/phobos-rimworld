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
public class OutpostPatchesMod : Mod
{
    /// <summary>The Harmony ID is the packageId, shared by every part; never change it once the mod is published.</summary>
    public const string PackageId = "PhobosDthorga.Patches";
    public const string ModVersion = "0.19.1";
    public const string LogPrefix = "[D'thorga Patches]";

    public static OutpostPatchesSettings Settings { get; private set; } = new();

    public OutpostPatchesMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<OutpostPatchesSettings>();
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

    public override string SettingsCategory() => "PhobosOutpostPatches_SettingsCategory".Translate();

    public override void DoSettingsWindowContents(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        SettingsUi.Section(listing, "PhobosOutpostPatches_SectionConvoys".Translate());
        listing.CheckboxLabeled("PhobosOutpostPatches_SettingRefuse".Translate(), ref Settings.refuseConvoyFounding,
            "PhobosOutpostPatches_SettingRefuseDesc".Translate());
        listing.Gap(6f);
        listing.Label("PhobosOutpostPatches_WhyThisExists".Translate());
        if (!Settings.refuseConvoyFounding) SettingsUi.Warning(listing, "PhobosOutpostPatches_OriginalBehaviourWarning".Translate());
        listing.End();
    }
}
