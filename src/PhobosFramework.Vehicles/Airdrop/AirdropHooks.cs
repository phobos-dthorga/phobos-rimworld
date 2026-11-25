using System.Collections.Generic;
using System.Linq;
using Phobos.Framework;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Fills the Framework core's hooks for other mods' air defence (Framework 0.297.0; docs/development/airdrop.md, "Air defence against
/// drops"): a parachute load of ours shot down lands on our own terms, and enemy aircraft in the air over a map can be named, checked and hit.
/// The tie-ins that call them live in D'thorga Patches.
/// </summary>
[StaticConstructorOnStartup]
public static class AirdropHooks
{
    static AirdropHooks()
    {
        ShotDownFallers.Handler = thing =>
        {
            if (thing is not PhobosParachuteFaller faller) return false;
            faller.ShootDown();
            return true;
        };
        AirTargets.Over = map => FlightRegistry.Over(map).Select(f => f.Vehicle).Where(Hostile).Cast<Thing>().ToList();
        AirTargets.IsHostileInAir = thing => thing is VehiclePawn v && FlightRegistry.InAir(v) && Hostile(v);
        AirTargets.Hit = (thing, amount) =>
        {
            if (thing is not VehiclePawn v || !v.Spawned) return;
            v.TakeDamage(new DamageInfo(DamageDefOf.Bullet, amount, armorPenetration: 0.5f));
            Seen.Note("airdrop.aircraft-hit", v);
        };
    }

    static bool Hostile(VehiclePawn v) => v.Faction != null && v.Faction.HostileTo(Faction.OfPlayer);
}
