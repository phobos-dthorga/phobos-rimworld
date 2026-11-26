using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The turbocharger as a part of its own (Framework 0.320.0; docs/development/vehicle-turbocharger.md; the owner's cards of 26 Nov 2026,
/// after a fitted turbo showed only as a line under the engine). One part on every design with a piston engine, appended after every other
/// part we add (Vehicle Framework matches saved parts by position), left out of the vehicle's destruction sum. It is there only while the
/// vehicle has a turbocharger: built in (the engine's Def says turbocharged) or fitted (a fitting that grants <see cref="Grant"/>). Without
/// one it sits at nothing, which Vehicle Framework never aims a hit at, and every list of parts leaves it out (<see cref="AbsentParts"/>).
/// Its system (Systems_Turbocharger.xml) reads its health; this class only keeps the part in step with the fittings.
/// </summary>
public static class Turbochargers
{
    /// <summary>The part's role, which the turbocharger system and the repair supply name.</summary>
    public const string Role = "Turbocharger";
    /// <summary>The appended part's key. Stable: saves hold it.</summary>
    public const string Key = "PhobosTurbocharger";
    /// <summary>What a fitting grants that puts a turbocharger on the engine (the turbocharger fitting), and what the system needs.</summary>
    public const string Grant = "Turbocharger";

    static PhobosVehicleRulesDef Rules => PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();

    /// <summary>Whether the vehicle had a turbocharger the last time its fittings were looked at; derived, never saved (set again at the
    /// first look after a load).</summary>
    static readonly ConditionalWeakTable<CompPhobosVehicleSystems, StrongBox<bool>> wasFitted = new();

    /// <summary>Appends the part to a design with a piston engine, at start-up, after every other part we add.</summary>
    internal static void Apply(VehicleDef def, CompProperties_PhobosVehicleSystems props)
    {
        if (props.trailer || def.components == null || !Piston(props) || def.components.Any(p => p.key == Key)) return;
        var add = new PartToAdd
        {
            key = Key,
            label = Rules.turbochargerPartLabel,
            health = Rules.turbochargerPartHealth,
            depth = VehicleComponent.VehiclePartDepth.Internal,
            // Bolted to the engine, inside the body: hit about as often as the radiator's core inside (authored).
            hitWeight = 0.1f,
            hitWeightCE = 4f,
            cellsOf = "Engine",
            countsTowardDestruction = false,
        };
        def.components.Add(VehicleSystems.MakePart(def, add, props));
        props.roleKeys[Role] = new List<string> { Key };
        props.addedKeys.Add(Key);
        props.uncountedKeys.Add(Key);
    }

    static bool Piston(CompProperties_PhobosVehicleSystems props) => props.drivetrain?.def?.engine is { kind: EngineKind.Piston };

    /// <summary>Whether the design's engine is turbocharged as built (the medium and heavy diesels, the turbo tractors).</summary>
    public static bool BuiltIn(CompProperties_PhobosVehicleSystems props) => props.drivetrain?.def?.engine is { kind: EngineKind.Piston, turbocharged: true };

    /// <summary>Whether this vehicle has a turbocharger, working or not: built in, or a fitting carrying one (lost with the engine it is on).</summary>
    public static bool Fitted(CompPhobosVehicleSystems comp) => BuiltIn(comp.Props) || comp.Fittings.Carries(Grant);

    /// <summary>Whether a turbocharger is on the vehicle and its part still stands, so it still boosts (the thin-air hold reads it). A
    /// design with no turbocharger part counts as working, as before the part existed.</summary>
    public static bool Works(CompPhobosVehicleSystems comp) => Fitted(comp) && comp.PartHealthShare(Role, 1f) > 0f;

    /// <summary>Whether this part is the turbocharger of a vehicle that has none: shown nowhere, repaired never.</summary>
    public static bool Absent(VehiclePawn vehicle, VehicleComponent part) =>
        part.props.key == Key && vehicle.GetComp<CompPhobosVehicleSystems>() is { } comp && !Fitted(comp);

    /// <summary>The part's health at the first look after a spawn or a load: the saved health while a turbocharger is on, whole for one new
    /// since the save, and nothing without one (Vehicle Framework makes every part whole, which would hand every petrol car a turbo).</summary>
    internal static float StartingHealth(CompPhobosVehicleSystems comp, VehicleComponent part, float? saved)
    {
        bool fitted = Fitted(comp);
        Remember(comp, fitted);
        return fitted ? saved ?? part.MaxHealth : 0f;
    }

    /// <summary>
    /// Keeps the part in step with the fittings, after any change to them: a turbocharger newly fitted is a new one, whole; one taken off or
    /// lost goes to nothing. A turbo worn or shot to nothing while still fitted stays at nothing, waiting for its replacement.
    /// </summary>
    internal static void FittingsChanged(CompPhobosVehicleSystems comp)
    {
        VehicleComponent? part = comp.Vehicle?.statHandler?.GetComponent(Key);
        if (part == null) return;
        bool now = Fitted(comp);
        if (!wasFitted.TryGetValue(comp, out StrongBox<bool>? before))
        {
            // Not looked at yet: the first look sets the part from the fittings as they stand.
            Remember(comp, now);
            return;
        }
        if (before.Value == now) return;
        before.Value = now;
        part.SetHealth(now ? part.MaxHealth : 0f);    // raises Vehicle Framework's HealthChanged itself
        // Remembered, or the next look reads the fitting or removal as damage ("came under fire").
        comp.RememberAllPartHealths();
        comp.Repairs.Invalidate();
        if (Seen.Wants("vehicles.turbo-part-set", comp.Vehicle)) Seen.Note("vehicles.turbo-part-set", comp.Vehicle, part.props.label, now ? "fitted" : "removed");
    }

    static void Remember(CompPhobosVehicleSystems comp, bool fitted)
    {
        if (wasFitted.TryGetValue(comp, out StrongBox<bool>? box)) box.Value = fitted;
        else wasFitted.Add(comp, new StrongBox<bool>(fitted));
    }
}

/// <summary>
/// Parts on the design that are not on this vehicle (Framework 0.320.0): a spall liner not fitted, a turbocharger on an engine without one.
/// Each sits at nothing by design, so the damage picture, the fleet's worst part, the diagnosis and the repairs leave it out.
/// </summary>
public static class AbsentParts
{
    public static bool Is(VehiclePawn vehicle, VehicleComponent part) => Spall.Unfitted(vehicle, part) || Turbochargers.Absent(vehicle, part);
}
