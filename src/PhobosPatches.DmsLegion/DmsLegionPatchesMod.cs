using System.Reflection;
using HarmonyLib;
using Phobos.Framework;
using Phobos.Framework.Health;
using Phobos.Shared;
using Verse;

namespace Phobos.DmsLegionPatches;

public class DmsLegionPatchesSettings : ModSettings
{
    /// <summary>Legion's nuclear strike gives D'thorga's radiation by distance and shelter in place of its own corrosion on everyone on the map
    /// (Patches 0.48.0; the owner's choice of 27 Nov 2026, "replace, with a setting"). On by default: it lowers the risk (graded low).</summary>
    public bool radiationByDistance = true;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref radiationByDistance, "PhobosDmsLegionPatches_RadiationByDistance", true);
    }
}

/// <summary>
/// D'thorga Patches' The Dead Man's Switch - Legion part (0.48.0), loaded only while Legion is active. It holds one behaviour, its nuclear
/// strike's radiation (<see cref="Patch_LegionAddHediff_RadiationBurst"/>), and reaches Legion by name, referencing no assembly of it. One
/// setting; no save data.
/// </summary>
public class DmsLegionPatchesMod : SettingsBookMod
{
    /// <summary>The Harmony ID is the packageId, shared by every part; never change it once the mod is published.</summary>
    public const string PackageId = "PhobosDthorga.Patches";
    public const string ModVersion = "0.51.0";
    public const string LogPrefix = "[D'thorga Patches]";
    public static readonly Phobos.Logging.PhobosLog Logger = Phobos.Framework.Logging.Logs.For("D'thorga Patches", "DMS Legion");
    const string Feature = "Legion's nuclear strike: radiation by distance and shelter in place of its corrosion on everyone";

    static readonly SettingsBook SettingsPages = SettingsBook.For("PhobosDthorga.Patches", () => "PhobosPatches_SettingsBook".Translate());
    static readonly DmsLegionPatchesSettings Defaults = new();
    protected override SettingsBook Book => SettingsPages;

    public static DmsLegionPatchesSettings Settings { get; private set; } = new();

    public DmsLegionPatchesMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<DmsLegionPatchesSettings>();
        SettingsPages.Join(this).Add(new SettingsPage("dms-legion", 60f, () => "PhobosDmsLegionPatches_Page".Translate(), DrawSettings));
        HarmonyPatchApplier.ApplyEach(new Harmony(PackageId), typeof(DmsLegionPatchesMod).Assembly, LogPrefix);
        LongEventHandler.ExecuteWhenFinished(LogLoaded);
    }

    static void LogLoaded()
    {
        var report = new LoadReport(LogPrefix, ModVersion, "DMS Legion");
        if (!Patch_LegionAddHediff_RadiationBurst.Prepare()) report.Off(Feature, "Legion's code has changed");
        else if (!Settings.radiationByDistance) report.Off(Feature, "its setting is off, as the player chose");
        else report.On(Feature);
        report.Log();
    }

    static void DrawSettings(SettingsList list)
    {
        DmsLegionPatchesSettings s = Settings;
        list.Section("PhobosDmsLegionPatches_SectionRadiation".Translate());
        list.Check("PhobosDmsLegionPatches_SettingRadiationByDistance".Translate(), ref s.radiationByDistance, Defaults.radiationByDistance,
            "PhobosDmsLegionPatches_SettingRadiationByDistanceDesc".Translate());
        if (!RadiationDoses.Active) list.Note("PhobosDmsLegionPatches_NeedsModel".Translate());
    }
}

/// <summary>
/// Legion's nuclear strike ends by giving everyone on the map its "radiation corrosion" at 0.9, which grows 25 a day and kills at 1: everyone
/// dies within seconds, deep bunkers included (read from Legion's 1.7 code and Defs, 27 Nov 2026). For a support type carrying D'thorga
/// Framework's PhobosRadiationBurst (its nuclear strike, by XML), this gives that burst at the target instead: a dose by distance, cut by
/// roofs, stone, rock and vehicle hulls, through D'thorga Medical's model. A prefix that skips the original (recorded reason, AGENTS.md): the
/// original is the corrosion itself, one static method with no extension point. Legion's own runs while the setting is off, while the model
/// is not on, and for any other hediff or support type. Its blast and heat rings and its toxic fallout stay Legion's.
/// </summary>
[HarmonyPatch]
public static class Patch_LegionAddHediff_RadiationBurst
{
    const string Corrosion = "DMSL_Hediff_RadiationCorrosion";
    static readonly FieldInfo? HediffName =
        AccessTools.Field("DMS_Legion.GroundSupport.SupportEffects.CompProperties_AerialSupportEffect_AddHediff:hediffDefName");

    public static bool Prepare() => TargetMethod() != null && HediffName != null;

    public static MethodBase? TargetMethod() =>
        AccessTools.Method("DMS_Legion.GroundSupport.SupportEffects.CompAerialSupportEffect_AddHediff:ExecuteEffect");

    public static bool Prefix(IntVec3 targetPos, object supportType, Map map, object props)
    {
        if (!DmsLegionPatchesMod.Settings.radiationByDistance || map == null || supportType is not Def def) return true;
        if (RadiationBursts.Of(def) is not { } burst || !RadiationBursts.On(burst.kind)) return true;
        if (HediffName?.GetValue(props) as string != Corrosion) return true;
        int dosed = RadiationBursts.Burst(map, targetPos, burst, 0f, def.defName);
        DmsLegionPatchesMod.Logger.Info("Legion's " + def.defName + ": our radiation by distance in place of its corrosion on everyone; " + dosed + " dosed.");
        return false;
    }
}
