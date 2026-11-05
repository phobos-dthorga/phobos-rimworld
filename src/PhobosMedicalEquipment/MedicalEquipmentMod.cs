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

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref displayMode, "PhobosMedical_DisplayMode", DisplayMode.Plain);
    }
}

/// <summary>
/// D'thorga Medical Equipment: hospital and field kit that gives More Injuries and D'thorga Burns and Smoke's conditions something to
/// be treated with. The first pieces are bedside facilities that give oxygen, on the pattern of the game's vitals monitor. No Harmony.
/// </summary>
public class MedicalEquipmentMod : Mod
{
    public const string PackageId = "PhobosDthorga.MedicalEquipment";
    public const string ModVersion = "0.2.0";
    public const string LogPrefix = "[D'thorga Medical Equipment]";
    const string VanillaGravshipExpanded = "vanillaexpanded.gravship";

    static ModContentPack? pack;

    public static MedicalEquipmentSettings Settings { get; private set; } = new();

    public MedicalEquipmentMod(ModContentPack content) : base(content)
    {
        pack = content;
        Settings = GetSettings<MedicalEquipmentSettings>();
        LongEventHandler.ExecuteWhenFinished(LogLoaded);
    }

    static void LogLoaded()
    {
        var report = new LoadReport(LogPrefix, ModVersion);
        report.On("oxygen concentrator");
        report.On("burn unit bed");
        const string stand = "bedside oxygen stand";
        if (OptionalMods.IsActive(VanillaGravshipExpanded)) report.On(stand); else report.Off(stand, "needs Vanilla Gravship Expanded's oxygen");
        report.Log();
    }

    public override string SettingsCategory() => "PhobosMedical_SettingsCategory".Translate();

    public override void DoSettingsWindowContents(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        SettingsUi.DrawDisplayMode(listing, ref Settings.displayMode);
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
