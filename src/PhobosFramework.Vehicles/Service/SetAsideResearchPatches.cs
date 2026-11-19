using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Phobos.Framework;
using Phobos.Framework.Research;
using RimWorld;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Research set aside, out of view (Framework 0.179.0; owner, 20 Nov 2026: submarine research "visible for colonies with pre-existing
/// data but for those otherwise ... invisible and out-of-sight"). The decision is the core's (<see cref="SetAsideResearch"/>); these
/// patches carry it into the two research windows, here because the core uses no Harmony and this part's mod brings it. Postfixes
/// only: each removes what the window was about to show, and does nothing for a colony that has started the tab's research.
/// </summary>
static class SetAsideResearchWindows
{
    public const string NiceResearchTabPackageId = "Andromeda.NiceResearchTab";
}

/// <summary>
/// The game's research window lists every research tab when it opens; a set-aside tab out of view is taken off the list (its search
/// results go with it, since the search walks the list), and the window leaves it if it was the one showing. The Dead Man's Switch
/// hides its own tabs the same way.
/// </summary>
[HarmonyPatch(typeof(MainTabWindow_Research), nameof(MainTabWindow_Research.PostOpen))]
public static class SetAsideResearchGameWindowPatch
{
    static readonly FieldInfo? RecordDef = AccessTools.Field(AccessTools.Inner(typeof(MainTabWindow_Research), "ResearchTabRecord"), "def");

    static bool Prepare() => RecordDef != null;

    public static void Postfix(MainTabWindow_Research __instance, IList ___tabs)
    {
        if (___tabs == null || RecordDef == null) return;
        HashSet<ResearchTabDef> hidden = SetAsideResearch.HiddenTabs();
        if (hidden.Count == 0) return;
        for (int i = ___tabs.Count - 1; i >= 0; i--)
        {
            if (RecordDef.GetValue(___tabs[i]) is ResearchTabDef tab && hidden.Contains(tab))
            {
                ___tabs.RemoveAt(i);
                // Framework 0.179.0 (row 254): a set-aside tab taken out of the game's research window.
                Seen.Note("research.set-aside-hidden", null, tab.defName, "game window");
            }
        }
        if (__instance.CurTab != null && hidden.Contains(__instance.CurTab)) __instance.CurTab = ResearchTabDefOf.Main;
    }
}

/// <summary>
/// Nice Research Tab builds its own tree from every project, grouped by tab (or by mod), in one private method before laying it out.
/// The projects of a set-aside tab out of view are taken out of what it returns, whichever group they landed in. A tie-in by name (no
/// reference to its assembly), checked by scripts/check-targets.py; it stands down when the mod is absent or the method is renamed.
/// </summary>
[HarmonyPatch]
public static class SetAsideResearchNiceResearchTabPatch
{
    static bool Prepare() => TargetMethod() != null;

    static MethodBase? TargetMethod() => OptionalMods.IsActive(SetAsideResearchWindows.NiceResearchTabPackageId)
        ? AccessTools.Method("NiceResearchTab.NodeServer:GroupProjectsByTab")
        : null;

    public static void Postfix(Dictionary<ResearchTabDef, List<ResearchProjectDef>> __result)
    {
        if (__result == null) return;
        HashSet<ResearchTabDef> hidden = SetAsideResearch.HiddenTabs();
        if (hidden.Count == 0) return;
        foreach (List<ResearchProjectDef> projects in __result.Values)
        {
            if (projects.RemoveAll(project => project.tab != null && hidden.Contains(project.tab)) > 0)
            {
                // Framework 0.179.0 (row 254): a set-aside tab's projects taken out of Nice Research Tab's tree.
                Seen.Note("research.set-aside-hidden", null, null, "nice research tab");
            }
        }
    }
}
