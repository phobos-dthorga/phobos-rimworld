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
/// Says why an upgrade cannot go on or come off: the research it waits for, the one-of-several upgrade it needs first (<see cref="UpgradeAnyOf"/>), or
/// the upgrade sitting on it. Skips the original only while one of those refuses: Vehicle Framework's own refusal is a fixed line about another upgrade,
/// which would be wrong here, and its Remove button simply vanishes, and every refusal has to say why. Draws its own button and message for those cases;
/// Cancel and every other refusal stay with the original.
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
        ResearchProjectDef? missing = UpgradeResearchGate.Missing(tree, ___selectedNode);
        if (missing != null)
        {
            Refused(rect, UpgradeLabelKey, UpgradeResearchGate.Refusal(missing));
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
