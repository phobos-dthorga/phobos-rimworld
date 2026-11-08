using System.Collections.Generic;
using Verse;

namespace Phobos.Framework;

/// <summary>
/// The one line every Phobos mod logs when it loads: what is on, and what is off and why. Players are asked to quote it, so it
/// keeps one shape: "[Prefix] version loaded. On: a, b. OFF because ...: c." A mod made of several parts (D'thorga Medical, D'thorga
/// Patches) logs one line per part, named: "[Prefix] version loaded (part). On: ...".
/// </summary>
public sealed class LoadReport
{
    readonly string prefix;
    readonly string version;
    readonly string? part;
    readonly List<string> on = new();
    readonly List<string> off = new();

    public LoadReport(string prefix, string version, string? part = null)
    {
        this.prefix = prefix;
        this.version = version;
        this.part = part;
    }

    public LoadReport On(string feature)
    {
        on.Add(feature);
        return this;
    }

    public LoadReport Off(string feature, string reason)
    {
        off.Add(feature + " (" + reason + ")");
        return this;
    }

    public override string ToString() => prefix + " " + Body();

    string Body() =>
        version + " loaded" + (part == null ? "" : " (" + part + ")") + ". On: " + (on.Count == 0 ? "nothing" : string.Join(", ", on)) + "."
        + (off.Count == 0 ? "" : " OFF: " + string.Join(", ", off) + ".");

    /// <summary>Written under the mod's own log category, the prefix without its brackets, so it keeps the shape players quote.</summary>
    public void Log() => Logging.HostLog.Write(prefix.Trim('[', ']', ' '), Logging.HostLevel.Info, Body());
}
