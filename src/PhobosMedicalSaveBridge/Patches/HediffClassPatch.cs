using System;
using System.Xml;
using Phobos.MedicalSaveBridge.Services;
using Verse;

namespace Phobos.MedicalSaveBridge.Patches;

/// <summary>
/// Postfix on BackCompatibility.GetBackCompatibleType(Type, string, XmlNode), the game's own hook for choosing the class a saved
/// object is created as. A save written before More Injuries holds its wounds as Verse.Hediff_Injury and its lost parts as
/// Verse.Hediff_MissingPart, while More Injuries' XML now says those defs are its BetterInjury and BetterMissingPart. Left alone,
/// Medical System Expansion 2 notices the mismatch during cross-reference resolution and re-installs each such hediff through
/// HediffSet.AddDirect, which refuses any hediff on a missing part and so throws once per lost limb's children. Creating the hediff
/// as its def's class from the start keeps its severity, part, age and tending, and leaves MSE2 nothing to re-install. The class
/// must be a subclass of the saved one, from a mod the save never had, with a parameterless constructor; otherwise the game's
/// answer stands.
/// </summary>
public static class HediffClassPatch
{
    public static void Postfix(Type baseType, XmlNode node, ref Type __result)
    {
        if (!BridgeState.Active || Scribe.mode != LoadSaveMode.LoadingVars || __result == null || node == null) return;
        if (baseType == null || !typeof(Hediff).IsAssignableFrom(baseType)) return;
        XmlElement? defNode = node["def"];
        if (defNode == null) return;
        HediffDef? def = DefDatabase<HediffDef>.GetNamedSilentFail(defNode.InnerText);
        Type? target = def?.hediffClass;
        if (target == null || target == __result || target.IsAbstract) return;
        if (!__result.IsAssignableFrom(target) || !BridgeState.IsFromNewMod(target)) return;
        if (target.GetConstructor(Type.EmptyTypes) == null) return;
        HediffStateDefaults.Apply(target, node);
        BridgeState.CountUpgraded(__result, target);
        __result = target;
    }
}
