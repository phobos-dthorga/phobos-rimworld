using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Phobos.Framework.Vehicles.Rules;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// EMP on vehicles (Framework 0.109.0; soft-dependency-crosslinks.md, SD9; owner, 13 Nov 2026). Vehicle Framework already has an EMP
/// model: each part has an EMP rating, an EMP hit may then stall the vehicle and cost those parts a little health, and a vehicle with no
/// rated part ignores EMP. No vehicle mod on the owner's list rates any part, so EMP did nothing to any vehicle. When a design gets its
/// loadout, ours rates the electrical parts it binds by the vehicle's hardening (EmpRule); Vehicle Framework then switches EMP on for
/// the design by itself, a player's own choice in its settings still winning. Our systems answer the parts' health loss with their own
/// EMP rules (a battery loses charge). Runs at Def resolution, once per design; costs nothing per tick.
/// </summary>
public static class VehicleEmp
{
    /// <summary>Designs an EMP may harm but never stall: mechanical ones, and those of mods whose authors opted out of stuns.</summary>
    static readonly HashSet<VehicleDef> neverStunned = new();

    public static bool NeverStunned(VehicleDef def) => neverStunned.Contains(def);

    /// <summary>Rates the parts bound to the rules' EMP roles; called from the loadout's binding, before Vehicle Framework resolves the design.</summary>
    public static void Rate(VehicleDef def, PhobosVehicleLoadoutDef loadout, CompProperties_PhobosVehicleSystems props)
    {
        PhobosVehicleRulesDef rules = PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();
        bool armed = def.comps?.Any(c => c is CompProperties_VehicleTurrets turrets && turrets.turrets.Count > 0) == true;
        EmpHardening hardening = EmpRule.ClassFor(loadout.empHardening, armed);
        string? package = def.modContentPack?.PackageId;
        bool optedOut = package != null && rules.empNoStunPackages.Any(p => string.Equals(p, package, StringComparison.OrdinalIgnoreCase));
        if (!EmpRule.Stuns(hardening, optedOut)) neverStunned.Add(def);
        foreach (string role in rules.empRoles)
        {
            if (!props.roleKeys.TryGetValue(role, out List<string>? keys)) continue;
            foreach (string key in keys)
            {
                VehicleComponentProperties? part = def.components.FirstOrDefault(p => p.key == key);
                if (part == null) continue;
                part.empSeverity = (VehicleEMPSeverity)EmpRule.Severity(hardening, (int)part.empSeverity, (int)rules.empSeverityCivilian,
                    (int)rules.empSeverityMilitary);
            }
        }
    }
}

/// <summary>
/// A design that is never stalled still takes the EMP's harm: Vehicle Framework's per-part EMP step returns how long the part stalls the
/// vehicle, after its health loss is applied; for such a design the stall is taken away. A postfix only (no extension point decides a
/// stall without also stopping the harm).
/// </summary>
[HarmonyPatch(typeof(VehicleComponent), nameof(VehicleComponent.ApplyEMPDamage))]
public static class EmpNoStunPatch
{
    public static void Postfix(VehicleComponent __instance, ref int __result)
    {
        if (__result > 0 && __instance.vehicle != null && VehicleEmp.NeverStunned(__instance.vehicle.VehicleDef)) __result = 0;
    }
}

/// <summary>
/// The EMP hardening kit (Framework 0.158.0; vehicle-militarised-service-vehicles.md, "The kits"; owner, 19 Nov 2026: hardening is its own
/// kit, whether or not the truck carries a gun). A part's EMP rating belongs to its design, shared by every vehicle of that design, and
/// Vehicle Framework reads it in this one method. On a vehicle whose kit is fitted (Phobos_EmpHardening above zero), the rating is lowered
/// to the military one for this call alone and put back in a finalizer, so it is restored even if the method throws. The call runs on the
/// game's main thread and nothing else reads the rating meanwhile. A prefix that never skips the original; the postfix above still
/// takes the stall away from a design never stalled. Costs one stat read per part per EMP hit, and nothing otherwise.
/// </summary>
[HarmonyPatch(typeof(VehicleComponent), nameof(VehicleComponent.ApplyEMPDamage))]
public static class EmpHardeningPatch
{
    public static void Prefix(VehicleComponent __instance, out VehicleEMPSeverity? __state)
    {
        __state = null;
        VehicleComponentProperties? props = __instance.props;
        VehiclePawn? vehicle = __instance.vehicle;
        if (props == null || vehicle == null || props.empSeverity == VehicleEMPSeverity.None) return;
        if (vehicle.GetStatValue(ArmourDefOf.Phobos_EmpHardening) <= 0f) return;
        int military = (int)PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>().empSeverityMilitary;
        var hardened = (VehicleEMPSeverity)EmpRule.HardenedSeverity((int)props.empSeverity, military);
        if (hardened == props.empSeverity) return;
        __state = props.empSeverity;
        props.empSeverity = hardened;
        if (Seen.Wants("vehicles.emp-hardened", vehicle)) Seen.Note("vehicles.emp-hardened", vehicle, props.key + ": " + __state + " to " + hardened);
    }

    public static void Finalizer(VehicleComponent __instance, VehicleEMPSeverity? __state)
    {
        if (__state is { } original && __instance.props != null) __instance.props.empSeverity = original;
    }
}
