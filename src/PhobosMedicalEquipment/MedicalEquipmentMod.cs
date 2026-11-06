using LudeonTK;
using Phobos.Framework;
using Phobos.Framework.Debug;
using Phobos.Framework.Defs;
using Phobos.Framework.Uninstall;
using Phobos.Medical.Defs;
using Phobos.Medical.Services;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.Medical;

/// <summary>Player preferences. Balance lives in <see cref="PhobosMedicalRulesDef"/> and on each machine's comp. The saved keys never change.</summary>
public class MedicalEquipmentSettings : ModSettings
{
    public DisplayMode displayMode = DisplayMode.Plain;
    /// <summary>The one-time letter recommending Vanilla Gravship Expanded has been shown on this install.</summary>
    public bool gravshipLetterShown;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref displayMode, "PhobosMedical_DisplayMode", DisplayMode.Plain);
        Scribe_Values.Look(ref gravshipLetterShown, "PhobosMedical_GravshipLetterShown", false);
    }
}

/// <summary>
/// D'thorga Medical Equipment: hospital and field kit that gives More Injuries and D'thorga Burns and Smoke's conditions something to
/// be treated with. Three Harmony postfixes, each with its reason on its class (Patches/MedicalPatches.cs): an unpowered liquid store
/// takes in nothing, no surgery in a pressure chamber, and the one-time letter recommending Vanilla Gravship Expanded.
/// </summary>
public class MedicalEquipmentMod : Mod
{
    public const string PackageId = "PhobosDthorga.MedicalEquipment";
    public const string ModVersion = "0.4.2";
    public const string LogPrefix = "[D'thorga Medical Equipment]";
    public const string HarmonyId = PackageId;

    static ModContentPack? pack;

    public static MedicalEquipmentSettings Settings { get; private set; } = new();

    public MedicalEquipmentMod(ModContentPack content) : base(content)
    {
        pack = content;
        Settings = GetSettings<MedicalEquipmentSettings>();
        Phobos.Shared.HarmonyPatchApplier.ApplyEach(new HarmonyLib.Harmony(HarmonyId), typeof(MedicalEquipmentMod).Assembly, LogPrefix);
        Phobos.Framework.Health.SurgeryBars.Register(ChamberService.SurgeryBarReason);
        LongEventHandler.ExecuteWhenFinished(LogLoaded);
    }

    static void LogLoaded()
    {
        var report = new LoadReport(LogPrefix, ModVersion);
        report.On("oxygen concentrator");
        report.On("burn unit bed");
        report.On("silver burn dressing");
        report.On("manual resuscitator");
        // Everything that stores or pipes oxygen uses Vanilla Gravship Expanded's oxygen, so it exists only with that mod.
        bool gravship = RecommendationService.GravshipActive;
        foreach (string piped in new[] { "bedside oxygen stand", "field oxygen kit", "hyperbaric chamber", "oxygen electrolyser", "liquid oxygen store" })
        {
            if (gravship) report.On(piped); else report.Off(piped, "needs Vanilla Gravship Expanded's oxygen");
        }
        report.Log();
    }

    public override string SettingsCategory() => "PhobosMedical_SettingsCategory".Translate();

    public override void DoSettingsWindowContents(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        SettingsUi.DrawDisplayMode(listing, ref Settings.displayMode);
        listing.Gap();
        listing.Label(RecommendationService.GravshipActive ? "PhobosMedical_SettingGravshipOn".Translate() : "PhobosMedical_SettingGravshipOff".Translate());
        listing.Gap();
        if (listing.ButtonText("PhobosMedical_SettingRemoveAll".Translate(), "PhobosMedical_SettingRemoveAllDesc".Translate())) RemoveEveryCondition();
        listing.End();
    }

    static void RemoveEveryCondition()
    {
        if (Current.Game == null || pack == null)
        {
            Messages.Message("PhobosMedical_RemoveNeedsGame".Translate(), MessageTypeDefOf.RejectInput, false);
            return;
        }
        int removed = CleanupService.RemoveHediffsOf(pack);
        Messages.Message("PhobosMedical_RemoveDone".Translate(removed.Named("COUNT")), MessageTypeDefOf.TaskCompletion, false);
    }

    [DebugAction(DebugCategory.Name, "Log a pawn's oxygen need", actionType = DebugActionType.ToolMapForPawns)]
    static void LogOxygenNeed(Pawn pawn)
    {
        var rules = PhobosRulesDef.Current<PhobosMedicalRulesDef>();
        Log.Message($"{LogPrefix} {pawn.LabelShort}: breathing {pawn.health.capacities.GetLevel(PawnCapacityDefOf.Breathing):0.00}, needs oxygen {OxygenService.NeedsOxygen(pawn, rules)}, " +
                    $"in bed {pawn.InBed()}, carbon monoxide half-life on oxygen {Phobos.Framework.Breathing.PhobosBreathing.SuppliedCarbonMonoxideHalfLifeHours(pawn)?.ToString("0.00") ?? "not on oxygen"}");
    }
}
