using System;
using System.Reflection;
using HarmonyLib;
using Phobos.Framework;
using Phobos.Framework.Work;
using Phobos.MissilePatches.Rules;
using Phobos.Shared;
using UnityEngine;
using Verse;

namespace Phobos.MissilePatches;

/// <summary>Player preferences for the missile mod's strikes. The saved keys never change; defaults are agent choices for the owner.</summary>
public class MissilePatchesSettings : ModSettings
{
    public bool spreadBlast = true;
    public float frameBudgetMs = IrbmBudgetRule.DefaultFrameBudgetMs;
    public bool whiteScreenBudget = true;
    public bool holdRoomRebuilds = true;
    public ShelterMode shelter = ShelterMode.AnyRockRoof;
    public bool rubble;
    public CorpseMode corpses = CorpseMode.Vaporised;
    public FragmentMode fragments = FragmentMode.None;
    public bool messages = true;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref spreadBlast, "PhobosMissilePatches_SpreadBlast", true);
        Scribe_Values.Look(ref frameBudgetMs, "PhobosMissilePatches_FrameBudgetMs", IrbmBudgetRule.DefaultFrameBudgetMs);
        Scribe_Values.Look(ref whiteScreenBudget, "PhobosMissilePatches_WhiteScreenBudget", true);
        Scribe_Values.Look(ref holdRoomRebuilds, "PhobosMissilePatches_HoldRoomRebuilds", true);
        Scribe_Values.Look(ref shelter, "PhobosMissilePatches_Shelter", ShelterMode.AnyRockRoof);
        Scribe_Values.Look(ref rubble, "PhobosMissilePatches_Rubble", false);
        Scribe_Values.Look(ref corpses, "PhobosMissilePatches_Corpses", CorpseMode.Vaporised);
        Scribe_Values.Look(ref fragments, "PhobosMissilePatches_Fragments", FragmentMode.None);
        Scribe_Values.Look(ref messages, "PhobosMissilePatches_Messages", true);
    }
}

/// <summary>
/// D'thorga Patches' InterRim Ballistic Missile part (D'thorga Missile Patches until 7 November 2026; the namespace keeps the old
/// name), switched on only while that mod is active (this assembly lives in its load folder). A strategic warhead's blast runs a slice
/// per frame through D'thorga Framework's queue, by the missile mod's rules but without the costly damage path.
/// </summary>
public class MissilePatchesMod : Mod
{
    /// <summary>The Harmony ID is the packageId; never change it once the mod is published.</summary>
    public const string PackageId = "PhobosDthorga.Patches";
    public const string ModVersion = "0.8.0";
    public const string LogPrefix = "[D'thorga Patches]";

    public static MissilePatchesSettings Settings { get; private set; } = new();

    readonly SettingsScroll scroll = new();

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

    /// <summary>The frame budget for a blast that started <paramref name="secondsSinceStart"/> seconds ago.</summary>
    public static float BudgetMs(float secondsSinceStart) =>
        IrbmBudgetRule.FrameBudget(Settings.frameBudgetMs, Settings.whiteScreenBudget, secondsSinceStart);

    static void LogLoaded()
    {
        var report = new LoadReport(LogPrefix, ModVersion, "InterRim Ballistic Missile");
        if (!Settings.spreadBlast)
        {
            report.Off("strategic blasts spread over frames", "setting");
            report.Log();
            return;
        }
        report.On("strategic blasts spread over frames, " + BudgetMs(999f).ToString("0") + " ms a frame"
            + (Settings.whiteScreenBudget ? " (" + IrbmBudgetRule.WhiteScreenBudgetMs.ToString("0") + " while the screen is white)" : ""));
        report.On("shelter: " + Settings.shelter);
        report.On(Settings.rubble ? "buildings leave rubble" : "buildings vaporised");
        report.On("corpses " + (Settings.corpses == CorpseMode.Vaporised ? "vaporised" : "left"));
        if (!Settings.holdRoomRebuilds) report.Off("rooms rebuilt once a frame during a blast", "setting");
        else if (!RoomRebuildHold.Available) report.Off("rooms rebuilt once a frame during a blast", "the game's room updater has no working flag");
        else report.On("rooms rebuilt once a frame during a blast");
        if (OptionalMods.IsActive(OptionalMods.CombatExtended))
        {
            if (CombatExtendedFragments.Present) report.On("Combat Extended's wall fragments during a strike: " + Settings.fragments
                + (CombatExtendedFragments.OnInCombatExtended ? "" : " (off in Combat Extended anyway)"));
            else report.Off("Combat Extended's wall fragments during a strike", "its setting was not found by name");
        }
        report.Log();
    }

    public override string SettingsCategory() => "PhobosMissilePatches_SettingsCategory".Translate();

