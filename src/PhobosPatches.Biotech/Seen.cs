using System.Collections.Generic;
using Phobos.Framework.Debug;
using Verse;

namespace Phobos.BiotechPatches;

/// <summary>
/// D'thorga Patches' Biotech part's "seen in play" notes for testing (Phobos.Framework.Debug.SeenInPlay, behind D'thorga Framework's
/// setting, off by default). Each key is tied to test rows by config/test-evidence.json in the repository; tests/test_test_evidence.py holds
/// the two to each other. Reads one boolean when the setting is off, before building any text. Main thread only.
/// </summary>
public static class Seen
{
    const string Mod = "D'thorga Patches";

    /// <summary>Whether this note would be written now (on, and not yet seen this session on this thing): check it before building a note's text.</summary>
    public static bool Wants(string key, Thing? on = null) => SeenInPlay.Wants(key, null, on);

    /// <summary>Notes a feature seen working, with the thing it was seen on, what was seen, and its figures.</summary>
    public static void Note(string key, Thing on, string what, IReadOnlyList<KeyValuePair<string, double>>? values = null)
    {
        if (!SeenInPlay.Wants(key, null, on)) return;
        SeenInPlay.Note(Mod, BiotechPatchesMod.ModVersion, key, on.LabelShort + ": " + what, on, null, values);
    }
}
