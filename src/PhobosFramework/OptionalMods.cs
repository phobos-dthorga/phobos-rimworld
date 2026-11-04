using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Phobos.Framework;

/// <summary>
/// Optional tie-ins by name (owner rule: a feature that needs another mod switches on only when that mod is active, and a core
/// assembly never references an optional mod's assembly). Every lookup is cached, and a missing Def or stat is a null, never an
/// error, so a player without the other mod sees nothing.
/// </summary>
public static class OptionalMods
{
    static readonly Dictionary<string, bool> active = new(StringComparer.OrdinalIgnoreCase);
    static readonly Dictionary<string, HediffDef?> hediffs = new();
    static readonly Dictionary<string, StatDef?> stats = new();
    static readonly Dictionary<string, ThingDef?> things = new();
    static readonly Dictionary<string, Type?> types = new();

    public const string CombatExtended = "CETeam.CombatExtended";
    public const string MoreInjuries = "Th3Fr3d.ExtendedInjuries";
    public const string MoreInjuriesOrganFailure = "wenders.moreinjuries.organbridge";

    public static bool IsActive(string packageId)
    {
        if (!active.TryGetValue(packageId, out bool result))
        {
            result = ModsConfig.IsActive(packageId);
            active[packageId] = result;
        }
        return result;
    }

    public static HediffDef? HediffDefNamed(string defName) => Cached(hediffs, defName, DefDatabase<HediffDef>.GetNamedSilentFail);
    public static StatDef? StatDefNamed(string defName) => Cached(stats, defName, DefDatabase<StatDef>.GetNamedSilentFail);
    public static ThingDef? ThingDefNamed(string defName) => Cached(things, defName, DefDatabase<ThingDef>.GetNamedSilentFail);

    /// <summary>A type from any loaded assembly by its full name, or null. Used to read another mod's public settings.</summary>
    public static Type? TypeNamed(string fullName)
    {
        if (!types.TryGetValue(fullName, out Type? type))
        {
            type = GenTypes.GetTypeInAnyAssembly(fullName);
            types[fullName] = type;
        }
        return type;
    }

    static T? Cached<T>(Dictionary<string, T?> cache, string key, Func<string, T?> lookup) where T : class
    {
        if (!cache.TryGetValue(key, out T? value))
        {
            value = lookup(key);
            cache[key] = value;
        }
        return value;
    }
}
