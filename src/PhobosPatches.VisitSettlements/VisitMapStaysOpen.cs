using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Phobos.Framework;
using Phobos.Framework.Rules;
using Phobos.VisitSettlementsPatches.Rules;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Phobos.VisitSettlementsPatches;

/// <summary>
/// Stops Visit Settlements closing a visited settlement's map while the player's people can still act there or a battle is fought on
/// it (<see cref="VisitMapRule"/>). Visit Settlements closes the map from its own postfix on the game's downing code, through its
/// private DeinitializeMap(Map), which calls the game's map removal directly and so skips every rule that would keep the map: the
/// game's own, RimPacts' for its defences and battles, ours for crew in vehicles.
///
/// The patch is a prefix on that one method that skips it when the map should stay. Recorded reason for skipping the original:
/// it has no extension point, and anything less than not running it loses the pawns on the map. When the prefix lets it run, Visit
/// Settlements behaves exactly as before. Everything is found by name: Visit Settlements' method, RimPacts' battle checks and the
/// crew of Vehicle Framework vehicles (their AllPawnsAboard property), so no other mod's assembly is referenced.
/// </summary>
public static class VisitMapStaysOpen
{
    const string OuterType = "VS_PenalizeForOpening";
    const string InnerType = "VS_Pawn_Downed_Patch";
    const string TargetMethod = "DeinitializeMap";
    const string RimPactsComponent = "RimPacts.WorldComponent_RimPacts";
    const int LogIntervalTicks = GenDate.TicksPerHour;

    static MethodInfo? target;
    static PropertyInfo? rimPactsInstance;
    static MethodInfo? defendOn, battleOn;
    static readonly Dictionary<Type, PropertyInfo?> aboardByType = new();
    static readonly Dictionary<int, int> lastLogTick = new();

    public static bool Ready => target != null;

    /// <summary>Finds Visit Settlements' method and, when present, RimPacts' battle checks. Returns why it could not, or null.</summary>
    public static string? Prepare()
    {
        Type? outer = OptionalMods.TypeNamed(OuterType);
        Type? inner = outer == null ? null : AccessTools.Inner(outer, InnerType);
        target = inner == null ? null : AccessTools.Method(inner, TargetMethod, new[] { typeof(Map) });
        Type? rimPacts = OptionalMods.TypeNamed(RimPactsComponent);
        rimPactsInstance = rimPacts?.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        defendOn = rimPacts == null ? null : AccessTools.Method(rimPacts, "DefendMissionOn", new[] { typeof(Settlement) });
        battleOn = rimPacts == null ? null : AccessTools.Method(rimPacts, "SpectateOn", new[] { typeof(Settlement) });
        return target == null ? "Visit Settlements' code has changed" : null;
    }

    public static void Apply(Harmony harmony)
    {
        if (target == null) return;
        harmony.Patch(target, prefix: new HarmonyMethod(AccessTools.Method(typeof(VisitMapStaysOpen), nameof(Prefix))));
    }

    /// <summary>Runs before Visit Settlements closes a map; false keeps the map open.</summary>
    public static bool Prefix(Map map)
    {
        try
        {
            KeepReason reason = Reason(map);
            if (reason == KeepReason.None) return true;
            Note(map, reason);
            return false;
        }
        catch (Exception error)
        {
            // Never stand between Visit Settlements and its own rule because of a fault of ours.
            Log.ErrorOnce(VisitSettlementsPatchesMod.LogPrefix + " could not check a visited map, so Visit Settlements' rule stood: " + error,
                0x5653_4b4d);
            return true;
        }
    }

    static KeepReason Reason(Map map)
    {
        if (map?.mapPawns == null) return KeepReason.None;
        int onFoot = 0, crew = 0, mechs = 0;
        foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
        {
            if (pawn.Dead) continue;
            if (pawn.IsFreeColonist && !pawn.Downed) onFoot++;
            else if (pawn.Faction == Faction.OfPlayer && pawn.RaceProps.IsMechanoid && !pawn.Downed) mechs++;
            if (pawn.Faction == Faction.OfPlayer) crew += ConsciousColonistsAboard(pawn);
        }
        foreach (Pawn pawn in map.mapPawns.AllPawnsUnspawned)
        {
            if (!pawn.Dead && !pawn.Downed && pawn.IsFreeColonist && pawn.SpawnedOrAnyParentSpawned) crew++;
        }
        return VisitMapRule.Decide(BattleOn(map), onFoot, crew, mechs);
    }

    /// <summary>Conscious free colonists aboard a Vehicle Framework vehicle, read through its AllPawnsAboard property.</summary>
    static int ConsciousColonistsAboard(Pawn pawn)
    {
        Type type = pawn.GetType();
        if (!aboardByType.TryGetValue(type, out PropertyInfo? aboard))
        {
            aboard = type.GetProperty("AllPawnsAboard", BindingFlags.Public | BindingFlags.Instance);
            aboardByType[type] = aboard;
        }
        if (aboard?.GetValue(pawn) is not IEnumerable list) return 0;
        int count = 0;
        foreach (object item in list)
        {
            if (item is Pawn p && !p.Dead && !p.Downed && p.IsFreeColonist) count++;
        }
        return count;
    }

    static bool BattleOn(Map map)
    {
        if (map.Parent is not Settlement settlement || rimPactsInstance == null) return false;
        object? component = rimPactsInstance.GetValue(null);
        if (component == null) return false;
        object[] args = { settlement };
        return (defendOn?.Invoke(component, args) is true) || (battleOn?.Invoke(component, args) is true);
    }

    static void Note(Map map, KeepReason reason)
    {
        int now = Find.TickManager.TicksGame;
        if (!TickThrottle.Due(now, lastLogTick.TryGetValue(map.uniqueID, out int last) ? last : TickThrottle.Never, LogIntervalTicks)) return;
        lastLogTick[map.uniqueID] = now;
        string why = reason switch
        {
            KeepReason.Battle => "a battle is being fought there",
            KeepReason.ColonistUp => "a colonist there is still on their feet",
            KeepReason.CrewUp => "colonists aboard a vehicle there are still conscious",
            _ => "a mechanoid of ours there is still on its feet",
        };
        Log.Message(VisitSettlementsPatchesMod.LogPrefix + " Kept the map of " + (map.Parent?.LabelCap ?? map.ToString())
            + " open: Visit Settlements would have closed it when a pawn went down, but " + why + ".");
    }
}
