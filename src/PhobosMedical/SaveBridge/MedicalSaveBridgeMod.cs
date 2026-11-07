using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using HarmonyLib;
using Phobos.Medical;
using Phobos.MedicalSaveBridge.Patches;
using Phobos.MedicalSaveBridge.Services;
using RimWorld;
using Verse;

namespace Phobos.MedicalSaveBridge;

/// <summary>Player preferences of the save bridge. The saved keys never change.</summary>
public class SaveBridgeSettings : ModSettings
{
    /// <summary>Save file names (without extension) the player told the bridge never to convert.</summary>
    public List<string> neverConvert = new();

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref neverConvert, "PhobosSaveBridge_NeverConvert", LookMode.Value);
        neverConvert ??= new List<string>();
    }
}

/// <summary>
/// D'thorga Medical's save bridge part (D'thorga Medical Save Bridge until 7 November 2026). Applies the bridge's patches from
/// <see cref="MedicalMod"/>'s constructor, before any save can load. Every target is looked up by name and checked: a missing one
/// switches that part of the bridge off with a log line, never an exception. Since the merge the player is asked before the bridge
/// acts on a save (owner's call, 7 November 2026): convert now, not now, or never for that save (<see cref="BridgeChoices"/>).
/// </summary>
public static class MedicalSaveBridgeMod
{
    public const string MoreInjuriesPackageId = "Th3Fr3d.ExtendedInjuries";

    public static SaveBridgeSettings Settings => MedicalMod.Settings.bridge;

    internal static void Start(Harmony harmony)
    {
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

        // The player's choice: both prefixes or neither, so the gate never waits for a load the menu can't announce.
        MethodBase? loadFromFile = AccessTools.Method(typeof(SavedGameLoaderNow), nameof(SavedGameLoaderNow.LoadGameFromSaveFileNow), new[] { typeof(string) });
        MethodBase? checkAndLoad = AccessTools.Method(typeof(GameDataSaveLoader), nameof(GameDataSaveLoader.CheckVersionAndLoadGame), new[] { typeof(string) });
        if (loadFromFile != null && checkAndLoad != null
            && ApplyPrefix(harmony, applied, missing, "which save is loading", loadFromFile, AccessTools.Method(typeof(LoadChoicePatches), nameof(LoadChoicePatches.BeforeLoadFromFile)))
            && ApplyPrefix(harmony, applied, missing, "ask before converting", checkAndLoad, AccessTools.Method(typeof(LoadChoicePatches), nameof(LoadChoicePatches.BeforeCheckVersionAndLoad))))
        {
            BridgeChoices.LoaderHooked = true;
        }
        else if (loadFromFile == null || checkAndLoad == null)
        {
            missing.Add("ask before converting (converts without asking, as before)");
        }

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

        var report = new Phobos.Framework.LoadReport(MedicalMod.LogPrefix, MedicalMod.ModVersion, "save bridge");
        foreach (string part in applied) report.On(part);
        foreach (string part in missing) report.Off(part, "the game or mod no longer has the method");
        report.Log();
    }

    internal static void DrawSettings(Listing_Standard listing)
    {
        listing.Label("PhobosSaveBridge_SettingAbout".Translate());
        int never = Settings.neverConvert.Count;
        listing.Label("PhobosSaveBridge_SettingNever".Translate(never.Named("COUNT")));
        if (never > 0 && listing.ButtonText("PhobosSaveBridge_SettingForget".Translate()))
        {
            BridgeChoices.ForgetNever();
            Messages.Message("PhobosSaveBridge_Forgotten".Translate(never.Named("COUNT")), MessageTypeDefOf.TaskCompletion, false);
        }
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

    static bool ApplyPrefix(Harmony harmony, List<string> applied, List<string> missing, string name, MethodBase target, MethodInfo? prefix)
    {
        if (prefix == null)
        {
            missing.Add(name);
            return false;
        }
        try
        {
            harmony.Patch(target, prefix: new HarmonyMethod(prefix));
            applied.Add(name);
            return true;
        }
        catch (Exception exception)
        {
            missing.Add(name + " (" + exception.GetType().Name + ": " + exception.Message + ")");
            return false;
        }
    }
}
