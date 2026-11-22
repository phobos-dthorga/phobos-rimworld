using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Defs;
using Phobos.Framework.Environment;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// How a large blast reaches a vehicle (Framework 0.218.0; docs/development/nuclear-strike-effects.md, NK1 to NK5, NK8 and NK9; owner,
/// 22 Nov 2026: "as one piece", graded with the strike and on). Registered with the Framework's large-blast hook for every vehicle, any
/// faction's. Until now a strike treated a vehicle as a body: one hit, which Combat Extended's armour step turns into nothing for a blast
/// of no penetration, and Vehicle Framework puts on one cell. Here the blast acts on the machine and on the people inside it:
/// - in the fireball, the people aboard die and the vehicle is gone (NK1);
/// - in the blast zone and beyond, the people aboard are thrown about, then the vehicle is wrecked or battered by its weight and its
///   distance (NK2, NK8), its soft parts burn (NK3), and a running engine takes the pulse (NK4);
/// - the people still aboard are handed back with the share of the radiation dose the hull lets through (NK9), for the blast to give;
/// - a vehicle counts as sheltered only when every cell it stands on is (NK5).
/// Parts lose health directly, outside any armour step, as a blast wind acts on the whole machine; then the vehicle hears it took damage,
/// as it does from Combat Extended's own part step. No reactor is called: a blast is not a round, and spall's would throw at it.
/// </summary>
[StaticConstructorOnStartup]
public sealed class VehicleBlasts : ILargeBlastTaker
{
    static readonly VehicleBlasts instance = new();

    static VehicleBlasts() => LargeBlasts.Register(instance);

    static PhobosVehicleBlastRulesDef Rules => PhobosRulesDef.Current<PhobosVehicleBlastRulesDef>();

    /// <summary>Every vehicle, unless the vehicle systems are set to show figures only, when the blast treats it as before.</summary>
    public bool Takes(Thing thing) => thing is VehiclePawn && !VehicleSystemsMod.Settings.showOnly;

    public bool Sheltered(Thing thing, Func<IntVec3, bool> cellSheltered)
    {
        foreach (IntVec3 cell in ((VehiclePawn)thing).OccupiedRect())
        {
            if (!cellSheltered(cell)) return false;
        }
        return true;
    }

