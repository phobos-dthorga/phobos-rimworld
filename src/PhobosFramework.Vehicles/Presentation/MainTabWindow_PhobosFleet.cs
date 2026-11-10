using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Interface;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>The bottom-bar button; its setting hides it without a restart.</summary>
public class MainButtonWorker_PhobosFleet : MainButtonWorker_ToggleTab
{
    public override bool Visible => base.Visible && VehicleSystemsMod.Settings.fleetBoardButton;
}

/// <summary>
/// The fleet board: every vehicle the player owns, wherever it is, in one sortable list (docs/development/vehicle-garage-design.md).
/// Presentation only. The list is a snapshot from <see cref="FleetService"/>, rebuilt on opening, on a refresh and a few times a game hour.
/// From 0.79.0 (docs/development/vehicle-presentation-design.md, P2) each row shows the vehicle's picture, its fuel and worst part as gauges,
/// and its state as the dashboard's check-engine lamp, so the fleet can be read without reading.
/// </summary>
public class MainTabWindow_PhobosFleet : MainTabWindow
{
    enum Column
    {
        Name,
        Where,
        Fuel,
        Health,
        Status,
        Crew,
    }

    const float Row = 40f;
    const float Thumbnail = 52f;
    const float StatusLamp = 22f;
    const string StatusIcon = "Phobos/VehicleSystems/Dash/CheckEngine";
    const float HeaderHeight = 30f;
    const float SearchHeight = 32f;
    const int RefreshTicks = 250;
    const float LightSize = 12f;

    /// <summary>Fuel below this share shows amber (authored: a quarter of a tank).</summary>
    const float LowFuel = 0.25f;

    static string StatusWord(int status) => (status == FleetEntry.Stopped ? "PhobosFleet_StatusStopped"
        : status == FleetEntry.Notable ? "PhobosFleet_StatusNotable" : "PhobosFleet_StatusGood").Translate();

    static readonly (Column column, string key, float share)[] Columns =
    {
        (Column.Name, "PhobosFleet_ColumnName", 0.28f),
        (Column.Where, "PhobosFleet_ColumnWhere", 0.30f),
        (Column.Fuel, "PhobosFleet_ColumnFuel", 0.09f),
        (Column.Health, "PhobosFleet_ColumnWorstPart", 0.13f),
        (Column.Status, "PhobosFleet_ColumnStatus", 0.10f),
        (Column.Crew, "PhobosFleet_ColumnCrew", 0.10f),
    };

    List<FleetEntry> entries = new();
    int builtAtTick = -1;
    Column sortBy = Column.Name;
    bool descending;
    string filter = "";
    Vector2 scroll;

    public override Vector2 RequestedTabSize => new(900f, 640f);

    public override void PreOpen()
    {
        base.PreOpen();
        Rebuild();
    }

    void Rebuild()
    {
        entries = FleetService.Build();
        builtAtTick = Find.TickManager.TicksGame;
    }

