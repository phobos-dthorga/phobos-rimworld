using HarmonyLib;
using Phobos.Framework;
using Verse;

namespace Phobos.VisitSettlementsPatches;

/// <summary>
/// D'thorga Patches' Visit Settlements part (from 0.13.0), switched on only while Visit Settlements is active (this assembly lives in
/// its load folder): a visited settlement's map stays open while the player's people can still act there or a battle is fought on it.
/// No settings: it fixes a fault and changes nothing else.
/// </summary>
public class VisitSettlementsPatchesMod : Mod
{
    /// <summary>The Harmony ID is the packageId, shared by every part; never change it once the mod is published.</summary>
    public const string PackageId = "PhobosDthorga.Patches";
    public const string ModVersion = "0.13.0";
    public const string LogPrefix = "[D'thorga Patches]";
    const string Feature = "a visited settlement's map stays open while your people can still act there or a battle is on";

    static string? notReady;

    public VisitSettlementsPatchesMod(ModContentPack content) : base(content)
    {
        notReady = VisitMapStaysOpen.Prepare();
        VisitMapStaysOpen.Apply(new Harmony(PackageId));
        LongEventHandler.ExecuteWhenFinished(LogLoaded);
    }

    static void LogLoaded()
    {
        var report = new LoadReport(LogPrefix, ModVersion, "Visit Settlements");
        if (VisitMapStaysOpen.Ready) report.On(Feature);
        else report.Off(Feature, notReady ?? "Visit Settlements' code has changed");
        report.Log();
    }
}
