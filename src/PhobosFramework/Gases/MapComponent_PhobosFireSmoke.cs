using System.Collections.Generic;
using Phobos.Framework.Rules;
using RimWorld;
using Verse;

namespace Phobos.Framework.Gases;

/// <summary>
/// What fires put into the air. The game's fires only throw smoke particles; Combat Extended's make black smoke under a roof.
/// So, every pass: when Combat Extended's smoke is not in use, each roofed fire adds the game's own smoke to its cell (which then
/// spreads through open doors, fades slower under a roof, and penalises aim and sight as real smoke would); and on either
/// path each roofed fire adds the framework's carbon monoxide to its room, if the rules name that gas and the owner's rule
/// says it is ours to carry.
/// </summary>
public sealed class MapComponent_PhobosFireSmoke : MapComponent
{
    public MapComponent_PhobosFireSmoke(Map map) : base(map) { }

    public override void MapComponentTick()
    {
        if (Find.TickManager.TicksGame % GasMixRule.PassTicks != 0) return;
        List<Thing> fires = map.listerThings.ThingsOfDef(ThingDefOf.Fire);
        if (fires.Count == 0) return;
        PhobosGasRulesDef rules = PhobosGasRulesDef.Current;
        bool vanillaSmoke = !PhobosGases.UsesCombatExtendedSmoke;
        PhobosGasDef? co = rules.fireCarbonMonoxideGas;
        bool ownCo = co != null && PhobosGases.Source(co) == GasSource.Own;
        if (!vanillaSmoke && !ownCo) return;
        MapComponent_PhobosRoomGases? rooms = ownCo ? MapComponent_PhobosRoomGases.For(map) : null;
        for (int i = 0; i < fires.Count; i++)
        {
            if (fires[i] is not Fire fire || !fire.Spawned) continue;
            bool roofed = fire.Position.Roofed(map);
            if (!roofed) continue;
            if (vanillaSmoke)
            {
                float smoke = FireSmokeRule.Amount(fire.fireSize, roofed, rules.fireSmokePerSizePerPass, rules.fireSmokeMaxPerPass);
                if (smoke > 0f) PhobosGases.Add(fire.Position, map, GasKind.Smoke, smoke);
            }
            if (ownCo && rooms != null)
            {
                float cells = FireSmokeRule.Amount(fire.fireSize, roofed, rules.fireCarbonMonoxidePerSizePerPass, rules.fireCarbonMonoxideMaxPerPass);
                if (cells > 0f) rooms.AddGas(fire.Position, co!, cells);
            }
        }
    }
}
