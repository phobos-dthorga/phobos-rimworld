using Phobos.BurnsAndSmoke.Defs;
using Phobos.BurnsAndSmoke.Services;
using Phobos.Framework;
using Phobos.Framework.Gases;
using Phobos.Medical;
using Verse;

namespace Phobos.BurnsAndSmoke;

/// <summary>Player preferences. Balance lives in <see cref="Defs.PhobosBurnRulesDef"/>, not here. The saved keys never change.</summary>
public class BurnsAndSmokeSettings : ModSettings
{
    public bool burnLoad = true;
    public bool burnShock = true;
    public bool airway = true;
    /// <summary>Tox gas does lingering harm, by the caustic gas model.</summary>
    public bool causticGas = true;
    /// <summary>The fast-acting model (closer to the game's own feel) instead of the slow-acting default.</summary>
    public bool causticFastActing;
    public bool causticEyes = true;
    public bool causticThroat = true;
    public bool causticLungs = true;
    public bool causticSkin = true;
    public bool causticImmunity = true;
    public bool causticAftermath = true;
    /// <summary>A badly burned eye leaves a scarred cornea once it heals (owner, 8 Nov 2026).</summary>
    public bool causticScarring = true;
    /// <summary>Plants in dense caustic gas wilt and can die (owner, 8 Nov 2026).</summary>
    public bool causticPlants = true;
    /// <summary>Clothes worn in the gas carry it and keep burning the skin until stripped and rinsed (owner, 8 Nov 2026: a setting, off;
    /// by default the shower is enough).</summary>
    public bool causticStripAndRinse;
    /// <summary>Acid left on the skin keeps burning until it is spent or rinsed off.</summary>
    public bool chemicalBurns = true;
    /// <summary>Deeply burned limbs form a constricting eschar.</summary>
    public bool eschar = true;
    public bool smoke = true;
    public bool carbonMonoxide = true;
    /// <summary>Fires in the open heat the cells around them.</summary>
    public bool fireHeatOutdoors = true;
    /// <summary>Fires in large rooms heat the cells around them on top of the room's temperature.</summary>
    public bool fireHeatIndoors = true;
    /// <summary>People and animals move out of cells hot enough to burn them.</summary>
    public bool fleeHotCells = true;
    /// <summary>Apply our acute smoke condition even while Combat Extended's runs.</summary>
    public bool acuteSmokeWithCombatExtended;
    /// <summary>People with smoke in their lungs or burned airways cough now and then (owner, 11 Nov 2026).</summary>
    public bool coughSounds = true;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref burnLoad, "PhobosBurns_BurnLoad", true);
        Scribe_Values.Look(ref burnShock, "PhobosBurns_BurnShock", true);
        Scribe_Values.Look(ref airway, "PhobosBurns_Airway", true);
        Scribe_Values.Look(ref eschar, "PhobosBurns_Eschar", true);
        Scribe_Values.Look(ref chemicalBurns, "PhobosBurns_ChemicalBurns", true);
        Scribe_Values.Look(ref causticGas, "PhobosBurns_CausticGas", true);
        Scribe_Values.Look(ref causticFastActing, "PhobosBurns_CausticFastActing", false);
        Scribe_Values.Look(ref causticEyes, "PhobosBurns_CausticEyes", true);
        Scribe_Values.Look(ref causticThroat, "PhobosBurns_CausticThroat", true);
        Scribe_Values.Look(ref causticLungs, "PhobosBurns_CausticLungs", true);
        Scribe_Values.Look(ref causticSkin, "PhobosBurns_CausticSkin", true);
        Scribe_Values.Look(ref causticImmunity, "PhobosBurns_CausticImmunity", true);
        Scribe_Values.Look(ref causticAftermath, "PhobosBurns_CausticAftermath", true);
        Scribe_Values.Look(ref causticScarring, "PhobosBurns_CausticScarring", true);
        Scribe_Values.Look(ref causticPlants, "PhobosBurns_CausticPlants", true);
        Scribe_Values.Look(ref causticStripAndRinse, "PhobosBurns_CausticStripAndRinse", false);
        Scribe_Values.Look(ref smoke, "PhobosBurns_Smoke", true);
        Scribe_Values.Look(ref carbonMonoxide, "PhobosBurns_CarbonMonoxide", true);
        Scribe_Values.Look(ref fireHeatOutdoors, "PhobosBurns_FireHeatOutdoors", true);
        Scribe_Values.Look(ref fireHeatIndoors, "PhobosBurns_FireHeatIndoors", true);
        Scribe_Values.Look(ref fleeHotCells, "PhobosBurns_FleeHotCells", true);
        Scribe_Values.Look(ref acuteSmokeWithCombatExtended, "PhobosBurns_AcuteSmokeWithCombatExtended", false);
        Scribe_Values.Look(ref coughSounds, "PhobosBurns_CoughSounds", true);
    }
}

