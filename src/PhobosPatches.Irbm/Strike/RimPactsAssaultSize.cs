using System;
using System.Reflection;
using HarmonyLib;
using Phobos.Framework;
using Phobos.MissilePatches.Rules;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Phobos.MissilePatches.Strike;

/// <summary>
/// RimPacts sizes an ally's assault squad to the defenders it counts standing when the squad is raised
/// (WorldComponent_RimPacts.DefenderHeadcountOn, read in RimPacts 1.7, 7 Nov 2026). That one count caps the pawn generation, sets the
/// top-up target and sets the final trim, all times RimPacts' tech factor. After one of our strategic strikes this postfix raises the
/// count to the garrison the town had before the strike (<see cref="StrikeMemory"/>), so every one of those steps
/// runs as it would have without the missile and the ally faces the squad it normally would (owner, 7 Nov 2026).
///
/// Why the count and not the trim: the first version (Patches 0.11.0 draft) raised only the final trim. RimPacts had already capped the
/// generation to the post-strike count, so the owner's ally still arrived with two soldiers (owner's log, 7 Nov 2026, Lance Village).
///
/// Narrow on purpose: only while RimPacts is raising its spectate assault (its battle record says spawned but not done), only on that
/// battle's map and for its attacker, only for a town with a recent strike record, and never when nobody is standing (RimPacts then
/// ends the battle as an attack win, which stays). Defend waves, the arrival check and support forces see RimPacts' own count. A
/// postfix that only raises an int; reached by name and applied only while RimPacts is active.
/// </summary>
public static class RimPactsAssaultSize
{
    const string RimPactsId = "wowgag.RimPacts";
    const string ComponentType = "RimPacts.WorldComponent_RimPacts";

    static PropertyInfo? instance;
    static PropertyInfo? spectate;
    static FieldInfo? battleMap, battleAttacker, battleTarget, battleSpawned, battleSpawnDone;
    static int lastLoggedTown = -1;    // its ID: a Settlement here would be kept across a load
    static int lastLoggedTick = -1;

    public static bool Applied { get; private set; }

    public static void Apply(Harmony harmony)
    {
        if (!OptionalMods.IsActive(RimPactsId)) return;
        Type? type = OptionalMods.TypeNamed(ComponentType);
        MethodInfo? count = type?.GetMethod("DefenderHeadcountOn", BindingFlags.Public | BindingFlags.Static, null,
            new[] { typeof(Map), typeof(Faction), typeof(bool), typeof(int).MakeByRefType(), typeof(Faction).MakeByRefType() }, null);
        instance = type?.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        spectate = type?.GetProperty("Spectate", BindingFlags.Public | BindingFlags.Instance);
        Type? battle = spectate?.PropertyType;
        const BindingFlags field = BindingFlags.Public | BindingFlags.Instance;
        battleMap = battle?.GetField("map", field);
        battleAttacker = battle?.GetField("attacker", field);
        battleTarget = battle?.GetField("target", field);
        battleSpawned = battle?.GetField("spawned", field);
        battleSpawnDone = battle?.GetField("spawnDone", field);
        if (count == null || instance == null || battleMap == null || battleAttacker == null || battleTarget == null
            || battleSpawned == null || battleSpawnDone == null)
        {
            MissilePatchesMod.Logger.Warn("RimPacts' assault sizing has changed, so a struck town's assault is left to RimPacts.");
            return;
        }
        try
        {
            harmony.Patch(count, postfix: new HarmonyMethod(typeof(RimPactsAssaultSize), nameof(Postfix)));
            Applied = true;
        }
        catch (Exception error)
        {
            MissilePatchesMod.Logger.Warn("could not patch RimPacts' assault sizing: " + error.Message);
        }
    }

    /// <summary>Parameter names match RimPacts' method (map, attacker), which Harmony binds by name.</summary>
    public static void Postfix(ref int __result, Map map, Faction attacker)
    {
        if (__result <= 0) return;
        if (StrikeMemory.Empty) return;
        try
        {
            __result = Adjust(__result, map, attacker);
        }
        catch (Exception error)
        {
            MissilePatchesMod.Logger.ErrorOnce("could not size RimPacts' assault; RimPacts' own size stands: " + error.Message);
        }
    }

    static int Adjust(int standing, Map map, Faction attacker)
    {
        object? component = instance!.GetValue(null);
        object? battle = component == null ? null : spectate!.GetValue(component);
        if (battle == null) return standing;
        if (battleSpawned!.GetValue(battle) is not true || battleSpawnDone!.GetValue(battle) is true) return standing;
        if (battleMap!.GetValue(battle) != map || battleAttacker!.GetValue(battle) != attacker) return standing;
        if (battleTarget!.GetValue(battle) is not Settlement town) return standing;
        int adjusted = AssaultSizeRule.Headcount(standing, StrikeMemory.DefendersBefore(town));
        int now = Find.TickManager.TicksGame;
        if (adjusted != standing && (town.ID != lastLoggedTown || now != lastLoggedTick))
        {
            lastLoggedTown = town.ID;
            lastLoggedTick = now;
            MissilePatchesMod.Logger.Info((attacker?.Name ?? "An ally") + "'s assault on " + town.Label + " is sized against "
                + adjusted + " defenders, the garrison before the strike (RimPacts counted " + standing + " still standing).");
        }
        return adjusted;
    }
}
