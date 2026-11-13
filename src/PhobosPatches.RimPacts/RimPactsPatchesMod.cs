using Phobos.Framework;
using Verse;

namespace Phobos.RimPactsPatches;

/// <summary>
/// D'thorga Patches' RimPacts part (from 0.12.0), switched on only while RimPacts is active (this assembly lives in its load folder):
/// the world war quest shows the game's own countdown. No settings.
/// </summary>
public class RimPactsPatchesMod : Mod
{
    public const string ModVersion = "0.19.1";
    public const string LogPrefix = "[D'thorga Patches]";
    public static readonly Phobos.Logging.PhobosLog Logger = Phobos.Framework.Logging.Logs.For("D'thorga Patches", "RimPacts");
    const int CheckIntervalTicks = 250;

    public RimPactsPatchesMod(ModContentPack content) : base(content)
    {
        WarQuestCountdown.Prepare();
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
        report.Log();
    }
}
