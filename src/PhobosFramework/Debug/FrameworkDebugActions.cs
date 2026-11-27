using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using Phobos.Framework.Gases;
using Phobos.Framework.Health;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.Framework.Debug;

/// <summary>Dev-mode reads and the gas overlay. Nothing here changes game state except the explicit "add" actions.</summary>
public static class FrameworkDebugActions
{
    /// <summary>Asks for every Def, with all patches applied, to be written to a file once the game next starts (the log names the
    /// path); restart the game to get it. The request is kept in D'thorga Framework's settings and turns itself off once
    /// done.</summary>
    [DebugAction(DebugCategory.Name, "Framework: write patched Defs at the next start", allowedGameStates = AllowedGameStates.Entry | AllowedGameStates.Playing)]
    public static void AskForPatchedDefs()
    {
        FrameworkMod.Settings.writePatchedDefsOnce = true;
        FrameworkMod.Settings.Write();
        Log.Message(FrameworkMod.LogPrefix + " Restart the game: once it has loaded, every Def with all patches applied is written to "
            + PatchedDefsDump.OutputPath + " (the request then turns itself off).");
    }

    /// <summary>Writes every parts bill to the log: each thing's whole cost, and each role with what filled it and whose part that is.
    /// Reads only.</summary>
    [DebugAction(DebugCategory.Name, "Framework: list every parts bill", allowedGameStates = AllowedGameStates.Entry | AllowedGameStates.Playing)]
    public static void ListPartsBills() => Log.Message(FrameworkMod.LogPrefix + " " + Bills.PartsBills.Report());

    /// <summary>Click a cell on the map: logs every gas there (the game's and other mods' kinds by density, the framework's own by
    /// room) and how enclosed the cell is. Reads only.</summary>
    [DebugAction(DebugCategory.Name, "Framework: log gases at cell", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static void LogGasesAtCell()
    {
        IntVec3 cell = UI.MouseCell();
        Map map = Find.CurrentMap;
        Log.Message(FrameworkMod.LogPrefix + " " + cell + ": " + Describe(cell, map));
    }

    /// <summary>Click a cell on the map: adds one cell's worth of each of the framework's own gases to the room there: real gas, saved
    /// with the room, that clears as any gas does.</summary>
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

    /// <summary>Select people: logs each one's patient group, care level and every registered care choice, saying whether it is saved
    /// or the default. Reads only.</summary>
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

    /// <summary>A radiation burst at the clicked cell, as any weapon carrying one would give it (Framework 0.358.0), so each can be tried
    /// without building the weapon. Gives nothing while its setting is off or D'thorga Medical's radiation model is not on, and says so.</summary>
    [DebugAction(DebugCategory.Name, "Framework: radiation burst at cell (choose a weapon)", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static void RadiationBurstAtCell() =>
        DebugChoices.Open(RadiationBursts.Carriers().Select(def => DebugChoices.Choice(def.defName + " (" + def.GetType().Name + ")", () =>
            DebugTools.curTool = new DebugTool("Radiation burst: " + def.defName, () =>
            {
                PhobosRadiationBurst burst = RadiationBursts.Of(def)!;
                if (!RadiationBursts.On(burst.kind))
                {
                    Messages.Message("Radiation bursts of this kind are off, or D'thorga Medical's radiation model is not on.", MessageTypeDefOf.RejectInput, false);
                    return;
                }
                int dosed = RadiationBursts.Burst(Find.CurrentMap, UI.MouseCell(), burst, RadiationBursts.ExplosionRadius(def), def.defName);
                Messages.Message(def.defName + ": " + dosed + " dosed; the log has the line.", MessageTypeDefOf.NeutralEvent, false);
            }))),
            "No Def carries a radiation burst.");

    /// <summary>Click a cell on the map: adds smoke there through every carrier that takes it (the game's gas grid, or Combat
    /// Extended's smoke), which spreads and clears as it would in play.</summary>
    [DebugAction(DebugCategory.Name, "Framework: add smoke at cell", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static void AddSmokeAtCell() => PhobosGases.Add(UI.MouseCell(), Find.CurrentMap, GasKind.Smoke, 1f);

    /// <summary>Logs how wet open ground is on the current map, with the rain, temperature, daylight and wind that move it. Reads
    /// only.</summary>
    [DebugAction(DebugCategory.Name, "Framework: say how wet the ground is", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static void SayGroundWetness()
    {
        Map map = Find.CurrentMap;
        Log.Message(FrameworkMod.LogPrefix + " Open ground on " + map + " is " + Environment.MapComponent_PhobosGroundWetness.OpenOn(map).ToString("P0")
            + " wet (rain " + map.weatherManager.RainRate.ToString("0.##") + ", " + map.mapTemperature.OutdoorTemp.ToString("0") + " °C, daylight "
            + map.skyManager.CurSkyGlow.ToString("0.##") + ", wind " + map.windManager.WindSpeed.ToString("0.##") + "). Under a roof it is always dry.");
    }

    /// <summary>Makes open ground on the current map fully wet at once, saved with the map; rain and sun move it on from
    /// there.</summary>
    [DebugAction(DebugCategory.Name, "Framework: soak the ground now", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static void SoakGround() => SetGround(1f);

    /// <summary>Makes open ground on the current map fully dry at once, saved with the map; rain and sun move it on from
    /// there.</summary>
    [DebugAction(DebugCategory.Name, "Framework: dry the ground now", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static void DryGround() => SetGround(0f);

    static void SetGround(float value)
    {
        Map map = Find.CurrentMap;
        Environment.MapComponent_PhobosGroundWetness.For(map)?.Set(value);
        Log.Message(FrameworkMod.LogPrefix + " Open ground on " + map + " set to " + value.ToString("P0") + " wet; rain and sun move it on from here.");
    }

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
