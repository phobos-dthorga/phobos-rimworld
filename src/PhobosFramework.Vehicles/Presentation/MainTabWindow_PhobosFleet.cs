using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Interface;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>The bottom-bar button; its setting hides it without a restart.</summary>
public class MainButtonWorker_PhobosFleet : MainButtonWorker_ToggleTab
{
    public override bool Visible => base.Visible && VehicleSystemsMod.Settings.fleetBoardButton;
}

/// <summary>
/// The fleet board: every vehicle the player owns, wherever it is (docs/development/vehicle-stations.md). Presentation only: the rows are a
/// snapshot from <see cref="FleetService"/>, rebuilt on opening, on a refresh and a few times a game hour, and the detail pane's words come
/// ready from it too. Refreshed in Framework 0.188.0 (the owner, 20 Nov 2026: "overdue for a refresh and update given all the new features
/// as of late"; choices by card the same day): the list is grouped by place, each row says what the vehicle is doing and what it needs,
/// trailers sit under what pulls them, lost vehicles, hulls and vehicles that could be claimed are listed, and a pane beside the list
/// shows the chosen vehicle's damage, faults with their causes, journey and newest logbook lines. The arrow keys move the choice and
/// Enter goes to it, so the page works from the keyboard.
/// </summary>
public class MainTabWindow_PhobosFleet : MainTabWindow
{
    enum Column
    {
        Name,
        Where,
        Doing,
        Needs,
        Fuel,
        Health,
        Status,
        Crew,
    }

    const float Row = 40f;
    const float GroupRow = 26f;
    const float Thumbnail = 52f;
    const float StatusLamp = 22f;
    const string StatusIcon = "Phobos/VehicleSystems/Dash/CheckEngine";
    const float HeaderHeight = 30f;
    const float TopHeight = 32f;
    const float DetailWidth = 370f;
    const float Gap = 10f;
    const float Indent = 22f;
    const float PictureSize = 190f;
    const float ButtonHeight = 32f;
    const int RefreshTicks = 250;
    const float LightSize = 12f;

    static string StatusWord(int status) => (status == FleetEntry.Stopped ? "PhobosFleet_StatusStopped"
        : status == FleetEntry.Notable ? "PhobosFleet_StatusNotable" : "PhobosFleet_StatusGood").Translate();

    static readonly (Column column, string key, float share)[] GroupedColumns =
    {
        (Column.Name, "PhobosFleet_ColumnName", 0.25f),
        (Column.Doing, "PhobosFleet_ColumnDoing", 0.23f),
        (Column.Needs, "PhobosFleet_ColumnNeeds", 0.22f),
        (Column.Fuel, "PhobosFleet_ColumnFuel", 0.08f),
        (Column.Health, "PhobosFleet_ColumnWorstPart", 0.10f),
        (Column.Status, "PhobosFleet_ColumnStatus", 0.06f),
        (Column.Crew, "PhobosFleet_ColumnCrew", 0.06f),
    };

    static readonly (Column column, string key, float share)[] FlatColumns =
    {
        (Column.Name, "PhobosFleet_ColumnName", 0.21f),
        (Column.Where, "PhobosFleet_ColumnWhere", 0.14f),
        (Column.Doing, "PhobosFleet_ColumnDoing", 0.18f),
        (Column.Needs, "PhobosFleet_ColumnNeeds", 0.17f),
        (Column.Fuel, "PhobosFleet_ColumnFuel", 0.08f),
        (Column.Health, "PhobosFleet_ColumnWorstPart", 0.10f),
        (Column.Status, "PhobosFleet_ColumnStatus", 0.06f),
        (Column.Crew, "PhobosFleet_ColumnCrew", 0.06f),
    };

    /// <summary>One line of the list: a place's heading, or a row.</summary>
    readonly struct Line
    {
        public readonly FleetEntry? Entry;
        public readonly string GroupKey;
        public readonly string GroupLabel;
        public readonly int Count;
        public readonly int Attention;
        public readonly bool Indented;

        public Line(FleetEntry entry, bool indented)
        {
            Entry = entry;
            GroupKey = entry.GroupKey;
            GroupLabel = "";
            Count = Attention = 0;
            Indented = indented;
        }

        public Line(string groupKey, string groupLabel, int count, int attention)
        {
            Entry = null;
            GroupKey = groupKey;
            GroupLabel = groupLabel;
            Count = count;
            Attention = attention;
            Indented = false;
        }

        public float Height => Entry == null ? GroupRow : Row;
    }

