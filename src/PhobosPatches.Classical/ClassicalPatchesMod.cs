using HarmonyLib;
using Phobos.Framework;
using Phobos.Shared;
using Verse;

namespace Phobos.ClassicalPatches;

/// <summary>
/// D'thorga Patches' Vanilla Factions Expanded - Classical part (from 0.25.0), switched on only while that mod is active (this assembly lives in
/// its load folder). <b>Temporary</b>: it holds one fix, so a save made during a scorpion raid loads with the raid's orders
/// (RaidOrdersOnLoadPatch). It patches only the game's own method and references no assembly of the mod. No settings, no save data.
/// How to take it out: docs/development/temporary-patches.md.
/// </summary>
public class ClassicalPatchesMod : Mod
{
    /// <summary>The Harmony ID is the packageId, shared by every part; never change it once the mod is published.</summary>
    public const string PackageId = "PhobosDthorga.Patches";
    public const string ModVersion = "0.31.0";
    public const string LogPrefix = "[D'thorga Patches]";
    public static readonly Phobos.Logging.PhobosLog Logger = Phobos.Framework.Logging.Logs.For("D'thorga Patches", "Classical");
    const string Feature = "temporary fix: a save made during a scorpion raid loads with the raid's orders";

    public ClassicalPatchesMod(ModContentPack content) : base(content)
    {
        HarmonyPatchApplier.ApplyEach(new Harmony(PackageId), typeof(ClassicalPatchesMod).Assembly, LogPrefix);
        LongEventHandler.ExecuteWhenFinished(LogLoaded);
    }

    static void LogLoaded() => new LoadReport(LogPrefix, ModVersion, "Classical").On(Feature).Log();
}
