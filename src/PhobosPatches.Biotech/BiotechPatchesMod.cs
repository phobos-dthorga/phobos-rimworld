using HarmonyLib;
using Phobos.Framework;
using Phobos.Shared;
using Verse;

namespace Phobos.BiotechPatches;

public class BiotechPatchesSettings : ModSettings
{
    /// <summary>The battery size presets: a factor on every mech's battery, and so on what its recharger draws.</summary>
    public const float ScaleStandard = 1f;
    public const float ScaleLarge = 3f;
    public const float ScaleHuge = 10f;

    /// <summary>Mech rechargers draw power by the mech on them. Off is the game's own flat figure.</summary>
    public bool mechsDrawRealPower;
    /// <summary>The factor on every mech's battery size, one of the presets.</summary>
    public float batteryScale = ScaleStandard;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref mechsDrawRealPower, "PhobosBiotechPatches_MechsDrawRealPower", false);
        Scribe_Values.Look(ref batteryScale, "PhobosBiotechPatches_BatteryScale", ScaleStandard);
        if (batteryScale <= 0f) batteryScale = ScaleStandard;
    }
}

/// <summary>
/// D'thorga Patches' Biotech part (from 0.28.0), switched on only while Biotech is active (this assembly lives in its load folder). It
/// holds one change, off by default: mech rechargers that draw real power (docs/development/mech-recharging-real-power.md). The setting
/// is read every tick a recharger ticks, so changing it needs no restart; every recharger is put back to the game's figure when it changes.
/// </summary>
public class BiotechPatchesMod : SettingsBookMod
{
    /// <summary>The Harmony ID is the packageId, shared by every part; never change it once the mod is published.</summary>
    public const string PackageId = "PhobosDthorga.Patches";
    public const string ModVersion = "0.50.0";
    public const string LogPrefix = "[D'thorga Patches]";
    public static readonly Phobos.Logging.PhobosLog Logger = Phobos.Framework.Logging.Logs.For("D'thorga Patches", "Biotech");

    public static BiotechPatchesSettings Settings { get; private set; } = new();

    public BiotechPatchesMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<BiotechPatchesSettings>();
        AddSettingsPages();
        HarmonyPatchApplier.ApplyEach(new Harmony(PackageId), typeof(BiotechPatchesMod).Assembly, LogPrefix);
        LongEventHandler.ExecuteWhenFinished(LogLoaded);
    }

    static void LogLoaded()
    {
        var report = new LoadReport(LogPrefix, ModVersion, "Biotech");
        const string feature = "mech rechargers draw power by the mech on them";
        if (Settings.mechsDrawRealPower) report.On(feature);
        else report.Off(feature, "its setting is off, the default");
        report.Log();
    }

    static readonly SettingsBook SettingsPages = SettingsBook.For("PhobosDthorga.Patches", () => "PhobosPatches_SettingsBook".Translate());
    static readonly BiotechPatchesSettings Defaults = new();
    static readonly float[] Scales = { BiotechPatchesSettings.ScaleStandard, BiotechPatchesSettings.ScaleLarge, BiotechPatchesSettings.ScaleHuge };
    protected override SettingsBook Book => SettingsPages;

    void AddSettingsPages() =>
        SettingsPages.Join(this).Add(new SettingsPage("biotech", 5f, () => "PhobosBiotechPatches_Page".Translate(), DrawSettings));

    static void DrawSettings(SettingsList list)
    {
        BiotechPatchesSettings s = Settings, d = Defaults;
        PhobosMechChargingRulesDef rules = MechCharging.Rules;
        list.Section("PhobosBiotechPatches_SectionRechargers".Translate());
        list.Note("PhobosBiotechPatches_WhatItDoes".Translate());
        // Every figure in the text comes from the rules Def and the game's own mech Defs, so a patched figure is the one shown.
        bool changed = list.Check("PhobosBiotechPatches_SettingRealPower".Translate(), ref s.mechsDrawRealPower, d.mechsDrawRealPower,
            "PhobosBiotechPatches_SettingRealPowerDesc".Translate(
                Watts(1f, s.batteryScale).Named("PERSIZE"),
                MechLabel(LightMech).Named("LIGHTMECH"), MechWatts(LightMech, s.batteryScale).Named("LIGHT"),
                MechLabel(SmallMech).Named("SMALLMECH"), MechWatts(SmallMech, s.batteryScale).Named("SMALL"),
                MechLabel(HeavyMech).Named("HEAVYMECH"), MechWatts(HeavyMech, s.batteryScale).Named("HEAVY"),
                MechLabel(HugeMech).Named("HUGEMECH"), MechWatts(HugeMech, s.batteryScale).Named("HUGE"),
                rules.easesFrom.ToStringPercent().Named("EASES"), (1f - rules.efficiency).ToStringPercent().Named("HEAT"),
                rules.idleShare.ToStringPercent().Named("IDLE")));
        changed |= list.Radio("PhobosBiotechPatches_SettingBatteryScale".Translate(), ref s.batteryScale, d.batteryScale, Scales, ScaleLabel,
            tip: "PhobosBiotechPatches_SettingBatteryScaleDesc".Translate());
        // The service puts every recharger back to the game's figure; the next tick sets ours again where the setting is on.
        if (changed) MechCharging.ResetAllRechargers();
    }

    // The game's own mechs the settings text gives as examples; a Def another mod removed shows as its defName with no figure lost.
    const string LightMech = "Mech_Lifter", SmallMech = "Mech_Cleansweeper", HeavyMech = "Mech_Tunneler", HugeMech = "Mech_Warqueen";

    static string MechLabel(string defName) => DefDatabase<ThingDef>.GetNamedSilentFail(defName)?.label ?? defName;

    static string MechWatts(string defName, float scale) =>
        Watts(DefDatabase<ThingDef>.GetNamedSilentFail(defName)?.race?.baseBodySize ?? 1f, scale);

    static string Watts(float bodySize, float scale) => MechCharging.FullRateWatts(bodySize, scale).ToString("F0");

    static string ScaleLabel(float scale)
    {
        NamedArgument light = MechWatts(LightMech, scale).Named("LIGHT"), heavy = MechWatts(HeavyMech, scale).Named("HEAVY");
        NamedArgument lightMech = MechLabel(LightMech).Named("LIGHTMECH"), heavyMech = MechLabel(HeavyMech).Named("HEAVYMECH");
        NamedArgument times = scale.ToString("0.#").Named("TIMES");
        return scale switch
        {
            BiotechPatchesSettings.ScaleLarge => "PhobosBiotechPatches_ScaleLarge".Translate(times, lightMech, light, heavyMech, heavy),
            BiotechPatchesSettings.ScaleHuge => "PhobosBiotechPatches_ScaleHuge".Translate(times, lightMech, light, heavyMech, heavy),
            _ => "PhobosBiotechPatches_ScaleStandard".Translate(lightMech, light, heavyMech, heavy),
        };
    }
}
