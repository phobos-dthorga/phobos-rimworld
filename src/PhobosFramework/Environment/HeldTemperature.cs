using System;
using System.Collections.Generic;
using Verse;

namespace Phobos.Framework.Environment;

/// <summary>
/// A fixed temperature for things held inside something: a vehicle's closed cab, a pod, a chamber. The game asks a held thing's
/// holder chain for one (ThingOwnerUtility.TryGetFixedTemperature) before it looks at the cell or the world tile, which is how a
/// transport pod stays at 14 °C, so a temperature given here reaches the game's own cold, heat and comfort rules on maps and on the
/// world map alike. A Phobos mod registers a reader for its holder type at startup; one Harmony postfix (in the mod that holds the
/// Harmony reference) applies the registry. Readers return null when they have nothing to say, and the game carries on as before.
/// </summary>
public static class HeldTemperature
{
    static readonly Dictionary<Type, Func<IThingHolder, float?>> readers = new();
    static readonly Dictionary<Type, Func<IThingHolder, float?>?> byConcreteType = new();

    /// <summary>Registers the reader for holders of a type (or its subtypes). The last registration for a type wins.</summary>
    public static void Register<THolder>(Func<THolder, float?> reader) where THolder : class, IThingHolder
    {
        readers[typeof(THolder)] = holder => holder is THolder typed ? reader(typed) : null;
        byConcreteType.Clear();
    }

    /// <summary>The fixed temperature a holder gives its contents, if a reader is registered for it and has one.</summary>
    public static bool TryGet(IThingHolder holder, out float temperature)
    {
        temperature = 0f;
        if (holder == null || readers.Count == 0) return false;
        Type type = holder.GetType();
        if (!byConcreteType.TryGetValue(type, out Func<IThingHolder, float?>? reader))
        {
            reader = null;
            for (Type? t = type; t != null && reader == null; t = t.BaseType) readers.TryGetValue(t, out reader);
            byConcreteType[type] = reader;
        }
        float? found = reader?.Invoke(holder);
        if (found == null) return false;
        temperature = found.Value;
        return true;
    }

    /// <summary>How many holder types have readers, for load lines and tests.</summary>
    public static int ReaderCount => readers.Count;
}
