using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Phobos.Framework;
using Phobos.Framework.Patching;
using Phobos.Shared;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>Player preferences for vehicle systems. Physics lives in the system Defs and the rules Def, never here.</summary>
public class VehicleSystemsSettings : ModSettings
{
    public enum Scope
    {
        AllVehicles,
        PhobosOnly,
    }

    /// <summary>Which vehicles get systems at all. Needs a restart.</summary>
    public Scope scope = Scope.AllVehicles;
    /// <summary>Package IDs (lower case) whose vehicles get no systems. Needs a restart.</summary>
    public List<string> disabledPackages = new();
    /// <summary>Loadout kinds that are off. Needs a restart.</summary>
    public List<string> disabledKinds = new();
    /// <summary>System Defs that are off. Live.</summary>
    public List<string> disabledSystems = new();
    /// <summary>Figures only: the systems run and show, but nothing happens to the vehicle. Live.</summary>
    public bool showOnly;
    /// <summary>Chemfuel thickens in deep cold the way diesel gels. Live.</summary>
    public bool fuelGelling;
    public DisplayMode displayMode = DisplayMode.Plain;
    /// <summary>The drivetrain and turbines: off, figures only, or feeding load, heat and the speed cap. Live.</summary>
    public DrivetrainMode drivetrainMode = DrivetrainMode.Full;
    /// <summary>Fuel follows the engine model's load instead of the vehicle author's flat rates (owner, 8 Nov 2026: off by default). Live.</summary>
    public bool loadDependentFuel;
    /// <summary>What happens when a jet is too heavy or the day too hot for it to hover onto its pad (owner, 8 Nov 2026: a choice). Live.</summary>
    public HoverLimit hoverLimit = HoverLimit.RefusePadUseRunway;
    /// <summary>A steam boiler without a fusible plug explodes if its crown sheet is uncovered (owner, 8 Nov 2026: off by default, with
    /// a warning). Live.</summary>
    public bool boilerExplosions;
    /// <summary>Water stops draw a steam locomotive's real tender or tank capacity instead of a tenth of it (owner, 8 Nov 2026: scaled by
    /// default). Live.</summary>
    public bool realisticFeedWater;
    /// <summary>A fuel-burning night heater keeps a closed cab warm while a caravan camps (owner, 8 Nov 2026: a setting, off). Live.</summary>
    public bool nightHeater;
    /// <summary>A jump-start needs a crafted jump-start kit, in stock or aboard (owner, 8 Nov 2026: default on). Live.</summary>
    public bool jumpStartNeedsKit = true;
    /// <summary>Every vehicle is assumed to carry a booster pack, so a jump-start needs no other vehicle (owner, 8 Nov 2026: off). Live.</summary>
    public bool assumeBoosterPack;
    /// <summary>People aboard a closed cab are glad of it or unhappy in it, by their own comfort range (agent default, owner to revise:
    /// on). Live.</summary>
    public bool cabMoods = true;
    /// <summary>Every closed cab has automatic climate control, not only the vehicles built with it (owner, 8 Nov 2026: a blanket
    /// option; agent default: off). Live.</summary>
    public bool climateControlAll;
    /// <summary>Other factions' vehicles on a map run the full model, as the player's do, instead of the lite tier (owner, 8 Nov 2026:
    /// lite by default with this setting). Live.</summary>
    public bool fullModelForEveryone;
    /// <summary>Keep Vehicle Framework's own Health tab instead of ours (agent default: ours). Needs a restart: tabs are set while Defs load.</summary>
    public bool keepVehicleFrameworkHealthTab;
    /// <summary>An electric vehicle's battery that overheats far enough catches fire and bursts (agent default, as boiler explosions:
    /// off, with a warning). Live.</summary>
    public bool batteryFires;
    /// <summary>D'thorga Vehicles turns Vanilla Vehicles Expanded's Lightning into an electric van, to try the electric systems (owner,
    /// 8 Nov 2026: a test vehicle; agent default: off). Needs a restart: it changes the vehicle's design while the game loads.</summary>
    public bool electricLightning;

    public enum HoverLimit
    {
        RefusePadUseRunway,
        AllowWithDamage,
        WarnOnly,
    }

