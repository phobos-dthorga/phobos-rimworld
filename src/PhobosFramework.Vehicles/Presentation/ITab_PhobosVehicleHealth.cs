using System;
using System.Collections.Generic;
using System.Text;
using Phobos.Framework;
using Phobos.Framework.Interface;
using Phobos.Framework.Rules;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;
using Verse.Sound;

namespace Phobos.VehicleSystems;

/// <summary>
/// The Health tab (docs/development/vehicle-health-design.md, slice 1): a vehicle's health laid out the way a pawn's is. Left, the
/// vehicle's stats as its capacities, each with a tooltip naming the parts that make it and our factors on it; the one thing it
/// waits for; the crew with the game's own word for each. Right, every part grouped External then Internal, by role, with its
/// health, efficiency and (in Operator and Engineer) armour, and under each part the conditions that already exist: the stages of
/// the systems that read its role, a fuel leak Vehicle Framework knows of, "destroyed" at no health. Hovering a part lights it on
/// the vehicle; clicking a condition opens the Systems tab at its system. It replaces Vehicle Framework's own Health tab (a
/// setting keeps that one). Presentation only: the row model is rebuilt only when the comp says something changed, and nothing
/// here changes the game.
/// </summary>
public class ITab_PhobosVehicleHealth : ITab
{
    const float Margin = 10f;
    const float Row = 22f;
    const float ConditionHeight = 20f;
    const float Header = 26f;
    const float LeftShare = 0.37f;
    /// <summary>The damage sheet's column (Framework 0.79.0, P7a), drawn when the tab is at least <see cref="SheetNeedsWidth"/> wide.</summary>
    const float SheetWidth = 210f;
    const float SheetNeedsWidth = 820f;
    /// <summary>The share of the left column a wear row's words take when its little graph is shown beside them.</summary>
    const float WearWordsShare = 0.74f;
    /// <summary>The share of the left column a figure or a word at its right edge takes: a whole-vehicle line's figure, a crew
    /// member's condition.</summary>
    const float LeftFigureShare = 0.3f;
    /// <summary>The least share of those words' room that what a reading is coming to keeps, where both cannot fit whole.</summary>
    const float WearOutcomeLeast = 0.45f;
    /// <summary>The gap between a wear row's reading and what it is coming to.</summary>
    const float WearGap = 6f;

    public ITab_PhobosVehicleHealth()
    {
        size = new Vector2(910f, 430f);
        labelKey = "PhobosVehicleSystems_TabHealth";
    }

    static CompPhobosVehicleSystems? CompOf(Thing? thing) => thing is VehiclePawn vehicle ? vehicle.GetComp<CompPhobosVehicleSystems>() : null;

    public override bool IsVisible => CompOf(SelThing)?.Usable == true;

    protected override void FillTab()
    {
        CompPhobosVehicleSystems? comp = CompOf(SelThing);
        if (comp == null || !comp.Usable) return;
        Draw(comp, new Rect(0f, 0f, size.x, size.y).ContractedBy(Margin), onWorld: false);
    }

    protected override void CloseTab()
    {
        base.CloseTab();
        ClearState();
    }

    /// <summary>Forgets every vehicle's view and stops lighting a part (this tab or the world's closed).</summary>
    public static void ClearState()
    {
        views.Clear();
        VehicleHighlight.Clear();
    }

    /// <summary>How many vehicles' views are held, for the dev action.</summary>
    public static int ViewCount => views.Count;

    /// <summary>Throws the views away, so the next draw rebuilds them (dev action).</summary>
    public static void Invalidate() => views.Clear();

    static readonly TabState<HealthView> views = new();

    /// <summary>Draws one vehicle's health in <paramref name="outer"/>: for this tab on a map, and for the world map's tab.</summary>
    public static void Draw(CompPhobosVehicleSystems comp, Rect outer, bool onWorld)
    {
        // Guarded: a throw is logged once and the tab rests, where the game would log it every frame (UiGuard).
        if (UiGuard.Resting(GuardKey)) { UiGuard.Fallback(outer); return; }
        try { DrawInner(comp, outer, onWorld); }
        catch (System.Exception e) { UiGuard.Report(GuardKey, e); }
    }

    const string GuardKey = "The vehicle Health tab";

    static void DrawInner(CompPhobosVehicleSystems comp, Rect outer, bool onWorld)
    {
        using Phobos.Framework.Debug.PhobosTiming.Scope timing = Phobos.Framework.Debug.PhobosTiming.Measure("health tab");
        DisplayMode mode = VehicleSystemsMod.Settings.displayMode;
        HealthView view = views.Get(comp.Vehicle.thingIDNumber);
        if (view.Version != comp.HealthViewVersion || view.Mode != mode || view.Parts == null) view.Build(comp, mode);
        if (view.WearStamp != comp.History.WearStamp || view.WearMode != mode) view.BuildWear(comp, mode);
        if (outer.width >= SheetNeedsWidth)
        {
            // The vehicle seen from above with its hurt parts tinted; hovering it lights the part's row, and the part on the map.
            var sheet = new Rect(outer.x, outer.y, SheetWidth, SheetWidth);
            view.SheetHover = DamageSheet.Draw(sheet, comp.Vehicle.VehicleDef, view.Sheet, view.LitKey ?? view.Picked);
            // A click picks the part under the mouse: its row stays marked and scrolls into view; a second click clears it (Framework 0.84.0).
            if (Widgets.ButtonInvisible(sheet, doMouseoverSound: false))
            {
                view.Picked = view.SheetHover == view.Picked ? null : view.SheetHover;
                view.ScrollToPicked = view.Picked != null;
            }
            outer.xMin += SheetWidth + 8f;
        }
        else view.SheetHover = null;
        var left = new Rect(outer.x, outer.y, outer.width * LeftShare, outer.height);
        var right = new Rect(left.xMax + 8f, outer.y, outer.width - left.width - 8f, outer.height);
        DrawLeft(comp, view, left, onWorld);
        DrawRight(comp, view, right, mode, onWorld);
    }

