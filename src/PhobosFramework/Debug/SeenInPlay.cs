using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Phobos.Framework.Rules;
using Verse;

namespace Phobos.Framework.Debug;

/// <summary>
/// "Seen in play" notes for testing (Framework 0.118.0; owner, 17 Nov 2026: tests the owner sees pass rarely reach the test list).
/// With the setting on, the first time each feature is seen working in a session, one line goes to a file beside the colony mood file
/// and one to the log. phobos-modkit's test-pass tool reads the file, and D'thorga Companion's Tests page shows the rows it flags as
/// "seen working, awaiting the owner's word": a line proves the code ran without error, never that the result looked right, so only the
/// owner's verdict passes a row. Off by default: one boolean is read and nothing else happens. Never saved with a game.
/// </summary>
public static class SeenInPlay
{
    public const string FileName = "SeenInPlay.jsonl";

    /// <summary>The file the notes go to, in the game's own data folder beside the colony mood file.</summary>
    public static string OutputPath => Path.Combine(GenFilePaths.SaveDataFolderPath, Mood.ColonyMoodFile.FolderName, FileName);

    static readonly HashSet<string> seenThisSession = new();
    static readonly object gate = new();
    static bool failureLogged;

    /// <summary>Whether notes are being taken (the setting).</summary>
    public static bool On => FrameworkMod.Settings.noteSeenInPlay;

    /// <summary>
    /// Notes that a feature was seen working, once a session per key. <paramref name="key"/> is stable and dotted
    /// ("vehicles.field-refit"); the test list's map (config/test-evidence.json in the repository) ties keys to test rows.
    /// <paramref name="detail"/> says what it was seen on, in a few words.
    /// </summary>
    public static void Note(string mod, string version, string key, string? detail = null)
    {
        if (!FrameworkMod.Settings.noteSeenInPlay) return;
        lock (gate)
        {
            if (!seenThisSession.Add(key)) return;
        }
        string line = SeenLineRule.Line(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture), key, mod, version, detail);
        FrameworkMod.Logger.Info("seen in play: " + key + (string.IsNullOrEmpty(detail) ? "" : " (" + detail + ")") + ", " + mod + " " + version);
        try
        {
            string path = OutputPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            lock (gate) File.AppendAllText(path, line + "\n", new System.Text.UTF8Encoding(false));
        }
        catch (Exception problem)
        {
            if (failureLogged) return;
            failureLogged = true;
            FrameworkMod.Logger.Warn("The seen-in-play notes could not be written (" + problem.Message + "). Nothing else is affected. "
                + "This is said once a session.");
        }
    }
}
