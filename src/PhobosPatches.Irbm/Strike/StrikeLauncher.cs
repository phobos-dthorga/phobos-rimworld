using System.Collections.Generic;
using System.Linq;
using IRBM;
using Phobos.MissilePatches.Rules;
using Phobos.Framework.Work;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Phobos.MissilePatches.Strike;

/// <summary>
/// What happens when the screen turns white after a strategic warhead with a <see cref="PhobosStrikeExtension"/> lands: in place of
/// the missile mod's StrategicImpactCountdown.OnWhiteScreenReached, the same flash, sound, visuals, letter and repeat strikes, with
/// D'thorga's strike (<see cref="StrikeWork"/>) for the damage and fire, and fallout as the player chose. The missile mod's world-level
/// effects (settlements without a map destroyed, world pollution, the doomsday clock) happen before this and are untouched.
/// </summary>
public static class StrikeLauncher
{
    public static void Launch(WorldObject_Missile? missile, WorldObjectCompProperties_StrategicPayload props, PhobosStrikeExtension zones,
        IReadOnlyCollection<Map> struck, SoundDef? sound)
    {
        StrategicVisualManager.PerformStrategicFlash();
        (sound ?? IRBM_DefOf.IRBM_Sound_StrategicImpact)?.PlayOneShotOnCamera();
        List<Map> maps = struck.Where(map => map != null && map.Index >= 0).ToList();
        foreach (Map map in maps) IRBM_StrategicVisuals.Execute(map, props);

        Map? target = (missile == null ? null : maps.FirstOrDefault(map => map.Tile == missile.destinationTile)) ?? maps.FirstOrDefault();
        MissilePatchesSettings settings = MissilePatchesMod.Settings;
        PhobosStrikeRulesDef rules = PhobosStrikeRulesDef.Rules;
        bool rimatomicsFallout = settings.fallout == FalloutMode.Automatic && zones.fallout && RimatomicsTieIn.Fallout != null;
        Thing? launcher = missile?.launcher;
        string? label = missile?.FullDisplayName;

        foreach (Map map in maps)
        {
            bool isTarget = map == target;
            FrameWorkQueue.Enqueue(new StrikeWork(map, props, zones, isTarget || settings.fullStrikeEverywhere, isTarget, launcher, label));
            // No air to carry fallout in space (Rimatomics++ makes the same call): the missile mod's own conditions apply there instead.
            ApplyConditions(map, props, settings.fallout, rimatomicsFallout && !InSpace(map.Tile), rules);
            if (props.damageTimes > 1) QueueRepeats(map, props, launcher, label);
        }
        if (rimatomicsFallout && target != null && !InSpace(target.Tile) && settings.falloutClouds > 0)
        {
            int clouds = RimatomicsTieIn.SpawnClouds(target.Tile, settings.falloutClouds, rules.cloudLifespanDays);
            Log.Message(MissilePatchesMod.LogPrefix + " " + clouds + " fallout cloud" + (clouds == 1 ? "" : "s") + " drift from " + (target.Parent?.Label ?? "the strike") + ".");
        }
        SendLetter(missile, maps);
        if (target == null) ScreenFader.StartFade(Color.clear, IrbmBudgetRule.WhiteScreenSeconds);
    }

    static bool InSpace(PlanetTile tile) => tile.Valid && tile.LayerDef != null && tile.LayerDef.isSpace;

    /// <summary>Rimatomics' fallout in place of the missile mod's nuclear winter when it is active and chosen; the winter otherwise; neither for None.</summary>
    static void ApplyConditions(Map map, WorldObjectCompProperties_StrategicPayload props, FalloutMode mode, bool rimatomicsFallout, PhobosStrikeRulesDef rules)
    {
        if (mode == FalloutMode.None) return;
        if (rimatomicsFallout)
        {
            RimatomicsTieIn.RegisterFallout(map, Mathf.RoundToInt(rules.falloutDays.RandomInRange * GenDate.TicksPerDay));
            return;
        }
        if (!props.gameConditionConfigs.NullOrEmpty())
        {
            foreach (GameConditionConfig config in props.gameConditionConfigs)
            {
                if (config.conditionDef != null) map.gameConditionManager.RegisterCondition(GameConditionMaker.MakeCondition(config.conditionDef, config.durationTicks));
            }
        }
        if (props.forcedWeather != null) map.weatherManager.TransitionTo(props.forcedWeather);
    }

    /// <summary>The missile mod's repeat strikes, queued as it queues them; each one runs as the fast sweep.</summary>
    static void QueueRepeats(Map map, WorldObjectCompProperties_StrategicPayload props, Thing? launcher, string? label)
    {
        Current.Game.GetComponent<StrategicImpactManager>()?.RegisterTask(new StrategicImpactTask
        {
            map = map,
            remainingTimes = props.damageTimes - 1,
            nextTick = Find.TickManager.TicksGame + 60,
            damageDef = props.damageDef ?? DamageDefOf.Vaporize,
            damageAmount = props.damageAmount > 0f ? props.damageAmount : 10000f,
            armorPenetration = props.armorPenetration >= 0f ? props.armorPenetration : 200f,
            scanCategories = props.scanCategories,
            penetrateThickRoof = props.penetrateThickRoof,
            lockCorePart = props.lockCorePart,
            completeDismantle = props.completeDismantle,
            damageSourceLabel = label,
            launcher = launcher,
        });
    }

    /// <summary>The missile mod's own letter, with its own text.</summary>
    static void SendLetter(WorldObject_Missile? missile, List<Map> maps)
    {
        string places = string.Join(", ", maps.Select(map => map.Parent?.Label ?? "Unknown Area"));
        bool playerHit = maps.Any(map => map.Parent != null && map.Parent.Faction == Faction.OfPlayer);
        string unknown = "Unknown".Translate();
        TaggedString title;
        TaggedString text;
        LetterDef def;
        if (missile != null && missile.Faction == Faction.OfPlayer)
        {
            title = "IRBM.LetterLabelStrategicImpact".Translate();
            text = "IRBM.LetterTextStrategicImpact".Translate(missile.Label, places);
            def = LetterDefOf.NeutralEvent;
        }
        else if (playerHit)
        {
            title = "IRBM.LetterLabelSettlementVaporized".Translate();
            text = "IRBM.LetterTextSettlementVaporizedAllDead".Translate(missile?.Faction?.Name ?? unknown, places);
            def = LetterDefOf.ThreatBig;
        }
        else
        {
            title = "IRBM.LetterLabelStrategicImpact".Translate();
            text = "IRBM.LetterTextStrategicImpactThirdParty".Translate(missile?.Faction?.Name ?? unknown, places);
            def = LetterDefOf.NeutralEvent;
        }
        Find.LetterStack.ReceiveLetter(title, text, def, missile != null ? new GlobalTargetInfo(missile.destinationTile) : GlobalTargetInfo.Invalid);
    }
}
