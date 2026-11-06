using System;
using System.Reflection;
using HarmonyLib;
using Phobos.BurnsAndSmoke.Defs;
using Phobos.BurnsAndSmoke.Services;
using Phobos.Framework;
using Phobos.Framework.Gases;
using Phobos.Framework.Uninstall;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.BurnsAndSmoke;

/// <summary>Player preferences. Balance lives in <see cref="Defs.PhobosBurnRulesDef"/>, not here. The saved keys never change.</summary>
public class BurnsAndSmokeSettings : ModSettings
{
    public bool burnLoad = true;
    public bool burnShock = true;
    public bool airway = true;
    public bool smoke = true;
    public bool carbonMonoxide = true;
    /// <summary>Fires in the open heat the cells around them.</summary>
    public bool fireHeatOutdoors = true;
    /// <summary>Apply our acute smoke condition even while Combat Extended's runs.</summary>
    public bool acuteSmokeWithCombatExtended;
    public DisplayMode displayMode = DisplayMode.Plain;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref burnLoad, "PhobosBurns_BurnLoad", true);
        Scribe_Values.Look(ref burnShock, "PhobosBurns_BurnShock", true);
        Scribe_Values.Look(ref airway, "PhobosBurns_Airway", true);
        Scribe_Values.Look(ref smoke, "PhobosBurns_Smoke", true);
        Scribe_Values.Look(ref carbonMonoxide, "PhobosBurns_CarbonMonoxide", true);
        Scribe_Values.Look(ref fireHeatOutdoors, "PhobosBurns_FireHeatOutdoors", true);
        Scribe_Values.Look(ref acuteSmokeWithCombatExtended, "PhobosBurns_AcuteSmokeWithCombatExtended", false);
        Scribe_Values.Look(ref displayMode, "PhobosBurns_DisplayMode", DisplayMode.Plain);
    }
}

/// <summary>
/// D'thorga Burns and Smoke: burns that matter the way More Injuries' wounds do. Everything hangs on one hediff giver the
/// game runs every 60 ticks and on the framework's gas layer, plus one Harmony postfix for the heat around fires in the open.
/// More Injuries and Combat Extended are optional.
/// </summary>
public class BurnsAndSmokeMod : Mod
{
    public const string PackageId = "PhobosDthorga.BurnsAndSmoke";
    /// <summary>The Harmony ID is the packageId; never change it once the mod is published.</summary>
    public const string HarmonyId = PackageId;
    public const string ModVersion = "0.5.0";
    public const string LogPrefix = "[D'thorga Burns and Smoke]";

    public static BurnsAndSmokeSettings Settings { get; private set; } = new();
    /// <summary>This mod's content pack, for the uninstall sweep (it removes every condition whose Def belongs to it).</summary>
    public static ModContentPack? Pack { get; private set; }
    /// <summary>Another mod models the air cell by cell (the rules Def lists them), so our hot zones stand down. Set once Defs are loaded.</summary>
    public static bool PerCellTemperatureModActive { get; private set; }

    public BurnsAndSmokeMod(ModContentPack content) : base(content)
    {
        Pack = content;
        Settings = GetSettings<BurnsAndSmokeSettings>();
        ApplyPatches(new Harmony(HarmonyId), typeof(BurnsAndSmokeMod).Assembly);
        LongEventHandler.ExecuteWhenFinished(LogLoaded);
    }

    /// <summary>Applies the assembly's patch classes one at a time, so a target an update renames switches off one part with a log line.</summary>
    static void ApplyPatches(Harmony harmony, Assembly assembly)
    {
        foreach (Type type in AccessTools.GetTypesFromAssembly(assembly))
        {
            if (!type.IsDefined(typeof(HarmonyPatch), inherit: false)) continue;
            try
            {
                harmony.CreateClassProcessor(type).Patch();
            }
            catch (Exception error)
            {
                Log.Error(LogPrefix + " Could not apply " + type.Name + "; that part is switched off. " + error);
            }
        }
    }

