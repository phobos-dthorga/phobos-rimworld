using System;
using System.Collections.Generic;

namespace Phobos.Framework.Rules;

/// <summary>
/// A family of namespaces holding copies of the same classes, game-free (Framework 0.309.0). When an author splits a shared framework into
/// one copy per mod, each under the mod's own namespace, a save names its objects by the old shared namespace, or by whichever copy an earlier
/// load picked for them. An object belongs to the copy its owner's Def names (a vehicle's thingClass), so its saved class is re-homed there:
/// the same short name, in the owner's namespace. The first family is the fixed-wing jets' (the Harrier, Close Air Support and the Prowler,
/// 26 Nov 2026), whose old-save converters each claimed every jet for their own copy.
/// </summary>
public sealed class ClassFamily
{
    readonly List<string> prefixes = new();

    /// <summary>A family of the namespaces given, the old shared one among them where there was one. Each is a full namespace, without
    /// the closing dot.</summary>
    public ClassFamily(IEnumerable<string> namespaces)
    {
        foreach (string name in namespaces)
        {
            if (string.IsNullOrEmpty(name)) continue;
            string prefix = name.EndsWith(".", StringComparison.Ordinal) ? name : name + ".";
            if (!prefixes.Contains(prefix)) prefixes.Add(prefix);
        }
    }

    /// <summary>The namespaces of the family, each with its closing dot.</summary>
    public IReadOnlyList<string> Prefixes => prefixes;

    /// <summary>Whether a class name is in one of the family's namespaces, directly (not in a namespace nested below one).</summary>
    public bool Holds(string? className) => PrefixOf(className) != null;

    /// <summary>
    /// The class a saved object should load as: its short name in the namespace of its owner's class. Null when the saved class or the
    /// owner's is not of this family, when both are in the same namespace already, or when either name is empty.
    /// </summary>
    public string? Rehome(string? savedClass, string? ownerClass)
    {
        string? saved = PrefixOf(savedClass);
        string? owner = PrefixOf(ownerClass);
        if (saved == null || owner == null || saved == owner) return null;
        return owner + savedClass!.Substring(saved.Length);
    }

    string? PrefixOf(string? className)
    {
        if (string.IsNullOrEmpty(className)) return null;
        foreach (string prefix in prefixes)
        {
            if (className!.Length > prefix.Length && className.StartsWith(prefix, StringComparison.Ordinal)
                && className.IndexOf('.', prefix.Length) < 0)
                return prefix;
        }
        return null;
    }
}
