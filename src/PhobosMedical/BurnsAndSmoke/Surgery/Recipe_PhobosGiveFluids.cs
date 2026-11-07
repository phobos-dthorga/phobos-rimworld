using System.Collections.Generic;
using Phobos.BurnsAndSmoke.Defs;
using Phobos.BurnsAndSmoke.Services;
using RimWorld;
using Verse;

namespace Phobos.BurnsAndSmoke.Surgery;

/// <summary>
/// A bill that runs a drip for burn shock: offered only while the patient has it, never fails (it is a drip, not an operation), and
/// says what it did. The decision is <see cref="FluidsService"/>'s.
/// </summary>
public class Recipe_PhobosGiveFluids : Recipe_Surgery
{
    public override bool AvailableOnNow(Thing thing, BodyPartRecord? part = null) =>
        thing is Pawn pawn && FluidsService.NeedsFluids(pawn) && base.AvailableOnNow(thing, part);

    public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
    {
        bool given = FluidsService.Give(pawn, PhobosBurnRulesDef.Current.fluidBillTendQuality);
        string doctor = billDoer?.LabelShort ?? "";
        if (given)
            Messages.Message("PhobosBurns_FluidsGiven".Translate(doctor.Named("DOCTOR"), pawn.LabelShort.Named("PATIENT")), pawn, MessageTypeDefOf.PositiveEvent);
        else
            Messages.Message("PhobosBurns_FluidsNotNeeded".Translate(pawn.LabelShort.Named("PATIENT")), pawn, MessageTypeDefOf.NeutralEvent);
    }
}
