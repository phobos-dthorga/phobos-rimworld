using System.Collections.Generic;
using System.Text;
using Phobos.Dosing.Services;
using Phobos.Framework;
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
    public DisplayMode displayMode = DisplayMode.Plain;

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
        Scribe_Values.Look(ref displayMode, "PhobosDosing_DisplayMode", DisplayMode.Plain);
    }
}

/// <summary>
/// D'thorga Emergency Dosing: doctors give painkillers and sedatives to patients who need them, from whatever mod made them.
/// One work giver under Doctor, one job, an index of ingestibles read from the Defs at startup, and one think-tree giver for pawns who
/// fetch their own. No Harmony; nothing saved.
/// </summary>
public class DosingMod : Mod
{
    public const string PackageId = "PhobosDthorga.Dosing";
    public const string ModVersion = "0.3.0";
    public const string LogPrefix = "[D'thorga Emergency Dosing]";

    static readonly MedicalCareCategory[] CareChoices = { MedicalCareCategory.HerbalOrWorse, MedicalCareCategory.NormalOrWorse, MedicalCareCategory.Best };

    public static DosingSettings Settings { get; private set; } = new();

    public DosingMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<DosingSettings>();
        LongEventHandler.ExecuteWhenFinished(LogLoaded);
    }

    static void LogLoaded()
    {
        var report = new LoadReport(LogPrefix, ModVersion);
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
        if (Settings.selfMedication) report.On("self-medication" + (Settings.guestsSelfMedicate ? ", guests too" : ", colonists only")); else report.Off("self-medication", "setting");
        report.Log();
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

    public override string SettingsCategory() => "PhobosDosing_SettingsCategory".Translate();

    public override void DoSettingsWindowContents(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        listing.CheckboxLabeled("PhobosDosing_SettingEnabled".Translate(), ref Settings.enabled, "PhobosDosing_SettingEnabledDesc".Translate());
        if (listing.ButtonTextLabeled("PhobosDosing_SettingMinimumCare".Translate(), Settings.minimumCare.GetLabel(), tooltip: "PhobosDosing_SettingMinimumCareDesc".Translate()))
        {
            var options = new List<FloatMenuOption>();
            foreach (MedicalCareCategory care in CareChoices)
            {
                MedicalCareCategory chosen = care;
                options.Add(new FloatMenuOption(care.GetLabel(), () => Settings.minimumCare = chosen));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }
        listing.CheckboxLabeled("PhobosDosing_SettingRecreational".Translate(), ref Settings.recreationalForPain, "PhobosDosing_SettingRecreationalDesc".Translate());
        listing.CheckboxLabeled("PhobosDosing_SettingIgnoreAversion".Translate(), ref Settings.ignoreAversionForMedical, "PhobosDosing_SettingIgnoreAversionDesc".Translate());
        listing.CheckboxLabeled("PhobosDosing_SettingImmunity".Translate(), ref Settings.immunityDosing, "PhobosDosing_SettingImmunityDesc".Translate());
        listing.Gap();
        listing.CheckboxLabeled("PhobosDosing_SettingSelfMedication".Translate(), ref Settings.selfMedication, "PhobosDosing_SettingSelfMedicationDesc".Translate());
        listing.CheckboxLabeled("PhobosDosing_SettingGuestsSelfMedicate".Translate(), ref Settings.guestsSelfMedicate, "PhobosDosing_SettingGuestsSelfMedicateDesc".Translate());
        listing.Gap();
        SettingsUi.DrawDisplayMode(listing, ref Settings.displayMode);
        listing.Gap();
        listing.Label("PhobosDosing_SettingRemovalNote".Translate());
        listing.End();
    }
}
