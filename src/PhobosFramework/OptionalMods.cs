using System;
using System.Collections.Generic;
using System.Reflection;
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

    const BindingFlags StaticMembers = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
    const BindingFlags InstanceFields = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    /// <summary>
    /// The member another mod keeps its settings in: a static property or field on <paramref name="typeFullName"/>, read by name.
    /// Its type, for <see cref="SettingField"/>, and its current value; both null when the mod, the type or the member is absent.
    /// </summary>
    public static (Type? type, object? value) StaticMember(string typeFullName, string memberName)
    {
        Type? owner = TypeNamed(typeFullName);
        if (owner == null) return (null, null);
        PropertyInfo? property = owner.GetProperty(memberName, StaticMembers);
        if (property != null) return (property.PropertyType, property.GetValue(null));
        FieldInfo? field = owner.GetField(memberName, StaticMembers);
        return field != null ? (field.FieldType, field.GetValue(null)) : (null, null);
    }

    /// <summary>
    /// A field of <paramref name="fieldType"/> on another mod's settings type: by exact name first, otherwise the one field whose name
    /// contains it (a source generator hides a setting in a longer backing field). Null when absent, of another type, or ambiguous.
    /// </summary>
    public static FieldInfo? SettingField(Type? settingsType, string name, Type fieldType)
    {
        if (settingsType == null) return null;
        FieldInfo? exact = settingsType.GetField(name, InstanceFields);
        if (exact != null) return exact.FieldType == fieldType ? exact : null;
        FieldInfo? found = null;
        foreach (FieldInfo field in settingsType.GetFields(InstanceFields))
        {
            if (field.FieldType != fieldType || field.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) < 0) continue;
            if (found != null) return null;
            found = field;
        }
        return found;
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
