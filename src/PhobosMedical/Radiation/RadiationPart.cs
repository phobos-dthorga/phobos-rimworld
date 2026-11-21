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

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref model, "PhobosRadiation_Model", true);
        Scribe_Values.Look(ref harshness, "PhobosRadiation_Harshness", RadiationHarshness.Moderate);
        Scribe_Values.Look(ref estimate, "PhobosRadiation_Estimate", true);
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
        Phobos.Framework.Health.RadiationDoses.Register((pawn, grays, source) => RadiationService.Receive(pawn, grays, source), () => On);
        RimatomicsDose.Apply(harmony);
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
    }
}
