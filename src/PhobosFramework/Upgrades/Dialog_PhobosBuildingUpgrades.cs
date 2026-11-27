using System;
using System.Collections.Generic;
using Phobos.Framework.Interface;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Phobos.Framework.Upgrades;

/// <summary>
/// A building's upgrades, in the game's own style (D'thorga Framework 0.210.0): each one it can take, what it does and costs, whether it is
/// fitted, under way or why it cannot be ordered, with the button that goes with it. Presentation only: every button is one call to
/// <see cref="BuildingUpgrades"/>, and a button that cannot be pressed says why.
/// <para>Since 0.353.1 (the owner's report of 27 Nov 2026: pressing Fit "feels like nothing has happened") an order shows at once: the game's
/// confirm tick, and the ordered upgrade's card highlighted, named as ordered, with what it waits for and its own Cancel button.</para>
/// </summary>
public class Dialog_PhobosBuildingUpgrades : Window
{
    const float RowGap = 8f;
    const float ButtonWidth = 150f;
    const float ButtonHeight = 32f;
    const string GuardKey = "The building upgrades window";

    readonly CompPhobosBuildingUpgrades comp;
    Vector2 scroll;
    float viewHeight = 400f;

    public Dialog_PhobosBuildingUpgrades(CompPhobosBuildingUpgrades comp)
    {
        this.comp = comp;
        doCloseX = true;
        doCloseButton = true;
        closeOnClickedOutside = true;
        absorbInputAroundWindow = true;
        forcePause = false;
    }

    public override Vector2 InitialSize => new(620f, 560f);

    public override void DoWindowContents(Rect inRect)
    {
        // Guarded: a throw is logged once and the window rests, where the game would log it every frame (UiGuard).
        if (UiGuard.Resting(GuardKey)) { UiGuard.Fallback(inRect); return; }
        try { Contents(inRect); }
        catch (Exception e) { UiGuard.Report(GuardKey, e); }
    }

    void Contents(Rect inRect)
    {
        if (!comp.parent.Spawned)
        {
            Close();
            return;
        }
        float y = inRect.y;
        using (new TextBlock(GameFont.Medium, TextAnchor.UpperLeft))
        {
            string title = "PhobosUpgrade_WindowTitle".Translate(comp.parent.LabelCap.Named("BUILDING")).Resolve();
            Widgets.Label(new Rect(inRect.x, y, inRect.width, 34f), title);
            y += 36f;
        }
        if (comp.Ordered != null)
        {
            y += Line(inRect.x, y, inRect.width, BuildingUpgrades.OrderLine(comp), ColoredText.SubtleGrayColor);
            var cancel = new Rect(inRect.xMax - ButtonWidth, y, ButtonWidth, ButtonHeight - 4f);
            if (Widgets.ButtonText(cancel, "PhobosUpgrade_CancelButton".Translate())) BuildingUpgrades.Cancel(comp);
            TooltipHandler.TipRegion(cancel, "PhobosUpgrade_CancelTip".Translate());
            y += ButtonHeight + 2f;
        }
        var outer = new Rect(inRect.x, y, inRect.width, inRect.yMax - y - CloseButSize.y - 8f);
        var view = new Rect(0f, 0f, outer.width - 16f, viewHeight);
        Widgets.BeginScrollView(outer, ref scroll, view);
        float rowY = 0f;
        List<PhobosBuildingUpgradeDef> upgrades = comp.Available;
        for (int i = 0; i < upgrades.Count; i++) rowY += DrawRow(new Rect(0f, rowY, view.width, 0f), upgrades[i]) + RowGap;
        Widgets.EndScrollView();
        viewHeight = rowY;
    }

