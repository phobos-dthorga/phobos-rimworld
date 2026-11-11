using System.Linq;
using System.Reflection;
using HarmonyLib;
using Phobos.Framework;
using RimWorld;
using Verse;

namespace Phobos.PrisonLaborPatches;

/// <summary>
/// D'thorga Patches' Prison Labor part. <b>Strictly temporary</b> (owner, 10 Nov 2026): it holds one guard against a crash in Prison Labor's first 1.7 build, reported to its author,
/// and comes out once the author's fix ships. It stands down by itself when Prison Labor's code no longer has the fault, and says so on the load line.
/// How to take it out: docs/development/crash-2026-11-09-stack-overflow.md. No settings, no save data, no shared code on purpose, so removal is deleting this part.
/// </summary>
public class PrisonLaborPatchesMod : Mod
{
    /// <summary>The Harmony ID is the packageId, shared by every part; never change it once the mod is published.</summary>
    public const string PackageId = "PhobosDthorga.Patches";
    public const string ModVersion = "0.16.9";
    public const string LogPrefix = "[D'thorga Patches]";
    const string Feature = "temporary guard against Prison Labor's food policy loop";

    public PrisonLaborPatchesMod(ModContentPack content) : base(content)
    {
        PrisonLaborFoodGuard.Apply(new Harmony(PackageId));
        LongEventHandler.ExecuteWhenFinished(LogLoaded);
    }

    static void LogLoaded()
    {
        var report = new LoadReport(LogPrefix, ModVersion, "Prison Labor");
        if (PrisonLaborFoodGuard.Active) report.On(Feature);
        else report.Off(Feature, PrisonLaborFoodGuard.StoodDownBecause);
        report.Log();
    }
}

/// <summary>
/// Prison Labor 1.7's GetFoodRestrictionPatch.Postfix calls GetCurrentRespectedRestriction, the method it patches. For a prisoner the game answers a null asker with null, so the
/// postfix runs again with the same arguments until the stack overflows and the game closes. This prefix on that postfix lets it run once: called from inside itself it hands back
/// what it was given, so the outer call ends with the game's own answer, no food policy. Nothing else about Prison Labor changes.
/// A prefix that skips the original, for the recorded reason above: the nested run is the fault.
/// </summary>
public static class PrisonLaborFoodGuard
{
    const string PatchType = "PrisonLabor.HarmonyPatches.Patches_Food.GetFoodRestrictionPatch";
    const string PatchMethod = "Postfix";

    [System.ThreadStatic]
    static bool inside;

    public static bool Active { get; private set; }
    public static string StoodDownBecause { get; private set; } = "not applied";

    /// <summary>Whether this method still calls the one it patches, which is the fault. Read from its IL, so the guard stands down when the author's fix ships.</summary>
    static bool CallsPatchedMethod(MethodBase method) =>
        PatchProcessor.ReadMethodBody(method).Any(step => step.Value is MethodBase called && called.Name == nameof(Pawn_FoodRestrictionTracker.GetCurrentRespectedRestriction));

    public static void Apply(Harmony harmony)
    {
        MethodBase? target = AccessTools.Method(AccessTools.TypeByName(PatchType), PatchMethod);
        if (target == null)
        {
            StoodDownBecause = "Prison Labor no longer has that patch, so this guard can be removed";
            return;
        }
        if (!CallsPatchedMethod(target))
        {
            StoodDownBecause = "Prison Labor no longer calls itself there, so this guard can be removed";
            return;
        }
        harmony.Patch(target, prefix: new HarmonyMethod(typeof(PrisonLaborFoodGuard), nameof(Prefix)), finalizer: new HarmonyMethod(typeof(PrisonLaborFoodGuard), nameof(Finalizer)));
        Active = true;
    }

    // The target's first parameter is itself named __result, which to Harmony means the return value, so it is read by position.
    public static bool Prefix(ref FoodPolicy __result, FoodPolicy __0, out bool __state)
    {
        __state = !inside;
        if (inside)
        {
            __result = __0;
            return false;
        }
        inside = true;
        return true;
    }

    public static void Finalizer(bool __state)
    {
        if (__state) inside = false;
    }
}
