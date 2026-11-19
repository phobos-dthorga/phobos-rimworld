using Phobos.Framework.Debug;
using Verse;

namespace Phobos.ClassicalPatches;

/// <summary>
/// D'thorga Patches' Classical part's "seen in play" notes for testing (Phobos.Framework.Debug.SeenInPlay, behind D'thorga Framework's
/// setting, off by default). Each key is tied to test rows by config/test-evidence.json in the repository; tests/test_test_evidence.py holds
/// the two to each other. Reads one boolean when the setting is off, before building any text. Main thread only.
/// </summary>
public static class Seen
{
    const string Mod = "D'thorga Patches";

    /// <summary>Whether this note would be written now (on, and not yet seen this session): check it before building a note's text.</summary>
    public static bool Wants(string key, Thing? on = null) => SeenInPlay.Wants(key, null, on);

    /// <summary>Notes a feature seen working, with what it was seen on.</summary>
    public static void Note(string key, Thing? on = null, string? what = null)
    {
        if (!SeenInPlay.On) return;
        string? detail = on == null ? what : what == null ? on.LabelShort : on.LabelShort + ": " + what;
        SeenInPlay.Note(Mod, ClassicalPatchesMod.ModVersion, key, detail);
    }
}
