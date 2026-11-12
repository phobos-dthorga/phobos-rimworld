using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Framework;
using Phobos.Framework.Interface;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Fittings and tunes for one vehicle: what is on it, what it can take, and why not yet. Presentation only: every click is one call on
/// the vehicle's <see cref="VehicleFittings"/>, and a mechanic does the work.
/// </summary>
public class Dialog_PhobosVehicleFittings : Window
{
    const float RowHeight = 54f;
    /// <summary>A tune's row: icon buttons, and a line under them (Framework 0.87.0).</summary>
    const float TuneRowHeight = 70f;
    const float TuneButtonHeight = 40f;
    const float IconSize = 24f;
    /// <summary>A part's choice as a square picture button (Framework 0.88.0), borrowed from the tune row.</summary>
    const float PictureButton = 36f;
    const float PartNameWidth = 170f;
    /// <summary>The space between a part's fittings and its grades.</summary>
    const float GroupGap = 14f;
    /// <summary>A preview's colour: the line says what would be, not what is.</summary>
    static readonly Color PreviewColour = new(0.62f, 0.78f, 0.95f);

    /// <summary>The tune the mouse rests on this frame, previewed on the rows it lands on below.</summary>
    PhobosVehicleTuneDef? previewTune;
    const float SectionHeight = 30f;
    const float ButtonWidth = 118f;
    const float ButtonHeight = 28f;
    const float Gap = 6f;

    readonly CompPhobosVehicleSystems comp;
    Vector2 scroll;

    public Dialog_PhobosVehicleFittings(CompPhobosVehicleSystems comp)
    {
        this.comp = comp;
        doCloseX = true;
        draggable = true;
        closeOnClickedOutside = true;
    }

    public override Vector2 InitialSize => new(640f, 660f);

    /// <summary>
    /// The details strip along the window's foot (Framework 0.90.1; owner, 11 Nov 2026: a choice's tooltip covered the "If chosen" line under
    /// its row). A tune's or a choice's name, materials, description, figures and skill show here while the mouse rests on it, in place of
    /// a tooltip, so nothing is drawn over the rows.
    /// </summary>
    const float DetailsHeight = 104f;

    /// <summary>What the mouse rests on this frame, for the details strip.</summary>
    string? hoverDetails;

    VehicleFittings Fittings => comp.Fittings;

    /// <summary>One row of the parts list as it is drawn: its parts (an axle's wheels), name, the fittings that suit it and whether it can
    /// be graded. Built when the vehicle's fittings change and every half second, not every frame (the performance pass, Framework 0.94.0).</summary>
    sealed class RowModel
    {
        public List<VehicleComponent> Parts = new();
        public string Name = "";
        public string? NameTip;
        public List<PhobosVehicleFittingDef> Options = new();
        public bool Gradable;
    }

    readonly List<RowModel> rows = new();

    /// <summary>A family of fittings as its row draws it (Framework 0.103.0): what ordering it would do here, and why not, worked out with the
    /// part rows rather than every frame.</summary>
    sealed class FamilyModel
    {
        public PhobosVehicleFamilyDef Def = null!;
        public List<VehicleFittings.FamilyStep> Steps = new();
        public string? Refusal;
        public bool Fitted;
        public bool Ordered;
    }

    readonly List<FamilyModel> families = new();
    readonly List<VehicleTuneAxis> axes = new();
    List<PhobosVehiclePartGradeDef> gradeList = new();
    static readonly List<PhobosVehiclePartGradeDef> NoGrades = new();
    int rowsVersion = -1;
    float rowsUntil;

    void EnsureRows()
    {
        if (rowsVersion == Fittings.Version && RealTime.LastRealTime < rowsUntil) return;
        rowsVersion = Fittings.Version;
        rowsUntil = RealTime.LastRealTime + (float)VehicleFittings.DisplayCacheSeconds;
        axes.Clear();
        foreach (VehicleTuneAxis axis in new[] { VehicleTuneAxis.Engine, VehicleTuneAxis.Gearing })
        {
            if (Fittings.CanTune(axis)) axes.Add(axis);
        }
        gradeList = VehicleFittings.Grades.ToList();
        families.Clear();
        foreach (PhobosVehicleFamilyDef family in Fittings.Families())
        {
            List<VehicleFittings.FamilyStep> steps = Fittings.FamilyPlan(family, out string? refusal);
            bool fitted = Fittings.FamilyFitted(family);
            families.Add(new FamilyModel
            {
                Def = family,
                Steps = steps,
                Fitted = fitted,
                Refusal = steps.Count > 0 || fitted ? null : Fittings.FamilyRefusal(family),
                Ordered = Fittings.Orders.Any(o => o.fitting != null && family.fittings.Contains(o.fitting)),
            });
        }
        rows.Clear();
        List<List<VehicleComponent>> partRows = Fittings.PartRows();
        int axleCount = partRows.Count(r => r.Count > 1), axle = 0;
        foreach (List<VehicleComponent> parts in partRows)
        {
            bool isAxle = parts.Count > 1;
            rows.Add(new RowModel
            {
                Parts = parts,
                Name = isAxle ? AxleName(axle++, axleCount) : parts[0].props.label.CapitalizeFirst(),
                NameTip = isAxle ? string.Join("\n", parts.Select(p => p.props.label.CapitalizeFirst())) : null,
                Options = Fittings.OptionsFor(parts[0]).ToList(),
                Gradable = parts.All(comp.Repairs.CanBeGraded),
            });
        }
    }

