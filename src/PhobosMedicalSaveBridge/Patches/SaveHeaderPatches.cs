using Phobos.MedicalSaveBridge.Services;
using Verse;

namespace Phobos.MedicalSaveBridge.Patches;

/// <summary>
/// Where the bridge learns which save is loading and when it is done. ScribeMetaHeaderUtility.LoadGameDataHeader reads the
/// save's mod list before Game.LoadGame reads anything else (Verse.SavedGameLoaderNow, RimWorld 1.7), so a postfix there sets
/// the gate for the whole load. Game.LoadGame's postfix closes it and writes the report; Game.InitNewGame makes sure a new
/// colony started later in the session sees an inert bridge.
/// </summary>
public static class SaveHeaderPatches
{
    /// <summary>Postfix on ScribeMetaHeaderUtility.LoadGameDataHeader: decide the gate from the header's mod list.</summary>
    public static void AfterHeaderRead(ScribeMetaHeaderUtility.ScribeHeaderMode mode)
    {
        if (mode != ScribeMetaHeaderUtility.ScribeHeaderMode.Map)
        {
            BridgeState.Reset();
            return;
        }
        BridgeState.Recompute(ScribeMetaHeaderUtility.loadedModIdsList);
        Log.Message(BridgeState.DescribeGate());
    }

    /// <summary>Postfix on Game.LoadGame: report what was done, check every pawn, then stop acting.</summary>
    public static void AfterGameLoaded()
    {
        if (!BridgeState.Active) return;
        Log.Message(BridgeState.DescribeCounts());
        LoadReport.Run();
        Log.Message(BridgeState.LogPrefix + " Save this game once and the bridge has nothing left to do for it.");
        BridgeState.Finish();
    }

    /// <summary>Postfix on Game.InitNewGame: a new colony has nothing to bridge.</summary>
    public static void AfterNewGame() => BridgeState.Reset();
}
