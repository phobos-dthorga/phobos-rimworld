using System.Collections.Generic;
using Phobos.Framework.Rules;
using RimWorld;
using Verse;

namespace Phobos.Framework.Research;

/// <summary>
/// Marks a research tab as set aside (Framework 0.179.0; owner, 20 Nov 2026): its projects are out of view in a colony that has put no
/// research into any of them, and in view, as before, in one that has (<see cref="SetAsideRule"/>). Any mod may put it on its own tab.
/// The core only decides; the research windows are patched where Harmony is at hand (D'thorga Framework's vehicles part, for the game's
/// window and Nice Research Tab's). Nothing is removed: the projects, their progress and what they unlock all stay loaded.
/// </summary>
public class ResearchSetAsideExtension : DefModExtension
{
}

/// <summary>Whether a research tab, or a project by its tab, is out of view in the colony now loaded.</summary>
public static class SetAsideResearch
{
    /// <summary>Whether this tab is set aside and out of view now. Cheap enough for a window opening: one pass over the projects.</summary>
    public static bool Hides(ResearchTabDef? tab)
    {
        if (tab == null || !tab.HasModExtension<ResearchSetAsideExtension>()) return false;
        return SetAsideRule.Hidden(true, ProgressOf(tab));
    }

    /// <summary>The set-aside tabs out of view now, for a caller that checks many projects.</summary>
    public static HashSet<ResearchTabDef> HiddenTabs()
    {
        var hidden = new HashSet<ResearchTabDef>();
        foreach (ResearchTabDef tab in DefDatabase<ResearchTabDef>.AllDefsListForReading)
        {
            if (Hides(tab)) hidden.Add(tab);
        }
        return hidden;
    }

    static List<float>? ProgressOf(ResearchTabDef tab)
    {
        ResearchManager? manager = Current.Game?.researchManager;
        if (manager == null) return null;
        var progress = new List<float>();
        foreach (ResearchProjectDef project in DefDatabase<ResearchProjectDef>.AllDefsListForReading)
        {
            if (project.tab == tab) progress.Add(manager.GetProgress(project));
        }
        return progress;
    }
}
