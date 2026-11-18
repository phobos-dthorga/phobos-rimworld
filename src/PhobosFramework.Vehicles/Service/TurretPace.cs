using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Phobos.Framework.Defs;
using Phobos.Framework.Rules;
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
        /// <summary>The factor on the weapon's accuracy (Framework 0.119.0, T5 and T6): below 1 while it is cranked by hand on the move or in
        /// poor visibility.</summary>
        public float accuracy = 1f;
        public bool damaged, gunnerHurt, handCranked;
        /// <summary>An autonomous weapon (no seat works it) with no power and nobody aboard to crank it: out of action until it has power
        /// (Framework 0.120.0; owner, 17 Nov 2026). Read by the TurretPowerStop hooks.</summary>
        public bool stoppedNoPower;
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
        pace.traverse = pace.aim = pace.reload = pace.accuracy = 1f;
        pace.damaged = pace.gunnerHurt = pace.handCranked = pace.stoppedNoPower = false;
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

        TurretPower.Paces(comp, turret, out double powerTraverse, out double powerAim, out bool cranked);
        // An autonomous weapon with no power stops while nobody is aboard to crank it (Framework 0.120.0; owner, 17 Nov 2026: "stops until it
        // has power"); with someone aboard it is cranked by hand like any other (owner, 16 Nov 2026: hand-cranked, never dead).
        if (cranked && rules.autonomousStopsWithoutPower && !TurretPower.Claimed(turret) && vehicle.AllPawnsAboard.Count == 0)
        {
            pace.stoppedNoPower = true;
            cranked = false;
            // Framework 0.120.5 (fault-read G1): a stopped weapon drops its target. Vehicle Framework clears a target only while the weapon
            // works and looks for a new one only when it has none, so one kept here held the drive "manned" and drawing, and the weapon
            // never looked for a target again once power came back.
            if (turret.targetInfo.IsValid) turret.SetTarget(LocalTargetInfo.Invalid);
            Seen.Note("vehicles.autonomous-turret-no-power", vehicle, turret.key);
        }
        pace.handCranked = cranked;
        // T5 and T6 (Framework 0.119.0): cranked by hand, the turret has no stabiliser on the move and no powered sights in the dark.
        if (cranked && rules.crankedTurretsAimWorse)
        {
            bool poor = When_PoorVisibility.Poor(vehicle);
            // Read from the vehicle now (Framework 0.120.5, fault-read A9): the systems' own flag is set on the rare tick, a few seconds behind.
            bool moving = vehicle.vehiclePather is { Moving: true } || comp.Moving;
            pace.accuracy = (float)TurretAimRule.Factor(true, moving, poor, rules.noStabiliserAccuracy, rules.noSightsAccuracy);
            if (pace.accuracy < 1f && Seen.Wants("vehicles.cranked-turret-aims-worse", vehicle)) Seen.Note("vehicles.cranked-turret-aims-worse", vehicle, turret.key + (moving ? " moving" : "") + (poor ? " dark" : ""));
        }

        // Seen in play (Framework 0.118.0): what slowed or stopped this weapon, once a session each.
        if (mount != null && mount.HealthPercent <= rules.stopsAtOrBelow) Seen.Note("vehicles.weapon-stopped-by-mount", vehicle, turret.key);
        else if (pace.damaged) Seen.Note("vehicles.weapon-slowed-by-damage", vehicle, turret.key);
        if (pace.gunnerHurt) Seen.Note("vehicles.weapon-slowed-by-gunner", vehicle, turret.key);
        if (cranked) Seen.Note("vehicles.turret-hand-cranked", vehicle, turret.key);
        pace.traverse = (float)(damage * hands * powerTraverse);
        pace.aim = (float)(damage * sight * powerAim);
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
            if (pace.stoppedNoPower)
            {
                string stopped = turret.gizmoLabel.NullOrEmpty() ? turret.def.LabelCap.Resolve() : turret.gizmoLabel;
                slowed.Add("PhobosVehicleSystems_WeaponSlowedOne".Translate(stopped.Named("WEAPON"), "PhobosVehicleSystems_WeaponNoPowerStopped".Translate().Resolve().Named("WHY")).Resolve());
                continue;
            }
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

