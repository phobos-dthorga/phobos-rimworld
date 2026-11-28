using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Phobos.VehicleSystems;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.ProwlerPatches;

/// <summary>
/// The Prowler's jamming fields, told to D'thorga Framework's jamming service (Patches 0.34.0 with Framework 0.225.0; owner, 22 Nov 2026:
/// "fields jam our systems"; docs/development/ea6b-prowler.md, step 2), so inside one our vehicles' radar and warning receiver go dark. Read
/// through the Prowler's own public calls (Prowler.Props and Comp, Jammer.TryGetJamCenter, Jammer.EffectiveRadius, from its source of
/// 19 Nov 2026), by name, never by patching it: where it says it is jamming, with the radius it works out, ICAP upgrades included. Nothing
/// is registered if any of them has changed.
/// </summary>
public static class ProwlerJamFields
{
    static MethodInfo? props, comp, center, radius;

    public static bool Found { get; private set; }

    public static void Register()
    {
        props = AccessTools.Method("EA6BProwler.Prowler:Props");
        comp = AccessTools.Method("EA6BProwler.Prowler:Comp");
        center = AccessTools.Method("EA6BProwler.Jammer:TryGetJamCenter");
        radius = AccessTools.Method("EA6BProwler.Jammer:EffectiveRadius");
        Found = props != null && comp != null && center != null && radius != null;
        if (Found) JamFields.Register(FieldsOn);
    }

    static IEnumerable<JamField> FieldsOn(Map map)
    {
        IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
        for (int i = 0; i < pawns.Count; i++)
        {
            if (pawns[i] is not VehiclePawn vehicle) continue;
            object? extension = props!.Invoke(null, new object[] { vehicle });
            if (extension == null) continue;
            object? own = comp!.Invoke(null, new object[] { vehicle });
            if (own == null) continue;
            var args = new object?[] { vehicle, own, extension, null };
            if (center!.Invoke(null, args) is not true || args[3] is not Vector3 at) continue;
            if (radius!.Invoke(null, new object[] { vehicle, extension }) is not float reach) continue;
            // Patches 0.51.0 (EW4): the reach is already the share on radar (ProwlerJamReachPatch); the radio fields go with it, so a Prowler
            // on radios alone, with no radar reach, still jams.
            foreach (JamField field in AircraftJamming.FieldsAt(vehicle, at, UnityEngine.Mathf.Max(0f, reach))) yield return field;
        }
    }
}

/// <summary>
/// The Prowler's Jamming button (Patches 0.51.0; docs/development/electronic-warfare.md, EW4): radar, radios or both, on the player's jet while
/// D'thorga Radio Framework is installed (without it the only choice is radar). Drawn through D'thorga Framework's pawn commands, so nothing
/// patches the author's buttons; the button and its choice are D'thorga Framework's (<see cref="AircraftJamming"/>).
/// </summary>
public static class ProwlerJamButton
{
    static MethodInfo? props;

    public static void Register()
    {
        props = AccessTools.Method("EA6BProwler.Prowler:Props");
        if (props != null) Phobos.Framework.Interface.PawnCommands.Register(For);
    }

    static IEnumerable<Gizmo> For(Pawn pawn)
    {
        if (pawn is not VehiclePawn vehicle || vehicle.Faction != RimWorld.Faction.OfPlayer || !AircraftJamming.RadiosHeard) yield break;
        if (props!.Invoke(null, new object[] { vehicle }) == null || vehicle.GetComp<CompPhobosVehicleSystems>() is not { Usable: true } systems) yield break;
        yield return GeneratorGizmos.AircraftJamButton(systems);
    }
}
