using System.Collections.Generic;
using Phobos.Dosing.Defs;
using Phobos.Dosing.Rules;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Dosing.Services;

/// <summary>
/// Protection while exposed (Medical 0.30.0; docs/development/medical-care-needs.md, slice D): whether a person is taking in toxins
/// now or is caught in vacuum, read from the same places the game deals the harm from, and the jobs for someone on their feet to take
/// their own. Every check reads the person's own cell and conditions, never the map, so it costs a few comparisons for anyone not
/// exposed. Nothing here mutates game state.
/// </summary>
public static class ExposureService
{
    /// <summary>The game's toxic buildup has begun and its source is still around this person.</summary>
    public static bool ToxicNow(Pawn pawn, PhobosDosingRulesDef rules)
    {
        if (!pawn.Spawned || pawn.Map == null || !pawn.RaceProps.IsFlesh) return false;
        float buildup = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.ToxicBuildup)?.Severity ?? 0f;
        if (buildup < rules.toxicExposureFrom) return false;
        if (!ToxicSourceNow(pawn)) return false;
        float intake = ExposureRule.IntakeFactor(pawn.GetStatValue(StatDefOf.ToxicResistance, true, 60), pawn.GetStatValue(StatDefOf.ToxicEnvironmentResistance, true, 60));
        return ExposureRule.ToxicUnderWay(buildup, rules.toxicExposureFrom, true, intake);
    }

    /// <summary>
    /// The sources the game deals toxic buildup from, at this person's cell: polluted ground and ground that builds toxins by itself
    /// (ToxicUtility.PawnToxicTickInterval), tox gas (GasUtility.PawnGasEffectsTickInterval), and open sky under toxic fallout or toxic
    /// weather (ToxicUtility.DoAirbornePawnToxicDamage).
    /// </summary>
    static bool ToxicSourceNow(Pawn pawn)
    {
        Map map = pawn.Map;
        IntVec3 cell = pawn.Position;
        if (cell.GetTerrain(map).toxicBuildupFactor > 0f) return true;
        if (ModsConfig.BiotechActive && (cell.IsPolluted(map) || cell.GasDensity(map, GasType.ToxGas) > 0)) return true;
        if (cell.Roofed(map)) return false;
        if (map.weatherManager.curWeather?.doToxicBuildup == true) return true;
        List<GameCondition> conditions = map.gameConditionManager.ActiveConditions;
        for (int i = 0; i < conditions.Count; i++)
        {
            if (conditions[i] is GameCondition_ToxicFallout) return true;
        }
        return false;
    }

    /// <summary>Standing where vacuum harms this person, with less resistance than keeps it out: the game's own figures.</summary>
    public static bool CaughtInVacuum(Pawn pawn)
    {
        if (!ModsConfig.OdysseyActive || !pawn.Spawned || pawn.Map == null || !pawn.Map.Biome.inVacuum) return false;
        return ExposureRule.CaughtInVacuum(pawn.Position.GetVacuum(pawn.Map), pawn.GetStatValue(StatDefOf.VacuumResistance, true, 60));
    }

    static readonly CareContext ToxicCare = new(CareSeriousness.Minor, null, true);
    static readonly CareContext VacuumCare = new(CareSeriousness.LifeThreatening);

    /// <summary>
    /// The game's own ingest job for what someone on their feet would take against toxins now, or null. Asked for every humanlike
    /// choosing a job, after a painkiller and a preventive, so the cheapest tests come first.
    /// </summary>
    public static Job? SelfToxicJob(Pawn pawn)
    {
        DosingSettings settings = DosingMod.Settings;
        if (!settings.enabled || !CareModeRule.SelfMay(settings.toxicMode) || DoseIndex.ToxicOptions.Count == 0) return null;
        if (!DoseService.MayTakeOwnPreventive(pawn, settings) || !DoseService.IsEligible(pawn, settings)) return null;
        if (!ToxicNow(pawn, PhobosDosingRulesDef.Current)) return null;
        if (DoseService.SelfCareDecide(pawn, DoseNeed.Toxic, DoseIndex.ToxicOptions, ToxicCare, false).Chosen is not DoseVerdict chosen || chosen.Item == null) return null;
        if (Medical.Seen.Wants("medical.toxic-self", pawn)) Medical.Seen.Note("medical.toxic-self", pawn, chosen.Option.Def.label);
        return DrugAIUtility.IngestAndTakeToInventoryJob(chosen.Item, pawn, 1);
    }

    /// <summary>
    /// The job for someone caught in vacuum to take a vacuum-resisting dose they carry, or null. Asked from the constant think tree,
    /// ahead of the game's search for air, for every humanlike every half second or so: the first test is whether the map is in
    /// vacuum at all. Nobody drafted, down, raging, in their own care or under a direct order is interrupted.
    /// </summary>
    public static Job? VacuumJob(Pawn pawn)
    {
        Map? map = pawn.Map;
        if (map == null || !map.Biome.inVacuum) return null;
        DosingSettings settings = DosingMod.Settings;
        if (!settings.enabled || !settings.vacuumRescue || DoseIndex.VacuumOptions.Count == 0) return null;
        if (pawn.inventory == null || pawn.inventory.innerContainer.Count == 0) return null;
        if (pawn.Drafted || pawn.Downed || pawn.InMentalState || !pawn.Awake() || PawnUtility.PlayerForcedJobNowOrSoon(pawn)) return null;
        if (pawn.CurJobDef == JobDefOf.Ingest) return null;
        if (pawn.DevelopmentalStage != DevelopmentalStage.Adult || !DoseService.MaySelfMedicate(pawn, settings) || !DoseService.IsEligible(pawn, settings)) return null;
        if (!CaughtInVacuum(pawn)) return null;
        if (DoseService.SelfCareDecide(pawn, DoseNeed.Vacuum, DoseIndex.VacuumOptions, VacuumCare, true).Chosen is not DoseVerdict chosen || chosen.Item == null) return null;
        if (Medical.Seen.Wants("medical.vacuum-self", pawn)) Medical.Seen.Note("medical.vacuum-self", pawn, chosen.Option.Def.label);
        // The game's own job for a drug a pawn carries, as its drug-policy giver uses for one in inventory.
        return DrugAIUtility.IngestAndTakeToInventoryJob(chosen.Item, pawn, 1);
    }
}
