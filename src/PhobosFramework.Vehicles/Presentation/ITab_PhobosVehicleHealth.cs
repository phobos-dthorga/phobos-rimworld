using System;
using System.Collections.Generic;
using System.Text;
using Phobos.Framework;
using Phobos.Framework.Interface;
using Phobos.Framework.Rules;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

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
                using (new TextBlock(GameFont.Tiny, TextAnchor.MiddleLeft))
                {
                    Widgets.Label(new Rect(rect.x + 4f, y, rect.width * (figures ? 0.62f : 1f) - 4f, Row), wear.Label.Colorize(wear.Colour));
                }
                if (figures && wear.Ring != null)
                {
                    PhobosWidgets.Sparkline(new Rect(rect.x + rect.width * 0.64f, y + 3f, rect.width * 0.36f, Row - 6f), wear.Ring, wear.Min, wear.Max, wear.Colour);
                }
                TooltipHandler.TipRegion(row, wear.Tip);
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
            Widgets.Label(row, crew.Label);
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
        foreach (PartGroup group in view.Parts!)
        {
            PhobosWidgets.Section(ref y, 0f, viewWidth, group.Header);
            foreach (PartRow part in group.Rows)
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
                float nameWidth = viewWidth * 0.42f;
                float iconsX = nameWidth;
                foreach (UnityEngine.Texture2D icon in part.Icons)
                {
                    iconsX -= RowIcon + 2f;
                    GUI.DrawTexture(new Rect(iconsX, y + (Row - RowIcon) / 2f, RowIcon, RowIcon), icon);
                }
                Widgets.Label(new Rect(4f, y, iconsX - 6f, Row), part.HealthShare >= 0.999f ? part.Label : part.Label.Colorize(part.HealthColour));
                PhobosWidgets.Gauge(new Rect(viewWidth * 0.44f, y + 4f, viewWidth * 0.22f, Row - 8f), part.HealthShare, part.HealthColour);
                using (new TextBlock(TextAnchor.MiddleRight))
                {
                    Widgets.Label(new Rect(viewWidth * 0.68f, y, viewWidth * 0.14f, Row), part.HealthText);
                    Widgets.Label(new Rect(viewWidth * 0.84f, y, viewWidth * 0.16f, Row), part.EfficiencyText);
                }
                TooltipHandler.TipRegion(row, new TipSignal(part.Tip, part.TipId));
                y += Row;
                foreach (ConditionRow condition in part.Conditions)
                {
                    var line = new Rect(0f, y, viewWidth, ConditionHeight);
                    Widgets.DrawHighlightIfMouseover(line);
                    using (new TextBlock(GameFont.Tiny))
                    {
                        Widgets.Label(new Rect(24f, y, viewWidth - 24f, ConditionHeight), condition.Label.Colorize(condition.Colour));
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
            y += 6f;
        }
        Widgets.EndScrollView();
        view.LitKey = hovered?.props.key;
        if (!onWorld) VehicleHighlight.Set(comp.Vehicle, hovered ?? sheetHovered ?? picked);
    }

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
        public readonly List<WearRow> Wear = new();
        public int WearStamp = -1;
        public DisplayMode WearMode;

        /// <summary>The slow readings' rows: when each reaches its next stage worth a look, at the rate of the last rise (TrendRule).
        /// Built again only when a new hourly sample is taken or the display mode changes.</summary>
        public void BuildWear(CompPhobosVehicleSystems comp, DisplayMode mode)
        {
            WearStamp = comp.History.WearStamp;
            WearMode = mode;
            Wear.Clear();
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
                row.Label = "PhobosVehicleSystems_HealthWearNow".Translate(reading.Named("READING"), current.label.Named("STAGE")).Resolve();
                row.Colour = current.Severe ? PhobosWidgets.Bad : PhobosWidgets.Caution;
            }
            else if (next != null && samples is double until)
            {
                int ticks = (int)Math.Min(int.MaxValue / 2, until * QuantityHistory.WearStrideTicks);
                row.Label = "PhobosVehicleSystems_HealthWearDue".Translate(reading.Named("READING"), next.label.Named("STAGE"), ticks.ToStringTicksToPeriod().Named("TIME")).Resolve();
                row.Colour = ticks < WearSoonTicks ? PhobosWidgets.Caution : PhobosWidgets.Good;
            }
            else
            {
                row.Label = "PhobosVehicleSystems_HealthWearSteady".Translate(reading.Named("READING")).Resolve();
                row.Colour = PhobosWidgets.Good;
            }
            return row;
        }

        public void Build(CompPhobosVehicleSystems comp, DisplayMode mode)
        {
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
            foreach (VehicleStatDef stat in vehicle.VehicleDef.StatCategoryDefs())
            {
                if (stat.alwaysHide) continue;
                float value = stat.Worker.GetValue(vehicle);
                float baseValue = stat.Worker.GetBaseValue(vehicle.VehicleDef);
                float share = baseValue > 0f ? value / baseValue : 1f;
                VehicleStatDef captured = stat;
                var parts = new List<VehicleComponent>();
                foreach (VehicleComponent part in components)
                {
                    if (part.props.categories != null && part.props.categories.Contains(stat)) parts.Add(part);
                }
                Stats.Add(new StatRow
                {
                    Label = stat.LabelCap,
                    Value = figures ? stat.Worker.StatValueFormatted(vehicle) : ShareWord(share),
                    Colour = stat.operationType == EfficiencyOperationType.None ? Color.white : PhobosWidgets.HealthColour(share),
                    TipId = vehicle.thingIDNumber ^ stat.index ^ 0x5A17,
                    Tip = () => StatTip(comp, captured, parts, figures),
                });
            }
        }

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
                        Label = "PhobosVehicleSystems_HealthCrewRow".Translate(role.CapitalizeFirst().Named("ROLE"), pawn.LabelShort.Named("NAME"),
                            HealthUtility.GetGeneralConditionLabel(pawn, shortVersion: true).Named("CONDITION")).Resolve(),
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
            var roleOf = new Dictionary<string, string>();
            var firstRolePart = new Dictionary<string, bool>();
            PhobosVehicleLoadoutDef? loadout = comp.Props.loadout;
            var ordered = new List<VehicleComponent>();
            if (loadout != null)
            {
                foreach (PartRoleSpec spec in loadout.partRoles)
                {
                    if (!comp.Props.roleKeys.TryGetValue(spec.role, out List<string>? keys)) continue;
                    foreach (string key in keys)
                    {
                        VehicleComponent? part = vehicle.statHandler.GetComponent(key);
                        if (part == null || roleOf.ContainsKey(key)) continue;
                        roleOf[key] = spec.role;
                        ordered.Add(part);
                    }
                }
            }
            foreach (VehicleComponent part in vehicle.statHandler.components)
            {
                if (!roleOf.ContainsKey(part.props.key)) ordered.Add(part);
            }
            bool leaking = vehicle.CompFueledTravel?.FuelLeaking == true;
            foreach (VehicleComponent part in ordered)
            {
                roleOf.TryGetValue(part.props.key, out string? role);
                PartRow row = MakeRow(comp, part, role, figures, leaking);
                (part.Depth == VehicleComponent.VehiclePartDepth.External ? external : internalGroup).Rows.Add(row);
            }
            Parts = new List<PartGroup>();
            if (external.Rows.Count > 0) Parts.Add(external);
            if (internalGroup.Rows.Count > 0) Parts.Add(internalGroup);
            Height = Row;
            foreach (PartGroup group in Parts)
            {
                Height += Header;
                foreach (PartRow row in group.Rows) Height += Row + row.Conditions.Count * ConditionHeight;
                Height += 6f;
            }
        }

        static PartRow MakeRow(CompPhobosVehicleSystems comp, VehicleComponent part, string? role, bool figures, bool leaking)
        {
            float share = part.MaxHealth > 0f ? part.Health / part.MaxHealth : 0f;
            bool feedsStat = part.props.categories != null && part.props.categories.Count > 0;
            string label = role != null ? ITab_VehicleSystems.PartLabel(role, part.props.label) : part.props.label.CapitalizeFirst();
            var row = new PartRow
            {
                Part = part,
                Label = label,
                HealthShare = share,
                HealthColour = PhobosWidgets.HealthColour(share),
                HealthText = figures ? part.HealthPercent.ToStringPercent() : ShareWord(share),
                EfficiencyText = !feedsStat ? "-" : figures ? part.Efficiency.ToStringPercent() : "",
                TipId = comp.Vehicle.thingIDNumber ^ part.props.key.GetHashCode(),
            };
            if (comp.Fittings.FittedOn(part.props.key)?.Icon is { } fittingIcon) row.Icons.Add(fittingIcon);
            if (comp.Fittings.GradeOf(part.props.key).Icon is { } gradeIcon) row.Icons.Add(gradeIcon);
            VehicleComponent captured = part;
            bool capturedFigures = figures;
            row.Tip = () => PartTip(captured, capturedFigures);
            if (part.Health <= 0f) row.Conditions.Add(new ConditionRow { Label = "PhobosVehicleSystems_HealthDestroyed".Translate(), Colour = PhobosWidgets.Bad });
            foreach (RepairWork repair in comp.Repairs.Pending)
            {
                if (repair.Kind == RepairKind.ReplacePart && repair.Part == part) row.Conditions.Add(new ConditionRow { Label = comp.Repairs.NeedsLine(repair), Colour = PhobosWidgets.Caution });
            }
            if (!comp.Fittings.GradeOf(part.props.key).isDefault) row.Conditions.Add(new ConditionRow { Label = "PhobosVehicleSystems_GradeLine".Translate(comp.Fittings.GradeOf(part.props.key).label.Named("GRADE")), Colour = PhobosWidgets.Good });
            if (comp.Fittings.FittedOn(part.props.key) is { } fitting) row.Conditions.Add(new ConditionRow { Label = "PhobosVehicleSystems_FitFittedLine".Translate(fitting.label.Named("FITTING")), Colour = PhobosWidgets.Good });
            if (leaking && HasReactor<Reactor_FuelLeak>(part)) row.Conditions.Add(new ConditionRow { Label = "PhobosVehicleSystems_HealthLeaking".Translate(), Colour = PhobosWidgets.Bad });
            if (role != null) AddStages(comp, role, row);
            AddConditions(comp, part, figures, row);
            return row;
        }

        /// <summary>The part's own conditions (a holed radiator), each with its stage, and its severity when figures are shown.</summary>
        static void AddConditions(CompPhobosVehicleSystems comp, VehicleComponent part, bool figures, PartRow row)
        {
            foreach (VehicleCondition condition in comp.Conditions.On(part.props.key))
            {
                StageSpec? stage = condition.CurrentStage;
                if (stage == null) continue;
                NamedArgument name = condition.def!.label.CapitalizeFirst().Named("CONDITION");
                row.Conditions.Add(new ConditionRow
                {
                    Label = figures
                        ? "PhobosVehicleSystems_HealthConditionFigure".Translate(name, stage.label.Named("STAGE"), condition.severity.ToStringPercent().Named("SEVERITY")).Resolve()
                        : "PhobosVehicleSystems_HealthCondition".Translate(name, stage.label.Named("STAGE")).Resolve(),
                    Colour = ITab_VehicleDashboard.StageColour(stage),
                    SystemDefName = string.IsNullOrEmpty(condition.system) ? null : condition.system,
                });
                if (condition.fieldFixed) row.Conditions.Add(new ConditionRow { Label = "PhobosVehicleSystems_HealthFieldFixed".Translate(), Colour = PhobosWidgets.Caution });
                string? clock = comp.Repairs.DangerClock(condition);
                if (clock != null) row.Conditions.Add(new ConditionRow { Label = clock, Colour = PhobosWidgets.Bad });
                if (condition.def.NeedsService(condition.severity)) row.Conditions.Add(new ConditionRow { Label = "PhobosVehicleSystems_HealthNeedsService".Translate(condition.def.service!.label.Named("SERVICE")).Resolve(), Colour = PhobosWidgets.Caution });
            }
        }

        /// <summary>The stages of every system whose first role is this part's role (so a system's conditions sit under one part, as a
        /// pawn's condition sits under one body part), when the stage is notable or does something.</summary>
        static void AddStages(CompPhobosVehicleSystems comp, string role, PartRow row)
        {
            foreach (SystemState state in comp.States)
            {
                if (!comp.IsEnabled(state) || state.Def.roles.Count == 0 || state.Def.roles[0] != role) continue;
                for (int q = 0; q < state.values.Length; q++)
                {
                    QuantitySpec quantity = state.Def.quantities[q];
                    StageSpec? stage = state.CurrentStage(q);
                    if (stage == null || (!stage.notable && stage.effects.Count == 0) || (quantity.hidden && stage.effects.Count == 0)) continue;
                    row.Conditions.Add(new ConditionRow
                    {
                        Label = "PhobosVehicleSystems_InspectFigureStage".Translate(quantity.label.CapitalizeFirst().Named("QUANTITY"),
                            InspectText.Format(quantity, state.values[q]).Named("VALUE"), stage.label.Named("STAGE")).Resolve(),
                        Colour = ITab_VehicleDashboard.StageColour(stage),
                        SystemDefName = state.Def.defName,
                    });
                }
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

        static string PartTip(VehicleComponent part, bool figures)
        {
            var text = new StringBuilder(part.props.label.CapitalizeFirst());
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
                    if (armour > 0f) text.Append('\n').Append(category.armorRatingStat.LabelCap).Append(": ").Append(armour.ToStringPercent());
                }
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

    sealed class WearRow
    {
        public string Label = "";
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
        public string Tip = "";
    }

    sealed class PartGroup
    {
        public string Header = "";
        public readonly List<PartRow> Rows = new();
    }

    sealed class PartRow
    {
        public VehicleComponent Part = null!;
        public string Label = "";
        public float HealthShare;
        public Color HealthColour;
        public string HealthText = "";
        public string EfficiencyText = "";
        public int TipId;
        public Func<string> Tip = () => "";
        public readonly List<ConditionRow> Conditions = new();
        /// <summary>The small pictures at the right of its name: its fitting and its grade, where they have one.</summary>
        public readonly List<UnityEngine.Texture2D> Icons = new();
    }

    sealed class ConditionRow
    {
        public string Label = "";
        public Color Colour;
        public string? SystemDefName;
    }
}
