using System.Reflection;
using HarmonyLib;
using Phobos.Framework;
using Phobos.MissilePatches.Rules;
using Phobos.Shared;
using UnityEngine;
using Verse;

namespace Phobos.MissilePatches;

/// <summary>Player preferences. The saved keys never change.</summary>
public class MissilePatchesSettings : ModSettings
{
    public bool spreadBlast = true;
    public float frameBudgetMs = SweepBudgetRule.DefaultFrameBudgetMs;
    public bool holdRoomRebuilds = true;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref spreadBlast, "PhobosMissilePatches_SpreadBlast", true);
        Scribe_Values.Look(ref frameBudgetMs, "PhobosMissilePatches_FrameBudgetMs", SweepBudgetRule.DefaultFrameBudgetMs);
        Scribe_Values.Look(ref holdRoomRebuilds, "PhobosMissilePatches_HoldRoomRebuilds", true);
    }
}

/// <summary>
/// D'thorga Patches' InterRim Ballistic Missile part (D'thorga Missile Patches until 7 November 2026; the namespace keeps the old
/// name), switched on only while that mod is active (this assembly lives in its load folder): a strategic warhead's blast is
/// spread over frames instead of hanging the game.
/// </summary>
public class MissilePatchesMod : Mod
{
    /// <summary>The Harmony ID is the packageId; never change it once the mod is published.</summary>
    public const string PackageId = "PhobosDthorga.Patches";
    public const string ModVersion = "0.7.0";
    public const string LogPrefix = "[D'thorga Patches]";

    public static MissilePatchesSettings Settings { get; private set; } = new();

    public MissilePatchesMod(ModContentPack content) : base(content)
    {
        // The settings file is named after the mod's folder, which changed when the patch mods were merged (7 Nov 2026).
        bool fresh = !SettingsImport.HasOwnFile(this);
        Settings = GetSettings<MissilePatchesSettings>();
        if (fresh && SettingsImport.ImportInto(Settings, "PhobosMissilePatches", nameof(MissilePatchesMod)))
        {
            LongEventHandler.ExecuteWhenFinished(WriteSettings);
            Log.Message(LogPrefix + " Settings carried over from D'thorga Missile Patches.");
        }
        HarmonyPatchApplier.ApplyEach(new Harmony(PackageId), Assembly.GetExecutingAssembly(), LogPrefix);
        LongEventHandler.ExecuteWhenFinished(LogLoaded);
    }

    static void LogLoaded()
    {
        var report = new LoadReport(LogPrefix, ModVersion, "InterRim Ballistic Missile");
        if (Settings.spreadBlast) report.On("strategic blasts spread over frames, " + SweepBudgetRule.ClampBudget(Settings.frameBudgetMs).ToString("0") + " ms a frame");
        else report.Off("strategic blasts spread over frames", "setting");
        if (!Settings.holdRoomRebuilds) report.Off("rooms rebuilt once a frame during a blast", "setting");
        else if (!StrategicSweeps.CanHoldRoomRebuilds) report.Off("rooms rebuilt once a frame during a blast", "the game's room updater has no working flag");
        else report.On("rooms rebuilt once a frame during a blast");
        report.Log();
    }

    public override string SettingsCategory() => "PhobosMissilePatches_SettingsCategory".Translate();

    public override void DoSettingsWindowContents(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        SettingsUi.Section(listing, "PhobosMissilePatches_SectionIrbm".Translate());
        listing.CheckboxLabeled("PhobosMissilePatches_SettingSpread".Translate(), ref Settings.spreadBlast, "PhobosMissilePatches_SettingSpreadDesc".Translate());
        listing.Gap();
        listing.Label("PhobosMissilePatches_SettingBudget".Translate(SweepBudgetRule.ClampBudget(Settings.frameBudgetMs).ToString("0").Named("MS")),
            tooltip: "PhobosMissilePatches_SettingBudgetDesc".Translate());
        Settings.frameBudgetMs = listing.Slider(SweepBudgetRule.ClampBudget(Settings.frameBudgetMs), SweepBudgetRule.MinFrameBudgetMs, SweepBudgetRule.MaxFrameBudgetMs);
        listing.Gap();
        listing.CheckboxLabeled("PhobosMissilePatches_SettingHoldRooms".Translate(), ref Settings.holdRoomRebuilds, "PhobosMissilePatches_SettingHoldRoomsDesc".Translate());
        listing.End();
    }
}
