using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld.Planet;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehiclePatches;

/// <summary>
/// Lets RimPacts and any other mod patch Vehicle Framework's LaunchProtocol.GetArrivalOptions together.
///
/// RimPacts patches that method with a postfix in a generic class, RptVfArrivalFilter&lt;ArrivalOption&gt;, because it has
/// no compile-time reference to Vehicle Framework. Harmony stores a patch as a module ID and a method token, and a token
/// can't record generic arguments, so the next patch to rebuild the method gets RimPacts' postfix back as the open generic
/// definition and throws InvalidProgramException ("token resolves to an open generic type"). The Harrier's flight framework
/// is that next patch, and it uses PatchAll, so more than 50 later patches never load, boarding and refuelling among them.
/// Read in Harmony 3.0.0-rc.3 and RimPacts' 1.7 assemblies, 2 Nov 2026.
///
/// The cure is an ordinary postfix with the same signature that calls RimPacts' closed method through a delegate, so
/// RimPacts' landing rule still runs and Harmony can rebuild the method as often as it needs. The real fix belongs in
/// RimPacts; once its postfix is no longer in a generic class, this finds nothing to do.
/// </summary>
public static class RimPactsArrivalFilter
{
    private const string LogPrefix = "[D'thorga Vehicle Patches] ";
    private const string RimPactsId = "wowgag.RimPacts";
    private const string FilterTypeName = "RimPacts.RptVfArrivalFilter`1";

    private static Harmony? ourHarmony;
    private static Assembly? rimPacts;
    private static Func<IEnumerable<ArrivalOption>, GlobalTargetInfo, IEnumerable<ArrivalOption>>? filter;

    /// <summary>
    /// Waits for RimPacts to install its vehicle patches. A postfix on its Install method is the one point that is always
    /// after its patch and, in either load order, before anything else can trip over it.
    /// </summary>
    public static void Watch(Harmony harmony)
    {
        if (!ModsConfig.IsActive(RimPactsId)) return;
        var bridge = AccessTools.TypeByName("RimPacts.RptVehicleBridge");
        var install = bridge == null ? null : AccessTools.Method(bridge, "Install", new[] { typeof(Harmony) });
        if (install == null)
        {
            Log.Warning(LogPrefix + "RimPacts' RptVehicleBridge.Install is gone, so its vehicle patch is left as it is.");
            return;
        }
        ourHarmony = harmony;
        rimPacts = install.DeclaringType.Assembly;
        harmony.Patch(install, postfix: new HarmonyMethod(AccessTools.Method(typeof(RimPactsArrivalFilter), nameof(AfterInstall))));
    }

    /// <summary>Must never stop RimPacts starting up, so any failure here is only logged.</summary>
    public static void AfterInstall()
    {
        try
        {
            ReplaceGenericPostfix();
        }
        catch (Exception error)
        {
            Log.Error(LogPrefix + "Could not replace RimPacts' generic postfix, so the Harrier's flight framework may fail to load. " + error);
        }
    }

    /// <summary>The replacement for RimPacts' postfix: the same pass-through signature, in a plain class.</summary>
    public static IEnumerable<ArrivalOption> Postfix(IEnumerable<ArrivalOption> values, GlobalTargetInfo target) =>
        filter == null ? values : filter(values, target);

    private static void ReplaceGenericPostfix()
    {
        var original = AccessTools.Method(typeof(LaunchProtocol), nameof(LaunchProtocol.GetArrivalOptions), new[] { typeof(GlobalTargetInfo) });
        var patches = original == null ? null : Harmony.GetPatchInfo(original);
        if (patches == null || !patches.Postfixes.Any(patch => patch.owner == RimPactsId && patch.PatchMethod?.DeclaringType?.IsGenericType == true)) return;

        // Everything that can fail on a RimPacts update comes before the first change, so a failure leaves its patch in place.
        var method = rimPacts?.GetType(FilterTypeName)?.MakeGenericType(typeof(ArrivalOption)).GetMethod("Postfix", BindingFlags.Public | BindingFlags.Static);
        if (method == null)
        {
            Log.Warning(LogPrefix + FilterTypeName + " is gone, so RimPacts' vehicle patch is left as it is.");
            return;
        }
        filter = (Func<IEnumerable<ArrivalOption>, GlobalTargetInfo, IEnumerable<ArrivalOption>>)
            Delegate.CreateDelegate(typeof(Func<IEnumerable<ArrivalOption>, GlobalTargetInfo, IEnumerable<ArrivalOption>>), method);

        ourHarmony!.Unpatch(original, HarmonyPatchType.Postfix, RimPactsId);
        ourHarmony.Patch(original, postfix: new HarmonyMethod(AccessTools.Method(typeof(RimPactsArrivalFilter), nameof(Postfix))));
        Log.Message(LogPrefix + "RimPacts' landing rule now runs through a plain postfix, so other mods can patch GetArrivalOptions as well.");
    }
}
