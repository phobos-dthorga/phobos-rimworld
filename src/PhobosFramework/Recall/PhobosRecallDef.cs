using System;
using System.Reflection;
using Verse;

namespace Phobos.Framework.Recall;

/// <summary>
/// Names a kind of drone: a pawn sent out from a home that holds its material, and which runs on the game's mechanoid power cell
/// (Framework 0.240.0; docs/development/drone-recall-design.md; owner, 23 Nov 2026: drone bays starved and drone wrecks lay everywhere).
/// Every pawn whose Def carries the named comp is then called home while its power still covers the walk, and its wreck, if it dies
/// anyway, is carried home by a hauler and taken back into the home's store. In XML only, read by name, as <c>PhobosTopUpDef</c> is: the
/// comp, where on it to find the home, the comp's own method that sends the drone home, and the job it does on the way. A Def for another
/// mod's comp belongs in that mod's IfModActive folder. A name that no longer fits makes the Def stand down with one warning.
/// </summary>
public class PhobosRecallDef : Def
{
    /// <summary>The drone's comp, by its class's full name ("Fortified.CompDrone"); a subclass counts.</summary>
    public string compClass = "";
    /// <summary>On the comp: the drone's home, a Thing ("PlatformOwner"). A home that is a building holds the store a wreck is taken to.</summary>
    public string homeMember = "";
    /// <summary>The comp's own method that sends the drone home ("ReturnToPlatform"); its first true-or-false parameter, if any, is given
    /// <see cref="interrupt"/> and every other takes its default.</summary>
    public string recallMethod = "";
    /// <summary>Whether the call home stops what the drone is doing (true, as the drone's own call when its power runs low).</summary>
    public bool interrupt = true;
    /// <summary>On the comp: the job a drone does on its way home, a JobDef ("Props.returnToDraftPlatformJob"), so a drone already going
    /// is left to go.</summary>
    public string? returningJobMember;
    /// <summary>How long a drone waits at its home before it is taken in, in ticks (Fortified's platforms: 180). Authored per kind.</summary>
    public int dockTicks;
    /// <summary>Drones of this kind are called home in time.</summary>
    public bool recall = true;
    /// <summary>Wrecks of this kind are carried home and taken back into its store.</summary>
    public bool reclaimWrecks = true;

    [Unsaved] Type? type;
    [Unsaved] TopUp.MemberPath? home;
    [Unsaved] TopUp.MemberPath? returning;
    [Unsaved] MethodInfo? method;
    [Unsaved] object?[]? arguments;
    [Unsaved] bool resolved;

    /// <summary>Why this Def stands down, in words, or null while it works.</summary>
    [Unsaved] public string? problem;

    /// <summary>The comp's type once found, or null.</summary>
    public Type? CompType
    {
        get
        {
            Resolve();
            return problem == null ? type : null;
        }
    }

    public override System.Collections.Generic.IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (compClass.NullOrEmpty()) yield return "compClass is empty";
        if (homeMember.NullOrEmpty()) yield return "homeMember is empty";
        if (recall && recallMethod.NullOrEmpty()) yield return "recallMethod is empty";
        if (dockTicks < 0) yield return "dockTicks must be 0 or more";
    }

    void Resolve()
    {
        if (resolved) return;
        resolved = true;
        type = OptionalMods.TypeNamed(compClass);
        if (type == null) { problem = "no class named " + compClass; return; }
        if (!typeof(ThingComp).IsAssignableFrom(type)) { problem = compClass + " is not a comp"; return; }
        home = TopUp.MemberPath.On(type, homeMember, out problem);
        if (home == null) return;
        if (!typeof(Thing).IsAssignableFrom(home.ValueType)) { problem = homeMember + " is not a Thing"; return; }
        if (!returningJobMember.NullOrEmpty())
        {
            returning = TopUp.MemberPath.On(type, returningJobMember!, out problem);
            if (returning == null) return;
            if (!typeof(Verse.JobDef).IsAssignableFrom(returning.ValueType)) { problem = returningJobMember + " is not a JobDef"; return; }
        }
        if (!recall) return;
        foreach (MethodInfo candidate in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (candidate.Name != recallMethod || candidate.IsGenericMethodDefinition) continue;
            object?[]? args = ArgumentsFor(candidate);
            if (args == null) continue;
            method = candidate;
            arguments = args;
            return;
        }
        problem = compClass + " has no method " + recallMethod + " that can be called with its defaults";
    }

    /// <summary>The arguments for a call: the first true-or-false parameter is <see cref="interrupt"/>, every other its default. Null when a
    /// parameter has no default.</summary>
    object?[]? ArgumentsFor(MethodInfo candidate)
    {
        ParameterInfo[] parameters = candidate.GetParameters();
        var args = new object?[parameters.Length];
        bool interruptGiven = false;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (!interruptGiven && parameters[i].ParameterType == typeof(bool))
            {
                args[i] = interrupt;
                interruptGiven = true;
            }
            else if (parameters[i].HasDefaultValue) args[i] = parameters[i].DefaultValue;
            else return null;
        }
        return args;
    }

    /// <summary>The drone's home, or null.</summary>
    public Thing? HomeOf(ThingComp comp) => Read(comp, home) as Thing;

    /// <summary>The job a drone of this kind does on its way home, or null where the Def names none.</summary>
    public Verse.JobDef? ReturningJob(ThingComp comp) => Read(comp, returning) as Verse.JobDef;

    /// <summary>Sends the drone home through its comp's own method. False when it could not be called.</summary>
    public bool CallHome(ThingComp comp)
    {
        Resolve();
        if (problem != null || method == null) return false;
        try
        {
            method.Invoke(comp, (object?[])arguments!.Clone());
            return true;
        }
        catch (Exception error)
        {
            problem = "calling " + compClass + "." + recallMethod + " failed: " + error.GetBaseException().Message;
            Recalls.ReportProblem(this);
            return false;
        }
    }

    object? Read(ThingComp comp, TopUp.MemberPath? reader)
    {
        Resolve();
        if (problem != null || reader == null) return null;
        try
        {
            return reader.Read(comp);
        }
        catch (Exception error)
        {
            problem = "reading " + compClass + " failed: " + error.GetBaseException().Message;
            Recalls.ReportProblem(this);
            return null;
        }
    }
}
