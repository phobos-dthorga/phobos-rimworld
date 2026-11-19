using System;
using System.Xml;
using Phobos.Framework.Rules;
using Verse;

namespace Phobos.Framework.Persistence;

/// <summary>
/// A second try at loading a saved object the game could not rebuild because its class has no constructor without arguments
/// (Framework 0.168.0; the rule and its reason are in ConstructorDefaultsRule). Call it where the game's own Scribe_Deep.Look for the
/// same label has just run and left nothing, while that object's owner is still being read: the game then builds the object through
/// the constructor it does have, with empty values, and reads its saved values, its references and its after-load step as for any
/// other. It never touches an object that loaded, a save that holds none, or a class it cannot build safely.
/// </summary>
public static class DeepLoad
{
    /// <summary>
    /// Tries again to load what is saved under <paramref name="label"/> in the object being read. True when it loaded this time;
    /// <paramref name="type"/> is the saved class whenever it could be named, loaded or not.
    /// </summary>
    public static bool RetryWithEmptyArguments<T>(ref T? target, string label, out Type? type) where T : class, IExposable
    {
        type = null;
        if (Scribe.mode != LoadSaveMode.LoadingVars || target != null) return false;
        XmlNode? node = Scribe.loader?.curXmlParent?[label];
        if (node == null) return false;
        string? isNull = node.Attributes?["IsNull"]?.Value;
        if (isNull != null && isNull.Equals("true", StringComparison.InvariantCultureIgnoreCase)) return false;
        string? name = node.Attributes?["Class"]?.Value;
        if (name == null) return false;
        type = BackCompatibility.GetBackCompatibleType(typeof(T), name, node);
        if (type == null || !typeof(T).IsAssignableFrom(type)) return false;
        object?[]? arguments = ConstructorDefaultsRule.EmptyArguments(type);
        if (arguments == null) return false;
        Scribe_Deep.Look(ref target!, label, arguments!);
        return target != null;
    }
}
