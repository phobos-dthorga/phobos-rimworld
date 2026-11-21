using System;
using Phobos.Framework;
using Phobos.Medical;
using Phobos.Radiation.Rules;
using Phobos.Radiation.Services;
using Verse;

namespace Phobos.Radiation;

/// <summary>The harshness presets (docs/development/radiation-model.md, RM10; the owner's default: Moderate).</summary>
public enum RadiationHarshness
{
    Gentle,
    Moderate,
    Realistic,
    Harsh,
}

/// <summary>Player preferences for the radiation model. Balance lives in the rules Def; the saved keys never change.</summary>
public class RadiationSettings : ModSettings
{
    /// <summary>Our model replaces Dubs Rimatomics' radiation sickness for people (owner, 22 Nov 2026: "with a setting to disable it").</summary>
    public bool model = true;
    public RadiationHarshness harshness = RadiationHarshness.Moderate;
    /// <summary>A doctor's estimate before Rimatomics' Geiger research, or without Rimatomics; off shows the dose exactly always.</summary>
    public bool estimate = true;
    /// <summary>Late effects, each with its own switch (owner, 22 Nov 2026: "configurable settings for those who want to disable various
    /// things"); their strength follows the harshness preset.</summary>
    public bool cancer = true;
    public bool cataract = true;
    public bool sterility = true;
    public bool pregnancyLoss = true;
    public bool pregnancyChild = true;
    public bool pregnancyCancer = true;
    /// <summary>What a person wears shields them a little from radiation from outside, by its mass (RM17; owner, 22 Nov 2026).</summary>
    public bool wornShielding = true;
    /// <summary>Fallout dust on skin and clothes, breathed and swallowed (RM6; owner, 22 Nov 2026: "on clothes and skin").</summary>
    public bool contamination = true;
    /// <summary>A dusty colonist strips off dusty clothes at a safety shower and washes (owner, 22 Nov 2026: off by default, as caustic's).</summary>
    public bool stripAndWash;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref model, "PhobosRadiation_Model", true);
        Scribe_Values.Look(ref harshness, "PhobosRadiation_Harshness", RadiationHarshness.Moderate);
        Scribe_Values.Look(ref estimate, "PhobosRadiation_Estimate", true);
        Scribe_Values.Look(ref cancer, "PhobosRadiation_Cancer", true);
        Scribe_Values.Look(ref cataract, "PhobosRadiation_Cataract", true);
        Scribe_Values.Look(ref sterility, "PhobosRadiation_Sterility", true);
        Scribe_Values.Look(ref pregnancyLoss, "PhobosRadiation_PregnancyLoss", true);
        Scribe_Values.Look(ref pregnancyChild, "PhobosRadiation_PregnancyChild", true);
        Scribe_Values.Look(ref pregnancyCancer, "PhobosRadiation_PregnancyCancer", true);
        Scribe_Values.Look(ref wornShielding, "PhobosRadiation_WornShielding", true);
        Scribe_Values.Look(ref contamination, "PhobosRadiation_Contamination", true);
        Scribe_Values.Look(ref stripAndWash, "PhobosRadiation_StripAndWash", false);
    }
}

/// <summary>
/// D'thorga Medical's radiation part (0.21.0; docs/development/radiation-model.md; owner, 22 Nov 2026): a dose in grays per person,
/// repaired over days; the real course of radiation sickness; care that changes who lives; a doctor's estimate or, after Rimatomics'
/// Geiger research, the exact dose. Takes every Dubs Rimatomics dose for people, and our strike's, behind one setting.
/// </summary>
public static class RadiationPart
{
    public static RadiationSettings Settings => MedicalMod.Settings.radiation;
    static readonly RadiationSettings Defaults = new();

    /// <summary>Whether the model runs now.</summary>
    public static bool On => Settings.model;

    public static RadiationPreset Preset => Settings.harshness switch
    {
        RadiationHarshness.Gentle => RadiationPreset.Gentle,
        RadiationHarshness.Realistic => RadiationPreset.Realistic,
        RadiationHarshness.Harsh => RadiationPreset.Harsh,
        _ => RadiationPreset.Moderate,
    };

