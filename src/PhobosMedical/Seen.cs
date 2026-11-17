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
}
