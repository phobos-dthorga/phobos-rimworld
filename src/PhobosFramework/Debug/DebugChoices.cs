using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Phobos.Framework.Debug;

/// <summary>
/// Choices for a dev action that depend on what is selected, built at the moment of the click. A [DebugAction] that returns its child
/// nodes is asked once, the first time its entry is opened, and kept: opened with nothing selected it has no children and no action, and
/// the game throws on entering it (the owner hit this on 10 Nov 2026 with "Vehicle systems: hit a part"); opened with one vehicle
/// selected it keeps that vehicle's choices for every later one. So such an action is a plain one that opens a float menu instead, and
/// says why when there is nothing to choose.
/// </summary>
public static class DebugChoices
{
    /// <summary>Opens the choices, or says why there are none, as a message and in the log.</summary>
    public static void Open(IEnumerable<FloatMenuOption> choices, string noneReason)
    {
        List<FloatMenuOption> list = choices.ToList();
        if (list.Count == 0)
        {
            Messages.Message(noneReason, MessageTypeDefOf.RejectInput, historical: false);
            Log.Message("[D'thorga dev] " + noneReason);
            return;
        }
        Find.WindowStack.Add(new FloatMenu(list));
    }

    public static FloatMenuOption Choice(string label, Action action) => new(label, action);

    /// <summary>A choice that opens further choices, themselves built when it is clicked.</summary>
    public static FloatMenuOption Submenu(string label, Func<IEnumerable<FloatMenuOption>> choices, string noneReason) =>
        new(label + " ...", () => Open(choices(), noneReason));
}
