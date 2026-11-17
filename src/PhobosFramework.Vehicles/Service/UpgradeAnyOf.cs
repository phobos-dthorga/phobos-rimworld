using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Phobos.Framework.Vehicles.Rules;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// On an upgrade tree: "any one of" prerequisites (Framework 0.127.1; owner, 17 Nov 2026: the tanker's tree should branch from its tanks,
/// which are alternatives). Vehicle Framework's own prerequisites all have to be fitted. A key naming a node the tree does not have (a gas
/// vessel added only with a tie-in) is never fitted and is not drawn. Every other author's tree is unchanged.
/// </summary>
public sealed class PhobosUpgradeAnyOf : DefModExtension
{
    public List<PhobosUpgradeAnyOfLink> links = new();

    List<UpgradeAnyOfLink>? rules;

    public IReadOnlyList<UpgradeAnyOfLink> Rules => rules ??= links.Select(l => new UpgradeAnyOfLink(l.node, l.anyOf, l.holdsParent)).ToList();

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        foreach (PhobosUpgradeAnyOfLink link in links)
        {
            if (link.node.NullOrEmpty()) yield return "an any-of link names no node";
            if (link.anyOf.NullOrEmpty()) yield return "the any-of link for " + link.node + " names no parents";
        }
    }
}

/// <summary>One link, as written in XML: <c>node</c> needs any one of <c>anyOf</c>; <c>holdsParent</c> keeps that parent on while it is fitted.</summary>
public sealed class PhobosUpgradeAnyOfLink
{
    public string node = "";
    public List<string> anyOf = new();
    public bool holdsParent;
}

/// <summary>Reads the any-of links for a vehicle's tree (service: decides, never draws).</summary>
public static class UpgradeAnyOf
{
    public static PhobosUpgradeAnyOf? For(CompUpgradeTree tree) => tree.Props?.def?.GetModExtension<PhobosUpgradeAnyOf>();

    static Func<string, bool> Fitted(CompUpgradeTree tree) => key => tree.Props.def.GetNode(key) is { } node && tree.NodeUnlocked(node);

    /// <summary>Why <paramref name="node"/> cannot be fitted yet, naming the upgrades that would let it, or null.</summary>
    public static string? Refusal(CompUpgradeTree tree, UpgradeNode? node)
    {
        if (node == null || For(tree) is not { } ext) return null;
        UpgradeAnyOfLink? unmet = UpgradeAnyOfRule.Unmet(node.key, ext.Rules, Fitted(tree));
        if (unmet == null) return null;
        string choices = unmet.AnyOf.Select(key => tree.Props.def.GetNode(key)?.label).Where(label => !label.NullOrEmpty()).Select(label => label!).ToCommaListOr();
        return "PhobosVehicleSystems_UpgradeNeedsOneOf".Translate(choices.Named("UPGRADES"));
    }

    /// <summary>Why fitted <paramref name="node"/> cannot come off: another upgrade sits on it. Null when it may.</summary>
    public static string? RemovalRefusal(CompUpgradeTree tree, UpgradeNode? node)
    {
        if (node == null || For(tree) is not { } ext) return null;
        string? holder = UpgradeAnyOfRule.HeldBy(node.key, ext.Rules, Fitted(tree));
        if (holder == null) return null;
        string label = tree.Props.def.GetNode(holder)?.label ?? holder;
        return "PhobosVehicleSystems_UpgradeHeldBy".Translate(label.Named("UPGRADE"));
    }
}

/// <summary>An upgrade with an any-of link counts as locked until one of its parents is fitted. The click, the greyed node and Vehicle
/// Framework's random upgrades for other factions' vehicles all ask this method.</summary>
[HarmonyPatch(typeof(CompUpgradeTree), nameof(CompUpgradeTree.PrerequisitesMet))]
public static class UpgradeAnyOfPatch
{
    public static void Postfix(CompUpgradeTree __instance, UpgradeNode node, ref bool __result)
    {
        if (__result && UpgradeAnyOf.Refusal(__instance, node) != null) __result = false;
    }
}

