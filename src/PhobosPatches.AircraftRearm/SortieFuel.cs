using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CoreLib.Collections;
using HarmonyLib;
using Phobos.Framework;
using RimWorld;
using RimWorld.Planet;
using SmashTools.Targeting;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.AircraftRearmPatches;

/// <summary>
/// The fixed-wing jets' sortie fuel rule, back in force on Vehicle Framework 1.7 (Patches 0.42.0; the owner's word, 25 Nov 2026: "Yes,
/// I'd like that patched"). The framework the Harrier, Close Air Support and the Prowler share refuses a strike unless the tank holds the
/// trip there, time over the target and the trip back, with a tenth of the tank in reserve (its FixedWingSortieUtility.TryFuelForSortie,
/// called here as it stands, so its figures stay the author's). It applied the rule from a postfix on CompVehicleLauncher.HasEnoughFuel,
/// which Vehicle Framework 1.7 removed, so on 1.7 no sortie was checked: a jet could be sent with enough fuel only to arrive.
///
/// Vehicle Framework 1.7 checks fuel in the destination picker instead (FuelTargetUpdater): ValidateTargets refuses a click with its reason,
/// and LaunchStatus colours the line. A postfix on each applies the author's rule to the same targets the author did (a site, a faction
/// base or any map that is not a home, whether by Sortie or Launch), and to a home when it is a sortie (SortieOverHome, 0.41.0). It runs last,
/// after D'thorga Framework's refuel-from-cargo allowance, since a strike's fuel must be in the tank. Temporary: it stands down by itself if
/// Vehicle Framework has a HasEnoughFuel again (the author's own rule would then run) or the framework's fuel sum is not found.
///
/// Since 26 Nov 2026 each jet mod carries its own copy of that framework (FixedWingCopies): each jet is held to its own copy's rule, found
/// by its class, and a copy whose sum is not found leaves only its own jets unchecked (Patches 0.45.1).
/// </summary>
public static class SortieFuel
{
    /// <summary>Each copy's jet class and its fuel sum, TryFuelForSortie(jet, target, out needed).</summary>
    static readonly List<(Type Pawn, MethodInfo TryFuel)> rules = new();
    static Func<GlobalTargetInfo>? underMouse;

    public static void Apply(Harmony harmony, LoadReport report)
    {
        // Expected to be absent on Vehicle Framework 1.7: looked for by comparing names, so the member-targets check (which reads
        // direct lookups and would flag one that finds nothing) does not report it as a broken target.
        bool authorsOwn = typeof(CompVehicleLauncher).GetMethods(AccessTools.all).Any(m => m.Name == "HasEnoughFuel");
        MethodInfo? mouse = AccessTools.Method(typeof(FlightPathTargetUpdater), "CurrentTargetUnderMouse");
        MethodInfo? validate = AccessTools.Method(typeof(FuelTargetUpdater), nameof(FuelTargetUpdater.ValidateTargets));
        MethodInfo? status = AccessTools.Method(typeof(FuelTargetUpdater), "LaunchStatus");
        bool vehicleFramework = mouse != null && validate != null && status != null;
        foreach (FixedWingCopies.Copy copy in FixedWingCopies.Active())
        {
            string feature = "a strike by " + copy.Label + " needs the fuel to get there, stay over the target and come back";
            if (authorsOwn)
            {
                report.Off(feature, "Vehicle Framework checks it through the jets' own rule again");
                continue;
            }
            Type? pawn = copy.Type("VtolVehiclePawn");
            Type? utility = copy.Type("FixedWingSortieUtility");
            MethodInfo? tryFuel = utility == null ? null : AccessTools.Method(utility, "TryFuelForSortie");
            if (!vehicleFramework || pawn == null || tryFuel == null || tryFuel.GetParameters().Length != 3)
            {
                report.Off(feature, vehicleFramework ? "its code has changed" : "Vehicle Framework's code has changed");
                continue;
            }
            rules.Add((pawn, tryFuel));
            report.On(feature);
        }
        if (rules.Count == 0) return;
        // Vehicle Framework's destination picker, once for every copy.
        underMouse = AccessTools.MethodDelegate<Func<GlobalTargetInfo>>(mouse);
        harmony.Patch(validate, postfix: new HarmonyMethod(typeof(SortieFuel), nameof(ValidatePostfix)) { priority = Priority.Last });
        harmony.Patch(status, postfix: new HarmonyMethod(typeof(SortieFuel), nameof(StatusPostfix)) { priority = Priority.Last });
    }

    /// <summary>The fuel sum of the copy this jet belongs to; null for any other vehicle.</summary>
    static MethodInfo? RuleFor(VehiclePawn vehicle)
    {
        foreach ((Type pawn, MethodInfo tryFuel) in rules)
            if (pawn.IsInstanceOfType(vehicle)) return tryFuel;
        return null;
    }

    /// <summary>The fuel a strike on this target needs, where the author's rule applies and the tank holds less; null otherwise.</summary>
    static float? Short(VehiclePawn? vehicle, GlobalTargetInfo target)
    {
        if (vehicle?.CompFueledTravel is not { } fuel || !target.IsValid || target.WorldObject is not MapParent) return null;
        if (RuleFor(vehicle) is not { } tryFuel || !SortieOverHome.IsFixedWing(vehicle)) return null;
        // A home is a strike target only on a sortie; Launch to World Map onto a home lands there, outside the rule.
        if (SortieOverHome.HomeAt(target) != null && !SortieOverHome.Targeting(vehicle)) return null;
        object?[] args = { vehicle, target, null };
        if (tryFuel.Invoke(null, args) is not true || args[2] is not float needed) return null;
        return fuel.Fuel < needed ? needed : null;
    }

    /// <summary>Refuses the click, saying how much fuel the strike needs and how much is aboard.</summary>
    public static void ValidatePostfix(VehiclePawn ___vehicle, ReadOnlyList<GlobalTargetInfo> targets, ref TargetValidation __result)
    {
        if (!__result.isValid || targets.Count == 0) return;
        if (Short(___vehicle, targets[targets.Count - 1]) is not { } needed) return;
        float has = ___vehicle.CompFueledTravel.Fuel;
        __result = TargetValidation.Failed with
        {
            Tooltip = "PhobosAircraftPatches_SortieNotEnoughFuel".Translate(___vehicle.LabelShort.Named("VEHICLE"),
                needed.ToString("0").Named("NEEDED"), has.ToString("0").Named("HAS")).Resolve()
        };
        if (Seen.Wants("patches.sortie-fuel", ___vehicle))
            Seen.Note("patches.sortie-fuel", ___vehicle, "refused: needs " + needed.ToString("0") + ", has " + has.ToString("0"));
    }

    /// <summary>Draws the line to such a target red, as Vehicle Framework does for a journey the tank cannot make.</summary>
    public static void StatusPostfix(VehiclePawn ___vehicle, ref ShuttleLaunchStatus __result)
    {
        if (__result == ShuttleLaunchStatus.Invalid || underMouse == null) return;
        if (Short(___vehicle, underMouse()) != null) __result = ShuttleLaunchStatus.Invalid;
    }
}
