using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Phobos.Framework.Rules;
using RimWorld;
using Verse;

namespace Phobos.Framework.Debug;

/// <summary>
/// "Seen in play" notes for testing (Framework 0.118.0; owner, 17 Nov 2026: tests the owner sees pass rarely reach the test list).
/// With the setting on, the first time each feature is seen working in a session, one line goes to a file beside the colony mood file
/// and one to the log. phobos-modkit's test-pass tool reads the file, and D'thorga Companion's Tests page shows the rows it flags as
/// "seen working, awaiting the owner's word": a line proves the code ran without error, never that the result looked right, so only the
/// owner's verdict passes a row. Off by default: one boolean is read and nothing else happens. Never saved with a game.
/// Framework 0.138.0 (docs/development/seen-in-play-checks.md): a note says what it was seen on, which case and its figures, is new
/// once a session per key, case and thing, and with dev mode and the second setting on, a message says when a listed test happened.
/// </summary>
public static class SeenInPlay
{
    public const string FileName = "SeenInPlay.jsonl";

    /// <summary>The file the notes go to, in the game's own data folder beside the colony mood file.</summary>
    public static string OutputPath => Path.Combine(GenFilePaths.SaveDataFolderPath, Mood.ColonyMoodFile.FolderName, FileName);

    static readonly HashSet<string> seenThisSession = new();
    static readonly HashSet<string> rowsSaidThisSession = new();
    static readonly object gate = new();
    static bool failureLogged;
    static List<TestRowsRule.Entry>? testRows;

    /// <summary>Whether notes are being taken (the setting).</summary>
    public static bool On => FrameworkMod.Settings.noteSeenInPlay;

    /// <summary>
    /// Notes that a feature was seen working, once a session per key, case and thing. <paramref name="key"/> is stable and dotted
    /// ("vehicles.field-refit"); the test list's map (config/test-evidence.json in the repository) ties keys to test rows.
    /// <paramref name="detail"/> says what it was seen on, in a few words; <paramref name="on"/> is the thing itself, written as its ID and
    /// kind; <paramref name="variant"/> is which case of the feature (a stage's label); <paramref name="values"/> are its figures.
    /// </summary>
    public static void Note(string mod, string version, string key, string? detail = null, Thing? on = null, string? variant = null,
        IReadOnlyList<KeyValuePair<string, double>>? values = null)
    {
        if (!FrameworkMod.Settings.noteSeenInPlay) return;
        string? id = on?.ThingID;
        lock (gate)
        {
            if (!seenThisSession.Add(SeenLineRule.Identity(key, variant, id))) return;
        }
        string line = SeenLineRule.Line(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture), key, mod, version, detail,
            id, on?.def?.defName, variant, values);
        FrameworkMod.Logger.Info("seen in play: " + key + (string.IsNullOrEmpty(variant) ? "" : " [" + variant + "]")
            + (string.IsNullOrEmpty(detail) ? "" : " (" + detail + ")") + ", " + mod + " " + version);
        try
        {
            string path = OutputPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            lock (gate) File.AppendAllText(path, line + "\n", new System.Text.UTF8Encoding(false));
        }
        catch (Exception problem)
        {
            if (!failureLogged)
            {
                failureLogged = true;
                FrameworkMod.Logger.Warn("The seen-in-play notes could not be written (" + problem.Message + "). Nothing else is affected. "
                    + "This is said once a session.");
            }
        }
        SayTestRow(key, variant, detail, on);
    }

    /// <summary>
    /// The test-moment message (Framework 0.138.0, E8): with dev mode and its setting on, the first time a session sees a note standing for
    /// a row still waiting for the owner, a message says which. Dev mode only, as it names a test row. The rows come from the installed
    /// mod's TestRows.tsv, written by scripts/deploy.py; without it nothing is said.
    /// </summary>
    static void SayTestRow(string key, string? variant, string? detail, Thing? on)
    {
        if (!FrameworkMod.Settings.sayWhenTestHappens || !Prefs.DevMode || !UnityData.IsInMainThread) return;
        foreach (TestRowsRule.Entry row in TestRows())
        {
            if (!row.Matches(key, variant, detail)) continue;
            lock (gate)
            {
                if (!rowsSaidThisSession.Add(row.Row)) continue;
            }
            string text = "PhobosFramework_SeenTestRow".Translate(row.Row.Named("ROW"), row.Title.Named("TITLE"), (detail ?? key).Named("DETAIL"));
            Messages.Message(text, on is { Spawned: true } ? new LookTargets(on) : LookTargets.Invalid, MessageTypeDefOf.SilentInput, historical: false);
        }
    }

    /// <summary>The rows from the installed mod's TestRows.tsv, read once; none when it is missing or unreadable.</summary>
    static List<TestRowsRule.Entry> TestRows()
    {
        if (testRows != null) return testRows;
        var found = new List<TestRowsRule.Entry>();
        try
        {
            string? root = FrameworkMod.Instance?.Content?.RootDir;
            string path = root == null ? "" : Path.Combine(root, TestRowsRule.FileName);
            if (path.Length > 0 && File.Exists(path)) found = TestRowsRule.Parse(File.ReadAllLines(path));
        }
        catch (Exception problem)
        {
            FrameworkMod.Logger.Warn("The test rows could not be read (" + problem.Message + "); no test-moment messages this session.");
        }
        return testRows = found;
    }
}
