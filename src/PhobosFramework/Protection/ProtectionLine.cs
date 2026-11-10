using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Framework.Protection;

/// <summary>
/// What a protective-gear line does to the pawns that cross it (docs/development/radiation-control-line.md). A colonist about to step
/// in without enough protection stops, fetches and puts on gear that raises the zone's stat, then carries on with what they were doing;
/// one stepping out in gear put on at the line takes it off just outside. With no gear in reach they turn back, say so once, and keep
/// clear of the line for an hour. Drafted pawns, and pawns the player does not control, cross freely: an order is the player's call.
/// The core needs no Harmony: a content mod's two patches call <see cref="BeforeStep"/>, <see cref="AfterStep"/> and
/// <see cref="AllowsCell"/>.
/// </summary>
public static class ProtectionLine
{
    /// <summary>How long a pawn turned back for want of gear keeps clear of the line before trying again (agent's choice).</summary>
    public const int TurnBackTicks = GenDate.TicksPerHour;

    /// <summary>A pawn sent to suit up this many times within the turn-back time, and back at the line still unprotected, is turned back:
    /// the gear it was sent for is not getting onto it, and each sending costs a scan of the map's apparel (agent's choice).</summary>
    public const int MaxSuitUpAttempts = 3;

    /// <summary>
    /// The room a control point at a cell, facing a direction, closes off: the room in front of it, or past the door in front of it;
    /// null for a wall, the map's edge or open ground, which a sign can't close off.
    /// </summary>
    public static Room? RoomFaced(Map map, IntVec3 at, Rot4 facing)
    {
        IntVec3 cell = at + facing.FacingCell;
        if (!cell.InBounds(map)) return null;
        if (cell.GetDoor(map) != null)
        {
            cell += facing.FacingCell;
            if (!cell.InBounds(map)) return null;
        }
        Room? room = cell.GetRoom(map);
        if (room == null || room.UsesOutdoorTemperature) return null;
        return room;
    }

    /// <summary>A control point's inspect line: the room it closes off and what crossing needs, or why it closes off nothing.</summary>
    public static string Describe(ProtectionZoneDef zone, Map map, IntVec3 at, Rot4 facing)
    {
        Room? room = RoomFaced(map, at, facing);
        if (room != null)
        {
            return "PhobosProtection_PointCloses".Translate(zone.label.Named("ZONE"), room.GetRoomRoleLabel().Named("ROOM"),
                zone.stat.label.Named("STAT"), zone.required.ToStringByStyle(zone.stat.toStringStyle).Named("REQUIRED"));
        }
        IntVec3 cell = at + facing.FacingCell;
        bool wall = !cell.InBounds(map) || cell.Impassable(map);
        return (wall ? "PhobosProtection_PointFacesWall" : "PhobosProtection_PointFacesOpenGround").Translate();
    }

    public static void DrawRoom(Room? room, ProtectionZoneDef zone)
    {
        if (room != null) GenDraw.DrawFieldEdges(room.Cells.ToList(), zone.color);
    }

    /// <summary>Whether a pawn's protection against a zone is enough already, from what it wears, its genes and everything else.</summary>
    public static bool Protected(Pawn pawn, ProtectionZoneDef zone) => ProtectionRule.IsProtected(pawn.GetStatValue(zone.stat), zone.required);

    static bool Applies(Pawn pawn) =>
        pawn.Spawned && pawn.RaceProps.Humanlike && pawn.apparel != null && pawn.IsColonistPlayerControlled && !pawn.Drafted;

    /// <summary>A pawn on its way to put gear on or take it off walks where it must.</summary>
    static bool Changing(Pawn pawn)
    {
        JobDef? def = pawn.CurJob?.def;
        return def == JobDefOf.Wear || def == JobDefOf.RemoveApparel;
    }

    static Pawn? lastBefore;
    static int lastBeforeTick = -1;
    static bool lastBeforeResult = true;
    static Pawn? lastAfter;
    static int lastAfterTick = -1;

