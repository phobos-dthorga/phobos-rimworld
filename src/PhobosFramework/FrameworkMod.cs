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
    /// <summary>Write every colonist's thoughts beside each save, for the mood report (Mood/ColonyMoodFile; owner, 10 Nov 2026; on by default, agent default).</summary>
    public bool writeColonyMoodFile = true;
    /// <summary>Every D'thorga sound's volume, from none to half as loud again (Sounds/PhobosSoundVolume; AGENTS.md's volume setting; agent default 1).</summary>
    public float soundVolume = 1f;
    /// <summary>Whether the vehicle systems' sounds (a tyre bursting, a knock, the warning chime) also play on other mods' vehicles running them
    /// (owner, 10 Nov 2026: yes, with a setting to turn it off). Their engines and doors are always their authors' own.</summary>
    public bool systemSoundsOnOtherVehicles = true;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref soundVolume, "PhobosFramework_SoundVolume", 1f);
        Scribe_Values.Look(ref systemSoundsOnOtherVehicles, "PhobosFramework_SystemSoundsOnOtherVehicles", true);
        Scribe_Values.Look(ref writeColonyMoodFile, "PhobosFramework_WriteColonyMoodFile", true);
        Scribe_Values.Look(ref gasSourceMode, "PhobosFramework_GasSourceMode", GasSourceMode.Automatic);
        Scribe_Values.Look(ref showGasOverlay, "PhobosFramework_ShowGasOverlay", false);
        Scribe_Values.Look(ref writePatchedDefsOnce, "PhobosFramework_WritePatchedDefsOnce", false);
        Scribe_Values.Look(ref keepGameLogAlive, "PhobosFramework_KeepGameLogAlive", true);
    }
}

/// <summary>
/// D'thorga Framework: the shared library every Phobos RimWorld mod with C# depends on. It holds the gas layer (what a pawn
/// breathes, the game's and other mods' gases reused first, and the framework's own additive room gases), the health layer the
/// medical mods share (who the doctors look after, how dangerous a condition is, per-pawn care choices) and the small helpers the
/// mods used to repeat, and keeps the game's log going past its message limit. It references no other mod's assembly and needs no
/// Harmony.
/// </summary>
public class FrameworkMod : Mod
{
    public const string PackageId = "PhobosDthorga.Framework";
    public const string ModVersion = "0.92.0";
    public const string LogPrefix = "[D'thorga Framework]";
    public static readonly Phobos.Logging.PhobosLog Logger = Logging.Logs.For("D'thorga Framework");

    public static FrameworkSettings Settings { get; private set; } = new();

    public FrameworkMod(ModContentPack content) : base(content)
    {
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
        const string moodFile = "colony mood file at each save, for the mood report";
        if (Settings.writeColonyMoodFile) report.On(moodFile);
        else report.Off(moodFile, "setting");
        Sounds.PhobosSoundVolume.Apply(Settings.soundVolume);
        report.On("sound volume " + SoundVolumeRule.Percent(Settings.soundVolume) + "% on " + Sounds.PhobosSoundVolume.Marked + " sound" +
                  (Sounds.PhobosSoundVolume.Marked == 1 ? "" : "s"));
        report.Log();
    }

    public override string SettingsCategory() => "PhobosFramework_SettingsCategory".Translate();

    public override void DoSettingsWindowContents(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        listing.Label("PhobosFramework_SettingGasSourceMode".Translate(), tooltip: "PhobosFramework_SettingGasSourceModeDesc".Translate());
        foreach (GasSourceMode mode in System.Enum.GetValues(typeof(GasSourceMode)))
        {
            if (listing.RadioButton(("PhobosFramework_GasSourceMode" + mode).Translate(), Settings.gasSourceMode == mode, 8f,
                    ("PhobosFramework_GasSourceMode" + mode + "Desc").Translate()))
            {
                Settings.gasSourceMode = mode;
            }
        }
        listing.Gap();
        listing.Label("PhobosFramework_SettingSoundVolume".Translate(SoundVolumeRule.Percent(Settings.soundVolume).Named("PERCENT")),
            tooltip: "PhobosFramework_SettingSoundVolumeDesc".Translate(SoundVolumeRule.Percent(SoundVolumeRule.MaxFactor).Named("MAX")));
        float volume = SoundVolumeRule.Clamp(listing.Slider(Settings.soundVolume, 0f, SoundVolumeRule.MaxFactor));
        if (volume != Settings.soundVolume)
        {
            Settings.soundVolume = volume;
            Sounds.PhobosSoundVolume.Apply(volume);
        }
        listing.CheckboxLabeled("PhobosFramework_SettingSystemSoundsOnOtherVehicles".Translate(), ref Settings.systemSoundsOnOtherVehicles,
            "PhobosFramework_SettingSystemSoundsOnOtherVehiclesDesc".Translate());
        listing.Gap();
        listing.CheckboxLabeled("PhobosFramework_SettingShowGasOverlay".Translate(), ref Settings.showGasOverlay,
            "PhobosFramework_SettingShowGasOverlayDesc".Translate());
        listing.CheckboxLabeled("PhobosFramework_SettingKeepGameLogAlive".Translate(), ref Settings.keepGameLogAlive,
            "PhobosFramework_SettingKeepGameLogAliveDesc".Translate(Rules.GameLogRule.MaxRevivals.Named("COUNT")));
        listing.CheckboxLabeled("PhobosFramework_SettingWriteColonyMoodFile".Translate(), ref Settings.writeColonyMoodFile,
            "PhobosFramework_SettingWriteColonyMoodFileDesc".Translate(Mood.ColonyMoodFile.OutputPath.Named("PATH")));
        if (Prefs.DevMode)
        {
            listing.CheckboxLabeled("PhobosFramework_SettingWritePatchedDefs".Translate(), ref Settings.writePatchedDefsOnce,
                "PhobosFramework_SettingWritePatchedDefsDesc".Translate(Debug.PatchedDefsDump.OutputPath.Named("PATH")));
        }
        listing.End();
    }
}