    IEnumerable<FleetEntry> Shown()
    {
        IEnumerable<FleetEntry> shown = entries.Where(e => !e.Vehicle.Destroyed);
        if (!filter.NullOrEmpty())
        {
            shown = shown.Where(e => e.Vehicle.LabelShort.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                || e.Where.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
        }
        Func<FleetEntry, IComparable> key = sortBy switch
        {
            Column.Where => e => e.Where,
            Column.Fuel => e => e.Fuel,
            Column.Health => e => e.Health,
            Column.Status => e => e.Status,
            Column.Crew => e => e.Crew,
            _ => e => e.Vehicle.LabelShort,
        };
        return descending ? shown.OrderByDescending(key) : shown.OrderBy(key);
    }

    public override void DoWindowContents(Rect inRect)
    {
        if (Find.TickManager.TicksGame - builtAtTick >= RefreshTicks) Rebuild();
        List<FleetEntry> shown = Shown().ToList();

        var top = new Rect(inRect.x, inRect.y, inRect.width, SearchHeight);
        using (new TextBlock(TextAnchor.MiddleLeft))
        {
            Widgets.Label(new Rect(top.x, top.y, 240f, top.height), "PhobosFleet_Count".Translate(shown.Count.Named("COUNT")));
        }
        filter = Widgets.TextField(new Rect(top.x + 250f, top.y + 2f, 240f, top.height - 4f), filter);
        if (filter.NullOrEmpty())
        {
            using (new TextBlock(TextAnchor.MiddleLeft, new Color(1f, 1f, 1f, 0.4f)))
            {
                Widgets.Label(new Rect(top.x + 258f, top.y, 230f, top.height), "PhobosFleet_FilterHint".Translate());
            }
        }
        if (Widgets.ButtonText(new Rect(top.xMax - 120f, top.y + 2f, 120f, top.height - 4f), "PhobosFleet_Refresh".Translate())) Rebuild();

        var header = new Rect(inRect.x, top.yMax + 4f, inRect.width - 16f, HeaderHeight);
        DrawHeader(header);
        var outer = new Rect(inRect.x, header.yMax, inRect.width, inRect.yMax - header.yMax);
        if (shown.Count == 0)
        {
            using (new TextBlock(TextAnchor.MiddleCenter)) Widgets.Label(outer, "PhobosFleet_Empty".Translate());
            return;
        }
        var view = new Rect(0f, 0f, outer.width - 16f, shown.Count * Row);
        Widgets.BeginScrollView(outer, ref scroll, view);
        for (int i = 0; i < shown.Count; i++) DrawRow(new Rect(0f, i * Row, view.width, Row), shown[i], i);
        Widgets.EndScrollView();
    }

    void DrawHeader(Rect rect)
    {
        float x = rect.x;
        foreach ((Column column, string key, float share) in Columns)
        {
            var cell = new Rect(x, rect.y, rect.width * share, rect.height);
            string label = key.Translate();
            if (column == sortBy) label += descending ? " v" : " ^";
            using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(cell.ContractedBy(4f, 0f), label);
            Widgets.DrawHighlightIfMouseover(cell);
            if (Widgets.ButtonInvisible(cell))
            {
                if (sortBy == column) descending = !descending;
                else
                {
                    sortBy = column;
                    descending = false;
                }
            }
            x = cell.xMax;
        }
        Widgets.DrawLineHorizontal(rect.x, rect.yMax - 1f, rect.width);
    }

    static void DrawRow(Rect row, FleetEntry entry, int index)
    {
        if (index % 2 == 1) Widgets.DrawLightHighlight(row);
        Widgets.DrawHighlightIfMouseover(row);
        float x = row.x;
        foreach ((Column column, string _, float share) in Columns)
        {
            var cell = new Rect(x, row.y, row.width * share, row.height);
            DrawCell(cell, column, entry);
            x = cell.xMax;
        }
        string tip = entry.Vehicle.LabelCap;
        if (entry.Status != FleetEntry.NoStatus) tip += "\n" + StatusWord(entry.Status);
        if (entry.WorstPart != null) tip += "\n" + "PhobosFleet_TipWorstPart".Translate(entry.WorstPart.Named("PART"), entry.Health.ToStringPercent().Named("HEALTH"));
        if (entry.Waiting != null) tip += "\n" + entry.Waiting;
        TooltipHandler.TipRegion(row, tip);
        if (Widgets.ButtonInvisible(row)) FleetService.Jump(entry);
    }

    static void DrawCell(Rect cell, Column column, FleetEntry entry)
    {
        Rect text = cell.ContractedBy(4f, 0f);
        switch (column)
        {
            case Column.Status:
            {
                if (entry.Status == FleetEntry.NoStatus) return;
                Color colour = entry.Status == FleetEntry.Stopped ? PhobosWidgets.Bad : entry.Status == FleetEntry.Notable ? PhobosWidgets.Caution : PhobosWidgets.Good;
                if (SystemIcons.IfExists(StatusIcon) is { } icon)
                {
                    // The dashboard's check-engine lamp, tinted green, amber or red by the vehicle's state.
                    GUI.color = colour;
                    GUI.DrawTexture(new Rect(text.x, cell.y + (cell.height - StatusLamp) / 2f, StatusLamp, StatusLamp), icon);
                    GUI.color = Color.white;
                }
                else
                {
                    var light = new Rect(text.x, cell.y + (cell.height - LightSize) / 2f, LightSize, LightSize);
                    Widgets.DrawBoxSolid(light, colour);
                    Widgets.DrawBox(light);
                }
                return;
            }
            case Column.Name:
            {
                var picture = new Rect(text.x, cell.y + 2f, Thumbnail, cell.height - 4f);
                if (entry.Vehicle.VehicleDef is { } def) VehiclePicture.Draw(picture, def, facingNorth: false);
                using (new TextBlock(TextAnchor.MiddleLeft))
                {
                    var label = new Rect(picture.xMax + 6f, cell.y, text.xMax - picture.xMax - 6f, cell.height);
                    Widgets.Label(label, entry.Vehicle.LabelShort.Truncate(label.width));
                }
                return;
            }
            case Column.Fuel when entry.Fuel >= 0f:
                PhobosWidgets.Gauge(new Rect(text.x, cell.y + 10f, text.width, cell.height - 20f), entry.Fuel,
                    entry.Fuel < LowFuel ? PhobosWidgets.Caution : PhobosWidgets.Good, figure: entry.Fuel.ToStringPercent());
                return;
            case Column.Health:
                PhobosWidgets.Gauge(new Rect(text.x, cell.y + 10f, text.width, cell.height - 20f), entry.Health,
                    PhobosWidgets.HealthColour(entry.Health), figure: entry.Health.ToStringPercent());
                return;
        }
        string value = column switch
        {
            Column.Name => entry.Vehicle.LabelShort,
            Column.Where => entry.Where,
            Column.Fuel => "-",
            Column.Crew => entry.Crew + "/" + entry.Seats,
            _ => "",
        };
        using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(text, value.Truncate(text.width));
    }
}
