using System.Collections.Generic;
using System.Linq;
using Phobos.Framework;
using Phobos.Framework.Rules;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Ammunition in a burning vehicle's cargo cooking off (cross-links round three, G13; Framework 0.113.0; owner, 13 Nov 2026: on by default,
/// only where Combat Extended's ammunition system is on, else off and the load line says why). Combat Extended already cooks off ammunition
/// lying in a fire on the ground (AmmoThing), but not ammunition stowed in a vehicle, which no fire reaches; this closes that gap with the
/// same ammunition CE marks as able to cook off or detonate. Each blast uses a share of that ammunition and is a game explosion sized by the
/// mass it uses (CookOffRule, figures in the rules Def). Runs on the rare tick for any faction's vehicle, our full tier and the lite tier
/// alike, and only while the vehicle burns; read nothing otherwise. The rule decides; this applies it.
/// </summary>
public static class CookOff
{
    static readonly float RareTickHours = GenTicks.TickRareInterval / (float)ElapsedRule.TicksPerHour;

    /// <summary>Whether cook-off can happen at all now: the player's setting and Combat Extended's ammunition system.</summary>
    public static bool AmmoSystemOn => CombatExtendedAmmo.SystemOn;

    static bool Enabled => VehicleSystemsMod.Settings.ammoCookOff && !VehicleSystemsMod.Settings.showOnly && CombatExtendedAmmo.SystemOn;

    /// <summary>The kilograms of ammunition aboard that can cook off.</summary>
    public static float AmmoKilograms(VehiclePawn vehicle)
    {
        ThingOwner? cargo = vehicle.inventory?.innerContainer;
        if (cargo == null) return 0f;
        float kilograms = 0f;
        foreach (Thing thing in cargo)
        {
            if (CombatExtendedAmmo.CooksOff(thing.def)) kilograms += thing.GetStatValue(StatDefOf.Mass) * thing.stackCount;
        }
        return kilograms;
    }

    /// <summary>Called on the vehicle's rare tick.</summary>
    public static void RareTick(CompPhobosVehicleSystems comp)
    {
        VehiclePawn vehicle = comp.Vehicle;
        if (vehicle == null || !vehicle.Spawned) return;
        float fire = comp.FireShare;
        if (fire <= 0f || !Enabled) return;
        float kilograms = AmmoKilograms(vehicle);
        PhobosVehicleRulesDef rules = PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();
        double mtb = CookOffRule.MeanHoursBetween(fire, kilograms, rules.cookOffMtbHours);
        if (double.IsInfinity(mtb) || !Rand.Chance((float)FailureRule.ChanceIn(RareTickHours, mtb))) return;
        Blast(comp, kilograms, rules);
    }

    static void Blast(CompPhobosVehicleSystems comp, float aboardKg, PhobosVehicleRulesDef rules)
    {
        VehiclePawn vehicle = comp.Vehicle;
        double used = CookOffRule.KilogramsPerBlast(aboardKg, rules.cookOffShare, rules.cookOffLeastKg);
        Consume(vehicle, used);
        float radius = (float)CookOffRule.Radius(used, rules.cookOffRadiusBase, rules.cookOffRadiusPerCubeRootKg, rules.cookOffRadiusMax);
        int damage = CookOffRule.Damage(used, rules.cookOffDamageBase, rules.cookOffDamagePerCubeRootKg, rules.cookOffDamageMax);
        if (vehicle.Faction == Faction.OfPlayer && comp.MayNotify(NoticeKey, rules.messageCooldownHours))
        {
            Messages.Message("PhobosVehicleSystems_MsgCookOff".Translate(vehicle.LabelShort.Named("VEHICLE")), vehicle, MessageTypeDefOf.ThreatSmall);
        }
        GenExplosion.DoExplosion(vehicle.Position, vehicle.Map, radius, DamageDefOf.Bomb, vehicle, damage,
            CombatExtendedRules.BlastPenetrationForDamage(damage, true));
    }

    const string NoticeKey = "PhobosCookOff";

    /// <summary>Takes <paramref name="kilograms"/> of cook-off-prone ammunition out of the cargo, stack by stack in random order, whole rounds only.</summary>
    static void Consume(VehiclePawn vehicle, double kilograms)
    {
        ThingOwner? cargo = vehicle.inventory?.innerContainer;
        if (cargo == null) return;
        List<Thing> stacks = cargo.Where(t => CombatExtendedAmmo.CooksOff(t.def)).InRandomOrder().ToList();
        double left = kilograms;
        foreach (Thing stack in stacks)
        {
            if (left <= 0) break;
            float each = Mathf.Max(0.001f, stack.GetStatValue(StatDefOf.Mass));
            int count = Mathf.Clamp(Mathf.CeilToInt((float)(left / each)), 1, stack.stackCount);
            left -= count * each;
            if (count >= stack.stackCount) stack.Destroy();
            else stack.SplitOff(count).Destroy();
        }
    }
}
