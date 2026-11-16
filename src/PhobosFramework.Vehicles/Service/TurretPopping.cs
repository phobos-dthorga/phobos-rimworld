using System.Linq;
using Phobos.Framework.Defs;
using Phobos.Framework.Vehicles.Rules;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Vehicle Framework's own "turret blown off" reaction, following our cook-off setting (Framework 0.116.0; owner, 17 Nov 2026: "yes, with
/// cook-off"; docs/development/vehicle-knocked-out-systems.md, T8). Its base fires when its part is hit at or under a health share, with a
/// chance; this one stands down while ammunition cook-off is off, and can also be set off by a cook-off blast.
/// </summary>
public class Reactor_PhobosTurretPopper : Reactor_TurretPopper
{
    public override void Hit(VehiclePawn vehicle, VehicleComponent component, ref DamageInfo dinfo, VehicleComponent.Penetration penetration)
    {
        if (!CookOff.Allowed) return;
        base.Hit(vehicle, component, ref dinfo, penetration);
    }

    /// <summary>Sets it off now (a cook-off blast reached the ammunition rack). Vehicle Framework's own way to do this is internal.</summary>
    public void Detonate(VehiclePawn vehicle, VehicleComponent component)
    {
        if (CreateExploder(vehicle, component) is { } exploder) vehicle.AddTimedExplosion(exploder);
    }
}

/// <summary>
/// Gives a tank that stores its ammunition in a rack part (RimThunder's tanks) the turret-blown-off reaction on that part, aimed at its main
/// weapon (Framework 0.116.0, T8). The main weapon is the first turning weapon that is not hand-swung and has a mount requirement, which
/// Vehicle Framework's reaction needs (step 1 of 0.115.0 gives every free weapon one). At start-up, on the design; nothing is saved.
/// </summary>
public static class TurretPopping
{
    /// <summary>At start-up. Returns whether the design got the reaction.</summary>
    public static bool Apply(VehicleDef def, CompProperties_PhobosVehicleSystems props)
    {
        PhobosKnockOutRulesDef rules = PhobosRulesDef.Current<PhobosKnockOutRulesDef>();
        if (!rules.turretPopping) return false;
        VehicleComponentProperties? rack = def.components.FirstOrDefault(p => rules.ammoRackKeys.Any(k => string.Equals(p.key, k, System.StringComparison.OrdinalIgnoreCase)));
        if (rack == null) return false;
        VehicleTurret? main = def.GetCompProperties<CompProperties_VehicleTurrets>()?.turrets?.FirstOrDefault(t =>
            string.IsNullOrEmpty(t.parentKey) && t.component != null && t.def != null && t.def.turretType != TurretType.Static
            && !TurretPowerRule.HandSwung(t.key, rules.handSwungTurretKeys));
        if (main == null) return false;
        rack.reactors ??= new System.Collections.Generic.List<Reactor>();
        if (rack.reactors.Any(r => r is Reactor_TurretPopper)) return false;    // an author's own stands
        rack.reactors.Add(new Reactor_PhobosTurretPopper
        {
            turretKey = main.key,
            chance = rules.popChanceOnHit,
            healthPercent = rules.popAtOrBelowRackHealth,
            radius = rules.popRadius,
            damageDef = DefDatabase<DamageDef>.GetNamedSilentFail("Bomb"),
            indicator = DefDatabase<IndicatorDef>.GetNamedSilentFail("VF_Explosive"),
        });
        props.ammoRackKey = rack.key;
        return true;
    }

    /// <summary>After a cook-off blast in this vehicle: by chance, the ammunition rack goes up and the turret with it.</summary>
    public static void AfterCookOff(CompPhobosVehicleSystems comp)
    {
        string? key = comp.Props.ammoRackKey;
        if (key == null || comp.Vehicle?.statHandler?.GetComponent(key) is not { Health: > 0f } rack) return;
        if (rack.props.reactors?.OfType<Reactor_PhobosTurretPopper>().FirstOrDefault() is not { } popper) return;
        if (!Rand.Chance(PhobosRulesDef.Current<PhobosKnockOutRulesDef>().popChanceOnCookOff)) return;
        popper.Detonate(comp.Vehicle, rack);
    }
}
