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
            if (radius!.Invoke(null, new object[] { vehicle, extension }) is not float reach || reach <= 0f) continue;
            yield return new JamField(at, reach, vehicle.Faction, vehicle);
        }
    }
}
