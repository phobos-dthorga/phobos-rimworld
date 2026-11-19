using System;
using System.Text.RegularExpressions;

namespace Phobos.Framework.Rules;

/// <summary>How an error's trace shows our code on its path, strongest first.</summary>
public enum AttributionKind
{
    /// <summary>No sign of our code.</summary>
    None,
    /// <summary>A Harmony patch of ours sits on a method on the path: we may be innocent.</summary>
    OurPatch,
    /// <summary>Our own code was running: a frame of ours is in the trace.</summary>
    OurFrame,
}

/// <summary>What an error's trace says about our part in it: how, and where (a type and member of ours).</summary>
public readonly record struct Attribution(AttributionKind Kind, string Where)
{
    public static readonly Attribution Nothing = new(AttributionKind.None, "");
}

/// <summary>
/// Reads an error's text and trace for our code on its path, game-free (Framework 0.170.0; docs/development/logging-framework-design.md,
/// "Saying when our code was on an error's path"). The owner asked (19 Nov 2026) that an error which passed through our code say so, so
/// that a report reaches us and not only the other author whose code also shows. Two clues: a frame of ours ("at Phobos.…"), the
/// stronger, and a Harmony annotation naming our Harmony ID ("- PREFIX PhobosDthorga.…: …"), which Better Stacktraces and Harmony add
/// under a patched frame. It says where our code was, never that it caused the error. Frames of our logging code are left out: every
/// line we log ourselves passes through them.
/// </summary>
public static class ErrorAttributionRule
{
    /// <summary>The start every namespace of ours shares.</summary>
    public const string OurNamespace = "Phobos.";

    /// <summary>The start every Harmony ID of ours shares: the packageIds.</summary>
    public const string OurHarmonyId = "PhobosDthorga.";

    /// <summary>Notes a session may write at most, so a storm of different errors cannot double the log (agent default).</summary>
    public const int MaxNotesPerSession = 50;

    /// <summary>The longest quote of the error a note carries, characters.</summary>
    public const int QuoteLength = 120;

    static readonly string[] OwnLogging = { "Phobos.Logging.", "Phobos.Framework.Logging." };
    static readonly string[] OwnPrefixes = { "[D'thorga", "[Phobos" };

    /// <summary>Whether this message is one of ours already: it names us, so it needs no note.</summary>
    public static bool IsOurOwnMessage(string? message)
    {
        if (string.IsNullOrEmpty(message)) return false;
        string start = message!.TrimStart();
        foreach (string prefix in OwnPrefixes)
            if (start.StartsWith(prefix, StringComparison.Ordinal)) return true;
        return false;
    }

    /// <summary>Our part in an error, from its message (which may hold the exception's trace) and its call-site trace.</summary>
    public static Attribution Find(string? message, string? stackTrace)
    {
        if (IsOurOwnMessage(message)) return Attribution.Nothing;
        Attribution best = Attribution.Nothing;
        foreach (string? text in new[] { message, stackTrace })
        {
            if (string.IsNullOrEmpty(text)) continue;
            foreach (string raw in text!.Split('\n'))
            {
                string line = raw.Trim();
                string? frame = OurFrame(line);
                if (frame != null) return new Attribution(AttributionKind.OurFrame, frame);
                if (best.Kind == AttributionKind.None)
                {
                    string? patch = OurPatch(line);
                    if (patch != null) best = new Attribution(AttributionKind.OurPatch, patch);
                }
            }
        }
        return best;
    }

    /// <summary>The type and member of ours a trace line runs in, or null: "at Phobos.X.Y.Member(…)", Harmony's leading dot allowed.</summary>
    public static string? OurFrame(string line)
    {
        if (!line.StartsWith("at ", StringComparison.Ordinal)) return null;
        string name = line.Substring(3).TrimStart('.');
        int paren = name.IndexOf('(');
        if (paren >= 0) name = name.Substring(0, paren);
        name = name.Trim();
        if (!name.StartsWith(OurNamespace, StringComparison.Ordinal)) return null;
        foreach (string own in OwnLogging)
            if (name.StartsWith(own, StringComparison.Ordinal)) return null;
        return Tidy(name);
    }

    /// <summary>The patch method of ours a Harmony annotation names, or null: "- PREFIX PhobosDthorga.X: Target &lt;- Type Ns.Patch:Method(…)".</summary>
    public static string? OurPatch(string line)
    {
        if (!line.StartsWith("- ", StringComparison.Ordinal)) return null;
        int space = line.IndexOf(' ', 2);
        int colon = line.IndexOf(": ", StringComparison.Ordinal);
        if (space < 0 || colon < space) return null;
        string owner = line.Substring(space + 1, colon - space - 1).Trim();
        if (!owner.StartsWith(OurHarmonyId, StringComparison.OrdinalIgnoreCase)) return null;
        int arrow = line.IndexOf("<- ", StringComparison.Ordinal);
        if (arrow < 0) return owner;
        string method = line.Substring(arrow + 3);
        int typeEnd = method.IndexOf(' ');
        if (typeEnd >= 0) method = method.Substring(typeEnd + 1);
        int paren = method.IndexOf('(');
        if (paren >= 0) method = method.Substring(0, paren);
        method = method.Trim().Replace(':', '.');
        return method.Length == 0 ? owner : Tidy(method);
    }

    /// <summary>The type name a "Type.Member" names: everything before the last dot.</summary>
    public static string TypeOf(string where)
    {
        int dot = where.LastIndexOf('.');
        return dot > 0 ? where.Substring(0, dot) : where;
    }

    /// <summary>The error's first line, cut to <see cref="QuoteLength"/> characters, for a note to quote.</summary>
    public static string Quote(string? message)
    {
        if (string.IsNullOrEmpty(message)) return "(no message)";
        string first = message!.Split('\n')[0].Trim();
        return first.Length <= QuoteLength ? first : first.Substring(0, QuoteLength) + "…";
    }

    /// <summary>Drops the compiler's names for lambdas and iterators ("Type.&lt;&gt;c.&lt;Method&gt;b__0" becomes "Type.Method").</summary>
    static string Tidy(string name)
    {
        int angle = name.IndexOf('<');
        if (angle < 0) return name;
        string head = name.Substring(0, angle).TrimEnd('.', '+');
        MatchCollection named = Regex.Matches(name, "<([^<>]+)>");
        return named.Count == 0 ? head : head + "." + named[named.Count - 1].Groups[1].Value;
    }
}