    public override void DoWindowContents(Rect inRect)
    {
        // Guarded: a throw is logged once and the window rests, where the game would log it every frame (UiGuard).
        if (UiGuard.Resting(GuardKey)) { UiGuard.Fallback(inRect); return; }
        try { Contents(inRect); }
        catch (System.Exception e) { UiGuard.Report(GuardKey, e); }
    }

    const string GuardKey = "The fittings and tunes window";

    void Contents(Rect inRect)
    {
        using Phobos.Framework.Debug.PhobosTiming.Scope timing = Phobos.Framework.Debug.PhobosTiming.Measure("fittings window");
        previewTune = null;
        hoverDetails = null;
        EnsureRows();
        float y = inRect.y;
        using (new TextBlock(GameFont.Medium))
        {
            Widgets.Label(new Rect(inRect.x, y, inRect.width, 34f), "PhobosVehicleSystems_FitWindowTitle".Translate(comp.Vehicle.LabelShortCap.Named("VEHICLE")));
        }
        y += 36f;
        string intro = "PhobosVehicleSystems_FitWindowIntro".Translate();
        if (Fittings.RigMissing) intro += " " + "PhobosVehicleSystems_FitNoRigNote".Translate(VehicleFittings.RigReach.Named("CELLS")).Resolve();
        using (new TextBlock(GameFont.Tiny))
        {
            float height = Text.CalcHeight(intro, inRect.width);
            Widgets.Label(new Rect(inRect.x, y, inRect.width, height), intro);
            y += height + 8f;
        }

        var outer = new Rect(inRect.x, y, inRect.width, inRect.yMax - y - DetailsHeight - 6f);
        // The rows grow to fit their lines, so the height is what was drawn last frame (an estimate on the first).
        float content = drawnHeight > 0f ? drawnHeight : SectionHeight * 2f + TuneRowHeight * Mathf.Max(axes.Count, 1) + RowHeight * Mathf.Max(rows.Count, 1);
        var view = new Rect(0f, 0f, outer.width - 18f, content);
        Widgets.BeginScrollView(outer, ref scroll, view);
        float row = 0f;
        DrawSection(view, ref row, "PhobosVehicleSystems_FitSectionTunes".Translate());
        if (axes.Count == 0) DrawNote(view, ref row, "PhobosVehicleSystems_FitNoTunes".Translate());
        foreach (VehicleTuneAxis axis in axes) DrawTune(view, ref row, axis);
        if (families.Count > 0)
        {
            DrawSection(view, ref row, "PhobosVehicleSystems_FitSectionFamilies".Translate());
            foreach (FamilyModel family in families) DrawFamily(view, ref row, family);
        }
        DrawSection(view, ref row, "PhobosVehicleSystems_FitSectionParts".Translate());
        if (rows.Count == 0) DrawNote(view, ref row, "PhobosVehicleSystems_FitNoParts".Translate());
        foreach (RowModel model in rows) DrawRow(view, ref row, model);
        Widgets.EndScrollView();
        drawnHeight = row;
        DrawDetails(new Rect(inRect.x, inRect.yMax - DetailsHeight, inRect.width, DetailsHeight));
    }

    /// <summary>The details strip: what the mouse rests on, or a line saying how to see it.</summary>
    void DrawDetails(Rect rect) => PhobosWidgets.DetailsStrip(rect, hoverDetails, "PhobosVehicleSystems_FitDetailsHint".Translate().Resolve());

    /// <summary>The height of everything drawn in the scroll view last frame.</summary>
    float drawnHeight;

    static void DrawSection(Rect view, ref float y, string label)
    {
        using (new TextBlock(GameFont.Small, TextAnchor.LowerLeft)) Widgets.Label(new Rect(0f, y, view.width, SectionHeight - 4f), label);
        Widgets.DrawLineHorizontal(0f, y + SectionHeight - 3f, view.width);
        y += SectionHeight;
    }

