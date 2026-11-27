using System.Collections.Generic;
using System.Text;
using Phobos.Dosing.Rules;
using Phobos.Dosing.Services;
using Phobos.Framework;
using Phobos.Medical;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.Dosing;

/// <summary>Player preferences. Balance lives in <see cref="Defs.PhobosDosingRulesDef"/>, not here. The saved keys never change.</summary>
public class DosingSettings : ModSettings
{
    public bool enabled = true;
    /// <summary>A patient is dosed only if their medical care setting is this or better.</summary>
    public MedicalCareCategory minimumCare = MedicalCareCategory.HerbalOrWorse;
    /// <summary>Allow social and hard drugs as painkillers when they relieve pain.</summary>
    public bool recreationalForPain;
    /// <summary>Give medical drugs even to a patient who refuses drugs on principle.</summary>
    public bool ignoreAversionForMedical;
    /// <summary>Doctors also give a medical drug that speeds up immunity to a patient losing to a disease.</summary>
    public bool immunityDosing = true;
    /// <summary>A colonist in serious pain fetches a painkiller their drug policy allows.</summary>
    public bool selfMedication = true;
    /// <summary>Guests may do the same, from the colony's stock.</summary>
    public bool guestsSelfMedicate;
    /// <summary>The colony's secure slaves may do the same (the owner's card of 23 Nov 2026; off by default).</summary>
    public bool slavesSelfMedicate;
    /// <summary>An awake patient in extreme pain is harder to tend well.</summary>
    public bool fussing = true;
    /// <summary>Who gives an item that removes a condition outright (Medical 0.28.0, care needs): nobody, or doctors.</summary>
    public CareMode cureMode = CareMode.DoctorsOnly;
    /// <summary>Who gives a preventive when an outbreak disease is on the map: nobody, doctors, or doctors and each person themself.</summary>
    public CareMode preventionMode = CareMode.DoctorsAndSelf;
    /// <summary>Prevention never takes a preventive below this many in stock (the owner's card of 26 Nov 2026).</summary>
    public int preventionReserve = 5;
    /// <summary>An alert names anyone with a care need nothing in stock answers (the owner's card of 26 Nov 2026).</summary>
    public bool unmetAlert = true;
    /// <summary>Who gives a stimulant to a patient who can barely breathe (Medical 0.29.0): nobody, or doctors.</summary>
    public CareMode breathingMode = CareMode.DoctorsOnly;
    /// <summary>A medicine worth this much silver or more is rare, and kept for saving a life; zero keeps none back (the owner's
    /// answer of 26 Nov 2026: "a configurable setting via a slider... 300 silver is the default").</summary>
    public float rareItemMarketValue = 300f;
    /// <summary>Who gives something that keeps toxins out while toxic exposure is under way (Medical 0.30.0).</summary>
    public CareMode toxicMode = CareMode.DoctorsAndSelf;
    /// <summary>Someone caught in vacuum takes a vacuum-resisting dose they carry (Medical 0.30.0).</summary>
    public bool vacuumRescue = true;
    /// <summary>Doctors dose the colony's bonded animals and those with a master (Medical 0.34.0): moderate risk, so on.</summary>
    public bool animalCare = true;
    /// <summary>The long-term needs (Medical 0.31.0), each off until the player turns it on (the owner's card of 26 Nov 2026).</summary>
    public CareMode strainMode = CareMode.Off;
    public CareMode addictionMode = CareMode.Off;
    public CareMode oldWoundsMode = CareMode.Off;
    public CareMode missingPartMode = CareMode.Off;
    public CareMode reservoirMode = CareMode.Off;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref enabled, "PhobosDosing_Enabled", true);
        Scribe_Values.Look(ref minimumCare, "PhobosDosing_MinimumCare", MedicalCareCategory.HerbalOrWorse);
        Scribe_Values.Look(ref recreationalForPain, "PhobosDosing_RecreationalForPain", false);
        Scribe_Values.Look(ref ignoreAversionForMedical, "PhobosDosing_IgnoreAversionForMedical", false);
        Scribe_Values.Look(ref immunityDosing, "PhobosDosing_ImmunityDosing", true);
        Scribe_Values.Look(ref selfMedication, "PhobosDosing_SelfMedication", true);
        Scribe_Values.Look(ref guestsSelfMedicate, "PhobosDosing_GuestsSelfMedicate", false);
        Scribe_Values.Look(ref slavesSelfMedicate, "PhobosDosing_SlavesSelfMedicate", false);
        Scribe_Values.Look(ref fussing, "PhobosDosing_Fussing", true);
        Scribe_Values.Look(ref cureMode, "PhobosDosing_CureMode", CareMode.DoctorsOnly);
        Scribe_Values.Look(ref preventionMode, "PhobosDosing_PreventionMode", CareMode.DoctorsAndSelf);
        Scribe_Values.Look(ref preventionReserve, "PhobosDosing_PreventionReserve", 5);
        Scribe_Values.Look(ref unmetAlert, "PhobosDosing_UnmetAlert", true);
        Scribe_Values.Look(ref breathingMode, "PhobosDosing_BreathingMode", CareMode.DoctorsOnly);
        Scribe_Values.Look(ref rareItemMarketValue, "PhobosDosing_RareItemMarketValue", 300f);
        Scribe_Values.Look(ref toxicMode, "PhobosDosing_ToxicMode", CareMode.DoctorsAndSelf);
        Scribe_Values.Look(ref vacuumRescue, "PhobosDosing_VacuumRescue", true);
        Scribe_Values.Look(ref animalCare, "PhobosDosing_AnimalCare", true);
        Scribe_Values.Look(ref strainMode, "PhobosDosing_StrainMode", CareMode.Off);
        Scribe_Values.Look(ref addictionMode, "PhobosDosing_AddictionMode", CareMode.Off);
        Scribe_Values.Look(ref oldWoundsMode, "PhobosDosing_OldWoundsMode", CareMode.Off);
        Scribe_Values.Look(ref missingPartMode, "PhobosDosing_MissingPartMode", CareMode.Off);
        Scribe_Values.Look(ref reservoirMode, "PhobosDosing_ReservoirMode", CareMode.Off);
    }
}

