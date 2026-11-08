using System;
using RimWorld;
using RimWorld.Planet;
using Verse;

// Not "Phobos.Framework.UI": a namespace named UI would hide the game's Verse.UI class from every Phobos.Framework.* namespace.
namespace Phobos.Framework.Interface;

/// <summary>Opening inspector tabs from code. The game's own InspectPaneUtility.OpenTab serves the map's pane only.</summary>
public static class PhobosTabs
{
    /// <summary>Opens the world inspector's tab of this type on the selected world object, if it has one; does nothing otherwise.</summary>
    public static void OpenWorldTab(Type type)
    {
        WorldObject? selected = Find.WorldSelector?.SingleSelectedObject;
        WorldInspectPane? pane = Find.World?.UI?.inspectPane;
        if (selected == null || pane == null) return;
        foreach (InspectTabBase tab in selected.GetInspectTabs())
        {
            if (!type.IsAssignableFrom(tab.GetType())) continue;
            if (pane.OpenTabType != tab.GetType())
            {
                tab.OnOpen();
                pane.OpenTabType = tab.GetType();
            }
            return;
        }
    }
}