    /// <summary>Before a step: false stops it, when the pawn was sent to suit up or turned back instead. Two mods hooking the same step
    /// get the same answer, and the pawn is sent to suit up once.</summary>
    public static bool BeforeStep(Pawn pawn, IntVec3 next)
    {
        ProtectionZones? zones = ProtectionZones.For(pawn.Map);
        if (zones == null || !zones.Active) return true;
        int now = Verse.Find.TickManager.TicksGame;
        if (pawn == lastBefore && now == lastBeforeTick) return lastBeforeResult;
        lastBefore = pawn;
        lastBeforeTick = now;
        lastBeforeResult = Step(pawn, next, zones);
        return lastBeforeResult;
    }

    static bool Step(Pawn pawn, IntVec3 next, ProtectionZones zones)
    {
        if (!Applies(pawn) || Changing(pawn)) return true;
        ProtectionZoneDef? to = zones.ZoneAt(next);
        if (to == null) return true;
        ProtectionZoneDef? from = zones.ZoneAt(pawn.Position);
        CrossingAction action = ProtectionRule.Decide(from != null && from == to, true, Protected(pawn, to), false);
        if (action != CrossingAction.SuitUp) return true;
        // Already kept clear of the line, and here again because the route only crosses the zone (pathing ignores allowed areas): no
        // gear search and no message. The step fails as a path fails, so the game waits its 250 ticks before the next job search.
        // Before 0.72.1 every such step searched the gear and the pawn's work again, every tick, at tens of milliseconds a tick.
        if (zones.TurnedBack(pawn))
        {
            Refuse(pawn, JobCondition.ErroredPather);
            return false;
        }
        SuitUp(pawn, to, zones);
        return false;
    }

    /// <summary>After a step out of a zone: gear put on at the line comes off, just outside it, then the pawn carries on.</summary>
    public static void AfterStep(Pawn pawn, IntVec3 from)
    {
        ProtectionZones? zones = ProtectionZones.For(pawn.Map);
        if (zones == null || !zones.Active || !Applies(pawn) || Changing(pawn)) return;
        int now = Verse.Find.TickManager.TicksGame;
        if (pawn == lastAfter && now == lastAfterTick) return;
        lastAfter = pawn;
        lastAfterTick = now;
        bool fromControlled = zones.ZoneAt(from) != null;
        bool toControlled = zones.ZoneAt(pawn.Position) != null;
        List<Apparel> lineGear = pawn.apparel.WornApparel.Where(zones.IsLineGear).ToList();
        if (ProtectionRule.Decide(fromControlled, toControlled, true, lineGear.Count > 0) != CrossingAction.StepOff) return;
        var jobs = new List<Job>();
        foreach (Apparel apparel in lineGear)
        {
            zones.RemoveLineGear(apparel);
            pawn.outfits?.forcedHandler?.SetForced(apparel, false);
            jobs.Add(JobMaker.MakeJob(JobDefOf.RemoveApparel, apparel));
        }
        Start(pawn, jobs);
    }

    /// <summary>For the game's allowed-area check: a pawn turned back for want of gear treats the zone as out of bounds for a while, so
    /// it takes up other work rather than walking back to the line.</summary>
    public static bool AllowsCell(Pawn pawn, IntVec3 cell)
    {
        ProtectionZones? zones = ProtectionZones.For(pawn.Map);
        if (zones == null || !zones.Active || !zones.TurnedBack(pawn)) return true;
        return zones.ZoneAt(cell) == null;
    }