    static void DrawNote(Rect view, ref float y, string text)
    {
        using (new TextBlock(GameFont.Small, TextAnchor.MiddleLeft)) Widgets.Label(new Rect(0f, y, view.width, RowHeight), text);
        y += RowHeight;
    }

    void DrawTune(Rect view, ref float y, VehicleTuneAxis axis)
    {
        FitOrder? order = Fittings.OrderFor(axis);
        PhobosVehicleTuneDef? current = Fittings.Current(axis);
        string name = axis == VehicleTuneAxis.Engine ? "PhobosVehicleSystems_TuneAxisEngine".Translate() : "PhobosVehicleSystems_TuneAxisGearing".Translate();
        using (new TextBlock(GameFont.Small, TextAnchor.UpperLeft)) Widgets.Label(new Rect(0f, y + 2f, 110f, 24f), name);
        float x = 112f;
        bool plain = VehicleSystemsMod.Settings.displayMode == DisplayMode.Plain;
        foreach (PhobosVehicleTuneDef tune in VehicleFittings.TunesOf(axis))
        {
            // Each setting as a picture and its name; the one the vehicle runs on lit, as the Dashboard lights a switch that is on.
            var button = new Rect(x, y, ButtonWidth, TuneButtonHeight);
            bool isCurrent = tune == current;
            Widgets.DrawOptionBackground(button, isCurrent);
            if (isCurrent) Widgets.DrawBoxSolid(new Rect(button.x + 2f, button.yMax - 4f, button.width - 4f, 3f), PhobosWidgets.Good);
            float textX = button.x + 4f;
            if (tune.Icon is { } icon)
            {
                GUI.DrawTexture(new Rect(button.x + 4f, button.y + (button.height - 32f) / 2f, 32f, 32f), icon);
                textX += 36f;
            }
            using (new TextBlock(GameFont.Tiny, TextAnchor.MiddleLeft)) Widgets.Label(new Rect(textX, button.y, button.xMax - textX - 2f, button.height), tune.LabelCap);
            if (!isCurrent && Mouse.IsOver(button)) previewTune = tune;
            if (!isCurrent && Widgets.ButtonInvisible(button)) Fittings.OrderTune(tune);
            if (Mouse.IsOver(button)) hoverDetails = TuneTip(tune);
            x += ButtonWidth + Gap;
        }
        // What the setting it runs on now changes, or the one the mouse rests on would, unless work on it is ordered.
        bool preview = previewTune != null && previewTune.axis == axis;
        PhobosVehicleTuneDef? shown = preview ? previewTune : current;
        float lineHeight = 0f;
        if (order == null && shown != null && FittingText.Effects(shown, plain) is { } effects)
        {
            if (preview) effects = "PhobosVehicleSystems_EffectPreview".Translate(effects.Named("LINE")).Resolve().Colorize(PreviewColour);
            lineHeight = PhobosWidgets.WrappedLine(112f, y + TuneButtonHeight + 2f, view.width - 112f, effects);
        }
        DrawOrderLine(view, y + TuneButtonHeight - ButtonHeight, order);
        y += Mathf.Max(TuneRowHeight, TuneButtonHeight + 2f + lineHeight + 8f);
    }

    string TuneTip(PhobosVehicleTuneDef tune)
    {
        string text = tune.description;
        if (VehicleSystemsMod.Settings.displayMode != DisplayMode.Engineer && FittingText.Effects(tune, VehicleSystemsMod.Settings.displayMode == DisplayMode.Plain) is { } effects)
        {
            text += "\n\n" + effects;
        }
        if (VehicleSystemsMod.Settings.displayMode == DisplayMode.Engineer && TuneFigures(tune) is { } figures) text += "\n\n" + figures;
        if (tune.minSkill > 0) text += "\n\n" + "PhobosVehicleSystems_FitNeedsSkill".Translate(tune.minSkill.Named("SKILL")).CapitalizeFirst();
        if (tune.materials.Count > 0) text += "\n" + Materials(Fittings.MaterialsFor(tune.materials));
        text += "\n" + Hours(tune.work);
        return text;
    }

