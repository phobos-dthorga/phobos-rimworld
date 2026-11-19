using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using Phobos.Framework.Rules;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The vehicle message level (Framework 0.187.0; owner's choice of 20 Nov 2026, docs/development/settings-pages.md): how far the vehicle
/// systems' messages nobody asked for are quietened. They post messages from about 150 places; most answer the player's own clicks and
/// always show (MessageLevelRule), but the rest (a fitting finished, a top-up done, a part worn) came with no way to quiet them.
///
/// <para>A prefix on the game's one message method that may skip the original. Recorded reason (AGENTS.md: a skipping prefix needs one):
/// the game offers no hook for filtering messages, and sorting 150 call sites by hand would miss every message added later. It skips only
/// a message posted by our own vehicle code, outside the game's GUI (so no click caused it), of a kind the level hides. At the default
/// level, All, it returns at once and costs nothing. Six other mods in the owner's list prefix this method (query-patches, 20 Nov 2026),
/// each for its own messages; this one leaves theirs alone.</para>
/// </summary>
[HarmonyPatch(typeof(Messages), nameof(Messages.Message), new[] { typeof(Message), typeof(bool) })]
static class VehicleMessageLevel
{
    /// <summary>Our vehicle assemblies' names start so (PhobosFramework.Vehicles and its tie-ins).</summary>
    const string OurAssemblies = "PhobosFramework.Vehicles";
    static readonly Dictionary<Assembly, bool> ours = new();

    /// <summary>Messages hidden this session, for the dev action and the seen-in-play note.</summary>
    public static int Hidden { get; private set; }

    static bool Prefix(Message msg)
    {
        MessageLevel level = VehicleSystemsMod.Settings.messageLevel;
        if (level == MessageLevel.All || msg?.def == null) return true;
        MessageKind kind = MessageLevelRule.KindOf(msg.def.defName);
        // Cheap tests first: a reply, or a kind this level keeps, never needs the stack read.
        if (!MessageLevelRule.Hides(level, kind, askedFor: false)) return true;
        if (!PostedByOurCodeUnasked(out bool askedFor) || MessageLevelRule.Hides(level, kind, askedFor) == false) return true;
        Hidden++;
        if (Seen.Wants("vehicles.message-level", variant: level.ToString())) Seen.Note("vehicles.message-level", null, msg.def.defName, variant: level.ToString());
        return false;
    }

    /// <summary>
    /// Whether our vehicle code posted the message (the first caller past the game's own Messages class), and whether the player asked
    /// for it: posted during the game's GUI pass (Root.OnGUI on the stack), where every click, gizmo and menu choice runs. Ticks run in
    /// the game's Update pass, so a message from them is one nobody asked for.
    /// </summary>
    static bool PostedByOurCodeUnasked(out bool askedFor)
    {
        askedFor = false;
        bool? caller = null;
        var trace = new StackTrace(1, false);
        for (int i = 0; i < trace.FrameCount; i++)
        {
            MethodBase? method = trace.GetFrame(i)?.GetMethod();
            Type? type = method?.DeclaringType;
            if (type == null) continue;
            if (caller == null)
            {
                if (type == typeof(Messages) || type == typeof(VehicleMessageLevel)) continue;
                caller = Ours(type.Assembly);
                if (caller == false) return false;
                continue;
            }
            if (method!.Name == nameof(Root.OnGUI) && typeof(Root).IsAssignableFrom(type))
            {
                askedFor = true;
                break;
            }
        }
        return caller == true;
    }

    static bool Ours(Assembly assembly)
    {
        if (!ours.TryGetValue(assembly, out bool mine))
        {
            mine = assembly.GetName().Name?.StartsWith(OurAssemblies, StringComparison.Ordinal) == true;
            ours[assembly] = mine;
        }
        return mine;
    }
}