/// <summary>
/// Draws each any-of link as a branch under Vehicle Framework's own lines: a drop from every parent to a bar, and from the bar to the child.
/// Grey, with the path from a fitted parent to a fitted child drawn white, as Vehicle Framework draws a fitted prerequisite. Links sharing the
/// same parents share a bar; each set of parents gets its own bar height, so the liquid tanks' branch reads apart from the any-tank one.
/// </summary>
[HarmonyPatch(typeof(ITab_Vehicle_Upgrades), "DrawPrerequisites")]
public static class UpgradeAnyOfLinesPatch
{
    /// <summary>Vehicle Framework's own colour for a line to an upgrade not yet fitted, and its line width.</summary>
    static readonly Color UnfittedColor = new(0.3f, 0.3f, 0.3f, 1f);
    const float LineWidth = 2f;

    /// <summary>Pixels between the bars of different parent sets.</summary>
    const float BarSpacing = 10f;

    static readonly Func<ITab_Vehicle_Upgrades, VehiclePawn> VehicleOf =
        AccessTools.MethodDelegate<Func<ITab_Vehicle_Upgrades, VehiclePawn>>(AccessTools.PropertyGetter(typeof(ITab_Vehicle_Upgrades), "Vehicle"));

    static readonly Func<ITab_Vehicle_Upgrades, IntVec2, Vector2> ScreenPos =
        AccessTools.MethodDelegate<Func<ITab_Vehicle_Upgrades, IntVec2, Vector2>>(
            AccessTools.Method(typeof(ITab_Vehicle_Upgrades), "GridCoordinateToScreenPos", new[] { typeof(IntVec2) }));

    public static void Postfix(ITab_Vehicle_Upgrades __instance)
    {
        CompUpgradeTree? tree = VehicleOf(__instance)?.CompUpgradeTree;
        if (tree == null || UpgradeAnyOf.For(tree) is not { } ext) return;
        UpgradeTreeDef def = tree.Props.def;
        List<IGrouping<string, UpgradeAnyOfLink>> groups = ext.Rules.GroupBy(link => string.Join("|", link.AnyOf)).ToList();
        for (int g = 0; g < groups.Count; g++)
        {
            List<UpgradeNode> parents = groups[g].First().AnyOf.Select(def.GetNode).Where(n => n is { hidden: false }).ToList();
            List<UpgradeNode> children = groups[g].Select(link => def.GetNode(link.Node)).Where(n => n is { hidden: false }).ToList();
            if (parents.Count == 0 || children.Count == 0) continue;
            List<Vector2> parentAt = parents.Select(n => ScreenPos(__instance, n.GridCoordinate)).ToList();
            List<Vector2> childAt = children.Select(n => ScreenPos(__instance, n.GridCoordinate)).ToList();
            float top = parentAt.Max(p => p.y);
            float bottom = childAt.Min(c => c.y);
            float barY = top + (bottom - top) / 2f + (g - (groups.Count - 1) / 2f) * BarSpacing;
            float left = Mathf.Min(parentAt.Min(p => p.x), childAt.Min(c => c.x));
            float right = Mathf.Max(parentAt.Max(p => p.x), childAt.Max(c => c.x));

            // The grey skeleton first, then the white path over it.
            foreach (Vector2 p in parentAt) Widgets.DrawLine(p, new Vector2(p.x, barY), UnfittedColor, LineWidth);
            Widgets.DrawLine(new Vector2(left, barY), new Vector2(right, barY), UnfittedColor, LineWidth);
            foreach (Vector2 c in childAt) Widgets.DrawLine(new Vector2(c.x, barY), c, UnfittedColor, LineWidth);

            int fittedParent = parents.FindIndex(tree.NodeUnlocked);
            if (fittedParent < 0) continue;
            Vector2 from = parentAt[fittedParent];
            for (int i = 0; i < children.Count; i++)
            {
                if (!tree.NodeUnlocked(children[i])) continue;
                Widgets.DrawLine(from, new Vector2(from.x, barY), Color.white, LineWidth);
                Widgets.DrawLine(new Vector2(from.x, barY), new Vector2(childAt[i].x, barY), Color.white, LineWidth);
                Widgets.DrawLine(new Vector2(childAt[i].x, barY), childAt[i], Color.white, LineWidth);
            }
        }
    }
}