    /// <summary>The figures a tune moves on this vehicle, for the Engineer display mode: the same factors the fittings service applies.</summary>
    string? TuneFigures(PhobosVehicleTuneDef tune)
    {
        var figures = new List<string>();
        if (tune.axis == VehicleTuneAxis.Gearing && Fittings.FinalDriveAsBuilt is var built && !float.IsNaN(built))
        {
            string ratio = (built * tune.finalDriveFactor).ToString("0.00");
            figures.Add(tune.finalDriveFactor == 1f
                ? "PhobosVehicleSystems_TuneFigureFinalDriveAsBuilt".Translate(ratio.Named("RATIO")).Resolve()
                : "PhobosVehicleSystems_TuneFigureFinalDrive".Translate(ratio.Named("RATIO"), built.ToString("0.00").Named("BUILT"),
                    Factor(tune.finalDriveFactor).Named("FACTOR")).Resolve());
        }
        if (tune.powerFactor != 1f) figures.Add(Figure("PhobosVehicleSystems_TuneFigurePower".Translate(), tune.powerFactor));
        float fuel = VehicleFittings.TuneFuelFactor(tune);
        if (fuel != 1f) figures.Add(Figure("PhobosVehicleSystems_TuneFigureFuel".Translate(), fuel));
        else if (tune.axis == VehicleTuneAxis.Gearing && !tune.isDefault && VehicleFittings.FuelFollowsModel) figures.Add("PhobosVehicleSystems_TuneFigureFuelModel".Translate());
        foreach (ParameterFactorSpec spec in tune.parameterFactors)
        {
            if (!spec.label.NullOrEmpty() && spec.factor != 1f) figures.Add(Figure(spec.label, spec.factor));
        }
        return figures.Count == 0 ? null : "PhobosVehicleSystems_TuneFigures".Translate(figures.ToLineList("  - ", capitalizeItems: true).Named("FIGURES")).Resolve();
    }

    static string Figure(string label, float factor) =>
        "PhobosVehicleSystems_TuneFigure".Translate(label.Named("LABEL"), Factor(factor).Named("FACTOR")).Resolve();

    /// <summary>A factor as the Def gives it, unrounded beyond what the Def holds.</summary>
    static string Factor(float factor) => factor.ToString("0.###");

