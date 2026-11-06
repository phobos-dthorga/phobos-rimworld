using System.Collections.Generic;
using LudeonTK;
using Phobos.Framework.Gases;
using Phobos.Framework.Health;
using UnityEngine;
using Verse;

namespace Phobos.Framework.Debug;

/// <summary>Dev-mode reads and the gas overlay. Nothing here changes game state except the explicit "add" actions.</summary>
public static class FrameworkDebugActions
{
    [DebugAction(DebugCategory.Name, "Framework: log gases at cell", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static void LogGasesAtCell()
    {
        IntVec3 cell = UI.MouseCell();
        Map map = Find.CurrentMap;
        Log.Message(FrameworkMod.LogPrefix + " " + cell + ": " + Describe(cell, map));
    }

    [DebugAction(DebugCategory.Name, "Framework: add 1 cell of each own gas to room at cell", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static void AddOwnGasesAtCell()
    {
        IntVec3 cell = UI.MouseCell();
        Map map = Find.CurrentMap;
        foreach (PhobosGasDef gas in DefDatabase<PhobosGasDef>.AllDefsListForReading)
        {
            PhobosGases.AddOwn(cell, map, gas, 1f);
        }
    }

    [DebugAction(DebugCategory.Name, "Framework: log care choices of selected pawns", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static void LogPatientPreferences()
    {
        var text = new System.Text.StringBuilder(FrameworkMod.LogPrefix);
        foreach (object selected in Find.Selector.SelectedObjects)
        {
            if (selected is not Pawn pawn) continue;
            text.Append('\n').Append(pawn.LabelShort).Append(": group ").Append(Patients.GroupOf(pawn)?.ToString() ?? "none")
                .Append(", care ").Append(Patients.CareLevel(pawn));
            CompPhobosPatient? comp = pawn.TryGetComp<CompPhobosPatient>();
            if (comp == null) { text.Append(", no patient comp"); continue; }
            foreach (PatientPreference preference in PatientPreferences.All)
            {
                int? saved = comp.SavedChoice(preference.Key);
                text.Append("; ").Append(preference.Key).Append(' ').Append(PatientPreferences.Choice(pawn, preference))
                    .Append(saved == null ? " (default)" : " (saved)");
            }
        }
        Log.Message(text.ToString());
    }

    [DebugAction(DebugCategory.Name, "Framework: add smoke at cell", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static void AddSmokeAtCell() => PhobosGases.Add(UI.MouseCell(), Find.CurrentMap, GasKind.Smoke, 1f);

    /// <summary>Every gas the framework can see at a cell: existing kinds by carrier density, framework gases by room.</summary>
    public static string Describe(IntVec3 cell, Map map)
    {
        var parts = new List<string>();
        if (!cell.InBounds(map)) return "out of bounds";
        foreach (GasKind kind in System.Enum.GetValues(typeof(GasKind)))
        {
            if (kind == GasKind.None) continue;
            float density = PhobosGases.Density(cell, map, kind);
            if (density > 0f) parts.Add(kind + " " + density.ToString("0.00") + " (existing)");
        }
        string own = MapComponent_PhobosRoomGases.For(map)?.Describe(cell) ?? "";
        if (own.Length > 0) parts.Add(own);
        parts.Add("enclosure " + PhobosGases.Enclosure(cell, map).ToString("0.00"));
        if (PhobosGases.UsesCombatExtendedSmoke) parts.Add("smoke signal: Combat Extended");
        return string.Join(", ", parts);
    }
}

/// <summary>The dev overlay: a label at the mouse while the setting is on in dev mode. Presentation only.</summary>
public sealed class MapComponent_PhobosGasOverlay : MapComponent
{
    public MapComponent_PhobosGasOverlay(Map map) : base(map) { }

    public override void MapComponentOnGUI()
    {
        if (!Prefs.DevMode || !FrameworkMod.Settings.showGasOverlay || Find.CurrentMap != map) return;
        if (Find.WindowStack.IsOpen<Window>() && Find.WindowStack.CurrentWindowGetsInput) return;
        IntVec3 cell = UI.MouseCell();
        if (!cell.InBounds(map)) return;
        string text = FrameworkDebugActions.Describe(cell, map);
        Vector2 mouse = Event.current.mousePosition;
        var rect = new Rect(mouse.x + 16f, mouse.y + 16f, 420f, 48f);
        Text.Font = GameFont.Tiny;
        Widgets.Label(rect, text);
        Text.Font = GameFont.Small;
    }
}
