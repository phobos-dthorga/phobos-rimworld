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

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref burnLoad, "PhobosBurns_BurnLoad", true);
        Scribe_Values.Look(ref burnShock, "PhobosBurns_BurnShock", true);
        Scribe_Values.Look(ref airway, "PhobosBurns_Airway", true);
        Scribe_Values.Look(ref eschar, "PhobosBurns_Eschar", true);
        Scribe_Values.Look(ref chemicalBurns, "PhobosBurns_ChemicalBurns", true);
        Scribe_Values.Look(ref smoke, "PhobosBurns_Smoke", true);
        Scribe_Values.Look(ref carbonMonoxide, "PhobosBurns_CarbonMonoxide", true);
        Scribe_Values.Look(ref fireHeatOutdoors, "PhobosBurns_FireHeatOutdoors", true);
        Scribe_Values.Look(ref fireHeatIndoors, "PhobosBurns_FireHeatIndoors", true);
        Scribe_Values.Look(ref fleeHotCells, "PhobosBurns_FleeHotCells", true);
        Scribe_Values.Look(ref acuteSmokeWithCombatExtended, "PhobosBurns_AcuteSmokeWithCombatExtended", false);
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
        if (OptionalMods.IsActive(OptionalMods.MoreInjuries))
            report.On("More Injuries tie-in, its fire inhalation " + (MoreInjuriesInhalation.On ? "on" : "off") + " in its settings");
        else report.On("More Injuries not active");
        return report;
    }

    internal static void DrawSettings(Listing_Standard listing)
    {
        listing.CheckboxLabeled("PhobosBurns_SettingBurnLoad".Translate(), ref Settings.burnLoad, "PhobosBurns_SettingBurnLoadDesc".Translate());
        listing.CheckboxLabeled("PhobosBurns_SettingBurnShock".Translate(), ref Settings.burnShock, "PhobosBurns_SettingBurnShockDesc".Translate());
        listing.CheckboxLabeled("PhobosBurns_SettingAirway".Translate(), ref Settings.airway, "PhobosBurns_SettingAirwayDesc".Translate());
        listing.CheckboxLabeled("PhobosBurns_SettingEschar".Translate(), ref Settings.eschar, "PhobosBurns_SettingEscharDesc".Translate());
        listing.CheckboxLabeled("PhobosBurns_SettingChemicalBurns".Translate(), ref Settings.chemicalBurns, "PhobosBurns_SettingChemicalBurnsDesc".Translate());
        listing.CheckboxLabeled("PhobosBurns_SettingSmoke".Translate(), ref Settings.smoke, "PhobosBurns_SettingSmokeDesc".Translate());
        listing.CheckboxLabeled("PhobosBurns_SettingCarbonMonoxide".Translate(), ref Settings.carbonMonoxide, "PhobosBurns_SettingCarbonMonoxideDesc".Translate());
        listing.CheckboxLabeled("PhobosBurns_SettingFireHeat".Translate(), ref Settings.fireHeatOutdoors, "PhobosBurns_SettingFireHeatDesc".Translate());
        listing.CheckboxLabeled("PhobosBurns_SettingFireHeatIndoors".Translate(), ref Settings.fireHeatIndoors, "PhobosBurns_SettingFireHeatIndoorsDesc".Translate());
        listing.CheckboxLabeled("PhobosBurns_SettingFleeHotCells".Translate(), ref Settings.fleeHotCells, "PhobosBurns_SettingFleeHotCellsDesc".Translate());
        listing.CheckboxLabeled("PhobosBurns_SettingAcuteSmokeWithCombatExtended".Translate(), ref Settings.acuteSmokeWithCombatExtended, "PhobosBurns_SettingAcuteSmokeWithCombatExtendedDesc".Translate());
        if (OptionalMods.IsActive(OptionalMods.MoreInjuries))
            listing.Label((MoreInjuriesInhalation.On ? "PhobosBurns_MoreInjuriesInhalationOn" : "PhobosBurns_MoreInjuriesInhalationOff").Translate());
    }
}