    /// <summary>
    /// A part's row, or an axle's (Framework 0.88.0 and 0.89.0; owner, 11 Nov 2026): its fittings and grades as picture buttons borrowed
    /// from the tune row, what it has lit, and under them what its fitting and the tune landing on it give together. An axle's choice is
    /// ordered for each of its wheels, and lit only when every wheel has it. Resting the mouse on any choice, here or a tune above, shows
    /// what the row would give instead. A row with more choices than fit across falls back to the menus, part by part.
    /// </summary>
    void DrawRow(Rect view, ref float y, RowModel model)
    {
        List<VehicleComponent> parts = model.Parts;
        string name = model.Name;
        string? nameTip = model.NameTip;
        VehicleComponent first = parts[0];
        List<PhobosVehicleFittingDef?> fitted = parts.Select(part => Fittings.FittedOn(part.props.key)).ToList();
        List<FitOrder> orders = parts.Select(part => Fittings.OrderOn(part.props.key)).Where(o => o != null).Select(o => o!).ToList();
        // The fitting most of the row has, and how many have it; null when none has one.
        PhobosVehicleFittingDef? common = fitted.Where(f => f != null).GroupBy(f => f).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key;
        int commonCount = fitted.Count(f => f != null && f == common);
        bool anyFitted = fitted.Any(f => f != null);
        List<PhobosVehicleFittingDef> options = model.Options;
        List<PhobosVehiclePartGradeDef> grades = model.Gradable ? gradeList : NoGrades;
        int fitButtons = options.Count > 0 || anyFitted ? options.Count + 1 : 0;
        string noneLabel = "PhobosVehicleSystems_FitNoneShort".Translate();
        float buttonsWidth = (fitButtons > 0 ? PhobosWidgets.ChoiceWidth(null, noneLabel, PictureButton) + 2f + options.Sum(o => PhobosWidgets.ChoiceWidth(o.Icon, o.LabelCap, PictureButton) + 2f) + GroupGap : 0f)
                             + grades.Sum(g => PhobosWidgets.ChoiceWidth(g.Icon, g.LabelCap, PictureButton) + 2f);
        if (PartNameWidth + buttonsWidth > view.width)
        {
            foreach (VehicleComponent part in parts) DrawPartMenus(view, ref y, part);
            return;
        }
        bool plain = VehicleSystemsMod.Settings.displayMode == DisplayMode.Plain;
        var nameRect = new Rect(0f, y, PartNameWidth - 4f, PictureButton);
        using (new TextBlock(GameFont.Small, TextAnchor.MiddleLeft)) Widgets.Label(nameRect, name);
        if (nameTip != null) TooltipHandler.TipRegion(nameRect, nameTip);
        float x = PartNameWidth;
        bool hoverFitting = false;
        PhobosVehicleFittingDef? hovered = null;
        if (fitButtons > 0)
        {
            var none = new Rect(x, y, PhobosWidgets.ChoiceWidth(null, noneLabel, PictureButton), PictureButton);
            bool noneClicked = Choice(none, null, noneLabel, !anyFitted, null, orders.Any(o => o.remove), out bool overNone);
            // A choice's details are put together only while the mouse rests on it, not for every button every frame.
            if (overNone) hoverDetails = common != null ? "PhobosVehicleSystems_FitRemove".Translate(common.label.Named("FITTING")).Resolve() : "PhobosVehicleSystems_FitPartNone".Translate().CapitalizeFirst().Resolve();
            if (noneClicked)
            {
                for (int i = 0; i < parts.Count; i++)
                {
                    if (fitted[i] != null) Fittings.OrderRemoval(parts[i]);
                }
            }
            if (anyFitted && Mouse.IsOver(none)) hoverFitting = true;
            x += none.width + 2f;
            foreach (PhobosVehicleFittingDef def in options)
            {
                var button = new Rect(x, y, PhobosWidgets.ChoiceWidth(def.Icon, def.LabelCap, PictureButton), PictureButton);
                string? refusal = parts.Select(part => Fittings.Refusal(def, part)).FirstOrDefault(r => r != null);
                bool all = fitted.All(f => f == def);
                bool clicked = Choice(button, def.Icon, def.LabelCap, all, refusal, orders.Any(o => o.fitting == def), out bool over);
                if (over) hoverDetails = FittingTip(def, refusal);
                if (clicked)
                {
                    for (int i = 0; i < parts.Count; i++)
                    {
                        if (fitted[i] != def) Fittings.OrderFitting(parts[i], def);
                    }
                }
                if (!all && Mouse.IsOver(button))
                {
                    hoverFitting = true;
                    hovered = def;
                }
                x += button.width + 2f;
            }
            x += GroupGap;
        }
        foreach (PhobosVehiclePartGradeDef grade in grades)
        {
            var button = new Rect(x, y, PhobosWidgets.ChoiceWidth(grade.Icon, grade.LabelCap, PictureButton), PictureButton);
            bool lit = parts.All(part => Fittings.GradeOf(part.props.key) == grade && part.Health > 0f);
            string? refusal = parts.Select(part => Fittings.Refusal(grade, part)).FirstOrDefault(r => r != null);
            bool clicked = Choice(button, grade.Icon, grade.LabelCap, lit, refusal, orders.Any(o => o.grade == grade), out bool over);
            if (over) hoverDetails = GradeTip(parts, grade, refusal);
            if (clicked)
            {
                foreach (VehicleComponent part in parts)
                {
                    if (Fittings.GradeOf(part.props.key) != grade || part.Health <= 0f) Fittings.OrderGrade(part, grade);
                }
            }
            x += button.width + 2f;
        }

        // Under the buttons: the work ordered, or what the row gives (or would give, while the mouse rests on a choice).
        float rowLine = 0f;
        if (orders.Count > 0)
        {
            DrawOrderLines(view, y + PictureButton - ButtonHeight, orders);
        }
        else
        {
            VehicleTuneAxis? axis = Fittings.AxisOf(first);
            PhobosVehicleTuneDef? running = axis == null ? null : Fittings.Current(axis.Value);
            bool tunePreview = axis != null && previewTune != null && previewTune.axis == axis;
            PhobosVehicleTuneDef? tune = tunePreview ? previewTune : running;
            PhobosVehicleFittingDef? fitting = hoverFitting ? hovered : common;
            int count = hoverFitting ? (hovered == null ? 0 : parts.Count) : commonCount;
            string? line = FittingText.RowLine(fitting, tune, Fittings.RowFigures(fitting, tune, count), plain);
            if (line != null)
            {
                if (!hoverFitting && parts.Count > 1 && commonCount > 0 && commonCount < parts.Count)
                {
                    line = "PhobosVehicleSystems_AxleMixed".Translate(line.Named("LINE"), commonCount.Named("FITTED"), parts.Count.Named("WHEELS")).Resolve();
                }
                if (hoverFitting || tunePreview) line = "PhobosVehicleSystems_EffectPreview".Translate(line.Named("LINE")).Resolve().Colorize(PreviewColour);
                rowLine = PhobosWidgets.WrappedLine(PartNameWidth, y + PictureButton + 2f, view.width - PartNameWidth, line);
            }
        }
        y += Mathf.Max(RowHeight, PictureButton + 2f + rowLine + 8f);
    }

