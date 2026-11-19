using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Phobos.Framework;

/// <summary>One page of a <see cref="SettingsBook"/>: a name in the book's page list and what it draws.</summary>
public sealed class SettingsPage
{
    /// <summary>Stable within a session: the book remembers the page last open by it. Never saved.</summary>
    public readonly string Id;
    /// <summary>Where the page sits in the list; pages of equal order keep the order they were added in.</summary>
    public readonly float Order;
    readonly Func<string> label;
    readonly Action<SettingsList> draw;
    readonly Func<bool>? visible;

    /// <param name="label">Translated when drawn, so a language change shows at once: pass <c>() =&gt; "Key".Translate()</c>.</param>
    /// <param name="visible">Null for always; else the page is listed only while it answers true (a tie-in's page while its mod is active).</param>
    public SettingsPage(string id, float order, Func<string> label, Action<SettingsList> draw, Func<bool>? visible = null)
    {
        Id = id;
        Order = order;
        this.label = label;
        this.draw = draw;
        this.visible = visible;
    }

    public string Label => label();
    public bool Shown => visible?.Invoke() ?? true;
    internal void Draw(SettingsList list) => draw(list);
}

/// <summary>
/// One entry in the game's mod settings list, laid out as the game's own Options window is (owner's choice, 20 Nov 2026,
/// docs/development/settings-pages.md): pages down the left, a search box above them that searches every page, a tick that shows only
/// what the player has changed (each changed setting marked with a dot), and a button that puts one page back to its defaults. Several
/// Mod classes may share one book (D'thorga Patches' tie-ins, each loaded only with its mod): the first to host it is the one the
/// settings list shows, and closing the window writes every member's file. A setting that needs a restart says so beside its label,
/// and closing the window after changing one says which.
/// </summary>
public sealed class SettingsBook
{
    const float PageListWidth = 200f;
    const float ColumnGap = 14f;
    const float PageRowHeight = 30f;
    const float PageRowSpacing = 2f;
    const float HeaderHeight = 34f;
    const float BottomBarHeight = 32f;
    const float ResetButtonWidth = 220f;
    /// <summary>Frames a reset keeps applying defaults, so a setting shown only under another (a sub-option) is reset too once its parent is.</summary>
    const int ResetFrames = 3;
    /// <summary>A frame gap this long between draws means the window was closed and opened again.</summary>
    const int ReopenFrameGap = 3;

    static readonly Dictionary<string, SettingsBook> Books = new();
    // Test row 267's seen-in-play note, "framework.settings-pages", is written with its key in full at each call (the evidence test reads
    // it there), one per case: search, changed only, reset, restart reminder.

    public readonly string Id;
    readonly Func<string> title;
    readonly List<SettingsPage> pages = new();
    readonly List<Mod> members = new();
    readonly List<Func<Mod?>> alsoWrites = new();
    readonly QuickSearchWidget search = new();
    readonly SettingsScroll content = new();
    readonly Dictionary<string, object> restartBefore = new();
    HashSet<string> pagesWithRows = new();
    bool changedOnly;
    string? selectedId;
    Vector2 pageListScroll;
    int lastDrawFrame = -100;
    int focusSearchOnFrame = -1;
    string? resetPageId;
    int resetFramesLeft;

    /// <summary>The Mod whose entry the settings list shows; the others return no category and stay out of it.</summary>
    public Mod? Host { get; private set; }

    SettingsBook(string id, Func<string> title)
    {
        Id = id;
        this.title = title;
    }

    /// <summary>The book of this ID, made on first use. Call from a Mod constructor.</summary>
    public static SettingsBook For(string id, Func<string> title)
    {
        if (!Books.TryGetValue(id, out SettingsBook? book)) Books[id] = book = new SettingsBook(id, title);
        return book;
    }

    public string Title => title();

    /// <summary>Joins a Mod to the book, as its host if it has none yet.</summary>
    public SettingsBook Join(Mod mod)
    {
        Host ??= mod;
        if (!members.Contains(mod)) members.Add(mod);
        return this;
    }