    public LargeBlastOutcome Take(Thing thing, LargeBlastHit hit)
    {
        var outcome = new LargeBlastOutcome();
        var vehicle = (VehiclePawn)thing;
        if (vehicle.Destroyed) return outcome;
        List<Pawn> aboard = vehicle.AllPawnsAboard.ToList();
        if (hit.Band == LargeBlastBand.Fireball)
        {
            Vanish(vehicle, aboard, hit, outcome);
            return outcome;
        }

        PhobosVehicleBlastRulesDef rules = Rules;
        VehicleBlastFigures f = rules.Figures;
        CompPhobosVehicleSystems? comp = vehicle.GetComp<CompPhobosVehicleSystems>();
        double tonnes = Tonnes(vehicle, comp);
        double severity = VehicleBlastRule.Severity(hit.Wind, tonnes, f);
        VehicleBlastFate fate = VehicleBlastRule.Fate(severity, f);
        // The dose arrives within the first second, before the blast wind; the hull's share is the vehicle's as it stood.
        float doseShare = (float)VehicleBlastRule.HullShare(comp?.ClosedCab ?? true, tonnes, f);

        HurtCrew(vehicle, aboard, severity, f, hit, outcome);
        if (fate == VehicleBlastFate.Wrecked)
        {
            outcome.Wrecked = true;
            if (Seen.Wants("vehicles.large-blast", vehicle, "wrecked"))
            {
                Seen.Note("vehicles.large-blast", vehicle, "wrecked at severity " + severity.ToString("0.00"), "wrecked",
                    new[] { new KeyValuePair<string, double>("severity", severity), new KeyValuePair<string, double>("tonnes", tonnes) });
            }
            // Vehicle Framework puts out whoever is still alive, and our hulls rule leaves the wreck where the setting says.
            vehicle.Kill(BlastDamage(hit), DestroyMode.KillFinalize);
        }
        else
        {
            // Glass breaks first and furthest (NK2, NK3; Framework 0.238.0, VG3): all of it from a quarter of the fireball edge's wind, cracked
            // beyond (authored).
            Glass.Strike(vehicle, Math.Max(hit.Wind * 4f, hit.Heat));
            bool battered = fate == VehicleBlastFate.Battered && Batter(vehicle, comp, severity, f);
            bool scorched = Scorch(vehicle, comp, rules, hit.Heat, f);
            if (battered || scorched)
            {
                outcome.Battered = true;
                if (Seen.Wants("vehicles.large-blast", vehicle, "battered"))
                {
                    Seen.Note("vehicles.large-blast", vehicle, "battered at severity " + severity.ToString("0.00") + (scorched ? ", soft parts burned" : ""),
                        "battered", new[] { new KeyValuePair<string, double>("severity", severity), new KeyValuePair<string, double>("tonnes", tonnes) });
                }
                TookDamage(vehicle, hit);
            }
            if (!vehicle.Destroyed && hit.PulseAmount > 0f && EngineRunning(vehicle, comp))
            {
                // A parked vehicle shrugged off the pulse in the real tests; a running one may stall (the EMP Commission, 2008). Through the
                // game's EMP damage, so Vehicle Framework's own EMP step and ours under Combat Extended decide the stall by the vehicle's hardening.
                vehicle.TakeDamage(new DamageInfo(DamageDefOf.EMP, hit.PulseAmount, 0f, -1f, hit.Launcher));
                outcome.Pulsed = true;
                Seen.Note("vehicles.large-blast", vehicle, "pulse on a running engine", "pulsed");
            }
        }
        foreach (Pawn pawn in aboard)
        {
            if (!pawn.Dead) outcome.Inside.Add((pawn, doseShare));
        }
        return outcome;
    }

    /// <summary>Inside the fireball: everyone aboard dies, and the vehicle is gone with them (NK1).</summary>
    static void Vanish(VehiclePawn vehicle, List<Pawn> aboard, LargeBlastHit hit, LargeBlastOutcome outcome)
    {
        foreach (Pawn pawn in aboard)
        {
            if (pawn.Dead) continue;
            try
            {
                pawn.Kill(BlastDamage(hit));
                outcome.Killed.Add(pawn);
            }
            catch (Exception error)
            {
                VehicleSystemsMod.Logger.Warn("Someone aboard the " + vehicle.LabelShort + " could not be killed by the blast: " + error.Message);
            }
        }
        outcome.Gone = true;
        if (Seen.Wants("vehicles.large-blast", vehicle, "gone")) Seen.Note("vehicles.large-blast", vehicle, aboard.Count + " aboard", "gone");
        if (!vehicle.Destroyed) vehicle.Destroy(DestroyMode.Vanish);
    }

    /// <summary>The people aboard are thrown about inside as the vehicle is hit (NK8), in blunt blows, as our falls from aircraft are.</summary>
    static void HurtCrew(VehiclePawn vehicle, List<Pawn> aboard, double severity, VehicleBlastFigures f, LargeBlastHit hit, LargeBlastOutcome outcome)
    {
        double harm = VehicleBlastRule.CrewHarm(severity, f);
        int blows = VehicleBlastRule.Blows(harm, f);
        if (blows == 0) return;
        float each = (float)(harm / blows);
        foreach (Pawn pawn in aboard)
        {
            if (pawn.Dead) continue;
            for (int i = 0; i < blows && !pawn.Dead; i++) pawn.TakeDamage(new DamageInfo(DamageDefOf.Blunt, each, 0f, -1f, hit.Launcher, null, hit.Weapon));
            outcome.Hurt++;
            if (pawn.Dead) outcome.Killed.Add(pawn);
            if (Seen.Wants("vehicles.large-blast-crew", vehicle, pawn.Dead ? "killed" : "hurt"))
            {
                Seen.Note("vehicles.large-blast-crew", vehicle, pawn.LabelShort + (pawn.Dead ? " killed" : " hurt") + ", harm " + harm.ToString("0"),
                    pawn.Dead ? "killed" : "hurt");
            }
        }
    }