    /// <summary>
    /// A family's row (Framework 0.103.0; owner, 12 Nov 2026: a family in one order, or part by part): its picture button, lit green once every
    /// part it suits carries it, an amber bar while its work waits, dimmed with the reason when it cannot be ordered. One click orders every
    /// fitting of it this vehicle can take; the parts below still take each one on its own.
    /// </summary>
    void DrawFamily(Rect view, ref float y, FamilyModel model)
    {
        PhobosVehicleFamilyDef family = model.Def;
        using (new TextBlock(GameFont.Small, TextAnchor.MiddleLeft)) Widgets.Label(new Rect(0f, y, PartNameWidth - 4f, PictureButton), family.LabelCap);
        string label = "PhobosVehicleSystems_FamilyButton".Translate().Resolve();
        var button = new Rect(PartNameWidth, y, PhobosWidgets.ChoiceWidth(family.Icon, label, PictureButton), PictureButton);
        bool clicked = Choice(button, family.Icon, label, model.Fitted, model.Steps.Count == 0 ? model.Refusal ?? "" : null, model.Ordered, out bool over);
        if (over) hoverDetails = FamilyTip(model);
        if (clicked) Fittings.OrderFamily(family);
        string line = model.Fitted ? "PhobosVehicleSystems_FamilyLineFitted".Translate().Resolve()
            : model.Steps.Count > 0 ? "PhobosVehicleSystems_FamilyLineParts".Translate(model.Steps.Count.Named("COUNT")).Resolve()
            : model.Refusal ?? "";
        float lineHeight = line.NullOrEmpty() ? 0f : PhobosWidgets.WrappedLine(button.xMax + 8f, y + 8f, view.width - button.xMax - 8f, line);
        y += Mathf.Max(RowHeight, Mathf.Max(PictureButton, lineHeight + 8f) + 10f);
    }

    /// <summary>The details strip for a family: what it is, what ordering it does here, what it costs and how long it takes.</summary>
    string FamilyTip(FamilyModel model)
    {
        PhobosVehicleFamilyDef family = model.Def;
        string tip = family.LabelCap + "\n\n" + family.description;
        if (model.Steps.Count > 0)
        {
            IEnumerable<string> steps = model.Steps.Select(step => step.Replaces != null
                ? "PhobosVehicleSystems_FamilyStepReplaces".Translate(step.Fitting.label.Named("FITTING"), step.Part.props.label.Named("PART"), step.Replaces.label.Named("OLD")).Resolve()
                : "PhobosVehicleSystems_FamilyStep".Translate(step.Fitting.label.Named("FITTING"), step.Part.props.label.Named("PART")).Resolve());
            tip += "\n\n" + "PhobosVehicleSystems_FamilyWork".Translate(steps.ToLineList("  - ", capitalizeItems: true).Named("STEPS")).Resolve();
            var materials = new Dictionary<ThingDef, int>();
            float work = 0f;
            foreach (VehicleFittings.FamilyStep step in model.Steps)
            {
                foreach (ThingDefCountClass m in Fittings.MaterialsFor(step.Fitting.materials)) materials[m.thingDef] = (materials.TryGetValue(m.thingDef, out int had) ? had : 0) + m.count;
                work += Fittings.WorkFor(step.Fitting.work);
            }
            tip += "\n" + Materials(materials.Select(kv => new ThingDefCountClass(kv.Key, kv.Value)));
            tip += "\n" + "PhobosVehicleSystems_FitHours".Translate((work / GenDate.TicksPerHour).ToString("0.#").Named("HOURS")).Resolve();
            int skill = model.Steps.Max(step => step.Fitting.minSkill);
            if (skill > 0) tip += "\n\n" + "PhobosVehicleSystems_FitNeedsSkill".Translate(skill.Named("SKILL")).CapitalizeFirst();
        }
        if (model.Refusal != null) tip += "\n\n" + model.Refusal.Colorize(PhobosWidgets.Bad);
        return tip;
    }

    /// <summary>A row's name: the part's own, or an axle's by its place, front first (and "rear" for the last of several).</summary>
    static string AxleName(int index, int count) =>
        index == 0 ? "PhobosVehicleSystems_AxleFront".Translate().Resolve()
        : index == count - 1 ? "PhobosVehicleSystems_AxleRear".Translate().Resolve()
        : "PhobosVehicleSystems_AxleNumbered".Translate((index + 1).Named("NUMBER")).Resolve();

    /// <summary>The work ordered on a row: one line, naming how many parts it is for, with one button that cancels it all.</summary>
    void DrawOrderLines(Rect view, float y, List<FitOrder> orders)
    {
        var line = new Rect(0f, y + ButtonHeight + 2f, view.width - 80f, 20f);
        using (new TextBlock(GameFont.Tiny, TextAnchor.MiddleLeft))
        {
            string work = Fittings.LabelOf(orders[0]);
            Widgets.Label(line, orders.Count == 1
                ? "PhobosVehicleSystems_FitOrdered".Translate(work.Named("WORK")).Resolve()
                : "PhobosVehicleSystems_FitOrderedAxle".Translate(work.Named("WORK"), orders.Count.Named("COUNT")).Resolve());
            if (Widgets.ButtonText(new Rect(view.width - 74f, line.y, 74f, 20f), "PhobosVehicleSystems_FitCancel".Translate()))
            {
                foreach (FitOrder order in orders.ToList()) Fittings.Cancel(order);
            }
        }
    }

