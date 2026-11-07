using System.Collections.Generic;
using System.Linq;
using System.Text;
using LudeonTK;
using Phobos.MedicalSaveBridge.Services;
using Verse;

namespace Phobos.MedicalSaveBridge.Debug;

/// <summary>Dev-mode actions that only read and log, so the owner can check the re-numbering and the pawns by eye.</summary>
public static class BridgeDebugActions
{
    const string Category = "D'thorga";

    [DebugAction(Category, "Medical Save Bridge: log body part indices", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Entry | AllowedGameStates.Playing)]
    public static void LogBodyPartIndices()
    {
        var text = new StringBuilder(BridgeState.DescribeGate());
        List<BodyDef> bodies = BridgeState.BridgedBodies.Select(pair => pair.Key).ToList();
        BodyDef? human = DefDatabase<BodyDef>.GetNamedSilentFail("Human");
        if (human != null && !bodies.Contains(human)) bodies.Add(human);
        foreach (BodyDef body in bodies)
        {
            int[]? map = BridgeState.MapFor(body);
            var oldIndexOf = new Dictionary<int, int>();
            if (map != null)
            {
                for (int old = 0; old < map.Length; old++) oldIndexOf[map[old]] = old;
            }
            text.Append("\n\n").Append(body.defName).Append(": ").Append(body.AllParts.Count).Append(" parts");
            text.Append(map == null ? " (no part is newer than the save)" : ", " + (body.AllParts.Count - map.Length) + " newer than the save");
            for (int i = 0; i < body.AllParts.Count; i++)
            {
                BodyPartRecord part = body.AllParts[i];
                text.Append('\n').Append(i.ToString().PadLeft(4)).Append("  ").Append(part.def.defName);
                if (!part.customLabel.NullOrEmpty()) text.Append(" (").Append(part.customLabel).Append(')');
                if (map == null) continue;
                text.Append(oldIndexOf.TryGetValue(i, out int oldIndex)
                    ? "  was " + oldIndex
                    : "  NEW from " + (part.def.modContentPack?.Name ?? "an unknown mod"));
            }
        }
        Log.Message(text.ToString());
    }

    [DebugAction(Category, "Medical Save Bridge: check every pawn's parts", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
    public static void CheckEveryPawn() => LoadReport.Run();
}
