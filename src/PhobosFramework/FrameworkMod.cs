using System.Collections.Generic;
using Phobos.Framework.Gases;
using Phobos.Framework.Health;
using Phobos.Framework.Rules;
using UnityEngine;
using Verse;

namespace Phobos.Framework;

/// <summary>Player preferences for the framework itself. Physics and balance live in the rules Defs, not here.</summary>
public class FrameworkSettings : ModSettings
{
    /// <summary>The owner's gas rule and its manual override: see <see cref="SourceRule"/>.</summary>
    public GasSourceMode gasSourceMode = GasSourceMode.Automatic;
    /// <summary>Dev mode only: a tooltip at the mouse cell naming every gas the framework can see there.</summary>
    public bool showGasOverlay;
    /// <summary>Dev mode only, one-shot: write every Def with all patches applied at the next start (Debug/PatchedDefsDump).</summary>
    public bool writePatchedDefsOnce;
    /// <summary>Turn the game's log back on when it stops itself at 10,000 messages (Logging/GameLogKeeper; owner, 8 Nov 2026: on by default).</summary>
    public bool keepGameLogAlive = true;
    /// <summary>After an error whose trace shows D'thorga code, say so in the log (Logging/ErrorNotes; owner, 19 Nov 2026; on by default).</summary>
    public bool noteOurPartInErrors = true;
    /// <summary>Write every colonist's thoughts beside each save, for the mood report (Mood/ColonyMoodFile; owner, 10 Nov 2026; on by default, agent default).</summary>
    public bool writeColonyMoodFile = true;
    /// <summary>Every D'thorga sound's volume, from none to twice as loud (Sounds/PhobosSoundVolume; AGENTS.md's volume setting; agent default 1).</summary>
    public float soundVolume = 1f;
    /// <summary>Whether the vehicle systems' sounds (a tyre bursting, a knock, the warning chime) also play on other mods' vehicles running them
    /// (owner, 10 Nov 2026: yes, with a setting to turn it off). Their engines and doors are always their authors' own.</summary>
    public bool systemSoundsOnOtherVehicles = true;
    /// <summary>Whose parts a build of ours asks for (Bills/Bills; owner, 11 Nov 2026: other mods' where installed, with a setting; applied at the next start).</summary>
    public Bills.BillPartsSource billPartsSource = Bills.BillPartsSource.OthersWhereInstalled;
    /// <summary>Haulers keep machines that hold a material topped up by themselves, and mechanitors the mechanoids that carry one (TopUp/TopUps;
    /// owner, 18 Nov 2026; on by default, agent default).</summary>
    public bool topUpMachines = true;
    /// <summary>How full topping up fills a store (Framework 0.136.0; owner, 18 Nov 2026: a setting; agent default: the machine's own level).</summary>
    public TopUpLevel topUpLevel = TopUpLevel.OwnLevel;
    /// <summary>"Seen in play" notes for testing (Framework 0.118.0; owner, 17 Nov 2026: off by default): Debug/SeenInPlay.cs.</summary>
    public bool noteSeenInPlay;
    /// <summary>With dev mode on, a message when a note stands for a test row still waiting (Framework 0.138.0; owner, 18 Nov 2026; off by
    /// default, agent default): Debug/SeenInPlay.cs.</summary>
    public bool sayWhenTestHappens;
    /// <summary>Verbose logging (owner, 16 Nov 2026: a master switch and a tick per part; both off by default): every log of ours writes
    /// its detail lines.</summary>
    public bool verboseAll;
    /// <summary>The logs whose detail is asked for one by one, by their keys (Logging/LogTopic.Key).</summary>
    public List<string> verboseTopics = new();
    /// <summary>The same keys for a quick look-up; replaced whole when a tick changes, so a log read on another thread never sees it half made.</summary>
    HashSet<string> verboseSet = new();

    /// <summary>Whether the log of this key writes its detail lines now.</summary>
    public bool Verbose(string key) => verboseAll || verboseSet.Contains(key);