    /// <summary>
    /// One choice as a square picture button, the tune row's look: lit green when it is what the part has, an amber bar when it is the work
    /// ordered, dimmed with its reason in the tooltip when it cannot be chosen yet. Returns whether it was clicked to choose it.
    /// </summary>
    static bool Choice(Rect rect, UnityEngine.Texture2D? icon, string label, bool lit, string? refusal, bool ordered, out bool over) =>
        PhobosWidgets.ChoiceButton(rect, icon, label, lit, refusal != null, ordered, out over);

    string FittingTip(PhobosVehicleFittingDef def, string? refusal)
    {
        string tip = def.LabelCap + " (" + Materials(Fittings.MaterialsFor(def.materials)) + ")\n" + Hours(def.work) + "\n\n" + def.description;
        if (FittingText.Effects(def, VehicleSystemsMod.Settings.displayMode == DisplayMode.Plain) is { } effects) tip += "\n\n" + effects;
        if (def.minSkill > 0) tip += "\n\n" + "PhobosVehicleSystems_FitNeedsSkill".Translate(def.minSkill.Named("SKILL")).CapitalizeFirst();
        if (refusal != null) tip += "\n\n" + refusal.Colorize(PhobosWidgets.Bad);
        return tip;
    }

    string GradeTip(List<VehicleComponent> parts, PhobosVehiclePartGradeDef grade, string? refusal)
    {
        string cost = comp.Repairs.CostLine(parts[0], grade);
        if (parts.Count > 1) cost = "PhobosVehicleSystems_AxleEach".Translate(cost.Named("COST"), parts.Count.Named("COUNT")).Resolve();
        string tip = grade.LabelCap + " (" + cost + ")\n\n" + grade.description;
        if (grade.minSkill > 0) tip += "\n\n" + "PhobosVehicleSystems_FitNeedsSkill".Translate(grade.minSkill.Named("SKILL")).CapitalizeFirst();
        if (refusal != null) tip += "\n\n" + refusal.Colorize(PhobosWidgets.Bad);
        return tip;
    }

    /// <summary>The part row as it was before 0.88.0, with menus: for a part with more choices than fit across as pictures.</summary>
    void DrawPartMenus(Rect view, ref float y, VehicleComponent part)
    {
        PhobosVehicleFittingDef? on = Fittings.FittedOn(part.props.key);
        PhobosVehiclePartGradeDef grade = Fittings.GradeOf(part.props.key);
        FitOrder? order = Fittings.OrderOn(part.props.key);
        const float LabelWidth = 190f;
        float buttonsX = view.width - 2f * ButtonWidth - Gap;
        using (new TextBlock(GameFont.Small, TextAnchor.UpperLeft)) Widgets.Label(new Rect(0f, y + 2f, LabelWidth, 24f), part.props.label.CapitalizeFirst());
        string fitting = on != null ? on.label : "PhobosVehicleSystems_FitPartNone".Translate().Resolve();
        // The fitting's and the grade's pictures before the words, as the Health tab shows them.
        float statusX = LabelWidth + 4f;
        foreach (UnityEngine.Texture2D? icon in new[] { on?.Icon, grade.Icon })
        {
            if (icon == null) continue;
            GUI.DrawTexture(new Rect(statusX, y + 2f, IconSize, IconSize), icon);
            statusX += IconSize + 2f;
        }
        var statusRect = new Rect(statusX, y + 2f, buttonsX - statusX - 4f, 24f);
        using (new TextBlock(GameFont.Tiny, TextAnchor.UpperLeft)) Widgets.Label(statusRect, "PhobosVehicleSystems_FitPartStatus".Translate(grade.LabelCap.Named("GRADE"), fitting.Named("FITTING")));
        if (order == null && on != null && FittingText.Effects(on, VehicleSystemsMod.Settings.displayMode == DisplayMode.Plain) is { } effects)
        {
            using (new TextBlock(GameFont.Tiny, TextAnchor.MiddleLeft)) Widgets.Label(new Rect(LabelWidth + 4f, y + ButtonHeight + 2f, buttonsX - LabelWidth - 8f, 20f), effects);
        }
        TooltipHandler.TipRegion(statusRect, grade.description + (on != null ? "\n\n" + on.description : ""));
        if ((on != null || Fittings.OptionsFor(part).Any()) && Widgets.ButtonText(new Rect(buttonsX, y, ButtonWidth, ButtonHeight), "PhobosVehicleSystems_FitButton".Translate()))
        {
            Find.WindowStack.Add(new FloatMenu(Options(part, on)));
        }
        if (comp.Repairs.CanBeGraded(part) && Widgets.ButtonText(new Rect(buttonsX + ButtonWidth + Gap, y, ButtonWidth, ButtonHeight), "PhobosVehicleSystems_GradeButton".Translate()))
        {
            Find.WindowStack.Add(new FloatMenu(GradeOptions(part, grade)));
        }
        DrawOrderLine(view, y, order);
        y += RowHeight;
    }