    List<FleetEntry> entries = new();
    int builtAtTick = -1;
    Column sortBy = Column.Name;
    bool descending;
    string filter = "";
    Vector2 scroll, detailScroll;
    bool grouped = true;
    bool attentionOnly;
    /// <summary>The yard filter (Framework 0.210.0): every row, or only those standing so with the yards.</summary>
    FleetYard? yardFilter;
    readonly HashSet<string> folded = new();
    int selectedId = -1;
    FleetDetail? detail;
    bool showSelected;
    float detailHeight = 400f;

    (Column column, string key, float share)[] Columns => grouped ? GroupedColumns : FlatColumns;

    public override Vector2 RequestedTabSize => new(Mathf.Min(1340f, UI.screenWidth - 40f), 680f);

    /// <summary>The arrow keys move through the list here, so they do not also move the map behind the page.</summary>
    public MainTabWindow_PhobosFleet() => preventCameraMotion = true;

    public override void PreOpen()
    {
        base.PreOpen();
        Rebuild();
    }

    void Rebuild()
    {
        entries = FleetService.Build();
        builtAtTick = Find.TickManager.TicksGame;
        detail = null;
    }

    FleetEntry? Selected => selectedId < 0 ? null : entries.FirstOrDefault(e => e.Id == selectedId && !e.Subject.Destroyed);

    /// <summary>Enter goes to the chosen vehicle, where the game would only close the page.</summary>
    public override void OnAcceptKeyPressed()
    {
        if (Selected is { } entry) FleetService.Jump(entry);
        else base.OnAcceptKeyPressed();
    }

    bool Matches(FleetEntry e) =>
        e.Label.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 || e.Where.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
        || e.Doing.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 || (e.Needs != null && e.Needs.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);

    List<Line> Lines()
    {
        IEnumerable<FleetEntry> shown = entries.Where(e => !e.Subject.Destroyed);
        if (!filter.NullOrEmpty()) shown = shown.Where(Matches);
        if (attentionOnly) shown = shown.Where(e => e.NeedsAttention);
        if (yardFilter is { } yards) shown = shown.Where(e => e.YardKind == yards);
        Func<FleetEntry, IComparable> key = sortBy switch
        {
            Column.Where => e => e.Where,
            Column.Doing => e => e.Doing,
            Column.Needs => e => e.Needs ?? "",
            Column.Fuel => e => e.Fuel,
            Column.Health => e => e.Health,
            Column.Status => e => e.Status,
            Column.Crew => e => e.Crew,
            _ => e => e.Label,
        };
        List<FleetEntry> sorted = (descending ? shown.OrderByDescending(key) : shown.OrderBy(key)).ToList();
        var inList = new HashSet<FleetEntry>(sorted);
        var lines = new List<Line>();

        void AddRows(IEnumerable<FleetEntry> rows)
        {
            foreach (FleetEntry entry in rows)
            {
                // A towed vehicle sits under what pulls it, and a follower under its convoy's leader, when that is in the list too.
                if (entry.TowedBy != null && inList.Contains(entry.TowedBy)) continue;
                if (entry.FollowerOf != null && inList.Contains(entry.FollowerOf)) continue;
                lines.Add(new Line(entry, indented: false));
                AddTowed(entry, 0);
            }
        }

        // Framework 0.193.0: a chain (a plough and the cart behind it) lists every vehicle in it under the one that drives.
        void AddTowed(FleetEntry front, int depth)
        {
            if (depth > 8) return;
            foreach (FleetEntry towed in sorted)
            {
                if (towed.TowedBy != front || towed == front) continue;
                lines.Add(new Line(towed, indented: true));
                AddTowed(towed, depth + 1);
            }
            // Framework 0.281.0: a convoy's followers under its leader.
            if (depth > 0) return;
            foreach (FleetEntry follower in sorted)
            {
                if (follower.FollowerOf != front || follower == front) continue;
                lines.Add(new Line(follower, indented: true));
                AddTowed(follower, depth + 1);
            }
        }

        if (!grouped)
        {
            AddRows(sorted);
            return lines;
        }
        foreach (IGrouping<string, FleetEntry> group in sorted.GroupBy(e => e.GroupKey).OrderBy(g => g.First().GroupOrder).ThenBy(g => g.First().GroupLabel))
        {
            lines.Add(new Line(group.Key, group.First().GroupLabel, group.Count(), group.Count(e => e.NeedsAttention)));
            if (!folded.Contains(group.Key)) AddRows(group);
        }
        return lines;
    }

