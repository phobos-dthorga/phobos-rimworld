using System;
using System.Reflection;
using HarmonyLib;

namespace Phobos.Framework.Patching;

/// <summary>
/// One Harmony patch that a setting puts in and takes out while the game runs (Framework 0.269.0, first for rain soaking the fields):
/// while its setting is off the method is left exactly as the game wrote it, so a patch on a busy method costs nothing until the player
/// asks for it, and no restart is needed either way. The target is found the first time the patch goes in; a target that is gone
/// leaves the patch out with <see cref="Problem"/> saying why, never an error the player sees twice.
/// </summary>
public sealed class SwitchedPatch
{
    readonly Harmony harmony;
    readonly Func<MethodBase?> findTarget;
    readonly MethodInfo? prefix, postfix;
    MethodBase? target;

    /// <summary>A patch by <paramref name="harmony"/> on what <paramref name="findTarget"/> finds (write the target's full name in it, so
    /// scripts/check-targets.py can read it). Give a prefix, a postfix or both.</summary>
    public SwitchedPatch(Harmony harmony, Func<MethodBase?> findTarget, MethodInfo? prefix = null, MethodInfo? postfix = null)
    {
        this.harmony = harmony;
        this.findTarget = findTarget;
        this.prefix = prefix;
        this.postfix = postfix;
    }

    /// <summary>Whether the patch is in the game now.</summary>
    public bool Applied { get; private set; }

    /// <summary>Why the patch could not go in or come out, or null.</summary>
    public string? Problem { get; private set; }

    /// <summary>Puts the patch in or takes it out; nothing happens when it is already as asked. True when it is now as asked.</summary>
    public bool Set(bool on)
    {
        if (on == Applied) return true;
        try
        {
            if (on)
            {
                target ??= findTarget();
                if (target == null)
                {
                    Problem = "the method it patches was not found";
                    return false;
                }
                harmony.Patch(target, prefix: prefix == null ? null : new HarmonyMethod(prefix), postfix: postfix == null ? null : new HarmonyMethod(postfix));
                Applied = true;
            }
            else
            {
                if (prefix != null) harmony.Unpatch(target, prefix);
                if (postfix != null) harmony.Unpatch(target, postfix);
                Applied = false;
            }
            Problem = null;
            return true;
        }
        catch (Exception error)
        {
            Problem = error.GetType().Name + ": " + error.Message;
            FrameworkMod.Logger.Error("could not " + (on ? "put in" : "take out") + " the patch on " + (target?.DeclaringType?.Name + "." + target?.Name) + ": " + error);
            return false;
        }
    }
}