    public override void DoSettingsWindowContents(Rect inRect)
    {
        Listing_Standard listing = scroll.Begin(inRect);
        SettingsUi.Section(listing, "PhobosMissilePatches_SectionIrbm".Translate());
        listing.Label("PhobosMissilePatches_SectionBlast".Translate());
        listing.CheckboxLabeled("PhobosMissilePatches_SettingSpread".Translate(), ref Settings.spreadBlast, "PhobosMissilePatches_SettingSpreadDesc".Translate());
        SettingsUi.Warning(listing, Settings.spreadBlast ? null : "PhobosMissilePatches_WarnSpreadOff".Translate().ToString());

        float budget = SliceBudget(Settings.frameBudgetMs);
        listing.Label("PhobosMissilePatches_SettingBudget".Translate(budget.ToString("0").Named("MS")), tooltip: "PhobosMissilePatches_SettingBudgetDesc".Translate());
        Settings.frameBudgetMs = listing.Slider(budget, IrbmBudgetRule.MinFrameBudgetMs, IrbmBudgetRule.MaxFrameBudgetMs);
        SettingsUi.Warning(listing, budget > IrbmBudgetRule.StutterWarningMs ? "PhobosMissilePatches_WarnBudget".Translate().ToString() : null);
        listing.CheckboxLabeled("PhobosMissilePatches_SettingWhiteScreen".Translate(IrbmBudgetRule.WhiteScreenBudgetMs.ToString("0").Named("MS")),
            ref Settings.whiteScreenBudget, "PhobosMissilePatches_SettingWhiteScreenDesc".Translate());

        SettingsUi.Choice(listing, "PhobosMissilePatches_SettingShelter".Translate(), Settings.shelter, (ShelterMode[])Enum.GetValues(typeof(ShelterMode)),
            mode => ("PhobosMissilePatches_Shelter" + mode).Translate(), mode => Settings.shelter = mode, "PhobosMissilePatches_SettingShelterDesc".Translate());
        SettingsUi.Warning(listing, Settings.shelter == ShelterMode.AnyRockRoof ? null : "PhobosMissilePatches_WarnShelter".Translate().ToString());

        listing.CheckboxLabeled("PhobosMissilePatches_SettingRubble".Translate(), ref Settings.rubble, "PhobosMissilePatches_SettingRubbleDesc".Translate());
        SettingsUi.Warning(listing, Settings.rubble ? "PhobosMissilePatches_WarnRubble".Translate().ToString() : null);

        SettingsUi.Choice(listing, "PhobosMissilePatches_SettingCorpses".Translate(), Settings.corpses, (CorpseMode[])Enum.GetValues(typeof(CorpseMode)),
            mode => ("PhobosMissilePatches_Corpses" + mode).Translate(), mode => Settings.corpses = mode, "PhobosMissilePatches_SettingCorpsesDesc".Translate());
        listing.CheckboxLabeled("PhobosMissilePatches_SettingHoldRooms".Translate(), ref Settings.holdRoomRebuilds, "PhobosMissilePatches_SettingHoldRoomsDesc".Translate());
        listing.CheckboxLabeled("PhobosMissilePatches_SettingMessages".Translate(), ref Settings.messages, "PhobosMissilePatches_SettingMessagesDesc".Translate());

        if (CombatExtendedFragments.Present)
        {
            SettingsUi.Section(listing, "PhobosMissilePatches_SectionCombatExtended".Translate());
            SettingsUi.Choice(listing, "PhobosMissilePatches_SettingFragments".Translate(), Settings.fragments, (FragmentMode[])Enum.GetValues(typeof(FragmentMode)),
                mode => ("PhobosMissilePatches_Fragments" + mode).Translate(), mode => Settings.fragments = mode, "PhobosMissilePatches_SettingFragmentsDesc".Translate());
            string? warning = Settings.fragments switch
            {
                FragmentMode.Reduced => "PhobosMissilePatches_WarnFragmentsReduced".Translate().ToString(),
                FragmentMode.AsCombatExtended => "PhobosMissilePatches_WarnFragmentsFull".Translate().ToString(),
                _ => null,
            };
            SettingsUi.Warning(listing, warning);
            if (!CombatExtendedFragments.OnInCombatExtended) listing.Label("PhobosMissilePatches_FragmentsOffInCombatExtended".Translate());
        }
        scroll.End(listing);
    }

    static float SliceBudget(float value) => value < IrbmBudgetRule.MinFrameBudgetMs ? IrbmBudgetRule.MinFrameBudgetMs
        : value > IrbmBudgetRule.MaxFrameBudgetMs ? IrbmBudgetRule.MaxFrameBudgetMs : value;
}
