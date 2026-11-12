using HarmonyLib;
using Phobos.Framework;
using Verse;

namespace Phobos.AircraftRearmPatches;

/// <summary>D'thorga Patches' aircraft rearming part, switched on only while the Harrier, the Hind or the Mongoose is active (this assembly lives in a load folder for them). No settings: it fixes a fault and changes nothing else.</summary>
public class AircraftRearmPatchesMod : Mod
{
    /// <summary>The Harmony ID is the packageId, shared by every part; never change it once the mod is published.</summary>
    public const string PackageId = "PhobosDthorga.Patches";
    public const string ModVersion = "0.19.0";
    public const string LogPrefix = "[D'thorga Patches]";

    public AircraftRearmPatchesMod(ModContentPack content) : base(content)
    {
        var report = new LoadReport(LogPrefix, ModVersion, "Aircraft rearming");
        RearmOneKind.Apply(new Harmony(PackageId), report);
        LongEventHandler.ExecuteWhenFinished(report.Log);
    }
}