    /// <summary>Ticks or unticks one log's detail.</summary>
    public void SetVerbose(string key, bool on)
    {
        var next = new HashSet<string>(verboseSet);
        if (on) next.Add(key);
        else next.Remove(key);
        verboseSet = next;
        verboseTopics = new List<string>(next);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref verboseAll, "PhobosFramework_VerboseAll", false);
        Scribe_Collections.Look(ref verboseTopics, "PhobosFramework_VerboseTopics", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit || Scribe.mode == LoadSaveMode.LoadingVars)
        {
            verboseTopics ??= new List<string>();
            verboseSet = new HashSet<string>(verboseTopics);
        }
        Scribe_Values.Look(ref soundVolume, "PhobosFramework_SoundVolume", 1f);
        Scribe_Values.Look(ref systemSoundsOnOtherVehicles, "PhobosFramework_SystemSoundsOnOtherVehicles", true);
        Scribe_Values.Look(ref billPartsSource, "PhobosFramework_BillPartsSource", Bills.BillPartsSource.OthersWhereInstalled);
        Scribe_Values.Look(ref writeColonyMoodFile, "PhobosFramework_WriteColonyMoodFile", true);
        Scribe_Values.Look(ref gasSourceMode, "PhobosFramework_GasSourceMode", GasSourceMode.Automatic);
        Scribe_Values.Look(ref showGasOverlay, "PhobosFramework_ShowGasOverlay", false);
        Scribe_Values.Look(ref writePatchedDefsOnce, "PhobosFramework_WritePatchedDefsOnce", false);
        Scribe_Values.Look(ref keepGameLogAlive, "PhobosFramework_KeepGameLogAlive", true);
        Scribe_Values.Look(ref noteOurPartInErrors, "PhobosFramework_NoteOurPartInErrors", true);
        Scribe_Values.Look(ref noteSeenInPlay, "PhobosFramework_NoteSeenInPlay", false);
        Scribe_Values.Look(ref sayWhenTestHappens, "PhobosFramework_SayWhenTestHappens", false);
        Scribe_Values.Look(ref topUpMachines, "PhobosFramework_TopUpMachines", true);
        Scribe_Values.Look(ref topUpLevel, "PhobosFramework_TopUpLevel", TopUpLevel.OwnLevel);
    }
}

/// <summary>
/// D'thorga Framework: the shared library every Phobos RimWorld mod with C# depends on. It holds the gas layer (what a pawn
/// breathes, the game's and other mods' gases reused first, and the framework's own additive room gases), the health layer the
/// medical mods share (who the doctors look after, how dangerous a condition is, per-pawn care choices) and the small helpers the
/// mods used to repeat, and keeps the game's log going past its message limit. It references no other mod's assembly and needs no
/// Harmony.
/// </summary>
public class FrameworkMod : SettingsBookMod
{
    public const string PackageId = "PhobosDthorga.Framework";
    public const string ModVersion = "0.190.0";
    public const string LogPrefix = "[D'thorga Framework]";
    public static readonly Phobos.Logging.PhobosLog Logger = Logging.Logs.For("D'thorga Framework");

    public static FrameworkSettings Settings { get; private set; } = new();

    /// <summary>The mod itself, for its folder (Debug/SeenInPlay reads the installed TestRows.tsv there).</summary>
    public static FrameworkMod? Instance { get; private set; }

    public FrameworkMod(ModContentPack content) : base(content)
    {
        Instance = this;
        AddSettingsPages();
        Settings = GetSettings<FrameworkSettings>();
        Logging.GameLogKeeper.Listen();
        LongEventHandler.ExecuteWhenFinished(LogLoaded);
    }

    static void LogLoaded()
    {
        var report = new LoadReport(LogPrefix, ModVersion);
        report.On("gas layer: " + GasCarriers.Describe());
        report.On(CombatExtendedSmoke.InUse
            ? "Combat Extended smoke inhalation: in use as the smoke signal"
            : CombatExtendedSmoke.Active
                ? "Combat Extended smoke inhalation: not in use (its smoke effects setting is off), the game's smoke carries fire smoke"
                : "Combat Extended: not active, the game's smoke carries fire smoke");
        int ownGases = DefDatabase<PhobosGasDef>.AllDefsListForReading.Count;
        report.On("own room gases: " + ownGases + " Def" + (ownGases == 1 ? "" : "s") + ", source mode " + Settings.gasSourceMode);
        // Mods register their care choices after this line is written, so it names only the comp; each mod's own line names its choice.
        report.On("patient comp on " + PatientCompInjector.Added + " humanlike race" + (PatientCompInjector.Added == 1 ? "" : "s"));
        string keeper = "the game's log turned back on after its 10,000-message limit (at most " + Rules.GameLogRule.MaxRevivals + " times a session)";
        if (!Settings.keepGameLogAlive) report.Off(keeper, "setting");
        else if (Logging.GameLogKeeper.NotListening != null) report.On(keeper + ", without naming the loudest message: " + Logging.GameLogKeeper.NotListening);
        else report.On(keeper);
        const string notes = "a note after an error whose trace shows D'thorga code";
        if (!Settings.noteOurPartInErrors) report.Off(notes, "setting");
        else if (Logging.GameLogKeeper.NotListening != null) report.Off(notes, Logging.GameLogKeeper.NotListening);
        else report.On(notes);
        const string moodFile = "colony mood file at each save, for the mood report";
        if (Settings.writeColonyMoodFile) report.On(moodFile);
        else report.Off(moodFile, "setting");
        Sounds.PhobosSoundVolume.Apply(Settings.soundVolume);
        report.On("sound volume " + SoundVolumeRule.Percent(Settings.soundVolume) + "% on " + Sounds.PhobosSoundVolume.Marked + " sound" +
                  (Sounds.PhobosSoundVolume.Marked == 1 ? "" : "s"));
        report.On("parts bills: " + Bills.PartsBills.Applied + " applied" + (Bills.PartsBills.Failed > 0 ? ", " + Bills.PartsBills.Failed + " failed" : "")
                  + (Bills.PartsBills.OthersAllowed ? ", other mods' parts where installed" : ", always our own parts"));
        string topUps = "topping up: " + TopUp.TopUps.BuildingKindCount + " kind(s) of building by haulers, " + TopUp.TopUps.MechanoidKindCount
            + " kind(s) of mechanoid by mechanitors, " + TopUp.TopUps.VehicleKindCount + " kind(s) of vehicle by haulers";
        foreach (TopUp.PhobosTopUpDef broken in TopUp.TopUps.Problems) topUps += "; " + broken.defName + " stands down (" + broken.problem + ")";
        topUps += ", filling to " + Settings.topUpLevel;
        if (!Settings.topUpMachines) report.Off(topUps, "setting");
        else report.On(topUps);
        report.Log();
    }