    static void DrawLeft(CompPhobosVehicleSystems comp, HealthView view, Rect rect, bool onWorld)
    {
        float y = rect.y;
        using (new TextBlock(GameFont.Medium))
        {
            Widgets.Label(new Rect(rect.x, y, rect.width, 30f), comp.Vehicle.LabelCap);
            y += 30f;
        }
        Widgets.Label(new Rect(rect.x, y, rect.width, Row), view.Condition);
        y += Row + 4f;
        foreach (StatRow stat in view.Stats)
        {
            var row = new Rect(rect.x, y, rect.width, Row);
            Widgets.DrawHighlightIfMouseover(row);
            Widgets.Label(new Rect(rect.x, y, rect.width * 0.6f, Row), stat.Label);
            using (new TextBlock(TextAnchor.MiddleRight))
            {
                Widgets.Label(new Rect(rect.x + rect.width * 0.6f, y, rect.width * 0.4f, Row), stat.Value.Colorize(stat.Colour));
            }
            TooltipHandler.TipRegion(row, new TipSignal(stat.Tip, stat.TipId));
            y += Row;
        }
        if (view.Wear.Count > 0)
        {
            // The slow readings and when each reaches its next stage worth a look (Framework 0.86.0), with fifteen days of it beside them
            // for those who read figures.
            y += 4f;
            PhobosWidgets.Section(ref y, rect.x, rect.width, "PhobosVehicleSystems_HealthWear".Translate());
            bool figures = view.WearMode != DisplayMode.Plain;
            foreach (WearRow wear in view.Wear)
            {
                var row = new Rect(rect.x, y, rect.width, Row);
                Widgets.DrawHighlightIfMouseover(row);
                // Framework 0.266.0 (the owner's word of 23 Nov 2026: the Systems tab's changes, here too): the reading at the left and what
                // it is coming to at the right, in its colour, where they were one sentence. The reading keeps its name whole where both
                // fit; where they do not, each is cut with an ellipsis, never mid-word and silently (found by the owner, 16 Nov 2026:
                // "Hot-section life used: No"), and the whole line is in the tooltip.
                float room = rect.width * (figures ? WearWordsShare : 1f) - 4f;
                float outcomeRoom, readingRoom;
                using (new TextBlock(GameFont.Tiny, TextAnchor.MiddleLeft))
                {
                    if (wear.ReadingWidth < 0f)
                    {
                        wear.ReadingWidth = Text.CalcSize(wear.Reading).x;
                        wear.OutcomeWidth = Text.CalcSize(wear.Outcome).x;
                    }
                    outcomeRoom = Mathf.Min(wear.OutcomeWidth + 2f, Mathf.Max(room - wear.ReadingWidth - WearGap, room * WearOutcomeLeast));
                    readingRoom = room - outcomeRoom - WearGap;
                    Widgets.Label(new Rect(rect.x + 4f, y, readingRoom, Row), wear.Reading.Truncate(readingRoom, view.WearTruncated));
                }
                using (new TextBlock(GameFont.Tiny, TextAnchor.MiddleRight))
                {
                    Widgets.Label(new Rect(rect.x + 4f + room - outcomeRoom, y, outcomeRoom, Row), wear.Outcome.Truncate(outcomeRoom, view.WearTruncated).Colorize(wear.Colour));
                }
                if (figures && wear.Ring != null)
                {
                    PhobosWidgets.Sparkline(new Rect(rect.x + rect.width * (WearWordsShare + 0.02f), y + 3f, rect.width * (0.98f - WearWordsShare), Row - 6f), wear.Ring, wear.Min, wear.Max, wear.Colour);
                }
                TooltipHandler.TipRegion(row, wear.Label + "\n\n" + wear.Tip);
                y += Row;
            }
        }
        if (view.WholeVehicle.Count > 0)
        {
            // Framework 0.256.0: what the systems with no part of their own say, so nothing the Systems tab warns of is missing here.
            y += 4f;
            PhobosWidgets.Section(ref y, rect.x, rect.width, "PhobosVehicleSystems_HealthWholeVehicle".Translate());
            foreach (ConditionRow condition in view.WholeVehicle)
            {
                var line = new Rect(rect.x, y, rect.width, Row);
                Widgets.DrawHighlightIfMouseover(line);
                float figureRoom = condition.Figure == null ? 0f : rect.width * LeftFigureShare;
                using (new TextBlock(GameFont.Tiny, TextAnchor.MiddleLeft))
                {
                    float words = rect.width - 4f - figureRoom;
                    Widgets.Label(new Rect(rect.x + 4f, y, words, Row), condition.Label.Truncate(words).Colorize(condition.Colour));
                }
                if (condition.Figure != null)
                {
                    using (new TextBlock(GameFont.Tiny, TextAnchor.MiddleRight))
                    {
                        Widgets.Label(new Rect(rect.xMax - figureRoom, y, figureRoom, Row), condition.Figure.Colorize(condition.Colour));
                    }
                }
                TooltipHandler.TipRegion(line, condition.Figure == null ? condition.Label : condition.Label + "\n" + condition.Figure);
                if (condition.SystemDefName != null && view.Mode != DisplayMode.Plain && Widgets.ButtonInvisible(line))
                {
                    ITab_VehicleSystems.ScrollTo(comp.Vehicle.thingIDNumber, condition.SystemDefName);
                    if (onWorld) PhobosTabs.OpenWorldTab(typeof(WITab_PhobosVehicleSystems));
                    else InspectPaneUtility.OpenTab(typeof(ITab_VehicleSystems));
                }
                y += Row;
            }
        }
        y += 6f;
        using (new TextBlock(GameFont.Small, TextAnchor.UpperLeft))
        {
            float height = Mathf.Max(Row, Text.CalcHeight(view.Waiting, rect.width));
            Widgets.Label(new Rect(rect.x, y, rect.width, height), view.Waiting);
            y += height + 6f;
        }
        Widgets.Label(new Rect(rect.x, y, rect.width, Row), "PhobosVehicleSystems_HealthCrew".Translate());
        y += Row;
        if (view.Crew.Count == 0)
        {
            Widgets.Label(new Rect(rect.x + 8f, y, rect.width - 8f, Row), "PhobosVehicleSystems_HealthCrewNone".Translate());
            return;
        }
        foreach (CrewRow crew in view.Crew)
        {
            var row = new Rect(rect.x + 8f, y, rect.width - 8f, Row);
            Widgets.DrawHighlightIfMouseover(row);
            float who = row.width * (1f - LeftFigureShare) - 4f;
            Widgets.Label(new Rect(row.x, y, who, Row), crew.Label.Truncate(who, view.CrewTruncated));
            using (new TextBlock(TextAnchor.MiddleRight))
            {
                Widgets.Label(new Rect(row.xMax - row.width * LeftFigureShare, y, row.width * LeftFigureShare, Row), crew.Condition);
            }
            TooltipHandler.TipRegion(row, crew.Tip);
            if (!onWorld && crew.Pawn != null && Widgets.ButtonInvisible(row)) CameraJumper.TryJumpAndSelect(crew.Pawn);
            y += Row;
        }
    }