    /// <summary>The first of the rules Def's per-cell temperature mods that is active, or null.</summary>
    public static string? ActivePerCellTemperatureMod()
    {
        foreach (string packageId in PhobosBurnRulesDef.Current.perCellTemperatureMods)
        {
            if (OptionalMods.IsActive(packageId)) return packageId;
        }
        return null;
    }

    static void LogLoaded()
    {
        string? perCellMod = ActivePerCellTemperatureMod();
        PerCellTemperatureModActive = perCellMod != null;
        var report = new LoadReport(LogPrefix, ModVersion);
        if (Settings.burnLoad) report.On("burn load"); else report.Off("burn load", "setting");
        if (Settings.burnShock) report.On("burn shock"); else report.Off("burn shock", "setting");
        if (Settings.airway) report.On("airway burns"); else report.Off("airway burns", "setting");
        if (Settings.smoke) report.On("smoke inhalation, " + (PhobosGases.UsesCombatExtendedSmoke ? "Combat Extended's condition as the signal" : "the game's smoke as the signal")); else report.Off("smoke inhalation", "setting");
        if (Settings.carbonMonoxide) report.On("carbon monoxide, " + SmokeExposureService.DescribeCarbonMonoxideSource()); else report.Off("carbon monoxide", "setting");
        if (!Settings.fireHeatOutdoors) report.Off("fire heat in the open", "setting");
        else if (perCellMod != null) report.Off("fire heat in the open", perCellMod + " models the air cell by cell");
        else report.On("fire heat in the open");
        report.On(OptionalMods.IsActive(OptionalMods.MoreInjuries) ? "More Injuries tie-in" : "More Injuries not active");
        report.Log();
    }

    public override string SettingsCategory() => "PhobosBurns_SettingsCategory".Translate();

    public override void DoSettingsWindowContents(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        listing.CheckboxLabeled("PhobosBurns_SettingBurnLoad".Translate(), ref Settings.burnLoad, "PhobosBurns_SettingBurnLoadDesc".Translate());
        listing.CheckboxLabeled("PhobosBurns_SettingBurnShock".Translate(), ref Settings.burnShock, "PhobosBurns_SettingBurnShockDesc".Translate());
        listing.CheckboxLabeled("PhobosBurns_SettingAirway".Translate(), ref Settings.airway, "PhobosBurns_SettingAirwayDesc".Translate());
        listing.CheckboxLabeled("PhobosBurns_SettingSmoke".Translate(), ref Settings.smoke, "PhobosBurns_SettingSmokeDesc".Translate());
        listing.CheckboxLabeled("PhobosBurns_SettingCarbonMonoxide".Translate(), ref Settings.carbonMonoxide, "PhobosBurns_SettingCarbonMonoxideDesc".Translate());
        listing.CheckboxLabeled("PhobosBurns_SettingFireHeat".Translate(), ref Settings.fireHeatOutdoors, "PhobosBurns_SettingFireHeatDesc".Translate());
        listing.CheckboxLabeled("PhobosBurns_SettingAcuteSmokeWithCombatExtended".Translate(), ref Settings.acuteSmokeWithCombatExtended, "PhobosBurns_SettingAcuteSmokeWithCombatExtendedDesc".Translate());
        listing.Gap();
        SettingsUi.DrawDisplayMode(listing, ref Settings.displayMode);
        listing.Gap();
        if (Current.Game != null && listing.ButtonText("PhobosBurns_SettingRemoveAll".Translate()))
        {
            int removed = Pack == null ? 0 : CleanupService.RemoveHediffsOf(Pack);
            Messages.Message("PhobosBurns_MessageRemovedAll".Translate(removed.Named("COUNT")), MessageTypeDefOf.NeutralEvent, false);
        }
        listing.Label("PhobosBurns_SettingRemoveAllDesc".Translate());
        listing.End();
    }
}
