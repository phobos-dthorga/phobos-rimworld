using System;
using System.Reflection;
using HarmonyLib;
using Phobos.Framework;
using Phobos.Framework.Work;
using Phobos.MissilePatches.Rules;
using Phobos.MissilePatches.Strike;
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
    public bool blastModel = true;
    public bool fullStrikeEverywhere;
    public FireMode fires = FireMode.Moderate;
    public FalloutMode fallout = FalloutMode.Automatic;
    public int falloutClouds = MaxFalloutClouds;
    public bool radiation = true;
    public bool pollution = true;
    public bool emp = true;
    public bool quietReveals = true;

    /// <summary>Dubs Rimatomics' own high-yield strike makes six (WorldObject_ICBMfission, read 7 Nov 2026).</summary>
    public const int MaxFalloutClouds = 6;

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
        Scribe_Values.Look(ref blastModel, "PhobosMissilePatches_BlastModel", true);
        Scribe_Values.Look(ref fullStrikeEverywhere, "PhobosMissilePatches_FullStrikeEverywhere", false);
        Scribe_Values.Look(ref fires, "PhobosMissilePatches_Fires", FireMode.Moderate);
        Scribe_Values.Look(ref fallout, "PhobosMissilePatches_Fallout", FalloutMode.Automatic);
        Scribe_Values.Look(ref falloutClouds, "PhobosMissilePatches_FalloutClouds", MaxFalloutClouds);
        Scribe_Values.Look(ref radiation, "PhobosMissilePatches_Radiation", true);
        Scribe_Values.Look(ref pollution, "PhobosMissilePatches_Pollution", true);
        Scribe_Values.Look(ref emp, "PhobosMissilePatches_Emp", true);
        Scribe_Values.Look(ref quietReveals, "PhobosMissilePatches_QuietReveals", true);
    }
}

