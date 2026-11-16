using System.Globalization;
using System.Text;

namespace Phobos.Framework.Rules;

/// <summary>
/// One line of the "seen in play" file (Framework 0.118.0; owner, 17 Nov 2026): a JSON object on its own line, read by phobos-modkit's
/// test-pass tool and D'thorga Companion's Tests page. Fields: format, time (UTC), key, mod, version and an optional detail. Game-free.
/// </summary>
public static class SeenLineRule
{
    /// <summary>The line format's version, raised if a field changes meaning.</summary>
    public const int Format = 1;

    /// <summary>The line for one sighting, without its line ending.</summary>
    public static string Line(string timeUtc, string key, string mod, string version, string? detail)
    {
        var text = new StringBuilder(128);
        text.Append("{\"format\":").Append(Format.ToString(CultureInfo.InvariantCulture));
        Field(text, "time", timeUtc);
        Field(text, "key", key);
        Field(text, "mod", mod);
        Field(text, "version", version);
        if (!string.IsNullOrEmpty(detail)) Field(text, "detail", detail!);
        return text.Append('}').ToString();
    }

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
