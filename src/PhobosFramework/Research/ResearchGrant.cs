using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using RimWorld;
using Verse;

namespace Phobos.Framework.Research;

/// <summary>On a research project: the older research whose content this project took over. An existing colony that had all of it gets the project free, once (docs/development/research-tree.md).</summary>
public class ResearchReplacesExtension : DefModExtension
{
    public List<ResearchProjectDef> replaces = new();
}

/// <summary>Remembers which research projects a colony has already been considered for, by name, so a project added in a later version also grants once. Names, so that a removed mod leaves nothing the game cannot load.</summary>
public sealed class ResearchGrantComponent : GameComponent
{
    HashSet<string> handled = new();

    public ResearchGrantComponent(Game game)
    {
    }

    public override void ExposeData()
    {
        Scribe_Collections.Look(ref handled, "PhobosResearchGrantHandled", LookMode.Value);
        handled ??= new HashSet<string>();
    }

    public override void StartedNewGame() => ResearchGrantService.MarkHandled(handled);

    public override void LoadedGame() => ResearchGrantService.GrantOnLoad(handled);
}

/// <summary>Decides and applies the one-time grant.</summary>
public static class ResearchGrantService
{
    static IEnumerable<(ResearchProjectDef project, ResearchReplacesExtension extension)> Extended() =>
        DefDatabase<ResearchProjectDef>.AllDefsListForReading
            .Select(project => (project, extension: project.GetModExtension<ResearchReplacesExtension>()))
            .Where(entry => entry.extension != null);

    /// <summary>A new colony researches everything normally, so every project counts as considered from the start.</summary>
    public static void MarkHandled(HashSet<string> handled)
    {
        foreach ((ResearchProjectDef project, _) in Extended()) handled.Add(project.defName);
    }

    public static void GrantOnLoad(HashSet<string> handled)
    {
        int granted = 0;
        foreach ((ResearchProjectDef project, ResearchReplacesExtension extension) in Extended())
        {
            int unfinished = extension.replaces.Count(replaced => !replaced.IsFinished);
            ResearchGrantDecision decision = ResearchGrantRule.Decide(handled.Contains(project.defName), extension.replaces.Count, unfinished);
            if (decision == ResearchGrantDecision.LeaveAlone) continue;
            handled.Add(project.defName);
            if (decision != ResearchGrantDecision.Grant || project.IsFinished) continue;
            Finish(project);
            granted++;
        }
        if (granted == 0) return;
        FrameworkMod.Logger.Info("Finished " + granted + " research project(s) this colony already had the groundwork for.");
        int count = granted;
        LongEventHandler.ExecuteWhenFinished(() =>
            Messages.Message("PhobosFramework_ResearchGranted".Translate(count.Named("COUNT")), MessageTypeDefOf.NeutralEvent, historical: false));
    }

    /// <summary>FinishProject also finishes any unfinished prerequisite, which would give away other research, so it is used only when there are none.</summary>
    static void Finish(ResearchProjectDef project)
    {
        ResearchManager research = Find.ResearchManager;
        if (project.PrerequisitesCompleted) research.FinishProject(project, doCompletionDialog: false, researcher: null, doCompletionLetter: false);
        else research.AddProgress(project, project.Cost);
    }
}
