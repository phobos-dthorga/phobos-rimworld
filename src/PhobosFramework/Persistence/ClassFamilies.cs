using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using HarmonyLib;
using Phobos.Framework.Rules;
using Verse;

namespace Phobos.Framework.Persistence;

/// <summary>
/// Loads a saved object as the copy of its class that its owner's Def names, for families of namespaces that hold copies of the same
/// classes (Framework 0.309.0; the rule is <see cref="ClassFamily"/>). A mod registers a family at its start-up; nothing is done for a
/// class outside every family, so with none registered this costs one count check per saved class.
///
/// The owner is the nearest saved thing around the object (the object itself, for a vehicle) whose Def the game knows: its thingClass
/// says which copy it belongs to. The saved class is then re-homed to that namespace when the class exists there and fits where it is
/// loaded. A postfix, because the game skips its converters entirely when a save's mod list matches the running game's, and this method
/// still returns then; so a save already written with the wrong copy is put right too. It runs only while a save loads.
/// </summary>
public static class ClassFamilies
{
    static readonly Phobos.Logging.PhobosLog Logger = Logging.Logs.For("D'thorga Framework", "Saves");
    static readonly List<ClassFamily> families = new();
    static readonly Dictionary<string, int> rehomed = new();
    static bool summaryQueued;

    /// <summary>Adds a family; a mod calls it from its constructor, before any save can load.</summary>
    public static void Register(ClassFamily family)
    {
        if (family != null && family.Prefixes.Count > 1) families.Add(family);
    }

    /// <summary>The families registered.</summary>
    public static int Count => families.Count;

    /// <summary>The class a saved object should load as, or null to keep the game's answer.</summary>
    internal static Type? Rehome(Type? baseType, string? savedClass, XmlNode? node)
    {
        if (families.Count == 0 || savedClass == null || node == null) return null;
        foreach (ClassFamily family in families)
        {
            if (!family.Holds(savedClass)) continue;
            string? name = family.Rehome(savedClass, OwnerClass(node, family));
            if (name == null) return null;
            Type? type = GenTypes.GetTypeInAnyAssembly(name);
            if (type == null || type.IsAbstract || (baseType != null && !baseType.IsAssignableFrom(type))) return null;
            Tally(savedClass, name);
            return type;
        }
        return null;
    }

    /// <summary>The full class name the nearest known Def around the node gives its things, when that class is of the family.</summary>
    static string? OwnerClass(XmlNode node, ClassFamily family)
    {
        for (XmlNode? at = node; at != null; at = at.ParentNode)
        {
            if (at is not XmlElement element || element["def"] is not { } defNode) continue;
            ThingDef? def = DefDatabase<ThingDef>.GetNamedSilentFail(defNode.InnerText);
            if (def == null) continue;
            string? owner = def.thingClass?.FullName;
            return family.Holds(owner) ? owner : null;
        }
        return null;
    }

    static void Tally(string from, string to)
    {
        string key = from + " → " + to;
        rehomed[key] = rehomed.TryGetValue(key, out int count) ? count + 1 : 1;
        if (summaryQueued) return;
        summaryQueued = true;
        // Saves load inside a long event, so this runs once the load is over.
        LongEventHandler.ExecuteWhenFinished(Summarise);
    }

    static void Summarise()
    {
        summaryQueued = false;
        if (rehomed.Count == 0) return;
        int total = rehomed.Values.Sum();
        string detail = string.Join("; ", rehomed.Select(pair => pair.Value + " × " + pair.Key));
        Logger.Info("Loaded " + total + " saved object(s) as the copy of their class their own mod holds: " + detail);
        if (Seen.Wants("framework.class-family-rehome")) Seen.Note("framework.class-family-rehome", null, total + " object(s): " + detail);
        rehomed.Clear();
    }
}

/// <summary>
/// The game's hook for choosing the class a saved object is made as (BackCompatibility.GetBackCompatibleType(Type, string, XmlNode)): a
/// postfix that re-homes a class of a registered family to its owner's copy (<see cref="ClassFamilies"/>). It never skips the game's own
/// answer, and leaves it alone outside a save's loading and outside every family.
/// </summary>
[HarmonyPatch(typeof(BackCompatibility), nameof(BackCompatibility.GetBackCompatibleType), new[] { typeof(Type), typeof(string), typeof(XmlNode) })]
public static class ClassFamiliesPatch
{
    public static void Postfix(Type baseType, string providedClassName, XmlNode node, ref Type __result)
    {
        if (ClassFamilies.Count == 0 || Scribe.mode != LoadSaveMode.LoadingVars) return;
        if (ClassFamilies.Rehome(baseType, providedClassName, node) is { } type) __result = type;
    }
}