    static readonly SettingsBook SettingsPages = SettingsBook.For("PhobosDthorga.Framework", () => "PhobosFramework_SettingsCategory".Translate());
    /// <summary>Every setting at its default, for the settings pages' changed marks and their reset.</summary>
    static readonly FrameworkSettings Defaults = new();

    protected override SettingsBook Book => SettingsPages;

    /// <summary>The settings window's pages (owner's choice of 20 Nov 2026: pages with search, docs/development/settings-pages.md).</summary>
    void AddSettingsPages()
    {
        SettingsPages.Join(this)
            .Add(new SettingsPage("general", 0f, () => "PhobosFramework_PageGeneral".Translate(), DrawGeneral))
            .Add(new SettingsPage("logs", 10f, () => "PhobosFramework_PageLogs".Translate(), DrawLogs))
            .Add(new SettingsPage("verbose", 20f, () => "PhobosFramework_PageVerbose".Translate(), DrawVerbose))
            .Add(new SettingsPage("testing", 30f, () => "PhobosFramework_PageTesting".Translate(), DrawTesting));
    }

    static void DrawGeneral(SettingsList list)
    {
        FrameworkSettings s = Settings;
        list.Section("PhobosFramework_SectionGases".Translate());
        list.Radio("PhobosFramework_SettingGasSourceMode".Translate(), ref s.gasSourceMode, Defaults.gasSourceMode,
            (GasSourceMode[])System.Enum.GetValues(typeof(GasSourceMode)), mode => ("PhobosFramework_GasSourceMode" + mode).Translate(),
            mode => ("PhobosFramework_GasSourceMode" + mode + "Desc").Translate(), "PhobosFramework_SettingGasSourceModeDesc".Translate());

        list.Section("PhobosFramework_SectionBuildCosts".Translate());
        list.Radio("PhobosFramework_SettingBillParts".Translate(), ref s.billPartsSource, Defaults.billPartsSource,
            new[] { Bills.BillPartsSource.OthersWhereInstalled, Bills.BillPartsSource.AlwaysOurs },
            source => source == Bills.BillPartsSource.AlwaysOurs ? "PhobosFramework_BillPartsOurs".Translate() : "PhobosFramework_BillPartsOthers".Translate(),
            source => source == Bills.BillPartsSource.AlwaysOurs ? "PhobosFramework_BillPartsOursDesc".Translate() : "PhobosFramework_BillPartsOthersDesc".Translate(),
            "PhobosFramework_SettingBillPartsDesc".Translate(), restart: true);

        list.Section("PhobosFramework_SectionTopUp".Translate());
        list.Check("PhobosFramework_SettingTopUpMachines".Translate(), ref s.topUpMachines, Defaults.topUpMachines, "PhobosFramework_SettingTopUpMachinesDesc".Translate());
        if (s.topUpMachines)
        {
            list.Indent();
            list.Radio("PhobosFramework_SettingTopUpLevel".Translate(), ref s.topUpLevel, Defaults.topUpLevel,
                (TopUpLevel[])System.Enum.GetValues(typeof(TopUpLevel)), level => ("PhobosFramework_TopUpLevel" + level).Translate(),
                level => ("PhobosFramework_TopUpLevel" + level + "Desc").Translate(), "PhobosFramework_SettingTopUpLevelDesc".Translate());
            list.Outdent();
        }

        list.Section("PhobosFramework_SectionSound".Translate());
        if (list.Slider("PhobosFramework_SettingSoundVolume".Translate(SoundVolumeRule.Percent(s.soundVolume).Named("PERCENT")), ref s.soundVolume,
                Defaults.soundVolume, 0f, SoundVolumeRule.MaxFactor,
                "PhobosFramework_SettingSoundVolumeDesc".Translate(SoundVolumeRule.Percent(SoundVolumeRule.MaxFactor).Named("MAX")), SoundVolumeRule.Clamp))
        {
            Sounds.PhobosSoundVolume.Apply(s.soundVolume);
        }
        list.Note("PhobosFramework_SoundVehiclesElsewhere".Translate());
    }

