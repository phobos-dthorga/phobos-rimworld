using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Phobos.Framework.Defs;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// How fast one weapon turns, aims and reloads now (Framework 0.115.0; owner, 16 and 17 Nov 2026; docs/development/vehicle-knocked-out-systems.md,
/// T2 and C4, and T3 for power): its mount's damage, its gunner's sight and hands, and whether its drive has power. A turret's figures come
/// from its Def, shared by every vehicle that mounts it, so the hooks in TurretPacePatches.cs scale them per weapon instead.
/// Each weapon's pace is worked out at most once a second and kept, so the hooks cost a table lookup. Any faction's weapons, on vehicles
/// with our systems; nothing is saved.
/// </summary>
public static class TurretPace
{
    const int RecheckTicks = GenTicks.TicksPerRealSecond;

    /// <summary>One weapon's paces, and why they are short of full.</summary>
    public sealed class Pace
    {
        public int tick = -RecheckTicks;
        public float traverse = 1f, aim = 1f, reload = 1f;
        public bool damaged, gunnerHurt, handCranked;
    }

    static readonly ConditionalWeakTable<VehicleTurret, Pace> paces = new();
    static readonly Pace Full = new();
    static bool faulted;

    /// <summary>The weapon's paces now (full where none applies, or after a fault).</summary>
    public static Pace For(VehicleTurret turret)
    {
        if (faulted || turret?.vehicle == null) return Full;
        Pace pace = paces.GetOrCreateValue(turret);
        int now = Find.TickManager?.TicksGame ?? 0;
        if (now - pace.tick < RecheckTicks) return pace;
        pace.tick = now;
        try
        {
            Work(turret, pace);
        }
        catch (System.Exception error)
        {
            faulted = true;
            VehicleSystemsMod.Logger.Error("could not work out a weapon's pace, so every weapon works at full pace until the game restarts: " + error);
            return Full;
        }
        return pace;
    }

    static void Work(VehicleTurret turret, Pace pace)
    {
        pace.traverse = pace.aim = pace.reload = 1f;
        pace.damaged = pace.gunnerHurt = pace.handCranked = false;
        VehiclePawn vehicle = turret.vehicle;
        CompPhobosVehicleSystems? comp = vehicle.GetComp<CompPhobosVehicleSystems>();
        if (comp == null || !comp.Usable) return;
        PhobosKnockOutRulesDef rules = PhobosRulesDef.Current<PhobosKnockOutRulesDef>();

        double damage = 1;
        VehicleComponent? mount = Mount(turret);
        if (rules.damageSlowsWeapons && mount != null)
        {
            damage = TurretPaceRule.DamagePace(mount.HealthPercent, rules.stopsAtOrBelow, rules.damagedMountPace);
            pace.damaged = damage < 1;
        }

        double sight = 1, hands = 1;
        if (rules.gunnerConditionSlowsWeapons)
        {
            BestGunner(vehicle, turret, out double? bestSight, out double? bestHands);
            sight = TurretPaceRule.CrewPace(bestSight, rules.hurtGunnerPaceFloor);
            hands = TurretPaceRule.CrewPace(bestHands, rules.hurtGunnerPaceFloor);
            pace.gunnerHurt = sight < 1 || hands < 1;
        }

        double power = TurretPower.Pace(comp, turret, out bool cranked);
        pace.handCranked = cranked;

        pace.traverse = (float)(damage * hands * power);
        pace.aim = (float)(damage * sight * power);
        pace.reload = (float)(damage * hands);
    }

    /// <summary>The part this weapon stops with: its own requirement's, else the one it is mounted on.</summary>
    public static VehicleComponent? Mount(VehicleTurret turret)
    {
        for (VehicleTurret? t = turret; t != null; t = t.attachedTo)
        {
            if (t.component?.Component is { } part) return part;
        }
        return null;
    }

    /// <summary>The best sight and hands among those working this weapon, or null where nobody is.</summary>
    static void BestGunner(VehiclePawn vehicle, VehicleTurret turret, out double? sight, out double? hands)
    {
        sight = null;
        hands = null;
        if (vehicle.handlers == null) return;
        foreach (VehicleRoleHandler handler in vehicle.handlers)
        {
            VehicleRole? role = handler?.role;
            if (handler == null || role == null || (role.HandlingTypes & HandlingType.Turret) == 0 || role.TurretIds == null) continue;
            if (!role.TurretIds.Contains(turret.key) && (turret.groupKey == null || !role.TurretIds.Contains(turret.groupKey))) continue;
            foreach (Pawn pawn in handler.thingOwner)
            {
                if (pawn?.health?.capacities == null) continue;
                double s = pawn.health.capacities.GetLevel(PawnCapacityDefOf.Sight);
                double h = pawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation);
                if (sight == null || s > sight) sight = s;
                if (hands == null || h > hands) hands = h;
            }
        }
    }

    /// <summary>"Weapons slowed: main turret (turret ring damaged, gunner hurt)", for the inspect pane; null when every weapon is at full pace.</summary>
    public static string? Line(VehiclePawn vehicle)
    {
        var turrets = vehicle.CompVehicleTurrets?.Turrets;
        if (turrets == null || turrets.Count == 0) return null;
        var slowed = new List<string>();
        foreach (VehicleTurret turret in turrets)
        {
            if (turret.def == null || turret.def.turretType == TurretType.Static || turret.ComponentDisabled) continue;
            Pace pace = For(turret);
            if (!pace.damaged && !pace.gunnerHurt && !pace.handCranked) continue;
            var why = new List<string>();
            if (pace.handCranked) why.Add("PhobosVehicleSystems_WeaponHandCranked".Translate().Resolve());
            if (pace.damaged && Mount(turret) is { } mount) why.Add("PhobosVehicleSystems_WeaponMountDamaged".Translate((mount.props.label ?? mount.props.key).Named("PART")).Resolve());
            if (pace.gunnerHurt) why.Add("PhobosVehicleSystems_WeaponGunnerHurt".Translate().Resolve());
            string name = turret.gizmoLabel.NullOrEmpty() ? turret.def.LabelCap.Resolve() : turret.gizmoLabel;
            slowed.Add("PhobosVehicleSystems_WeaponSlowedOne".Translate(name.Named("WEAPON"), string.Join(", ", why).Named("WHY")).Resolve());
        }
        return slowed.Count == 0 ? null : "PhobosVehicleSystems_InspectWeaponsSlowed".Translate(string.Join("; ", slowed).Named("WEAPONS")).Resolve();
    }
}

/// <summary>A turret drive's power (step 5 fills this in; until then every drive has full power).</summary>
public static class TurretPower
{
    public static double Pace(CompPhobosVehicleSystems comp, VehicleTurret turret, out bool handCranked)
    {
        handCranked = false;
        return 1;
    }
}
