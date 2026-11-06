using Phobos.Framework;
using Phobos.Framework.Health;
using Phobos.Triage.Rules;
using Phobos.Triage.Services;
using Verse;

namespace Phobos.Triage;

/// <summary>The per-person choice this mod offers, registered with the framework at startup, and the triage line on the pawn.</summary>
[StaticConstructorOnStartup]
public static class TriagePreferences
{
    public static readonly PatientPreference Authority = new(
        "PhobosTriage_Authority", "PhobosTriage_AuthorityLabel", "PhobosTriage_AuthorityDesc",
        new[] { "PhobosTriage_AuthorityNone", "PhobosTriage_AuthorityLifeSaving", "PhobosTriage_AuthorityLifeSavingAndCrippling", "PhobosTriage_AuthorityAnyRepair" },
        DefaultFor, iconPath: AuthorityIconPath);

    /// <summary>The button's own icon, a scalpel across a red cross; a missing texture falls back to the framework's default.</summary>
    public const string AuthorityIconPath = "Phobos/Triage/UI/AuthorityIcon";

    static TriagePreferences()
    {
        PatientPreferences.Register(Authority);
        PatientPreferences.RegisterInspectLine(TriageText.InspectLine);
    }

    static int DefaultFor(Pawn pawn) => (int)TriageMod.Settings.DefaultFor(Patients.GroupOf(pawn) ?? PatientGroup.Colonist);
}

/// <summary>The words a player reads: the triage line and the reasons. Everything comes from Keyed strings; nothing names a mod.</summary>
public static class TriageText
{
    public static string? InspectLine(Pawn pawn)
    {
        TriageEntry? entry = MapComponent_PhobosTriage.For(pawn.Map)?.EntryOf(pawn);
        if (entry == null || entry.Worst == null || entry.Category == TriageCategory.Minor) return null;
        string detail;
        if (entry.WorstClass == ConditionClass.Lethal)
        {
            bool clock = !float.IsInfinity(entry.HoursToDeath) && TriageMod.Settings.displayMode != DisplayMode.Plain;
            detail = clock
                ? "PhobosTriage_DetailDyingClock".Translate(entry.Worst.LabelBase.Named("CONDITION"), entry.HoursToDeath.ToString("0").Named("HOURS"))
                : "PhobosTriage_DetailDying".Translate(entry.Worst.LabelBase.Named("CONDITION"));
        }
        else
        {
            detail = "PhobosTriage_DetailCrippled".Translate(entry.Worst.LabelBase.Named("CONDITION"));
        }
        if (entry.Held && entry.HoldReason != null)
        {
            detail += " " + (entry.Queued != null
                ? "PhobosTriage_DetailQueued".Translate(entry.Queued.LabelCap.Named("OPERATION")) + " " + "PhobosTriage_DetailHeldQueued".Translate(entry.HoldReason.Named("WHY"))
                : "PhobosTriage_DetailHeld".Translate(entry.HoldReason.Named("WHY")));
        }
        else if (entry.Queued != null) detail += " " + "PhobosTriage_DetailQueued".Translate(entry.Queued.LabelCap.Named("OPERATION"));
        else if (entry.Refusal != Refusal.None) detail += " " + "PhobosTriage_DetailNoOne".Translate(RefusalText(entry.Refusal).Named("WHY"));
        return "PhobosTriage_InspectLine".Translate(("PhobosTriage_Category" + entry.Category).Translate().Named("CATEGORY"), detail.Named("DETAIL"));
    }

    public static string RefusalText(Refusal refusal) => ("PhobosTriage_Refusal" + refusal).Translate();
}
