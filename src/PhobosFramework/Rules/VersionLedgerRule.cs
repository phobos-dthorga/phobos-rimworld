using System;
using System.Collections.Generic;
using System.Globalization;

namespace Phobos.Framework.Rules;

/// <summary>
/// The version ledger's lines, game-free (Framework 0.287.0; docs/development/test-automation.md, part 3). A save does not record which
/// version of a mod was running when something happened, so a line is kept each time one of our mods starts running at a version it was
/// not running before: "tick|mod name|version". phobos-modkit's test-pass reads them, so an event a vehicle saved names the version that
/// recorded it, and one older than its row's change counts as stale. The format is the kit's (docs/test-pass.md, "Saved histories"):
/// change it there first.
/// </summary>
public static class VersionLedgerRule
{
    public const char Separator = '|';

    /// <summary>One line of the ledger. A name or version holding the separator has it replaced, so a line always reads back.</summary>
    public static string Line(int tick, string mod, string version) =>
        tick.ToString(CultureInfo.InvariantCulture) + Separator + Clean(mod) + Separator + Clean(version);

    static string Clean(string text) => (text ?? "").Replace(Separator, '/').Trim();

    /// <summary>A line read back; false for anything else.</summary>
    public static bool TryRead(string line, out int tick, out string mod, out string version)
    {
        tick = 0;
        mod = version = "";
        string[] parts = (line ?? "").Split(Separator);
        if (parts.Length != 3 || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out tick)) return false;
        mod = parts[1].Trim();
        version = parts[2].Trim();
        return mod.Length > 0;
    }

    /// <summary>The version a mod last started running at, by the ledger; null when it has no line.</summary>
    public static string? LastVersion(IEnumerable<string> lines, string mod)
    {
        string? found = null;
        foreach (string line in lines)
        {
            if (TryRead(line, out _, out string name, out string version) && string.Equals(name, mod, StringComparison.Ordinal))
                found = version;
        }
        return found;
    }

    /// <summary>Whether a mod running at this version needs a new line: it has none, or its last says another version.</summary>
    public static bool NeedsLine(IEnumerable<string> lines, string mod, string version) =>
        !string.Equals(LastVersion(lines, Clean(mod)), Clean(version), StringComparison.Ordinal);
}