/// <summary>How a weapon is turned (Framework 0.117.0, for the Systems tab).</summary>
public enum WeaponDrive
{
    /// <summary>Nothing modelled: a fixed launcher, or a vehicle without a turret drive.</summary>
    None,
    /// <summary>A pintle or ring-mounted gun, swung by hand anyway.</summary>
    HandSwung,
    /// <summary>A powered drive with power.</summary>
    Powered,
    /// <summary>A powered drive without power, cranked by hand.</summary>
    HandCranked,
    /// <summary>Mounted on another weapon (a coaxial gun, a launcher on the turret's side): it turns with that one (Framework 0.117.1).</summary>
    TurnsWithTurret,
}

/// <summary>
/// A turret's power (Framework 0.115.0; owner, 16 Nov 2026; T3, T4): a powered turret is the electrical load "turret drive", drawing while
/// someone mans it; with that load shed or the battery flat it is cranked by hand, slowly, never dead. Pintle and ring-mounted machine guns
/// are swung by hand anyway, and a weapon riding on a turret either shares its drive or stands on its own hand-swung mount
/// (TurretPowerRule.Classify, Framework 0.117.2). The player's vehicles only (the owner's choice): other factions' batteries do not evolve.
/// </summary>
public static class TurretPower
{
    /// <summary>The traverse and aiming paces the drive's power allows this weapon now.</summary>
    public static void Paces(CompPhobosVehicleSystems comp, VehicleTurret turret, out double traverse, out double aim, out bool handCranked)
    {
        traverse = aim = 1;
        handCranked = false;
        PhobosKnockOutRulesDef rules = PhobosRulesDef.Current<PhobosKnockOutRulesDef>();
        if (!rules.turretPower || comp.Tier != DetailTier.Full || !OnPoweredDrive(turret)) return;
        ElectricalLoadSpec? drive = comp.Props.loadout?.loads.FirstOrDefault(l => l.name == rules.turretDriveLoad);
        if (drive == null) return;
        // Framework 0.117.1 (the owner's screenshot, 17 Nov 2026): the drive has power unless it is cut, switched off or without power. It
        // draws only while someone mans the gun, so "not drawing" (idle) still has power. Since 0.129.1 "without power" is the one rule
        // every load reads (check C1), which already counts a grid carrying the loads (fault-read G5) and a working alternator.
        LoadState state = comp.StateOf(drive);
        bool powered = state is LoadState.Running or LoadState.Idle;
        TurretPowerRule.Paces(powered, rules.handCrankTraversePace, rules.handCrankAimPace, out traverse, out aim);
        handCranked = !powered;
    }

    /// <summary>How a weapon is turned, for the Systems tab (Framework 0.117.0).</summary>
    public static WeaponDrive DriveOf(CompPhobosVehicleSystems comp, VehicleTurret turret)
    {
        PhobosKnockOutRulesDef rules = PhobosRulesDef.Current<PhobosKnockOutRulesDef>();
        if (!rules.turretPower || turret.def == null) return WeaponDrive.None;
        switch (Kind(turret))
        {
            case WeaponMountKind.Fixed: return WeaponDrive.None;
            case WeaponMountKind.HandSwung: return WeaponDrive.HandSwung;
            case WeaponMountKind.WithTurret: return WeaponDrive.TurnsWithTurret;
        }
        if (comp.Props.loadout?.loads.Any(l => l.name == rules.turretDriveLoad) != true) return WeaponDrive.None;
        return TurretPace.For(turret).handCranked ? WeaponDrive.HandCranked : WeaponDrive.Powered;
    }

    /// <summary>Whether the vehicle carries a weapon with a powered drive (and, with <paramref name="manned"/>, whether someone mans one).</summary>
    public static bool AnyPowered(VehiclePawn vehicle, bool manned)
    {
        PhobosKnockOutRulesDef rules = PhobosRulesDef.Current<PhobosKnockOutRulesDef>();
        if (!rules.turretPower) return false;
        var turrets = vehicle?.CompVehicleTurrets?.Turrets;
        if (turrets == null) return false;
        foreach (VehicleTurret turret in turrets)
        {
            if (Kind(turret) != WeaponMountKind.Powered) continue;
            if (!manned || Crewed(turret)) return true;
        }
        return false;
    }

