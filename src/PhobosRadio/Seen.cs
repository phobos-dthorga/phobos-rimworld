using Phobos.Framework.Debug;
using Verse;

namespace Phobos.Radio;

/// <summary>
/// D'thorga Radio Framework's "seen in play" notes for testing (Radio 0.12.0; Phobos.Framework.Debug.SeenInPlay, behind D'thorga
/// Framework's setting, off by default). Each key is tied to test rows by config/test-evidence.json in the repository;
/// tests/test_test_evidence.py holds the two to each other. Reads one boolean when the setting is off, before building any text.
/// </summary>
public static class Seen
{
    private const string Mod = "D'thorga Radio Framework";

    /// <summary>Whether this note would be written now: check it before building a note's text at the call.</summary>
    public static bool Wants(string key, Thing? on = null, string? variant = null) => SeenInPlay.Wants(key, variant, on);

    /// <summary>Notes a feature seen working, with what it was seen on and which case it was.</summary>
    public static void Note(string key, Thing? on = null, string? what = null, string? variant = null)
    {
        if (!SeenInPlay.Wants(key, variant, on)) return;
        var detail = on == null ? what : what == null ? on.LabelShort : on.LabelShort + ": " + what;
        SeenInPlay.Note(Mod, typeof(Seen).Assembly.GetName().Version?.ToString(3) ?? "?", key, detail, on, variant);
    }
}
