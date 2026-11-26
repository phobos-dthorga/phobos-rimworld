using System.Collections.Generic;
using Phobos.Dosing.Defs;
using Phobos.Dosing.Rules;
using Phobos.Framework.Health;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Dosing.Services;

/// <summary>
/// Prevention in an outbreak (Medical 0.28.0; docs/development/medical-care-needs.md): which outbreak diseases are on a map, who is
/// unprotected against them, and who would take a preventive on their own. Two per-map lists, each rebuilt at most once per the
/// dosing rules' interval and only when asked, so the check a person makes each time they choose a job is one lookup; with no
/// preventive in the game, or no such disease on the map, it is less. Nothing is saved; nothing here mutates game state.
/// </summary>
public static class PreventionService
{
    static readonly MapPatientCache<HediffDef> outbreaks = new(RebuildOutbreaks);
    static readonly MapPatientCache<Pawn> selfTakers = new(RebuildSelfTakers);
    static readonly List<HediffDef> selfTargets = new();

    /// <summary>The outbreak diseases some indexed item prevents that one of the player's people on the map visibly has.</summary>
    public static IReadOnlyList<HediffDef> Outbreaks(Map map) => outbreaks.Get(map, PhobosDosingRulesDef.Current.recomputeIntervalTicks);

    static void RebuildOutbreaks(Map map, List<HediffDef> found)
    {
        HashSet<HediffDef> preventable = DoseIndex.PreventableOutbreaks;
        if (preventable.Count == 0) return;
        IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
        for (int i = 0; i < pawns.Count; i++)
        {
            if (!Patients.IsPlayersPerson(pawns[i])) continue;
            List<Hediff> hediffs = pawns[i].health.hediffSet.hediffs;
            for (int j = 0; j < hediffs.Count; j++)
            {
                HediffDef condition = hediffs[j].def;
                if (hediffs[j].Visible && preventable.Contains(condition) && !found.Contains(condition)) found.Add(condition);
            }
        }
    }

    /// <summary>The outbreak diseases on this person's map that could still strike them, into <paramref name="into"/>; how many.</summary>
    public static int Collect(Pawn pawn, PhobosDosingRulesDef rules, List<HediffDef> into)
    {
        into.Clear();
        if (pawn.Map == null || pawn.health?.immunity == null || !pawn.RaceProps.IsFlesh) return 0;
        IReadOnlyList<HediffDef> onMap = Outbreaks(pawn.Map);
        for (int i = 0; i < onMap.Count; i++)
        {
            if (PreventionRule.Unprotected(pawn.health.immunity.DiseaseContractChanceFactor(onMap[i]), rules.preventionMinRisk)) into.Add(onMap[i]);
        }
        return into.Count;
    }

    /// <summary>The people on a map who would take a preventive on their own now, as last worked out.</summary>
    static IReadOnlyList<Pawn> SelfTakers(Map map) => selfTakers.Get(map, PhobosDosingRulesDef.Current.recomputeIntervalTicks);

    static void RebuildSelfTakers(Map map, List<Pawn> found)
    {
        DosingSettings settings = DosingMod.Settings;
        if (!settings.enabled || !CareModeRule.SelfMay(settings.preventionMode) || DoseIndex.PreventionOptions.Count == 0 || Outbreaks(map).Count == 0) return;
        PhobosDosingRulesDef rules = PhobosDosingRulesDef.Current;
        IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
        for (int i = 0; i < pawns.Count; i++)
        {
            Pawn pawn = pawns[i];
            if (!pawn.RaceProps.Humanlike || !DoseService.MayTakeOwnPreventive(pawn, settings) || !DoseService.IsEligible(pawn, settings)) continue;
            if (Collect(pawn, rules, selfTargets) > 0 && DoseService.SelfPreventionDecide(pawn, selfTargets).Chosen != null) found.Add(pawn);
        }
    }

    /// <summary>
    /// The game's own ingest job for the preventive this person would take, or null. Asked for every humanlike choosing a job, so the
    /// first tests are the cheapest: the settings, whether the game holds a preventive at all, then the map's list.
    /// </summary>
    public static Job? SelfJob(Pawn pawn)
    {
        DosingSettings settings = DosingMod.Settings;
        if (!settings.enabled || !CareModeRule.SelfMay(settings.preventionMode) || DoseIndex.PreventionOptions.Count == 0 || pawn.Map == null) return null;
        IReadOnlyList<Pawn> takers = SelfTakers(pawn.Map);
        if (takers.Count == 0 || !Contains(takers, pawn)) return null;
        // The list can be up to an interval old: ask again before sending them off (they may have taken it, or been drafted, since).
        if (!DoseService.MayTakeOwnPreventive(pawn, settings) || Collect(pawn, PhobosDosingRulesDef.Current, selfTargets) == 0) return null;
        if (DoseService.SelfPreventionDecide(pawn, selfTargets).Chosen is not DoseVerdict chosen || chosen.Item == null) return null;
        if (Medical.Seen.Wants("medical.prevention-self", pawn)) Medical.Seen.Note("medical.prevention-self", pawn, chosen.Option.Def.label);
        return DrugAIUtility.IngestAndTakeToInventoryJob(chosen.Item, pawn, 1);
    }

    static bool Contains(IReadOnlyList<Pawn> pawns, Pawn pawn)
    {
        for (int i = 0; i < pawns.Count; i++)
        {
            if (pawns[i] == pawn) return true;
        }
        return false;
    }
}
