using HarmonyLib;
using Phobos.Framework;
using Phobos.Shared;
using Verse;

namespace Phobos.NiceResearchTabPatches;

/// <summary>
/// D'thorga Patches' Nice Research Tab part (from 0.38.0), switched on only while that mod is active (this assembly lives in its load
/// folder). <b>Temporary</b>: it holds one guard, so a blank entry in a research project's unlock list cannot stop the research tab opening
/// (ResearchUnlocksGuard). It patches only the game's own getter and references no assembly of the mod. No settings, no save data.
/// How to take it out: docs/development/temporary-patches.md, row 23.
/// </summary>
public class NiceResearchTabPatchesMod : Mod
{
    /// <summary>The Harmony ID is the packageId, shared by every part; never change it once the mod is published.</summary>
    public const string PackageId = "PhobosDthorga.Patches";
    public const string ModVersion = "0.40.0";
    public const string LogPrefix = "[D'thorga Patches]";
    public static readonly Phobos.Logging.PhobosLog Logger = Phobos.Framework.Logging.Logs.For("D'thorga Patches", "Nice Research Tab");
    const string Feature = "temporary guard: research projects' unlock lists are read one at a time and kept free of blanks";

    public NiceResearchTabPatchesMod(ModContentPack content) : base(content)
    {
        HarmonyPatchApplier.ApplyEach(new Harmony(PackageId), typeof(NiceResearchTabPatchesMod).Assembly, LogPrefix);
        LongEventHandler.ExecuteWhenFinished(LogLoaded);
    }

    static void LogLoaded() => new LoadReport(LogPrefix, ModVersion, "Nice Research Tab").On(Feature).Log();
}
