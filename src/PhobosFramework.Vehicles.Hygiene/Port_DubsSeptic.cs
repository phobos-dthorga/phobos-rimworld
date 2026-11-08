using System;
using DubsBadHygiene;
using Phobos.Framework.Hygiene;
using Verse;

namespace Phobos.VehicleSystems.Hygiene;

/// <summary>Pumps a septic tank beside the truck, one unit to one unit of Dubs Bad Hygiene's sewage. It only draws: the load goes back out as
/// barrels through the item port, at Dubs' own 25 units to a barrel.</summary>
public class Port_DubsSeptic : TankPort
{
    public override float Draw(CompPhobosTank tank, float most, out string? reason)
    {
        reason = null;
        if (PlumbingFeed.LiteMode) return 0f;
        foreach (Thing thing in Port_DubsPlumbing.ThingsBeside(tank))
        {
            CompSepticTank? septic = thing.TryGetComp<CompSepticTank>();
            if (septic == null || septic.sewageBuffer <= 0f) continue;
            float taken = Math.Min(most, septic.sewageBuffer);
            septic.sewageBuffer -= taken;
            return taken;
        }
        reason = "PhobosVehicleSystems_TankNoSeptic".Translate().Resolve();
        return 0f;
    }
}
