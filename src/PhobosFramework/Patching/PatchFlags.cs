using System;
using System.Collections.Generic;
using System.Xml;
using Verse;

namespace Phobos.Framework.Patching;

/// <summary>
/// Player settings that XML patches may read. A mod registers each flag in its Mod constructor, which the game runs before it applies
/// any patch; <see cref="PatchOperationPhobosFlag"/> then applies its operations only while the flag is on. For changes a setting makes
/// to Defs (another mod's vehicle turned electric): they take effect on the next start. Flag names are the registering mod's own
/// (prefix them with its namespace) and stay stable once players have them.
/// </summary>
public static class PatchFlags
{
    static readonly Dictionary<string, Func<bool>> flags = new();

    public static void Register(string name, Func<bool> isOn) => flags[name] = isOn;

    /// <summary>Whether the flag is registered and on. An unregistered flag (its mod is not loaded) counts as off.</summary>
    public static bool IsOn(string name)
    {
        try
        {
            return flags.TryGetValue(name, out Func<bool>? isOn) && isOn();
        }
        catch (Exception)
        {
            return false;
        }
    }
}

/// <summary>
/// Applies its operations, in order, only while a registered setting flag is on; succeeds without doing anything while it is off.
/// <code>
/// &lt;Operation Class="Phobos.Framework.Patching.PatchOperationPhobosFlag"&gt;
///   &lt;flag&gt;PhobosVehicleSystems.electricLightning&lt;/flag&gt;
///   &lt;operations&gt; ... &lt;/operations&gt;
/// &lt;/Operation&gt;
/// </code>
/// </summary>
public class PatchOperationPhobosFlag : PatchOperation
{
    public string flag = "";
    public List<PatchOperation> operations = new();

    protected override bool ApplyWorker(XmlDocument xml)
    {
        if (!PatchFlags.IsOn(flag)) return true;
        foreach (PatchOperation operation in operations)
        {
            if (!operation.Apply(xml)) return false;
        }
        return true;
    }
}
