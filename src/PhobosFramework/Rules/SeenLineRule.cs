using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Phobos.Framework.Rules;

/// <summary>
/// One line of the "seen in play" file (Framework 0.118.0; owner, 17 Nov 2026): a JSON object on its own line, read by phobos-modkit's
/// test-pass tool and D'thorga Companion's Tests page. Fields: format, time (UTC), key, mod, version and an optional detail; since
/// Framework 0.138.0 (docs/development/seen-in-play-checks.md, N1) also, when given, what it was seen on (on), what kind of thing that is
/// (kind), which case of the feature (variant) and its figures (values). Readers ignore fields they do not know, so the format stays 1.
/// Game-free.
/// </summary>
public static class SeenLineRule
{
    /// <summary>The line format's version, raised if a field changes meaning.</summary>
    public const int Format = 1;

    /// <summary>The line for one sighting, without its line ending. A figure that is not a finite number is left out.</summary>
    public static string Line(string timeUtc, string key, string mod, string version, string? detail, string? on = null, string? kind = null,
        string? variant = null, IReadOnlyList<KeyValuePair<string, double>>? values = null)
    {
        var text = new StringBuilder(160);
        text.Append("{\"format\":").Append(Format.ToString(CultureInfo.InvariantCulture));
        Field(text, "time", timeUtc);
        Field(text, "key", key);
        Field(text, "mod", mod);
        Field(text, "version", version);
        if (!string.IsNullOrEmpty(detail)) Field(text, "detail", detail!);
        if (!string.IsNullOrEmpty(on)) Field(text, "on", on!);
        if (!string.IsNullOrEmpty(kind)) Field(text, "kind", kind!);
        if (!string.IsNullOrEmpty(variant)) Field(text, "variant", variant!);
        if (values != null && values.Count > 0)
        {
            text.Append(",\"values\":{");
            bool first = true;
            foreach (KeyValuePair<string, double> value in values)
            {
                if (double.IsNaN(value.Value) || double.IsInfinity(value.Value)) continue;
                if (!first) text.Append(',');
                first = false;
                Quote(text, value.Key);
                text.Append(':').Append(value.Value.ToString("R", CultureInfo.InvariantCulture));
            }
            text.Append('}');
        }
        return text.Append('}').ToString();
    }

    /// <summary>What makes a sighting new in a session: its key, its case and what it was seen on (Framework 0.138.0; before, the key
    /// alone, so a stage's warning and refusal shared one note a session and a second vehicle never had its own).</summary>
    public static string Identity(string key, string? variant, string? on) => key + "\u001f" + (variant ?? "") + "\u001f" + (on ?? "");

    static void Field(StringBuilder text, string name, string value)
    {
        text.Append(",\"").Append(name).Append("\":");
        Quote(text, value);
    }

    /// <summary>A JSON string: quotes, backslashes and control characters escaped, everything else as it is.</summary>
    public static void Quote(StringBuilder text, string value)
    {
        text.Append('"');
        foreach (char c in value ?? "")
        {
            switch (c)
            {
                case '"': text.Append("\\\""); break;
                case '\\': text.Append("\\\\"); break;
                case '\n': text.Append("\\n"); break;
                case '\r': text.Append("\\r"); break;
                case '\t': text.Append("\\t"); break;
                default:
                    if (c < ' ') text.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    else text.Append(c);
                    break;
            }
        }
        text.Append('"');
    }
}
