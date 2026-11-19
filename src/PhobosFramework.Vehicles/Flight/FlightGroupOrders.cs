using Phobos.Framework.Vehicles.Rules;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Several aircraft sent to one spot together (Framework 0.174.0; the audit of 19 Nov 2026, finding 2, after VTOL Vehicle's formation
/// targets): Vehicle Framework hands each selected vehicle the same clicked cell, one after another in the same frame, so every aircraft
/// made for one cell. The first still goes there; the rest go to rings round it (<see cref="FormationRule"/>), spaced by their size.
/// Derived and never saved: only the frame's last spot and count are kept.
/// </summary>
public static class FlightGroupOrders
{
    static int frame = -1;
    static IntVec3 spot = IntVec3.Invalid;
    static int sent;

    /// <summary>Cells between two aircraft's spots beyond their longest side (authored: room for rotor wash).</summary>
    const float MarginCells = 3f;

    /// <summary>The cell this aircraft should fly to for an order to <paramref name="click"/>: the cell itself for the first sent there this
    /// frame, a place in the rings round it for each one after.</summary>
    public static IntVec3 Spread(VehiclePawn vehicle, IntVec3 click)
    {
        int now = Time.frameCount;
        if (now != frame || click != spot)
        {
            frame = now;
            spot = click;
            sent = 0;
        }
        int index = sent++;
        if (index == 0 || vehicle.Map == null) return click;
        IntVec2 size = vehicle.VehicleDef.Size;
        (double dx, double dz) = FormationRule.Offset(index, Mathf.Max(size.x, size.z) + MarginCells);
        var cell = new IntVec3(click.x + Mathf.RoundToInt((float)dx), 0, click.z + Mathf.RoundToInt((float)dz)).ClampInsideMap(vehicle.Map);
        if (Seen.On) Seen.Note("aircraft.group-spread", vehicle, "place " + index);
        return cell;
    }
}