    static void DrawRight(CompPhobosVehicleSystems comp, HealthView view, Rect rect, DisplayMode mode, bool onWorld)
    {
        float viewWidth = rect.width - 16f;
        var inner = new Rect(0f, 0f, viewWidth, view.Height);
        Widgets.BeginScrollView(rect, ref view.Scroll, inner);
        float y = 0f;
        VehicleComponent? hovered = null, sheetHovered = null, picked = null;

        // One part's row and the lines under it, with its health bar (Framework 0.114.1: a whole part's single square read as a nearly
        // empty bar, the owner's report of 16 Nov 2026, so every part has its full bar again).
        void DrawPart(PartRow part, float indent)
        {
            var row = new Rect(0f, y, viewWidth, Row);
            if (part.Part.props.key == view.Picked)
            {
                Widgets.DrawHighlightSelected(row);
                picked = part.Part;
                if (view.ScrollToPicked)
                {
                    view.Scroll.y = Mathf.Max(0f, y - Row * 2f);
                    view.ScrollToPicked = false;
                }
            }
            if (Mouse.IsOver(row))
            {
                Widgets.DrawHighlight(row);
                hovered = part.Part;
            }
            else if (view.SheetHover == part.Part.props.key)
            {
                Widgets.DrawHighlight(row);
                sheetHovered = part.Part;
            }
            // As the game's health card: the part's name in its health colour, and a stack of small pictures at the right of the
            // name column for what is fitted to it and how well it is made (Framework 0.87.0).
            float nameWidth = viewWidth * NameShare;
            float iconsX = nameWidth;
            foreach (UnityEngine.Texture2D icon in part.Icons)
            {
                iconsX -= RowIcon + 2f;
                GUI.DrawTexture(new Rect(iconsX, y + (Row - RowIcon) / 2f, RowIcon, RowIcon), icon);
            }
            bool sound = part.HealthShare >= PhobosWidgets.HealthSlightlyImpairedBelow;
            float nameRoom = iconsX - 6f - indent;
            string name = part.Label.Truncate(nameRoom, view.Truncated);
            Widgets.Label(new Rect(4f + indent, y, nameRoom, Row), sound ? name : name.Colorize(part.HealthColour));
            PhobosWidgets.Gauge(new Rect(viewWidth * GaugeX, y + 4f, viewWidth * GaugeShare, Row - 8f), part.HealthShare, part.HealthColour);
            using (new TextBlock(TextAnchor.MiddleRight))
            {
                Widgets.Label(new Rect(viewWidth * HealthColumnX, y, viewWidth * HealthColumnShare, Row), part.HealthText);
                Widgets.Label(new Rect(viewWidth * EfficiencyColumnX, y, viewWidth * EfficiencyColumnShare, Row), part.EfficiencyText);
            }
            TooltipHandler.TipRegion(row, new TipSignal(part.Tip, part.TipId));
            y += Row;
            foreach (ConditionRow condition in part.Conditions)
            {
                var line = new Rect(0f, y, viewWidth, ConditionHeight);
                Widgets.DrawHighlightIfMouseover(line);
                if (condition.Figure == null)
                {
                    using (new TextBlock(GameFont.Tiny))
                    {
                        Widgets.Label(new Rect(24f + indent, y, viewWidth - 24f - indent, ConditionHeight), condition.Label.Colorize(condition.Colour));
                    }
                }
                else
                {
                    // Framework 0.266.0: its figure under the part's own health, so figures read down one column; its words stop short of it.
                    float words = viewWidth * HealthColumnX - 28f - indent;
                    using (new TextBlock(GameFont.Tiny))
                    {
                        Widgets.Label(new Rect(24f + indent, y, words, ConditionHeight), condition.Label.Truncate(words, view.ConditionTruncated).Colorize(condition.Colour));
                    }
                    using (new TextBlock(GameFont.Tiny, TextAnchor.UpperRight))
                    {
                        Widgets.Label(new Rect(viewWidth * HealthColumnX, y, viewWidth * HealthColumnShare, ConditionHeight), condition.Figure.Colorize(condition.Colour));
                    }
                    Seen.Note("vehicles.health-tab.figure-column", comp.Vehicle);
                }
                if (condition.SystemDefName != null && Widgets.ButtonInvisible(line) && mode != DisplayMode.Plain)
                {
                    ITab_VehicleSystems.ScrollTo(comp.Vehicle.thingIDNumber, condition.SystemDefName);
                    if (onWorld) PhobosTabs.OpenWorldTab(typeof(WITab_PhobosVehicleSystems));
                    else InspectPaneUtility.OpenTab(typeof(ITab_VehicleSystems));
                }
                y += ConditionHeight;
            }
        }

        foreach (PartGroup group in view.Parts!)
        {
            // A card a group, as the Systems tab has (Framework 0.94.0), its dot in the colour of the worst part in it.
            float top = y;
            PhobosWidgets.CardHeader(ref y, 0f, viewWidth, group.Header, GroupColour(group));
            foreach (PartBundle bundle in group.Bundles)
            {
                if (bundle.Members.Count == 1)
                {
                    DrawPart(bundle.Members[0], 0f);
                    continue;
                }
                // Like parts folded into one row (Framework 0.114.0; owner, 16 Nov 2026): one bar split into a segment a part, each filled to
                // that part's health in its colour (0.114.1: separate squares read as nearly empty bars). A click opens and closes the row;
                // resting on a segment is resting on that part. A row with a hurt part, or one with something to say, starts open.
                bool open = bundle.OpenByDefault != view.Flipped.Contains(bundle.Key);
                if (!open && view.ScrollToPicked && view.Picked != null && bundle.Holds(view.Picked))
                {
                    // The part picked on the picture is inside: open the row so it can be marked and scrolled to.
                    if (!view.Flipped.Add(bundle.Key)) view.Flipped.Remove(bundle.Key);
                    open = true;
                }
                var row = new Rect(0f, y, viewWidth, Row);
                if (Mouse.IsOver(row)) Widgets.DrawHighlight(row);
                GUI.DrawTexture(new Rect(4f, y + (Row - ArrowSize) / 2f, ArrowSize, ArrowSize), open ? TexButton.Collapse : TexButton.Reveal);
                bool sound = bundle.WorstShare >= PhobosWidgets.HealthSlightlyImpairedBelow;
                Widgets.Label(new Rect(BundleIndent, y, viewWidth * NameShare - BundleIndent, Row), sound ? bundle.Label : bundle.Label.Colorize(bundle.WorstColour));
                float barWidth = viewWidth * GaugeShare;
                float segment = (barWidth - SegmentGap * (bundle.Members.Count - 1)) / bundle.Members.Count;
                float segmentX = viewWidth * GaugeX;
                PartRow? pipOver = null;
                foreach (PartRow member in bundle.Members)
                {
                    var segmentRect = new Rect(segmentX, y + 4f, segment, Row - 8f);
                    PhobosWidgets.Gauge(segmentRect, member.HealthShare, member.HealthColour);
                    if (member.Part.props.key == view.Picked || view.SheetHover == member.Part.props.key) Widgets.DrawBox(segmentRect.ExpandedBy(1f), 2);
                    if (Mouse.IsOver(new Rect(segmentX - SegmentGap / 2f, y, segment + SegmentGap, Row))) pipOver = member;
                    segmentX += segment + SegmentGap;
                }
                using (new TextBlock(TextAnchor.MiddleRight))
                {
                    Widgets.Label(new Rect(viewWidth * HealthColumnX, y, viewWidth * HealthColumnShare, Row), bundle.WorstText);
                    Widgets.Label(new Rect(viewWidth * EfficiencyColumnX, y, viewWidth * EfficiencyColumnShare, Row), bundle.EfficiencyText);
                }
                if (pipOver != null)
                {
                    hovered = pipOver.Part;
                    TooltipHandler.TipRegion(row, new TipSignal(pipOver.Tip, pipOver.TipId));
                }
                else TooltipHandler.TipRegion(row, new TipSignal(bundle.Tip, bundle.TipId));
                if (Widgets.ButtonInvisible(row))
                {
                    if (!view.Flipped.Add(bundle.Key)) view.Flipped.Remove(bundle.Key);
                    SoundDefOf.Tick_High.PlayOneShotOnCamera();
                }
                y += Row;
                if (!open)
                {
                    // Closed, its members still answer the picture: the one picked or under the mouse there lights on the map.
                    foreach (PartRow member in bundle.Members)
                    {
                        if (member.Part.props.key == view.Picked) picked = member.Part;
                        else if (view.SheetHover == member.Part.props.key) sheetHovered = member.Part;
                    }
                    continue;
                }
                foreach (PartRow member in bundle.Members) DrawPart(member, BundleIndent);
            }
            PhobosWidgets.CardEnd(top, ref y, 0f, viewWidth);
        }
        Widgets.EndScrollView();
        // Measured, not worked out twice: cards and their gaps set the height.
        view.Height = y + 4f;
        view.LitKey = hovered?.props.key;
        if (!onWorld) VehicleHighlight.Set(comp.Vehicle, hovered ?? sheetHovered ?? picked);
    }

