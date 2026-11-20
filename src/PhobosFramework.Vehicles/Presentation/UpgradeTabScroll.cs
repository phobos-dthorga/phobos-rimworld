using System;
using System.Collections.Generic;
using HarmonyLib;
using Phobos.Framework.Interface;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Gives Vehicle Framework's upgrade tab a scroll view when a tree is drawn past its edges (Framework, the release after 0.201.0; owner, 20 Nov 2026: the
/// generator set's last upgrade was cut off). Vehicle Framework draws the tab at a fixed 880 by 520 with no scroll view and places each
/// node by its grid cell (ITab_Vehicle_Upgrades: UpdateSize and GridCoordinateToScreenPosAdjusted, read in its 1.7 build of 18 Nov 2026),
/// so a node below row 21 or right of column 40 is never seen; five of our trees had rows below that. A tree that fits is drawn exactly as
/// before, every other author's included. One that does not is drawn by Vehicle Framework's own DrawGrid inside a scroll view, so its nodes,
/// lines and info panel scroll together with every other mod's additions to them (the Harrier, the Hind, the Mongoose and our any-of links
/// draw lines in DrawPrerequisites, inside DrawGrid). The info panel is kept in view, as Vehicle Framework keeps it inside the tab.
/// A prefix that skips the original only to run it once inside the view, through every patch on it, never twice.
/// </summary>
[HarmonyPatch(typeof(ITab_Vehicle_Upgrades), "DrawGrid", new[] { typeof(Rect) })]
public static class UpgradeTabScrollPatch
{
    /// <summary>Room left past the last node, as Vehicle Framework's EdgePadding leaves inside the tab.</summary>
    const float Margin = 5f;

    static readonly Func<ITab_Vehicle_Upgrades, VehiclePawn> VehicleOf =
        AccessTools.MethodDelegate<Func<ITab_Vehicle_Upgrades, VehiclePawn>>(AccessTools.PropertyGetter(typeof(ITab_Vehicle_Upgrades), "Vehicle"));

    static readonly Func<ITab_Vehicle_Upgrades, IntVec2, Vector2, Vector2> NodeCorner =
        AccessTools.MethodDelegate<Func<ITab_Vehicle_Upgrades, IntVec2, Vector2, Vector2>>(
            AccessTools.Method(typeof(ITab_Vehicle_Upgrades), "GridCoordinateToScreenPosAdjusted", new[] { typeof(IntVec2), typeof(Vector2) }));

    static readonly Action<ITab_Vehicle_Upgrades, Rect> DrawGrid =
        AccessTools.MethodDelegate<Action<ITab_Vehicle_Upgrades, Rect>>(
            AccessTools.Method(typeof(ITab_Vehicle_Upgrades), "DrawGrid", new[] { typeof(Rect) }));

    /// <summary>The far corner of each tree's drawn nodes: fixed by its Def, so worked out once.</summary>
    static readonly Dictionary<UpgradeTreeDef, Vector2> ContentEnds = new();

    static bool drawing;
    static Vector2 scroll;
    static UpgradeTreeDef? scrolledTree;

    public static bool Prefix(ITab_Vehicle_Upgrades __instance, Rect rect)
    {
        if (drawing) return true;
        VehiclePawn? vehicle = VehicleOf(__instance);
        UpgradeTreeDef? tree = vehicle?.CompUpgradeTree?.Props?.def;
        if (tree == null) return true;
        Vector2 end = ContentEnd(__instance, tree);
        if (end.x <= rect.xMax && end.y <= rect.yMax) return true;

        // Each tree opens at its top: a scroll kept from another vehicle's tree would land somewhere arbitrary.
        if (tree != scrolledTree)
        {
            scrolledTree = tree;
            scroll = Vector2.zero;
        }
        Rect inView = PhobosWidgets.BeginScrollInPlace(rect, ref scroll, end);
        drawing = true;
        try
        {
            DrawGrid(__instance, inView);
        }
        finally
        {
            drawing = false;
            PhobosWidgets.EndScrollInPlace();
        }
        if (scroll != Vector2.zero) Seen.Note("vehicles.upgrade-tab-scrolled", vehicle, tree.defName);
        return false;
    }

    static Vector2 ContentEnd(ITab_Vehicle_Upgrades tab, UpgradeTreeDef tree)
    {
        if (ContentEnds.TryGetValue(tree, out Vector2 end)) return end;
        end = Vector2.zero;
        foreach (UpgradeNode node in tree.nodes)
        {
            if (node.hidden) continue;
            Vector2 corner = NodeCorner(tab, node.GridCoordinate, node.drawSize) + node.drawSize;
            end = Vector2.Max(end, corner);
        }
        end += new Vector2(Margin, Margin);
        ContentEnds[tree] = end;
        return end;
    }
}
