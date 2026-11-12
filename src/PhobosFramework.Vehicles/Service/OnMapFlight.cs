using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Aircraft that fly on the map by themselves (Framework 0.102.0). Vehicle Framework's own aircraft leave the map in a stand-in and land
/// in one; rabiosus's VTOL Vehicle framework (the Hind) and the fixed-wing framework (the Harrier) instead keep their aircraft spawned and
/// fly them across the map, taking off and landing where they stand. Each such vehicle class exposes a public bool IsAirborne, and a float
/// CurrentDrawProgress for how far it has climbed (0 on the ground, 1 at cruise). This reads both by name from whatever class a vehicle
/// is, so no core assembly references either framework (optional tie-ins only). A class without IsAirborne is never airborne here.
/// <para>The lookups are by a type found at run time, so scripts/check-targets.py lists them as unchecked; read 12 Nov 2026 from
/// VTOLVehicle.decompiled.cs:10369 and :10487 and FixedWingAircraftFramework.decompiled.cs:17261 and :17379.</para>
/// </summary>
public static class OnMapFlight
{
    sealed class Readers
    {
        public MethodInfo? Airborne;
        public MethodInfo? Progress;
    }

    /// <summary>One entry per vehicle class, read once: the game's assemblies do not change while it runs.</summary>
    static readonly Dictionary<Type, Readers?> byType = new();
    static readonly object[] none = Array.Empty<object>();

    static Readers? For(Type type)
    {
        if (byType.TryGetValue(type, out Readers? readers)) return readers;
        MethodInfo? airborne = AccessTools.PropertyGetter(type, "IsAirborne");
        readers = airborne != null && airborne.ReturnType == typeof(bool)
            ? new Readers { Airborne = airborne, Progress = AccessTools.PropertyGetter(type, "CurrentDrawProgress") is { ReturnType: var t } p && t == typeof(float) ? p : null }
            : null;
        byType[type] = readers;
        return readers;
    }

    /// <summary>Whether vehicles of this class fly on the map by themselves.</summary>
    public static bool FliesItself(Type? vehicleClass) => vehicleClass != null && For(vehicleClass) != null;

    /// <summary>Whether this vehicle flies on the map by itself.</summary>
    public static bool FliesItself(VehiclePawn vehicle) => For(vehicle.GetType()) != null;

    /// <summary>Whether this vehicle is in the air on its map now: taking off, flying, landing or leaving. Always false for an aircraft that
    /// does not fly on the map by itself (Vehicle Framework's own are off the map in the air).</summary>
    public static bool Airborne(VehiclePawn vehicle)
    {
        if (For(vehicle.GetType()) is not { Airborne: { } getter }) return false;
        try { return getter.Invoke(vehicle, none) is true; }
        catch (Exception e)
        {
            VehicleSystemsMod.Logger.WarnOnce($"Could not tell whether a {vehicle.GetType().Name} is in the air ({e.GetType().Name}); it is taken to be on the ground.");
            return false;
        }
    }

    /// <summary>How far the vehicle has climbed, 0 on the ground to 1 at cruise; 1 when airborne and its climb cannot be read, 0 otherwise.</summary>
    public static float Climb(VehiclePawn vehicle)
    {
        if (!Airborne(vehicle)) return 0f;
        if (For(vehicle.GetType()) is not { Progress: { } getter }) return 1f;
        try { return getter.Invoke(vehicle, none) is float progress ? UnityEngine.Mathf.Clamp01(progress) : 1f; }
        catch { return 1f; }
    }
}