    /// <summary>The parts list's columns, as shares of its width: the name, then the bar or pips.</summary>
    const float NameShare = 0.42f;
    const float GaugeX = 0.44f;
    const float GaugeShare = 0.22f;
    /// <summary>The health column and the efficiency column after it, as shares of the list's width.</summary>
    const float HealthColumnX = 0.68f;
    const float HealthColumnShare = 0.14f;
    const float EfficiencyColumnX = 0.84f;
    const float EfficiencyColumnShare = 0.16f;
    /// <summary>The gap between a folded row's bar segments; its opening arrow, and how far its name and its members sit in.</summary>
    const float SegmentGap = 3f;
    const float ArrowSize = 14f;
    const float BundleIndent = 22f;

    // ---- The row model: built when the comp says something changed, drawn every frame without allocation. ----

    sealed class HealthView
    {
        public int Version = -1;
        public DisplayMode Mode;
        public Vector2 Scroll;
        public string Condition = "";
        public string Waiting = "";
        public readonly List<StatRow> Stats = new();
        public readonly List<CrewRow> Crew = new();
        public List<PartGroup>? Parts;
        public float Height;
        public readonly List<DamageSheet.SheetPart> Sheet = new();
        /// <summary>The part whose row the mouse was over on the last frame, outlined on the sheet; and the part under the mouse on the sheet.</summary>
        public string? LitKey;
        public string? SheetHover;
        public string? Picked;
        public bool ScrollToPicked;
        /// <summary>The folded rows the player has opened or closed against their start (kept while the tab stays open; never saved).</summary>
        public readonly HashSet<string> Flipped = new();
        /// <summary>Part names as cut to fit their column, kept so none is measured twice.</summary>
        public readonly Dictionary<string, string> Truncated = new();
        public readonly List<WearRow> Wear = new();
        /// <summary>The warnings of systems with no part of their own (the cab, the driver, the flight controls; Framework 0.256.0), which
        /// the Systems tab shows and the parts list cannot hold.</summary>
        public readonly List<ConditionRow> WholeVehicle = new();
        /// <summary>The wear rows' words as cut to fit, kept so no row is measured twice.</summary>
        public readonly Dictionary<string, string> WearTruncated = new();
        /// <summary>The crew's rows and the conditions with a figure, as cut to fit: each in its own font and room.</summary>
        public readonly Dictionary<string, string> CrewTruncated = new();
        public readonly Dictionary<string, string> ConditionTruncated = new();
        public int WearStamp = -1;
        public DisplayMode WearMode;

        /// <summary>The slow readings' rows: when each reaches its next stage worth a look, at the rate of the last rise (TrendRule).
        /// Built again only when a new hourly sample is taken or the display mode changes.</summary>
        public void BuildWear(CompPhobosVehicleSystems comp, DisplayMode mode)
        {
            WearStamp = comp.History.WearStamp;
            WearMode = mode;
            Wear.Clear();
            WearTruncated.Clear();
            foreach (SystemState state in comp.States)
            {
                if (!comp.IsEnabled(state)) continue;
                for (int q = 0; q < state.values.Length; q++)
                {
                    QuantitySpec quantity = state.Def.quantities[q];
                    if (!quantity.wearTrend) continue;
                    Wear.Add(WearRowFor(comp, state, q, quantity));
                }
            }
        }

        static WearRow WearRowFor(CompPhobosVehicleSystems comp, SystemState state, int q, QuantitySpec quantity)
        {
            string reading = (quantity.remainingLabel ?? quantity.label).CapitalizeFirst();
            HistoryRing? ring = comp.History.WearFor(state, q);
            bool remaining = !quantity.remainingLabel.NullOrEmpty();
            var row = new WearRow
            {
                Reading = reading,
                Ring = ring,
                Min = remaining ? quantity.max : quantity.min,
                Max = remaining ? quantity.min : quantity.max,
                Tip = "PhobosVehicleSystems_HealthWearTip".Translate((QuantityHistory.WearHours / 24).Named("DAYS")).Resolve(),
            };
            double now = state.values[q];
            StageSpec? current = state.CurrentStage(q);
            // The next stage worth a look above where the reading is now.
            StageSpec? next = null;
            float nextFrom = float.MaxValue;
            foreach (StageSpec stage in quantity.stages)
            {
                float from = float.IsNaN(stage.from) ? quantity.min : stage.from;
                if (stage.notable && from > now && from < nextFrom)
                {
                    next = stage;
                    nextFrom = from;
                }
            }
            double span = Math.Max(1e-6, quantity.max - quantity.min);
            double? samples = next == null || ring == null ? null
                : TrendRule.SamplesUntil(ring.Count, i => ring[i], nextFrom, WearMinSamples, span * 1e-4, span * 1e-7);
            if (current is { notable: true })
            {
                row.Outcome = "PhobosVehicleSystems_HealthWearNow".Translate(current.label.Named("STAGE")).Resolve();
                row.Colour = current.Severe ? PhobosWidgets.Bad : PhobosWidgets.Caution;
            }
            else if (next != null && samples is double far && far * QuantityHistory.WearStrideTicks > WearFarTicks)
            {
                // Framework 0.136.1 (the owner's screenshot, 18 Nov 2026: "Clogged in about 298 years"): past a year a date means nothing.
                row.Outcome = "PhobosVehicleSystems_HealthWearFar".Translate(next.label.Named("STAGE")).Resolve();
                row.Colour = PhobosWidgets.Good;
            }
            else if (next != null && samples is double until)
            {
                int ticks = (int)Math.Min(int.MaxValue / 2, until * QuantityHistory.WearStrideTicks);
                row.Outcome = "PhobosVehicleSystems_HealthWearDue".Translate(next.label.Named("STAGE"), ticks.ToStringTicksToPeriod().Named("TIME")).Resolve();
                row.Colour = ticks < WearSoonTicks ? PhobosWidgets.Caution : PhobosWidgets.Good;
            }
            else
            {
                row.Outcome = "PhobosVehicleSystems_HealthWearSteady".Translate().Resolve();
                row.Colour = PhobosWidgets.Good;
            }
            row.Label = "PhobosVehicleSystems_HealthWearLine".Translate(reading.Named("READING"), row.Outcome.Named("OUTCOME")).Resolve();
            return row;
        }

