using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using RimWorld;
using Verse;

namespace Phobos.Framework.Setup;

/// <summary>
/// The setup wizard's service (Framework 0.362.0; docs/development/first-load-setup-wizard.md, the owner's cards of 28 Nov 2026): on the
/// main menu, once per install, and again only for pages that are new or changed; reopened from D'thorga Framework's settings. It only
/// drives settings that already exist, through <see cref="SetupSettings"/>. The window presents; everything it changes goes through here.
/// </summary>
public static class SetupWizard
{
    /// <summary>Every page, in order, with a setting list trimmed to what is registered (a tie-in's settings only with its mod).</summary>
    public static List<PhobosSetupPageDef> Pages() =>
        DefDatabase<PhobosSetupPageDef>.AllDefsListForReading
            .OrderBy(p => p.order)
            .ThenBy(p => p.modContentPack?.loadOrder ?? int.MaxValue)
            .ToList();

    /// <summary>The settings a page shows now: those registered.</summary>
    public static List<SetupSetting> SettingsOf(PhobosSetupPageDef page) =>
        page.settings.Select(SetupSettings.Get).Where(s => s != null).Select(s => s!).ToList();

    /// <summary>Its settings no preset sets: shown at all times.</summary>
    public static HashSet<string> OutsidePresets(PhobosSetupPageDef page) =>
        new(SetupRule.OutsidePresets(page.settings, page.PresetValues()));

    /// <summary>The preset the settings match now, or -1.</summary>
    public static int MatchingPreset(PhobosSetupPageDef page) =>
        SetupRule.MatchingPreset(page.PresetValues(), id => SetupSettings.Get(id)?.Current);

    static Dictionary<string, int> SeenPages => FrameworkMod.Settings.setupSeen;

    /// <summary>The pages the player has not seen at their present version.</summary>
    public static List<PhobosSetupPageDef> DuePages()
    {
        List<PhobosSetupPageDef> pages = Pages();
        var due = new HashSet<string>(SetupRule.DuePages(SeenPages, pages.Select(p => (p.defName, p.version))));
        return pages.Where(p => due.Contains(p.defName)).ToList();
    }

    /// <summary>At start-up, on the main menu: opens the wizard with the pages due, if any. A game started straight into a save waits.</summary>
    public static void OpenIfDue()
    {
        if (Current.ProgramState != ProgramState.Entry || Find.WindowStack == null) return;
        List<PhobosSetupPageDef> due = DuePages();
        if (due.Count == 0) return;
        Find.WindowStack.Add(new Dialog_PhobosSetup(new SetupSession(due, firstTime: SeenPages.Count == 0)));
        Note("opened", () => due.Count == Pages().Count ? "every page" : "new pages");
    }

    /// <summary>From D'thorga Framework's settings: every page, at any time.</summary>
    public static void OpenAll()
    {
        Find.WindowStack.Add(new Dialog_PhobosSetup(new SetupSession(Pages(), firstTime: false)));
        Note("opened", () => "from the settings");
    }

    internal static void MarkSeen(IEnumerable<PhobosSetupPageDef> pages)
    {
        foreach (PhobosSetupPageDef page in pages) SeenPages[page.defName] = page.version;
        LoadedModManager.GetMod<FrameworkMod>()?.WriteSettings();
    }

    /// <summary>Test rows 732 to 734's seen-in-play note, one per case: opened, preset, finished, skipped. Its text is built only when wanted.</summary>
    internal static void Note(string variant, System.Func<string> what)
    {
        if (Phobos.Framework.Seen.Wants("framework.setup-wizard", variant: variant)) Phobos.Framework.Seen.Note("framework.setup-wizard", what: what(), variant: variant);
    }
}

/// <summary>
/// One showing of the wizard: the pages shown, each setting's value when it opened, and what finishing or skipping does with them.
/// </summary>
public sealed class SetupSession
{
    public readonly List<PhobosSetupPageDef> Pages;
    /// <summary>The first showing on this install: the welcome says so.</summary>
    public readonly bool FirstTime;
    readonly Dictionary<string, string> before = new();

    public SetupSession(List<PhobosSetupPageDef> pages, bool firstTime)
    {
        Pages = pages;
        FirstTime = firstTime;
        foreach (SetupSetting setting in pages.SelectMany(SetupWizard.SettingsOf)) before[setting.Id] = setting.Current;
    }

    /// <summary>Sets every value a preset names that is registered.</summary>
    public void Apply(PhobosSetupPageDef page, SetupPreset preset)
    {
        foreach (SetupValue value in preset.values) SetupSettings.Get(value.setting)?.Set(value.value);
        SetupWizard.Note("preset", () => page.defName + ": " + preset.label);
    }

    public void Set(SetupSetting setting, string key) => setting.Set(key);

    /// <summary>What changed since the wizard opened: the setting, its value then and now.</summary>
    public List<(SetupSetting Setting, string Was, string Now)> Changes() =>
        before.Select(b => (Setting: SetupSettings.Get(b.Key), Was: b.Value))
            .Where(c => c.Setting != null && c.Setting.Current != c.Was)
            .Select(c => (c.Setting!, c.Was, c.Setting!.Current))
            .ToList();

    /// <summary>Keeps the choices: writes each mod's settings file, marks the pages seen, and asks for a restart when a choice needs one.</summary>
    public void Finish()
    {
        List<(SetupSetting Setting, string Was, string Now)> changes = Changes();
        foreach (Mod owner in changes.Select(c => c.Setting.Owner).Distinct()) owner.WriteSettings();
        SetupWizard.MarkSeen(Pages);
        SetupWizard.Note("finished", () => changes.Count + " change(s)");
        List<string> restart = changes.Where(c => c.Setting.Restart).Select(c => "  - " + c.Setting.Label).ToList();
        if (restart.Count == 0) return;
        NamedArgument names = string.Join("\n", restart).Named("SETTINGS");
        // As the settings window asks (SettingsBook): restart now from the main menu; in a game, a reminder only.
        if (Current.ProgramState == ProgramState.Entry)
            Find.WindowStack.Add(new Dialog_MessageBox("PhobosFramework_SettingsRestartAtMenu".Translate(names),
                "PhobosFramework_SettingsRestartNow".Translate(), GenCommandLine.Restart, "PhobosFramework_SettingsRestartLater".Translate()));
        else Find.WindowStack.Add(new Dialog_MessageBox("PhobosFramework_SettingsRestartInGame".Translate(names)));
    }

    /// <summary>Skips: every setting goes back to what it was when the wizard opened, and the pages count as seen.</summary>
    public void Skip()
    {
        foreach (KeyValuePair<string, string> was in before) SetupSettings.Get(was.Key)?.Set(was.Value);
        SetupWizard.MarkSeen(Pages);
        SetupWizard.Note("skipped", () => Pages.Count + " page(s)");
    }
}
