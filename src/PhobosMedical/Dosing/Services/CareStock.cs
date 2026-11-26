using System.Collections.Generic;
using Phobos.Dosing.Defs;
using Phobos.Dosing.Needs;
using Phobos.Dosing.Rules;
using Phobos.Framework.Health;
using Verse;

namespace Phobos.Dosing.Services;

/// <summary>One person with a care need nothing in stock answers, and what they lack, in the game world's words.</summary>
public readonly struct UnmetCare
{
    public readonly Pawn Pawn;
    public readonly string What;

    public UnmetCare(Pawn pawn, string what)
    {
        Pawn = pawn;
        What = what;
    }
}

/// <summary>
/// What the colony could have and does not (Medical 0.28.0; the owner's card of 26 Nov 2026: an alert while a need has nothing in
/// stock that answers it). An item counts as known when a recipe that makes it is available now, by the game's own test (its research
/// done): a colony that cannot make a painkiller is not nagged about having none. An item nobody can craft is never known. The
/// per-map list is rebuilt at most once per the dosing rules' interval, when the alert asks; nothing is saved.
/// </summary>
public static class CareStock
{
    static Dictionary<ThingDef, List<RecipeDef>>? makers;
    static readonly MapPatientCache<UnmetCare> unmet = new(Rebuild);

    /// <summary>Whether the colony knows how to make this item now.</summary>
    public static bool Known(ThingDef def)
    {
        if (makers == null)
        {
            makers = new Dictionary<ThingDef, List<RecipeDef>>();
            List<RecipeDef> recipes = DefDatabase<RecipeDef>.AllDefsListForReading;
            for (int i = 0; i < recipes.Count; i++)
            {
                List<ThingDefCountClass>? products = recipes[i].products;
                if (products == null) continue;
                for (int j = 0; j < products.Count; j++)
                {
                    ThingDef? product = products[j].thingDef;
                    if (product == null) continue;
                    if (!makers.TryGetValue(product, out List<RecipeDef>? list)) makers[product] = list = new List<RecipeDef>();
                    list.Add(recipes[i]);
                }
            }
        }
        if (!makers.TryGetValue(def, out List<RecipeDef>? made)) return false;
        for (int i = 0; i < made.Count; i++)
        {
            if (made[i].AvailableNow) return true;
        }
        return false;
    }

    /// <summary>The people on a map with a need nothing in stock answers, as last worked out.</summary>
    public static IReadOnlyList<UnmetCare> Unmet(Map map) => unmet.Get(map, PhobosDosingRulesDef.Current.recomputeIntervalTicks);

    static void Rebuild(Map map, List<UnmetCare> found)
    {
        DosingSettings settings = DosingMod.Settings;
        if (!settings.enabled || !settings.unmetAlert) return;
        PhobosDosingRulesDef rules = PhobosDosingRulesDef.Current;
        IReadOnlyList<PhobosCareNeedDef> needs = CareNeeds.Ordered;
        IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
        for (int i = 0; i < pawns.Count; i++)
        {
            Pawn pawn = pawns[i];
            if (!DoseService.IsEligible(pawn, settings)) continue;
            for (int j = 0; j < needs.Count; j++)
            {
                if (!needs[j].alertWhenUnmet) continue;
                CareNeedWorker worker = needs[j].Worker;
                if (!CareModeRule.DoctorsMay(worker.Mode)) continue;
                string? what = worker.Unmet(pawn, settings, rules);
                if (what == null) continue;
                found.Add(new UnmetCare(pawn, what));
                if (Medical.Seen.Wants("medical.care-unmet", pawn, needs[j].defName)) Medical.Seen.Note("medical.care-unmet", pawn, what, needs[j].defName);
            }
        }
    }
}