        public void Build(CompPhobosVehicleSystems comp, DisplayMode mode)
        {
            Truncated.Clear();
            CrewTruncated.Clear();
            ConditionTruncated.Clear();
            Version = comp.HealthViewVersion;
            Mode = mode;
            VehiclePawn vehicle = comp.Vehicle;
            bool figures = mode != DisplayMode.Plain;
            Condition = HealthUtility.GetGeneralConditionLabel(vehicle).CapitalizeFirst();
            Waiting = comp.Tier != DetailTier.Full
                ? "PhobosVehicleSystems_DashLiteModel".Translate()
                : InspectText.WaitingLine(comp) ?? "PhobosVehicleSystems_DashReady".Translate();
            BuildStats(comp, figures);
            BuildCrew(vehicle);
            BuildParts(comp, figures);
            Sheet.Clear();
            Sheet.AddRange(DamageSheet.PartsOf(vehicle));
        }

        void BuildStats(CompPhobosVehicleSystems comp, bool figures)
        {
            Stats.Clear();
            VehiclePawn vehicle = comp.Vehicle;
            List<VehicleComponent> components = vehicle.statHandler.components;
            // Vehicle Framework lists a stat once for the vehicle and again for each comp that names it (an aircraft's flight speed came
            // twice: found by the owner, 16 Nov 2026), so each is shown once.
            var seen = new HashSet<VehicleStatDef>();
            foreach (VehicleStatDef stat in vehicle.VehicleDef.StatCategoryDefs())
            {
                if (stat.alwaysHide || !seen.Add(stat)) continue;
                float value = stat.Worker.GetValue(vehicle);
                float baseValue = stat.Worker.GetBaseValue(vehicle.VehicleDef);
                float share = baseValue > 0f ? value / baseValue : 1f;
                VehicleStatDef captured = stat;
                // Framework 0.136.1 (the owner's screenshot, 18 Nov 2026: a Harrier "700 kg"): the mass the drivetrain model drives or flies,
                // where the vehicle has one, so this tab agrees with the Systems tab and the hover check.
                Powertrain? train = comp.Powertrain;
                bool modelMass = figures && train != null && stat.defName == MassStat;
                var parts = new List<VehicleComponent>();
                foreach (VehicleComponent part in components)
                {
                    if (part.props.categories != null && part.props.categories.Contains(stat)) parts.Add(part);
                }
                Stats.Add(new StatRow
                {
                    Label = stat.LabelCap,
                    Value = modelMass ? "PhobosVehicleSystems_HealthMassKg".Translate(train!.KerbKg.ToString("N0").Named("MASS")).Resolve()
                        : figures ? stat.Worker.StatValueFormatted(vehicle) : ShareWord(share),
                    Colour = stat.operationType == EfficiencyOperationType.None ? Color.white : PhobosWidgets.HealthColour(share),
                    TipId = vehicle.thingIDNumber ^ stat.index ^ 0x5A17,
                    Tip = modelMass ? () => StatTip(comp, captured, parts, figures) + "\n\n" + "PhobosVehicleSystems_HealthMassTip".Translate(
                            train!.KerbKg.ToString("N0").Named("EMPTY"), train.MassKg().ToString("N0").Named("NOW")).Resolve()
                        : () => StatTip(comp, captured, parts, figures),
                });
            }
        }

        /// <summary>Vehicle Framework's mass stat, by its defName.</summary>
        const string MassStat = "Mass";

        static string StatTip(CompPhobosVehicleSystems comp, VehicleStatDef stat, List<VehicleComponent> parts, bool figures)
        {
            var text = new StringBuilder(stat.LabelCap);
            if (!stat.description.NullOrEmpty()) text.Append('\n').Append(stat.description);
            if (parts.Count > 0 && stat.operationType != EfficiencyOperationType.None)
            {
                text.Append("\n\n").Append("PhobosVehicleSystems_HealthStatOperation".Translate(OperationWord(stat.operationType).Named("HOW")).Resolve());
                foreach (VehicleComponent part in parts)
                {
                    text.Append('\n').Append("PhobosVehicleSystems_HealthStatPart".Translate(part.props.label.CapitalizeFirst().Named("PART"),
                        (figures ? part.Efficiency.ToStringPercent() : ShareWord(part.Efficiency)).Named("EFFICIENCY")).Resolve());
                    if (part.props.priorityStatEfficiency) text.Append(' ').Append("PhobosVehicleSystems_HealthStatCeiling".Translate());
                }
            }
            bool any = false;
            foreach (string reason in comp.StatFactorReasons(stat))
            {
                if (!any) text.Append('\n');
                any = true;
                text.Append('\n').Append(reason);
            }
            return text.ToString();
        }

        static string OperationWord(EfficiencyOperationType operation) => (operation switch
        {
            EfficiencyOperationType.MinValue => "PhobosVehicleSystems_HealthOperationLowest",
            EfficiencyOperationType.MaxValue => "PhobosVehicleSystems_HealthOperationHighest",
            EfficiencyOperationType.Sum => "PhobosVehicleSystems_HealthOperationSum",
            _ => "PhobosVehicleSystems_HealthOperationAverage",
        }).Translate();

        /// <summary>A share as a word, for Plain mode: the game's own part-condition words by the game's thresholds.</summary>
        static string ShareWord(float share) => HealthWords.Of(share);

        void BuildCrew(VehiclePawn vehicle)
        {
            Crew.Clear();
            foreach (VehicleRoleHandler handler in vehicle.handlers)
            {
                if (handler.thingOwner == null) continue;
                foreach (Pawn pawn in handler.thingOwner)
                {
                    string role = handler.role?.label ?? "";
                    Crew.Add(new CrewRow
                    {
                        Pawn = pawn,
                        Label = "PhobosVehicleSystems_HealthCrewRow".Translate(role.CapitalizeFirst().Named("ROLE"), pawn.LabelShort.Named("NAME")).Resolve(),
                        Condition = HealthUtility.GetGeneralConditionLabel(pawn, shortVersion: true),
                        Tip = pawn.LabelCap + "\n" + HealthUtility.GetGeneralConditionLabel(pawn).CapitalizeFirst(),
                    });
                }
            }
        }