    public bool FlagOn(string name) => name switch
    {
        "fuelGelling" => fuelGelling,
        "boilerExplosions" => boilerExplosions,
        "realisticFeedWater" => realisticFeedWater,
        "nightHeater" => nightHeater,
        "jumpStartNeedsKit" => jumpStartNeedsKit,
        "assumeBoosterPack" => assumeBoosterPack,
        "cabMoods" => cabMoods,
        "climateControlAll" => climateControlAll,
        "fullModelForEveryone" => fullModelForEveryone,
        "keepVehicleFrameworkHealthTab" => keepVehicleFrameworkHealthTab,
        "batteryFires" => batteryFires,
        "electricLightning" => electricLightning,
        _ => false,
    };

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref scope, "PhobosVehicleSystems_Scope", Scope.AllVehicles);
        Scribe_Collections.Look(ref disabledPackages, "PhobosVehicleSystems_DisabledPackages", LookMode.Value);
        Scribe_Collections.Look(ref disabledKinds, "PhobosVehicleSystems_DisabledKinds", LookMode.Value);
        Scribe_Collections.Look(ref disabledSystems, "PhobosVehicleSystems_DisabledSystems", LookMode.Value);
        Scribe_Values.Look(ref showOnly, "PhobosVehicleSystems_ShowOnly", false);
        Scribe_Values.Look(ref fuelGelling, "PhobosVehicleSystems_FuelGelling", false);
        Scribe_Values.Look(ref displayMode, "PhobosVehicleSystems_DisplayMode", DisplayMode.Plain);
        Scribe_Values.Look(ref drivetrainMode, "PhobosVehicleSystems_DrivetrainMode", DrivetrainMode.Full);
        Scribe_Values.Look(ref loadDependentFuel, "PhobosVehicleSystems_LoadDependentFuel", false);
        Scribe_Values.Look(ref hoverLimit, "PhobosVehicleSystems_HoverLimit", HoverLimit.RefusePadUseRunway);
        Scribe_Values.Look(ref boilerExplosions, "PhobosVehicleSystems_BoilerExplosions", false);
        Scribe_Values.Look(ref realisticFeedWater, "PhobosVehicleSystems_RealisticFeedWater", false);
        Scribe_Values.Look(ref nightHeater, "PhobosVehicleSystems_NightHeater", false);
        Scribe_Values.Look(ref jumpStartNeedsKit, "PhobosVehicleSystems_JumpStartNeedsKit", true);
        Scribe_Values.Look(ref assumeBoosterPack, "PhobosVehicleSystems_AssumeBoosterPack", false);
        Scribe_Values.Look(ref cabMoods, "PhobosVehicleSystems_CabMoods", true);
        Scribe_Values.Look(ref climateControlAll, "PhobosVehicleSystems_ClimateControlAll", false);
        Scribe_Values.Look(ref fullModelForEveryone, "PhobosVehicleSystems_FullForEveryone", false);
        Scribe_Values.Look(ref keepVehicleFrameworkHealthTab, "PhobosVehicleSystems_KeepVfHealthTab", false);
        Scribe_Values.Look(ref batteryFires, "PhobosVehicleSystems_BatteryFires", false);
        Scribe_Values.Look(ref electricLightning, "PhobosVehicleSystems_ElectricLightning", false);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            disabledPackages ??= new List<string>();
            disabledKinds ??= new List<string>();
            disabledSystems ??= new List<string>();
        }
    }
}

/// <summary>
/// D'thorga Framework's vehicle systems part, loaded only with Vehicle Framework (LoadFolders.xml). Runs in the Mod constructor so
/// its patch on VehicleDef.ResolveReferences is in place before any Def is resolved.
/// </summary>
public class VehicleSystemsMod : Mod
{
    /// <summary>The Harmony ID is the Framework's packageId with the part's name; never change it once published.</summary>
    public const string HarmonyId = "PhobosDthorga.Framework.VehicleSystems";
    public const string LogPrefix = "[D'thorga Framework]";

    public static VehicleSystemsSettings Settings { get; private set; } = new();

    readonly SettingsScroll scroll = new();