    List<FloatMenuOption> Options(VehicleComponent part, PhobosVehicleFittingDef? on)
    {
        var options = new List<FloatMenuOption>();
        foreach (PhobosVehicleFittingDef def in Fittings.OptionsFor(part))
        {
            if (def == on) continue;
            string label = def.LabelCap + " (" + Materials(Fittings.MaterialsFor(def.materials)) + ")";
            string tip = def.description + (FittingText.Effects(def, VehicleSystemsMod.Settings.displayMode == DisplayMode.Plain) is { } effects ? "\n\n" + effects : "")
                + (def.minSkill > 0 ? "\n\n" + "PhobosVehicleSystems_FitNeedsSkill".Translate(def.minSkill.Named("SKILL")).CapitalizeFirst() : "");
            string? refusal = Fittings.Refusal(def, part);
            PhobosVehicleFittingDef captured = def;
            string text = refusal == null ? label : label + ": " + refusal;
            Action? act = refusal == null ? () => Fittings.OrderFitting(part, captured) : null;
            options.Add(def.Icon is { } icon
                ? new FloatMenuOption(text, act, icon, Color.white, mouseoverGuiAction: rect => TooltipHandler.TipRegion(rect, tip))
                : new FloatMenuOption(text, act, mouseoverGuiAction: rect => TooltipHandler.TipRegion(rect, tip)));
        }
        if (on != null) options.Add(new FloatMenuOption("PhobosVehicleSystems_FitRemove".Translate(on.label.Named("FITTING")), () => Fittings.OrderRemoval(part)));
        return options;
    }

    List<FloatMenuOption> GradeOptions(VehicleComponent part, PhobosVehiclePartGradeDef current)
    {
        var options = new List<FloatMenuOption>();
        foreach (PhobosVehiclePartGradeDef grade in VehicleFittings.Grades)
        {
            if (grade == current && part.Health > 0f) continue;
            string label = grade.LabelCap + " (" + comp.Repairs.CostLine(part, grade) + ")";
            string tip = grade.description + (grade.minSkill > 0 ? "\n\n" + "PhobosVehicleSystems_FitNeedsSkill".Translate(grade.minSkill.Named("SKILL")).CapitalizeFirst() : "");
            string? refusal = Fittings.Refusal(grade, part);
            PhobosVehiclePartGradeDef captured = grade;
            string text = refusal == null ? label : label + ": " + refusal;
            Action? act = refusal == null ? () => Fittings.OrderGrade(part, captured) : null;
            options.Add(grade.Icon is { } icon
                ? new FloatMenuOption(text, act, icon, Color.white, mouseoverGuiAction: rect => TooltipHandler.TipRegion(rect, tip))
                : new FloatMenuOption(text, act, mouseoverGuiAction: rect => TooltipHandler.TipRegion(rect, tip)));
        }
        return options;
    }

    void DrawOrderLine(Rect view, float y, FitOrder? order)
    {
        if (order == null) return;
        var line = new Rect(0f, y + ButtonHeight + 2f, view.width - 80f, 20f);
        using (new TextBlock(GameFont.Tiny, TextAnchor.MiddleLeft))
        {
            Widgets.Label(line, "PhobosVehicleSystems_FitOrdered".Translate(Fittings.LabelOf(order).Named("WORK")));
            if (Widgets.ButtonText(new Rect(view.width - 74f, line.y, 74f, 20f), "PhobosVehicleSystems_FitCancel".Translate())) Fittings.Cancel(order);
        }
    }

    /// <summary>How long the work takes on this vehicle, in hours for a builder of normal speed (Framework 0.91.0: time is now a real cost).</summary>
    string Hours(float listedWork) =>
        "PhobosVehicleSystems_FitHours".Translate((Fittings.WorkFor(listedWork) / GenDate.TicksPerHour).ToString("0.#").Named("HOURS")).Resolve();

    static string Materials(IEnumerable<ThingDefCountClass> materials) => materials.Select(m => m.thingDef.label + " x" + m.count).ToCommaList(useAnd: true);
}