        void BuildParts(CompPhobosVehicleSystems comp, bool figures)
        {
            VehiclePawn vehicle = comp.Vehicle;
            var external = new PartGroup { Header = "PhobosVehicleSystems_HealthExternal".Translate() };
            var internalGroup = new PartGroup { Header = "PhobosVehicleSystems_HealthInternal".Translate() };
            // Framework 0.256.0: every bound part, the loadout's roles first and then those bound by code (SystemParts), and each system's
            // warnings under the part the Systems tab lists first for it; a system with no part has them with the whole vehicle.
            Dictionary<string, string> roleOf = SystemParts.RoleByKey(comp);
            var ordered = new List<VehicleComponent>();
            foreach (string key in roleOf.Keys)
            {
                if (vehicle.statHandler.GetComponent(key) is { } bound) ordered.Add(bound);
            }
            foreach (VehicleComponent part in vehicle.statHandler.components)
            {
                if (!roleOf.ContainsKey(part.props.key)) ordered.Add(part);
            }
            var homes = new Dictionary<VehicleComponent, List<SystemState>>();
            WholeVehicle.Clear();
            foreach (SystemState state in comp.States)
            {
                if (!comp.IsEnabled(state)) continue;
                if (SystemParts.Home(comp, state) is { } home)
                {
                    if (!homes.TryGetValue(home, out List<SystemState>? list)) homes[home] = list = new List<SystemState>();
                    list.Add(state);
                }
                else WholeVehicle.AddRange(Stages(state));
            }
            bool leaking = vehicle.CompFueledTravel?.FuelLeaking == true;
            foreach (VehicleComponent part in ordered)
            {
                // A turbocharger is shown only on an engine that has one (Framework 0.320.0; the owner's card of 26 Nov 2026: hidden until fitted).
                if (Turbochargers.Absent(vehicle, part)) continue;
                PartRow row = MakeRow(comp, part, homes.TryGetValue(part, out List<SystemState>? systems) ? systems : null, figures, leaking);
                (part.Depth == VehicleComponent.VehiclePartDepth.External ? external : internalGroup).Rows.Add(row);
            }
            Parts = new List<PartGroup>();
            if (external.Rows.Count > 0) Parts.Add(external);
            if (internalGroup.Rows.Count > 0) Parts.Add(internalGroup);
            foreach (PartGroup group in Parts) Fold(group, figures, comp.Vehicle.thingIDNumber);
            Height = Row;
            foreach (PartGroup group in Parts)
            {
                Height += Header;
                foreach (PartRow row in group.Rows) Height += Row + row.Conditions.Count * ConditionHeight;
                Height += 6f;
            }
        }

        /// <summary>Folds a group's like parts into rows (PartGroupRule): one row for the landing wheels, one for the engine covers.</summary>
        static void Fold(PartGroup group, bool figures, int vehicleId)
        {
            var labels = new List<string>(group.Rows.Count);
            foreach (PartRow row in group.Rows) labels.Add(row.Label);
            foreach (PartGroupRule.Group folded in PartGroupRule.Groups(labels, PositionWords))
            {
                var bundle = new PartBundle { Key = group.Header + "/" + folded.Label };
                float worstEfficiency = float.MaxValue;
                foreach (int index in folded.Members)
                {
                    PartRow row = group.Rows[index];
                    bundle.Members.Add(row);
                    bundle.WorstShare = Mathf.Min(bundle.WorstShare, row.HealthShare);
                    if (row.FeedsStat) worstEfficiency = Mathf.Min(worstEfficiency, row.Part.Efficiency);
                    if (row.HealthShare < PhobosWidgets.HealthSlightlyImpairedBelow || row.Conditions.Count > 0) bundle.OpenByDefault = true;
                }
                bundle.Label = bundle.Members.Count == 1 ? bundle.Members[0].Label
                    : Find.ActiveLanguageWorker.Pluralize(folded.Label, bundle.Members.Count).CapitalizeFirst();
                bundle.WorstColour = PhobosWidgets.HealthColour(bundle.WorstShare);
                bundle.WorstText = figures ? PhobosWidgets.HealthPercent(bundle.WorstShare) : ShareWord(bundle.WorstShare);
                bundle.EfficiencyText = worstEfficiency == float.MaxValue ? "-" : figures ? worstEfficiency.ToStringPercent() : "";
                bundle.TipId = vehicleId ^ bundle.Key.GetHashCode() ^ 0x0B0D;
                PartBundle captured = bundle;
                bundle.Tip = () => BundleTip(captured);
                group.Bundles.Add(bundle);
            }
        }

        static string BundleTip(PartBundle bundle)
        {
            var text = new StringBuilder(bundle.Label);
            foreach (PartRow member in bundle.Members) text.Append('\n').Append("  ").Append(member.Label).Append(": ").Append(member.HealthText);
            text.Append("\n\n").Append("PhobosVehicleSystems_HealthBundleTip".Translate());
            return text.ToString();
        }

        static string[]? positionWords;

        /// <summary>The words that say where a part sits, from the language file, so a translation folds parts by its own words.</summary>
        static string[] PositionWords => positionWords ??= "PhobosVehicleSystems_HealthPositionWords".Translate().Resolve().Split(',');

        static PartRow MakeRow(CompPhobosVehicleSystems comp, VehicleComponent part, List<SystemState>? systems, bool figures, bool leaking)
        {
            float share = part.MaxHealth > 0f ? part.Health / part.MaxHealth : 0f;
            bool feedsStat = part.props.categories != null && part.props.categories.Count > 0;
            string label = ITab_VehicleSystems.PartLabel(part.props.label);
            var row = new PartRow
            {
                Part = part,
                Label = label,
                HealthShare = share,
                HealthColour = PhobosWidgets.HealthColour(share),
                HealthText = figures ? PhobosWidgets.HealthPercent(share) : ShareWord(share),
                EfficiencyText = !feedsStat ? "-" : figures ? part.Efficiency.ToStringPercent() : "",
                FeedsStat = feedsStat,
                TipId = comp.Vehicle.thingIDNumber ^ part.props.key.GetHashCode(),
            };
            if (comp.Fittings.FittedOn(part.props.key)?.Icon is { } fittingIcon) row.Icons.Add(fittingIcon);
            if (comp.Fittings.GradeOf(part.props.key).Icon is { } gradeIcon) row.Icons.Add(gradeIcon);
            VehicleComponent captured = part;
            bool capturedFigures = figures;
            CompPhobosVehicleSystems capturedComp = comp;
            row.Tip = () => PartTip(captured, capturedFigures, capturedComp.Repairs.NoteFor(captured));
            // A spall liner names its state, not "destroyed": one not fitted sits at nothing by design (Framework 0.207.0), and its health
            // column says so too, in grey (Framework 0.209.0).
            if (part.props.key == Spall.LinerKey)
            {
                LinerState state = Spall.StateOf(comp.Vehicle, part);
                row.Conditions.Add(LinerRow(state));
                if (state == LinerState.NotFitted)
                {
                    row.HealthText = Spall.StateWord(state);
                    row.HealthColour = PhobosWidgets.Off;
                }
            }
            // Glass names its kind and state, not "destroyed" (Framework 0.238.0, VG11).
            else if (part.props.key == Glass.Key)
            {
                GlassState state = Glass.StateOf(part);
                GlassKind kind = Glass.KindOf(comp.Vehicle);
                row.Conditions.Add(new ConditionRow
                {
                    Label = Glass.KindWord(kind) + ": " + Glass.StateWord(state, kind),
                    Colour = state == GlassState.Sound ? PhobosWidgets.Good : state == GlassState.Cracked ? PhobosWidgets.Caution : PhobosWidgets.Bad,
                });
            }
            // The extinguisher's bracket and the suppression systems name their state (Framework 0.241.0, FR10).
            else if (FireFighting.IsOwnGoods(part.props.key) && comp.Vehicle.GetComp<CompPhobosFireFighting>() is { } fireFighting)
            {
                row.Conditions.Add(FireFightingRow(fireFighting, part));
                if (part.Health <= 0f) row.Conditions.Add(new ConditionRow { Label = "PhobosVehicleSystems_HealthDestroyed".Translate(), Colour = PhobosWidgets.Bad });
            }
            else if (part.Health <= 0f) row.Conditions.Add(new ConditionRow { Label = "PhobosVehicleSystems_HealthDestroyed".Translate(), Colour = PhobosWidgets.Bad });
            foreach (RepairWork repair in comp.Repairs.Pending)
            {
                if (repair.Kind is RepairKind.ReplacePart or RepairKind.Reline or RepairKind.Reglaze or RepairKind.Refill or RepairKind.Recharge && repair.Part == part) row.Conditions.Add(new ConditionRow { Label = comp.Repairs.NeedsLine(repair), Colour = PhobosWidgets.Caution });
            }
            if (!comp.Fittings.GradeOf(part.props.key).isDefault) row.Conditions.Add(new ConditionRow { Label = "PhobosVehicleSystems_GradeLine".Translate(comp.Fittings.GradeOf(part.props.key).label.Named("GRADE")), Colour = PhobosWidgets.Good });
            if (comp.Fittings.FittedOn(part.props.key) is { } fitting) row.Conditions.Add(new ConditionRow { Label = "PhobosVehicleSystems_FitFittedLine".Translate(fitting.label.Named("FITTING")), Colour = PhobosWidgets.Good });
            // Framework 0.276.0 (the owner's yes of 24 Nov 2026, after the A-10's port engine showed its new engine and the starboard nothing):
            // a part whose fitting, removal or grade is still to be done says so, in grey, beside what it carries now.
            if (comp.Fittings.OrderOn(part.props.key) is { tune: null } waiting)
            {
                string line = waiting.grade != null ? "PhobosVehicleSystems_GradeWaitingLine".Translate(waiting.grade.label.Named("GRADE")).Resolve()
                    : waiting.remove ? "PhobosVehicleSystems_FitWaitingRemoveLine".Translate((comp.Fittings.FittedOn(part.props.key)?.label ?? "").Named("FITTING")).Resolve()
                    : "PhobosVehicleSystems_FitWaitingLine".Translate((waiting.fitting?.label ?? "").Named("FITTING")).Resolve();
                row.Conditions.Add(new ConditionRow { Label = line, Colour = PhobosWidgets.Off });
            }
            if (leaking && HasReactor<Reactor_FuelLeak>(part)) row.Conditions.Add(new ConditionRow { Label = "PhobosVehicleSystems_HealthLeaking".Translate(), Colour = PhobosWidgets.Bad });
            if (systems != null) foreach (SystemState state in systems) row.Conditions.AddRange(Stages(state));
            AddConditions(comp, part, figures, row);
            return row;
        }

