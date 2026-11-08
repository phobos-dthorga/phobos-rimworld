using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Phobos.Framework;
using Phobos.Framework.Rules;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Phobos.VisitSettlementsPatches;

/// <summary>
/// Stops Visit Settlements' settlement events from throwing on every tick (docs/development/missile-patches.md, "Visit Settlements'
/// food drops throw every tick", 8 Nov 2026). Its world component counts days and, when the count is reached, drops food for each
/// friendly settlement map it keeps, then resets the count. The food sum reads every friendly pawn's food need, and a pawn that does
/// not eat (a mechanoid, a robot, an android) has none, so the sum throws; the reset after it never runs, and the component tries
/// again, and throws again, on the next tick: 6,846 errors in six minutes on the owner's colony, enough to hit the game's
/// 10,000-message log limit and silence every mod's logging for the rest of the session.
///
/// Two patches, both found by name or shape, with no reference to Visit Settlements' assembly:
/// - A prefix on the food sum's per-pawn lambda: a pawn with no food need needs no food. It skips the original only for such a
///   pawn, the one case in which the original throws (recorded reason: there is no extension point inside a lambda); every other
///   pawn is counted by Visit Settlements' own code.
/// - A finalizer on the component's world tick: if anything still throws there, the day counters are reset as a success would
///   have reset them, so the events wait their usual days instead of retrying every tick, and one warning a day says so. The
///   exception is swallowed (the finalizer returns null): left to escape, the game would log it again on the next tick, which is
///   the fault itself. Search and Rescue's finalizer rethrows and hides an origin; this one logs the original exception in full.
/// </summary>
public static class SettlementEventsGuard
{
    const string ComponentType = "VS_WorldComponent_SettlementEvents";
    const string FoodRoutine = "DropFoodPodsForSettlements";
    const string DayCounter = "tickCounter";
    const string RaidCounter = "raidTickCounter";

    static Type? component;
    static MethodInfo? perPawnFood;
    static MethodInfo? worldTick;
    static AccessTools.FieldRef<object, int>? dayCounter;
    static AccessTools.FieldRef<object, int>? raidCounter;
    static Phobos.Logging.Throttle faultWarning;

    public static bool FoodReady => perPawnFood != null;
    public static bool GuardReady => worldTick != null && dayCounter != null && raidCounter != null;

    /// <summary>Finds Visit Settlements' component, its per-pawn food lambda and its counters. Returns why something is missing, or null.</summary>
    public static string? Prepare()
    {
        component = OptionalMods.TypeNamed(ComponentType);
        if (component == null) return "Visit Settlements' settlement events are not where they were";
        worldTick = AccessTools.Method(component, nameof(WorldComponent.WorldComponentTick), Type.EmptyTypes);
        dayCounter = Counter(DayCounter);
        raidCounter = Counter(RaidCounter);
        perPawnFood = PerPawnFoodLambda(component);
        if (perPawnFood == null) return "Visit Settlements' food sum has changed";
        return GuardReady ? null : "Visit Settlements' day counters have changed";
    }

    static AccessTools.FieldRef<object, int>? Counter(string name) =>
        AccessTools.Field(component, name)?.FieldType == typeof(int) ? AccessTools.FieldRefAccess<int>(component, name) : null;

    /// <summary>
    /// The compiler-made lambda <c>pawn => pawn.needs.food...</c> inside the food routine, found by shape rather than by its numbered
    /// name: declared in a nested type, named after the routine, one Pawn parameter, returning float. Exactly one must match.
    /// </summary>
    static MethodInfo? PerPawnFoodLambda(Type owner)
    {
        const BindingFlags all = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        MethodInfo[] matches = owner.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
            .SelectMany(nested => nested.GetMethods(all))
            .Where(m => m.Name.Contains("<" + FoodRoutine + ">") && m.ReturnType == typeof(float)
                        && m.GetParameters() is { Length: 1 } parameters && parameters[0].ParameterType == typeof(Pawn))
            .ToArray();
        return matches.Length == 1 ? matches[0] : null;
    }

    public static void Apply(Harmony harmony)
    {
        if (perPawnFood != null)
        {
            harmony.Patch(perPawnFood, prefix: new HarmonyMethod(AccessTools.Method(typeof(SettlementEventsGuard), nameof(PerPawnFoodPrefix))));
        }
        if (GuardReady)
        {
            harmony.Patch(worldTick, finalizer: new HarmonyMethod(AccessTools.Method(typeof(SettlementEventsGuard), nameof(TickFinalizer))));
        }
    }

    /// <summary>A pawn with no food need (it does not eat) needs no food; every other pawn goes to Visit Settlements' own sum.</summary>
    public static bool PerPawnFoodPrefix(Pawn __0, ref float __result)
    {
        if (__0?.needs?.food != null) return true;
        __result = 0f;
        return false;
    }

    /// <summary>Resets the day counters when the world tick threw, so it does not throw again on the next tick; warns once a day.</summary>
    public static Exception? TickFinalizer(Exception? __exception)
    {
        if (__exception == null) return null;
        try
        {
            object? instance = Find.World?.components?.FirstOrDefault(c => c != null && c.GetType() == component);
            if (instance != null)
            {
                dayCounter!(instance) = 0;
                raidCounter!(instance) = 0;
            }
            if (VisitSettlementsPatchesMod.Logger.Every(ref faultWarning, GenDate.TicksPerDay))
            {
                VisitSettlementsPatchesMod.Logger.Warn("Visit Settlements' settlement events failed; they will try again in their "
                    + "usual number of days instead of on every tick (this is said at most once an in-game day). The fault: " + __exception);
            }
        }
        catch (Exception ours)
        {
            VisitSettlementsPatchesMod.Logger.ErrorOnce("could not reset Visit Settlements' settlement events after a fault: " + ours);
        }
        return null;
    }
}
