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
