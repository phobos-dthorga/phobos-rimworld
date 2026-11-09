using HarmonyLib;
using Phobos.Framework;
using Verse;

namespace Phobos.VisitSettlementsPatches;

/// <summary>
/// D'thorga Patches' Visit Settlements part (from 0.13.0), switched on only while Visit Settlements is active (this assembly lives in
/// its load folder): a visited settlement's map stays open while the player's people can still act there or a battle is fought on it,
/// and (from 0.14.1) its settlement events no longer throw on every tick when a friendly settlement holds pawns that do not eat.
/// No settings: it fixes faults and changes nothing else.
/// </summary>
public class VisitSettlementsPatchesMod : Mod
{
    /// <summary>The Harmony ID is the packageId, shared by every part; never change it once the mod is published.</summary>
    public const string PackageId = "PhobosDthorga.Patches";
    public const string ModVersion = "0.16.4";
    public const string LogPrefix = "[D'thorga Patches]";
    public static readonly Phobos.Logging.PhobosLog Logger = Phobos.Framework.Logging.Logs.For("D'thorga Patches", "Visit Settlements");
    const string Feature = "a visited settlement's map stays open while your people can still act there or a battle is on";
    const string FoodFeature = "friendly settlements' food drops leave out pawns that do not eat";
    const string GuardFeature = "a fault in its settlement events waits its usual days instead of repeating every tick";

    static string? notReady;
    static string? eventsNotReady;

    public VisitSettlementsPatchesMod(ModContentPack content) : base(content)
    {
        var harmony = new Harmony(PackageId);
        notReady = VisitMapStaysOpen.Prepare();
        VisitMapStaysOpen.Apply(harmony);
        eventsNotReady = SettlementEventsGuard.Prepare();
        SettlementEventsGuard.Apply(harmony);
        LongEventHandler.ExecuteWhenFinished(LogLoaded);
    }

    static void LogLoaded()
    {
        var report = new LoadReport(LogPrefix, ModVersion, "Visit Settlements");
        if (VisitMapStaysOpen.Ready) report.On(Feature);
        else report.Off(Feature, notReady ?? "Visit Settlements' code has changed");
        if (SettlementEventsGuard.FoodReady) report.On(FoodFeature);
        else report.Off(FoodFeature, eventsNotReady ?? "Visit Settlements' code has changed");
        if (SettlementEventsGuard.GuardReady) report.On(GuardFeature);
        else report.Off(GuardFeature, eventsNotReady ?? "Visit Settlements' code has changed");
        report.Log();
    }
}
