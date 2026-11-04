using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Phobos.BurnsAndSmoke.Surgery;

/// <summary>
/// The game's remove-a-condition surgery, plus the condition the operation leaves behind (its Def's addsHediff), on the same
/// part. Used by the tracheotomy: the airway burn goes, a tracheostomy stays for a few days. Keeping removesHediff set means
/// Search and Rescue still sees the airway burn as an urgent surgical case.
/// </summary>
public class Recipe_PhobosRemoveAddHediff : Recipe_RemoveHediff
{
    public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
    {
        bool hadIt = recipe.removesHediff != null && pawn.health.hediffSet.HasHediff(recipe.removesHediff);
        base.ApplyOnPawn(pawn, part, billDoer, ingredients, bill);
        if (pawn.Dead || recipe.addsHediff == null) return;
        bool removed = recipe.removesHediff == null || !pawn.health.hediffSet.HasHediff(recipe.removesHediff);
        if (!hadIt || !removed || pawn.health.hediffSet.HasHediff(recipe.addsHediff)) return;
        pawn.health.AddHediff(recipe.addsHediff, part);
    }
}
