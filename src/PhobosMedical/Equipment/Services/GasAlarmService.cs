using System.Collections.Generic;
using Phobos.BurnsAndSmoke.Services;
using Phobos.Framework.Defs;
using Phobos.Framework.Gases;
using Phobos.Medical.Comps;
using Phobos.Medical.Defs;
using Phobos.Medical.Rules;
using RimWorld;
using Verse;

namespace Phobos.Medical.Services;

/// <summary>
/// Decides what an alarm senses and does what it does: a warning naming the room and what to do, and waking whoever sleeps in it. The
/// comp only calls this. Separate from the hydrogen detector planned with the vents (owner, 7 Nov 2026: two alarms, as in Ostranauts).
/// </summary>
public static class GasAlarmService
{
    public static AlarmGases Sense(CompPhobosGasAlarm alarm, AlarmGases previous)
    {
        Thing building = alarm.parent;
        if (!Working(alarm)) return AlarmGases.None;
        var rules = PhobosRulesDef.Current<PhobosMedicalRulesDef>();
        float carbonMonoxide = SmokeExposureService.CarbonMonoxideAt(building.Position, building.Map);
        float smoke = PhobosGases.Density(building.Position, building.Map, GasKind.Smoke);
        AlarmGases next = GasAlarmRule.Next(previous, carbonMonoxide, smoke, rules.alarmCarbonMonoxideLevel, rules.alarmSmokeLevel, rules.alarmQuietShare);
        if (GasAlarmRule.NewWarning(previous, next)) Warn(building, next);
        return next;
    }

    public static bool Working(CompPhobosGasAlarm alarm)
    {
        Thing building = alarm.parent;
        if (!building.Spawned || !FlickUtility.WantsToBeOn(building)) return false;
        CompPowerTrader? power = building.TryGetComp<CompPowerTrader>();
        return power == null || power.PowerOn;
    }

    static void Warn(Thing building, AlarmGases gases)
    {
        Room? room = building.GetRoom();
        string roomLabel = room?.GetRoomRoleLabel() ?? "";
        string key = gases switch
        {
            AlarmGases.CarbonMonoxide => "PhobosMedical_AlarmCarbonMonoxide",
            AlarmGases.Smoke => "PhobosMedical_AlarmSmoke",
            _ => "PhobosMedical_AlarmBoth",
        };
        int woken = WakeSleepers(building, room);
        string text = key.Translate(roomLabel.Named("ROOM")).Resolve();
        if (woken > 0) text += " " + "PhobosMedical_AlarmWoke".Translate(woken.Named("COUNT")).Resolve();
        Messages.Message(text, building, MessageTypeDefOf.ThreatSmall);
    }

    /// <summary>Wakes everyone asleep in the alarm's room (owner, 7 Nov 2026). Someone unconscious or downed can't be woken by noise.</summary>
    static int WakeSleepers(Thing building, Room? room)
    {
        if (room == null) return 0;
        int woken = 0;
        IReadOnlyList<Pawn> pawns = building.Map.mapPawns.AllPawnsSpawned;
        for (int i = 0; i < pawns.Count; i++)
        {
            Pawn pawn = pawns[i];
            if (pawn.Awake() || pawn.Downed || !pawn.health.capacities.CanBeAwake || pawn.GetRoom() != room) continue;
            RestUtility.WakeUp(pawn);
            woken++;
        }
        return woken;
    }
}
