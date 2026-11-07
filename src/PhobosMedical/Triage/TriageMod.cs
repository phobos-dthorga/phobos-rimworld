using System.Collections.Generic;
using Phobos.Framework;
using Phobos.Framework.Health;
using Phobos.Medical;
using Phobos.Triage.Rules;
using Phobos.Triage.Services;
using Verse;

namespace Phobos.Triage;

/// <summary>Player preferences. Thresholds and bands live in the rules Def, not here. The saved keys never change.</summary>
public class TriageSettings : ModSettings
{
    public bool enabled = true;
    public TriageAuthority defaultColonist = TriageAuthority.LifeSaving;
    public TriageAuthority defaultSlave = TriageAuthority.LifeSaving;
    public TriageAuthority defaultPrisoner = TriageAuthority.LifeSaving;
    public TriageAuthority defaultGuest = TriageAuthority.None;
    /// <summary>The highest chance of death on the table a doctor may accept without orders (authored default).</summary>
    public float maxDeathOnFailureChance = 0.05f;
    public bool allowRareTools = true;
    public bool allowViolations;
    public bool messages = true;
    public bool triageOrdering = true;

    public TriageAuthority DefaultFor(PatientGroup group) => group switch
    {
        PatientGroup.Slave => defaultSlave,
        PatientGroup.Prisoner => defaultPrisoner,
        PatientGroup.Guest => defaultGuest,
        _ => defaultColonist,
    };

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref enabled, "PhobosTriage_Enabled", true);
        Scribe_Values.Look(ref defaultColonist, "PhobosTriage_DefaultColonist", TriageAuthority.LifeSaving);
        Scribe_Values.Look(ref defaultSlave, "PhobosTriage_DefaultSlave", TriageAuthority.LifeSaving);
        Scribe_Values.Look(ref defaultPrisoner, "PhobosTriage_DefaultPrisoner", TriageAuthority.LifeSaving);
        Scribe_Values.Look(ref defaultGuest, "PhobosTriage_DefaultGuest", TriageAuthority.None);
        Scribe_Values.Look(ref maxDeathOnFailureChance, "PhobosTriage_MaxDeathOnFailureChance", 0.05f);
        Scribe_Values.Look(ref allowRareTools, "PhobosTriage_AllowRareTools", true);
        Scribe_Values.Look(ref allowViolations, "PhobosTriage_AllowViolations", false);
        Scribe_Values.Look(ref messages, "PhobosTriage_Messages", true);
        Scribe_Values.Look(ref triageOrdering, "PhobosTriage_TriageOrdering", true);
    }
}

/// <summary>
/// D'thorga Medical's triage part (D'thorga Triage until 7 November 2026): doctors queue the life-saving operations a patient
/// needs, and take the worst case first. One planning pass per map on an interval, the game's own bills, a per-person authority
/// through the framework's patient comp, and a prioritised copy of the game's operate giver. No Harmony. More Injuries, Search and
/// Rescue and Hospitality are optional and never named: operations are found by what they remove.
/// </summary>
public static class TriageMod
{
    public const string LogPrefix = "[D'thorga Medical: triage]";

    public static TriageSettings Settings => MedicalMod.Settings.triage;

    internal static LoadReport Report(LoadReport report)
    {
        if (!Settings.enabled) report.Off("operations on the doctors' own", "setting");
        else report.On("operations on the doctors' own: colonists " + Word(Settings.defaultColonist) + ", slaves " + Word(Settings.defaultSlave)
            + ", prisoners " + Word(Settings.defaultPrisoner) + ", guests " + Word(Settings.defaultGuest)
            + ", death chance accepted up to " + (Settings.maxDeathOnFailureChance * 100f).ToString("0") + "%");
        int operations = OperationIndex.All.Count;
        report.On(operations + " operation" + (operations == 1 ? "" : "s") + " indexed by the condition they remove");
        if (Settings.triageOrdering) report.On("doctors take the worst case first"); else report.Off("doctors take the worst case first", "setting");
        if (Settings.messages) report.On("messages"); else report.Off("messages", "setting");
        return report;
    }

    static string Word(TriageAuthority authority) => authority switch
    {
        TriageAuthority.None => "never",
        TriageAuthority.LifeSaving => "life-saving",
        TriageAuthority.LifeSavingAndCrippling => "life-saving and crippling",
        _ => "any repair",
    };

    internal static void DrawSettings(Listing_Standard listing)
    {
        listing.CheckboxLabeled("PhobosTriage_SettingEnabled".Translate(), ref Settings.enabled, "PhobosTriage_SettingEnabledDesc".Translate());
        listing.Gap();
        listing.Label("PhobosTriage_SettingDefaults".Translate(), tooltip: "PhobosTriage_SettingDefaultsDesc".Translate());
        AuthorityRow(listing, "PhobosTriage_GroupColonists", Settings.defaultColonist);
        AuthorityRow(listing, "PhobosTriage_GroupSlaves", Settings.defaultSlave);
        AuthorityRow(listing, "PhobosTriage_GroupPrisoners", Settings.defaultPrisoner);
        AuthorityRow(listing, "PhobosTriage_GroupGuests", Settings.defaultGuest);
        listing.Gap();
        listing.Label("PhobosTriage_SettingMaxDeath".Translate((Settings.maxDeathOnFailureChance * 100f).ToString("0").Named("PERCENT")),
            tooltip: "PhobosTriage_SettingMaxDeathDesc".Translate());
        Settings.maxDeathOnFailureChance = listing.Slider(Settings.maxDeathOnFailureChance, 0f, 0.5f);
        listing.CheckboxLabeled("PhobosTriage_SettingRareTools".Translate(), ref Settings.allowRareTools, "PhobosTriage_SettingRareToolsDesc".Translate());
        listing.CheckboxLabeled("PhobosTriage_SettingViolations".Translate(), ref Settings.allowViolations, "PhobosTriage_SettingViolationsDesc".Translate());
        listing.CheckboxLabeled("PhobosTriage_SettingMessages".Translate(), ref Settings.messages, "PhobosTriage_SettingMessagesDesc".Translate());
        listing.CheckboxLabeled("PhobosTriage_SettingOrdering".Translate(), ref Settings.triageOrdering, "PhobosTriage_SettingOrderingDesc".Translate());
    }

    static void AuthorityRow(Listing_Standard listing, string groupKey, TriageAuthority current)
    {
        if (listing.ButtonTextLabeled(groupKey.Translate(), ("PhobosTriage_Authority" + current).Translate()))
        {
            var options = new List<FloatMenuOption>();
            foreach (TriageAuthority authority in System.Enum.GetValues(typeof(TriageAuthority)))
            {
                TriageAuthority chosen = authority;
                options.Add(new FloatMenuOption(("PhobosTriage_Authority" + chosen).Translate(), () => SetDefault(groupKey, chosen)));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }
    }

    /// <summary>A float menu outlives the settings frame, so the choice lands by group name rather than by reference.</summary>
    static void SetDefault(string groupKey, TriageAuthority authority)
    {
        switch (groupKey)
        {
            case "PhobosTriage_GroupSlaves": Settings.defaultSlave = authority; break;
            case "PhobosTriage_GroupPrisoners": Settings.defaultPrisoner = authority; break;
            case "PhobosTriage_GroupGuests": Settings.defaultGuest = authority; break;
            default: Settings.defaultColonist = authority; break;
        }
    }
}
