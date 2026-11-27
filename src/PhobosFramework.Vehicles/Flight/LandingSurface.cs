using System;
using System.Collections.Generic;
using Phobos.Framework.Defs;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// What the ground under an aircraft on our own flight does to it as it lifts off or touches down (Framework 0.158.0; the owner's decision of
/// 18 Nov 2026, docs/development/vehicle-osprey.md, "What it lands on"). Loose, dry soil or sand under it wears each engine a little, less
/// when rolling and much less behind an inlet barrier; dry growth under it may catch fire, more in a heat wave; floors, rock and wet or
/// snowy ground cost nothing. Loose ground is read from the terrain's own fields (natural soil or sand that takes footprints), never from
/// its name, so other mods' ground counts too. Works on any faction's aircraft; called by CompPhobosFlight at lift-off and touch-down. Saves
/// nothing, and never throws: a fault is logged once and the rule stands down until the game restarts.
/// </summary>
public static class LandingSurface
{
    static bool faulted;
    static readonly List<IntVec3> dryCells = new();

    /// <summary>The aircraft is leaving the ground, straight up or (<paramref name="rolling"/>) along a strip. A roll is judged as it starts,
    /// so one that is then stopped has still raised its dust.</summary>
    public static void OnTakeOff(VehiclePawn vehicle, bool rolling = false) => Judge(vehicle, rolling, "take-off");

    /// <summary>The aircraft has touched down, straight down or (<paramref name="rolling"/>) at the end of its landing roll, where it stopped.</summary>
    public static void OnTouchDown(VehiclePawn vehicle, bool rolling) => Judge(vehicle, rolling, "touch-down");

    static void Judge(VehiclePawn vehicle, bool rolling, string when)
    {
        // "Figures only" (the systems' setting): nothing happens to the vehicle, and nothing is set alight.
        if (faulted || VehicleSystemsMod.Settings.showOnly || vehicle == null || !vehicle.Spawned || vehicle.Map is not { } map) return;
        try
        {
            PhobosLandingSurfaceRulesDef rules = PhobosRulesDef.Current<PhobosLandingSurfaceRulesDef>();
            bool raining = map.weatherManager.RainRate > 0f;
            int cells = 0, loose = 0, rough = 0;
            dryCells.Clear();
            foreach (IntVec3 cell in vehicle.OccupiedRect())
            {
                if (!cell.InBounds(map)) continue;
                cells++;
                // Framework 0.252.0: stones and grit lie on any ground but a floor or water, wet or dry.
                TerrainDef under = cell.GetTerrain(map);
                if (!under.IsFloor && !under.IsWater) rough++;
                // Framework 0.204.0 (LM2): ground still wet after the rain counts as wet, neither dusty nor dry.
                bool wet = raining || Phobos.Framework.Environment.MapComponent_PhobosGroundWetness.At(map, cell) > rules.wetAbove;
                bool dry = LandingSurfaceRule.Dry(wet ? 1 : 0, map.snowGrid.GetDepth(cell), rules.snowAtMost);
                if (!dry) continue;
                TerrainDef terrain = cell.GetTerrain(map);
                if (Loose(terrain)) loose++;
                if (!terrain.extinguishesFire && cell.GetPlant(map) is { } plant && plant.def.BaseFlammability > 0f) dryCells.Add(cell);
            }
            if (cells == 0) return;
            WearEngines(vehicle, rules, (double)loose / cells, rolling, when);
            MaybeFire(vehicle, map, rules, (double)dryCells.Count / cells, when);
            StripDebris.OnEvent(vehicle, (double)rough / cells, when);
        }
        catch (Exception error)
        {
            faulted = true;
            VehicleSystemsMod.Logger.Error("could not judge the ground under an aircraft, so the ground costs nothing until the game restarts: " + error);
        }
        finally
        {
            dryCells.Clear();
        }
    }

    /// <summary>Natural soil or sand loose enough to take footprints: what the rotors throw up.</summary>
    static bool Loose(TerrainDef terrain) =>
        terrain.natural && terrain.takeFootprints
        && (terrain.categoryType == TerrainDef.TerrainCategoryType.Soil || terrain.categoryType == TerrainDef.TerrainCategoryType.Sand);

    static void WearEngines(VehiclePawn vehicle, PhobosLandingSurfaceRulesDef rules, double looseShare, bool rolling, string when)
    {
        if (CompPhobosVehicleSystems.Of(vehicle) is not { Usable: true } comp) return;
        bool barrier = vehicle.GetStatValue(AircraftUpgradeDefOf.Phobos_InletBarrierLoss) > 0f;
        double share = LandingSurfaceRule.EngineWearShare(looseShare, rolling, barrier, rules.engineWearPerEvent, rules.rollingFactor, rules.barrierStops);
        if (share <= 0) return;
        // Never below the rules' floor, which stands above the share at which the engine model counts an engine out (fault-read L2).
        foreach (string role in Powertrain.EngineRolesOf(comp)) comp.WearRole(role, 0f, (float)share, rules.engineWearFloor, shedsMetal: false);
        string variant = barrier ? "barrier" : "bare";
        if (VehicleSystemsMod.Logger.DebugEnabled)
            VehicleSystemsMod.Logger.Debug(vehicle.LabelShort + " " + when + " over " + looseShare.ToString("P0") + " loose ground" + (rolling ? ", rolling" : "")
                + (barrier ? ", behind its inlet barrier" : "") + ": each engine loses " + share.ToString("P2") + " of its health");
        if (Seen.Wants("aircraft.landing-dust", vehicle, variant))
            Seen.Note("aircraft.landing-dust", vehicle, when + (rolling ? " rolling" : ""), variant,
                new[] { new KeyValuePair<string, double>("loose", looseShare), new KeyValuePair<string, double>("wear", share) });
    }

    static void MaybeFire(VehiclePawn vehicle, Map map, PhobosLandingSurfaceRulesDef rules, double dryShare, string when)
    {
        if (dryCells.Count == 0) return;
        bool heatWave = GameConditionDefOf.HeatWave != null && map.gameConditionManager.ConditionIsActive(GameConditionDefOf.HeatWave);
        double chance = LandingSurfaceRule.FireChance(dryShare, heatWave, rules.fireChancePerEvent, rules.heatWaveFactor);
        if (chance <= 0 || !Rand.Chance((float)chance)) return;
        IntVec3 cell = dryCells.RandomElement();
        if (!FireUtility.TryStartFireIn(cell, map, rules.fireSize, vehicle)) return;
        if (VehicleSystemsMod.Logger.DebugEnabled)
            VehicleSystemsMod.Logger.Debug(vehicle.LabelShort + " " + when + ": its exhaust set dry growth alight at " + cell + " (chance " + chance.ToString("P1")
                + (heatWave ? ", in a heat wave" : "") + ")");
        string variant = heatWave ? "heat-wave" : "ordinary";
        if (Seen.Wants("aircraft.landing-fire", vehicle, variant))
            Seen.Note("aircraft.landing-fire", vehicle, when, variant, new[] { new KeyValuePair<string, double>("chance", chance) });
    }
}
