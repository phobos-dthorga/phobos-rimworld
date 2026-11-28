using System;
using System.Collections.Generic;
using Verse;

namespace Phobos.Framework.Logging;

/// <summary>
/// Reports a fault in a function another mod registered with the framework (a button provider, an inspect line, a care choice's test),
/// once per function a game run (Framework 0.381.1). The framework calls such functions inside its own comp on every pawn, so one
/// that fails must not take a person's whole command bar or inspect pane with it: the caller catches the fault, reports it here, and
/// carries on with the rest. The line names the function and its assembly, so the fault reaches the mod that owns it.
/// </summary>
public static class ProviderFaults
{
    static readonly HashSet<Delegate> reported = new();

    /// <summary>Logs the fault the first time this function fails; later faults from it are silent, since it is called every frame.</summary>
    public static void Report(Delegate provider, string what, Pawn pawn, Exception exception)
    {
        if (!reported.Add(provider)) return;
        FrameworkMod.Logger.Error(
            Describe(provider) + " failed while giving " + what + " for " + pawn.LabelShort + " (" + pawn.def.defName
            + "). The rest still show; further faults from it are not logged again until the game restarts.", exception);
    }

    /// <summary>Whether this function has failed and been reported, for tests and dev actions.</summary>
    public static bool HasFailed(Delegate provider) => reported.Contains(provider);

    static string Describe(Delegate provider)
    {
        Type? owner = provider.Method.DeclaringType;
        return (owner?.FullName ?? "?") + "." + provider.Method.Name + " (" + (owner?.Assembly.GetName().Name ?? "?") + ")";
    }
}