    /// <summary>Called once from the mod's constructor: registers the model with the Framework and applies the Rimatomics patch.</summary>
    internal static void Start(HarmonyLib.Harmony harmony)
    {
        // Every dose through the Framework (the strike) comes from outside, so what is worn shields it.
        Phobos.Framework.Health.RadiationDoses.Register((pawn, grays, source) => RadiationService.Receive(pawn, grays * WornShielding.Pass(pawn), source), () => On);
        RimatomicsDose.Apply(harmony);
        Contamination.Start();
    }

    internal static LoadReport Report(LoadReport report)
    {
        if (!Settings.model)
        {
            report.Off("radiation model", "setting: Dubs Rimatomics' own radiation as before");
            return report;
        }
        report.On("radiation model, " + Settings.harshness + (Settings.estimate ? ", a doctor's estimate before the Geiger research" : ", doses shown exactly"));
        if (OptionalMods.IsActive(RimatomicsDose.PackageId))
        {
            if (RimatomicsDose.Applied) report.On("Dubs Rimatomics' doses taken for people (robots stay Rimatomics')");
            else report.Off("Dubs Rimatomics' doses", "its dose method was not found by name");
        }
        return report;
    }

    internal static void DrawSettings(SettingsList list)
    {
        RadiationSettings s = Settings, d = Defaults;
        list.Check("PhobosRadiation_SettingModel".Translate(), ref s.model, d.model, "PhobosRadiation_SettingModelDesc".Translate());
        list.Radio("PhobosRadiation_SettingHarshness".Translate(), ref s.harshness, d.harshness, (RadiationHarshness[])Enum.GetValues(typeof(RadiationHarshness)),
            h => ("PhobosRadiation_Harshness" + h).Translate(), h => ("PhobosRadiation_Harshness" + h + "Desc").Translate(),
            "PhobosRadiation_SettingHarshnessDesc".Translate());
        list.Check("PhobosRadiation_SettingEstimate".Translate(), ref s.estimate, d.estimate, "PhobosRadiation_SettingEstimateDesc".Translate());
        list.Check("PhobosRadiation_SettingWornShielding".Translate(), ref s.wornShielding, d.wornShielding, "PhobosRadiation_SettingWornShieldingDesc".Translate());
        list.Check("PhobosRadiation_SettingContamination".Translate(), ref s.contamination, d.contamination, "PhobosRadiation_SettingContaminationDesc".Translate());
        list.Check("PhobosRadiation_SettingStripAndWash".Translate(), ref s.stripAndWash, d.stripAndWash, "PhobosRadiation_SettingStripAndWashDesc".Translate(),
            enabled: s.contamination);
        list.Section("PhobosRadiation_SettingLateHeading".Translate());
        list.Check("PhobosRadiation_SettingCancer".Translate(), ref s.cancer, d.cancer, "PhobosRadiation_SettingCancerDesc".Translate());
        list.Check("PhobosRadiation_SettingCataract".Translate(), ref s.cataract, d.cataract, "PhobosRadiation_SettingCataractDesc".Translate());
        list.Check("PhobosRadiation_SettingSterility".Translate(), ref s.sterility, d.sterility, "PhobosRadiation_SettingSterilityDesc".Translate());
        list.Check("PhobosRadiation_SettingPregnancyLoss".Translate(), ref s.pregnancyLoss, d.pregnancyLoss, "PhobosRadiation_SettingPregnancyLossDesc".Translate());
        list.Check("PhobosRadiation_SettingPregnancyChild".Translate(), ref s.pregnancyChild, d.pregnancyChild, "PhobosRadiation_SettingPregnancyChildDesc".Translate());
        list.Check("PhobosRadiation_SettingPregnancyCancer".Translate(), ref s.pregnancyCancer, d.pregnancyCancer, "PhobosRadiation_SettingPregnancyCancerDesc".Translate());
    }
}
