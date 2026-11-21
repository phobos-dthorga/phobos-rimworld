using System.Collections.Generic;
using Phobos.Framework.Environment;
using Phobos.Framework.Rules;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Framework.Power;

/// <summary>
/// Standby power's look at one map (Framework 0.222.0; docs/development/standby-power.md): once a second of game time it walks the map's
/// people once, noting the rooms someone awake is in and the buildings someone is working at or watching, then settles every building
/// standby power looks after. Nothing runs between looks, no game method is patched, and the buildings come from a list kept as they are
/// built and taken down (<see cref="MapRegistry{T}"/>), so no look ever walks every building. The cost grows with people plus buildings,
/// once a second. Nothing is saved.
/// </summary>
public sealed class MapComponent_PhobosStandby : MapComponent
{
    readonly HashSet<Room> roomsAwake = new();
    readonly HashSet<Thing> used = new();

    public MapComponent_PhobosStandby(Map map) : base(map)
    {
    }

    public override void MapComponentTick()
    {
        if (!StandbyPowerRule.SweepDue(Find.TickManager.TicksGame, map.Index)) return;
        IReadOnlyList<CompPhobosStandby> comps = MapRegistry<CompPhobosStandby>.On(map);
        if (comps.Count == 0) return;
        LookAtPeople();
        int percent = FrameworkMod.Settings.standbyPercent;
        // Facilities last: they follow what they serve, settled first.
        for (int i = 0; i < comps.Count; i++)
        {
            if (comps[i].Kind != StandbyKind.Facility) comps[i].Settle(Wanted(comps[i]), percent);
        }
        for (int i = 0; i < comps.Count; i++)
        {
            if (comps[i].Kind == StandbyKind.Facility) comps[i].Settle(Wanted(comps[i]), percent);
        }
        roomsAwake.Clear();
        used.Clear();
    }

    /// <summary>
    /// Who is where and doing what: people not hostile to the colony (colonists, prisoners, slaves, guests and visitors), and animals when
    /// the setting says they light rooms. Mechanoids need no light and work at no bench. A sleeper lights nothing.
    /// </summary>
    void LookAtPeople()
    {
        roomsAwake.Clear();
        used.Clear();
        bool animalsLight = FrameworkMod.Settings.standbyAnimalsLight;
        Faction player = Faction.OfPlayer;
        IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
        for (int i = 0; i < pawns.Count; i++)
        {
            Pawn pawn = pawns[i];
            if (pawn.Dead || pawn.RaceProps == null) continue;
            bool person = pawn.RaceProps.Humanlike;
            if (!person && !(animalsLight && pawn.RaceProps.Animal)) continue;
            if (pawn.HostileTo(player)) continue;
            if (pawn.Awake())
            {
                Room? room = pawn.GetRoom();
                if (room != null) roomsAwake.Add(room);
            }
            if (!person || pawn.CurJob is not Job job) continue;
            bool watching = pawn.jobs?.curDriver is JobDriver_WatchBuilding;
            MarkUsed(pawn, job.targetA, watching);
            MarkUsed(pawn, job.targetB, watching);
        }
    }

    /// <summary>
    /// A building is in use when someone's job is at it and they are there: at its interaction spot where it has one, beside it otherwise.
    /// Someone watching a television counts from wherever they sit.
    /// </summary>
    void MarkUsed(Pawn pawn, LocalTargetInfo target, bool watching)
    {
        Thing? thing = target.Thing;
        if (thing == null || !thing.Spawned || thing.Map != map) return;
        bool there = watching
            || (thing.def.hasInteractionCell ? pawn.Position == thing.InteractionCell : thing.OccupiedRect().ExpandedBy(1).Contains(pawn.Position));
        if (there) used.Add(thing);
    }

    bool Wanted(CompPhobosStandby comp)
    {
        ThingWithComps building = comp.parent;
        StandbyKind kind = comp.Kind;
        if (!StandbyKinds.On(kind) || building.Faction != Faction.OfPlayer) return true;
        switch (kind)
        {
            case StandbyKind.Light:
                Room? room = LightsRoom(building);
                bool indoors = room != null && !room.PsychologicallyOutdoors;
                return StandbyPowerRule.LightWanted(true, indoors, room != null && roomsAwake.Contains(room));
            case StandbyKind.Machine:
            case StandbyKind.Watch:
                return used.Contains(building);
            case StandbyKind.Turret:
                return building is Building_Turret turret && turret.CurrentTarget.IsValid;
            case StandbyKind.Grower:
                if (building is not Building_PlantGrower grower) return true;
                foreach (Plant _ in grower.PlantsOnMe) return true;
                return false;
            case StandbyKind.Door:
                return building is Building_Door door && door.Open;
            case StandbyKind.Facility:
                return ServesSomethingInUse(comp);
            default:
                return true;
        }
    }

    /// <summary>The room a light lights: its own cell's, or for a light on a wall, the cell it faces.</summary>
    Room? LightsRoom(Thing light)
    {
        Room? room = light.GetRoom();
        if (room != null) return room;
        IntVec3 facing = light.Position + light.Rotation.FacingCell;
        return facing.InBounds(map) ? facing.GetRoom(map) : null;
    }

    /// <summary>A facility is in use while a bed it serves is occupied, or a building it serves is in use.</summary>
    bool ServesSomethingInUse(CompPhobosStandby comp)
    {
        List<Thing>? linked = comp.Facility?.LinkedBuildings;
        if (linked == null) return true;
        for (int i = 0; i < linked.Count; i++)
        {
            Thing served = linked[i];
            if (served is Building_Bed bed && bed.AnyOccupants) return true;
            if (used.Contains(served)) return true;
            if (served is ThingWithComps withComps
                && withComps.GetComp<CompPhobosStandby>() is { Kind: StandbyKind.Machine or StandbyKind.Watch, InUse: true })
            {
                return true;
            }
        }
        return false;
    }
}
