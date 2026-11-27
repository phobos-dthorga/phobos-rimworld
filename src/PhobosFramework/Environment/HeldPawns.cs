using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace Phobos.Framework.Environment;

/// <summary>
/// A mod that keeps people off a map, despawned, and brings them back when they are wanted (Framework 0.364.0): Custom Base Framework keeps
/// much of a base's garrison in its reserve. Data, so a row covers another such mod without code: the map component's class, and its own
/// public method that puts everyone it holds back on the map (no parameters). Another modder adds a row the same way.
/// </summary>
public class PhobosHeldPawnsDef : Def
{
    /// <summary>The map component that holds the people, by full name.</summary>
    public string componentClass = "";
    /// <summary>Its public instance method, taking nothing, that puts everyone it holds back on the map.</summary>
    public string bringOutMethod = "";
}

/// <summary>
/// People some mods keep off a map, brought out before a blast so it reaches them too (Framework 0.364.0; owner, 28 Nov 2026: 40 of a
/// struck base's garrison sat out a nuclear strike in Custom Base Framework's reserve and walked out unhurt, and "can that be genericized to
/// work with other mods which might do the same? As a defensive measure"). Each <see cref="PhobosHeldPawnsDef"/> row's own method is called,
/// by name. Then, as the defence, every other map component that holds living people (any that is a thing holder) is named once in the
/// log, with how many it holds, so a row can be added for it: nothing reaches people no one can see.
/// </summary>
public static class HeldPawns
{
    static readonly HashSet<Type> named = new();

    /// <summary>Brings out everyone the map's known holders keep, before <paramref name="why"/>, and names any other holder; how many came out.</summary>
    public static int BringOut(Map? map, string why)
    {
        if (map?.components == null) return 0;
        int brought = 0;
        var handled = new HashSet<Type>();
        foreach (PhobosHeldPawnsDef def in DefDatabase<PhobosHeldPawnsDef>.AllDefsListForReading)
        {
            Type? type = OptionalMods.TypeNamed(def.componentClass);
            if (type == null) continue;
            handled.Add(type);
            MethodInfo? method = type.GetMethod(def.bringOutMethod, BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
            foreach (MapComponent component in map.components.ToList())
            {
                if (!type.IsInstanceOfType(component)) continue;
                int before = Held(component);
                if (before == 0) continue;
                if (method == null)
                {
                    FrameworkMod.Logger.Warn(def.componentClass + " holds " + before + " people off " + Label(map) + ", but its " + def.bringOutMethod
                        + " is gone; " + why + " cannot reach them (" + def.defName + ").");
                    continue;
                }
                try
                {
                    method.Invoke(component, null);
                }
                catch (Exception error)
                {
                    FrameworkMod.Logger.Error(def.componentClass + "." + def.bringOutMethod + " failed before " + why + "; " + error.InnerException?.Message);
                }
                brought += before - Held(component);
            }
        }
        foreach (MapComponent component in map.components)
        {
            Type type = component.GetType();
            if (handled.Any(t => t.IsAssignableFrom(type))) continue;
            int held = Held(component);
            if (held > 0 && named.Add(type))
            {
                FrameworkMod.Logger.Warn(type.FullName + " (" + type.Assembly.GetName().Name + ") holds " + held + " people off " + Label(map)
                    + "; " + why + " cannot reach them. A PhobosHeldPawnsDef row naming its own way to bring them out would.");
            }
        }
        if (brought > 0)
        {
            FrameworkMod.Logger.Info(brought + " people held off " + Label(map) + " were brought out before " + why + ".");
            if (Seen.Wants("environment.held-pawns-out")) Seen.Note("environment.held-pawns-out", null, brought + " before " + why);
        }
        return brought;
    }

    static int Held(MapComponent component)
    {
        if (component is not IThingHolder holder) return 0;
        ThingOwner? owner = holder.GetDirectlyHeldThings();
        if (owner == null) return 0;
        int count = 0;
        for (int i = 0; i < owner.Count; i++)
        {
            if (owner[i] is Pawn { Dead: false }) count++;
        }
        return count;
    }

    static string Label(Map map) => map.Parent?.Label ?? "a map";
}
