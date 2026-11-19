using Verse;

namespace Phobos.Framework;

/// <summary>
/// D'thorga Framework core's "seen in play" notes for testing (Debug/SeenInPlay, behind its setting, off by default; AGENTS.md: a feature
/// with a test row notes itself). One helper for every part of the core, so each key is written in the one form (the helper's Note with the key first,
/// in quotes) that tests/test_test_evidence.py finds; moved here from TopUps (Framework 0.170.0) when the error notes needed it too. Main thread only.
/// </summary>
static class Seen
{
    /// <summary>Whether this note would be written now: check it before building a note's text (Framework 0.138.1).</summary>
    public static bool Wants(string key, Thing? on = null, string? variant = null) => Debug.SeenInPlay.Wants(key, variant, on);

    /// <summary>Notes a feature seen working, with what it was seen on, what was seen, and which case of the feature it was.</summary>
    public static void Note(string key, Thing? on = null, string? what = null, string? variant = null)
    {
        if (!Debug.SeenInPlay.Wants(key, variant, on)) return;
        string? detail = on == null ? what : what == null ? on.LabelShort : on.LabelShort + ": " + what;
        Debug.SeenInPlay.Note("D'thorga Framework", FrameworkMod.ModVersion, key, detail, on, variant);
    }
}
