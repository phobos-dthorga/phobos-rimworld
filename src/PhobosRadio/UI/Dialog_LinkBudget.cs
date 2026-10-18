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

    public override Vector2 InitialSize => new(520f, 560f);

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(0f, 0f, inRect.width - 40f, 36f), title);
        Text.Font = GameFont.Small;

        var body = new Rect(0f, 40f, inRect.width, inRect.height - 40f - 40f);
        var text = LinkText.Describe(link(), depth);
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
