using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Phobos.Framework;
using Phobos.Framework.Patching;
using Phobos.Framework.Rules;
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
    /// <summary>A flammable load explodes when its tank is destroyed under it (owner, 8 Nov 2026: on, with this setting to turn it off). Live.</summary>
    public bool tankExplosions = true;
    /// <summary>A tank holds one kind of fluid until its compartment upgrades are fitted (owner, 9 Nov 2026: on). Off, every liquid tank has its most compartments free. Live.</summary>
    public bool tankCompartmentsNeedUpgrades = true;
    /// <summary>A destroyed part needs replacement supplies; ordinary damage stays free (vehicle health, slice 3). Live.</summary>
    public bool destroyedPartsNeedSupplies = true;
    /// <summary>How much of the part an engine or transmission costs to replace: at 100%, the whole part. Live.</summary>
    public float wholePartShare = 1f;
    /// <summary>How much of the full materials any other destroyed part costs to replace. Agent default. Live.</summary>
    public float otherPartShare = 0.5f;
    /// <summary>Colonists can patch leaks and shorts in the field. Live.</summary>
    public bool fieldFixes = true;
    public bool tuningNeedsRig = true;
    /// <summary>What the time to fit and tune is multiplied by (Framework 0.91.0; owner, 11 Nov 2026: a day or more each by default, with
    /// presets). Live: waiting work takes the new figure.</summary>
    public float fitTimeFactor = 1f;
    /// <summary>What the materials to fit and tune are multiplied by (Framework 0.91.0). Live.</summary>
    public float fitMaterialFactor = 1f;
    /// <summary>A bigger vehicle costs more to fit and tune, by its weight (Framework 0.91.0). Live.</summary>
    public bool fitCostScalesWithWeight = true;
    /// <summary>Sling loads (Framework 0.92.0; owner, 11 Nov 2026: each of these a setting): a hot day lifts less. Live.</summary>
    public bool slingHeatDerating = true;
    /// <summary>A colonist hooks a sling load up; off, the helicopter does it itself. Live.</summary>
    public bool slingGroundCrew = true;
    /// <summary>Sling slice 2 (Framework 0.101.0; owner, 11 Nov 2026: each a setting): strong wind or a storm grounds a lift. Live.</summary>
    public bool slingWeather = true;
    /// <summary>A slung flight flies slower and burns more fuel per tile. Live.</summary>
    public bool slingSlowFlight = true;
    /// <summary>Emergency release in flight, and a load dropped when its helicopter crashes; off, no release and a crash keeps the load. Live.</summary>
    public bool slingRelease = true;
    /// <summary>Vehicles without a drivetrain of ours may tow, on the plain rule (Framework 0.101.0; owner, 11 Nov 2026: off by default, with a
    /// warning). Live.</summary>
    public bool hitchOthersPlainRule;
    public bool powerPointLargeSocket;
    /// <summary>Another faction's vehicle may be found with fittings and a tune.</summary>
    public bool enemyFitSets = true;
    public DisplayMode displayMode = DisplayMode.Plain;
    /// <summary>The drivetrain and turbines: off, figures only, or feeding load, heat and the speed cap. Live.</summary>
    public DrivetrainMode drivetrainMode = DrivetrainMode.Full;
    /// <summary>Fuel follows the engine model's load instead of the vehicle author's flat rates. On by default since Framework 0.131.0
    /// (owner, 18 Nov 2026, reversing the default of 8 Nov 2026): the author's rate still holds at a nominal cruise. Live.</summary>
    public bool loadDependentFuel = true;
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
    /// <summary>Blind flying (Framework 0.110.0, avionics A1): an aircraft without working instruments takes off at night or in fog with a
    /// warning instead of being refused (owner, 13 Nov 2026: refuse by default, a setting to warn only).</summary>
    public bool blindFlightWarnOnly;
    /// <summary>A crew whose oxygen fails at height is short of breath for a while (Framework 0.119.0, avionics A14; owner, 13 and 17 Nov 2026: a
    /// setting, off by default, giving a short-lived condition of our own). Live.</summary>
    public bool altitudeHypoxia;
    /// <summary>Another mod's air defence sees our aircraft in flight on the world map and may shoot at them (Framework 0.119.0, AD3; owner,
    /// 13 Nov 2026). Shown only while The Dead Man's Switch - Expedition is active. Live.</summary>
    public bool airDefenceSeesAircraft = true;
    /// <summary>An engine's service falls due as soon as its oil is low, so a worn engine burning oil calls a mechanic early (owner, 10 Nov
    /// 2026: oil use is a reason to service, with a setting to make it only a cost: on). Live.</summary>
    public bool lowOilCallsService = true;
    /// <summary>Lights, heaters and climate control take power from the engine through the alternator, so they cost a little heat, and
    /// fuel when fuel follows the engine model (crosslink 5; owner, 10 Nov 2026: on, with this setting to turn it off). Live.</summary>
    public bool accessoriesLoadEngine = true;
    /// <summary>Rough ground wears tyres, tracks and suspension faster than a road (crosslink 6; owner, 10 Nov 2026: a setting, on). Live.</summary>
    public bool roughGroundWear = true;
    /// <summary>Headlights light the ground ahead of the vehicle (Framework 0.104.0; owner, 12 Nov 2026: by default). Off, they still draw
    /// on the battery and give no light: for a player who wants every last tick. Live.</summary>
    public bool headlightBeams = true;
    /// <summary>The headlights and work lamps of the player's vehicles switch themselves on and off (Framework 0.171.0; owner, 19 Nov 2026:
    /// "a setting that's on by default"): headlights while the vehicle is driven at night, work lamps while it stands at night with people
    /// near. Each vehicle's button then says whether its lights may switch themselves on at all. Off, the buttons switch them by hand. Live.</summary>
    public bool autoLights = true;
    /// <summary>Rain, falling snow and ice take grip from the driven wheels, which tells on hills, in mud and when towing (Framework 0.113.0,
    /// cross-links G5; owner, 13 Nov 2026: on, with this setting). Live.</summary>
    public bool weatherGrip = true;
    /// <summary>Thin air at height takes engine power, rotor lift and cooling (Framework 0.132.0; owner, 18 Nov 2026: altitude, built with the
    /// turbine slice). Off, every tile is at the sea. Live.</summary>
    public bool thinAir = true;
    /// <summary>An aircraft's fuel follows its engines: flying between tiles, and hovering, climbing and idling on a map (Framework 0.132.0;
    /// owner, 18 Nov 2026: "world flight and on-map phases"). Off, aircraft burn their authors' flat rates. Live.</summary>
    public bool aircraftFuel = true;
    /// <summary>Each leg between tiles is flown at a height of its own: over the highest ground, under the ceiling the engines reach and the
    /// oxygen line, with fuel and speed following it (Framework 0.141.0; owner, 18 Nov 2026: build it). Off, every leg is flown as its author
    /// made it, with avionics A14's fixed figures for a jet whose oxygen is out. Live.</summary>
    public bool cruiseHeight = true;
    /// <summary>Ammunition in a burning vehicle's cargo can cook off (Framework 0.113.0, cross-links G13; owner, 13 Nov 2026: on by default,
    /// and only ever where Combat Extended's ammunition system is on). Live.</summary>
    public bool ammoCookOff = true;
    /// <summary>Raiders of an industrial or better faction may arrive in one of our aircraft, which puts them down and leaves (Framework 0.157.0;
    /// owner, 19 Nov 2026: on, with a setting). Off, raids never arrive that way. Live: read as each raid picks how it arrives.</summary>
    public bool raidersArriveByAir = true;
    /// <summary>Vehicle Framework's own helicopters given our flight in XML (the Cherokee, the Louie, the Mosquito) fly over the map on it
    /// (Framework 0.160.0; owner, 18 Nov 2026: a setting, off by default). Off, they fly as their authors made them. Live, for a helicopter
    /// on the ground; one in the air finishes its flight first.</summary>
    public bool stockHelicoptersFly;
    /// <summary>A resting caravan's cooks turn its raw food into simple meals at a field kitchen it carries, once a night (Framework 0.121.0;
    /// vehicle-field-kitchen.md; owner, 13 Nov 2026: a setting, on by default). Live.</summary>
    public bool caravanCooking = true;
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
    /// <summary>Keep Vehicle Framework's own Health tab instead of ours (agent default: ours). Needs a restart: tabs are set while Defs load.</summary>
    public bool keepVehicleFrameworkHealthTab;
    /// <summary>The fleet board's button on the bottom bar. Takes effect at once.</summary>
    public bool fleetBoardButton = true;
    /// <summary>Days of warning before a vehicle left parked in the wild is lost; 0 is no warning.</summary>
    public int parkedWarningDays = 3;
    /// <summary>An electric vehicle's battery that overheats far enough catches fire and bursts (agent default, as boiler explosions:
    /// off, with a warning). Live.</summary>
    public bool batteryFires;
    /// <summary>D'thorga Vehicles turns Vanilla Vehicles Expanded's Lightning into an electric van, to try the electric systems (owner,
    /// 8 Nov 2026: a test vehicle; agent default: off). Needs a restart: it changes the vehicle's design while the game loads.</summary>
    public bool electricLightning;
    /// <summary>A journey away from any map where something went wrong (a new fault, a halt, someone hurt) is also a letter (Framework
    /// 0.181.0, EM25; agent default, agreed by the owner on 20 Nov 2026: on). Every journey goes in the logbook either way. Live.</summary>
    public bool tripLetters = true;
    /// <summary>A running engine fills the closed room it stands in with carbon monoxide (Framework 0.187.0, EM20; owner, 20 Nov 2026: off by
    /// default). Harms only with D'thorga Medical, which turns breathing it into poisoning. Live.</summary>
    public bool exhaustIndoors;
    /// <summary>How far the vehicle systems' messages nobody asked for are quietened (Framework 0.186.0, VehicleMessageLevel; owner's
    /// choice of 20 Nov 2026: a setting; agent default: every message, as before). Live.</summary>
    public MessageLevel messageLevel = MessageLevel.All;

    public enum HoverLimit
    {
        RefusePadUseRunway,
        AllowWithDamage,
        WarnOnly,
    }

    public bool FlagOn(string name) => name switch
    {
        "fuelGelling" => fuelGelling,
        "tankExplosions" => tankExplosions,
        "tankCompartmentsNeedUpgrades" => tankCompartmentsNeedUpgrades,
        "boilerExplosions" => boilerExplosions,
        "realisticFeedWater" => realisticFeedWater,
        "nightHeater" => nightHeater,
        "blindFlightWarnOnly" => blindFlightWarnOnly,
        "altitudeHypoxia" => altitudeHypoxia,
        "airDefenceSeesAircraft" => airDefenceSeesAircraft,
        "lowOilCallsService" => lowOilCallsService,
        "accessoriesLoadEngine" => accessoriesLoadEngine,
        "roughGroundWear" => roughGroundWear,
        "weatherGrip" => weatherGrip,
        "thinAir" => thinAir,
        "aircraftFuel" => aircraftFuel,
        "cruiseHeight" => cruiseHeight,
        "ammoCookOff" => ammoCookOff,
        "raidersArriveByAir" => raidersArriveByAir,
        "stockHelicoptersFly" => stockHelicoptersFly,
        "caravanCooking" => caravanCooking,
        "jumpStartNeedsKit" => jumpStartNeedsKit,
        "assumeBoosterPack" => assumeBoosterPack,
        "cabMoods" => cabMoods,
        "climateControlAll" => climateControlAll,
        "keepVehicleFrameworkHealthTab" => keepVehicleFrameworkHealthTab,
        "batteryFires" => batteryFires,
        "exhaustIndoors" => exhaustIndoors,
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
        Scribe_Values.Look(ref tankExplosions, "PhobosVehicleSystems_TankExplosions", true);
        Scribe_Values.Look(ref tankCompartmentsNeedUpgrades, "PhobosVehicleSystems_TankCompartmentsNeedUpgrades", true);
        Scribe_Values.Look(ref destroyedPartsNeedSupplies, "PhobosVehicleSystems_DestroyedPartsNeedSupplies", true);
        Scribe_Values.Look(ref wholePartShare, "PhobosVehicleSystems_WholePartShare", 1f);
        Scribe_Values.Look(ref otherPartShare, "PhobosVehicleSystems_OtherPartShare", 0.5f);
        Scribe_Values.Look(ref fieldFixes, "PhobosVehicleSystems_FieldFixes", true);
        Scribe_Values.Look(ref tuningNeedsRig, "PhobosVehicleSystems_TuningNeedsRig", true);
        Scribe_Values.Look(ref fitTimeFactor, "PhobosVehicleSystems_FitTimeFactor", 1f);
        Scribe_Values.Look(ref fitMaterialFactor, "PhobosVehicleSystems_FitMaterialFactor", 1f);
        Scribe_Values.Look(ref fitCostScalesWithWeight, "PhobosVehicleSystems_FitCostScalesWithWeight", true);
        Scribe_Values.Look(ref slingHeatDerating, "PhobosVehicleSystems_SlingHeatDerating", true);
        Scribe_Values.Look(ref slingGroundCrew, "PhobosVehicleSystems_SlingGroundCrew", true);
        Scribe_Values.Look(ref slingWeather, "PhobosVehicleSystems_SlingWeather", true);
        Scribe_Values.Look(ref slingSlowFlight, "PhobosVehicleSystems_SlingSlowFlight", true);
        Scribe_Values.Look(ref slingRelease, "PhobosVehicleSystems_SlingRelease", true);
        Scribe_Values.Look(ref hitchOthersPlainRule, "PhobosVehicleSystems_HitchOthersPlainRule", false);
        Scribe_Values.Look(ref powerPointLargeSocket, "PhobosVehicleSystems_PowerPointLargeSocket", false);
        Scribe_Values.Look(ref enemyFitSets, "PhobosVehicleSystems_EnemyFitSets", true);
        Scribe_Values.Look(ref displayMode, "PhobosVehicleSystems_DisplayMode", DisplayMode.Plain);
        Scribe_Values.Look(ref drivetrainMode, "PhobosVehicleSystems_DrivetrainMode", DrivetrainMode.Full);
        Scribe_Values.Look(ref loadDependentFuel, "PhobosVehicleSystems_LoadDependentFuel", true);
        Scribe_Values.Look(ref hoverLimit, "PhobosVehicleSystems_HoverLimit", HoverLimit.RefusePadUseRunway);
        Scribe_Values.Look(ref boilerExplosions, "PhobosVehicleSystems_BoilerExplosions", false);
        Scribe_Values.Look(ref realisticFeedWater, "PhobosVehicleSystems_RealisticFeedWater", false);
        Scribe_Values.Look(ref nightHeater, "PhobosVehicleSystems_NightHeater", false);
        Scribe_Values.Look(ref blindFlightWarnOnly, "PhobosVehicleSystems_BlindFlightWarnOnly", false);
        Scribe_Values.Look(ref altitudeHypoxia, "PhobosVehicleSystems_AltitudeHypoxia", false);
        Scribe_Values.Look(ref airDefenceSeesAircraft, "PhobosVehicleSystems_AirDefenceSeesAircraft", true);
        Scribe_Values.Look(ref lowOilCallsService, "PhobosVehicleSystems_LowOilCallsService", true);
        Scribe_Values.Look(ref accessoriesLoadEngine, "PhobosVehicleSystems_AccessoriesLoadEngine", true);
        Scribe_Values.Look(ref roughGroundWear, "PhobosVehicleSystems_RoughGroundWear", true);
        Scribe_Values.Look(ref headlightBeams, "PhobosVehicleSystems_HeadlightBeams", true);
        Scribe_Values.Look(ref autoLights, "PhobosVehicleSystems_AutoLights", true);
        Scribe_Values.Look(ref weatherGrip, "PhobosVehicleSystems_WeatherGrip", true);
        Scribe_Values.Look(ref thinAir, "PhobosVehicleSystems_ThinAir", true);
        Scribe_Values.Look(ref aircraftFuel, "PhobosVehicleSystems_AircraftFuel", true);
        Scribe_Values.Look(ref cruiseHeight, "PhobosVehicleSystems_CruiseHeight", true);
        Scribe_Values.Look(ref ammoCookOff, "PhobosVehicleSystems_AmmoCookOff", true);
        Scribe_Values.Look(ref raidersArriveByAir, "PhobosVehicleSystems_RaidersArriveByAir", true);
        Scribe_Values.Look(ref stockHelicoptersFly, "PhobosVehicleSystems_StockHelicoptersFly", false);
        Scribe_Values.Look(ref caravanCooking, "PhobosVehicleSystems_CaravanCooking", true);
        Scribe_Values.Look(ref jumpStartNeedsKit, "PhobosVehicleSystems_JumpStartNeedsKit", true);
        Scribe_Values.Look(ref assumeBoosterPack, "PhobosVehicleSystems_AssumeBoosterPack", false);
        Scribe_Values.Look(ref cabMoods, "PhobosVehicleSystems_CabMoods", true);
        Scribe_Values.Look(ref climateControlAll, "PhobosVehicleSystems_ClimateControlAll", false);
        Scribe_Values.Look(ref keepVehicleFrameworkHealthTab, "PhobosVehicleSystems_KeepVfHealthTab", false);
        Scribe_Values.Look(ref fleetBoardButton, "PhobosVehicleSystems_FleetBoardButton", true);
        Scribe_Values.Look(ref parkedWarningDays, "PhobosVehicleSystems_ParkedWarningDays", 3);
        Scribe_Values.Look(ref batteryFires, "PhobosVehicleSystems_BatteryFires", false);
        Scribe_Values.Look(ref electricLightning, "PhobosVehicleSystems_ElectricLightning", false);
        Scribe_Values.Look(ref tripLetters, "PhobosVehicleSystems_TripLetters", true);
        Scribe_Values.Look(ref exhaustIndoors, "PhobosVehicleSystems_ExhaustIndoors", false);
        Scribe_Values.Look(ref messageLevel, "PhobosVehicleSystems_MessageLevel", MessageLevel.All);
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
public class VehicleSystemsMod : SettingsBookMod
{
    public static readonly Phobos.Logging.PhobosLog Logger = Phobos.Framework.Logging.Logs.For("D'thorga Framework", "Vehicle systems");
    /// <summary>The finer logs for verbose logging (Framework 0.114.0; owner, 16 Nov 2026: a tick per part). Each writes only Debug lines
    /// so far: stage changes, breakdowns and cook-off; repairs and fittings done; hitching; sling loads.</summary>
    public static readonly Phobos.Logging.PhobosLog StagesLog = Phobos.Framework.Logging.Logs.For("D'thorga Framework", "Vehicle stages");
    public static readonly Phobos.Logging.PhobosLog RepairsLog = Phobos.Framework.Logging.Logs.For("D'thorga Framework", "Vehicle repairs and fittings");
    public static readonly Phobos.Logging.PhobosLog HitchLog = Phobos.Framework.Logging.Logs.For("D'thorga Framework", "Hitch and trailers");
    public static readonly Phobos.Logging.PhobosLog SlingLog = Phobos.Framework.Logging.Logs.For("D'thorga Framework", "Sling loads");
    /// <summary>The drive model (Framework 0.114.2; owner, 16 Nov 2026: the first gap to fill): what holds a vehicle back, its cap, the clutch.</summary>
    public static readonly Phobos.Logging.PhobosLog DriveLog = Phobos.Framework.Logging.Logs.For("D'thorga Framework", "Vehicle drive");

    /// <summary>The Harmony ID is the Framework's packageId with the part's name; never change it once published.</summary>
    public const string HarmonyId = "PhobosDthorga.Framework.VehicleSystems";
    public const string LogPrefix = "[D'thorga Framework]";

    public static VehicleSystemsSettings Settings { get; private set; } = new();

    public VehicleSystemsMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<VehicleSystemsSettings>();
        AddSettingsPages();
        // XML patches (PatchOperationPhobosFlag) run after every mod's constructor, so the flags are ready for them.
        PatchFlags.Register("PhobosVehicleSystems.electricLightning", () => Settings.electricLightning);
        HarmonyPatchApplier.ApplyEach(new Harmony(HarmonyId), typeof(VehicleSystemsMod).Assembly, LogPrefix);
        // Before the load line, which counts them (Framework 0.125.0, UH12).
        LongEventHandler.ExecuteWhenFinished(VehicleLoadOut.AddSwitches);
        LongEventHandler.ExecuteWhenFinished(LogLoaded);
        Phobos.Framework.Work.PhobosGameEvents.GameLoaded += VehicleSelfTest.AfterLoad;
    }

    static void LogLoaded()
    {
        var report = new LoadReport(LogPrefix, FrameworkMod.ModVersion, "vehicle systems");
        int systems = DefDatabase<PhobosVehicleSystemDef>.AllDefsListForReading.Count;
        int loadouts = DefDatabase<PhobosVehicleLoadoutDef>.AllDefsListForReading.Count;
        int fitted = VehicleSystems.Designs.Count(d => d.skippedBecause == null);
        report.On(systems + " systems, " + loadouts + " loadouts, " + fitted + " of " + VehicleSystems.Designs.Count + " vehicle designs fitted, "
                  + VehicleSystems.Designs.Sum(d => d.partsAdded) + " parts appended, "
                  + VehicleSystems.Designs.Sum(d => d.def.GetCompProperties<CompProperties_PhobosVehicleSystems>()?.weaponsTied ?? 0) + " weapons tied to a mount");
        if (Settings.showOnly) report.On("figures only (setting)");
        if (Settings.messageLevel != MessageLevel.All) report.On("vehicle messages nobody asked for: " + Settings.messageLevel + " (setting)");
        if (Settings.boilerExplosions) report.On("boiler explosions (setting)");
        if (Settings.realisticFeedWater) report.On("realistic feed water (setting)");
        if (Settings.nightHeater) report.On("night heater at camp (setting)");
        if (!Settings.lowOilCallsService) report.Off("low oil calling a service", "off (setting)");
        if (!Settings.accessoriesLoadEngine) report.Off("accessories loading the engine", "off (setting)");
        if (!Settings.roughGroundWear) report.Off("rough-ground wear", "off (setting)");
        if (!Settings.headlightBeams) report.Off("headlight beams", "off (setting)");
        if (!Settings.autoLights) report.Off("lights switching themselves", "off (setting)");
        if (!Settings.weatherGrip) report.Off("weather on grip", "off (setting)");
        if (!Settings.ammoCookOff) report.Off("ammunition cook-off", "off (setting)");
        else if (!CookOff.AmmoSystemOn) report.Off("ammunition cook-off", "Combat Extended's ammunition is not in use");
        if (!Settings.caravanCooking) report.Off("caravan cooking", "off (setting)");
        if (!Settings.raidersArriveByAir) report.Off("raiders arriving by aircraft", "off (setting)");
        if (Settings.stockHelicoptersFly) report.On("stock helicopters flying over the map (setting)");
        if (Settings.jumpStartNeedsKit) report.On("jump-start leads must be crafted (setting)");
        else report.Off("jump-start leads", "free (setting)");
        if (Settings.assumeBoosterPack) report.On("booster pack assumed on every vehicle (setting)");
        if (!Settings.cabMoods) report.Off("cab moods", "off (setting)");
        if (Settings.climateControlAll) report.On("climate control in every closed cab (setting)");
        if (Settings.keepVehicleFrameworkHealthTab) report.Off("the Health tab", "Vehicle Framework's kept (setting)");
        report.On("other factions' vehicles on the lite tier");
        report.On("\"Vehicles may load from here\" and \"Vehicles may unload here\" on " + VehicleLoadOut.SwitchesAdded + " storage types");
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
        if (Logger.DebugEnabled)
        {
            foreach (DesignRecord design in VehicleSystems.Designs)
            {
                Logger.Debug(design.def.defName + " (" + design.modName + "): " + (design.skippedBecause != null ? "no systems, " + design.skippedBecause
                    : "loadout " + design.loadout?.defName + ", " + design.partsAdded + " parts appended, "
                      + (design.def.GetCompProperties<CompProperties_PhobosVehicleSystems>()?.weaponsTied ?? 0) + " weapons tied to a mount"));
            }
        }
    }

    static readonly SettingsBook SettingsPages = SettingsBook.For("PhobosDthorga.Framework.VehicleSystems", () => "PhobosVehicleSettings_Book".Translate());
    /// <summary>Every setting at its default, for the pages' changed marks and their reset.</summary>
    static readonly VehicleSystemsSettings Defaults = new();
    static readonly FrameworkSettings FrameworkDefaults = new();
    protected override SettingsBook Book => SettingsPages;

    /// <summary>
    /// Its own entry in the mod settings list, "D'thorga Vehicle Systems", with a page per subject (owner's choices of 20 Nov 2026:
    /// pages with search, and an entry of its own; docs/development/settings-pages.md). Until then it was one scroll of some sixty rows.
    /// The display page also shows a Framework setting (vehicle sounds on other mods' vehicles), so the Framework's file is written too.
    /// </summary>
    void AddSettingsPages()
    {
        SettingsPages.Join(this).AlsoWrites(() => FrameworkMod.Instance)
            .Add(new SettingsPage("which", 0f, () => "PhobosVehicleSettings_PageWhich".Translate(), DrawWhich))
            .Add(new SettingsPage("systems", 10f, () => "PhobosVehicleSettings_PageSystems".Translate(), DrawSystems))
            .Add(new SettingsPage("engines", 20f, () => "PhobosVehicleSettings_PageEngines".Translate(), DrawEngines))
            .Add(new SettingsPage("aircraft", 30f, () => "PhobosVehicleSettings_PageAircraft".Translate(), DrawAircraft))
            .Add(new SettingsPage("steam", 40f, () => "PhobosVehicleSettings_PageSteamElectric".Translate(), DrawSteamElectric))
            .Add(new SettingsPage("dangers", 50f, () => "PhobosVehicleSettings_PageDangers".Translate(), DrawDangers))
            .Add(new SettingsPage("repairs", 60f, () => "PhobosVehicleSettings_PageRepairs".Translate(), DrawRepairs))
            .Add(new SettingsPage("crews", 70f, () => "PhobosVehicleSettings_PageCrews".Translate(), DrawCrews))
            .Add(new SettingsPage("display", 80f, () => "PhobosVehicleSettings_PageDisplay".Translate(), DrawDisplay))
            .Add(new SettingsPage("removing", 90f, () => "PhobosVehicleSettings_PageRemoving".Translate(), DrawRemoving));
    }

    static void DrawWhich(SettingsList list)
    {
        VehicleSystemsSettings s = Settings;
        list.Note("PhobosVehicleSystems_RestartNote".Translate());
        list.Radio("PhobosVehicleSettings_Scope".Translate(), ref s.scope, Defaults.scope,
            (VehicleSystemsSettings.Scope[])System.Enum.GetValues(typeof(VehicleSystemsSettings.Scope)), scope => ("PhobosVehicleSystems_Scope" + scope).Translate(),
            scope => ("PhobosVehicleSystems_Scope" + scope + "Desc").Translate(), restart: true);
        list.Section("PhobosVehicleSettings_SectionByMod".Translate());
        foreach (IGrouping<string, DesignRecord> mod in VehicleSystems.DesignsByMod().OrderBy(g => g.First().modName))
        {
            bool on = !s.disabledPackages.Contains(mod.Key);
            if (list.Check("PhobosVehicleSystems_ModVehicles".Translate(mod.First().modName.Named("MOD"), mod.Count().Named("COUNT")), ref on, true,
                    string.Join(", ", mod.Select(d => d.def.LabelCap.ToString())), restart: true))
            {
                Toggle(s.disabledPackages, mod.Key, on);
            }
        }
        list.Section("PhobosVehicleSettings_SectionByKind".Translate());
        foreach (string kind in DefDatabase<PhobosVehicleLoadoutDef>.AllDefsListForReading.Where(l => l.systems.Count > 0).Select(l => l.kind).Distinct().OrderBy(k => k))
        {
            bool on = !s.disabledKinds.Contains(kind);
            string label = ("PhobosVehicleSystems_Kind" + kind).TryTranslate(out TaggedString known) ? known.ToString() : kind;
            if (list.Check(label, ref on, true, restart: true)) Toggle(s.disabledKinds, kind, on);
        }
    }

    static void DrawSystems(SettingsList list)
    {
        VehicleSystemsSettings s = Settings;
        list.Check("PhobosVehicleSystems_SettingShowOnly".Translate(), ref s.showOnly, Defaults.showOnly, "PhobosVehicleSystems_SettingShowOnlyDesc".Translate());
        list.Section("PhobosVehicleSystems_SectionSystems".Translate());
        foreach (PhobosVehicleSystemDef system in DefDatabase<PhobosVehicleSystemDef>.AllDefsListForReading)
        {
            bool on = !s.disabledSystems.Contains(system.defName);
            if (list.Check(system.LabelCap, ref on, true, system.description)) Toggle(s.disabledSystems, system.defName, on);
        }
    }

    static void DrawEngines(SettingsList list)
    {
        VehicleSystemsSettings s = Settings, d = Defaults;
        list.Radio("PhobosVehicleSettings_Drivetrain".Translate(), ref s.drivetrainMode, d.drivetrainMode, (DrivetrainMode[])System.Enum.GetValues(typeof(DrivetrainMode)),
            mode => ("PhobosVehicleSystems_Drivetrain" + mode).Translate(), mode => ("PhobosVehicleSystems_Drivetrain" + mode + "Desc").Translate());
        list.Section("PhobosVehicleSettings_SectionFuel".Translate());
        list.Check("PhobosVehicleSystems_SettingLoadFuel".Translate(), ref s.loadDependentFuel, d.loadDependentFuel, "PhobosVehicleSystems_SettingLoadFuelDesc".Translate());
        list.Check("PhobosVehicleSystems_SettingAccessoryLoad".Translate(), ref s.accessoriesLoadEngine, d.accessoriesLoadEngine, "PhobosVehicleSystems_SettingAccessoryLoadDesc".Translate());
        list.Check("PhobosVehicleSystems_SettingFuelGelling".Translate(), ref s.fuelGelling, d.fuelGelling, "PhobosVehicleSystems_SettingFuelGellingDesc".Translate());
        list.Check("PhobosVehicleSystems_SettingThinAir".Translate(), ref s.thinAir, d.thinAir, "PhobosVehicleSystems_SettingThinAirDesc".Translate());
        list.Section("PhobosVehicleSettings_SectionGround".Translate());
        list.Check("PhobosVehicleSystems_SettingWeatherGrip".Translate(), ref s.weatherGrip, d.weatherGrip, "PhobosVehicleSystems_SettingWeatherGripDesc".Translate());
        list.Check("PhobosVehicleSystems_SettingRoughGround".Translate(), ref s.roughGroundWear, d.roughGroundWear, "PhobosVehicleSystems_SettingRoughGroundDesc".Translate());
        list.Check("PhobosHitch_SettingOthers".Translate(), ref s.hitchOthersPlainRule, d.hitchOthersPlainRule, "PhobosHitch_SettingOthersDesc".Translate());
        list.Section("PhobosVehicleSettings_SectionLights".Translate());
        list.Check("PhobosVehicleSystems_SettingHeadlightBeams".Translate(), ref s.headlightBeams, d.headlightBeams, "PhobosVehicleSystems_SettingHeadlightBeamsDesc".Translate());
        list.Check("PhobosVehicleSystems_SettingAutoLights".Translate(), ref s.autoLights, d.autoLights, "PhobosVehicleSystems_SettingAutoLightsDesc".Translate());
        list.Section("PhobosVehicleSystems_SectionJumpStarts".Translate());
        list.Check("PhobosVehicleSystems_SettingJumpStartNeedsKit".Translate(), ref s.jumpStartNeedsKit, d.jumpStartNeedsKit, "PhobosVehicleSystems_SettingJumpStartNeedsKitDesc".Translate());
        list.Check("PhobosVehicleSystems_SettingAssumeBoosterPack".Translate(), ref s.assumeBoosterPack, d.assumeBoosterPack, "PhobosVehicleSystems_SettingAssumeBoosterPackDesc".Translate());
    }

    static void DrawAircraft(SettingsList list)
    {
        VehicleSystemsSettings s = Settings, d = Defaults;
        list.Section("PhobosVehicleSettings_SectionFlight".Translate());
        list.Check("PhobosVehicleSystems_SettingAircraftFuel".Translate(), ref s.aircraftFuel, d.aircraftFuel, "PhobosVehicleSystems_SettingAircraftFuelDesc".Translate());
        list.Check("PhobosVehicleSystems_SettingCruiseHeight".Translate(), ref s.cruiseHeight, d.cruiseHeight, "PhobosVehicleSystems_SettingCruiseHeightDesc".Translate());
        list.Radio("PhobosVehicleSystems_SettingHoverLimit".Translate(), ref s.hoverLimit, d.hoverLimit,
            (VehicleSystemsSettings.HoverLimit[])System.Enum.GetValues(typeof(VehicleSystemsSettings.HoverLimit)),
            limit => ("PhobosVehicleSystems_Hover" + limit).Translate(), limit => ("PhobosVehicleSystems_Hover" + limit + "Desc").Translate());
        list.Check("PhobosVehicleSystems_SettingBlindFlightWarnOnly".Translate(), ref s.blindFlightWarnOnly, d.blindFlightWarnOnly, "PhobosVehicleSystems_SettingBlindFlightWarnOnlyDesc".Translate());
        list.Check("PhobosVehicleSystems_SettingAltitudeHypoxia".Translate(), ref s.altitudeHypoxia, d.altitudeHypoxia, "PhobosVehicleSystems_SettingAltitudeHypoxiaDesc".Translate());
        list.Check("PhobosVehicleSystems_SettingStockHelicopters".Translate(), ref s.stockHelicoptersFly, d.stockHelicoptersFly, "PhobosVehicleSystems_SettingStockHelicoptersDesc".Translate());
        if (ModsConfig.IsActive(DmseTieIn.PackageId))
        {
            list.Check("PhobosVehicleSystems_SettingAirDefenceSees".Translate(), ref s.airDefenceSeesAircraft, d.airDefenceSeesAircraft, "PhobosVehicleSystems_SettingAirDefenceSeesDesc".Translate());
        }
        list.Section("PhobosSling_Section".Translate());
        list.Check("PhobosSling_SettingHeat".Translate(), ref s.slingHeatDerating, d.slingHeatDerating, "PhobosSling_SettingHeatDesc".Translate());
        list.Check("PhobosSling_SettingCrew".Translate(), ref s.slingGroundCrew, d.slingGroundCrew, "PhobosSling_SettingCrewDesc".Translate());
        list.Check("PhobosSling_SettingWeather".Translate(), ref s.slingWeather, d.slingWeather, "PhobosSling_SettingWeatherDesc".Translate());
        list.Check("PhobosSling_SettingSlowFlight".Translate(), ref s.slingSlowFlight, d.slingSlowFlight, "PhobosSling_SettingSlowFlightDesc".Translate());
        list.Check("PhobosSling_SettingRelease".Translate(), ref s.slingRelease, d.slingRelease, "PhobosSling_SettingReleaseDesc".Translate());
    }

    static void DrawSteamElectric(SettingsList list)
    {
        VehicleSystemsSettings s = Settings, d = Defaults;
        list.Section("PhobosVehicleSystems_SectionSteam".Translate());
        list.Check("PhobosVehicleSystems_SettingRealisticFeedWater".Translate(), ref s.realisticFeedWater, d.realisticFeedWater, "PhobosVehicleSystems_SettingRealisticFeedWaterDesc".Translate());
        list.Check("PhobosVehicleSystems_SettingBoilerExplosions".Translate(), ref s.boilerExplosions, d.boilerExplosions, "PhobosVehicleSystems_SettingBoilerExplosionsDesc".Translate());
        list.Warning(s.boilerExplosions ? "PhobosVehicleSystems_BoilerExplosionsWarning".Translate().ToString() : null);
        list.Section("PhobosVehicleSystems_SectionElectric".Translate());
        list.Check("PhobosVehicleSystems_SettingBatteryFires".Translate(), ref s.batteryFires, d.batteryFires, "PhobosVehicleSystems_SettingBatteryFiresDesc".Translate());
        list.Warning(s.batteryFires ? "PhobosVehicleSystems_BatteryFiresWarning".Translate().ToString() : null);
        list.Check("PhobosVehicleSystems_SettingElectricLightning".Translate(), ref s.electricLightning, d.electricLightning,
            "PhobosVehicleSystems_SettingElectricLightningDesc".Translate(), restart: true);
    }

    static void DrawDangers(SettingsList list)
    {
        VehicleSystemsSettings s = Settings, d = Defaults;
        list.Check("PhobosVehicleSystems_SettingTankExplosions".Translate(), ref s.tankExplosions, d.tankExplosions, "PhobosVehicleSystems_SettingTankExplosionsDesc".Translate());
        list.Check("PhobosVehicleSystems_SettingAmmoCookOff".Translate(), ref s.ammoCookOff, d.ammoCookOff, "PhobosVehicleSystems_SettingAmmoCookOffDesc".Translate());
        list.Section("PhobosVehicleSettings_SectionOthers".Translate());
        list.Check("PhobosVehicleSystems_SettingRaidersFly".Translate(), ref s.raidersArriveByAir, d.raidersArriveByAir, "PhobosVehicleSystems_SettingRaidersFlyDesc".Translate());
        list.Check("PhobosVehicleSystems_SettingEnemyFitSets".Translate(), ref s.enemyFitSets, d.enemyFitSets, "PhobosVehicleSystems_SettingEnemyFitSetsDesc".Translate());
    }

    static void DrawRepairs(SettingsList list)
    {
        VehicleSystemsSettings s = Settings, d = Defaults;
        list.Section("PhobosVehicleSystems_SectionRepairs".Translate());
        list.Check("PhobosVehicleSystems_SettingDestroyedParts".Translate(), ref s.destroyedPartsNeedSupplies, d.destroyedPartsNeedSupplies, "PhobosVehicleSystems_SettingDestroyedPartsDesc".Translate());
        if (s.destroyedPartsNeedSupplies)
        {
            list.Indent();
            list.Slider("PhobosVehicleSystems_SettingWholePartShare".Translate(s.wholePartShare.ToStringPercent().Named("SHARE")), ref s.wholePartShare, d.wholePartShare, 0f, 1f,
                "PhobosVehicleSystems_SettingWholePartShareDesc".Translate());
            list.Slider("PhobosVehicleSystems_SettingOtherPartShare".Translate(s.otherPartShare.ToStringPercent().Named("SHARE")), ref s.otherPartShare, d.otherPartShare, 0f, 1f,
                "PhobosVehicleSystems_SettingOtherPartShareDesc".Translate());
            list.Outdent();
        }
        list.Check("PhobosVehicleSystems_SettingFieldFixes".Translate(), ref s.fieldFixes, d.fieldFixes, "PhobosVehicleSystems_SettingFieldFixesDesc".Translate());
        list.Section("PhobosVehicleSettings_SectionService".Translate());
        list.Check("PhobosVehicleSystems_SettingLowOilService".Translate(), ref s.lowOilCallsService, d.lowOilCallsService, "PhobosVehicleSystems_SettingLowOilServiceDesc".Translate());
        if (GameComponent_PhobosServiceKits.InGame)
        {
            // This colony's own choice, saved with it (S4): shown here for convenience, changed only through its service, never reset here.
            bool raw = GameComponent_PhobosServiceKits.RawAccepted;
            if (list.Check("PhobosVehicleSystems_SettingRawServices".Translate(), ref raw, null, "PhobosVehicleSystems_SettingRawServicesDesc".Translate()))
                GameComponent_PhobosServiceKits.SetRawAccepted(raw);
        }
        list.Section("PhobosVehicleSettings_SectionFittings".Translate());
        list.Check("PhobosVehicleSystems_SettingTuningNeedsRig".Translate(), ref s.tuningNeedsRig, d.tuningNeedsRig, "PhobosVehicleSystems_SettingTuningNeedsRigDesc".Translate());
        list.Check("PhobosVehicleSystems_SettingTankCompartments".Translate(), ref s.tankCompartmentsNeedUpgrades, d.tankCompartmentsNeedUpgrades, "PhobosVehicleSystems_SettingTankCompartmentsDesc".Translate());
        // What fitting and tuning cost (Framework 0.91.0): presets, not sliders (owner); a change re-prices work already waiting.
        list.Choice("PhobosVehicleSystems_SettingFitTime".Translate(), s.fitTimeFactor, d.fitTimeFactor, new[] { 0.25f, 0.5f, 1f, 2f }, FitTimeLabel,
            chosen => { Settings.fitTimeFactor = chosen; VehicleFittings.CostsChanged(); }, "PhobosVehicleSystems_SettingFitTimeDesc".Translate());
        list.Choice("PhobosVehicleSystems_SettingFitMaterials".Translate(), s.fitMaterialFactor, d.fitMaterialFactor, new[] { 0.4f, 1f, 1.5f }, FitMaterialLabel,
            chosen => { Settings.fitMaterialFactor = chosen; VehicleFittings.CostsChanged(); }, "PhobosVehicleSystems_SettingFitMaterialsDesc".Translate());
        if (list.Check("PhobosVehicleSystems_SettingFitByWeight".Translate(), ref s.fitCostScalesWithWeight, d.fitCostScalesWithWeight, "PhobosVehicleSystems_SettingFitByWeightDesc".Translate()))
            VehicleFittings.CostsChanged();
    }

    static void DrawCrews(SettingsList list)
    {
        VehicleSystemsSettings s = Settings, d = Defaults;
        list.Section("PhobosVehicleSettings_SectionCabs".Translate());
        list.Check("PhobosVehicleSystems_SettingCabMoods".Translate(), ref s.cabMoods, d.cabMoods, "PhobosVehicleSystems_SettingCabMoodsDesc".Translate());
        list.Check("PhobosVehicleSystems_SettingClimateAll".Translate(), ref s.climateControlAll, d.climateControlAll, "PhobosVehicleSystems_SettingClimateAllDesc".Translate());
        list.Section("PhobosVehicleSettings_SectionCamps".Translate());
        list.Check("PhobosVehicleSystems_SettingNightHeater".Translate(), ref s.nightHeater, d.nightHeater, "PhobosVehicleSystems_SettingNightHeaterDesc".Translate());
        list.Check("PhobosVehicleSystems_SettingCaravanCooking".Translate(), ref s.caravanCooking, d.caravanCooking, "PhobosVehicleSystems_SettingCaravanCookingDesc".Translate());
        list.SliderInt("PhobosVehicleSystems_SettingParkedWarning".Translate(s.parkedWarningDays.Named("DAYS")), ref s.parkedWarningDays, d.parkedWarningDays, 0, 14,
            "PhobosVehicleSettings_ParkedWarningDesc".Translate());
    }

    static void DrawDisplay(SettingsList list)
    {
        VehicleSystemsSettings s = Settings, d = Defaults;
        SettingsUi.DisplayModeRow(list, ref s.displayMode);
        list.Check("PhobosVehicleSystems_SettingKeepVfHealthTab".Translate(), ref s.keepVehicleFrameworkHealthTab, d.keepVehicleFrameworkHealthTab,
            "PhobosVehicleSystems_SettingKeepVfHealthTabDesc".Translate(), restart: true);
        list.Check("PhobosVehicleSystems_SettingFleetButton".Translate(), ref s.fleetBoardButton, d.fleetBoardButton, "PhobosVehicleSystems_SettingFleetButtonDesc".Translate());
        list.Check("PhobosVehicleSystems_SettingPowerPointLarge".Translate(), ref s.powerPointLargeSocket, d.powerPointLargeSocket, "PhobosVehicleSystems_SettingPowerPointLargeDesc".Translate());
        list.Section("PhobosFramework_SectionSound".Translate());
        list.Check("PhobosFramework_SettingSystemSoundsOnOtherVehicles".Translate(), ref FrameworkMod.Settings.systemSoundsOnOtherVehicles,
            FrameworkDefaults.systemSoundsOnOtherVehicles, "PhobosFramework_SettingSystemSoundsOnOtherVehiclesDesc".Translate());
        // The vehicle message level (Framework 0.186.0): only messages nobody asked for; replies and letters always show.
        list.Section("PhobosVehicleSettings_SectionMessages".Translate());
        list.Radio("PhobosVehicleSettings_MessageLevel".Translate(), ref s.messageLevel, d.messageLevel,
            (MessageLevel[])System.Enum.GetValues(typeof(MessageLevel)), level => ("PhobosVehicleSettings_MessageLevel" + level).Translate(),
            level => ("PhobosVehicleSettings_MessageLevel" + level + "Desc").Translate(), "PhobosVehicleSettings_MessageLevelDesc".Translate());
        // Stories (Framework 0.181.0; docs/development/vehicle-emergent-play.md): what a vehicle's life leaves for the player to read.
        list.Section("PhobosVehicleSystems_SectionStories".Translate());
        list.Check("PhobosVehicleSystems_SettingTripLetters".Translate(), ref s.tripLetters, d.tripLetters, "PhobosVehicleSystems_SettingTripLettersDesc".Translate());
        list.Check("PhobosVehicleSystems_SettingExhaustIndoors".Translate(), ref s.exhaustIndoors, d.exhaustIndoors, "PhobosVehicleSystems_SettingExhaustIndoorsDesc".Translate());
    }

    static void DrawRemoving(SettingsList list)
    {
        list.Note("PhobosVehicleSystems_RemoveExplain".Translate());
        if (VehicleSystems.Swept) list.Warning("PhobosVehicleSystems_RemoveDone".Translate().ToString());
        else if (Current.Game != null && list.Button("PhobosVehicleSystems_RemoveButton".Translate()))
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("PhobosVehicleSystems_RemoveConfirm".Translate(), Sweep, destructive: true));
    }

    static string FitTimeLabel(float factor) => factor switch
    {
        <= 0.25f => "PhobosVehicleSystems_FitTimeQuick".Translate().Resolve(),
        <= 0.5f => "PhobosVehicleSystems_FitTimeShorter".Translate().Resolve(),
        <= 1f => "PhobosVehicleSystems_FitTimeStandard".Translate().Resolve(),
        _ => "PhobosVehicleSystems_FitTimeLong".Translate().Resolve(),
    };

    static string FitMaterialLabel(float factor) => factor switch
    {
        <= 0.4f => "PhobosVehicleSystems_FitMaterialsLight".Translate().Resolve(),
        <= 1f => "PhobosVehicleSystems_FitMaterialsStandard".Translate().Resolve(),
        _ => "PhobosVehicleSystems_FitMaterialsHeavy".Translate().Resolve(),
    };

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
    public static readonly Texture2D Fittings = Load("Phobos/VehicleSystems/Fittings");
    static readonly Dictionary<string, Texture2D> byPath = new();

    static Texture2D Load(string path) => ContentFinder<Texture2D>.Get(path, reportFailure: false) ?? BaseContent.WhiteTex;

    /// <summary>A picture if it exists, else null, for what falls back to words instead of an icon.</summary>
    public static Texture2D? IfExists(string? path) => path.NullOrEmpty() ? null : ContentFinder<Texture2D>.Get(path, reportFailure: false);

    static readonly Dictionary<string, Texture2D?> ifExists = new();

    /// <summary><see cref="IfExists"/>, looked up once per path: for pictures drawn every frame.</summary>
    public static Texture2D? IfExistsCached(string? path)
    {
        if (path.NullOrEmpty()) return null;
        if (!ifExists.TryGetValue(path!, out Texture2D? texture)) ifExists[path!] = texture = IfExists(path);
        return texture;
    }

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