/// <summary>
/// D'thorga Medical's burns and smoke part (D'thorga Burns and Smoke until 7 November 2026): burns that matter the way More
/// Injuries' wounds do. Everything hangs on one hediff giver the game runs every 60 ticks and on the framework's gas layer, plus
/// one Harmony postfix for the heat around fires in the open (applied with the rest of the mod's patches by <see cref="MedicalMod"/>).
/// More Injuries and Combat Extended are optional.
/// </summary>
public static class BurnsAndSmokeMod
{
    public const string LogPrefix = "[D'thorga Medical: burns and smoke]";

    public static BurnsAndSmokeSettings Settings => MedicalMod.Settings.burns;

    /// <summary>The setting flag the caustic eye injury's lasting scar reads (the Framework's PatchFlags; registered by MedicalMod).</summary>
    public const string ScarringFlag = "PhobosBurns.causticScarring";
    /// <summary>The mod's content pack, for the uninstall sweep (it removes every condition whose Def belongs to it).</summary>
    public static ModContentPack? Pack => MedicalMod.Pack;
    /// <summary>Another mod models the air cell by cell (the rules Def lists them), so our hot zones stand down. Set once Defs are loaded.</summary>
    public static bool PerCellTemperatureModActive { get; private set; }

    /// <summary>The first of the rules Def's per-cell temperature mods that is active, or null.</summary>
    public static string? ActivePerCellTemperatureMod()
    {
        foreach (string packageId in PhobosBurnRulesDef.Current.perCellTemperatureMods)
        {
            if (OptionalMods.IsActive(packageId)) return packageId;
        }
        return null;
    }

    internal static LoadReport Report(LoadReport report)
    {
        string? perCellMod = ActivePerCellTemperatureMod();
        PerCellTemperatureModActive = perCellMod != null;
        if (Settings.burnLoad) report.On("burn load"); else report.Off("burn load", "setting");
        if (Settings.burnShock) report.On("burn shock"); else report.Off("burn shock", "setting");
        if (Settings.airway) report.On("airway burns"); else report.Off("airway burns", "setting");
        if (Settings.chemicalBurns) report.On("acid on the skin"); else report.Off("acid on the skin", "setting");
        if (!Settings.causticGas) report.Off("caustic gas", "setting");
        else if (!ModsConfig.BiotechActive) report.Off("caustic gas", "no tox gas without Biotech");
        else report.On("caustic gas, " + (Settings.causticFastActing ? "fast-acting" : "slow-acting")
            + (Settings.causticScarring && Settings.causticEyes ? ", corneal scarring" : "") + (Settings.causticPlants ? ", plants wilt" : "")
            + (Settings.causticStripAndRinse ? ", strip and rinse" : "") + (CausticService.WashingProbes.Count > 0 ? ", washing elsewhere decontaminates" : ""));
        if (!Settings.eschar) report.Off("eschar", "setting");
        else report.On(OptionalMods.IsActive(OptionalMods.MoreInjuries) ? "eschar, More Injuries' gangrene for a starved limb" : "eschar, ischaemic damage for a starved limb");
        if (Settings.smoke) report.On("smoke inhalation, " + (PhobosGases.UsesCombatExtendedSmoke ? "Combat Extended's condition as the signal" : "the game's smoke as the signal")); else report.Off("smoke inhalation", "setting");
        if (Settings.carbonMonoxide) report.On("carbon monoxide, " + SmokeExposureService.DescribeCarbonMonoxideSource()); else report.Off("carbon monoxide", "setting");
        if (!Settings.fireHeatOutdoors) report.Off("fire heat in the open", "setting");
        else if (perCellMod != null) report.Off("fire heat in the open", perCellMod + " models the air cell by cell");
        else report.On("fire heat in the open");
        if (!Settings.fireHeatIndoors) report.Off("fire heat in rooms", "setting");
        else if (perCellMod != null) report.Off("fire heat in rooms", perCellMod + " models the air cell by cell");
        else report.On("fire heat in rooms");
        if (Settings.fleeHotCells) report.On("moving away from heat"); else report.Off("moving away from heat", "setting");
        Comps.CoughSounds.Decide();
        if (!Settings.coughSounds) report.Off("coughing", "setting");
        else if (Comps.CoughSounds.StoodDownFor is { } other) report.Off("coughing", other + " already sounds coughs");
        else report.On("coughing");
        if (OptionalMods.IsActive(OptionalMods.MoreInjuries))
            report.On("More Injuries tie-in, its fire inhalation " + (MoreInjuriesInhalation.On ? "on" : "off") + " in its settings");
        else report.On("More Injuries not active");
        return report;
    }

    static readonly BurnsAndSmokeSettings Defaults = new();