    /// <summary>Another Mod whose settings a page here changes (the vehicle pages show a Framework setting): written when this book is.
    /// Looked up when written, as Mods are made in no fixed order.</summary>
    public SettingsBook AlsoWrites(Func<Mod?> mod)
    {
        alsoWrites.Add(mod);
        return this;
    }

    public SettingsBook Add(SettingsPage page)
    {
        pages.RemoveAll(p => p.Id == page.Id);
        int at = pages.FindLastIndex(p => p.Order <= page.Order);
        pages.Insert(at + 1, page);
        return this;
    }

    /// <summary>Writes every member's settings file; when the settings window is closing, names any change that needs a restart.</summary>
    public void WriteAll()
    {
        foreach (Mod mod in members) Write(mod);
        foreach (Func<Mod?> other in alsoWrites)
        {
            if (other() is { } mod && !members.Contains(mod)) Write(mod);
        }
        if (restartBefore.Count > 0 && Find.WindowStack != null && Find.WindowStack.IsOpen<Dialog_ModSettings>()) AskToRestart();
    }

    static void Write(Mod mod)
    {
        if (mod is SettingsBookMod own) own.WriteOwn();
        else mod.WriteSettings();
    }

    void AskToRestart()
    {
        string names = string.Join("\n", restartBefore.Keys.Select(label => "  - " + label));
        restartBefore.Clear();
        Seen.Note("framework.settings-pages", what: Id, variant: "restart reminder");
        if (Current.ProgramState == ProgramState.Entry)
        {
            Find.WindowStack.Add(new Dialog_MessageBox("PhobosFramework_SettingsRestartAtMenu".Translate(names.Named("SETTINGS")),
                "PhobosFramework_SettingsRestartNow".Translate(), GenCommandLine.Restart, "PhobosFramework_SettingsRestartLater".Translate()));
        }
        else
        {
            Find.WindowStack.Add(new Dialog_MessageBox("PhobosFramework_SettingsRestartInGame".Translate(names.Named("SETTINGS"))));
        }
    }

    /// <summary>A setting that needs a restart changed: remembered with its first value, forgotten if put back to it.</summary>
    internal void NoteRestartChange(string label, object before, object now)
    {
        if (!restartBefore.TryGetValue(label, out object? first)) restartBefore[label] = before;
        else if (Equals(first, now)) restartBefore.Remove(label);
    }

    public void Draw(Rect inRect)
    {
        int frame = Time.frameCount;
        if (frame - lastDrawFrame > ReopenFrameGap) Opened(frame);
        lastDrawFrame = frame;
        if (frame == focusSearchOnFrame) search.Focus();

        List<SettingsPage> shown = pages.Where(p => p.Shown).ToList();
        if (shown.Count == 0) return;
        SettingsPage page = shown.FirstOrDefault(p => p.Id == selectedId) ?? shown[0];
        selectedId = page.Id;
        bool results = search.filter.Active || changedOnly;

        var left = new Rect(inRect.x, inRect.y, PageListWidth, inRect.height);
        var right = new Rect(left.xMax + ColumnGap, inRect.y, inRect.width - PageListWidth - ColumnGap, inRect.height);
        DrawPageList(left, shown, page, results);
        if (results) DrawResults(right, shown);
        else DrawPage(right, page);
    }

    void Opened(int frame)
    {
        search.Reset();
        changedOnly = false;
        restartBefore.Clear();
        content.ResetPosition();
        // Typing goes straight into the search box (owner, keyboard over mouse); the box exists only once drawn, so a frame later.
        focusSearchOnFrame = frame + 1;
    }

