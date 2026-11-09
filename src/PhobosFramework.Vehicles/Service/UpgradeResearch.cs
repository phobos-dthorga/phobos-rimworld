using System;
using HarmonyLib;
using Phobos.Framework.Rules;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>On an upgrade tree: its nodes need the research they name. Vehicle Framework declares that research and never checks it, so a tree opts in here and every other author's tree is unchanged (docs/development/research-tree.md).</summary>
public sealed class PhobosEnforceUpgradeResearch : DefModExtension
{
}

/// <summary>Decides whether research holds an upgrade back. God mode passes, as it does for the game's own build menu.</summary>
public static class UpgradeResearchGate
{
    public static ResearchProjectDef? Missing(CompUpgradeTree tree, UpgradeNode? node)
    {
        if (node == null || DebugSettings.godMode) return null;
        if (tree.Props?.def?.GetModExtension<PhobosEnforceUpgradeResearch>() == null) return null;
        return ResearchGateRule.FirstMissing(node.researchPrerequisites, project => project.IsFinished);
    }

    public static string Refusal(ResearchProjectDef project) =>
        "PhobosVehicleSystems_UpgradeNeedsResearch".Translate(project.label.Named("RESEARCH"));
}

/// <summary>Makes an opted-in upgrade count as locked until its research is done. One patch covers the click, the greyed node and the highlight, which all ask this method. Saved upgrades load by key, so nothing already fitted is affected.</summary>
[HarmonyPatch(typeof(CompUpgradeTree), nameof(CompUpgradeTree.PrerequisitesMet))]
public static class UpgradeNeedsResearchPatch
{
    public static void Postfix(CompUpgradeTree __instance, UpgradeNode node, ref bool __result)
    {
        if (__result && UpgradeResearchGate.Missing(__instance, node) != null) __result = false;
    }
}

/// <summary>
/// Says which research an upgrade is waiting for. Skips the original only while research refuses the upgrade: Vehicle Framework's own refusal is a fixed line about another upgrade, which would
/// be wrong here, and every refusal has to say why. Draws its own button and message for that one case; Cancel, Remove and every other refusal stay with the original.
/// </summary>
[HarmonyPatch(typeof(ITab_Vehicle_Upgrades), "DrawButtons", new[] { typeof(Rect) })]
public static class UpgradeResearchButtonPatch
{
    /// <summary>Vehicle Framework's own label for this button, so that it reads the same as every other upgrade.</summary>
    const string UpgradeLabelKey = "VF_Upgrade";

    static readonly Func<ITab_Vehicle_Upgrades, VehiclePawn> VehicleOf =
        AccessTools.MethodDelegate<Func<ITab_Vehicle_Upgrades, VehiclePawn>>(AccessTools.PropertyGetter(typeof(ITab_Vehicle_Upgrades), "Vehicle"));

    public static bool Prefix(ITab_Vehicle_Upgrades __instance, Rect rect, UpgradeNode ___selectedNode)
    {
        CompUpgradeTree? tree = VehicleOf(__instance)?.CompUpgradeTree;
        if (tree == null || ___selectedNode == null) return true;
        if (tree.NodeUnlocking == ___selectedNode || tree.NodeUnlocked(___selectedNode) || tree.Disabled(___selectedNode)) return true;
        ResearchProjectDef? missing = UpgradeResearchGate.Missing(tree, ___selectedNode);
        if (missing == null) return true;
        string reason = UpgradeResearchGate.Refusal(missing);
        TooltipHandler.TipRegion(rect, reason);
        if (Widgets.ButtonText(rect, UpgradeLabelKey.Translate())) Messages.Message(reason, MessageTypeDefOf.RejectInput, historical: false);
        return false;
    }
}
