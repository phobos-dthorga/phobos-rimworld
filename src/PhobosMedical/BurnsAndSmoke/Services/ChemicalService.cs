using System.Collections.Generic;
using System.Linq;
using Phobos.BurnsAndSmoke.Defs;
using Phobos.BurnsAndSmoke.Rules;
using RimWorld;
using Verse;

namespace Phobos.BurnsAndSmoke.Services;

/// <summary>
/// Acid left on the skin. When a chemical burn (a Def marked <see cref="PhobosRinsedByWaterExtension"/>) lands, the pawn gets "acid on
/// the skin", which deepens their chemical burns through the burn's own damage kind until it is spent or rinsed off. Rinsing is the
/// safety shower's job; this only decides and does the effect.
/// </summary>
public static class ChemicalService
{
    static readonly Dictionary<HediffDef, DamageDef?> damageFor = new();
    static readonly List<Hediff> burns = new();
    /// <summary>Set while this deepens a burn, so the damage it adds doesn't start the acid over.</summary>
    static bool deepening;

    public static bool IsRinsable(HediffDef def) => def.HasModExtension<PhobosRinsedByWaterExtension>();

    /// <summary>Called for every hediff the game adds to a humanlike pawn: fresh acid puts (or keeps) acid on the skin.</summary>
    public static void NoteHediffAdded(Pawn pawn, Hediff hediff)
    {
        if (deepening || pawn.Dead || !BurnsAndSmokeMod.Settings.chemicalBurns || hediff is not Hediff_Injury || !IsRinsable(hediff.def)) return;
        Hediff? onSkin = pawn.health.hediffSet.GetFirstHediffOfDef(BurnsDefOf.PhobosBurns_ChemicalOnSkin);
        if (onSkin == null) pawn.health.AddHediff(BurnsDefOf.PhobosBurns_ChemicalOnSkin);
        else onSkin.Severity = 1f;
    }

    /// <summary>Acid on the skin; or, with strip and rinse on, clothes carrying caustic residue.</summary>
    public static bool NeedsRinse(Pawn pawn) => pawn.health.hediffSet.HasHediff(BurnsDefOf.PhobosBurns_ChemicalOnSkin)
        || (BurnsAndSmokeMod.Settings.causticStripAndRinse && CausticService.WearsContaminated(pawn));

    /// <summary>Whether every open chemical burn is on an eye, so an eyewash's water is enough.</summary>
    public static bool OnEyesOnly(Pawn pawn)
    {
        bool any = false;
        foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
        {
            if (hediff is not Hediff_Injury || hediff.IsPermanent() || !IsRinsable(hediff.def)) continue;
            if (hediff.Part == null || !hediff.Part.def.tags.Contains(BodyPartTagDefOf.SightSource)) return false;
            any = true;
        }
        return any;
    }

    /// <summary>Spreads <paramref name="damage"/> over the pawn's open chemical burns, through each burn's own damage kind. Armour is
    /// ignored: the acid is already under it.</summary>
    public static void Deepen(Pawn pawn, float damage)
    {
        if (damage <= 0f || pawn.Dead) return;
        burns.Clear();
        foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
        {
            if (hediff is Hediff_Injury && hediff.Part != null && !hediff.IsPermanent() && IsRinsable(hediff.def) && DamageFor(hediff.def) != null) burns.Add(hediff);
        }
        if (burns.Count == 0) return;
        float share = damage / burns.Count;
        deepening = true;
        try
        {
            foreach (Hediff burn in burns)
            {
                if (pawn.Dead || burn.Part == null || pawn.health.hediffSet.PartIsMissing(burn.Part)) continue;
                var dinfo = new DamageInfo(DamageFor(burn.def)!, share, hitPart: burn.Part);
                dinfo.SetIgnoreArmor(true);
                dinfo.SetAllowDamagePropagation(false);
                pawn.TakeDamage(dinfo);
            }
        }
        finally
        {
            deepening = false;
            burns.Clear();
        }
    }

    /// <summary>
    /// A chemical-deepening comp on a condition other than our acid on the skin (the owner's cards of 28 Nov 2026, "only its own burn"
    /// and "let it take the part"): deepens that burn alone, by raising its own severity rather than through a fresh hit, so nothing else
    /// on the person is touched and no new wound carrying the comp is made. It may grow by all its part has left, and no more, so the
    /// damage is counted once. Does nothing with the player's acid setting off.
    /// </summary>
    public static void DeepenOwn(Hediff burn, float damage)
    {
        Pawn pawn = burn.pawn;
        BodyPartRecord? part = burn.Part;
        if (!BurnsAndSmokeMod.Settings.chemicalBurns || pawn == null || pawn.Dead || part == null) return;
        HediffSet set = pawn.health.hediffSet;
        if (set.PartIsMissing(part)) return;
        float grow = ChemicalRule.OwnDeepening(damage, set.GetPartHealth(part));
        if (grow <= 0f) return;
        burn.Severity += grow;
        // The game takes a part only when a wound is added (HediffSet.AddDirect), never when one grows, so the same step follows here,
        // as AddDirect does it: a part other than the body's core with no health left as the game counts it (GetPartHealth keeps its own
        // exceptions: a part damage cannot destroy, a wound that does not destroy parts) is lost, and the wounds on it go with it.
        if (part != pawn.RaceProps.body.corePart && set.GetPartHealth(part) == 0f)
        {
            bool replaced = set.HasDirectlyAddedPartFor(part);
            var missing = (Hediff_MissingPart)HediffMaker.MakeHediff(HediffDefOf.MissingBodyPart, pawn);
            missing.IsFresh = !replaced;
            missing.lastInjury = replaced ? null : burn.def;
            pawn.health.AddHediff(missing, part);
        }
        else
        {
            // Whatever the wound's growth changes (the body's core, bleeding, a lethal total) is checked as for any changed condition.
            pawn.health.Notify_HediffChanged(burn);
        }
    }

    /// <summary>The acid is washed off: it stops burning, and the cool water eases the pain for a few hours.</summary>
    public static void Rinse(Pawn pawn)
    {
        Hediff? onSkin = pawn.health.hediffSet.GetFirstHediffOfDef(BurnsDefOf.PhobosBurns_ChemicalOnSkin);
        if (onSkin != null) pawn.health.RemoveHediff(onSkin);
        CausticService.Decontaminate(pawn);
        Hediff? rinsed = pawn.health.hediffSet.GetFirstHediffOfDef(BurnsDefOf.PhobosBurns_Rinsed);
        if (rinsed == null) pawn.health.AddHediff(BurnsDefOf.PhobosBurns_Rinsed);
        else rinsed.Severity = 1f;
    }

    /// <summary>The damage kind that makes this injury, found once from the Defs (by its effect, never its name).</summary>
    static DamageDef? DamageFor(HediffDef injury)
    {
        if (!damageFor.TryGetValue(injury, out DamageDef? found))
        {
            found = DefDatabase<DamageDef>.AllDefsListForReading.FirstOrDefault(d => d.hediff == injury);
            damageFor[injury] = found;
        }
        return found;
    }
}
