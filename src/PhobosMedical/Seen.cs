using Phobos.Framework.Debug;
using Verse;

namespace Phobos.Medical;

/// <summary>
/// D'thorga Medical's "seen in play" notes for testing (Phobos.Framework.Debug.SeenInPlay, behind D'thorga Framework's setting, off by
/// default). Each key is tied to test rows by config/test-evidence.json in the repository; tests/test_test_evidence.py holds the two
/// to each other. Reads one boolean when the setting is off, before building any text.
/// </summary>
public static class Seen
{
    const string Mod = "D'thorga Medical";

    /// <summary>Notes a feature seen working, with what it was seen on.</summary>
    public static void Note(string key, Thing? on = null, string? what = null)
    {
        if (!SeenInPlay.On) return;
        string? detail = on == null ? what : what == null ? on.LabelShort : on.LabelShort + ": " + what;
        SeenInPlay.Note(Mod, MedicalMod.ModVersion, key, detail);
    }

    /// <summary>Whether this note would be written now (on, and not yet seen this session): check it before building a note's text at the
    /// call (Medical 0.21.0; tests/test_seen_note_cost.py).</summary>
    public static bool Wants(string key, Thing? on = null, string? variant = null) => SeenInPlay.Wants(key, variant, on);

    /// <summary>Notes a feature seen working in one of its cases (Medical 0.21.0: the radiation model's phases).</summary>
    public static void Note(string key, Thing? on, string? what, string? variant)
    {
        if (!SeenInPlay.Wants(key, variant, on)) return;
        string? detail = on == null ? what : what == null ? on.LabelShort : on.LabelShort + ": " + what;
        SeenInPlay.Note(Mod, MedicalMod.ModVersion, key, detail, on, variant);
    }

    /// <summary>A finish action for every job of ours that goes to a patient: notes care that gave way to someone carrying them (Medical 0.18.0).</summary>
    public static void WhenFieldCareGaveWay(Verse.AI.JobDriver driver, System.Func<Pawn?> patient) =>
        driver.AddFinishAction(condition =>
        {
            if (condition != Verse.AI.JobCondition.Succeeded && patient() is { } lifted && Phobos.Framework.Health.FieldCare.BeingCarried(lifted))
            {
                Seen.Note("medical.field-care-gave-way", lifted, driver.job.def.label);
            }
        });
}