    public VehicleSystemsMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<VehicleSystemsSettings>();
        // XML patches (PatchOperationPhobosFlag) run after every mod's constructor, so the flags are ready for them.
        PatchFlags.Register("PhobosVehicleSystems.electricLightning", () => Settings.electricLightning);
        HarmonyPatchApplier.ApplyEach(new Harmony(HarmonyId), typeof(VehicleSystemsMod).Assembly, LogPrefix);
        LongEventHandler.ExecuteWhenFinished(LogLoaded);
    }

    static void LogLoaded()
    {
        var report = new LoadReport(LogPrefix, FrameworkMod.ModVersion, "vehicle systems");
        int systems = DefDatabase<PhobosVehicleSystemDef>.AllDefsListForReading.Count;
        int loadouts = DefDatabase<PhobosVehicleLoadoutDef>.AllDefsListForReading.Count;
        int fitted = VehicleSystems.Designs.Count(d => d.skippedBecause == null);
        report.On(systems + " systems, " + loadouts + " loadouts, " + fitted + " of " + VehicleSystems.Designs.Count + " vehicle designs fitted, "
                  + VehicleSystems.Designs.Sum(d => d.partsAdded) + " parts appended");
        if (Settings.showOnly) report.On("figures only (setting)");
        if (Settings.boilerExplosions) report.On("boiler explosions (setting)");
        if (Settings.realisticFeedWater) report.On("realistic feed water (setting)");
        if (Settings.nightHeater) report.On("night heater at camp (setting)");
        if (Settings.jumpStartNeedsKit) report.On("jump-start leads must be crafted (setting)");
        else report.Off("jump-start leads", "free (setting)");
        if (Settings.assumeBoosterPack) report.On("booster pack assumed on every vehicle (setting)");
        if (!Settings.cabMoods) report.Off("cab moods", "off (setting)");
        if (Settings.climateControlAll) report.On("climate control in every closed cab (setting)");
        if (Settings.keepVehicleFrameworkHealthTab) report.Off("the Health tab", "Vehicle Framework's kept (setting)");
        if (Settings.fullModelForEveryone) report.On("full model for every faction's vehicles (setting)");
        else report.On("other factions' vehicles on the lite tier");
        if (Settings.batteryFires) report.On("battery fires (setting)");
        if (Settings.electricLightning) report.On("the Lightning runs on batteries (setting)");
        foreach (IGrouping<string, DesignRecord> skipped in VehicleSystems.Designs.Where(d => d.skippedBecause != null && d.loadout != null).GroupBy(d => d.skippedBecause!))
        {
            report.Off(skipped.Count() + " design" + (skipped.Count() == 1 ? "" : "s"), skipped.Key);
        }
        foreach (DesignRecord tied in VehicleSystems.Designs.Where(d => d.tied))
        {
            VehicleSystems.Problems.Add(tied.def.defName + ": two loadouts of the same priority claim it; " + tied.loadout?.defName + " (loaded last) wins");
        }
        report.Log();
        foreach (string problem in VehicleSystems.Problems) Log.Warning(LogPrefix + " Vehicle systems: " + problem);
    }

    public override string SettingsCategory() => "PhobosVehicleSystems_SettingsCategory".Translate();

    public override void DoSettingsWindowContents(Rect inRect)
    {
        Listing_Standard listing = scroll.Begin(inRect);
        VehicleSystemsSettings s = Settings;

        SettingsUi.Section(listing, "PhobosVehicleSystems_SectionWhich".Translate());
        listing.Label("PhobosVehicleSystems_RestartNote".Translate());
        foreach (VehicleSystemsSettings.Scope scope in System.Enum.GetValues(typeof(VehicleSystemsSettings.Scope)))
        {
            if (listing.RadioButton(("PhobosVehicleSystems_Scope" + scope).Translate(), s.scope == scope, 8f, ("PhobosVehicleSystems_Scope" + scope + "Desc").Translate()))
            {
                s.scope = scope;
            }
        }
        listing.Gap(6f);
        foreach (IGrouping<string, DesignRecord> mod in VehicleSystems.DesignsByMod().OrderBy(g => g.First().modName))
        {
            bool on = !s.disabledPackages.Contains(mod.Key);
            bool before = on;
            listing.CheckboxLabeled("PhobosVehicleSystems_ModVehicles".Translate(mod.First().modName.Named("MOD"), mod.Count().Named("COUNT")), ref on,
                string.Join(", ", mod.Select(d => d.def.LabelCap.ToString())));
            if (on != before) Toggle(s.disabledPackages, mod.Key, on);
        }
        listing.Gap(6f);
        foreach (string kind in DefDatabase<PhobosVehicleLoadoutDef>.AllDefsListForReading.Where(l => l.systems.Count > 0).Select(l => l.kind).Distinct().OrderBy(k => k))
        {
            bool on = !s.disabledKinds.Contains(kind);
            bool before = on;
            string label = ("PhobosVehicleSystems_Kind" + kind).TryTranslate(out TaggedString known) ? known.ToString() : kind;
            listing.CheckboxLabeled(label, ref on);
            if (on != before) Toggle(s.disabledKinds, kind, on);
        }

        SettingsUi.Section(listing, "PhobosVehicleSystems_SectionSystems".Translate());
        listing.CheckboxLabeled("PhobosVehicleSystems_SettingFullForEveryone".Translate(), ref s.fullModelForEveryone, "PhobosVehicleSystems_SettingFullForEveryoneDesc".Translate());
        foreach (PhobosVehicleSystemDef system in DefDatabase<PhobosVehicleSystemDef>.AllDefsListForReading)
        {
            bool on = !s.disabledSystems.Contains(system.defName);
            bool before = on;
            listing.CheckboxLabeled(system.LabelCap, ref on, system.description);
            if (on != before) Toggle(s.disabledSystems, system.defName, on);
        }
        listing.Gap(6f);
        listing.CheckboxLabeled("PhobosVehicleSystems_SettingShowOnly".Translate(), ref s.showOnly, "PhobosVehicleSystems_SettingShowOnlyDesc".Translate());
        listing.CheckboxLabeled("PhobosVehicleSystems_SettingFuelGelling".Translate(), ref s.fuelGelling, "PhobosVehicleSystems_SettingFuelGellingDesc".Translate());

        SettingsUi.Section(listing, "PhobosVehicleSystems_SectionEngines".Translate());
        foreach (DrivetrainMode mode in System.Enum.GetValues(typeof(DrivetrainMode)))
        {
            if (listing.RadioButton(("PhobosVehicleSystems_Drivetrain" + mode).Translate(), s.drivetrainMode == mode, 8f, ("PhobosVehicleSystems_Drivetrain" + mode + "Desc").Translate()))
            {
                s.drivetrainMode = mode;
            }
        }
        listing.Gap(6f);
        listing.CheckboxLabeled("PhobosVehicleSystems_SettingLoadFuel".Translate(), ref s.loadDependentFuel, "PhobosVehicleSystems_SettingLoadFuelDesc".Translate());
        listing.CheckboxLabeled("PhobosVehicleSystems_SettingNightHeater".Translate(), ref s.nightHeater, "PhobosVehicleSystems_SettingNightHeaterDesc".Translate());
        listing.CheckboxLabeled("PhobosVehicleSystems_SettingCabMoods".Translate(), ref s.cabMoods, "PhobosVehicleSystems_SettingCabMoodsDesc".Translate());
        listing.CheckboxLabeled("PhobosVehicleSystems_SettingClimateAll".Translate(), ref s.climateControlAll, "PhobosVehicleSystems_SettingClimateAllDesc".Translate());
        listing.Gap(6f);
        listing.Label("PhobosVehicleSystems_SettingHoverLimit".Translate());
        foreach (VehicleSystemsSettings.HoverLimit limit in System.Enum.GetValues(typeof(VehicleSystemsSettings.HoverLimit)))
        {
            if (listing.RadioButton(("PhobosVehicleSystems_Hover" + limit).Translate(), s.hoverLimit == limit, 8f, ("PhobosVehicleSystems_Hover" + limit + "Desc").Translate()))
            {
                s.hoverLimit = limit;
            }
        }

        SettingsUi.Section(listing, "PhobosVehicleSystems_SectionSteam".Translate());
        listing.CheckboxLabeled("PhobosVehicleSystems_SettingRealisticFeedWater".Translate(), ref s.realisticFeedWater, "PhobosVehicleSystems_SettingRealisticFeedWaterDesc".Translate());
        listing.CheckboxLabeled("PhobosVehicleSystems_SettingBoilerExplosions".Translate(), ref s.boilerExplosions, "PhobosVehicleSystems_SettingBoilerExplosionsDesc".Translate());
        if (s.boilerExplosions) SettingsUi.Warning(listing, "PhobosVehicleSystems_BoilerExplosionsWarning".Translate());

        SettingsUi.Section(listing, "PhobosVehicleSystems_SectionElectric".Translate());
        listing.CheckboxLabeled("PhobosVehicleSystems_SettingBatteryFires".Translate(), ref s.batteryFires, "PhobosVehicleSystems_SettingBatteryFiresDesc".Translate());
        if (s.batteryFires) SettingsUi.Warning(listing, "PhobosVehicleSystems_BatteryFiresWarning".Translate());
        listing.CheckboxLabeled("PhobosVehicleSystems_SettingElectricLightning".Translate(), ref s.electricLightning, "PhobosVehicleSystems_SettingElectricLightningDesc".Translate());

        SettingsUi.Section(listing, "PhobosVehicleSystems_SectionJumpStarts".Translate());
        listing.CheckboxLabeled("PhobosVehicleSystems_SettingJumpStartNeedsKit".Translate(), ref s.jumpStartNeedsKit, "PhobosVehicleSystems_SettingJumpStartNeedsKitDesc".Translate());
        listing.CheckboxLabeled("PhobosVehicleSystems_SettingAssumeBoosterPack".Translate(), ref s.assumeBoosterPack, "PhobosVehicleSystems_SettingAssumeBoosterPackDesc".Translate());

        SettingsUi.Section(listing, "PhobosVehicleSystems_SectionShow".Translate());
        SettingsUi.DrawDisplayMode(listing, ref s.displayMode);
        listing.CheckboxLabeled("PhobosVehicleSystems_SettingKeepVfHealthTab".Translate(), ref s.keepVehicleFrameworkHealthTab, "PhobosVehicleSystems_SettingKeepVfHealthTabDesc".Translate());

        SettingsUi.Section(listing, "PhobosVehicleSystems_SectionRemove".Translate());
        listing.Label("PhobosVehicleSystems_RemoveExplain".Translate());
        if (Current.Game != null && !VehicleSystems.Swept && listing.ButtonText("PhobosVehicleSystems_RemoveButton".Translate()))
        {
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("PhobosVehicleSystems_RemoveConfirm".Translate(), Sweep, destructive: true));
        }
        else if (VehicleSystems.Swept)
        {
            SettingsUi.Warning(listing, "PhobosVehicleSystems_RemoveDone".Translate());
        }
        scroll.End(listing);
    }

    static void Sweep()
    {
        int changed = VehicleSystems.Sweep();
        Messages.Message("PhobosVehicleSystems_RemoveResult".Translate(changed.Named("COUNT")), MessageTypeDefOf.TaskCompletion, historical: false);
    }

    static void Toggle(List<string> off, string key, bool on)
    {
        if (on) off.Remove(key);
        else if (!off.Contains(key)) off.Add(key);
    }
}

/// <summary>The service's own button icons, with a visible fallback of ours if a path is wrong (never the game's red X).</summary>
[StaticConstructorOnStartup]
public static class SystemIcons
{
    public static readonly Texture2D Service = Load("Phobos/VehicleSystems/Service");
    public static readonly Texture2D Switch = Load("Phobos/VehicleSystems/Switch");
    static readonly Dictionary<string, Texture2D> byPath = new();

    static Texture2D Load(string path) => ContentFinder<Texture2D>.Get(path, reportFailure: false) ?? BaseContent.WhiteTex;

    public static Texture2D For(string? path)
    {
        if (path.NullOrEmpty()) return Switch;
        if (!byPath.TryGetValue(path!, out Texture2D? texture))
        {
            texture = ContentFinder<Texture2D>.Get(path, reportFailure: false) ?? Switch;
            byPath[path!] = texture;
        }
        return texture;
    }
}
