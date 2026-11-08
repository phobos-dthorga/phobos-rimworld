using System.Collections.Generic;
using Phobos.BurnsAndSmoke.Defs;
using Phobos.Framework.Gases;
using RimWorld;
using Verse;

namespace Phobos.BurnsAndSmoke.Services;

/// <summary>
/// Plants in dense caustic gas wilt (owner, 8 Nov 2026; docs/development/caustic-gas.md): once an in-game hour, every plant standing
/// in tox gas at least the rules Def's density loses hit points in proportion to the density, and dies at none. Corpses are left
/// alone. Only while caustic gas and its plants setting are on, and only with Biotech (no tox gas without it). Nothing is saved.
/// </summary>
public sealed class MapComponent_PhobosCausticPlants : MapComponent
{
    static readonly List<Thing> plants = new();

    public MapComponent_PhobosCausticPlants(Map map) : base(map)
    {
    }

    public static bool Enabled => BurnsAndSmokeMod.Settings.causticGas && BurnsAndSmokeMod.Settings.causticPlants && ModsConfig.BiotechActive;

    public override void MapComponentTick()
    {
        PhobosBurnRulesDef rules = PhobosBurnRulesDef.Current;
        int interval = rules.causticPlantIntervalTicks > 0 ? rules.causticPlantIntervalTicks : 2500;
        if ((Find.TickManager.TicksGame + map.uniqueID * 97) % interval != 0 || !Enabled) return;
        Scorch(interval / 2500f, rules);
    }

    /// <summary>Scorches every plant in dense enough gas for the hours that passed; returns how many were hurt (for the dev log).</summary>
    public int Scorch(float hours, PhobosBurnRulesDef rules)
    {
        plants.Clear();
        plants.AddRange(map.listerThings.ThingsInGroup(ThingRequestGroup.Plant));
        int hurt = 0;
        try
        {
            foreach (Thing plant in plants)
            {
                if (plant.Destroyed || !plant.Spawned) continue;
                float density = PhobosGases.Density(plant.Position, map, GasKind.Toxic);
                if (density < rules.causticPlantMinDensity) continue;
                int damage = GenMath.RoundRandom(density * rules.causticPlantDamagePerHourAtFull * hours);
                if (damage <= 0) continue;
                plant.TakeDamage(new DamageInfo(BurnsDefOf.PhobosBurns_CausticScorch, damage));
                hurt++;
            }
        }
        finally
        {
            plants.Clear();
        }
        return hurt;
    }
}
