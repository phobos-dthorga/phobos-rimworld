using Phobos.Framework.Effects;
using Phobos.Framework.Rules;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Vehicles' visual effects (Framework 0.321.0; docs/development/vehicle-effects.md; the owner's cards of 26 Nov 2026): where on a vehicle a
/// puff comes from, what colour it is, and the one setting (Off, Light or Full; Full to start). Faults show as signs: smoke in the colour
/// of what burns, sparks, drips, coughs at the key. The Framework core's emitter (MapComponent_PhobosFx) draws them, on the map being looked
/// at and on screen only, and saves nothing.
/// </summary>
public static class VehicleFx
{
    /// <summary>How long a stage's puffs last after its last systems update: a little more than the rare tick that renews them.</summary>
    public const int HoldTicks = 300;

    static PhobosVehicleFxRulesDef Rules => PhobosVehicleFxRulesDef.Rules;

    /// <summary>The player's setting.</summary>
    public static FxLevel Level => VehicleSystemsMod.Settings.vehicleEffects;

    /// <summary>A vehicle's heading for the emitter: its full rotation and any turn Vehicle Framework draws it at (as our flight reads it).</summary>
    public static void UseVehicleHeadings()
    {
        System.Func<Thing, float> before = MapComponent_PhobosFx.HeadingOf;
        MapComponent_PhobosFx.HeadingOf = thing => thing is VehiclePawn v ? v.FullRotation.AsAngle + v.Transform.rotation : before(thing);
    }

    public static Color? ColourOf(FxTint tint) => tint switch
    {
        FxTint.OilSmoke => Rules.oilSmoke,
        FxTint.RichSmoke => Rules.richSmoke,
        FxTint.Steam => Rules.steam,
        FxTint.BrakeSmoke => Rules.brakeSmoke,
        FxTint.Acrid => Rules.acrid,
        FxTint.Fuel => Rules.fuel,
        FxTint.TyreSmoke => Rules.tyreSmoke,
        FxTint.Debris => Rules.debris,
        _ => null,
    };

    /// <summary>Half the vehicle's length and width, in cells, along its heading (Vehicle Framework's size is width by length facing north).</summary>
    public static (float halfLength, float halfWidth) Half(VehiclePawn vehicle)
    {
        IntVec2 size = vehicle.VehicleDef.Size;
        return (Mathf.Max(0.5f, size.z / 2f), Mathf.Max(0.5f, size.x / 2f));
    }

    /// <summary>A place on the vehicle: cells ahead of its middle, cells to its right, and how far a puff may land from it.</summary>
    public static (float forward, float right, float spread) Place(VehiclePawn vehicle, FxSpot spot)
    {
        (float l, float w) = Half(vehicle);
        return spot switch
        {
            FxSpot.Exhaust => (-l + 0.2f, w * 0.4f, 0.15f),
            FxSpot.Engine => (l * 0.45f, 0f, w * 0.3f),
            FxSpot.Front => (l - 0.2f, 0f, w * 0.3f),
            FxSpot.Wheels => (0f, 0f, Mathf.Max(l, w) * 0.8f),
            FxSpot.Under => (0f, 0f, w * 0.35f),
            _ => (0f, 0f, Mathf.Max(l, w) * 0.5f),
        };
    }

    /// <summary>An emission of this fleck at this place on the vehicle, in this tint.</summary>
    public static FxEmission Emission(VehiclePawn vehicle, FleckDef fleck, FxTint tint, FxSpot spot, float perHour, float scale, bool drift)
    {
        (float forward, float right, float spread) = Place(vehicle, spot);
        FxEmission e = FxEmission.Of(fleck, perHour, scale);
        e.colour = ColourOf(tint);
        e.forward = forward;
        e.right = right;
        e.spread = spread;
        e.drift = drift;
        e.speed = drift ? 0.3f : 0.6f;
        return e;
    }

    /// <summary>Keeps a fault's sign going while its stage lasts (renewed at each systems update).</summary>
    public static void HoldSign(VehiclePawn vehicle, object key, FxEmission emission)
    {
        if (Level == FxLevel.Off || !vehicle.Spawned) return;
        MapComponent_PhobosFx.For(vehicle.Map)?.Hold(vehicle, key, emission, HoldTicks);
    }

    /// <summary>A sign at once (a tyre blowing, a cough at the key), when the vehicle is on screen.</summary>
    public static void BurstSign(VehiclePawn vehicle, FxEmission emission, int count)
    {
        if (Level == FxLevel.Off || !vehicle.Spawned) return;
        MapComponent_PhobosFx.For(vehicle.Map)?.Burst(vehicle, emission, count);
    }

    /// <summary>The cough of an engine cranked without starting (the key refused): what the refusing stage names, at the exhaust.</summary>
    public static void Crank(VehiclePawn vehicle, Effect_RefuseDraft? refusal)
    {
        if (refusal == null || refusal.crankPuffs <= 0) return;
        BurstSign(vehicle, Emission(vehicle, FleckDefOf.Smoke, refusal.crankTint, FxSpot.Exhaust, 0f, 0.7f, drift: true), refusal.crankPuffs);
        if (Seen.Wants("vehicles.fx-crank", vehicle, refusal.crankTint.ToString())) Seen.Note("vehicles.fx-crank", vehicle, null, refusal.crankTint.ToString());
    }

    /// <summary>Drips under a vehicle leaking fuel (Vehicle Framework's own leak), looked at each rare tick.</summary>
    public static void FuelDrips(VehiclePawn vehicle)
    {
        if (vehicle.CompFueledTravel is not { FuelLeaking: true } || !vehicle.Spawned) return;
        FxEmission e = Emission(vehicle, FleckDefOf.DustPuff, FxTint.Fuel, FxSpot.Under, 50f, 0.35f, drift: false);
        e.speed = 0f;
        HoldSign(vehicle, DripKey, e);
        if (Seen.Wants("vehicles.fx-fuel-drip", vehicle)) Seen.Note("vehicles.fx-fuel-drip", vehicle);
    }

    static readonly object DripKey = new();
}
