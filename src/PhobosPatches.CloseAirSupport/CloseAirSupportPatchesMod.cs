using Phobos.Framework;
using Phobos.Framework.Patching;
using Phobos.Shared;
using Verse;

namespace Phobos.CloseAirSupportPatches;

/// <summary>Player preferences for the Close Air Support part. The saved key never changes.</summary>
public class CloseAirSupportPatchesSettings : ModSettings
{
    /// <summary>The A-10's and Su-25's speeds on the Harrier's scale by their real figures, and the A-10 slower on one engine (off: the
    /// author's figures). Read once, by the XML patch, at start-up.</summary>
    public bool realisticHandling;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref realisticHandling, "PhobosCloseAirSupportPatches_RealisticHandling", false);
    }
}

/// <summary>
/// D'thorga Patches' Close Air Support part (from 0.29.0; owner, 21 Nov 2026: the Harrier's treatment for the A-10 and Su-25), switched on
/// only while Close Air Support is active (this assembly lives in its load folder). Its parts lists, parts bills and fabricator recipes are
/// XML; this registers the one setting, realistic handling, off by default (owner's choice), as a patch flag in the Mod constructor, which
/// the game runs before it applies any patch. Its XML (Handling_CloseAirSupport.xml) holds every figure and where it comes from.
/// </summary>
public class CloseAirSupportPatchesMod : SettingsBookMod
{
    /// <summary>The Harmony ID is the packageId, shared by every part; never change it once the mod is published.</summary>
    public const string PackageId = "PhobosDthorga.Patches";
    public const string ModVersion = "0.31.0";
    public const string LogPrefix = "[D'thorga Patches]";
    public const string RealisticHandlingFlag = "PhobosCloseAirSupportPatches.realisticHandling";

    public static CloseAirSupportPatchesSettings Settings { get; private set; } = new();

    public CloseAirSupportPatchesMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<CloseAirSupportPatchesSettings>();
        PatchFlags.Register(RealisticHandlingFlag, () => Settings.realisticHandling);
        AddSettingsPages();
        LongEventHandler.ExecuteWhenFinished(LogLoaded);
    }

    static void LogLoaded()
    {
        var report = new LoadReport(LogPrefix, ModVersion, "Close Air Support");
        report.On("the A-10's and Su-25's fuller parts lists (D'thorga Framework's flight controls rule grounds them when they are too damaged)");
        report.On("the jets built from parts by role, their engines turbines");
        if (DefDatabase<ThingDef>.GetNamedSilentFail("DMS_TableAmmoPrinter") != null) report.On("their bombs at the ammunition fabricator");
        else report.Off("their bombs at the ammunition fabricator", "The Dead Man's Switch's fabricator is not loaded");
        const string handling = "realistic handling (the jets' speeds by their real figures, the A-10 slower on one engine)";
        if (Settings.realisticHandling) report.On(handling);
        else report.Off(handling, "its setting is off, the default");
        report.Log();
    }

    static readonly SettingsBook SettingsPages = SettingsBook.For("PhobosDthorga.Patches", () => "PhobosPatches_SettingsBook".Translate());
    static readonly CloseAirSupportPatchesSettings Defaults = new();
    protected override SettingsBook Book => SettingsPages;

    void AddSettingsPages() =>
        SettingsPages.Join(this).Add(new SettingsPage("closeairsupport", 21f, () => "PhobosCloseAirSupportPatches_Page".Translate(), DrawSettings));

    static void DrawSettings(SettingsList list)
    {
        CloseAirSupportPatchesSettings s = Settings, d = Defaults;
        list.Section("PhobosCloseAirSupportPatches_SectionHandling".Translate());
        list.Check("PhobosCloseAirSupportPatches_SettingRealisticHandling".Translate(), ref s.realisticHandling, d.realisticHandling,
            "PhobosCloseAirSupportPatches_SettingRealisticHandlingDesc".Translate());
        list.Note("PhobosCloseAirSupportPatches_NeedsRestart".Translate());
    }
}
