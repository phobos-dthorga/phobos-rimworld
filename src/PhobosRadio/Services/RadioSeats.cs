using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Radio.Services;

/// <summary>
/// A job that, while its colonist sits at a radio station's desk, makes a call that goes out through that station (Radio 0.51.0; the owner's
/// card of 27 Nov 2026: a call at the desk takes a channel "like a hand-held"). The net reads it each pass and serves the call from the
/// station's channels, of the local voice kind; a call the station has no room for is busy, and ends.
/// </summary>
public interface IRadioDeskCall
{
    /// <summary>The desk whose channel the call takes now, or null: still walking there, on a hand-held, or talking to someone beside them.</summary>
    Thing? CallingFrom { get; }
}

/// <summary>
/// Where a colonist works a radio station (Radio 0.51.0; the owner's cards of 27 Nov 2026: a spot in front of the desk with the player's
/// own chair, one seat on the base station and two on the radio terminal): the game's own interaction spots, from the station's Def, each
/// held by one colonist at a time. A colonist sits on a chair placed there, and stands if there is none, as at a research bench. A station
/// of another mod's with no spots is worked standing beside it, as before.
/// <para>Cost: a few cell lookups when a job picks a desk, and a facing and a comfort check a tick while seated, as the game's benches.</para>
/// </summary>
public static class RadioSeats
{
    static readonly List<IntVec3> cells = new();

    /// <summary>The desk's seats now, in the order its Def gives them; empty for a desk with no spots.</summary>
    public static List<IntVec3> SeatsOf(Thing desk)
    {
        cells.Clear();
        if (desk.Spawned && desk.def.HasSingleOrMultipleInteractionCells) cells.AddRange(desk.InteractionCells);
        return cells;
    }

    /// <summary>Whether the desk has seats at all; one without is worked standing beside it.</summary>
    public static bool HasSeats(Thing desk) => desk.def.HasSingleOrMultipleInteractionCells;

    /// <summary>A seat this colonist can take and reach: one they hold already first; Invalid when every seat is taken or blocked.</summary>
    public static IntVec3 FreeSeat(Pawn pawn, Thing desk)
    {
        IntVec3 found = IntVec3.Invalid;
        foreach (IntVec3 cell in SeatsOf(desk))
        {
            if (!cell.InBounds(desk.Map) || !cell.Standable(desk.Map)) continue;
            if (desk.Map.reservationManager.ReservedBy(cell, pawn)) return cell;
            if (found.IsValid || !pawn.CanReserve(cell) || !pawn.CanReach(cell, PathEndMode.OnCell, Danger.Some)) continue;
            found = cell;
        }
        return found;
    }

    /// <summary>How many of the desk's seats this colonist could take now (for two people to sit together).</summary>
    public static int FreeSeats(Pawn pawn, Thing desk)
    {
        int count = 0;
        foreach (IntVec3 cell in SeatsOf(desk))
            if (cell.InBounds(desk.Map) && cell.Standable(desk.Map) && pawn.CanReserve(cell) && pawn.CanReach(cell, PathEndMode.OnCell, Danger.Some)) count++;
        return count;
    }

    /// <summary>
    /// How many of the desk's seats something stands on, so nobody can sit there (Radio 0.51.0): a station built before it had seats may have
    /// a wall or a machine where a colonist now sits. 0 for a desk with no seats.
    /// </summary>
    public static int BlockedSeats(Thing desk)
    {
        int count = 0;
        foreach (IntVec3 cell in SeatsOf(desk))
            if (!cell.InBounds(desk.Map) || !cell.Standable(desk.Map)) count++;
        return count;
    }

    /// <summary>Whether this colonist can work the desk now: a seat free for them, or a desk with no seats.</summary>
    public static bool CanSit(Pawn pawn, Thing desk) => !HasSeats(desk) || FreeSeat(pawn, desk).IsValid;

    /// <summary>
    /// Takes a seat at the desk for the job, the one held already when still free: false when none is free. A desk with no seats needs
    /// none (and <paramref name="seat"/> stays Invalid).
    /// </summary>
    public static bool Claim(Pawn pawn, Job job, Thing desk, ref IntVec3 seat, bool errorOnFailed)
    {
        if (!HasSeats(desk))
        {
            seat = IntVec3.Invalid;
            return true;
        }
        if (!seat.IsValid || !SeatsOf(desk).Contains(seat) || !pawn.CanReserve(seat)) seat = FreeSeat(pawn, desk);
        if (!seat.IsValid) return false;
        return pawn.Reserve(seat, job, 1, -1, null, errorOnFailed);
    }

    /// <summary>Walks to the seat (or, at a desk with none, to beside it).</summary>
    public static Toil GoTo(Thing desk, Func<IntVec3> seat)
    {
        Toil toil = ToilMaker.MakeToil("PhobosRadio_GoToSeat");
        toil.initAction = () =>
        {
            Pawn actor = toil.actor;
            IntVec3 cell = seat();
            if (cell.IsValid)
            {
                if (actor.Position == cell) actor.jobs.curDriver.ReadyForNextToil();
                else actor.pather.StartPath(cell, PathEndMode.OnCell);
            }
            else if (actor.CanReachImmediate(desk, PathEndMode.Touch)) actor.jobs.curDriver.ReadyForNextToil();
            else actor.pather.StartPath(desk, PathEndMode.Touch);
        };
        toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
        return toil;
    }

    /// <summary>A tick at the desk: facing it, and the chair's comfort when there is one, as at the game's benches.</summary>
    public static void AtDesk(Pawn pawn, Thing desk, int delta)
    {
        pawn.rotationTracker.FaceTarget(desk);
        pawn.GainComfortFromCellIfPossible(delta, chairsOnly: true);
    }

    /// <summary>Saying it was seen sitting at a radio desk (row 720), with a chair or standing.</summary>
    public static void NoteSeated(Pawn pawn, Thing desk)
    {
        if (!HasSeats(desk)) return;
        // A chair as the game's own comfort check finds one (PawnUtility.GainComfortFromCellIfPossible, chairs only).
        Building? edifice = pawn.Position.GetEdifice(pawn.Map);
        string variant = edifice != null && edifice.def.category == ThingCategory.Building && edifice.def.building?.isSittable == true ? "chair" : "standing";
        if (Seen.Wants("radio.desk-seat", pawn, variant)) Seen.Note("radio.desk-seat", pawn, desk.LabelShort, variant);
    }

    /// <summary>
    /// Ends a call at the desk that the station has no channel for (Radio 0.51.0), with a message: looked at every 250 ticks, as the net
    /// serves calls about once a second. Returns true when it ended the job.
    /// </summary>
    public static bool HungUpBusy(JobDriver driver, Pawn pawn, Thing desk, int delta)
    {
        if (!pawn.IsHashIntervalTick(250, delta) || !RadioNet.For(pawn.Map).DeskCallBusy(pawn)) return false;
        Messages.Message("PhobosRadio_DeskCallBusy".Translate(pawn.LabelShort.Named("PAWN"), desk.LabelShort.Named("DESK")).Resolve(),
            new LookTargets(pawn), MessageTypeDefOf.NeutralEvent, historical: false);
        if (Seen.Wants("radio.desk-call-busy", pawn)) Seen.Note("radio.desk-call-busy", pawn, desk.LabelShort);
        driver.EndJobWith(JobCondition.Incompletable);
        return true;
    }
}
