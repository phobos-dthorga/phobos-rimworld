using LudeonTK;
using Phobos.Framework;
using Phobos.Framework.Debug;
using Phobos.Framework.Defs;
using Phobos.Medical.Defs;
using Phobos.Medical.Services;
using RimWorld;
using Verse;

namespace Phobos.Medical;

/// <summary>
/// Player preferences. Balance lives in <see cref="PhobosMedicalRulesDef"/> and on each machine's comp. The saved keys never change.
/// The display mode here is the whole mod's (<see cref="MedicalMod.DisplayMode"/>): its key was Medical Equipment's.
/// </summary>
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
/// D'thorga Medical's equipment part (D'thorga Medical Equipment until 7 November 2026): hospital and field kit that gives More
/// Injuries' and the burns part's conditions something to be treated with. Three Harmony postfixes, each with its reason on its
/// class (Patches/MedicalPatches.cs), applied with the rest of the mod's patches by <see cref="MedicalMod"/>: an unpowered liquid
/// store takes in nothing, no surgery in a pressure chamber, and the one-time letter recommending Vanilla Gravship Expanded.
/// </summary>
public static class MedicalEquipmentMod
{
    public const string LogPrefix = "[D'thorga Medical: equipment]";

    public static MedicalEquipmentSettings Settings => MedicalMod.Settings.equipment;

    internal static void Start() => Phobos.Framework.Health.SurgeryBars.Register(ChamberService.SurgeryBarReason);

    internal static LoadReport Report(LoadReport report)
    {
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
        return report;
    }

    internal static void DrawSettings(Listing_Standard listing) =>
        listing.Label(RecommendationService.GravshipActive ? "PhobosMedical_SettingGravshipOn".Translate() : "PhobosMedical_SettingGravshipOff".Translate());

    [DebugAction(DebugCategory.Name, "Log a pawn's oxygen need", actionType = DebugActionType.ToolMapForPawns)]
    static void LogOxygenNeed(Pawn pawn)
    {
        var rules = PhobosRulesDef.Current<PhobosMedicalRulesDef>();
        Log.Message($"{LogPrefix} {pawn.LabelShort}: breathing {pawn.health.capacities.GetLevel(PawnCapacityDefOf.Breathing):0.00}, needs oxygen {OxygenService.NeedsOxygen(pawn, rules)}, " +
                    $"in bed {pawn.InBed()}, carbon monoxide half-life on oxygen {Phobos.Framework.Breathing.PhobosBreathing.SuppliedCarbonMonoxideHalfLifeHours(pawn)?.ToString("0.00") ?? "not on oxygen"}");
    }
}
