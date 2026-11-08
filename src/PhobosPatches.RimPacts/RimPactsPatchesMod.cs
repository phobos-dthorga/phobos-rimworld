using Phobos.Framework;
using Verse;

namespace Phobos.RimPactsPatches;

/// <summary>
/// D'thorga Patches' RimPacts part (from 0.12.0), switched on only while RimPacts is active (this assembly lives in its load folder):
/// the world war quest shows the game's own countdown. No settings.
/// </summary>
public class RimPactsPatchesMod : Mod
{
    public const string ModVersion = "0.14.2";
    public const string LogPrefix = "[D'thorga Patches]";

    public RimPactsPatchesMod(ModContentPack content) : base(content)
    {
        WarQuestCountdown.Prepare();
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
