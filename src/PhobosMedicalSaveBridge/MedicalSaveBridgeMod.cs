using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using HarmonyLib;
using Phobos.MedicalSaveBridge.Patches;
using Phobos.MedicalSaveBridge.Services;
using Verse;

namespace Phobos.MedicalSaveBridge;

/// <summary>
/// Applies the bridge's patches in the Mod constructor, before any save can load. Every target is looked up by name and checked:
/// a missing one switches that part of the bridge off with a log line, never an exception. The Harmony ID is the packageId;
/// never change it once the mod is published.
/// </summary>
public class MedicalSaveBridgeMod : Mod
{
    public const string HarmonyId = "PhobosDthorga.MedicalSaveBridge";
    public const string ModVersion = "0.1.0";
    public const string MoreInjuriesPackageId = "Th3Fr3d.ExtendedInjuries";

    public MedicalSaveBridgeMod(ModContentPack content) : base(content)
    {
        var harmony = new Harmony(HarmonyId);
        var applied = new List<string>();
        var missing = new List<string>();

        Apply(harmony, applied, missing, "save header gate",
            AccessTools.Method(typeof(ScribeMetaHeaderUtility), nameof(ScribeMetaHeaderUtility.LoadGameDataHeader),
                new[] { typeof(ScribeMetaHeaderUtility.ScribeHeaderMode), typeof(bool) }),
            AccessTools.Method(typeof(SaveHeaderPatches), nameof(SaveHeaderPatches.AfterHeaderRead)));
        Apply(harmony, applied, missing, "body part re-numbering",
            AccessTools.Method(typeof(BackCompatibility), nameof(BackCompatibility.GetBackCompatibleBodyPartIndex),
                new[] { typeof(BodyDef), typeof(int) }),
            AccessTools.Method(typeof(BodyPartIndexPatch), nameof(BodyPartIndexPatch.Postfix)));
        Apply(harmony, applied, missing, "hediff class upgrade",
            AccessTools.Method(typeof(BackCompatibility), nameof(BackCompatibility.GetBackCompatibleType),
                new[] { typeof(Type), typeof(string), typeof(XmlNode) }),
            AccessTools.Method(typeof(HediffClassPatch), nameof(HediffClassPatch.Postfix)));
        Apply(harmony, applied, missing, "load report",
            AccessTools.Method(typeof(Game), nameof(Game.LoadGame), Type.EmptyTypes),
            AccessTools.Method(typeof(SaveHeaderPatches), nameof(SaveHeaderPatches.AfterGameLoaded)));
        Apply(harmony, applied, missing, "new game reset",
            AccessTools.Method(typeof(Game), nameof(Game.InitNewGame), Type.EmptyTypes),
            AccessTools.Method(typeof(SaveHeaderPatches), nameof(SaveHeaderPatches.AfterNewGame)));

        if (ModsConfig.IsActive(MoreInjuriesPackageId))
        {
            Apply(harmony, applied, missing, "More Injuries first-load wipe",
                MoreInjuriesFirstLoadPatch.FindTarget(),
                AccessTools.Method(typeof(MoreInjuriesFirstLoadPatch), nameof(MoreInjuriesFirstLoadPatch.Postfix)));
        }
        else
        {
            applied.Add("More Injuries first-load wipe (not needed: More Injuries is not active)");
        }

        Log.Message(BridgeState.LogPrefix + " " + ModVersion + " loaded. On: " + string.Join(", ", applied) + "."
            + (missing.Count == 0 ? "" : " OFF because the game or mod no longer has the method: " + string.Join(", ", missing) + "."));
    }

    static void Apply(Harmony harmony, List<string> applied, List<string> missing, string name, MethodBase? target, MethodInfo? postfix)
    {
        if (target == null || postfix == null)
        {
            missing.Add(name);
            return;
        }
        try
        {
            harmony.Patch(target, postfix: new HarmonyMethod(postfix));
            applied.Add(name);
        }
        catch (Exception exception)
        {
            missing.Add(name + " (" + exception.GetType().Name + ": " + exception.Message + ")");
        }
    }
}
