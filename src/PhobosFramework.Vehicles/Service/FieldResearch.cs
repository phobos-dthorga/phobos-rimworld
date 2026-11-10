using Phobos.Framework.Rules;
using RimWorld;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Marks a research project, or a whole tab, as one that only a laboratory carried by a vehicle can work on (docs/development/vehicle-seabed-crawler.md, "Aboard-only research").
/// Such a project never becomes the benches' current project: choosing it puts it in a slot of its own, and benches carry on with theirs.
/// </summary>
public class FieldResearchExtension : DefModExtension
{
}

/// <summary>The aboard-only project the colony has chosen, saved with the game. One at a time, worked on by every laboratory aboard a vehicle of the player's.</summary>
public class GameComponent_PhobosFieldResearch : GameComponent
{
    ResearchProjectDef? project;

    public GameComponent_PhobosFieldResearch(Game game)
    {
    }

    /// <summary>The chosen project, or null once there is none or it has been finished.</summary>
    public ResearchProjectDef? Project
    {
        get
        {
            if (project != null && project.IsFinished) project = null;
            return project;
        }
        set => project = value;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref project, "PhobosFieldResearchProject");
    }
}

/// <summary>The one place aboard-only research is chosen, stopped and advanced. The research patches and the laboratory comp call only this.</summary>
public static class FieldResearch
{
    static GameComponent_PhobosFieldResearch? Slot => Verse.Current.Game?.GetComponent<GameComponent_PhobosFieldResearch>();

    /// <summary>Whether only a laboratory aboard a vehicle can work on this project.</summary>
    public static bool IsFieldOnly(ResearchProjectDef? project) =>
        project != null && (project.HasModExtension<FieldResearchExtension>() || (project.tab?.HasModExtension<FieldResearchExtension>() ?? false));

    /// <summary>The aboard-only project being worked on, or null.</summary>
    public static ResearchProjectDef? Current => Slot?.Project;

    /// <summary>Chooses an aboard-only project and says where it will be done. Saying it once, on a change, keeps a research queue that re-asks from repeating it.</summary>
    public static void Start(ResearchProjectDef project)
    {
        if (Slot is not { } slot || slot.Project == project) return;
        slot.Project = project;
        Messages.Message("PhobosFieldResearch_Started".Translate(project.LabelCap.Named("PROJECT")).Resolve(), MessageTypeDefOf.NeutralEvent, historical: false);
    }

    /// <summary>Stops the aboard-only project, if it is this one.</summary>
    public static void Stop(ResearchProjectDef project)
    {
        if (Slot is { } slot && slot.Project == project) slot.Project = null;
    }

    /// <summary>
    /// Adds this person's research to the aboard-only project. The game's AddProgress takes points, not research speed, so the points are worked out as the game's
    /// ResearchPerformed does (FieldResearchRule.Points), and the person's record of research done is kept as it would be at a bench.
    /// </summary>
    public static void Perform(ResearchProjectDef project, float speedTicks, Pawn researcher)
    {
        float costFactor = researcher.Faction != null ? project.CostFactor(researcher.Faction.def.techLevel) : 1f;
        float points = FieldResearchRule.Points(speedTicks, Find.Storyteller.difficulty.researchSpeedFactor, costFactor, DebugSettings.fastResearch);
        if (points <= 0f) return;
        researcher.records.AddTo(RecordDefOf.ResearchPointsResearched, points);
        Find.ResearchManager.AddProgress(project, points, researcher);
    }
}