    /// <summary>Every part loses a share of its health, the outer ones most (NK2). Our empty mounting points are left alone.</summary>
    static bool Batter(VehiclePawn vehicle, CompPhobosVehicleSystems? comp, double severity, VehicleBlastFigures f)
    {
        HashSet<string>? uncounted = comp?.Props.uncountedKeys is { Count: > 0 } keys ? new HashSet<string>(keys) : null;
        bool any = false;
        foreach (VehicleComponent part in vehicle.statHandler.components)
        {
            if (part.Health <= 0f || uncounted?.Contains(part.props.key) == true) continue;
            bool outer = part.Depth == VehicleComponent.VehiclePartDepth.External;
            double share = VehicleBlastRule.PartShare(severity, outer, Rand.Value, f);
            if (share <= 0) continue;
            part.SetHealth(part.Health - (float)(share * part.MaxHealth));
            any = true;
        }
        return any;
    }

    /// <summary>Heat takes the soft parts: tyres, belts, hoses and hydraulic lines, by the roles our loadouts give them (NK3).</summary>
    static bool Scorch(VehiclePawn vehicle, CompPhobosVehicleSystems? comp, PhobosVehicleBlastRulesDef rules, float heat, VehicleBlastFigures f)
    {
        double share = VehicleBlastRule.SoftShare(heat, f);
        if (share <= 0 || comp == null || rules.softRoles.Count == 0) return false;
        bool any = false;
        foreach (string role in rules.softRoles)
        {
            if (!comp.Props.roleKeys.TryGetValue(role, out List<string>? keys)) continue;
            foreach (VehicleComponent part in vehicle.statHandler.components)
            {
                if (part.Health <= 0f || !keys.Contains(part.props.key)) continue;
                part.SetHealth(part.Health - (float)(share * part.MaxHealth));
                any = true;
            }
        }
        return any;
    }

    /// <summary>
    /// What Combat Extended's own part step does after a part loses health: the vehicle hears it took damage (its stats and health are worked
    /// out again) and that its health changed (a holed fuel tank leaks), stops if it can no longer move, and is destroyed under 1 %.
    /// </summary>
    static void TookDamage(VehiclePawn vehicle, LargeBlastHit hit)
    {
        vehicle.EventRegistry[VehicleEventDefOf.DamageTaken].ExecuteEvents();
        vehicle.EventRegistry[VehicleEventDefOf.HealthChanged].ExecuteEvents();
        if (vehicle.Destroyed) return;
        if (vehicle.GetStatValue(VehicleStatDefOf.MoveSpeed) <= 0.1f) vehicle.ignition.Drafted = false;
        if (vehicle.Spawned && vehicle.GetStatValue(VehicleStatDefOf.BodyIntegrity) < 0.01f) vehicle.Kill(BlastDamage(hit), DestroyMode.KillFinalize);
    }

    static DamageInfo BlastDamage(LargeBlastHit hit) => new(DamageDefOf.Bomb, 1f, 0f, -1f, hit.Launcher, null, hit.Weapon);

    /// <summary>The vehicle empty, in tonnes: its kerb mass, as its repairs and drive use it.</summary>
    static double Tonnes(VehiclePawn vehicle, CompPhobosVehicleSystems? comp) =>
        (comp != null ? comp.Repairs.Kilograms : Powertrain.KerbFor(vehicle.VehicleDef, null, null)) / 1000.0;

    static bool EngineRunning(VehiclePawn vehicle, CompPhobosVehicleSystems? comp) => comp?.EngineRunning ?? vehicle.ignition.Drafted;
}
