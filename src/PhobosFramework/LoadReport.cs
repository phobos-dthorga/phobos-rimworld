using System.Collections.Generic;
using Verse;

namespace Phobos.Framework;

/// <summary>
/// The one line every Phobos mod logs when it loads: what is on, and what is off and why. Players are asked to quote it, so it
/// keeps one shape: "[Prefix] version loaded. On: a, b. OFF because ...: c."
/// </summary>
public sealed class LoadReport
{
    readonly string prefix;
    readonly string version;
    readonly List<string> on = new();
    readonly List<string> off = new();

    public LoadReport(string prefix, string version)
    {
        this.prefix = prefix;
        this.version = version;
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

    public override string ToString() =>
        prefix + " " + version + " loaded. On: " + (on.Count == 0 ? "nothing" : string.Join(", ", on)) + "."
        + (off.Count == 0 ? "" : " OFF: " + string.Join(", ", off) + ".");

    public void Log() => Verse.Log.Message(ToString());
}