        /// <summary>The part's own conditions (a holed radiator), each with its stage, and its severity when figures are shown.</summary>
        /// <summary>The extinguisher's charges, or a suppression system's state (Framework 0.241.0, FR10).</summary>
        static ConditionRow FireFightingRow(CompPhobosFireFighting comp, VehicleComponent part)
        {
            if (part.props.key == FireFighting.BracketKey)
            {
                if (!comp.ExtinguisherFittedAtAll) return new ConditionRow { Label = "PhobosFireFighting_HealthNoExtinguisher".Translate(), Colour = PhobosWidgets.Off };
                int full = FireFighting.Rules.extinguisherCharges;
                return new ConditionRow
                {
                    Label = "PhobosFireFighting_HealthCharges".Translate(comp.Charges.Named("LEFT"), full.Named("FULL")),
                    Colour = comp.Charges >= full ? PhobosWidgets.Good : comp.Charges > 0 ? PhobosWidgets.Caution : PhobosWidgets.Bad,
                };
            }
            FireZones zone = part.props.key == FireFighting.CrewSystemKey ? FireZones.CrewCompartment : FireZones.EngineBay;
            if ((comp.Fitted & zone) == 0) return new ConditionRow { Label = "PhobosFireFighting_HealthNotFitted".Translate(), Colour = PhobosWidgets.Off };
            return (comp.Ready & zone) != 0
                ? new ConditionRow { Label = "PhobosFireFighting_Ready".Translate(), Colour = PhobosWidgets.Good }
                : new ConditionRow { Label = "PhobosFireFighting_Spent".Translate(), Colour = PhobosWidgets.Bad };
        }

        /// <summary>A spall liner's state, in the colour of what it means for the crew.</summary>
        static ConditionRow LinerRow(LinerState state) => state switch
        {
            LinerState.Sound => new ConditionRow { Label = "PhobosVehicleSystems_HealthLinerSound".Translate(), Colour = PhobosWidgets.Good },
            LinerState.Worn => new ConditionRow { Label = "PhobosVehicleSystems_HealthLinerWorn".Translate(), Colour = PhobosWidgets.Caution },
            LinerState.Spent => new ConditionRow { Label = "PhobosVehicleSystems_HealthLinerSpent".Translate(), Colour = PhobosWidgets.Bad },
            _ => new ConditionRow { Label = "PhobosVehicleSystems_HealthLinerNotFitted".Translate(), Colour = PhobosWidgets.Off },
        };

        static void AddConditions(CompPhobosVehicleSystems comp, VehicleComponent part, bool figures, PartRow row)
        {
            foreach (VehicleCondition condition in comp.Conditions.On(part.props.key))
            {
                StageSpec? stage = condition.CurrentStage;
                if (stage == null) continue;
                NamedArgument name = condition.def!.label.CapitalizeFirst().Named("CONDITION");
                row.Conditions.Add(new ConditionRow
                {
                    Label = "PhobosVehicleSystems_HealthCondition".Translate(name, stage.label.Named("STAGE")).Resolve(),
                    Figure = figures ? condition.severity.ToStringPercent() : null,
                    Colour = ITab_VehicleDashboard.StageColour(stage),
                    SystemDefName = string.IsNullOrEmpty(condition.system) ? null : condition.system,
                });
                // Framework 0.181.0 (EM24): what gave it, when and where.
                if (condition.cause != null) row.Conditions.Add(new ConditionRow { Label = VehicleStory.CauseLine(condition.cause), Colour = ColoredText.SubtleGrayColor });
                if (condition.fieldFixed) row.Conditions.Add(new ConditionRow { Label = "PhobosVehicleSystems_HealthFieldFixed".Translate(), Colour = PhobosWidgets.Caution });
                string? clock = comp.Repairs.DangerClock(condition);
                if (clock != null) row.Conditions.Add(new ConditionRow { Label = clock, Colour = PhobosWidgets.Bad });
                if (condition.def.NeedsService(condition.severity)) row.Conditions.Add(new ConditionRow { Label = "PhobosVehicleSystems_HealthNeedsService".Translate(condition.def.service!.label.Named("SERVICE")).Resolve(), Colour = PhobosWidgets.Caution });
            }
        }

