using HarmonyLib;
using RimWorld;
using Verse;

namespace Phobos.VehicleSystems;

// Aboard-only research (docs/development/vehicle-seabed-crawler.md). The game has one current project, worked on by every bench, and no hook for a second; these three patches
// give aboard-only projects a slot of their own. Vanilla Gravship Expanded patches the same three methods for its own projects; each mod acts only on projects it has marked.

/// <summary>
/// Sends an aboard-only project to its own slot instead of the benches'. A prefix that skips the original, and needs to: whatever chooses a project (the research window, a
/// research queue, another mod) ends here, and a bench must never be handed a project only a laboratory aboard can do. It skips only for such a project.
/// </summary>
[HarmonyPatch(typeof(ResearchManager), nameof(ResearchManager.SetCurrentProject))]
public static class FieldResearchChosen
{
    public static bool Prefix(ResearchProjectDef proj)
    {
        if (!FieldResearch.IsFieldOnly(proj)) return true;
        FieldResearch.Start(proj);
        return false;
    }
}

/// <summary>An aboard-only project being worked on counts as current, so the research window shows it in progress and offers to stop it. A postfix.</summary>
[HarmonyPatch(typeof(ResearchManager), nameof(ResearchManager.IsCurrentProject))]
public static class FieldResearchIsCurrent
{
    public static void Postfix(ResearchProjectDef proj, ref bool __result)
    {
        if (!__result && proj != null && FieldResearch.Current == proj) __result = true;
    }
}

/// <summary>Stopping an aboard-only project empties its slot. A postfix.</summary>
[HarmonyPatch(typeof(ResearchManager), nameof(ResearchManager.StopProject))]
public static class FieldResearchStopped
{
    public static void Postfix(ResearchProjectDef proj)
    {
        if (proj != null) FieldResearch.Stop(proj);
    }
}