    void DrawPageList(Rect rect, List<SettingsPage> shown, SettingsPage selected, bool results)
    {
        float y = rect.y;
        search.OnGUI(new Rect(rect.x, y, rect.width, QuickSearchWidget.WidgetHeight), content.ResetPosition);
        y += QuickSearchWidget.WidgetHeight + 6f;
        var tick = new Rect(rect.x, y, rect.width, Text.LineHeight);
        bool before = changedOnly;
        Widgets.CheckboxLabeled(tick, "PhobosFramework_SettingsChangedOnly".Translate(), ref changedOnly);
        TooltipHandler.TipRegion(tick, "PhobosFramework_SettingsChangedOnlyDesc".Translate());
        if (before != changedOnly) content.ResetPosition();
        y += Text.LineHeight + 8f;

        var outRect = new Rect(rect.x, y, rect.width, rect.yMax - y);
        float height = shown.Count * (PageRowHeight + PageRowSpacing);
        bool scrolls = height > outRect.height;
        var view = new Rect(0f, 0f, outRect.width - (scrolls ? 16f : 0f), height);
        Widgets.BeginScrollView(outRect, ref pageListScroll, view);
        for (int i = 0; i < shown.Count; i++)
        {
            SettingsPage page = shown[i];
            var row = new Rect(0f, i * (PageRowHeight + PageRowSpacing), view.width, PageRowHeight);
            Widgets.DrawOptionBackground(row, !results && page == selected);
            if (results && !pagesWithRows.Contains(page.Id)) GUI.color = Color.gray;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(row.x + 8f, row.y, row.width - 12f, row.height), page.Label.Truncate(row.width - 12f));
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            if (Widgets.ButtonInvisible(row))
            {
                selectedId = page.Id;
                search.Reset();
                changedOnly = false;
                content.ResetPosition();
                SoundDefOf.Click.PlayOneShotOnCamera();
            }
        }
        Widgets.EndScrollView();
    }

    void DrawPage(Rect rect, SettingsPage page)
    {
        Header(rect, page.Label);
        var bottom = new Rect(rect.x, rect.yMax - BottomBarHeight, rect.width, BottomBarHeight);
        var body = new Rect(rect.x, rect.y + HeaderHeight, rect.width, bottom.y - rect.y - HeaderHeight - 6f);
        bool resetting = resetPageId == page.Id && resetFramesLeft > 0;
        var list = new SettingsList(this, content.Begin(body), null, false, resetting, results: false);
        page.Draw(list);
        content.End(list.Listing);
        if (resetting && list.ResetApplied) Seen.Note("framework.settings-pages", what: Id, variant: "reset");
        if (resetting && --resetFramesLeft <= 0) resetPageId = null;

        // The bottom bar: how many settings here differ from their defaults, the restart reminder, and the reset button.
        var note = new Rect(bottom.x, bottom.y, bottom.width - ResetButtonWidth - 10f, bottom.height);
        Text.Font = GameFont.Tiny;
        Text.Anchor = TextAnchor.MiddleLeft;
        if (restartBefore.Count > 0)
        {
            GUI.color = SettingsUi.WarningColor;
            Widgets.Label(note, "PhobosFramework_SettingsRestartPending".Translate(restartBefore.Count.Named("COUNT")));
        }
        else
        {
            GUI.color = ColoredText.SubtleGrayColor;
            Widgets.Label(note, list.ChangedRows == 0 ? "PhobosFramework_SettingsPageAtDefaults".Translate()
                : "PhobosFramework_SettingsPageChanged".Translate(list.ChangedRows.Named("COUNT")));
        }
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Small;
        var button = new Rect(bottom.xMax - ResetButtonWidth, bottom.y + 1f, ResetButtonWidth, bottom.height - 2f);
        if (list.ChangedRows == 0) TooltipHandler.TipRegion(button, "PhobosFramework_SettingsPageAtDefaults".Translate());
        if (Widgets.ButtonText(button, "PhobosFramework_SettingsResetPage".Translate(), active: list.ChangedRows > 0) && list.ChangedRows > 0)
        {
            string pageId = page.Id;
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("PhobosFramework_SettingsResetConfirm".Translate(page.Label.Named("PAGE")), () =>
            {
                resetPageId = pageId;
                resetFramesLeft = ResetFrames;
            }, destructive: true));
        }
    }

    void DrawResults(Rect rect, List<SettingsPage> shown)
    {
        Header(rect, search.filter.Active ? "PhobosFramework_SettingsResultsFor".Translate(search.filter.Text.Named("TEXT")).ToString()
            : "PhobosFramework_SettingsResultsChanged".Translate().ToString());
        var body = new Rect(rect.x, rect.y + HeaderHeight, rect.width, rect.height - HeaderHeight);
        var list = new SettingsList(this, content.Begin(body), search.filter.Active ? search.filter : null, changedOnly, false, results: true);
        var found = new HashSet<string>();
        foreach (SettingsPage page in shown)
        {
            list.BeginPage(page.Label);
            page.Draw(list);
            if (list.PageHadRows) found.Add(page.Id);
        }
        if (list.Rows == 0)
        {
            GUI.color = ColoredText.SubtleGrayColor;
            list.Listing.Label(changedOnly && !search.filter.Active ? "PhobosFramework_SettingsNoneChanged".Translate() : "PhobosFramework_SettingsNoMatch".Translate());
            GUI.color = Color.white;
        }
        search.noResultsMatched = list.Rows == 0;
        if (list.Rows > 0) Seen.Note("framework.settings-pages", what: Id, variant: search.filter.Active ? "search" : "changed only");
        content.End(list.Listing);
        pagesWithRows = found;
    }

    static void Header(Rect rect, string label)
    {
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(rect.x, rect.y, rect.width, HeaderHeight), label.Truncate(rect.width));
        Text.Font = GameFont.Small;
    }
}

