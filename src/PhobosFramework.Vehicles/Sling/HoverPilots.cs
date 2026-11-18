using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// What the sling's lift and set-down from a hover need from whatever flies the helicopter over the map (Framework 0.165.0): our own flight,
/// or VTOL Vehicle's (the Hind). Where it is, whether it is hovering, how high, and an order to fly to a cell and hover there.
/// </summary>
public abstract class HoverPilot
{
    /// <summary>Whether it is hovering now: in the air and stopped where it was sent.</summary>
    public abstract bool Hovering { get; }

    /// <summary>Whether it can hold a hover at all now (its engines, at its weight, in this air).</summary>
    public abstract bool CanHover { get; }

    /// <summary>Its true centre over the map.</summary>
    public abstract Vector3 Position { get; }

    /// <summary>Its height above the ground now, metres.</summary>
    public abstract float HeightMetres { get; }

    /// <summary>The nearest of the four facings to its heading.</summary>
    public abstract Rot4 Facing { get; }

    /// <summary>Sends it to hover over this cell; false where its flight refuses.</summary>
    public abstract bool GoTo(IntVec3 cell);

    /// <summary>Whatever flies this helicopter over the map now, in the air; null on the ground or for one that does not fly the map itself.</summary>
    public static HoverPilot? For(VehiclePawn vehicle)
    {
        if (!vehicle.Spawned) return null;
        if (vehicle.GetComp<CompPhobosFlight>() is { Airborne: true } flight && flight.Active) return new OurFlightPilot(flight);
        return VtolPilot.Make(vehicle);
    }
}

/// <summary>Our own flight (Framework 0.161.0's hover lift, now behind the shared pilot).</summary>
public sealed class OurFlightPilot : HoverPilot
{
    readonly CompPhobosFlight flight;

    public OurFlightPilot(CompPhobosFlight flight) => this.flight = flight;

    public override bool Hovering => flight.State == FlightState.Hover;
    public override bool CanHover => flight.HoversSteadily;
    public override Vector3 Position => flight.TrueCenter;
    public override float HeightMetres => flight.HeightMetresNow;
    public override Rot4 Facing => FlightRules.Rot4For(flight.Heading);

    public override bool GoTo(IntVec3 cell)
    {
        if (flight.FlyToRefusal(cell) != null) return false;
        flight.FlyTo(cell);
        return true;
    }
}

/// <summary>
/// VTOL Vehicle's flight (rabiosus; the Hind's), read and ordered by name through its public members, so no assembly of ours references it
/// (its source is not public; read from the decompile, 19 Nov 2026). It hovers over its move target by default. Its height is a share of its
/// climb, so it is given in metres by an authored figure.
/// </summary>
public sealed class VtolPilot : HoverPilot
{
    /// <summary>Its height at full climb, metres (authored: VTOL Vehicle sets no height, only a share of its climb drawn).</summary>
    public const float FullHeightMetres = 30f;

    static readonly MethodInfo? IsFlyingGetter = AccessTools.PropertyGetter("VTOLVehicle.VtolVehiclePawn:IsFlying");
    static readonly MethodInfo? DrawProgressGetter = AccessTools.PropertyGetter("VTOLVehicle.VtolVehiclePawn:CurrentDrawProgress");
    static readonly MethodInfo? HasArrived = AccessTools.Method("VTOLVehicle.VtolVehiclePawn:HasArrivedAtMoveTarget");
    static readonly MethodInfo? SetMoveTarget = AccessTools.Method("VTOLVehicle.VtolVehiclePawn:SetMoveTarget", new[] { typeof(IntVec3) });
    static readonly FieldInfo? RealPosition = AccessTools.Field("VTOLVehicle.VtolVehiclePawn:RealPosition");
    static readonly FieldInfo? FacingAngle = AccessTools.Field("VTOLVehicle.VtolVehiclePawn:FacingAngle");
    static readonly Type? VtolType = AccessTools.TypeByName("VTOLVehicle.VtolVehiclePawn");

    readonly VehiclePawn vehicle;

    VtolPilot(VehiclePawn vehicle) => this.vehicle = vehicle;

    /// <summary>A pilot for a VTOL Vehicle aircraft flying now, or null (another kind, on the ground, or a member renamed).</summary>
    public static VtolPilot? Make(VehiclePawn vehicle)
    {
        if (VtolType == null || !VtolType.IsInstanceOfType(vehicle) || IsFlyingGetter == null || SetMoveTarget == null || RealPosition == null) return null;
        try
        {
            return IsFlyingGetter.Invoke(vehicle, null) is true ? new VtolPilot(vehicle) : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public override bool Hovering
    {
        get
        {
            try
            {
                return HasArrived?.Invoke(vehicle, null) is true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    public override bool CanHover => FlightRules.WouldHoverWith(vehicle, 0);

    public override Vector3 Position => RealPosition?.GetValue(vehicle) is Vector3 p ? new Vector3(p.x, 0f, p.z) : vehicle.TrueCenter();

    public override float HeightMetres
    {
        get
        {
            try
            {
                return DrawProgressGetter?.Invoke(vehicle, null) is float share ? Mathf.Clamp01(share) * FullHeightMetres : FullHeightMetres;
            }
            catch (Exception)
            {
                return FullHeightMetres;
            }
        }
    }

    public override Rot4 Facing => FacingAngle?.GetValue(vehicle) is float angle ? FlightRules.Rot4For(angle) : vehicle.Rotation;

    public override bool GoTo(IntVec3 cell)
    {
        try
        {
            SetMoveTarget!.Invoke(vehicle, new object[] { cell });
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
