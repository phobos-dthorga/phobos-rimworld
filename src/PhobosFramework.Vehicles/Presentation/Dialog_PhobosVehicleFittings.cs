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

    public override Vector2 InitialSize => new(640f, 560f);

    VehicleFittings Fittings => comp.Fittings;

    public override void DoWindowContents(Rect inRect)
    {
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

        List<VehicleTuneAxis> axes = new[] { VehicleTuneAxis.Engine, VehicleTuneAxis.Gearing }.Where(Fittings.CanTune).ToList();
        List<VehicleComponent> parts = Fittings.ListedParts().ToList();
        var outer = new Rect(inRect.x, y, inRect.width, inRect.yMax - y);
        float content = SectionHeight * 2f + TuneRowHeight * Mathf.Max(axes.Count, 1) + RowHeight * Mathf.Max(parts.Count, 1);
        var view = new Rect(0f, 0f, outer.width - 18f, content);
        Widgets.BeginScrollView(outer, ref scroll, view);
        float row = 0f;
        DrawSection(view, ref row, "PhobosVehicleSystems_FitSectionTunes".Translate());
        if (axes.Count == 0) DrawNote(view, ref row, "PhobosVehicleSystems_FitNoTunes".Translate());
        foreach (VehicleTuneAxis axis in axes) DrawTune(view, ref row, axis);
        DrawSection(view, ref row, "PhobosVehicleSystems_FitSectionParts".Translate());
        if (parts.Count == 0) DrawNote(view, ref row, "PhobosVehicleSystems_FitNoParts".Translate());
        foreach (VehicleComponent part in parts) DrawPart(view, ref row, part);
        Widgets.EndScrollView();
    }

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
            if (!isCurrent && Widgets.ButtonInvisible(button)) Fittings.OrderTune(tune);
            TooltipHandler.TipRegion(button, TuneTip(tune));
            x += ButtonWidth + Gap;
        }
        // What the setting it runs on now changes, unless work on it is ordered.
        if (order == null && current != null && FittingText.Effects(current, plain) is { } effects)
        {
            using (new TextBlock(GameFont.Tiny, TextAnchor.MiddleLeft)) Widgets.Label(new Rect(112f, y + TuneButtonHeight + 2f, view.width - 112f, 20f), effects);
        }
        DrawOrderLine(view, y + TuneButtonHeight - ButtonHeight, order);
        y += TuneRowHeight;
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
        if (tune.materials.Count > 0) text += "\n" + Materials(tune.materials);
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

    void DrawPart(Rect view, ref float y, VehicleComponent part)
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
            string label = def.LabelCap + " (" + Materials(def.materials) + ")";
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

    static string Materials(IEnumerable<ThingDefCountClass> materials) => materials.Select(m => m.thingDef.label + " x" + m.count).ToCommaList(useAnd: true);
}
