using System;
using System.Reflection;
using Phobos.Framework;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Phobos.MissilePatches.Strike;

/// <summary>
/// Dubs Rimatomics' fallout and radiation, by name only (no reference to its assembly; read from its 1.7 code and Defs, 7 Nov 2026):
/// the NuclearFallout game condition, its drifting Fallout world object (public fields conditionDef, lifeSpan, destinationTile, as its
/// own WorldObject_ICBMfission.MakeFallout sets them), the FlashBlindness hediff, and DubUtils.applyRads(Pawn, float), which scales by
/// Rimatomics' own radiation setting and the pawn's toxic resistance. "Existing effects first" (owner's rule): nothing here makes a
/// radiation model of our own.
/// </summary>
internal static class RimatomicsTieIn
{
    public const string PackageId = "Dubwise.Rimatomics";
    const string FalloutCondition = "NuclearFallout";
    const string CloudObject = "Fallout";
    const string FlashBlindnessHediff = "FlashBlindness";

    static bool looked;
    static Action<Pawn, float>? applyRads;

    public static bool Active => OptionalMods.IsActive(PackageId);

    public static GameConditionDef? Fallout => Active ? DefDatabase<GameConditionDef>.GetNamedSilentFail(FalloutCondition) : null;

    public static HediffDef? FlashBlindness => Active ? OptionalMods.HediffDefNamed(FlashBlindnessHediff) : null;

    public static bool CanIrradiate
    {
        get
        {
            Look();
            return applyRads != null;
        }
    }

    public static void Irradiate(Pawn pawn, float strength)
    {
        Look();
        applyRads?.Invoke(pawn, strength);
    }

    /// <summary>Rimatomics' fallout on a map, unless it already has it: true if registered.</summary>
    public static bool RegisterFallout(Map map, int ticks)
    {
        GameConditionDef? def = Fallout;
        if (def == null || map.GameConditionManager.ConditionIsActive(def)) return false;
        map.GameConditionManager.RegisterCondition(GameConditionMaker.MakeCondition(def, ticks));
        return true;
    }

    /// <summary>Drifting fallout clouds from a tile, as Rimatomics' own high-yield strike makes them. Returns how many were made.</summary>
    public static int SpawnClouds(PlanetTile tile, int count, IntRange lifespanDays)
    {
        WorldObjectDef? def = Active ? DefDatabase<WorldObjectDef>.GetNamedSilentFail(CloudObject) : null;
        if (def == null || count <= 0) return 0;
        int made = 0;
        for (int i = 0; i < count; i++)
        {
            if (!TileFinder.TryFindPassableTileWithTraversalDistance(tile, 1, 2, out PlanetTile destination)) continue;
            WorldObject cloud = WorldObjectMaker.MakeWorldObject(def);
            cloud.Tile = tile;
            Type type = cloud.GetType();
            FieldInfo? condition = type.GetField("conditionDef", BindingFlags.Instance | BindingFlags.Public);
            FieldInfo? lifeSpan = type.GetField("lifeSpan", BindingFlags.Instance | BindingFlags.Public);
            FieldInfo? target = type.GetField("destinationTile", BindingFlags.Instance | BindingFlags.Public);
            if (condition?.FieldType != typeof(string) || lifeSpan?.FieldType != typeof(int) || target?.FieldType != typeof(PlanetTile)) return made;
            condition.SetValue(cloud, FalloutCondition);
            lifeSpan.SetValue(cloud, GenDate.TicksPerDay * lifespanDays.RandomInRange);
            target.SetValue(cloud, destination);
            Find.WorldObjects.Add(cloud);
            made++;
        }
        return made;
    }

    static void Look()
    {
        if (looked) return;
        looked = true;
        if (!Active) return;
        MethodInfo? method = OptionalMods.TypeNamed("Rimatomics.DubUtils")?.GetMethod("applyRads", BindingFlags.Public | BindingFlags.Static,
            null, new[] { typeof(Pawn), typeof(float) }, null);
        if (method == null) return;
        try
        {
            applyRads = (Action<Pawn, float>)Delegate.CreateDelegate(typeof(Action<Pawn, float>), method);
        }
        catch (ArgumentException)
        {
            applyRads = null;
        }
    }
}
