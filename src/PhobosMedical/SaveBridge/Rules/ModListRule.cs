using System;
using System.Collections.Generic;

namespace Phobos.MedicalSaveBridge.Rules;

/// <summary>
/// Which active mods a save has never seen. The save's header lists the package IDs it was written with; the game compares them
/// case-insensitively (ScribeMetaHeaderUtility.ModListsMatch), and so does this. Order is ignored on purpose: reordering mods
/// changes nothing about what a save holds. Game-free.
/// </summary>
public static class ModListRule
{
    /// <summary>
    /// The package IDs, lower-cased, that are running now but absent from the save. An absent header (null) means the save can't
    /// be judged, so nothing is new and the bridge stays inert.
    /// </summary>
    public static HashSet<string> NewModIds(IEnumerable<string>? savedModIds, IEnumerable<string> runningModIds)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        if (savedModIds == null) return result;
        var saved = new HashSet<string>(StringComparer.Ordinal);
        foreach (string id in savedModIds)
        {
            if (id != null) saved.Add(id.ToLowerInvariant());
        }
        foreach (string id in runningModIds)
        {
            if (id == null) continue;
            string lower = id.ToLowerInvariant();
            if (!saved.Contains(lower)) result.Add(lower);
        }
        return result;
    }
}
