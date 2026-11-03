using Phobos.MedicalSaveBridge.Rules;
using Phobos.MedicalSaveBridge.Services;
using Verse;

namespace Phobos.MedicalSaveBridge.Patches;

/// <summary>
/// Postfix on BackCompatibility.GetBackCompatibleBodyPartIndex(BodyDef, int), the game's own hook for carrying a saved body part
/// index across a change to the body. ScribeExtractor.BodyPartFromNode calls it for every saved part reference (hediffs,
/// medical bills, MSE2's limb configurations, any mod using Scribe_BodyParts), so this one place re-numbers them all. It
/// changes nothing unless the gate is open and the body gained parts from a mod the save never had.
/// </summary>
public static class BodyPartIndexPatch
{
    public static void Postfix(BodyDef body, ref int __result)
    {
        if (!BridgeState.Active || Scribe.mode != LoadSaveMode.LoadingVars || body == null) return;
        int[]? map = BridgeState.MapFor(body);
        if (map == null) return;
        if (PartIndexRule.TryRemap(map, __result, out int newIndex))
        {
            __result = newIndex;
            BridgeState.CountRemapped(body);
        }
        else
        {
            BridgeState.CountOutOfRange(body, __result);
        }
    }
}
