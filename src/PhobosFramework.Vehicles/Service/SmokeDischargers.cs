using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using SmashTools;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

// Smoke dischargers (Framework 0.319.0; docs/development/vehicle-militarised-service-vehicles.md, "Smoke dischargers"; the owner's cards of
// 26 Nov 2026). The dischargers themselves are Vehicle Framework's fixed ("Static") turrets, one bank a side under one group key, so one button
// fires both: each lobs its salvo a set distance along its own angle. What this file adds is the marker, the "Pop smoke when hit" switch,
// and other factions' trucks popping smoke too, since Vehicle Framework never fires a fixed turret for anyone but the player.

/// <summary>
/// Marks a turret Def as a smoke discharger of ours (Framework 0.319.0): a vehicle that can carry one gets <see cref="CompPhobosSmoke"/>, which
/// pops it when the vehicle is hit under threat. <see cref="switchIcon"/> is the switch's button picture, falling back to ours.
/// </summary>
public class PhobosSmokeDischargerExtension : DefModExtension
{
    public string? switchIcon;
}

/// <summary>Which turrets are smoke dischargers, which vehicles can carry them, and firing them.</summary>
public static class SmokeDischargers
{
    public static bool Is(VehicleTurretDef? def) => def?.HasModExtension<PhobosSmokeDischargerExtension>() == true;

    /// <summary>
    /// Whether a vehicle Def can carry dischargers: its own turrets, or a turret any node of its upgrade tree fits. Read once at start-up, when
    /// the comps are given out.
    /// </summary>
    public static bool Carries(VehicleDef def)
    {
        if (def.comps == null) return false;
        foreach (CompProperties props in def.comps)
        {
            if (props is CompProperties_VehicleTurrets { turrets: { } own } && own.Any(t => Is(t.def))) return true;
            if (props is not CompProperties_UpgradeTree { def.nodes: { } nodes }) continue;
            foreach (UpgradeNode node in nodes)
            {
                if (node.upgrades == null) continue;
                foreach (Upgrade upgrade in node.upgrades)
                {
                    if (upgrade is TurretUpgrade { turrets: { } fitted } && fitted.Any(t => Is(t.def))) return true;
                }
            }
        }
        return false;
    }

    /// <summary>The dischargers a vehicle has fitted now.</summary>
    public static IEnumerable<VehicleTurret> Of(VehiclePawn vehicle)
    {
        var turrets = vehicle.CompVehicleTurrets?.Turrets;
        if (turrets == null) yield break;
        foreach (VehicleTurret turret in turrets) if (Is(turret.def)) yield return turret;
    }

    /// <summary>Whether any fitted discharger could fire now: loaded, reloaded and working.</summary>
    public static bool AnyReady(VehiclePawn vehicle, bool loadIfEmpty) =>
        Of(vehicle).Any(t => !t.TurretDisabled && t.ReloadTicks <= 0 && (t.HasAmmo || loadIfEmpty));

    /// <summary>
    /// Fires every discharger that can, as Vehicle Framework's own button does: each at the point its full range away along its own angle.
    /// <paramref name="loadIfEmpty"/> loads an empty one for this salvo first (another faction's truck, which carries no chemfuel to reload).
    /// Returns how many fired.
    /// </summary>
    public static int Fire(VehiclePawn vehicle, bool loadIfEmpty)
    {
        int fired = 0;
        foreach (VehicleTurret turret in Of(vehicle).ToList())
        {
            if (turret.TurretDisabled || turret.ReloadTicks > 0) continue;
            if (!turret.HasAmmo && loadIfEmpty) Load(turret);
            if (!turret.HasAmmo) continue;
            Vector3 at = turret.TurretLocation.PointFromAngle(turret.MaxRange, turret.TurretRotation);
            turret.SetTarget(new LocalTargetInfo(at.ToIntVec3()));
            turret.PushTurretToQueue();
            turret.ResetPrefireTimer();
            fired++;
        }
        return fired;
    }