/// <summary>
/// D'thorga Medical's emergency dosing part (D'thorga Emergency Dosing until 7 November 2026): doctors give painkillers and
/// sedatives to patients who need them, from whatever mod made them. One work giver under Doctor, one job, an index of ingestibles
/// read from the Defs at startup, and one think-tree giver for pawns who fetch their own. One Harmony postfix, for fussing; nothing saved.
/// </summary>
public static class DosingMod
{
    public const string LogPrefix = "[D'thorga Medical: dosing]";

    static readonly MedicalCareCategory[] CareChoices = { MedicalCareCategory.HerbalOrWorse, MedicalCareCategory.NormalOrWorse, MedicalCareCategory.Best };
    static readonly CareMode[] CureModes = { CareMode.Off, CareMode.DoctorsOnly };
    static readonly CareMode[] PreventionModes = { CareMode.Off, CareMode.DoctorsOnly, CareMode.DoctorsAndSelf };
    /// <summary>Round figures on buttons (the owner's card of 26 Nov 2026).</summary>
    static readonly int[] ReserveChoices = { 0, 5, 10, 20 };
    /// <summary>The rare mark's slider: zero (no medicine kept back) to well past the dearest medicine a mod is likely to add, in steps of 25 silver.</summary>
    const float RareMarkMax = 2000f;
    const float RareMarkStep = 25f;

    public static DosingSettings Settings => MedicalMod.Settings.dosing;