/// <summary>
/// What a settings page draws into. Every row takes the setting's default, so the page can mark it when changed, list it under
/// "changed only" and put it back on a reset; and its label and tooltip, so search can find it. A row whose default is not given is
/// never marked, listed as changed or reset (a colony's own choice, shown here for convenience). Rows return true when their value
/// changed this frame, by the player or by a reset, so a caller can act on it (apply a volume, re-price waiting work).
/// </summary>
public sealed class SettingsList
{
    const float MarkerWidth = 16f;
    const float IndentStep = 24f;
    const float SliderHeight = 22f;
    static readonly Color MarkerColor = SettingsUi.WarningColor;

    readonly SettingsBook book;
    readonly QuickSearchFilter? filter;
    readonly bool changedOnly;
    readonly bool resetting;
    readonly bool results;
    string? pendingPage;
    string? pendingSection;
    bool pageMatched;
    bool sectionMatched;
    bool lastShown = true;
    float indent;

    /// <summary>The game's own listing, for a page that needs a widget this class lacks. Rows drawn on it are not searched or marked.</summary>
    public readonly Listing_Standard Listing;
    /// <summary>Rows drawn.</summary>
    public int Rows { get; private set; }
    /// <summary>Rows at a value other than their default, drawn or not.</summary>
    public int ChangedRows { get; private set; }
    internal bool PageHadRows { get; private set; }
    /// <summary>A reset put at least one row back to its default this frame.</summary>
    internal bool ResetApplied { get; private set; }

    /// <summary>True while only some rows are drawn (a search, or "changed only"): a page may skip work no row needs.</summary>
    public bool Filtering => filter != null || changedOnly;

    internal SettingsList(SettingsBook book, Listing_Standard listing, QuickSearchFilter? filter, bool changedOnly, bool resetting, bool results)
    {
        this.book = book;
        Listing = listing;
        this.filter = filter;
        this.changedOnly = changedOnly;
        this.resetting = resetting;
        this.results = results;
    }

    internal void BeginPage(string label)
    {
        pendingPage = label;
        pendingSection = null;
        pageMatched = filter != null && filter.Matches(label);
        sectionMatched = false;
        PageHadRows = false;
        lastShown = false;
        indent = 0f;
    }

