using System.Collections.Generic;
using System.Text;
using Phobos.Dosing.Rules;
using Phobos.Dosing.Services;
using Phobos.Framework;
using Phobos.Medical;
using RimWorld;
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
        list.Section("PhobosDosing_SectionCareNeeds".Translate());
        list.Radio("PhobosDosing_SettingCureMode".Translate(), ref s.cureMode, d.cureMode, CureModes, ModeLabel, tip: "PhobosDosing_SettingCureModeDesc".Translate());
        list.Radio("PhobosDosing_SettingPreventionMode".Translate(), ref s.preventionMode, d.preventionMode, PreventionModes, ModeLabel,
            tip: "PhobosDosing_SettingPreventionModeDesc".Translate());
        list.Radio("PhobosDosing_SettingPreventionReserve".Translate(), ref s.preventionReserve, d.preventionReserve, ReserveChoices, count => count.ToString(),
            tip: "PhobosDosing_SettingPreventionReserveDesc".Translate());
        list.Check("PhobosDosing_SettingUnmetAlert".Translate(), ref s.unmetAlert, d.unmetAlert, "PhobosDosing_SettingUnmetAlertDesc".Translate());
        list.Section("PhobosDosing_SectionSelf".Translate());
        list.Check("PhobosDosing_SettingSelfMedication".Translate(), ref s.selfMedication, d.selfMedication, "PhobosDosing_SettingSelfMedicationDesc".Translate());
        list.Check("PhobosDosing_SettingSlavesSelfMedicate".Translate(), ref s.slavesSelfMedicate, d.slavesSelfMedicate, "PhobosDosing_SettingSlavesSelfMedicateDesc".Translate());
        list.Check("PhobosDosing_SettingGuestsSelfMedicate".Translate(), ref s.guestsSelfMedicate, d.guestsSelfMedicate, "PhobosDosing_SettingGuestsSelfMedicateDesc".Translate());
    }
}
