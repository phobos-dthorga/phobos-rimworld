using System;
using System.Collections.Generic;

namespace Phobos.Framework.Rules;

/// <summary>
/// The test list's rows as the game sees them, for the test-moment message (Framework 0.138.0; docs/development/seen-in-play-checks.md,
/// E8): which rows still waiting for the owner each seen-in-play note stands for. scripts/deploy.py writes the file into the installed
/// mod only, never the repository's mod folder, so nothing of it ships. One line per piece of evidence, tab-separated: row, title, key,
/// case and a wildcard on the detail (the last two may be empty); a line starting with # is a comment. Game-free.
/// </summary>
public static class TestRowsRule
{
    public const string FileName = "TestRows.tsv";

    public sealed class Entry
    {
        public string Row = "";
        public string Title = "";
        public string Key = "";
        public string? Variant;
        public string? Detail;

        /// <summary>Whether a note with this key, case and detail stands for the row.</summary>
        public bool Matches(string key, string? variant, string? detail) =>
            key == Key && (Variant == null || Variant == variant) && (Detail == null || Wildcard(Detail, detail ?? ""));
    }

    /// <summary>The file's entries; lines that are not five fields with a row and a key are skipped.</summary>
    public static List<Entry> Parse(IEnumerable<string> lines)
    {
        var entries = new List<Entry>();
        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal)) continue;
            string[] fields = line.TrimEnd('\r').Split('\t');
            if (fields.Length != 5 || fields[0].Length == 0 || fields[2].Length == 0) continue;
            entries.Add(new Entry
            {
                Row = fields[0], Title = fields[1], Key = fields[2],
                Variant = fields[3].Length == 0 ? null : fields[3], Detail = fields[4].Length == 0 ? null : fields[4],
            });
        }
        return entries;
    }

    /// <summary>A wildcard as phobos-modkit's test-pass reads one: * for any run of characters, ? for one, ignoring case.</summary>
    public static bool Wildcard(string pattern, string text)
    {
        int p = 0, t = 0, star = -1, mark = 0;
        while (t < text.Length)
        {
            if (p < pattern.Length && (pattern[p] == '?' || char.ToLowerInvariant(pattern[p]) == char.ToLowerInvariant(text[t])))
            {
                p++;
                t++;
            }
            else if (p < pattern.Length && pattern[p] == '*')
            {
                star = p++;
                mark = t;
            }
            else if (star >= 0)
            {
                p = star + 1;
                t = ++mark;
            }
            else return false;
        }
        while (p < pattern.Length && pattern[p] == '*') p++;
        return p == pattern.Length;
    }
}