    internal static LoadReport Report(LoadReport report)
    {
        if (!Settings.enabled)
        {
            report.Off("dosing", "setting");
        }
        else
        {
            report.On("medical pain relief: " + Names(false));
            report.On("minimum care " + Settings.minimumCare.GetLabel());
            string recreational = "recreational pain relief: " + Names(true);
            if (Settings.recreationalForPain) report.On(recreational + ", as a last resort"); else report.Off(recreational, "setting; never given");
        }
        if (Settings.enabled)
        {
            string fighting = "drugs for fighting disease: " + NamesOf(DoseIndex.ImmunityOptions);
            if (Settings.immunityDosing) report.On(fighting); else report.Off(fighting, "setting");
        }
        if (Settings.enabled)
        {
            string toxic = "protection from toxins while exposed: " + NamesOf(DoseIndex.ToxicOptions);
            if (CareModeRule.DoctorsMay(Settings.toxicMode)) report.On(toxic + (CareModeRule.SelfMay(Settings.toxicMode) ? ", doctors and self" : ", doctors only"));
            else report.Off(toxic, "setting");
            if (ModsConfig.OdysseyActive)
            {
                string vacuum = "caught in vacuum: " + NamesOf(DoseIndex.VacuumOptions);
                if (Settings.vacuumRescue) report.On(vacuum); else report.Off(vacuum, "setting");
            }
            LongTermLine(report, "steadying someone close to a major break", DoseIndex.StrainOptions, Settings.strainMode);
            LongTermLine(report, "easing an addiction being quit", DoseIndex.AddictionOptions, Settings.addictionMode);
            LongTermLine(report, "healing old wounds", DoseIndex.OldWoundOptions, Settings.oldWoundsMode);
            LongTermLine(report, "regrowing missing parts", DoseIndex.RegrowthOptions, Settings.missingPartMode);
            LongTermLine(report, "refilling an artificial part's reservoir", DoseIndex.ReservoirOptions, Settings.reservoirMode);
            string breathing = "helps to breathing: " + NamesOf(DoseIndex.BreathingOptions);
            if (CareModeRule.DoctorsMay(Settings.breathingMode)) report.On(breathing); else report.Off(breathing, "setting");
            report.On(Settings.rareItemMarketValue > 0f ? "rare from " + Settings.rareItemMarketValue.ToString("0") + " silver" : "no rare mark");
            string cures = "cures: " + NamesOf(DoseIndex.CureOptions);
            if (CareModeRule.DoctorsMay(Settings.cureMode)) report.On(cures); else report.Off(cures, "setting");
            string prevention = "prevention in an outbreak: " + NamesOf(DoseIndex.PreventionOptions);
            if (CareModeRule.DoctorsMay(Settings.preventionMode))
                report.On(prevention + (CareModeRule.SelfMay(Settings.preventionMode) ? ", doctors and self" : ", doctors only") + ", reserve " + Settings.preventionReserve);
            else report.Off(prevention, "setting");
        }
        if (Settings.fussing) report.On("fussing in extreme pain"); else report.Off("fussing in extreme pain", "setting");
        if (Settings.selfMedication)
            report.On("self-medication: colonists" + (Settings.slavesSelfMedicate ? ", slaves" : "") + (Settings.guestsSelfMedicate ? ", guests" : ""));
        else report.Off("self-medication", "setting");
        return report;
    }

    static void LongTermLine(LoadReport report, string what, IReadOnlyList<DoseOption> options, CareMode mode)
    {
        string line = what + ": " + NamesOf(options);
        if (CareModeRule.DoctorsMay(mode)) report.On(line + (CareModeRule.SelfMay(mode) ? ", doctors and self" : ", doctors only"));
        else report.Off(line, "setting");
    }

    static string NamesOf(IReadOnlyList<DoseOption> options)
    {
        var names = new StringBuilder();
        foreach (DoseOption option in options)
        {
            if (names.Length > 0) names.Append(", ");
            names.Append(option.Def.label);
        }
        return names.Length == 0 ? "none" : names.ToString();
    }

    /// <summary>The labels of the indexed items of one kind, for the load line.</summary>
    static string Names(bool recreational)
    {
        var names = new StringBuilder();
        foreach (DoseOption option in DoseIndex.Options)
        {
            if (option.Effect.Recreational != recreational) continue;
            if (names.Length > 0) names.Append(", ");
            names.Append(option.Def.label).Append(option.Effect.Sedative ? " (sedative)" : "");
        }
        return names.Length == 0 ? "none" : names.ToString();
    }

    static readonly DosingSettings Defaults = new();

    static string ModeLabel(CareMode mode) => mode switch
    {
        CareMode.Off => "PhobosDosing_ModeOff".Translate(),
        CareMode.DoctorsOnly => "PhobosDosing_ModeDoctorsOnly".Translate(),
        _ => "PhobosDosing_ModeDoctorsAndSelf".Translate(),
    };

