using System;
using System.Collections.Generic;

namespace Phobos.Framework.Rules;

/// <summary>What to do with one research project when an older save first meets it (docs/development/research-tree.md).</summary>
public enum ResearchGrantDecision
{
    /// <summary>Considered on an earlier load: nothing to do.</summary>
    LeaveAlone,
    /// <summary>Note it as considered, and grant nothing.</summary>
    RecordOnly,
    /// <summary>Note it as considered, and finish it: the colony already had what it replaces.</summary>
    Grant,
}

/// <summary>The one-time grant for existing colonies: a project is free only if everything it replaces was already researched.</summary>
public static class ResearchGrantRule
{
    /// <summary>A project that replaces nothing has nothing to grant from. <paramref name="replacedCount"/> counts only entries whose mod is active.</summary>
    public static ResearchGrantDecision Decide(bool alreadyHandled, int replacedCount, int unfinishedCount)
    {
        if (alreadyHandled) return ResearchGrantDecision.LeaveAlone;
        if (replacedCount <= 0 || unfinishedCount > 0) return ResearchGrantDecision.RecordOnly;
        return ResearchGrantDecision.Grant;
    }
}

/// <summary>Why a person in a field laboratory's seat is or is not doing research now. Checked in this order, so the first reason a player can act on is the one shown.</summary>
public enum FieldResearcherState
{
    /// <summary>At work.</summary>
    Working,
    /// <summary>Will never do research: the work is disabled for them.</summary>
    Incapable,
    /// <summary>Below the Intellectual skill the laboratory asks for.</summary>
    Unskilled,
    /// <summary>Downed, asleep or not in their right mind.</summary>
    Unfit,
    /// <summary>Their schedule says it is not time for work.</summary>
    OffShift,
}

/// <summary>Whether a field laboratory can take on a project, and why not.</summary>
public enum FieldLabVerdict
{
    Allowed,
    /// <summary>No laboratory is fitted at all.</summary>
    NoLab,
    /// <summary>The project needs a bench this laboratory does not stand in for.</summary>
    NeedsBetterLab,
    /// <summary>The project needs a particular bench of its own, which no laboratory of this kind stands in for.</summary>
    NeedsOtherBench,
}

/// <summary>
/// Research done away from a bench, in a laboratory fitted to a vehicle or the like (docs/development/vehicle-seabed-crawler.md), free of the game.
/// The rate follows the game's own bench research: a person's research speed times the laboratory's factor, per tick, with Intellectual
/// experience at the bench's 0.1 per tick. Grade 1 is a basic laboratory, which takes what any bench can; grade 2 stands in for a hi-tech bench and its
/// facilities as well. The grades, and the half rate under way, are authored gameplay choices.
/// </summary>
public static class FieldResearchRule
{
    /// <summary>The Intellectual experience a research bench gives per tick (JobDriver_Research).</summary>
    public const float ExperiencePerTick = 0.1f;
    /// <summary>A laboratory of this grade or better takes projects that need no particular bench.</summary>
    public const int BasicGrade = 1;
    /// <summary>A laboratory of this grade or better stands in for a hi-tech bench and its facilities.</summary>
    public const int AdvancedGrade = 2;

    /// <summary>Whether this person is doing research now. <paramref name="workTime"/> is whether their schedule allows work.</summary>
    public static FieldResearcherState Researcher(bool capable, int intellectual, int minimumIntellectual, bool fit, bool workTime)
    {
        if (!capable) return FieldResearcherState.Incapable;
        if (intellectual < minimumIntellectual) return FieldResearcherState.Unskilled;
        if (!fit) return FieldResearcherState.Unfit;
        return workTime ? FieldResearcherState.Working : FieldResearcherState.OffShift;
    }

    /// <summary>The research speed this person adds per tick: nothing without a laboratory, and the share for being under way while it moves.</summary>
    public static float Rate(float researchSpeed, float labFactor, bool underWay, float underWayFactor)
    {
        if (researchSpeed <= 0f || labFactor <= 0f) return 0f;
        return researchSpeed * labFactor * (underWay ? Math.Clamp(underWayFactor, 0f, 1f) : 1f);
    }

    /// <summary>The Intellectual experience over this many ticks of research. Learning keeps its full rate under way: the crew is busy, but still learning.</summary>
    public static float Experience(int ticks) => ticks <= 0 ? 0f : ExperiencePerTick * ticks;

    /// <summary>Whether a laboratory of this grade can take on a project with these needs.</summary>
    public static FieldLabVerdict Lab(int grade, bool needsAnyBench, bool needsHiTechBench, bool needsFacilities)
    {
        if (grade < BasicGrade) return FieldLabVerdict.NoLab;
        if (needsAnyBench && !needsHiTechBench) return FieldLabVerdict.NeedsOtherBench;
        if ((needsHiTechBench || needsFacilities) && grade < AdvancedGrade) return FieldLabVerdict.NeedsBetterLab;
        return FieldLabVerdict.Allowed;
    }
}

/// <summary>Which research a gated thing is still waiting for.</summary>
public static class ResearchGateRule
{
    /// <summary>The first needed project not yet finished, or null when nothing is missing. Empty entries are ignored.</summary>
    public static T? FirstMissing<T>(IEnumerable<T>? needed, Func<T, bool> isFinished) where T : class
    {
        if (needed == null) return null;
        foreach (T? project in needed)
            if (project != null && !isFinished(project)) return project;
        return null;
    }
}
