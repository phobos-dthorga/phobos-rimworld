using System.Reflection;
using HarmonyLib;
using Phobos.VehicleSystems;
using Vehicles;
using Verse;

namespace Phobos.VehiclePatches;

/// <summary>
/// The Harrier's dodge chance follows its flight controls and warning receiver, and a salvo of chaff and flares adds to it for a few seconds
/// (Patches 0.21.0 with Framework 0.119.0; owner, 17 Nov 2026: avionics A12 and the Harrier's salvo button). The Harrier asks its static
/// Evasion.ChanceFor(target, launcher) on both its own and its Combat Extended paths (read from its assembly of 16 Nov 2026), so one postfix
/// covers both. A sound jet with a working receiver dodges exactly as its author set; D'thorga Framework's air-combat service does the sums.
/// A postfix, so the author's own figure is always worked out first and nothing is skipped. Reached by name: this assembly does not
/// reference the Harrier's.
/// </summary>
[HarmonyPatch]
public static class HarrierEvasionPatch
{
    static bool Prepare() => TargetMethod() != null;

    static MethodBase? TargetMethod() => AccessTools.Method("AV8BHarrier.Evasion:ChanceFor");

    public static void Postfix(Thing target, ref float __result)
    {
        if (__result <= 0f || target is not VehiclePawn vehicle) return;
        __result = AirCombat.Evasion(vehicle, __result);
    }
}

/// <summary>
/// Which vehicles are Harriers, for the salvo button D'thorga Framework shows on a map (only where a dodge chance exists to add to: the
/// Harrier's). The Harrier marks its own Def with its extension; Harrier.Props(thing) returns it, or null for any other vehicle. Read by name
/// once.
/// </summary>
public static class HarrierSalvo
{
    static MethodInfo? props;

    public static void Register()
    {
        props = AccessTools.Method("AV8BHarrier.Harrier:Props");
        if (props == null) return;
        AirCombat.RegisterMapSalvo(IsHarrier);
    }

    static bool IsHarrier(VehiclePawn vehicle) => props != null && props.Invoke(null, new object[] { vehicle }) != null;
}
