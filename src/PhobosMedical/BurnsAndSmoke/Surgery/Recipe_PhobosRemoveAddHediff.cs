using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Phobos.BurnsAndSmoke.Surgery;

/// <summary>
/// The game's remove-a-condition surgery, plus the condition the operation leaves behind (its Def's addsHediff), on the same
/// part. Used by the tracheotomy (the airway burn goes, a tracheostomy stays for a few days) and the escharotomy (the eschar goes,
/// the incisions stay while they heal). Each check is on the operated part, so an eschar on one limb is told apart from one on another.
/// Keeping removesHediff set means Search and Rescue still sees the airway burn as an urgent surgical case.
/// </summary>
public class Recipe_PhobosRemoveAddHediff : Recipe_RemoveHediff
{
    public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
    {
        bool hadIt = recipe.removesHediff != null && Has(pawn, recipe.removesHediff, part);
        base.ApplyOnPawn(pawn, part, billDoer, ingredients, bill);
        if (pawn.Dead || recipe.addsHediff == null) return;
        bool removed = recipe.removesHediff == null || !Has(pawn, recipe.removesHediff, part);
        if (!hadIt || !removed || Has(pawn, recipe.addsHediff, part)) return;
        pawn.health.AddHediff(recipe.addsHediff, part);
    }

    static bool Has(Pawn pawn, HediffDef def, BodyPartRecord? part)
    {
        if (part == null) return pawn.health.hediffSet.HasHediff(def);
        List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
        for (int i = 0; i < hediffs.Count; i++)
        {
            if (hediffs[i].def == def && hediffs[i].Part == part) return true;
        }
        return false;
    }
}