    public override void DoWindowContents(Rect inRect)
    {
        // Guarded: a throw is logged once and the window rests, where the game would log it every frame (UiGuard).
        if (UiGuard.Resting(GuardKey)) { UiGuard.Fallback(inRect); return; }
        try { Contents(inRect); }
        catch (Exception e) { UiGuard.Report(GuardKey, e); }
    }

    const string GuardKey = "The fleet board";

    void Contents(Rect inRect)
    {
        using Phobos.Framework.Debug.PhobosTiming.Scope timing = Phobos.Framework.Debug.PhobosTiming.Measure("fleet board");
        if (Find.TickManager.TicksGame - builtAtTick >= RefreshTicks) Rebuild();
        List<Line> lines = Lines();
        MoveByKeys(lines);

        var top = new Rect(inRect.x, inRect.y, inRect.width, TopHeight);
        float x = top.x;
        int count = lines.Count(l => l.Entry != null);
        using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(new Rect(x, top.y, 150f, top.height), "PhobosFleet_Count".Translate(count.Named("COUNT")));
        x += 156f;
        var field = new Rect(x, top.y + 2f, 220f, top.height - 4f);
        filter = Widgets.TextField(field, filter);
        if (filter.NullOrEmpty())
        {
            using (new TextBlock(TextAnchor.MiddleLeft, new Color(1f, 1f, 1f, 0.4f))) Widgets.Label(new Rect(field.x + 8f, top.y, 210f, top.height), "PhobosFleet_FilterHint".Translate());
        }
        x = field.xMax + 12f;
        var attention = new Rect(x, top.y + 4f, 250f, top.height - 8f);
        Widgets.CheckboxLabeled(attention, "PhobosFleet_OnlyAttention".Translate(), ref attentionOnly);
        x = attention.xMax + 12f;
        if (Widgets.ButtonText(new Rect(x, top.y + 2f, 160f, top.height - 4f), (grouped ? "PhobosFleet_ShowFlat" : "PhobosFleet_ShowGrouped").Translate()))
        {
            grouped = !grouped;
            if (!grouped && sortBy == Column.Where) sortBy = Column.Name;
        }
        x += 172f;
        // The yard filter (Framework 0.210.0): a menu of the four choices, as the game's own filters are.
        var yardButton = new Rect(x, top.y + 2f, 200f, top.height - 4f);
        if (Widgets.ButtonText(yardButton, YardFilterLabel(yardFilter)))
        {
            Find.WindowStack.Add(new FloatMenu(new FleetYard?[] { null, FleetYard.In, FleetYard.Waiting, FleetYard.CannotGet }
                .Select(choice => new FloatMenuOption(YardFilterLabel(choice), () => yardFilter = choice)).ToList()));
        }
        TooltipHandler.TipRegion(yardButton, "PhobosFleet_YardFilterTip".Translate());
        if (Widgets.ButtonText(new Rect(top.xMax - 110f, top.y + 2f, 110f, top.height - 4f), "PhobosFleet_Refresh".Translate())) Rebuild();

        float listWidth = inRect.width - DetailWidth - Gap;
        var header = new Rect(inRect.x, top.yMax + 4f, listWidth - 16f, HeaderHeight);
        DrawHeader(header);
        var outer = new Rect(inRect.x, header.yMax, listWidth, inRect.yMax - header.yMax);
        if (lines.Count == 0)
        {
            using (new TextBlock(TextAnchor.MiddleCenter)) Widgets.Label(outer, "PhobosFleet_Empty".Translate());
        }
        else
        {
            DrawList(outer, lines);
        }
        DrawDetail(new Rect(outer.xMax + Gap, top.yMax + 4f, DetailWidth, inRect.yMax - top.yMax - 4f));
    }

