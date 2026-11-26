using System.Reflection;
using HarmonyLib;
using Phobos.Framework;
using Phobos.Framework.Patching;
using Phobos.Shared;
using Phobos.VehicleSystems;
using Vehicles;
using Verse;

namespace Phobos.ProwlerPatches;

/// <summary>Player preferences for the Prowler part. The saved key never changes.</summary>
public class ProwlerPatchesSettings : ModSettings
{
    /// <summary>The Prowler's speeds set by the real EA-6B's on the Harrier's scale (off: the author's). Read once, by the XML patch.</summary>
    public bool realisticHandling;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref realisticHandling, "PhobosProwlerPatches_RealisticHandling", false);
    }
}

/// <summary>
/// D'thorga Patches' Prowler part (from 0.33.0; owner, 22 Nov 2026: the A-10's treatment, with the ICAP upgrades), switched on only while the
/// EA-6B Prowler is active (this assembly lives in its load folder). Its parts list, parts bill and cost are XML; this registers realistic
/// handling, off by default (owner's choice), as a patch flag in the Mod constructor, which the game runs before it applies any patch, and
/// applies the three patches below. Today the author's speeds already match the real jet's with its pods, so the setting holds them there
/// should a later update of his change them (owner, 22 Nov 2026: kept so nothing looks missing beside the A-10's).
/// </summary>
public class ProwlerPatchesMod : SettingsBookMod
{
    /// <summary>The Harmony ID is the packageId, shared by every part; never change it once the mod is published.</summary>
    public const string PackageId = "PhobosDthorga.Patches";
    public const string ModVersion = "0.45.1";
    public const string LogPrefix = "[D'thorga Patches]";
    public const string RealisticHandlingFlag = "PhobosProwlerPatches.realisticHandling";

    public static ProwlerPatchesSettings Settings { get; private set; } = new();

    public ProwlerPatchesMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<ProwlerPatchesSettings>();
        PatchFlags.Register(RealisticHandlingFlag, () => Settings.realisticHandling);
        AddSettingsPages();
        HarmonyPatchApplier.ApplyEach(new Harmony(PackageId), typeof(ProwlerPatchesMod).Assembly, LogPrefix);
        LongEventHandler.ExecuteWhenFinished(ProwlerSalvo.Register);
        LongEventHandler.ExecuteWhenFinished(ProwlerJamFields.Register);
        LongEventHandler.ExecuteWhenFinished(LogLoaded);
    }

    static void LogLoaded()
    {
        var report = new LoadReport(LogPrefix, ModVersion, "EA-6B Prowler");
        report.On("the Prowler's fuller parts list and its parts bill, its engines turbines");
        if (ProwlerEvasionPatch.Found) report.On("its dodge follows its flight controls and warning receiver, with a chaff and flares button");
        else report.Off("its dodge following its flight controls", "its code has changed");
        if (ProwlerJamFields.Found) report.On("its jamming fields blind other factions' vehicles' radar and warning receivers");
        else report.Off("its jamming fields blinding other vehicles' radar", "its code has changed");
        if (ProwlerJamReachPatch.Found) report.On("the ICAP upgrades widen its jamming field");
        else report.Off("the ICAP upgrades widening its jamming field", "its code has changed");
        const string handling = "realistic handling (the Prowler's speeds by the real EA-6B's)";
        if (Settings.realisticHandling) report.On(handling);
        else report.Off(handling, "its setting is off, the default");
        report.Log();
    }

    static readonly SettingsBook SettingsPages = SettingsBook.For("PhobosDthorga.Patches", () => "PhobosPatches_SettingsBook".Translate());
    static readonly ProwlerPatchesSettings Defaults = new();
    protected override SettingsBook Book => SettingsPages;

    void AddSettingsPages() =>
        SettingsPages.Join(this).Add(new Phobos.Framework.SettingsPage("prowler", 22f, () => "PhobosProwlerPatches_Page".Translate(), DrawSettings));

    static void DrawSettings(SettingsList list)
    {
        ProwlerPatchesSettings s = Settings, d = Defaults;
        list.Section("PhobosProwlerPatches_SectionHandling".Translate());
        list.Check("PhobosProwlerPatches_SettingRealisticHandling".Translate(), ref s.realisticHandling, d.realisticHandling,
            "PhobosProwlerPatches_SettingRealisticHandlingDesc".Translate());
        list.Note("PhobosProwlerPatches_NeedsRestart".Translate());
    }
}

/// <summary>
/// The Prowler's dodge chance follows its flight controls and warning receiver, and a salvo of chaff and flares adds to it for a few seconds,
/// as the Harrier's does (Patches 0.21.0). The Prowler asks its static Evasion.ChanceFor(target, launcher) on both its own and its Combat
/// Extended paths (read from its source of 19 Nov 2026), so one postfix covers both. A sound jet with a working receiver dodges exactly as
/// its author set; D'thorga Framework's air-combat service does the sums. A postfix: the author's figure is always worked out first.
/// </summary>
[HarmonyPatch]
public static class ProwlerEvasionPatch
{
    public static bool Found => TargetMethod() != null;

    static bool Prepare() => TargetMethod() != null;

    static MethodBase? TargetMethod() => AccessTools.Method("EA6BProwler.Evasion:ChanceFor");

    public static void Postfix(Thing target, ref float __result)
    {
        if (__result <= 0f || target is not VehiclePawn vehicle) return;
        __result = AirCombat.Evasion(vehicle, __result);
    }
}

/// <summary>Which vehicles are Prowlers, for the salvo button: the Prowler marks its Def with its extension, which Prowler.Props returns.</summary>
public static class ProwlerSalvo
{
    static MethodInfo? props;

    public static void Register()
    {
        props = AccessTools.Method("EA6BProwler.Prowler:Props");
        if (props == null) return;
        AirCombat.RegisterMapSalvo(IsProwler);
    }

    static bool IsProwler(VehiclePawn vehicle) => props != null && props.Invoke(null, new object[] { vehicle }) != null;
}

/// <summary>
/// The ICAP upgrades widen the Prowler's jamming field (owner, 22 Nov 2026; docs/development/ea6b-prowler.md): its own mod works the field's
/// radius in Jammer.EffectiveRadius (from its loadout, its damage and its operators), and every use of the field reads it there, so a
/// postfix multiplies it by the vehicle's fittings' jamming factor (VehicleFittings.JamRadiusFactor: 1 with none fitted, so a stock jet is
/// untouched). It only ever widens or keeps the author's figure, never skips it.
/// </summary>
[HarmonyPatch]
public static class ProwlerJamReachPatch
{
    public static bool Found => TargetMethod() != null;

    static bool Prepare() => TargetMethod() != null;

    static MethodBase? TargetMethod() => AccessTools.Method("EA6BProwler.Jammer:EffectiveRadius");

    public static void Postfix(VehiclePawn vehicle, ref float __result)
    {
        if (__result <= 0f || vehicle?.GetComp<CompPhobosVehicleSystems>() is not { Usable: true } systems) return;
        __result *= systems.Fittings.JamRadiusFactor;
    }
}
