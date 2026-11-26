using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace Phobos.Framework.Health;

/// <summary>What Search and Rescue should take a job of ours for. The names match its own roles, and are matched to them by name.</summary>
public enum PhobosPatientJobRole
{
    /// <summary>Carrying a patient somewhere: Search and Rescue leaves the carrier holding them and schedules round them.</summary>
    Transport,
    /// <summary>Treating a patient where they lie.</summary>
    Treatment,
}

/// <summary>
/// On a JobDef: tells Search and Rescue what the job does with a patient (Framework 0.328.1). Search and Rescue keeps a list of other
/// mods' patient jobs and makes a carrier drop anyone it has marked (its Rescue all, its rescue and treat orders) unless the carrier's job
/// is on that list as a transport. The job's patient is the first pawn among its targets, as Search and Rescue reads it, so a job whose
/// first target is a vehicle cannot use this.
/// </summary>
public sealed class PhobosSearchAndRescueRole : DefModExtension
{
    public PhobosPatientJobRole role = PhobosPatientJobRole.Transport;
}

/// <summary>
/// Adds our patient jobs to Search and Rescue's list at start-up (Framework 0.328.1). Seen in play, 26 Nov 2026: after Rescue all, a
/// colonist carrying a stranger to a helicopter's litters had them dropped as soon as they were lifted, and another colonist was sent for
/// them, round and round. Search and Rescue has no public way in, so its list is reached by name: its own Initialize first (it runs once
/// and is safe to call early), then one row per job. If any name has gone, nothing is changed, a warning says so, and
/// <see cref="Registered"/> is false, for our work to stand aside from its marked patients instead (<see cref="WouldDropCarry"/>).
/// </summary>
[StaticConstructorOnStartup]
public static class SearchAndRescueRoles
{
    const string RegistryTypeName = "SearchAndRescue.CompatibilityRegistry";
    const string RoleTypeName = "SearchAndRescue.PatientJobRole";
    /// <summary>Search and Rescue's marks on a patient that make it drop a carry it does not know: its treat, capture and rescue orders.</summary>
    static readonly string[] StageMarkNames = { "SAR_Treat", "SAR_Capture", "SAR_Rescue" };

    static readonly List<DesignationDef> stageMarks = new();

    /// <summary>False only when Search and Rescue is active and our jobs could not be added to its list.</summary>
    public static bool Registered { get; private set; } = true;

    /// <summary>How many of our jobs were added, for the debug line.</summary>
    public static int Count { get; private set; }

    static SearchAndRescueRoles()
    {
        if (!OptionalMods.IsActive(OptionalMods.SearchAndRescue)) return;
        foreach (string name in StageMarkNames)
        {
            if (DefDatabase<DesignationDef>.GetNamedSilentFail(name) is { } mark) stageMarks.Add(mark);
        }
        var asking = new List<(JobDef def, PhobosPatientJobRole role)>();
        foreach (JobDef def in DefDatabase<JobDef>.AllDefsListForReading)
        {
            if (def.GetModExtension<PhobosSearchAndRescueRole>() is { } ext) asking.Add((def, ext.role));
        }
        if (asking.Count == 0) return;
        string? fault;
        try { fault = Register(asking); }
        catch (Exception e) { fault = e.GetType().Name + ": " + e.Message; }
        if (fault == null)
        {
            Count = asking.Count;
            FrameworkMod.Logger.Debug($"Search and Rescue: {Count} of our patient jobs added to its list.");
            return;
        }
        Registered = false;
        FrameworkMod.Logger.Warn("Could not add our patient jobs to Search and Rescue's list (" + fault + "). Our litter carries stand aside "
            + "from people Search and Rescue has marked, so neither mod takes them from the other.");
    }

    /// <summary>Adds each job's role to Search and Rescue's table, keeping any role it already had. The reason it could not, or null.</summary>
    static string? Register(List<(JobDef def, PhobosPatientJobRole role)> asking)
    {
        Type? registry = AccessTools.TypeByName(RegistryTypeName);
        Type? roleType = AccessTools.TypeByName(RoleTypeName);
        if (registry == null || roleType == null || !roleType.IsEnum) return "its list's types are not where they were";
        MethodInfo? initialize = AccessTools.Method(registry, "Initialize");
        FieldInfo? table = AccessTools.Field(registry, "JobRoles");
        if (initialize == null || table == null) return "its list is not where it was";
        initialize.Invoke(null, null);
        if (table.GetValue(null) is not IDictionary roles) return "its list is not a table";
        foreach ((JobDef def, PhobosPatientJobRole role) in asking)
        {
            if (!Enum.IsDefined(roleType, role.ToString())) return "it has no role named " + role;
            long value = Convert.ToInt64(Enum.Parse(roleType, role.ToString()));
            if (roles.Contains(def)) value |= Convert.ToInt64(roles[def]);
            roles[def] = Enum.ToObject(roleType, value);
        }
        return null;
    }

    /// <summary>
    /// Whether Search and Rescue would take this patient out of a carrier's arms: it is active, our jobs are not on its list, and it has
    /// marked them. Our work skips such a patient, leaving them to Search and Rescue.
    /// </summary>
    public static bool WouldDropCarry(Pawn patient)
    {
        if (Registered || stageMarks.Count == 0 || patient.MapHeld is not { } map) return false;
        foreach (DesignationDef mark in stageMarks)
        {
            if (map.designationManager.DesignationOn(patient, mark) != null) return true;
        }
        return false;
    }
}
