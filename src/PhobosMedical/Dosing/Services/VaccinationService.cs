using System.Collections.Generic;
using Phobos.Dosing.Defs;
using Phobos.Dosing.Rules;
using Phobos.Framework.Health;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Dosing.Services;

/// <summary>
/// Vaccination (Medical 0.35.0; docs/development/medical-care-round-two.md, slice 3): someone without a vaccine's condition is given
/// that vaccine when one is on the map, by a doctor if they lie down, or takes their own at the think-tree hook painkillers and
/// preventives use. A vaccine lasts a year, so each person is done about once a year. Keeps the preventives' reserve. Nothing here
/// changes game state; it decides, and the game's own jobs act.
/// </summary>
public static class VaccinationService
{
    static readonly List<HediffDef> selfTargets = new();
    static readonly MapPatientCache<Pawn> selfTakers = new(RebuildSelfTakers);

    /// <summary>The vaccine conditions this person lacks, for vaccines on their map; their count.</summary>
    public static int Collect(Pawn pawn, List<HediffDef> into)
    {
        into.Clear();
        IReadOnlyList<DoseOption> options = DoseIndex.VaccineOptions;
        if (options.Count == 0 || pawn.Map == null) return 0;
        HediffSet set = pawn.health.hediffSet;
        for (int i = 0; i < options.Count; i++)
        {
            DoseOption option = options[i];
            if (option.Hediffs.Count == 0) continue;
            HediffDef condition = option.Hediffs[0].Def;
            if (into.Contains(condition) || set.HasHediff(condition) || !DoseService.OnMap(pawn.Map, option.Def)) continue;
            into.Add(condition);
        }
        return into.Count;
    }

    static IReadOnlyList<Pawn> SelfTakers(Map map) => selfTakers.Get(map, PhobosDosingRulesDef.Current.recomputeIntervalTicks);

    static void RebuildSelfTakers(Map map, List<Pawn> found)
    {
        DosingSettings settings = DosingMod.Settings;
        if (!settings.enabled || !CareModeRule.SelfMay(settings.vaccinationMode) || DoseIndex.VaccineOptions.Count == 0) return;
        IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
        for (int i = 0; i < pawns.Count; i++)
        {
            Pawn pawn = pawns[i];
            if (!pawn.RaceProps.Humanlike || !DoseService.MayTakeOwnPreventive(pawn, settings) || !DoseService.IsEligible(pawn, settings)) continue;
            if (Collect(pawn, selfTargets) > 0 && Decide(pawn, selfTargets).Chosen != null) found.Add(pawn);
        }
    }

    static DoseDecision Decide(Pawn pawn, List<HediffDef> targets) =>
        DoseService.SelfCareDecide(pawn, DoseNeed.Vaccination, DoseIndex.VaccineOptions, new CareContext(CareSeriousness.Minor, targets, true), false);

    /// <summary>The game's own ingest job for the vaccine this person would take, or null. Cheapest tests first.</summary>
    public static Job? SelfJob(Pawn pawn)
    {
        DosingSettings settings = DosingMod.Settings;
        if (!settings.enabled || !CareModeRule.SelfMay(settings.vaccinationMode) || DoseIndex.VaccineOptions.Count == 0 || pawn.Map == null) return null;
        IReadOnlyList<Pawn> takers = SelfTakers(pawn.Map);
        if (takers.Count == 0 || !Listed(takers, pawn)) return null;
        if (!DoseService.MayTakeOwnPreventive(pawn, settings) || Collect(pawn, selfTargets) == 0) return null;
        if (Decide(pawn, selfTargets).Chosen is not DoseVerdict chosen || chosen.Item == null) return null;
        if (Medical.Seen.Wants("medical.vaccination-self", pawn)) Medical.Seen.Note("medical.vaccination-self", pawn, chosen.Option.Def.label);
        return DrugAIUtility.IngestAndTakeToInventoryJob(chosen.Item, pawn, 1);
    }

    static bool Listed(IReadOnlyList<Pawn> pawns, Pawn pawn)
    {
        for (int i = 0; i < pawns.Count; i++)
        {
            if (pawns[i] == pawn) return true;
        }
        return false;
    }
}
