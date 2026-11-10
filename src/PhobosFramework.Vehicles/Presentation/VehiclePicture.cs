using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Vehicles;
using Vehicles.Rendering;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A vehicle Def's picture in a rect, by Vehicle Framework's own drawing (Framework 0.78.0 for the damage sheet; 0.79.0 for the fleet
/// board's thumbnails). Vehicle Framework is built for 1.6, and a 1.7 build cannot call its methods that take or return a Unity type (the
/// compiler refuses 1.6's Unity module); at run time the game's shim makes the types one, so these go through reflection, and
/// check-targets.py checks the names. Rot8 is SmashTools' type, read off the method's own parameter. If drawing ever fails, it stops
/// trying, says so once in the log, and callers draw without the picture.
/// </summary>
public static class VehiclePicture
{
    static readonly MethodInfo? DrawDefOnGui = AccessTools.Method(typeof(VehicleGui), "DrawVehicleDefOnGUI");
    static readonly MethodInfo? DisplayOffsetForRot = AccessTools.Method(typeof(VehicleDrawProperties), "DisplayOffsetForRot", new[] { typeof(Rot4) });
    static object?[]? northArguments;
    static object?[]? ownArguments;
    static bool failed;

    /// <summary>Draws the picture, facing north (seen from above, front up) or the way the Def likes to be shown. Draws on repaint only.</summary>
    public static void Draw(Rect rect, VehicleDef def, bool facingNorth)
    {
        if (failed || Event.current.type != EventType.Repaint) return;
        try
        {
            if (DrawDefOnGui == null) throw new System.MissingMethodException("VehicleGui", "DrawVehicleDefOnGUI");
            object?[] arguments = facingNorth ? northArguments ??= Arguments(north: true) : ownArguments ??= Arguments(north: false);
            arguments[0] = rect;
            arguments[1] = def;
            DrawDefOnGui.Invoke(null, arguments);
        }
        catch (System.Exception ex)
        {
            failed = true;
            VehicleSystemsMod.Logger.Warn("Vehicle pictures could not be drawn (" + def.defName + "), so windows show vehicles without them: " + (ex.InnerException ?? ex).Message);
        }
    }

    static object?[] Arguments(bool north)
    {
        ParameterInfo[] parameters = DrawDefOnGui!.GetParameters();
        var arguments = new object?[parameters.Length];
        if (north && parameters.Length == 4 && System.Nullable.GetUnderlyingType(parameters[3].ParameterType) is { } rot8)
        {
            arguments[3] = AccessTools.Field(rot8, "North")?.GetValue(null) ?? AccessTools.Property(rot8, "North")?.GetValue(null);
        }
        return arguments;
    }

    /// <summary>The Def's display offset facing north, as a share of the rect (zero if it cannot be read).</summary>
    public static Vector3 DisplayOffsetNorth(VehicleDef def)
    {
        try
        {
            if (def.drawProperties != null && DisplayOffsetForRot?.Invoke(def.drawProperties, new object[] { Rot4.North }) is Vector3 found) return found;
        }
        catch (System.Exception)
        {
            // An offset we cannot read is no offset: the cells sit centred.
        }
        return Vector3.zero;
    }
}