    /// <summary>The up and down arrows move the choice through the rows, wherever the keyboard's focus is.</summary>
    void MoveByKeys(List<Line> lines)
    {
        Event e = Event.current;
        if (e.type != EventType.KeyDown || (e.keyCode != KeyCode.DownArrow && e.keyCode != KeyCode.UpArrow)) return;
        List<FleetEntry> rows = lines.Where(l => l.Entry != null).Select(l => l.Entry!).ToList();
        if (rows.Count == 0) return;
        int at = rows.FindIndex(r => r.Id == selectedId);
        int next = at < 0 ? 0 : Mathf.Clamp(at + (e.keyCode == KeyCode.DownArrow ? 1 : -1), 0, rows.Count - 1);
        Select(rows[next]);
        showSelected = true;
        e.Use();
    }

    void Select(FleetEntry entry)
    {
        if (selectedId == entry.Id) return;
        selectedId = entry.Id;
        detail = null;
        detailScroll = Vector2.zero;
    }

    void DrawList(Rect outer, List<Line> lines)
    {
        float height = 0f;
        foreach (Line line in lines) height += line.Height;
        var view = new Rect(0f, 0f, outer.width - 16f, height);
        if (showSelected)
        {
            // Keep the row the keys moved to in sight.
            float y0 = 0f;
            foreach (Line line in lines)
            {
                if (line.Entry != null && line.Entry.Id == selectedId)
                {
                    if (y0 < scroll.y) scroll.y = y0;
                    else if (y0 + Row > scroll.y + outer.height) scroll.y = y0 + Row - outer.height;
                    break;
                }
                y0 += line.Height;
            }
            showSelected = false;
        }
        Widgets.BeginScrollView(outer, ref scroll, view);
        float y = 0f;
        int index = 0;
        foreach (Line line in lines)
        {
            var rect = new Rect(0f, y, view.width, line.Height);
            if (line.Entry == null) DrawGroup(rect, line);
            else DrawRow(rect, line.Entry, index++, line.Indented);
            y += line.Height;
        }
        Widgets.EndScrollView();
    }

    void DrawGroup(Rect rect, Line line)
    {
        Widgets.DrawBoxSolid(rect, new Color(1f, 1f, 1f, 0.07f));
        Widgets.DrawHighlightIfMouseover(rect);
        string label = (folded.Contains(line.GroupKey) ? "+ " : "- ") + "PhobosFleet_GroupHeader".Translate(line.GroupLabel.Named("GROUP"), line.Count.Named("COUNT"));
        using (new TextBlock(GameFont.Small, TextAnchor.MiddleLeft)) Widgets.Label(new Rect(rect.x + 6f, rect.y, rect.width * 0.6f, rect.height), label);
        if (line.Attention > 0)
        {
            using (new TextBlock(GameFont.Small, TextAnchor.MiddleRight, PhobosWidgets.Caution))
            {
                Widgets.Label(new Rect(rect.x + rect.width * 0.6f, rect.y, rect.width * 0.4f - 8f, rect.height),
                    "PhobosFleet_GroupAttention".Translate(line.Attention.Named("COUNT")));
            }
        }
        if (!Widgets.ButtonInvisible(rect)) return;
        if (!folded.Remove(line.GroupKey)) folded.Add(line.GroupKey);
    }