        /// <summary>A system's stages worth a line (notable, or doing something), for the part it sits under (SystemParts.Home) or the
        /// whole vehicle's list; a system's conditions sit under one part, as a pawn's condition sits under one body part.</summary>
        static IEnumerable<ConditionRow> Stages(SystemState state)
        {
            for (int q = 0; q < state.values.Length; q++)
            {
                QuantitySpec quantity = state.Def.quantities[q];
                StageSpec? stage = state.CurrentStage(q);
                if (stage == null || (!stage.notable && stage.effects.Count == 0) || (quantity.hidden && stage.effects.Count == 0)) continue;
                yield return new ConditionRow
                {
                    Label = "PhobosVehicleSystems_HealthCondition".Translate(quantity.label.CapitalizeFirst().Named("CONDITION"), stage.label.Named("STAGE")).Resolve(),
                    Figure = InspectText.Format(quantity, state.values[q]),
                    Colour = ITab_VehicleDashboard.StageColour(stage),
                    SystemDefName = state.Def.defName,
                };
            }
        }

        static bool HasReactor<T>(VehicleComponent part) where T : Reactor
        {
            List<Reactor>? reactors = part.props.reactors;
            if (reactors == null) return false;
            for (int i = 0; i < reactors.Count; i++)
            {
                if (reactors[i] is T) return true;
            }
            return false;
        }

        static string PartTip(VehicleComponent part, bool figures, string? note)
        {
            var text = new StringBuilder(part.props.label.CapitalizeFirst());
            // What the part does and what losing it costs (Framework 0.96.0).
            if (!note.NullOrEmpty()) text.Append('\n').Append(note).Append('\n');
            if (figures)
            {
                text.Append('\n').Append("PhobosVehicleSystems_HealthPartHealth".Translate(part.Health.ToString("0").Named("HEALTH"), part.MaxHealth.ToString("0").Named("MAX")).Resolve());
                if (part.props.categories != null && part.props.categories.Count > 0)
                {
                    text.Append('\n').Append("PhobosVehicleSystems_HealthPartFeeds".Translate(part.Efficiency.ToStringPercent().Named("EFFICIENCY")).Resolve());
                    foreach (VehicleStatDef stat in part.props.categories) text.Append(' ').Append(stat.label);
                }
                else text.Append('\n').Append("PhobosVehicleSystems_HealthFeedsNoStat".Translate());
                foreach (DamageArmorCategoryDef category in DefDatabase<DamageArmorCategoryDef>.AllDefsListForReading)
                {
                    if (category.armorRatingStat == null) continue;
                    float armour = part.ArmorRating(category, out _);
                    // The stat's own format (Framework 0.140.1): a percentage in the game, and millimetres of steel or megapascals where
                    // another mod has redefined the stat so. A fixed percentage showed 30 mm of armour as 3,000%.
                    if (armour > 0f) text.Append('\n').Append(category.armorRatingStat.LabelCap).Append(": ").Append(category.armorRatingStat.ValueToString(armour));
                }
            }
            // The part's armour class and what it stops, in every mode (Framework 0.154.0; vehicle-armour.md, "What the player sees"): the one
            // armour line Plain mode shows. Only a part whose figures are a class's: another author's vehicle shows none.
            if (ArmourClasses.For(part) is { } armourClass)
            {
                text.Append('\n').Append("PhobosVehicleSystems_HealthArmourClass".Translate(armourClass.LabelCap.Named("CLASS"), armourClass.stops.Named("STOPS")).Resolve());
                if (Seen.Wants("vehicles.armour-class-shown", part.vehicle)) Seen.Note("vehicles.armour-class-shown", part.vehicle, armourClass.defName);
            }
            if (HasReactor<Reactor_Explosive>(part)) text.Append('\n').Append("PhobosVehicleSystems_HealthReactorExplosive".Translate());
            return text.ToString();
        }
    }

    /// <summary>The size of a part row's small pictures (the game's health card uses about this).</summary>
    const float RowIcon = 18f;

    /// <summary>The hours of a rise the wear trend needs before it estimates (authored: a working day).</summary>
    const int WearMinSamples = 6;
    /// <summary>A stage due sooner than this shows amber (authored: two days).</summary>
    const int WearSoonTicks = 2 * GenDate.TicksPerDay;
    /// <summary>A stage further off than this is "far from" it, with no date (authored: a year).</summary>
    const double WearFarTicks = GenDate.TicksPerYear;

    sealed class WearRow
    {
        /// <summary>The whole line, for the tooltip: the reading and what it is coming to.</summary>
        public string Label = "";
        public string Reading = "";
        public string Outcome = "";
        /// <summary>How wide the two are in the tiny font, measured at the first draw (below 0 until then).</summary>
        public float ReadingWidth = -1f;
        public float OutcomeWidth;
        public Color Colour = Color.white;
        public string Tip = "";
        public HistoryRing? Ring;
        public float Min;
        public float Max;
    }

    sealed class StatRow
    {
        public string Label = "";
        public string Value = "";
        public Color Colour = Color.white;
        public int TipId;
        public Func<string> Tip = () => "";
    }

    sealed class CrewRow
    {
        public Pawn? Pawn;
        public string Label = "";
        /// <summary>The game's own short word for how they are, at the row's right edge.</summary>
        public string Condition = "";
        public string Tip = "";
    }

    /// <summary>The colour of the worst part in a group, for its card's dot.</summary>
    static Color GroupColour(PartGroup group)
    {
        float worst = 1f;
        foreach (PartRow row in group.Rows) worst = Mathf.Min(worst, row.HealthShare);
        return PhobosWidgets.HealthColour(worst);
    }

    sealed class PartGroup
    {
        public string Header = "";
        public readonly List<PartRow> Rows = new();
        /// <summary>The same rows as the list shows them: like parts folded into one row (Framework 0.114.0).</summary>
        public readonly List<PartBundle> Bundles = new();
    }

    /// <summary>One row of the parts list: a part alone, or like parts folded together with a pip each.</summary>
    sealed class PartBundle
    {
        public string Key = "";
        public string Label = "";
        public readonly List<PartRow> Members = new();
        public float WorstShare = 1f;
        public Color WorstColour;
        public string WorstText = "";
        public string EfficiencyText = "";
        public bool OpenByDefault;
        public int TipId;
        public Func<string> Tip = () => "";

        public bool Holds(string key)
        {
            foreach (PartRow member in Members)
            {
                if (member.Part.props.key == key) return true;
            }
            return false;
        }
    }

    sealed class PartRow
    {
        public VehicleComponent Part = null!;
        public string Label = "";
        public float HealthShare;
        public Color HealthColour;
        public string HealthText = "";
        public string EfficiencyText = "";
        public bool FeedsStat;
        public int TipId;
        public Func<string> Tip = () => "";
        public readonly List<ConditionRow> Conditions = new();
        /// <summary>The small pictures at the right of its name: its fitting and its grade, where they have one.</summary>
        public readonly List<UnityEngine.Texture2D> Icons = new();
    }

    sealed class ConditionRow
    {
        public string Label = "";
        /// <summary>Its figure (a reading's value, a condition's severity), drawn in the health column under the part's own; null for a
        /// line that is words alone.</summary>
        public string? Figure;
        public Color Colour;
        public string? SystemDefName;
    }
}
