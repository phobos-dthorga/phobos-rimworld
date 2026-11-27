using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Phobos.Framework.Setup;

/// <summary>
/// The setup wizard's window (Framework 0.362.0; docs/development/first-load-setup-wizard.md): the steps down the left as the settings
/// window lists its pages, a welcome, a page per mod (its preview, what it does, what it changes in other mods, presets first and each
/// setting on request), and a summary. Presentation only: every change goes through <see cref="SetupSession"/>.
/// </summary>
public class Dialog_PhobosSetup : Window
{
    const float StepListWidth = 200f;
    const float ColumnGap = 14f;
    const float FooterHeight = 38f;
    const float StepRowHeight = 30f;
    const float PictureWidth = 240f;
    const float PresetButtonHeight = 34f;
    const float FooterButtonWidth = 150f;

    readonly SetupSession session;
    readonly HashSet<string> detailsShown = new();
    int step;
    Vector2 contentScroll;
    Vector2 stepScroll;
    float contentHeight = 400f;

    public override Vector2 InitialSize => new(1000f, 720f);

    public Dialog_PhobosSetup(SetupSession session)
    {
        this.session = session;
        forcePause = true;
        absorbInputAroundWindow = true;
        doCloseX = false;
        closeOnAccept = false;
        closeOnCancel = false;
        closeOnClickedOutside = false;
    }

    int LastStep => session.Pages.Count + 1;
    PhobosSetupPageDef? PageAt(int index) => index >= 1 && index <= session.Pages.Count ? session.Pages[index - 1] : null;

    string StepLabel(int index) => index == 0 ? "PhobosFramework_SetupStepWelcome".Translate().ToString()
        : index == LastStep ? "PhobosFramework_SetupStepFinish".Translate().ToString()
        : PageAt(index)!.LabelCap.ToString();

    public override void DoWindowContents(Rect inRect)
    {
        var body = new Rect(inRect.x, inRect.y, inRect.width, inRect.height - FooterHeight - 8f);
        DrawSteps(new Rect(body.x, body.y, StepListWidth, body.height));
        DrawContent(new Rect(body.x + StepListWidth + ColumnGap, body.y, body.width - StepListWidth - ColumnGap, body.height));
        DrawFooter(new Rect(inRect.x, inRect.yMax - FooterHeight, inRect.width, FooterHeight));
    }

    // Keyboard over mouse: Enter goes on, Escape goes back.
    public override void OnAcceptKeyPressed()
    {
        if (step < LastStep) GoTo(step + 1);
        else FinishAndClose();
    }

    public override void OnCancelKeyPressed()
    {
        if (step > 0) GoTo(step - 1);
    }

    void GoTo(int index)
    {
        step = Mathf.Clamp(index, 0, LastStep);
        contentScroll = Vector2.zero;
        SoundDefOf.Click.PlayOneShotOnCamera();
    }