    /// <summary>
    /// Whether this weapon's drive is working now, and so drawing (Framework 0.119.1 and 0.119.2; the owner's report of 17 Nov 2026: a
    /// parked, empty L-ATV's battery ran flat on the grid). Vehicle Framework's IsManned is "no seat that works this weapon stands empty",
    /// which is yes with nobody aboard for a weapon no seat claims: an autonomous weapon station, as its author means it (GTek's L-ATV:
    /// "capable of firing autonomously, without a gunner or even a driver"). Read as "someone mans it", that made such a turret's drive
    /// draw 40 A day and night, four times a grid's trickle charge.
    /// <list type="bullet">
    /// <item>A weapon a seat works: its drive draws while a person sits in such a seat.</item>
    /// <item>A weapon no seat works (autonomous): its drive draws while it has a target, tracking or firing. Standing watch with nothing
    /// in sight it draws nothing (an authored simplification: a real station's standby draw is small beside its drive's).</item>
    /// </list>
    /// </summary>
    /// <summary>Whether a seat works this weapon (its role names it); an autonomous weapon station has none (Framework 0.120.0).</summary>
    public static bool Claimed(VehicleTurret turret)
    {
        VehiclePawn? vehicle = turret.vehicle;
        if (vehicle?.handlers == null) return false;
        foreach (VehicleRoleHandler handler in vehicle.handlers)
        {
            VehicleRole? role = handler?.role;
            if (role != null && (role.HandlingTypes & HandlingType.Turret) != 0 && role.TurretIds != null && Works(role, turret)) return true;
        }
        return false;
    }

    public static bool Crewed(VehicleTurret turret)
    {
        VehiclePawn? vehicle = turret.vehicle;
        if (vehicle?.handlers == null) return false;
        bool claimed = false;
        foreach (VehicleRoleHandler handler in vehicle.handlers)
        {
            VehicleRole? role = handler?.role;
            if (handler == null || role == null || (role.HandlingTypes & HandlingType.Turret) == 0 || role.TurretIds == null || !Works(role, turret)) continue;
            claimed = true;
            if (handler.thingOwner is { Count: > 0 }) return true;
        }
        return !claimed && turret.targetInfo.IsValid;
    }

    /// <summary>Whether this weapon is turned by a powered drive: its own, or that of the turret it goes with.</summary>
    static bool OnPoweredDrive(VehicleTurret turret)
    {
        for (VehicleTurret? t = turret; t != null; t = t.attachedTo)
        {
            WeaponMountKind kind = Kind(t);
            if (kind != WeaponMountKind.WithTurret) return kind == WeaponMountKind.Powered;
        }
        return false;
    }

    /// <summary>How each design's weapons are mounted, worked out once per weapon (Framework 0.117.2). Derived, never saved.</summary>
    static readonly Dictionary<(VehicleDef, string), WeaponMountKind> kinds = new();

    /// <summary>How this weapon is mounted (TurretPowerRule.Classify).</summary>
    public static WeaponMountKind Kind(VehicleTurret turret)
    {
        VehicleDef? def = turret.vehicle?.VehicleDef;
        if (def == null || turret.def == null) return WeaponMountKind.Fixed;
        if (kinds.TryGetValue((def, turret.key), out WeaponMountKind kind)) return kind;
        PhobosKnockOutRulesDef rules = PhobosRulesDef.Current<PhobosKnockOutRulesDef>();
        VehicleTurret? parent = turret.attachedTo;
        kind = TurretPowerRule.Classify(turret.def.turretType == TurretType.Static, parent != null, turret.key ?? "", turret.def.defName,
            parent != null && SameCrew(turret, parent), rules.handSwungTurretKeys, rules.coaxialTurretKeys, rules.ownMountTurretKeys);
        // Kept only once the crew roles can be read: a weapon read before the vehicle has its crew handlers is worked out again later.
        if (parent == null || turret.vehicle?.handlers is { Count: > 0 }) kinds[(def, turret.key ?? "")] = kind;
        return kind;
    }

    /// <summary>Whether one crew role works both weapons (a coaxial gun is the main gunner's; a cupola gun is the commander's).</summary>
    static bool SameCrew(VehicleTurret turret, VehicleTurret parent)
    {
        if (turret.vehicle?.handlers == null) return true;
        foreach (VehicleRoleHandler handler in turret.vehicle.handlers)
        {
            VehicleRole? role = handler?.role;
            if (role?.TurretIds == null) continue;
            if (Works(role, turret) && Works(role, parent)) return true;
        }
        return false;
    }

    static bool Works(VehicleRole role, VehicleTurret turret) =>
        role.TurretIds.Contains(turret.key) || (turret.groupKey != null && role.TurretIds.Contains(turret.groupKey));
}
