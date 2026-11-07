using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Phobos.Framework;
using Phobos.MissilePatches.Rules;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Phobos.MissilePatches.Strike;

/// <summary>
/// RimPacts trims an ally's assault squad to the defenders still standing when it arrives (WorldComponent_RimPacts.TrimToHeadcount,
/// called with the reason "관전 공격대", its spectate assault; read in RimPacts 1.7, 7 Nov 2026). After one of our strategic strikes this
/// prefix raises the trim to the garrison the town had before the strike (<see cref="WorldComponent_PhobosStrikeMemory"/>), so the
/// ally faces the squad it would have faced without the missile (owner, 7 Nov 2026). Other RimPacts squads (defend waves, support
/// forces) and towns never struck are untouched. Reached by name, applied only while RimPacts is active; a prefix that changes one
/// argument and never skips the original. RimPacts can only remove pawns it generated, so the squad is never bigger than RimPacts
/// made it.
/// </summary>
public static class RimPactsAssaultSize
{
    const string RimPactsId = "wowgag.RimPacts";
    const string ComponentType = "RimPacts.WorldComponent_RimPacts";
    /// <summary>RimPacts' own reason string for its spectate assault squad, as it passes it.</summary>
    const string AssaultReason = "관전 공격대";

    static PropertyInfo? instance;
    static PropertyInfo? spectate;
    static FieldInfo? target;

    public static bool Applied { get; private set; }

    public static void Apply(Harmony harmony)
    {
        if (!OptionalMods.IsActive(RimPactsId)) return;
        Type? type = OptionalMods.TypeNamed(ComponentType);
        MethodInfo? trim = type?.GetMethod("TrimToHeadcount", BindingFlags.Public | BindingFlags.Static, null,
            new[] { typeof(List<Pawn>), typeof(int), typeof(Faction), typeof(string) }, null);
        instance = type?.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        spectate = type?.GetProperty("Spectate", BindingFlags.Public | BindingFlags.Instance);
        if (trim == null || instance == null || spectate == null)
        {
            Log.Warning(MissilePatchesMod.LogPrefix + " RimPacts' assault trimming has changed, so a struck town's assault is left to RimPacts.");
            return;
        }
        try
        {
            harmony.Patch(trim, prefix: new HarmonyMethod(typeof(RimPactsAssaultSize), nameof(Prefix)));
            Applied = true;
        }
        catch (Exception error)
        {
            Log.Warning(MissilePatchesMod.LogPrefix + " could not patch RimPacts' assault trimming: " + error.Message);
        }
    }

    /// <summary>Parameter names match RimPacts' method (pawns, targetCount, faction, why), which Harmony binds by name.</summary>
    public static void Prefix(ref int targetCount, Faction faction, string why)
    {
        try
        {
            targetCount = Adjust(targetCount, faction, why);
        }
        catch (Exception error)
        {
            Log.ErrorOnce(MissilePatchesMod.LogPrefix + " could not size RimPacts' assault; RimPacts' own size stands: " + error.Message, 0x50485321);
        }
    }

    /// <summary>The town an assault is aimed at, from RimPacts' current battle record.</summary>
    static Settlement? BattleTown()
    {
        object? component = instance?.GetValue(null);
        object? battle = component == null ? null : spectate?.GetValue(component);
        if (battle == null) return null;
        target ??= battle.GetType().GetField("target", BindingFlags.Public | BindingFlags.Instance);
        return target?.GetValue(battle) as Settlement;
    }

    internal static int Adjust(int targetCount, Faction? attacker, string? why)
    {
        if (why != AssaultReason) return targetCount;
        Settlement? town = BattleTown();
        WorldComponent_PhobosStrikeMemory? memory = WorldComponent_PhobosStrikeMemory.Current;
        if (town == null || memory == null) return targetCount;
        int? before = memory.DefendersBefore(town);
        if (before == null) return targetCount;
        float factor = AssaultSizeRule.TechFactor((int)(attacker?.def?.techLevel ?? TechLevel.Undefined), (int)(town.Faction?.def?.techLevel ?? TechLevel.Undefined));
        int adjusted = AssaultSizeRule.Target(targetCount, before, factor);
        if (adjusted != targetCount)
            Log.Message(MissilePatchesMod.LogPrefix + " " + (attacker?.Name ?? "An ally") + "'s assault on " + town.Label + " keeps "
                + (adjusted == AssaultSizeRule.NoTrim ? "every soldier it raised" : "up to " + adjusted + " soldiers")
                + " (RimPacts would have trimmed it to " + targetCount + ", the defenders the strike left standing).");
        return adjusted;
    }
}