    static void SuitUp(Pawn pawn, ProtectionZoneDef zone, ProtectionZones zones)
    {
        // Sent for gear again and again and still unprotected: whatever stops the gear going on, it is not going on. Turn back.
        if (zones.SuitUps(pawn) >= MaxSuitUpAttempts)
        {
            TurnBack(pawn, zone, zones);
            return;
        }
        zones.PruneLineGear();
        List<Apparel> gear = GearFor(pawn, zone, zones, out List<float> gains, out List<float> costs);
        BodyDef body = pawn.RaceProps.body;
        IReadOnlyList<int> chosen = ProtectionRule.Choose(pawn.GetStatValue(zone.stat), zone.required, gains, costs,
            (a, b) => ApparelUtility.CanWearTogether(gear[a].def, gear[b].def, body));
        if (chosen.Count == 0)
        {
            TurnBack(pawn, zone, zones);
            return;
        }
        zones.NoteSuitUp(pawn, Find.TickManager.TicksGame + TurnBackTicks);
        var jobs = new List<Job>();
        foreach (int index in chosen)
        {
            Apparel apparel = gear[index];
            zones.AddLineGear(apparel);
            // Forced, so the pawn's apparel policy doesn't take it off again before they step back out.
            pawn.outfits?.forcedHandler?.SetForced(apparel, true);
            jobs.Add(JobMaker.MakeJob(JobDefOf.Wear, apparel));
        }
        MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "PhobosProtection_SuitingUp".Translate());
        Start(pawn, jobs);
    }

    /// <summary>Gear in reach that raises the zone's stat: outside every zone, not forbidden, wearable by this pawn and free to take.
    /// Its gain is what it adds after whatever it would replace comes off; its cost is the walk to it.</summary>
    static List<Apparel> GearFor(Pawn pawn, ProtectionZoneDef zone, ProtectionZones zones, out List<float> gains, out List<float> costs)
    {
        var gear = new List<Apparel>();
        gains = new List<float>();
        costs = new List<float>();
        BodyDef body = pawn.RaceProps.body;
        List<Apparel> worn = pawn.apparel.WornApparel;
        foreach (Thing thing in pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Apparel))
        {
            if (thing is not Apparel apparel) continue;
            float offset = apparel.def.equippedStatOffsets.GetStatOffsetFromList(zone.stat);
            if (offset <= 0f || apparel.IsForbidden(pawn) || zones.ZoneAt(apparel.Position) != null) continue;
            if (!ApparelUtility.HasPartsToWear(pawn, apparel.def) || !EquipmentUtility.CanEquip(apparel, pawn)) continue;
            if (pawn.apparel.WouldReplaceLockedApparel(apparel)) continue;
            float lost = 0f;
            for (int i = 0; i < worn.Count; i++)
            {
                if (!ApparelUtility.CanWearTogether(worn[i].def, apparel.def, body)) lost += worn[i].def.equippedStatOffsets.GetStatOffsetFromList(zone.stat);
            }
            if (offset - lost <= 0f) continue;
            if (!pawn.CanReserveAndReach(apparel, PathEndMode.ClosestTouch, Danger.Some)) continue;
            gear.Add(apparel);
            gains.Add(offset - lost);
            costs.Add((apparel.Position - pawn.Position).LengthHorizontalSquared);
        }
        return gear;
    }

    static void TurnBack(Pawn pawn, ProtectionZoneDef zone, ProtectionZones zones)
    {
        bool first = !zones.TurnedBack(pawn);
        zones.TurnBack(pawn, Find.TickManager.TicksGame + TurnBackTicks);
        zones.ForgetSuitUps(pawn);
        // Incompletable: the pawn looks for other work at once, with the zone out of bounds to it for the hour.
        Refuse(pawn, JobCondition.Incompletable);
        if (first)
        {
            Messages.Message("PhobosProtection_TurnedBack".Translate(pawn.Named("PAWN"), zone.label.Named("ZONE"), zone.stat.label.Named("STAT")),
                pawn, MessageTypeDefOf.RejectInput, historical: false);
        }
    }

    /// <summary>
    /// Ends the pawn's job where it stands. The pather is stopped first: the refused step is skipped, so left alone the pawn stays
    /// mid-step, a new job to the same place keeps the old path (the game's StartPath returns early for an unchanged destination), and
    /// the same step is tried, refused and re-planned every tick.
    /// </summary>
    static void Refuse(Pawn pawn, JobCondition condition)
    {
        pawn.pather.StopDead();
        pawn.jobs.EndCurrentJob(condition);
    }

    /// <summary>Starts the jobs in order, then the job the pawn was doing, which the game resumes when it can be paused. The pather is
    /// stopped first, so the first job paths afresh rather than finishing the step toward the line while its path is still being found.</summary>
    static void Start(Pawn pawn, List<Job> jobs)
    {
        if (jobs.Count == 0) return;
        pawn.pather.StopDead();
        pawn.jobs.StartJob(jobs[0], JobCondition.InterruptForced, resumeCurJobAfterwards: true);
        for (int i = jobs.Count - 1; i >= 1; i--) pawn.jobs.jobQueue.EnqueueFirst(jobs[i]);
    }
}