    /// <summary>A full magazine of the first round its ammunition allows (chemfuel, or with Combat Extended its smoke grenade), for one salvo.</summary>
    static void Load(VehicleTurret turret)
    {
        if (turret.def.ammunition != null && turret.loadedAmmo == null) turret.loadedAmmo = turret.def.ammunition.AllowedThingDefs.FirstOrDefault();
        turret.shellCount = turret.def.magazineCapacity;
    }
}

/// <summary>
/// Pop smoke when hit (Framework 0.319.0). D'thorga Framework gives it at start-up to any vehicle whose own turrets or upgrade tree carry a
/// turret marked <see cref="PhobosSmokeDischargerExtension"/>; it needs no fields.
/// </summary>
public class CompProperties_PhobosSmoke : CompProperties
{
    public CompProperties_PhobosSmoke() => compClass = typeof(CompPhobosSmoke);
}

/// <summary>
/// Pops smoke when the vehicle is hit while enemies are a threat on its map (Framework 0.319.0; the owner's card of 26 Nov 2026, "Switch, on;
/// raiders too"): the player's with its switch on (on by default), another faction's once a visit, loaded for that one salvo. Decided by
/// <see cref="SmokeRule"/>.
/// <para>Cost: nothing ticks. Vehicle Framework's "damage taken" event (raised under Combat Extended too) asks it; it reads a few fields, then
/// the map's threat list only when a discharger is ready.</para>
/// </summary>
public sealed class CompPhobosSmoke : VehicleComp
{
    bool popWhenHit = true;
    int lastPopTick = -100000;
    bool otherPoppedThisVisit;
    bool registered;

    public bool PopWhenHit => popWhenHit;

    /// <summary>Turns "Pop smoke when hit" on or off (the player's switch on the truck).</summary>
    public void SetPopWhenHit(bool on) => popWhenHit = on;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        // A visit is a spawn on a map: another faction's truck comes back loaded for one salvo.
        if (!respawningAfterLoad) otherPoppedThisVisit = false;
    }

    public override void EventRegistration()
    {
        base.EventRegistration();
        if (registered) return;
        registered = true;
        Vehicle.AddEvent(VehicleEventDefOf.DamageTaken, OnHit);
    }

    void OnHit()
    {
        VehiclePawn vehicle = Vehicle;
        if (!vehicle.Spawned || vehicle.Faction == null) return;
        bool players = vehicle.Faction == Faction.OfPlayer;
        if (players && !popWhenHit) return;
        int now = Find.TickManager.TicksGame;
        if (!SmokeDischargers.AnyReady(vehicle, loadIfEmpty: !players)) return;
        bool threat = GenHostility.AnyHostileActiveThreatTo(vehicle.Map, vehicle.Faction);
        if (!SmokeRule.PopsWhenHit(players, popWhenHit, threat, now - lastPopTick, otherPoppedThisVisit)) return;
        int fired = SmokeDischargers.Fire(vehicle, loadIfEmpty: !players);
        if (fired == 0) return;
        lastPopTick = now;
        if (!players) otherPoppedThisVisit = true;
        string variant = players ? "player" : "other";
        if (Seen.Wants("vehicles.smoke-pop", vehicle, variant)) Seen.Note("vehicles.smoke-pop", vehicle, vehicle.Faction.Name, variant);
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        if (Vehicle.Faction != Faction.OfPlayer) yield break;
        VehicleTurret? first = SmokeDischargers.Of(Vehicle).FirstOrDefault();
        if (first == null) yield break;
        yield return new Command_Toggle
        {
            defaultLabel = "PhobosSmoke_PopWhenHit".Translate(),
            defaultDesc = "PhobosSmoke_PopWhenHitDesc".Translate(SmokeRule.PopGapTicks.ToStringTicksToPeriod().Named("GAP")),
            icon = SystemIcons.For(first.def.GetModExtension<PhobosSmokeDischargerExtension>()?.switchIcon),
            isActive = () => popWhenHit,
            toggleAction = () => SetPopWhenHit(!popWhenHit),
        };
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref popWhenHit, "PhobosSmoke_PopWhenHit", true);
        Scribe_Values.Look(ref lastPopTick, "PhobosSmoke_LastPopTick", -100000);
        Scribe_Values.Look(ref otherPoppedThisVisit, "PhobosSmoke_OtherPopped");
    }
}
