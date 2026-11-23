using System;
using Phobos.Radio.Services;
using UnityEngine;
using Verse;

namespace Phobos.Radio.UI;

/// <summary>
/// The link-budget window. Opens at the player's display mode and can expand one level deeper on
/// request, as the design says; re-reads the net's cache every frame so it follows the pawn.
/// </summary>
public class Dialog_LinkBudget : Window
{
    private readonly string title;
    private readonly Func<LinkEvaluation?> link;
    private DisplayMode depth;
    private Vector2 scroll;

    public Dialog_LinkBudget(string title, Func<LinkEvaluation?> link)
    {
        this.title = title;
        this.link = link;
        depth = RadioMod.Settings.displayMode;
        doCloseX = true;
        draggable = true;
        forcePause = false;
        absorbInputAroundWindow = false;
        closeOnClickedOutside = false;
        preventCameraMotion = false;
    }

    /// <summary>Wide enough for Engineer's two step charts side by side; the words below scroll.</summary>
    public override Vector2 InitialSize => new(640f, 460f + PathProfile.Height + BudgetSteps.Height + 20f);

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(0f, 0f, inRect.width - 40f, 36f), title);
        Text.Font = GameFont.Small;

        var current = link();
        var top = 40f;
        // The side view of the path (Radio 0.3.0); its station's map is the map the path crosses.
        if (current != null && current.B.Thing.MapHeld is { } map)
        {
            PathProfile.Draw(new Rect(0f, top, inRect.width, PathProfile.Height), LinkPath.For(map, current), current, depth);
            top += PathProfile.Height + 10f;
        }
        // The step chart of the budget (Radio 0.6.0), at Operator and Engineer depth: Plain shows no decibels.
        if (current != null && depth != DisplayMode.Plain)
        {
            BudgetSteps.Draw(new Rect(0f, top, inRect.width, BudgetSteps.Height), current, depth);
            top += BudgetSteps.Height + 10f;
        }

        var body = new Rect(0f, top, inRect.width, inRect.height - top - 40f);
        var text = LinkText.Describe(current, depth);
        var height = Text.CalcHeight(text, body.width - 20f) + 10f;
        Widgets.BeginScrollView(body, ref scroll, new Rect(0f, 0f, body.width - 20f, height));
        Widgets.Label(new Rect(0f, 0f, body.width - 20f, height), text);
        Widgets.EndScrollView();

        var buttons = new Rect(0f, inRect.height - 32f, inRect.width, 32f);
        if (depth < DisplayMode.Engineer && Widgets.ButtonText(new Rect(buttons.x, buttons.y, 160f, 32f), "PhobosRadio_ShowMore".Translate()))
        {
            depth++;
        }
        if (depth > DisplayMode.Plain && Widgets.ButtonText(new Rect(buttons.x + 170f, buttons.y, 160f, 32f), "PhobosRadio_ShowLess".Translate()))
        {
            depth--;
        }
        if (Widgets.ButtonText(new Rect(buttons.xMax - 120f, buttons.y, 120f, 32f), "CloseButton".Translate()))
        {
            Close();
        }
    }
}