/// <summary>
/// D'thorga Patches' InterRim Ballistic Missile part (D'thorga Missile Patches until 7 November 2026; the namespace keeps the old
/// name), switched on only while that mod is active (this assembly lives in its load folder). A strategic warhead's blast runs a slice
/// per frame through D'thorga Framework's queue, by the missile mod's rules but without the costly damage path (0.8.0); a warhead
/// marked as a blast gets D'thorga's own strike, with zones from ground zero and fallout (0.9.0); the caves and vaults a blast breaks
/// open send no letters (0.10.0).
/// </summary>
public class MissilePatchesMod : Mod
{
    /// <summary>The Harmony ID is the packageId; never change it once the mod is published.</summary>
    public const string PackageId = "PhobosDthorga.Patches";
    public const string ModVersion = "0.11.0";
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
        var harmony = new Harmony(PackageId);
        HarmonyPatchApplier.ApplyEach(harmony, Assembly.GetExecutingAssembly(), LogPrefix);
        Strike.RimPactsAssaultSize.Apply(harmony);
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
        if (OptionalMods.IsActive("wowgag.RimPacts"))
        {
            if (Strike.RimPactsAssaultSize.Applied) report.On("an ally's RimPacts assault on a struck town faces its garrison from before the strike");
            else report.Off("an ally's RimPacts assault on a struck town faces its garrison from before the strike", "RimPacts' assault code has changed");
        }
        report.On(Settings.rubble ? "buildings leave rubble" : "buildings vaporised");
        report.On("corpses " + (Settings.corpses == CorpseMode.Vaporised ? "vaporised" : "left"));
        if (Settings.quietReveals) report.On("no \"Area revealed\" letters during a blast");
        else report.Off("no \"Area revealed\" letters during a blast", "setting");
        if (!Settings.holdRoomRebuilds) report.Off("rooms rebuilt once a frame during a blast", "setting");
        else if (!RoomRebuildHold.Available) report.Off("rooms rebuilt once a frame during a blast", "the game's room updater has no working flag");
        else report.On("rooms rebuilt once a frame during a blast");
        if (Settings.blastModel)
        {
            report.On("D'thorga's blast model for marked warheads, " + (Settings.fullStrikeEverywhere ? "full strike on every map in reach" : "heat and fallout only beyond the target"));
            report.On("fires " + Settings.fires + ", fallout " + Settings.fallout + (RimatomicsTieIn.Active ? " (Rimatomics active)" : " (Rimatomics not active)"));
            if (!Settings.emp) report.Off("electromagnetic pulse", "setting");
            if (!Settings.pollution) report.Off("ground pollution", "setting");
        }
        else report.Off("D'thorga's blast model", "setting");
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
        listing.CheckboxLabeled("PhobosMissilePatches_SettingQuietReveals".Translate(), ref Settings.quietReveals, "PhobosMissilePatches_SettingQuietRevealsDesc".Translate());

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
        DrawAfterEffects(listing);
        scroll.End(listing);
    }

    static void DrawAfterEffects(Listing_Standard listing)
    {
        SettingsUi.Section(listing, "PhobosMissilePatches_SectionAfterEffects".Translate());
        listing.CheckboxLabeled("PhobosMissilePatches_SettingBlastModel".Translate(), ref Settings.blastModel, "PhobosMissilePatches_SettingBlastModelDesc".Translate());
        SettingsUi.Warning(listing, Settings.blastModel ? null : "PhobosMissilePatches_WarnBlastModelOff".Translate().ToString());
        if (!Settings.blastModel) return;
        listing.CheckboxLabeled("PhobosMissilePatches_SettingFullStrike".Translate(), ref Settings.fullStrikeEverywhere, "PhobosMissilePatches_SettingFullStrikeDesc".Translate());
        SettingsUi.Warning(listing, Settings.fullStrikeEverywhere ? "PhobosMissilePatches_WarnFullStrike".Translate().ToString() : null);
        SettingsUi.Choice(listing, "PhobosMissilePatches_SettingFires".Translate(), Settings.fires, (FireMode[])Enum.GetValues(typeof(FireMode)),
            mode => ("PhobosMissilePatches_Fires" + mode).Translate(), mode => Settings.fires = mode, "PhobosMissilePatches_SettingFiresDesc".Translate());
        SettingsUi.Warning(listing, Settings.fires >= FireMode.Moderate ? "PhobosMissilePatches_WarnFires".Translate().ToString() : null);
        SettingsUi.Choice(listing, "PhobosMissilePatches_SettingFallout".Translate(), Settings.fallout, (FalloutMode[])Enum.GetValues(typeof(FalloutMode)),
            mode => ("PhobosMissilePatches_Fallout" + mode).Translate(), mode => Settings.fallout = mode, "PhobosMissilePatches_SettingFalloutDesc".Translate());
        if (Settings.fallout == FalloutMode.Automatic && RimatomicsTieIn.Active)
        {
            listing.Label("PhobosMissilePatches_SettingClouds".Translate(Settings.falloutClouds.Named("COUNT")), tooltip: "PhobosMissilePatches_SettingCloudsDesc".Translate());
            Settings.falloutClouds = Mathf.RoundToInt(listing.Slider(Settings.falloutClouds, 0f, MissilePatchesSettings.MaxFalloutClouds));
            listing.CheckboxLabeled("PhobosMissilePatches_SettingRadiation".Translate(), ref Settings.radiation, "PhobosMissilePatches_SettingRadiationDesc".Translate());
        }
        else if (Settings.fallout == FalloutMode.Automatic)
        {
            listing.Label("PhobosMissilePatches_RimatomicsAbsent".Translate());
        }
        if (ModsConfig.BiotechActive)
        {
            listing.CheckboxLabeled("PhobosMissilePatches_SettingPollution".Translate(), ref Settings.pollution, "PhobosMissilePatches_SettingPollutionDesc".Translate());
        }
        listing.CheckboxLabeled("PhobosMissilePatches_SettingEmp".Translate(), ref Settings.emp, "PhobosMissilePatches_SettingEmpDesc".Translate());
    }

    static float SliceBudget(float value) => value < IrbmBudgetRule.MinFrameBudgetMs ? IrbmBudgetRule.MinFrameBudgetMs
        : value > IrbmBudgetRule.MaxFrameBudgetMs ? IrbmBudgetRule.MaxFrameBudgetMs : value;
}