    static void DrawLogs(SettingsList list)
    {
        FrameworkSettings s = Settings;
        list.Check("PhobosFramework_SettingKeepGameLogAlive".Translate(), ref s.keepGameLogAlive, Defaults.keepGameLogAlive,
            "PhobosFramework_SettingKeepGameLogAliveDesc".Translate(Rules.GameLogRule.MaxRevivals.Named("COUNT")));
        list.Check("PhobosFramework_SettingNoteOurPartInErrors".Translate(), ref s.noteOurPartInErrors, Defaults.noteOurPartInErrors,
            "PhobosFramework_SettingNoteOurPartInErrorsDesc".Translate(Rules.ErrorAttributionRule.MaxNotesPerSession.Named("COUNT")));
        list.Check("PhobosFramework_SettingWriteColonyMoodFile".Translate(), ref s.writeColonyMoodFile, Defaults.writeColonyMoodFile,
            "PhobosFramework_SettingWriteColonyMoodFileDesc".Translate(Mood.ColonyMoodFile.OutputPath.Named("PATH")));
        // Both work only in dev mode (FrameworkDebugActions, PatchedDefsDump), so they are shown only there (settings audit, 20 Nov 2026).
        if (!Prefs.DevMode) return;
        list.Section("PhobosFramework_SectionDeveloper".Translate());
        list.Check("PhobosFramework_SettingShowGasOverlay".Translate(), ref s.showGasOverlay, Defaults.showGasOverlay,
            "PhobosFramework_SettingShowGasOverlayDesc".Translate());
        list.Check("PhobosFramework_SettingWritePatchedDefs".Translate(), ref s.writePatchedDefsOnce, Defaults.writePatchedDefsOnce,
            "PhobosFramework_SettingWritePatchedDefsDesc".Translate(Debug.PatchedDefsDump.OutputPath.Named("PATH")), restart: true);
    }

    /// <summary>Verbose logging (owner, 16 Nov 2026): the master switch, then a tick for each log of ours, named as its lines are headed in the log.</summary>
    static void DrawVerbose(SettingsList list)
    {
        FrameworkSettings s = Settings;
        list.Note("PhobosFramework_SectionVerboseDesc".Translate());
        list.Check("PhobosFramework_SettingVerboseAll".Translate(), ref s.verboseAll, Defaults.verboseAll, "PhobosFramework_SettingVerboseAllDesc".Translate());
        list.Section("PhobosFramework_SectionVerbose".Translate());
        foreach (Logging.LogTopic topic in Logging.Logs.Topics)
        {
            // Under the master switch every tick shows on and greyed, and none is marked as changed.
            bool on = s.verboseAll || s.Verbose(topic.Key);
            if (list.Check("PhobosFramework_SettingVerboseTopic".Translate(topic.Label.Named("LOG")), ref on, s.verboseAll ? null : false,
                    "PhobosFramework_SettingVerboseTopicDesc".Translate(), enabled: !s.verboseAll) && !s.verboseAll)
            {
                s.SetVerbose(topic.Key, on);
            }
        }
    }

    /// <summary>Seen-in-play notes (owner, 17 Nov 2026): for testing with D'thorga Companion's Tests page; off by default.</summary>
    static void DrawTesting(SettingsList list)
    {
        FrameworkSettings s = Settings;
        list.Check("PhobosFramework_SettingNoteSeenInPlay".Translate(), ref s.noteSeenInPlay, Defaults.noteSeenInPlay,
            "PhobosFramework_SettingNoteSeenInPlayDesc".Translate(Debug.SeenInPlay.OutputPath.Named("PATH")));
        // Framework 0.138.0 (seen-in-play checks, E8): greyed while notes are off, as it says nothing without them.
        list.Check("PhobosFramework_SettingSayWhenTestHappens".Translate(), ref s.sayWhenTestHappens, Defaults.sayWhenTestHappens,
            "PhobosFramework_SettingSayWhenTestHappensDesc".Translate(), enabled: s.noteSeenInPlay);
    }
}
