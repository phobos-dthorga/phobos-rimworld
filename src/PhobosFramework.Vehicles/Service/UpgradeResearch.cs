using System;
using System.Collections.Generic;
using HarmonyLib;
using Phobos.Framework.Rules;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// No longer needed: Vehicle Framework enforces the research named on every upgrade tree's nodes since 1.7.2537 (18 Nov 2026), and
/// our trees dropped this marker in Framework 0.137.2. Kept so an upgrade tree written for an older version, by us or another author
/// (docs/development/vehicle-author-overrides.md), still loads. It does nothing.
/// </summary>
public sealed class PhobosEnforceUpgradeResearch : DefModExtension
{
}

/// <summary>Decides whether research holds an upgrade back, on any author's tree, as Vehicle Framework now does. God mode passes, as it does for the game's own build menu.</summary>
public static class UpgradeResearchGate
{
    public static ResearchProjectDef? Missing(UpgradeNode? node)
    {
        if (node == null || DebugSettings.godMode) return null;
        return ResearchGateRule.FirstMissing(node.researchPrerequisites, project => project.IsFinished);
    }

    public static string Refusal(ResearchProjectDef project) =>
        "PhobosVehicleSystems_UpgradeNeedsResearch".Translate(project.label.Named("RESEARCH"));
}

/// <summary>
/// God mode lets an upgrade go on without its research, as it lets a building go up (Vehicle Framework's own check, since 1.7.2537, has no
/// such pass). It opens the node only when research alone held it back: every upgrade it needs is fitted. Runs before our other postfixes on
/// this method, so the any-of links (<see cref="UpgradeAnyOfPatch"/>) still apply.
/// </summary>
[HarmonyPatch(typeof(CompUpgradeTree), nameof(CompUpgradeTree.PrerequisitesMet))]
public static class UpgradeResearchGodModePatch
{
    [HarmonyPriority(Priority.First)]
    public static void Postfix(UpgradeNode node, HashSet<string> ___upgrades, ref bool __result)
    {
        if (__result || !DebugSettings.godMode || node == null) return;
        if (node.prerequisiteNodes != null && !node.prerequisiteNodes.TrueForAll(___upgrades.Contains)) return;
        __result = true;
    }
}

/// <summary>
/// Says why an upgrade cannot go on or come off: the research it waits for, the one-of-several upgrade it needs first (<see cref="UpgradeAnyOf"/>), or
/// the upgrade sitting on it. Skips the original only while one of those refuses: Vehicle Framework's own refusal is a fixed line about another upgrade,
/// which would be wrong here, and its Remove button simply vanishes, and every refusal has to say why. Draws its own button and message for those cases;
/// Cancel and every other refusal stay with the original. The research reason is given on every author's tree, since Vehicle Framework enforces it on all.
/// </summary>
[HarmonyPatch(typeof(ITab_Vehicle_Upgrades), "DrawButtons", new[] { typeof(Rect) })]
public static class UpgradeRefusalButtonPatch
{
    /// <summary>Vehicle Framework's own labels for these buttons, so that they read the same as every other upgrade.</summary>
    const string UpgradeLabelKey = "VF_Upgrade";
    const string RemoveLabelKey = "VF_RemoveUpgrade";

    static readonly Func<ITab_Vehicle_Upgrades, VehiclePawn> VehicleOf =
        AccessTools.MethodDelegate<Func<ITab_Vehicle_Upgrades, VehiclePawn>>(AccessTools.PropertyGetter(typeof(ITab_Vehicle_Upgrades), "Vehicle"));

    public static bool Prefix(ITab_Vehicle_Upgrades __instance, Rect rect, UpgradeNode ___selectedNode)
    {
        VehiclePawn? vehicle = VehicleOf(__instance);
        CompUpgradeTree? tree = vehicle?.CompUpgradeTree;
        if (tree == null || ___selectedNode == null || tree.NodeUnlocking == ___selectedNode) return true;
        if (tree.NodeUnlocked(___selectedNode))
        {
            string? held = UpgradeAnyOf.RemovalRefusal(tree, ___selectedNode);
            if (held == null) return true;
            if (Refused(rect, RemoveLabelKey, held)) Seen.Note("vehicles.upgrade-held-on", vehicle, held);
            return false;
        }
        if (tree.Disabled(___selectedNode)) return true;
        ResearchProjectDef? missing = UpgradeResearchGate.Missing(___selectedNode);
        if (missing != null)
        {
            string reason = UpgradeResearchGate.Refusal(missing);
            if (Refused(rect, UpgradeLabelKey, reason)) Seen.Note("vehicles.upgrade-needs-research", vehicle, reason);
            return false;
        }
        string? needs = UpgradeAnyOf.Refusal(tree, ___selectedNode);
        if (needs == null) return true;
        if (Refused(rect, UpgradeLabelKey, needs)) Seen.Note("vehicles.upgrade-needs-one-of", vehicle, needs);
        return false;
    }

    /// <summary>The button as Vehicle Framework would draw it, saying why when pressed; true when it was pressed. The caller skips the original.</summary>
    static bool Refused(Rect rect, string labelKey, string reason)
    {
        TooltipHandler.TipRegion(rect, reason);
        if (!Widgets.ButtonText(rect, labelKey.Translate())) return false;
        Messages.Message(reason, MessageTypeDefOf.RejectInput, historical: false);
        return true;
    }
}
