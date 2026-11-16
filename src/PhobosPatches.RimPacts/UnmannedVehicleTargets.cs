using System;
using System.Reflection;
using HarmonyLib;
using Phobos.Framework;
using Phobos.RimPactsPatches.Rules;
using Verse;

namespace Phobos.RimPactsPatches;

/// <summary>
/// TEMPORARY (docs/development/temporary-patches.md, row 17): RimPacts' assaults no longer attack a vehicle with nobody aboard
/// (owner, 16 Nov 2026, from the save PG8: 11 of an ally's 19 soldiers spent hours kicking one empty Highwayman).
///
/// What RimPacts does (read in its build of 16 Nov 2026): BuildAssaultTargets lists every standing pawn of the defending faction, and
/// a vehicle is a pawn. RptAssaultTarget.IsLive calls a target live while it is on the map and, for a pawn, neither dead nor downed. A
/// vehicle never goes down, so an empty one stays a live "pawn" target, the kind its assault prefers, until it is destroyed. The
/// game's order for such a target is to trash it: melee, or sometimes fire (TrashUtility.TrashJob), since Vehicle Framework rightly
/// says an empty vehicle is no threat and nobody shoots at it. Vehicle Framework destroys a vehicle only when all its parts together
/// fall under 1 %, which by hand takes many hours. The same count also holds back RimPacts' switch to its general assault, which waits
/// for no live pawn target to remain.
///
/// What this changes: a postfix on RptAssaultTarget.IsLive answers "not live" for a vehicle RimPacts' own RptVehicleBridge.IsUnmanned
/// calls unmanned (nobody aboard and not autonomous), the test its count of fighters already uses. Soldiers holding such a target
/// are given another at RimPacts' next look (every 300 ticks), and the vehicle is left standing. A crewed vehicle is untouched. It
/// only ever turns a "live" into "not live", so once RimPacts leaves unmanned vehicles out itself this does nothing and can go.
/// RimPacts is reached by name; if either method has changed, the patch stays off and the load line says so.
/// </summary>
public static class UnmannedVehicleTargets
{
    const string TargetType = "RimPacts.RptAssaultTarget";
    const string BridgeType = "RimPacts.RptVehicleBridge";

    static Func<Pawn, bool>? isUnmanned;
    static bool faulted;

    public static bool Applied { get; private set; }

    public static void Apply(Harmony harmony)
    {
        MethodInfo? isLive = OptionalMods.TypeNamed(TargetType)?.GetMethod("IsLive", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static,
            null, new[] { typeof(Thing), typeof(Map) }, null);
        MethodInfo? unmanned = OptionalMods.TypeNamed(BridgeType)?.GetMethod("IsUnmanned", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static,
            null, new[] { typeof(Pawn) }, null);
        if (isLive == null || isLive.ReturnType != typeof(bool) || unmanned == null || unmanned.ReturnType != typeof(bool))
        {
            RimPactsPatchesMod.Logger.Warn("RimPacts' assault targets have changed, so its allies may still attack empty vehicles.");
            return;
        }
        try
        {
            isUnmanned = (Func<Pawn, bool>)Delegate.CreateDelegate(typeof(Func<Pawn, bool>), unmanned);
            harmony.Patch(isLive, postfix: new HarmonyMethod(typeof(UnmannedVehicleTargets), nameof(Postfix)));
            Applied = true;
        }
        catch (Exception error)
        {
            RimPactsPatchesMod.Logger.Warn("could not patch RimPacts' assault targets: " + error.Message);
        }
    }

    /// <summary>The parameter name matches RimPacts' method (t), which Harmony binds by name. Only ever lowers the answer.</summary>
    public static void Postfix(ref bool __result, Thing t)
    {
        if (!__result || faulted || t is not Pawn pawn) return;
        try
        {
            bool unmanned = isUnmanned!(pawn);
            if (AssaultTargetRule.StillLive(__result, unmanned)) return;
            __result = false;
            if (RimPactsPatchesMod.Logger.DebugEnabled)
                RimPactsPatchesMod.Logger.Debug("assault target left out, nobody aboard: " + pawn.LabelShort + " at " + pawn.Position);
        }
        catch (Exception error)
        {
            faulted = true;
            RimPactsPatchesMod.Logger.Error("could not check a RimPacts assault target, so its own answer stands until the game restarts: " + error.Message);
        }
    }
}
