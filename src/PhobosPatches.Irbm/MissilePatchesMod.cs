using System;
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
    /// <summary>Vehicles take their own route through the strike (Patches 0.32.0; owner, 22 Nov 2026: graded with the strike, on).</summary>
    public bool vehiclesInStrike = true;
    /// <summary>Shadow and shelter (Patches 0.36.0, NK12, NK17, NK18; owner, 22 Nov 2026: with our own radiation model, a setting): any roof
    /// shadows the heat and flash, the dose is cut by mass, flash blindness follows the light, and doses are told on a letter.</summary>
    public bool shadowAndShelter = true;
    public bool quietReveals = true;
    /// <summary>IRBM's CIWS also intercepts Combat Extended's shells (Patches 0.19.1, AD14; owner, 13 Nov 2026: patch it).</summary>
    public bool ciwsShells = true;
    /// <summary>IRBM's CIWS also fires at enemy aircraft in the air over the map, D'thorga's flight (Patches 0.44.0; the owner's ask of 26 Nov
    /// 2026). It aims only at aircraft hostile to the player, so it starts on. Live.</summary>
    public bool ciwsAircraft = true;

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
        Scribe_Values.Look(ref vehiclesInStrike, "PhobosMissilePatches_VehiclesInStrike", true);
        Scribe_Values.Look(ref shadowAndShelter, "PhobosMissilePatches_ShadowAndShelter", true);
        Scribe_Values.Look(ref quietReveals, "PhobosMissilePatches_QuietReveals", true);
        Scribe_Values.Look(ref ciwsShells, "PhobosMissilePatches_CiwsShells", true);
        Scribe_Values.Look(ref ciwsAircraft, "PhobosMissilePatches_CiwsAircraft", true);
    }
}

/// <summary>
/// D'thorga Patches' InterRim Ballistic Missile part (D'thorga Missile Patches until 7 November 2026; the namespace keeps the old
/// name), switched on only while that mod is active (this assembly lives in its load folder). A strategic warhead's blast runs a slice
/// per frame through D'thorga Framework's queue, by the missile mod's rules but without the costly damage path (0.8.0); a warhead
/// marked as a blast gets D'thorga's own strike, with zones from ground zero and fallout (0.9.0); the caves and vaults a blast breaks
/// open send no letters (0.10.0).
/// </summary>
public class MissilePatchesMod : SettingsBookMod
{
    /// <summary>The Harmony ID is the packageId; never change it once the mod is published.</summary>
    public const string PackageId = "PhobosDthorga.Patches";
    public const string ModVersion = "0.48.0";
    public const string LogPrefix = "[D'thorga Patches]";
    public static readonly Phobos.Logging.PhobosLog Logger = Phobos.Framework.Logging.Logs.For("D'thorga Patches", "IRBM");

    public static MissilePatchesSettings Settings { get; private set; } = new();