    /// <summary>One upgrade's card; returns the height it took.</summary>
    float DrawRow(Rect at, PhobosBuildingUpgradeDef def)
    {
        float textWidth = at.width - ButtonWidth - 24f;
        bool fitted = comp.Has(def);
        // 0.353.1: the upgrade under way, being fitted or taken off, and what it waits for.
        string? order = comp.Ordered == def ? BuildingUpgrades.OrderLine(comp) : null;
        float height = Mathf.Max(Words(at, def, textWidth, fitted, order, comp.Removing, draw: false) + 12f, ButtonHeight + 16f);
        var card = new Rect(at.x, at.y, at.width, height);
        Widgets.DrawMenuSection(card);
        if (order != null) Widgets.DrawHighlightSelected(card);
        Words(at, def, textWidth, fitted, order, comp.Removing, draw: true);

        var button = new Rect(card.xMax - ButtonWidth - 8f, card.y + 8f, ButtonWidth, ButtonHeight);
        if (order != null)
        {
            if (Widgets.ButtonText(button, "PhobosUpgrade_CancelButton".Translate())) BuildingUpgrades.Cancel(comp);
            TooltipHandler.TipRegion(button, "PhobosUpgrade_CancelTip".Translate());
        }
        else if (fitted)
        {
            string? refused = BuildingUpgrades.RemoveRefusal(comp, def);
            if (Widgets.ButtonText(button, "PhobosUpgrade_RemoveButton".Translate(), active: refused == null) && refused == null)
            {
                BuildingUpgrades.OrderRemoval(comp, def);
                if (comp.Ordered == def) SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }
            TooltipHandler.TipRegion(button, refused ?? "PhobosUpgrade_RemoveTip".Translate(Mathf.RoundToInt(def.refundShare * 100f).Named("PERCENT")).Resolve());
        }
        else
        {
            string? refused = BuildingUpgrades.Refusal(comp, def);
            if (Widgets.ButtonText(button, "PhobosUpgrade_FitButton".Translate(), active: refused == null) && refused == null)
            {
                BuildingUpgrades.Order(comp, def);
                if (comp.Ordered == def) SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }
            TooltipHandler.TipRegion(button, refused ?? "PhobosUpgrade_FitTip".Translate(def.CostText.Named("COST")).Resolve());
        }
        return height;
    }

    /// <summary>The card's words: measured only, or drawn. Returns their height.</summary>
    static float Words(Rect at, PhobosBuildingUpgradeDef def, float width, bool fitted, string? order, bool removing, bool draw)
    {
        float x = at.x + 8f;
        float y = at.y + 6f;
        NamedArgument label = def.label.CapitalizeFirst().Named("UPGRADE");
        string name = order != null
            ? (removing ? "PhobosUpgrade_NameRemoving" : "PhobosUpgrade_NameOrdered").Translate(label).Resolve()
            : fitted ? "PhobosUpgrade_NameFitted".Translate(label).Resolve() : def.label.CapitalizeFirst();
        y += Line(x, y, width, name, order != null ? PhobosWidgets.Caution : fitted ? PhobosWidgets.Good : Color.white, draw);
        if (order != null) y += Line(x, y, width, order, PhobosWidgets.Caution, draw);
        y += Line(x, y, width, def.description, new Color(1f, 1f, 1f, 0.85f), draw);
        y += Line(x, y, width, "PhobosUpgrade_Cost".Translate(def.CostText.Named("COST")), ColoredText.SubtleGrayColor, draw);
        if (def.prerequisite != null) y += Line(x, y, width, "PhobosUpgrade_BuildsOn".Translate(def.prerequisite.label.Named("FIRST")), ColoredText.SubtleGrayColor, draw);
        if (def.researchPrerequisite != null)
        {
            y += Line(x, y, width, "PhobosUpgrade_Research".Translate(def.researchPrerequisite.label.Named("RESEARCH")),
                def.Researched ? ColoredText.SubtleGrayColor : PhobosWidgets.Caution, draw);
        }
        if (def.constructionSkill > 0) y += Line(x, y, width, "PhobosUpgrade_Skill".Translate(def.constructionSkill.Named("LEVEL")), ColoredText.SubtleGrayColor, draw);
        return y - at.y;
    }

    /// <summary>A wrapped line in the small font; returns the height it took.</summary>
    static float Line(float x, float y, float width, string text, Color colour, bool draw = true)
    {
        using (new TextBlock(GameFont.Small, TextAnchor.UpperLeft, colour))
        {
            float height = Text.CalcHeight(text, width);
            if (draw) Widgets.Label(new Rect(x, y, width, height), text);
            return height + 1f;
        }
    }
}