    /// <summary>A heading over the rows after it, as the game heads its own lists. A search that matches it shows the whole section.</summary>
    public void Section(string label)
    {
        indent = 0f;
        sectionMatched = filter != null && filter.Matches(label);
        if (results) pendingSection = label;
        else DrawSection(label);
    }

    public void Indent() => indent += IndentStep;
    public void Outdent() => indent = Mathf.Max(0f, indent - IndentStep);

    public bool Check(string label, ref bool value, bool? byDefault, string? tip = null, bool restart = false, bool enabled = true)
    {
        bool changed = byDefault.HasValue && value != byDefault.Value;
        if (changed) ChangedRows++;
        if (resetting && changed && enabled)
        {
            if (restart) book.NoteRestartChange(label, value, byDefault!.Value);
            value = byDefault!.Value;
            ResetApplied = true;
            return true;
        }
        if (!Show(label, tip, changed)) return false;
        string shown = restart ? label + RestartTag() : label;
        Rect row = Row(Text.CalcHeight(shown, RowWidth() - 24f), changed, tip);
        bool before = value;
        if (!enabled) GUI.color = Color.gray;
        Widgets.CheckboxLabeled(row, shown, ref value, disabled: !enabled);
        GUI.color = Color.white;
        if (value == before) return false;
        if (restart) book.NoteRestartChange(label, before, value);
        return true;
    }

    /// <summary>A choice shown as radio buttons under a label: for two to four options worth reading side by side.</summary>
    public bool Radio<T>(string label, ref T value, T byDefault, IEnumerable<T> options, Func<T, string> optionLabel, Func<T, string?>? optionTip = null,
        string? tip = null, bool restart = false)
    {
        List<T> all = options.ToList();
        bool changed = !EqualityComparer<T>.Default.Equals(value, byDefault);
        if (changed) ChangedRows++;
        if (resetting && changed)
        {
            if (restart) book.NoteRestartChange(label, value!, byDefault!);
            value = byDefault;
            ResetApplied = true;
            return true;
        }
        string? searchable = filter == null ? tip : tip + " " + string.Join(" ", all.Select(optionLabel));
        if (!Show(label, searchable, changed)) return false;
        string shown = restart ? label + RestartTag() : label;
        Rect head = Row(Text.CalcHeight(shown, RowWidth()), changed, tip);
        Widgets.Label(head, shown);
        T before = value;
        foreach (T option in all)
        {
            string text = optionLabel(option);
            Rect row = Listing.GetRect(Text.CalcHeight(text, RowWidth() - IndentStep - 24f));
            row.xMin += indent + MarkerWidth + IndentStep;
            Widgets.DrawHighlightIfMouseover(row);
            if (optionTip?.Invoke(option) is { } optionText) TooltipHandler.TipRegion(row, optionText);
            if (Widgets.RadioButtonLabeled(row, text, EqualityComparer<T>.Default.Equals(value, option))) value = option;
        }
        if (EqualityComparer<T>.Default.Equals(value, before)) return false;
        if (restart) book.NoteRestartChange(label, before!, value!);
        return true;
    }