    public MissilePatchesMod(ModContentPack content) : base(content)
    {
        // The settings file is named after the mod's folder, which changed when the patch mods were merged (7 Nov 2026).
        bool fresh = !SettingsImport.HasOwnFile(this);
        Settings = GetSettings<MissilePatchesSettings>();
        AddSettingsPages();
        if (fresh && SettingsImport.ImportInto(Settings, "PhobosMissilePatches", nameof(MissilePatchesMod)))
        {
            LongEventHandler.ExecuteWhenFinished(WriteSettings);
            Logger.Info("Settings carried over from D'thorga Missile Patches.");
        }
        var harmony = new Harmony(PackageId);
        // By type, never Assembly.GetExecutingAssembly(): inside a constructor another mod has patched, Harmony 3.0.3.0 on RimWorld 1.7
        // answers that with its own assembly, and no patch of ours would be found (17 Nov 2026; tests/test_no_executing_assembly.py).
        HarmonyPatchApplier.ApplyEach(harmony, typeof(MissilePatchesMod).Assembly, LogPrefix);
        Strike.RimPactsAssaultSize.Apply(harmony);
        // Patches 0.43.0 (docs/development/airdrop.md, "Air defence against drops"): IRBM's CIWS marks a falling load as intercepted and
        // applies its effect only in the game's skyfaller impact, which D'thorga's parachute loads never call; Framework asks this instead.
        SkyfallerInterception.Check = thing => thing is RimWorld.Skyfaller skyfaller && IRBM.SkyfallerInterceptionUtility.IsIntercepted(skyfaller);
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
            if (Settings.vehiclesInStrike) report.On("vehicles and the people aboard take their own route through the strike (with D'thorga Framework's vehicles)");
            else report.Off("vehicles' own route through the strike", "setting");
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


    static readonly SettingsBook SettingsPages = SettingsBook.For("PhobosDthorga.Patches", () => "PhobosPatches_SettingsBook".Translate());
    static readonly MissilePatchesSettings Defaults = new();
    protected override SettingsBook Book => SettingsPages;

    /// <summary>Two pages: the strike itself and the game's speed, then what a strike leaves behind.</summary>
    void AddSettingsPages() =>
        SettingsPages.Join(this)
            .Add(new SettingsPage("irbm", 30f, () => "PhobosMissilePatches_PageStrikes".Translate(), DrawStrikes))
            .Add(new SettingsPage("irbm-after", 31f, () => "PhobosMissilePatches_PageAfterEffects".Translate(), DrawAfterEffects));

    static void DrawStrikes(SettingsList list)
    {
        MissilePatchesSettings s = Settings, d = Defaults;
        list.Section("PhobosMissilePatches_SectionBlast".Translate());
        list.Check("PhobosMissilePatches_SettingSpread".Translate(), ref s.spreadBlast, d.spreadBlast, "PhobosMissilePatches_SettingSpreadDesc".Translate());
        list.Warning(s.spreadBlast ? null : "PhobosMissilePatches_WarnSpreadOff".Translate().ToString());
        float budget = SliceBudget(s.frameBudgetMs);
        if (list.Slider("PhobosMissilePatches_SettingBudget".Translate(budget.ToString("0").Named("MS")), ref budget, d.frameBudgetMs,
                IrbmBudgetRule.MinFrameBudgetMs, IrbmBudgetRule.MaxFrameBudgetMs, "PhobosMissilePatches_SettingBudgetDesc".Translate()))
        {
            s.frameBudgetMs = budget;
        }
        list.Warning(budget > IrbmBudgetRule.StutterWarningMs ? "PhobosMissilePatches_WarnBudget".Translate().ToString() : null);
        list.Check("PhobosMissilePatches_SettingWhiteScreen".Translate(IrbmBudgetRule.WhiteScreenBudgetMs.ToString("0").Named("MS")), ref s.whiteScreenBudget,
            d.whiteScreenBudget, "PhobosMissilePatches_SettingWhiteScreenDesc".Translate());
        list.Check("PhobosMissilePatches_SettingHoldRooms".Translate(), ref s.holdRoomRebuilds, d.holdRoomRebuilds, "PhobosMissilePatches_SettingHoldRoomsDesc".Translate());

        list.Section("PhobosMissilePatches_SectionWhoAndWhat".Translate());
        list.Choice("PhobosMissilePatches_SettingShelter".Translate(), s.shelter, d.shelter, (ShelterMode[])Enum.GetValues(typeof(ShelterMode)),
            mode => ("PhobosMissilePatches_Shelter" + mode).Translate(), mode => Settings.shelter = mode, "PhobosMissilePatches_SettingShelterDesc".Translate());
        list.Warning(s.shelter == ShelterMode.AnyRockRoof ? null : "PhobosMissilePatches_WarnShelter".Translate().ToString());
        list.Check("PhobosMissilePatches_SettingRubble".Translate(), ref s.rubble, d.rubble, "PhobosMissilePatches_SettingRubbleDesc".Translate());
        list.Warning(s.rubble ? "PhobosMissilePatches_WarnRubble".Translate().ToString() : null);
        list.Choice("PhobosMissilePatches_SettingCorpses".Translate(), s.corpses, d.corpses, (CorpseMode[])Enum.GetValues(typeof(CorpseMode)),
            mode => ("PhobosMissilePatches_Corpses" + mode).Translate(), mode => Settings.corpses = mode, "PhobosMissilePatches_SettingCorpsesDesc".Translate());
        list.Check("PhobosMissilePatches_SettingMessages".Translate(), ref s.messages, d.messages, "PhobosMissilePatches_SettingMessagesDesc".Translate());
        list.Check("PhobosMissilePatches_SettingQuietReveals".Translate(), ref s.quietReveals, d.quietReveals, "PhobosMissilePatches_SettingQuietRevealsDesc".Translate());

        if (CombatExtendedFragments.Present)
        {
            list.Section("PhobosMissilePatches_SectionCombatExtended".Translate());
            list.Choice("PhobosMissilePatches_SettingFragments".Translate(), s.fragments, d.fragments, (FragmentMode[])Enum.GetValues(typeof(FragmentMode)),
                mode => ("PhobosMissilePatches_Fragments" + mode).Translate(), mode => Settings.fragments = mode, "PhobosMissilePatches_SettingFragmentsDesc".Translate());
            list.Warning(s.fragments switch
            {
                FragmentMode.Reduced => "PhobosMissilePatches_WarnFragmentsReduced".Translate().ToString(),
                FragmentMode.AsCombatExtended => "PhobosMissilePatches_WarnFragmentsFull".Translate().ToString(),
                _ => null,
            });
            if (!CombatExtendedFragments.OnInCombatExtended) list.Note("PhobosMissilePatches_FragmentsOffInCombatExtended".Translate());
        }
    }

    static void DrawAfterEffects(SettingsList list)
    {
        MissilePatchesSettings s = Settings, d = Defaults;
        list.Check("PhobosMissilePatches_SettingBlastModel".Translate(), ref s.blastModel, d.blastModel, "PhobosMissilePatches_SettingBlastModelDesc".Translate());
        list.Warning(s.blastModel ? null : "PhobosMissilePatches_WarnBlastModelOff".Translate().ToString());
        if (s.blastModel)
        {
            list.Check("PhobosMissilePatches_SettingFullStrike".Translate(), ref s.fullStrikeEverywhere, d.fullStrikeEverywhere, "PhobosMissilePatches_SettingFullStrikeDesc".Translate());
            list.Warning(s.fullStrikeEverywhere ? "PhobosMissilePatches_WarnFullStrike".Translate().ToString() : null);
            list.Choice("PhobosMissilePatches_SettingFires".Translate(), s.fires, d.fires, (FireMode[])Enum.GetValues(typeof(FireMode)),
                mode => ("PhobosMissilePatches_Fires" + mode).Translate(), mode => Settings.fires = mode, "PhobosMissilePatches_SettingFiresDesc".Translate());
            list.Warning(s.fires >= FireMode.Moderate ? "PhobosMissilePatches_WarnFires".Translate().ToString() : null);
            list.Choice("PhobosMissilePatches_SettingFallout".Translate(), s.fallout, d.fallout, (FalloutMode[])Enum.GetValues(typeof(FalloutMode)),
                mode => ("PhobosMissilePatches_Fallout" + mode).Translate(), mode => Settings.fallout = mode, "PhobosMissilePatches_SettingFalloutDesc".Translate());
            if (s.fallout == FalloutMode.Automatic && RimatomicsTieIn.Active)
            {
                list.Indent();
                list.SliderInt("PhobosMissilePatches_SettingClouds".Translate(s.falloutClouds.Named("COUNT")), ref s.falloutClouds, d.falloutClouds, 0,
                    MissilePatchesSettings.MaxFalloutClouds, "PhobosMissilePatches_SettingCloudsDesc".Translate());
                list.Check("PhobosMissilePatches_SettingRadiation".Translate(), ref s.radiation, d.radiation, "PhobosMissilePatches_SettingRadiationDesc".Translate());
                list.Outdent();
            }
            else if (s.fallout == FalloutMode.Automatic)
            {
                list.Note("PhobosMissilePatches_RimatomicsAbsent".Translate());
            }
            if (ModsConfig.BiotechActive)
                list.Check("PhobosMissilePatches_SettingPollution".Translate(), ref s.pollution, d.pollution, "PhobosMissilePatches_SettingPollutionDesc".Translate());
            list.Check("PhobosMissilePatches_SettingEmp".Translate(), ref s.emp, d.emp, "PhobosMissilePatches_SettingEmpDesc".Translate());
            list.Check("PhobosMissilePatches_SettingVehicles".Translate(), ref s.vehiclesInStrike, d.vehiclesInStrike,
                "PhobosMissilePatches_SettingVehiclesDesc".Translate());
            list.Warning(s.vehiclesInStrike ? null : "PhobosMissilePatches_WarnVehiclesOff".Translate().ToString());
            list.Check("PhobosMissilePatches_SettingShadow".Translate(), ref s.shadowAndShelter, d.shadowAndShelter, "PhobosMissilePatches_SettingShadowDesc".Translate());
        }
        if (ModsConfig.IsActive(Patches.CiwsShells.CombatExtended))
        {
            list.Section("PhobosMissilePatches_SectionDefence".Translate());
            list.Check("PhobosMissilePatches_SettingCiwsShells".Translate(), ref s.ciwsShells, d.ciwsShells, "PhobosMissilePatches_SettingCiwsShellsDesc".Translate());
            list.Check("PhobosMissilePatches_SettingCiwsAircraft".Translate(), ref s.ciwsAircraft, d.ciwsAircraft, "PhobosMissilePatches_SettingCiwsAircraftDesc".Translate());
        }
    }


    static float SliceBudget(float value) => value < IrbmBudgetRule.MinFrameBudgetMs ? IrbmBudgetRule.MinFrameBudgetMs
        : value > IrbmBudgetRule.MaxFrameBudgetMs ? IrbmBudgetRule.MaxFrameBudgetMs : value;
}