    /// <summary>The "Burns and smoke" page (settings pages, 20 Nov 2026).</summary>
    internal static void DrawSettings(SettingsList list)
    {
        BurnsAndSmokeSettings s = Settings, d = Defaults;
        list.Section("PhobosBurns_SectionBurns".Translate());
        list.Check("PhobosBurns_SettingBurnLoad".Translate(), ref s.burnLoad, d.burnLoad, "PhobosBurns_SettingBurnLoadDesc".Translate());
        list.Check("PhobosBurns_SettingBurnShock".Translate(), ref s.burnShock, d.burnShock, "PhobosBurns_SettingBurnShockDesc".Translate());
        list.Check("PhobosBurns_SettingAirway".Translate(), ref s.airway, d.airway, "PhobosBurns_SettingAirwayDesc".Translate());
        list.Check("PhobosBurns_SettingEschar".Translate(), ref s.eschar, d.eschar, "PhobosBurns_SettingEscharDesc".Translate());
        list.Check("PhobosBurns_SettingChemicalBurns".Translate(), ref s.chemicalBurns, d.chemicalBurns, "PhobosBurns_SettingChemicalBurnsDesc".Translate());
        list.Section("PhobosBurns_SectionSmoke".Translate());
        list.Check("PhobosBurns_SettingSmoke".Translate(), ref s.smoke, d.smoke, "PhobosBurns_SettingSmokeDesc".Translate());
        list.Check("PhobosBurns_SettingCarbonMonoxide".Translate(), ref s.carbonMonoxide, d.carbonMonoxide, "PhobosBurns_SettingCarbonMonoxideDesc".Translate());
        // Only meaningful beside Combat Extended, so shown only there (settings audit, 20 Nov 2026).
        if (OptionalMods.IsActive(OptionalMods.CombatExtended))
        {
            list.Check("PhobosBurns_SettingAcuteSmokeWithCombatExtended".Translate(), ref s.acuteSmokeWithCombatExtended, d.acuteSmokeWithCombatExtended,
                "PhobosBurns_SettingAcuteSmokeWithCombatExtendedDesc".Translate());
        }
        if (OptionalMods.IsActive(OptionalMods.MoreInjuries))
            list.Note((MoreInjuriesInhalation.On ? "PhobosBurns_MoreInjuriesInhalationOn" : "PhobosBurns_MoreInjuriesInhalationOff").Translate());
        list.Check("PhobosBurns_SettingCoughSounds".Translate(), ref s.coughSounds, d.coughSounds, "PhobosBurns_SettingCoughSoundsDesc".Translate());
        if (Comps.CoughSounds.StoodDownFor is { } coughs) list.Note("PhobosBurns_CoughSoundsStoodDown".Translate(coughs.Named("MOD")));
        list.Section("PhobosBurns_SectionFireHeat".Translate());
        list.Check("PhobosBurns_SettingFireHeat".Translate(), ref s.fireHeatOutdoors, d.fireHeatOutdoors, "PhobosBurns_SettingFireHeatDesc".Translate());
        list.Check("PhobosBurns_SettingFireHeatIndoors".Translate(), ref s.fireHeatIndoors, d.fireHeatIndoors, "PhobosBurns_SettingFireHeatIndoorsDesc".Translate());
        list.Check("PhobosBurns_SettingFleeHotCells".Translate(), ref s.fleeHotCells, d.fleeHotCells, "PhobosBurns_SettingFleeHotCellsDesc".Translate());
    }

    /// <summary>The "Caustic gas" page: its parts shown, indented, only while caustic gas is on.</summary>
    internal static void DrawCausticSettings(SettingsList list)
    {
        BurnsAndSmokeSettings s = Settings, d = Defaults;
        list.Check("PhobosBurns_SettingCausticGas".Translate(), ref s.causticGas, d.causticGas, "PhobosBurns_SettingCausticGasDesc".Translate());
        if (!s.causticGas) return;
        list.Indent();
        list.Check("PhobosBurns_SettingCausticFast".Translate(), ref s.causticFastActing, d.causticFastActing, "PhobosBurns_SettingCausticFastDesc".Translate());
        list.Section("PhobosBurns_SectionCausticBody".Translate());
        list.Check("PhobosBurns_SettingCausticEyes".Translate(), ref s.causticEyes, d.causticEyes);
        list.Check("PhobosBurns_SettingCausticThroat".Translate(), ref s.causticThroat, d.causticThroat);
        list.Check("PhobosBurns_SettingCausticLungs".Translate(), ref s.causticLungs, d.causticLungs);
        list.Check("PhobosBurns_SettingCausticSkin".Translate(), ref s.causticSkin, d.causticSkin);
        list.Check("PhobosBurns_SettingCausticImmunity".Translate(), ref s.causticImmunity, d.causticImmunity, "PhobosBurns_SettingCausticImmunityDesc".Translate());
        list.Check("PhobosBurns_SettingCausticAftermath".Translate(), ref s.causticAftermath, d.causticAftermath);
        list.Check("PhobosBurns_SettingCausticScarring".Translate(), ref s.causticScarring, d.causticScarring, "PhobosBurns_SettingCausticScarringDesc".Translate());
        list.Section("PhobosBurns_SectionCausticAround".Translate());
        list.Check("PhobosBurns_SettingCausticPlants".Translate(), ref s.causticPlants, d.causticPlants, "PhobosBurns_SettingCausticPlantsDesc".Translate());
        list.Check("PhobosBurns_SettingCausticStrip".Translate(), ref s.causticStripAndRinse, d.causticStripAndRinse, "PhobosBurns_SettingCausticStripDesc".Translate());
    }
}