    void DrawSteps(Rect rect)
    {
        int count = LastStep + 1;
        float height = count * (StepRowHeight + 2f);
        bool scrolls = height > rect.height;
        var view = new Rect(0f, 0f, rect.width - (scrolls ? 16f : 0f), height);
        Widgets.BeginScrollView(rect, ref stepScroll, view);
        for (int i = 0; i < count; i++)
        {
            var row = new Rect(0f, i * (StepRowHeight + 2f), view.width, StepRowHeight);
            Widgets.DrawOptionBackground(row, i == step);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(row.x + 8f, row.y, row.width - 12f, row.height), StepLabel(i).Truncate(row.width - 12f));
            Text.Anchor = TextAnchor.UpperLeft;
            if (Widgets.ButtonInvisible(row) && i != step) GoTo(i);
        }
        Widgets.EndScrollView();
    }

    void DrawContent(Rect rect)
    {
        var view = new Rect(0f, 0f, rect.width - 16f, Mathf.Max(contentHeight, rect.height - 4f));
        Widgets.BeginScrollView(rect, ref contentScroll, view);
        var listing = new Listing_Standard();
        listing.Begin(view);
        if (step == 0) DrawWelcome(listing);
        else if (step == LastStep) DrawFinish(listing);
        else DrawPage(listing, PageAt(step)!);
        contentHeight = listing.CurHeight + 12f;
        listing.End();
        Widgets.EndScrollView();
    }

    static void Heading(Listing_Standard listing, string text)
    {
        Text.Font = GameFont.Medium;
        listing.Label(text);
        Text.Font = GameFont.Small;
        listing.Gap(4f);
    }

    static void Subtle(Listing_Standard listing, string text)
    {
        GUI.color = ColoredText.SubtleGrayColor;
        listing.Label(text);
        GUI.color = Color.white;
    }

    void DrawWelcome(Listing_Standard listing)
    {
        Heading(listing, "PhobosFramework_SetupWelcomeTitle".Translate());
        listing.Label(session.FirstTime ? "PhobosFramework_SetupWelcome".Translate() : "PhobosFramework_SetupWelcomeAgain".Translate());
        listing.Gap();
        Subtle(listing, "PhobosFramework_SetupPagesHere".Translate());
        foreach (PhobosSetupPageDef page in session.Pages) listing.Label("  - " + page.LabelCap);
    }

    void DrawPage(Listing_Standard listing, PhobosSetupPageDef page)
    {
        Heading(listing, page.LabelCap);
        if (page.modContentPack != null) Subtle(listing, "PhobosFramework_SetupFrom".Translate(page.modContentPack.Name.Named("MOD")));
        listing.Gap(6f);
        DrawIntroduction(listing, page);

        if (page.otherMods is { Length: > 0 } others)
        {
            listing.Gap();
            GUI.color = SettingsUi.WarningColor;
            listing.Label("PhobosFramework_SetupOtherMods".Translate());
            listing.Label(others);
            GUI.color = Color.white;
        }

        List<SetupSetting> settings = SetupWizard.SettingsOf(page);
        if (settings.Count == 0)
        {
            listing.Gap();
            Subtle(listing, "PhobosFramework_SetupNoSettings".Translate());
        }
        else
        {
            HashSet<string> always = SetupWizard.OutsidePresets(page);
            bool hasPresets = page.presets.Count > 0;
            if (hasPresets) DrawPresets(listing, page);
            bool details = !hasPresets || detailsShown.Contains(page.defName);
            if (hasPresets && settings.Any(s => !always.Contains(s.Id)))
            {
                bool before = details;
                listing.CheckboxLabeled("PhobosFramework_SetupShowEach".Translate(), ref details, "PhobosFramework_SetupShowEachDesc".Translate());
                if (details != before)
                {
                    if (details) detailsShown.Add(page.defName);
                    else detailsShown.Remove(page.defName);
                }
            }
            listing.GapLine();
            foreach (SetupSetting setting in settings)
                if (details || always.Contains(setting.Id)) DrawSetting(listing, setting);
        }

        if (page.moreSettings is { Length: > 0 } more)
        {
            listing.Gap();
            Subtle(listing, more);
        }
        if (page.guideUrl is { Length: > 0 } url)
        {
            listing.Gap();
            Rect button = listing.GetRect(30f);
            button.width = 260f;
            TooltipHandler.TipRegion(button, "PhobosFramework_SetupGuideDesc".Translate());
            if (Widgets.ButtonText(button, "PhobosFramework_SetupGuide".Translate())) Application.OpenURL(url);
        }
    }

    /// <summary>The mod's own preview beside its introduction; the text alone where the mod has no preview.</summary>
    static void DrawIntroduction(Listing_Standard listing, PhobosSetupPageDef page)
    {
        Texture2D? picture = page.modContentPack?.ModMetaData?.PreviewImage;
        string text = page.description ?? "";
        if (picture == null)
        {
            listing.Label(text);
            return;
        }
        float textWidth = listing.ColumnWidth - PictureWidth - ColumnGap;
        float pictureHeight = PictureWidth * picture.height / Mathf.Max(1f, picture.width);
        Rect row = listing.GetRect(Mathf.Max(pictureHeight, Text.CalcHeight(text, textWidth)));
        Widgets.DrawTextureFitted(new Rect(row.x, row.y, PictureWidth, pictureHeight), picture, 1f);
        Widgets.Label(new Rect(row.x + PictureWidth + ColumnGap, row.y, textWidth, row.height), text);
    }

    void DrawPresets(Listing_Standard listing, PhobosSetupPageDef page)
    {
        listing.Gap();
        listing.Label("PhobosFramework_SetupPresets".Translate());
        int matching = SetupWizard.MatchingPreset(page);
        int count = page.presets.Count;
        Rect row = listing.GetRect(PresetButtonHeight);
        float width = (row.width - (count - 1) * 8f) / count;
        for (int i = 0; i < count; i++)
        {
            SetupPreset preset = page.presets[i];
            var button = new Rect(row.x + i * (width + 8f), row.y, width, row.height);
            if (i == matching) Widgets.DrawHighlightSelected(button.ExpandedBy(2f));
            string label = preset.recommended ? preset.label + " " + "PhobosFramework_SetupRecommended".Translate() : preset.label;
            TooltipHandler.TipRegion(button, preset.description);
            if (Widgets.ButtonText(button, label)) session.Apply(page, preset);
        }
        listing.Gap(4f);
        string now = matching >= 0 ? page.presets[matching].label : "PhobosFramework_SetupOwnMix".Translate().ToString();
        Subtle(listing, "PhobosFramework_SetupNow".Translate(now.Named("PRESET")));
        if (matching >= 0 && !page.presets[matching].description.NullOrEmpty()) listing.Label(page.presets[matching].description);
    }

    void DrawSetting(Listing_Standard listing, SetupSetting setting)
    {
        string label = setting.Restart ? setting.Label + "  " + "PhobosFramework_SettingsNeedsRestart".Translate().Colorize(ColoredText.SubtleGrayColor) : setting.Label;
        if (setting.IsTick)
        {
            bool on = setting.Current == "true";
            bool was = on;
            listing.CheckboxLabeled(label, ref on, setting.Tip);
            if (on != was) session.Set(setting, on ? "true" : "false");
            return;
        }
        Rect row = listing.GetRect(Mathf.Max(30f, Text.CalcHeight(label, listing.ColumnWidth / 2f)));
        Widgets.DrawHighlightIfMouseover(row);
        if (setting.Tip is { } tip) TooltipHandler.TipRegion(row, tip);
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(row.LeftHalf(), label);
        Text.Anchor = TextAnchor.UpperLeft;
        bool clicked = Widgets.ButtonText(row.RightHalf().ContractedBy(0f, 2f), setting.OptionLabel(setting.Current));
        listing.Gap(listing.verticalSpacing);
        if (!clicked) return;
        var menu = new List<FloatMenuOption>();
        foreach (string key in setting.Options)
        {
            string chosen = key;
            menu.Add(new FloatMenuOption(setting.OptionLabel(chosen), () => session.Set(setting, chosen)));
        }
        Find.WindowStack.Add(new FloatMenu(menu));
    }

    void DrawFinish(Listing_Standard listing)
    {
        Heading(listing, "PhobosFramework_SetupFinishTitle".Translate());
        List<(SetupSetting Setting, string Was, string Now)> changes = session.Changes();
        if (changes.Count == 0) listing.Label("PhobosFramework_SetupNoChanges".Translate());
        else
        {
            listing.Label("PhobosFramework_SetupChanged".Translate());
            foreach ((SetupSetting setting, string was, string now) in changes)
            {
                string line = "  - " + "PhobosFramework_SetupChange".Translate(setting.Label.Named("SETTING"), setting.OptionLabel(was).Named("WAS"),
                    setting.OptionLabel(now).Named("NOW"));
                if (setting.Restart) line += "  " + "PhobosFramework_SettingsNeedsRestart".Translate().Colorize(SettingsUi.WarningColor);
                listing.Label(line);
            }
            if (changes.Any(c => c.Setting.Restart))
            {
                listing.Gap(4f);
                GUI.color = SettingsUi.WarningColor;
                listing.Label("PhobosFramework_SetupRestartNote".Translate());
                GUI.color = Color.white;
            }
        }
        listing.Gap();
        Subtle(listing, "PhobosFramework_SetupFinishText".Translate());
    }

    void DrawFooter(Rect rect)
    {
        var skip = new Rect(rect.x, rect.y, FooterButtonWidth, rect.height);
        TooltipHandler.TipRegion(skip, "PhobosFramework_SetupSkipDesc".Translate());
        if (Widgets.ButtonText(skip, "PhobosFramework_SetupSkip".Translate()))
        {
            session.Skip();
            Close();
        }
        var next = new Rect(rect.xMax - FooterButtonWidth, rect.y, FooterButtonWidth, rect.height);
        if (step < LastStep)
        {
            if (Widgets.ButtonText(next, "PhobosFramework_SetupNext".Translate())) GoTo(step + 1);
        }
        else if (Widgets.ButtonText(next, "PhobosFramework_SetupFinish".Translate())) FinishAndClose();
        if (step > 0)
        {
            var back = new Rect(next.x - FooterButtonWidth - 10f, rect.y, FooterButtonWidth, rect.height);
            if (Widgets.ButtonText(back, "PhobosFramework_SetupBack".Translate())) GoTo(step - 1);
        }
        Text.Anchor = TextAnchor.MiddleCenter;
        GUI.color = ColoredText.SubtleGrayColor;
        Widgets.Label(new Rect(skip.xMax, rect.y, next.x - skip.xMax - FooterButtonWidth - 10f, rect.height),
            "PhobosFramework_SetupStepOf".Translate((step + 1).Named("STEP"), (LastStep + 1).Named("COUNT")));
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;
    }

    void FinishAndClose()
    {
        Close();
        session.Finish();
    }
}
