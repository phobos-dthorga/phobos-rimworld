using Phobos.MedicalSaveBridge.Services;
using Verse;

namespace Phobos.MedicalSaveBridge.Patches;

/// <summary>
/// Where the player is asked before a save is converted. Both are prefixes, applied by name in
/// <see cref="MedicalSaveBridgeMod.Start"/>; the game's code is read from the 1.7 assembly (7 November 2026).
/// </summary>
public static class LoadChoicePatches
{
    /// <summary>
    /// Prefix on GameDataSaveLoader.CheckVersionAndLoadGame(string), which the load menu (main menu and in game) calls. The original
    /// is one line: PreLoadUtility.CheckVersionAndLoad reads the save's header, shows the game's own version and mod-mismatch
    /// dialogs, then loads. This does the same with one step added before the load, the bridge's question, so it skips the original
    /// (recorded reason: the step has to sit inside the original's callback, after the game's dialogs and before the load).
    /// </summary>
    public static bool BeforeCheckVersionAndLoad(string saveFileName)
    {
        PreLoadUtility.CheckVersionAndLoad(GenFilePaths.FilePathForSavedGame(saveFileName), ScribeMetaHeaderUtility.ScribeHeaderMode.Map,
            () => BridgeChoices.AskThenLoad(saveFileName));
        return false;
    }

    /// <summary>Prefix on SavedGameLoaderNow.LoadGameFromSaveFileNow(string): the real load starts, and this is its file.</summary>
    public static void BeforeLoadFromFile(string fileName) => BridgeChoices.NotifyLoadingFile(fileName);
}
