using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace Phobos.Framework.Defs;

/// <summary>
/// An object loaded inside a Def from XML (a condition, an input, an effect, a rule) that can say what is wrong with itself: a setting name
/// that is no setting, a reading that does not exist. <see cref="NestedConfigChecks"/> finds it at any depth and names the Def holding it.
/// </summary>
public interface IConfigChecked
{
    /// <summary>What is wrong, in a few words for a modder, or null.</summary>
    string? ConfigError();
}

/// <summary>
/// Config errors from the objects nested inside our Defs (Framework, the owner's card of 28 Nov 2026, "Load checks": a misspelt setting name
/// or engine reading had loaded without a word). A Def's own ConfigErrors cannot see an object three lists down, so this walks every Def of
/// a Phobos type, and every Phobos comp and mod extension on a ThingDef, once, when the game has finished loading, and logs each
/// <see cref="IConfigChecked"/> error in the game's own words ("Config error in DEF: ..."). It walks only our own types (never a game or
/// another mod's object, a Def it refers to, or a thing), each object once, with each type's fields read once; nothing is kept afterwards.
/// </summary>
public static class NestedConfigChecks
{
    const string OurNamespace = "Phobos";

    public static void Run()
    {
        try { Walk(); }
        catch (Exception error) { FrameworkMod.Logger.Warn("The nested config checks stopped early: " + error); }
    }

    static void Walk()
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var walker = new Walker();
        int roots = 0;
        // Every Def's mod extensions of ours, every Def of our own types, and the comps of ours on any ThingDef (a vehicle's looks). A Def in
        // two databases (a subtype's and its parent's) is walked once: the walker passes over what it has seen.
        foreach (Type defType in GenDefDatabase.AllDefTypesWithDatabases())
        {
            if (GenGeneric.GetStaticPropertyOnGenericType(typeof(DefDatabase<>), defType, "AllDefs") is not IEnumerable defs) continue;
            foreach (object item in defs)
            {
                if (item is not Def def || def.ignoreConfigErrors) continue;
                bool walked = false;
                if (Ours(def.GetType())) walked = walker.Root(def, def);
                if (def is ThingDef { comps: { } comps })
                {
                    foreach (CompProperties props in comps) if (props != null && Ours(props.GetType())) walked |= walker.Root(def, props);
                }
                if (def.modExtensions != null)
                {
                    foreach (DefModExtension extension in def.modExtensions) if (extension != null && Ours(extension.GetType())) walked |= walker.Root(def, extension);
                }
                if (walked) roots++;
            }
        }
        foreach ((Def def, string error) in walker.Errors) Log.Error("Config error in " + def + ": " + error);
        FrameworkMod.Logger.Debug("Nested config checks: " + roots + " Defs, " + walker.Objects + " objects walked, " + walker.Errors.Count + " errors, in "
            + watch.ElapsedMilliseconds + " ms.");
    }

    static bool Ours(Type type) => type.Namespace?.StartsWith(OurNamespace, StringComparison.Ordinal) == true;

    sealed class Walker
    {
        readonly HashSet<object> seen = new(ReferenceEqualityComparer.Instance);
        static readonly Dictionary<Type, FieldInfo[]> FieldsOf = new();
        public readonly List<(Def def, string error)> Errors = new();
        public int Objects;

        /// <summary>Walks one root of a Def; false when it was walked already.</summary>
        public bool Root(Def owner, object root)
        {
            if (seen.Contains(root)) return false;
            Walk(owner, root);
            return true;
        }

        void Walk(Def owner, object obj)
        {
            if (!seen.Add(obj)) return;
            Objects++;
            if (obj is IConfigChecked checkedObject && checkedObject.ConfigError() is { } error) Errors.Add((owner, error));
            foreach (FieldInfo field in Fields(obj.GetType()))
            {
                object? value = field.GetValue(obj);
                if (value == null) continue;
                if (value is IList list)
                {
                    foreach (object? element in list) if (element != null && Walkable(element.GetType())) Walk(owner, element);
                }
                else if (Walkable(value.GetType())) Walk(owner, value);
            }
        }

        /// <summary>Objects of our own types, not Defs (walked as their own roots) and not things or their comps.</summary>
        static bool Walkable(Type type) =>
            !type.IsValueType && Ours(type) && !typeof(Def).IsAssignableFrom(type) && !typeof(Thing).IsAssignableFrom(type) && !typeof(ThingComp).IsAssignableFrom(type);

        /// <summary>A type's public instance fields that could hold one of our objects: not numbers, text or Defs.</summary>
        static FieldInfo[] Fields(Type type)
        {
            if (FieldsOf.TryGetValue(type, out FieldInfo[]? fields)) return fields;
            var kept = new List<FieldInfo>();
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (CanHold(field.FieldType)) kept.Add(field);
            }
            return FieldsOf[type] = kept.ToArray();
        }

        /// <summary>Whether a field of this declared type could hold one of our objects: not a number, text or Def, nor a list or array of them.</summary>
        static bool CanHold(Type declared)
        {
            if (declared.IsValueType || declared == typeof(string) || typeof(Def).IsAssignableFrom(declared)) return false;
            if (declared.IsArray) return CanHold(declared.GetElementType()!);
            if (declared.IsGenericType && declared.GetGenericTypeDefinition() == typeof(List<>)) return CanHold(declared.GetGenericArguments()[0]);
            return true;
        }
    }
}