    /// <summary>The "Emergency dosing" page (settings pages, 20 Nov 2026).</summary>
    internal static void DrawSettings(SettingsList list)
    {
        DosingSettings s = Settings, d = Defaults;
        list.Check("PhobosDosing_SettingEnabled".Translate(), ref s.enabled, d.enabled, "PhobosDosing_SettingEnabledDesc".Translate());
        list.Choice("PhobosDosing_SettingMinimumCare".Translate(), s.minimumCare, d.minimumCare, CareChoices, care => care.GetLabel(),
            care => Settings.minimumCare = care, "PhobosDosing_SettingMinimumCareDesc".Translate());
        list.Check("PhobosDosing_SettingRecreational".Translate(), ref s.recreationalForPain, d.recreationalForPain, "PhobosDosing_SettingRecreationalDesc".Translate());
        list.Check("PhobosDosing_SettingIgnoreAversion".Translate(), ref s.ignoreAversionForMedical, d.ignoreAversionForMedical, "PhobosDosing_SettingIgnoreAversionDesc".Translate());
        list.Check("PhobosDosing_SettingImmunity".Translate(), ref s.immunityDosing, d.immunityDosing, "PhobosDosing_SettingImmunityDesc".Translate());
        list.Check("PhobosDosing_SettingFussing".Translate(), ref s.fussing, d.fussing, "PhobosDosing_SettingFussingDesc".Translate());
        list.Check("PhobosDosing_SettingAnimalCare".Translate(), ref s.animalCare, d.animalCare, "PhobosDosing_SettingAnimalCareDesc".Translate());
        list.Section("PhobosDosing_SectionCareNeeds".Translate());
        list.Radio("PhobosDosing_SettingBreathingMode".Translate(), ref s.breathingMode, d.breathingMode, CureModes, ModeLabel, tip: "PhobosDosing_SettingBreathingModeDesc".Translate());
        list.Radio("PhobosDosing_SettingCureMode".Translate(), ref s.cureMode, d.cureMode, CureModes, ModeLabel, tip: "PhobosDosing_SettingCureModeDesc".Translate());
        list.Radio("PhobosDosing_SettingPreventionMode".Translate(), ref s.preventionMode, d.preventionMode, PreventionModes, ModeLabel,
            tip: "PhobosDosing_SettingPreventionModeDesc".Translate());
        list.Radio("PhobosDosing_SettingToxicMode".Translate(), ref s.toxicMode, d.toxicMode, PreventionModes, ModeLabel, tip: "PhobosDosing_SettingToxicModeDesc".Translate());
        if (ModsConfig.OdysseyActive)
            list.Check("PhobosDosing_SettingVacuumRescue".Translate(), ref s.vacuumRescue, d.vacuumRescue, "PhobosDosing_SettingVacuumRescueDesc".Translate());
        list.Section("PhobosDosing_SectionLongTerm".Translate());
        list.Radio("PhobosDosing_SettingStrainMode".Translate(), ref s.strainMode, d.strainMode, PreventionModes, ModeLabel, tip: "PhobosDosing_SettingStrainModeDesc".Translate());
        list.Radio("PhobosDosing_SettingAddictionMode".Translate(), ref s.addictionMode, d.addictionMode, PreventionModes, ModeLabel, tip: "PhobosDosing_SettingAddictionModeDesc".Translate());
        list.Radio("PhobosDosing_SettingOldWoundsMode".Translate(), ref s.oldWoundsMode, d.oldWoundsMode, PreventionModes, ModeLabel, tip: "PhobosDosing_SettingOldWoundsModeDesc".Translate());
        list.Radio("PhobosDosing_SettingMissingPartMode".Translate(), ref s.missingPartMode, d.missingPartMode, PreventionModes, ModeLabel, tip: "PhobosDosing_SettingMissingPartModeDesc".Translate());
        list.Radio("PhobosDosing_SettingReservoirMode".Translate(), ref s.reservoirMode, d.reservoirMode, PreventionModes, ModeLabel, tip: "PhobosDosing_SettingReservoirModeDesc".Translate());
        list.Section("PhobosDosing_SectionStock".Translate());
        list.Radio("PhobosDosing_SettingPreventionReserve".Translate(), ref s.preventionReserve, d.preventionReserve, ReserveChoices, count => count.ToString(),
            tip: "PhobosDosing_SettingPreventionReserveDesc".Translate());
        list.Check("PhobosDosing_SettingUnmetAlert".Translate(), ref s.unmetAlert, d.unmetAlert, "PhobosDosing_SettingUnmetAlertDesc".Translate());
        string rareLabel = s.rareItemMarketValue > 0f
            ? "PhobosDosing_SettingRareMark".Translate(s.rareItemMarketValue.ToString("0").Named("SILVER"))
            : "PhobosDosing_SettingRareMarkNone".Translate();
        list.Slider(rareLabel, ref s.rareItemMarketValue, d.rareItemMarketValue, 0f, RareMarkMax, "PhobosDosing_SettingRareMarkDesc".Translate(),
            value => Mathf.Round(value / RareMarkStep) * RareMarkStep);
        list.Section("PhobosDosing_SectionSelf".Translate());
        list.Check("PhobosDosing_SettingSelfMedication".Translate(), ref s.selfMedication, d.selfMedication, "PhobosDosing_SettingSelfMedicationDesc".Translate());
        list.Check("PhobosDosing_SettingSlavesSelfMedicate".Translate(), ref s.slavesSelfMedicate, d.slavesSelfMedicate, "PhobosDosing_SettingSlavesSelfMedicateDesc".Translate());
        list.Check("PhobosDosing_SettingGuestsSelfMedicate".Translate(), ref s.guestsSelfMedicate, d.guestsSelfMedicate, "PhobosDosing_SettingGuestsSelfMedicateDesc".Translate());
    }
}