    /// <summary>A choice behind a button that opens a menu: for a long list, or where a row of radio buttons would crowd the page. The
    /// menu outlives the frame, so the choice lands through <paramref name="choose"/>, which writes the settings object.</summary>
    public void Choice<T>(string label, T current, T byDefault, IEnumerable<T> options, Func<T, string> optionLabel, Action<T> choose,
        string? tip = null, bool restart = false)
    {
        List<T> all = options.ToList();
        bool changed = !EqualityComparer<T>.Default.Equals(current, byDefault);
        if (changed) ChangedRows++;
        if (resetting && changed)
        {
            if (restart) book.NoteRestartChange(label, current!, byDefault!);
            choose(byDefault);
            ResetApplied = true;
            return;
        }
        string? searchable = filter == null ? tip : tip + " " + string.Join(" ", all.Select(optionLabel));
        if (!Show(label, searchable, changed)) return;
        string shown = restart ? label + RestartTag() : label;
        Rect row = Row(Mathf.Max(30f, Text.CalcHeight(shown, RowWidth() / 2f)), changed, tip);
        Rect labelRect = row.LeftHalf();
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(labelRect, shown);
        Text.Anchor = TextAnchor.UpperLeft;
        if (!Widgets.ButtonText(row.RightHalf().ContractedBy(0f, 2f), optionLabel(current))) return;
        var menu = new List<FloatMenuOption>();
        foreach (T option in all)
        {
            T chosen = option;
            menu.Add(new FloatMenuOption(optionLabel(chosen), () =>
            {
                if (restart) book.NoteRestartChange(label, current!, chosen!);
                choose(chosen);
            }));
        }
        Find.WindowStack.Add(new FloatMenu(menu));
    }

    /// <summary>A slider under its label. The label carries the value as the caller formats it, so search matches the setting's name in
    /// <paramref name="label"/> as shown; <paramref name="round"/> snaps the value (the game's own sliders do not round, so pass none
    /// unless the setting must).</summary>
    public bool Slider(string label, ref float value, float byDefault, float min, float max, string? tip = null, Func<float, float>? round = null)
    {
        bool changed = Mathf.Abs(value - byDefault) > 0.0001f;
        if (changed) ChangedRows++;
        if (resetting && changed)
        {
            value = byDefault;
            ResetApplied = true;
            return true;
        }
        if (!Show(label, tip, changed)) return false;
        Rect head = Row(Text.CalcHeight(label, RowWidth()), changed, tip);
        Widgets.Label(head, label);
        Rect bar = Listing.GetRect(SliderHeight);
        bar.xMin += indent + MarkerWidth;
        float next = Widgets.HorizontalSlider(bar, value, min, max, middleAlignment: true);
        if (round != null) next = round(next);
        Listing.Gap(Listing.verticalSpacing);
        if (Mathf.Approximately(next, value)) return false;
        value = next;
        return true;
    }

    public bool SliderInt(string label, ref int value, int byDefault, int min, int max, string? tip = null)
    {
        float f = value;
        if (!Slider(label, ref f, byDefault, min, max, tip, Mathf.Round)) return false;
        value = Mathf.RoundToInt(f);
        return true;
    }

    /// <summary>A plain line of text: what a part does, or why a setting is missing. In a search it shows only under a row it belongs to.</summary>
    public void Note(string text)
    {
        // In a search, a note shows under the row before it, or with its whole section or page when the search matched that.
        bool wholeMatched = filter != null && !changedOnly && (pageMatched || sectionMatched);
        if (Filtering && !lastShown && !wholeMatched) return;
        Flush();
        Rect row = Listing.GetRect(Text.CalcHeight(text, RowWidth()));
        row.xMin += indent + MarkerWidth;
        Widgets.Label(row, text);
    }

    /// <summary>A setting's cost, in amber small print under the row before it. Null or empty draws nothing, so callers can pass a
    /// warning only for the choices that cost.</summary>
    public void Warning(string? text)
    {
        if (text.NullOrEmpty() || (Filtering && !lastShown)) return;
        Color colour = GUI.color;
        GameFont font = Text.Font;
        Text.Font = GameFont.Tiny;
        GUI.color = SettingsUi.WarningColor;
        Rect row = Listing.GetRect(Text.CalcHeight(text, RowWidth()));
        row.xMin += indent + MarkerWidth;
        Widgets.Label(row, text);
        Text.Font = font;
        GUI.color = colour;
    }

    /// <summary>A button that acts at once (a removal sweep). Shown in a search that matches it, never under "changed only".</summary>
    public bool Button(string label, string? tip = null, bool active = true)
    {
        if (changedOnly) return false;
        if (!Show(label, tip, false)) return false;
        Rect row = Listing.GetRect(30f);
        row.xMin += indent + MarkerWidth;
        row.width = Mathf.Min(row.width, Mathf.Max(220f, Text.CalcSize(label).x + 40f));
        if (tip != null) TooltipHandler.TipRegion(row, tip);
        return Widgets.ButtonText(row, label, active: active) && active;
    }

