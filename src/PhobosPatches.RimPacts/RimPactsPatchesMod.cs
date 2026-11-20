using HarmonyLib;
using Phobos.Framework;
using Verse;

namespace Phobos.RimPactsPatches;

/// <summary>
/// D'thorga Patches' RimPacts part (from 0.12.0), switched on only while RimPacts is active (this assembly lives in its load folder):
/// the world war quest shows the game's own countdown, and (temporary, from 0.19.3) its assaults leave empty vehicles alone. No settings.
/// </summary>
public class RimPactsPatchesMod : Mod
{
    public const string ModVersion = "0.28.0";
    public const string LogPrefix = "[D'thorga Patches]";
    public static readonly Phobos.Logging.PhobosLog Logger = Phobos.Framework.Logging.Logs.For("D'thorga Patches", "RimPacts");
    /// <summary>The Harmony ID is the packageId; never change it once the mod is published.</summary>
    public const string PackageId = "PhobosDthorga.Patches";
    const int CheckIntervalTicks = 250;

    public RimPactsPatchesMod(ModContentPack content) : base(content)
    {
        WarQuestCountdown.Prepare();
        UnmannedVehicleTargets.Apply(new Harmony(PackageId));
        // Four times a game hour: it saves nothing and reads everything from RimPacts (on the Framework's part store, so the save never
        // names a class of this assembly).
        Phobos.Framework.Persistence.GameComponent_PhobosPartStore.Every(CheckIntervalTicks, "D'thorga Patches (RimPacts)", () =>
        {
            if (WarQuestCountdown.Ready) WarQuestCountdown.Refresh();
        });
        LongEventHandler.ExecuteWhenFinished(LogLoaded);
    }

    static void LogLoaded()
    {
        var report = new LoadReport(LogPrefix, ModVersion, "RimPacts");
        if (WarQuestCountdown.Ready) report.On("world war quests show their deadline in the quest window");
        else report.Off("world war quests show their deadline in the quest window", "RimPacts' quest code has changed");
        if (UnmannedVehicleTargets.Applied) report.On("allied assaults leave vehicles with nobody aboard alone (temporary)");
        else report.Off("allied assaults leave vehicles with nobody aboard alone (temporary)", "RimPacts' assault code has changed");
        report.Log();
    }
}