    void DrawHeader(Rect rect)
    {
        float x = rect.x;
        foreach ((Column column, string key, float share) in Columns)
        {
            var cell = new Rect(x, rect.y, rect.width * share, rect.height);
            string label = key.Translate();
            if (column == sortBy) label += descending ? " v" : " ^";
            // One line, never wrapped: a narrow column's heading runs on rather than dropping out of sight.
            using (new TextBlock(Color.white, TextAnchor.MiddleLeft, false)) Widgets.Label(cell.ContractedBy(4f, 0f), label);
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

    /// <summary>Whether this row's damage sheet is showing, so its text tooltip stays away rather than cover it.</summary>
    bool sheetShown;

    void DrawRow(Rect row, FleetEntry entry, int index, bool indented)
    {
        sheetShown = false;
        if (entry.Id == selectedId) Widgets.DrawHighlightSelected(row);
        else if (index % 2 == 1) Widgets.DrawLightHighlight(row);
        Widgets.DrawHighlightIfMouseover(row);
        float x = row.x;
        foreach ((Column column, string _, float share) in Columns)
        {
            var cell = new Rect(x, row.y, row.width * share, row.height);
            DrawCell(cell, column, entry, indented);
            x = cell.xMax;
        }
        string tip = entry.Label;
        if (entry.Status != FleetEntry.NoStatus) tip += "\n" + StatusWord(entry.Status);
        if (entry.WorstPart != null) tip += "\n" + "PhobosFleet_TipWorstPart".Translate(entry.WorstPart.Named("PART"), entry.Health.ToStringPercent().Named("HEALTH"));
        tip += "\n" + entry.Doing.CapitalizeFirst();
        if (entry.Needs != null) tip += "\n" + entry.Needs.CapitalizeFirst();
        if (!sheetShown) TooltipHandler.TipRegion(row, tip);
        if (!Widgets.ButtonInvisible(row)) return;
        // One click chooses the row; a click on the chosen row goes to it.
        if (entry.Id == selectedId) FleetService.Jump(entry);
        else Select(entry);
    }

    void DrawCell(Rect cell, Column column, FleetEntry entry, bool indented)
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
                float left = text.x + (indented ? Indent : 0f);
                var picture = new Rect(left, cell.y + 2f, Thumbnail, cell.height - 4f);
                if (entry.Def is { } def)
                {
                    VehiclePicture.Draw(picture, def, facingNorth: false);
                    // Where it is hurt, at a glance: the damage sheet beside the mouse while it rests on the picture.
                    if (entry.Vehicle is { Destroyed: false } vehicle && entry.Kind != FleetKind.Hull && Mouse.IsOver(picture))
                    {
                        DamageSheet.Popup(def, DamageSheet.PartsOf(vehicle));
                        sheetShown = true;
                    }
                }
                using (new TextBlock(TextAnchor.MiddleLeft))
                {
                    var label = new Rect(picture.xMax + 6f, cell.y, text.xMax - picture.xMax - 6f, cell.height);
                    Widgets.Label(label, entry.Label.Truncate(label.width));
                }
                return;
            }
            case Column.Fuel when entry.Fuel >= 0f:
                PhobosWidgets.Gauge(new Rect(text.x, cell.y + 10f, text.width, cell.height - 20f), entry.Fuel,
                    entry.Fuel < FleetService.LowFuel ? PhobosWidgets.Caution : PhobosWidgets.Good, figure: entry.Fuel.ToStringPercent());
                return;
            case Column.Health:
                PhobosWidgets.Gauge(new Rect(text.x, cell.y + 10f, text.width, cell.height - 20f), entry.Health,
                    PhobosWidgets.HealthColour(entry.Health), figure: entry.Health.ToStringPercent());
                return;
            case Column.Needs when entry.Needs != null:
                using (new TextBlock(GameFont.Tiny, TextAnchor.MiddleLeft, NeedsColour(entry))) Widgets.Label(text, entry.Needs.CapitalizeFirst());
                return;
            case Column.Doing:
                using (new TextBlock(GameFont.Tiny, TextAnchor.MiddleLeft)) Widgets.Label(text, entry.Doing.CapitalizeFirst());
                return;
        }
        string value = column switch
        {
            Column.Where => entry.Where,
            Column.Fuel => "-",
            Column.Crew => entry.Seats > 0 ? entry.Crew + "/" + entry.Seats : "",
            _ => "",
        };
        using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(text, value.Truncate(text.width));
    }

    /// <summary>Red for what stops it, amber for what wants a look, grey for an offer (a hull's parts, a vehicle to claim).</summary>
    static Color NeedsColour(FleetEntry entry) =>
        !entry.NeedsAttention ? ColoredText.SubtleGrayColor : entry.Status == FleetEntry.Stopped ? PhobosWidgets.Bad : PhobosWidgets.Caution;

    // ---- The pane beside the list ----

    /// <summary>
    /// The convoy buttons for the chosen vehicle (Framework 0.281.0): add a follower (any of the map's vehicles with a working route kit and
    /// nothing else to do), follow another vehicle, leave the convoy, or break it up. Each delegates to the vehicle's route comp.
    /// </summary>
    float DrawConvoy(float y, float width, VehiclePawn vehicle)
    {
        var route = vehicle.GetComp<CompPhobosRoute>();
        var followers = Convoys.Followers(vehicle);
        bool kit = route != null && RouteControl.KitWorks(vehicle);
        if (route == null && followers.Count == 0) return y;
        float top = y;
        PhobosWidgets.CardHeader(ref y, 0f, width, "PhobosFleet_DetailConvoy".Translate(), null);
        if (followers.Count > 0) y += Words(6f, y, width - 12f, "PhobosFleet_ConvoyLeads".Translate(followers.Count.Named("COUNT")), Color.white);
        if (route is { Mode: RouteMode.Follow, Leader: { } leader }) y += Words(6f, y, width - 12f, "PhobosFleet_ConvoyFollows".Translate(leader.LabelShort.Named("LEADER")), Color.white);
        var button = new Rect(6f, y, width - 12f, 26f);
        if (route?.Mode != RouteMode.Follow && RouteRule.MayJoin(followers.Count, PhobosRemoteControlRulesDef.Current.maxFollowers))
        {
            if (Widgets.ButtonText(button, "PhobosFleet_ConvoyAdd".Translate()))
            {
                var options = new List<FloatMenuOption>();
                foreach (Pawn pawn in vehicle.Map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer))
                {
                    if (pawn is not VehiclePawn other || other == vehicle || other.GetComp<CompPhobosRoute>() is not { Mode: RouteMode.None } candidate
                        || !RouteControl.KitWorks(other)) continue;
                    options.Add(new FloatMenuOption(other.LabelShortCap, () =>
                    {
                        if (candidate.Follow(vehicle) is { } why) Messages.Message(why, other, MessageTypeDefOf.RejectInput, false);
                    }));
                }
                if (options.Count == 0) options.Add(new FloatMenuOption("PhobosFleet_ConvoyNoneFree".Translate(), null));
                Find.WindowStack.Add(new FloatMenu(options));
            }
            y += 30f;
            button.y = y;
        }
        if (kit && route!.Mode == RouteMode.None && Widgets.ButtonText(button, "PhobosFleet_ConvoyFollow".Translate()))
        {
            var options = new List<FloatMenuOption>();
            foreach (Pawn pawn in vehicle.Map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer))
            {
                if (pawn is not VehiclePawn other || other == vehicle) continue;
                options.Add(new FloatMenuOption(other.LabelShortCap, () =>
                {
                    if (route.Follow(other) is { } why) Messages.Message(why, vehicle, MessageTypeDefOf.RejectInput, false);
                }));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }
        if (kit && route!.Mode == RouteMode.None) { y += 30f; button.y = y; }
        if (route is { Mode: RouteMode.Follow })
        {
            if (Widgets.ButtonText(button, "PhobosFleet_ConvoyLeave".Translate())) route.Stop();
            y += 30f;
            button.y = y;
        }
        if (followers.Count > 0)
        {
            if (Widgets.ButtonText(button, "PhobosFleet_ConvoyBreakUp".Translate())) Convoys.BreakUp(vehicle);
            y += 30f;
        }
        PhobosWidgets.CardEnd(top, ref y, 0f, width);
        return y;
    }

    void DrawDetail(Rect rect)
    {
        Widgets.DrawMenuSection(rect);
        Rect inner = rect.ContractedBy(8f);
        FleetEntry? entry = Selected;
        if (entry == null)
        {
            using (new TextBlock(GameFont.Small, TextAnchor.UpperLeft, new Color(1f, 1f, 1f, 0.6f))) Widgets.Label(inner, "PhobosFleet_DetailNone".Translate());
            return;
        }
        if (detail == null || detail.ForId != entry.Id) detail = FleetService.Detail(entry);

        var go = new Rect(inner.x, inner.yMax - ButtonHeight, inner.width, ButtonHeight);
        if (Widgets.ButtonText(go, "PhobosFleet_DetailGoTo".Translate())) FleetService.Jump(entry);
        TooltipHandler.TipRegion(go, "PhobosFleet_DetailGoToTip".Translate());

        var outer = new Rect(inner.x, inner.y, inner.width, go.y - inner.y - 6f);
        var view = new Rect(0f, 0f, outer.width - 16f, detailHeight);
        Widgets.BeginScrollView(outer, ref detailScroll, view);
        float width = view.width;
        float y = 0f;
        using (new TextBlock(GameFont.Medium, TextAnchor.UpperLeft))
        {
            float titleHeight = Text.CalcHeight(entry.Label, width);
            Widgets.Label(new Rect(0f, y, width, titleHeight), entry.Label);
            y += titleHeight + 2f;
        }
        if (entry.Def is { } def)
        {
            var picture = new Rect((width - PictureSize) / 2f, y, PictureSize, PictureSize);
            if (detail.Parts != null) DamageSheet.Draw(picture, def, detail.Parts, null);
            else VehiclePicture.Draw(picture, def, facingNorth: true);
            y += PictureSize + 6f;
        }
        y += Words(0f, y, width, "PhobosFleet_DetailWhere".Translate(entry.Where.Named("WHERE")), Color.white);
        y += Words(0f, y, width, "PhobosFleet_DetailDoing".Translate(entry.Doing.Named("DOING")), Color.white);
        if (entry.Needs != null)
        {
            y += Words(0f, y, width, "PhobosFleet_DetailNeeds".Translate(entry.Needs.Named("NEEDS")), NeedsColour(entry));
        }
        if (entry.Seats > 0) y += Words(0f, y, width, "PhobosFleet_DetailCrew".Translate(entry.Crew.Named("CREW"), entry.Seats.Named("SEATS")), Color.white);
        if (detail.Regular != null) y += Words(0f, y, width, "PhobosFleet_DetailRegular".Translate(detail.Regular.Named("NAME")), Color.white);
        if (detail.Journey != null) y += Words(0f, y, width, detail.Journey, Color.white);
        if (detail.Note != null) y += Words(0f, y, width, detail.Note, new Color(1f, 1f, 1f, 0.75f));
        y += 6f;

        // Bay trips (Framework 0.201.0): this vehicle's own switch, for the player's land vehicles on a map.
        if (entry.Kind == FleetKind.Own && BayTrips.On && entry.Vehicle is { Spawned: true } own && own.GetComp<CompPhobosBayTrip>() is { TakesPart: true } bayTrip)
        {
            var box = new Rect(0f, y, width, 24f);
            bool goes = !bayTrip.KeptOut;
            bool was = goes;
            Widgets.CheckboxLabeled(box, "PhobosBayTrip_FleetSwitch".Translate(), ref goes);
            TooltipHandler.TipRegion(box, "PhobosBayTrip_FleetSwitchTip".Translate());
            if (goes != was) BayTrips.SetKeptOut(bayTrip, !goes);
            y += 28f;
        }

        // The service yard (Framework 0.210.0): the yard it stands in or waits for, its dyno line and pumps, and the orders that go with them.
        if (entry.Kind == FleetKind.Own && entry.Vehicle is { Spawned: true } inMap) y = DrawYard(y, width, entry, inMap);

        // Convoys and loops (Framework 0.281.0): form and change a convoy here as with the vehicle's own buttons.
        if (entry.Kind == FleetKind.Own && entry.Vehicle is { Spawned: true } routed && RouteControl.On) y = DrawConvoy(y, width, routed);

        if (entry.Systems != null)
        {
            float faultsTop = y;
            Color? worst = detail.Faults.Count == 0 ? null : detail.Faults.Any(f => f.Severe) ? PhobosWidgets.Bad : PhobosWidgets.Caution;
            PhobosWidgets.CardHeader(ref y, 0f, width, "PhobosFleet_DetailFaults".Translate(), worst);
            if (detail.Faults.Count == 0) y += Words(6f, y, width - 12f, "PhobosFleet_DetailNoFaults".Translate(), new Color(1f, 1f, 1f, 0.6f));
            foreach (FleetFault fault in detail.Faults)
            {
                y += Words(6f, y, width - 12f, fault.Text, fault.Severe ? PhobosWidgets.Bad : PhobosWidgets.Caution);
                if (fault.Cause != null) y += Small(14f, y, width - 20f, fault.Cause);
            }
            PhobosWidgets.CardEnd(faultsTop, ref y, 0f, width);

            float logTop = y;
            PhobosWidgets.CardHeader(ref y, 0f, width, "PhobosFleet_DetailLogbook".Translate(), null);
            if (detail.Log.Count == 0) y += Words(6f, y, width - 12f, "PhobosFleet_DetailNoLog".Translate(), new Color(1f, 1f, 1f, 0.6f));
            foreach (FleetLogLine line in detail.Log)
            {
                y += Small(6f, y, width - 12f, line.Date);
                y += Words(6f, y, width - 12f, line.Text, line.Bad ? ColorLibrary.RedReadable : Color.white);
            }
            if (detail.LogTotal > detail.Log.Count) y += Small(6f, y, width - 12f, "PhobosFleet_DetailLogMore".Translate());
            PhobosWidgets.CardEnd(logTop, ref y, 0f, width);
        }
        Widgets.EndScrollView();
        detailHeight = y + 4f;
    }

    static string YardFilterLabel(FleetYard? choice) => (choice switch
    {
        FleetYard.In => "PhobosFleet_YardFilterIn",
        FleetYard.Waiting => "PhobosFleet_YardFilterWaiting",
        FleetYard.CannotGet => "PhobosFleet_YardFilterCannot",
        _ => "PhobosFleet_YardFilterAll",
    }).Translate();

    /// <summary>The yard card and its buttons; each button is one call to <see cref="ServiceYards"/>. Returns where the pane goes on.</summary>
    float DrawYard(float y, float width, FleetEntry entry, Verse.Pawn vehicle)
    {
        if (vehicle is not Vehicles.VehiclePawn own) return y;
        if (detail!.Yard is { } yard)
        {
            float top = y;
            PhobosWidgets.CardHeader(ref y, 0f, width, "PhobosFleet_YardCard".Translate(yard.label.Named("YARD")), yard.Works ? null : PhobosWidgets.Caution);
            foreach (string line in detail.YardLines) y += Words(6f, y, width - 12f, line, Color.white);
            float half = (width - 18f) / 2f;
            var left = new Rect(6f, y + 2f, half, 28f);
            var right = new Rect(left.xMax + 6f, y + 2f, half, 28f);
            if (Widgets.ButtonText(left, "PhobosFleet_YardGoTo".Translate())) ServiceYards.JumpTo(yard);
            TooltipHandler.TipRegion(left, "PhobosFleet_YardGoToTip".Translate());
            if (entry.Yard != null)
            {
                if (Widgets.ButtonText(right, "PhobosFleet_YardTakeOut".Translate())) ServiceYards.TakeOut(own);
                TooltipHandler.TipRegion(right, "PhobosFleet_YardTakeOutTip".Translate());
            }
            y += 32f;
            var moveUp = new Rect(6f, y, width - 12f, 28f);
            if (Widgets.ButtonText(moveUp, "PhobosFleet_YardMoveUp".Translate(), active: detail.CanMoveUp) && detail.CanMoveUp)
            {
                ServiceYards.MoveUp(own);
                Rebuild();
            }
            TooltipHandler.TipRegion(moveUp, detail.CanMoveUp ? "PhobosFleet_YardMoveUpTip".Translate() : "PhobosFleet_YardMoveUpNot".Translate());
            y += 32f;
            PhobosWidgets.CardEnd(top, ref y, 0f, width);
            return y;
        }
        // Not in a yard: Send to yard, choosing the yard when there are several.
        List<Zone_PhobosServiceYard> yards = ServiceYards.WorkingYards(own.Map);
        if (yards.Count == 0 || own.GetComp<CompPhobosBayTrip>() is not { TakesPart: true }) return y;
        var send = new Rect(0f, y, width, 28f);
        if (Widgets.ButtonText(send, "PhobosFleet_YardSend".Translate()))
        {
            if (yards.Count == 1) ServiceYards.SendTo(own, yards[0]);
            else Find.WindowStack.Add(new FloatMenu(yards.Select(choice => new FloatMenuOption(choice.label, () => ServiceYards.SendTo(own, choice))).ToList()));
        }
        TooltipHandler.TipRegion(send, "PhobosFleet_YardSendTip".Translate());
        return y + 32f;
    }

    /// <summary>A wrapped line in the small font; returns the height it took.</summary>
    static float Words(float x, float y, float width, string text, Color colour)
    {
        using (new TextBlock(GameFont.Small, TextAnchor.UpperLeft, colour))
        {
            float height = Text.CalcHeight(text, width);
            Widgets.Label(new Rect(x, y, width, height), text);
            return height + 1f;
        }
    }

    /// <summary>A wrapped line in the tiny font, greyed; returns the height it took.</summary>
    static float Small(float x, float y, float width, string text)
    {
        using (new TextBlock(GameFont.Tiny, TextAnchor.UpperLeft, ColoredText.SubtleGrayColor))
        {
            float height = Text.CalcHeight(text, width);
            Widgets.Label(new Rect(x, y, width, height), text);
            return height + 1f;
        }
    }
}
