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

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref gasSourceMode, "PhobosFramework_GasSourceMode", GasSourceMode.Automatic);
        Scribe_Values.Look(ref showGasOverlay, "PhobosFramework_ShowGasOverlay", false);
    }
}

/// <summary>
/// D'thorga Framework: the shared library every Phobos RimWorld mod with C# depends on. It holds the gas layer (what a pawn
/// breathes, the game's and other mods' gases reused first, and the framework's own additive room gases), the health layer the
/// medical mods share (who the doctors look after, how dangerous a condition is, per-pawn care choices) and the small helpers the
/// mods used to repeat. It references no other mod's assembly and needs no Harmony.
/// </summary>
public class FrameworkMod : Mod
{
    public const string PackageId = "PhobosDthorga.Framework";
    public const string ModVersion = "0.20.0";
    public const string LogPrefix = "[D'thorga Framework]";

    public static FrameworkSettings Settings { get; private set; } = new();

    public FrameworkMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<FrameworkSettings>();
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
        listing.CheckboxLabeled("PhobosFramework_SettingShowGasOverlay".Translate(), ref Settings.showGasOverlay,
            "PhobosFramework_SettingShowGasOverlayDesc".Translate());
        listing.End();
    }
}