    /// <summary>A row the page draws itself (a widget this class lacks): the rect to draw in, or null when the row is filtered out.
    /// Pass whether it differs from its default; a custom row cannot be reset, so the page resets it itself when <see cref="Resetting"/>.</summary>
    public Rect? CustomRow(string label, string? tip, bool changed, float height)
    {
        if (changed) ChangedRows++;
        if (!Show(label, tip, changed)) return null;
        return Row(height, changed, tip);
    }

    /// <summary>True on the frames a reset of this page is putting its rows back to their defaults.</summary>
    public bool Resetting => resetting;

    float RowWidth() => Listing.ColumnWidth - indent - MarkerWidth;

    bool Show(string label, string? tip, bool changed)
    {
        bool show = !changedOnly || changed;
        if (show && filter != null && !pageMatched && !sectionMatched)
            show = filter.Matches(label) || (tip != null && filter.Matches(tip));
        lastShown = show;
        if (!show) return false;
        Flush();
        Rows++;
        PageHadRows = true;
        return true;
    }

    void Flush()
    {
        if (pendingPage != null)
        {
            if (Rows > 0) Listing.Gap(10f);
            Text.Font = GameFont.Medium;
            Listing.Label(pendingPage);
            Text.Font = GameFont.Small;
            pendingPage = null;
        }
        if (pendingSection != null)
        {
            DrawSection(pendingSection);
            pendingSection = null;
        }
    }

    void DrawSection(string label)
    {
        Listing.Gap(6f);
        Rect row = Listing.GetRect(24f);
        Color colour = GUI.color;
        GUI.color = Widgets.SeparatorLabelColor;
        Widgets.Label(row, label);
        GUI.color = Widgets.SeparatorLineColor;
        Widgets.DrawLineHorizontal(row.x, row.yMax - 2f, row.width);
        GUI.color = colour;
        Listing.Gap(4f);
    }

    Rect Row(float height, bool changed, string? tip)
    {
        Rect row = Listing.GetRect(height);
        row.xMin += indent;
        if (changed)
        {
            var dot = new Rect(row.x + 3f, row.y + (Mathf.Min(row.height, 24f) - 8f) / 2f, 8f, 8f);
            Widgets.DrawBoxSolid(dot, MarkerColor);
            TooltipHandler.TipRegion(new Rect(row.x, row.y, MarkerWidth, row.height), "PhobosFramework_SettingsChangedMarker".Translate());
        }
        row.xMin += MarkerWidth;
        Widgets.DrawHighlightIfMouseover(row);
        if (tip != null) TooltipHandler.TipRegion(row, tip);
        Listing.Gap(Listing.verticalSpacing);
        return row;
    }

    static string RestartTag() => "  " + "PhobosFramework_SettingsNeedsRestart".Translate().Colorize(ColoredText.SubtleGrayColor);
}

/// <summary>
/// A Mod whose settings window is a <see cref="SettingsBook"/>. Only the book's host appears in the game's mod settings list; it
/// draws the whole book and, when the window closes, writes every member's file.
/// </summary>
public abstract class SettingsBookMod : Mod
{
    protected SettingsBookMod(ModContentPack content) : base(content)
    {
    }

    /// <summary>The book this Mod's pages are in.</summary>
    protected abstract SettingsBook Book { get; }

    public sealed override string SettingsCategory() => Book.Host == this ? Book.Title : "";

    public sealed override void DoSettingsWindowContents(Rect inRect) => Book.Draw(inRect);

    public override void WriteSettings()
    {
        if (Book.Host == this) Book.WriteAll();
        else base.WriteSettings();
    }

    internal void WriteOwn() => base.WriteSettings();
}
