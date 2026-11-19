using System;
using System.Reflection;

namespace Phobos.Framework.Rules;

/// <summary>
/// Empty arguments for a class that cannot be built with none, game-free (Framework 0.168.0). The game rebuilds a saved object by
/// making an empty one and then reading its saved values into it, so a class whose only constructors take arguments cannot be loaded
/// from a save at all (Vanilla Factions Expanded - Classical's scorpion raid, 19 Nov 2026: the raid's orders came back as nothing and
/// every raider failed each tick). Built with an empty value for each argument, such an object then reads its saved values as any
/// other would. The answer is for the constructor with the fewest arguments.
/// </summary>
public static class ConstructorDefaultsRule
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    /// <summary>
    /// An empty value for each argument of the type's shortest constructor: nothing for a class, the zero value for a number or a
    /// struct. Null when there is nothing to do or nothing safe to do: the type can be built with no arguments, cannot be built at
    /// all (abstract), is a struct, has no constructor whose arguments can be given plainly, or has two the empty values fit equally.
    /// </summary>
    public static object?[]? EmptyArguments(Type type)
    {
        if (type.IsAbstract || type.IsValueType) return null;
        ConstructorInfo[] all = type.GetConstructors(Any);
        ParameterInfo[]? best = null;
        foreach (ConstructorInfo constructor in all)
        {
            ParameterInfo[] parameters = constructor.GetParameters();
            if (parameters.Length == 0) return null;
            if (!Plain(parameters)) continue;
            if (best == null || parameters.Length < best.Length) best = parameters;
        }
        if (best == null) return null;
        int fits = 0;
        foreach (ConstructorInfo constructor in all)
            if (SameShape(constructor.GetParameters(), best)) fits++;
        if (fits != 1) return null;
        var arguments = new object?[best.Length];
        for (int i = 0; i < best.Length; i++)
        {
            Type wanted = best[i].ParameterType;
            arguments[i] = wanted.IsValueType ? Activator.CreateInstance(wanted) : null;
        }
        return arguments;
    }

    /// <summary>Whether every argument can be handed over as a plain value: no ref or out argument, and no pointer.</summary>
    static bool Plain(ParameterInfo[] parameters)
    {
        foreach (ParameterInfo parameter in parameters)
            if (parameter.ParameterType.IsByRef || parameter.ParameterType.IsPointer) return false;
        return true;
    }

    /// <summary>Whether the empty values for one constructor would fit the other as well: nothing fits any class, a struct only itself.</summary>
    static bool SameShape(ParameterInfo[] a, ParameterInfo[] b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
        {
            Type one = a[i].ParameterType, other = b[i].ParameterType;
            if (one.IsByRef || one.IsPointer) return false;
            if (one.IsValueType || other.IsValueType)
            {
                if (one != other) return false;
            }
        }
        return true;
    }
}
