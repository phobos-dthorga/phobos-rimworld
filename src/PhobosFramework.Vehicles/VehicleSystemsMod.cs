using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Phobos.Framework;
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
    /// <summary>Fuel follows the engine model's load instead of the vehicle author's flat rates (owner, 9 Nov 2026: off by default). Live.</summary>
    public bool loadDependentFuel;
    /// <summary>What happens when a jet is too heavy or the day too hot for it to hover onto its pad (owner, 9 Nov 2026: a choice). Live.</summary>
    public HoverLimit hoverLimit = HoverLimit.RefusePadUseRunway;

    public enum HoverLimit
    {
        RefusePadUseRunway,
        AllowWithDamage,
        WarnOnly,
    }

    public bool FlagOn(string name) => name switch
    {
        "fuelGelling" => fuelGelling,
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
        listing.Gap(6f);
        listing.Label("PhobosVehicleSystems_SettingHoverLimit".Translate());
        foreach (VehicleSystemsSettings.HoverLimit limit in System.Enum.GetValues(typeof(VehicleSystemsSettings.HoverLimit)))
        {
            if (listing.RadioButton(("PhobosVehicleSystems_Hover" + limit).Translate(), s.hoverLimit == limit, 8f, ("PhobosVehicleSystems_Hover" + limit + "Desc").Translate()))
            {
                s.hoverLimit = limit;
            }
        }

        SettingsUi.Section(listing, "PhobosVehicleSystems_SectionShow".Translate());
        SettingsUi.DrawDisplayMode(listing, ref s.displayMode);

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
