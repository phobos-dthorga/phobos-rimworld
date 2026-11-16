using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Draws the edge of where a selected vehicle caravan's fuel takes it (Framework 0.85.0; VehicleRange works it out), and since 0.114.1 a
/// circle round a selected aircraft in flight at the tiles its fuel still flies (ReachRule.FlightTiles, Vehicle Framework's own launch
/// formula turned round; found by the owner, 16 Nov 2026: no line on aircraft in flight). Until 0.112.1 a caravan comp drew the edge from
/// PostDrawExtraSelectionOverlays, but Vehicle Framework's VehicleCaravan.DrawExtraSelectionOverlays (1.6 and 1.7) never calls the base
/// method that runs its comps' overlays, so the edge was never drawn. The game gives every world component a per-frame update, and its own
/// selection overlays are drawn from the world interface's update the same way, so this draws while the world is shown. No Harmony; saves
/// nothing. Presentation only. With verbose logging on for "Vehicle systems", says once a selection why nothing is drawn.
/// </summary>
public sealed class WorldComponent_PhobosCaravanReach : WorldComponent
{
    /// <summary>The colour of the edge (authored: the dashboard's amber, so it reads as "fuel").</summary>
    static Material? rangeMaterial;
    static Material RangeMaterial => rangeMaterial ??= MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.WorldOverlayTransparent, new Color(0.95f, 0.70f, 0.25f, 0.9f), 3590);

    /// <summary>The circle's segments, and how far above the globe it sits (as the caravan's edge).</summary>
    const int CircleSegments = 96;
    const float Lift = 0.08f;

    /// <summary>The aircraft circle last worked out, kept until the aircraft, its place or its fuel (to a hundredth) changes.</summary>
    int circleFor = -1;
    Vector3 circleCentre;
    int circleFuelKey = -1;
    readonly Vector3[] circle = new Vector3[CircleSegments];
    bool circleShown;
    /// <summary>The last selection the verbose line spoke about, so it says so once, not every frame.</summary>
    int lastExplained = -1;

    public WorldComponent_PhobosCaravanReach(World world) : base(world)
    {
    }

    public override void WorldComponentUpdate()
    {
        if (!WorldRendererUtility.WorldSelected) return;
        foreach (WorldObject selected in Find.WorldSelector.SelectedObjects)
        {
            if (selected.Faction != Faction.OfPlayer) continue;
            if (selected is VehicleCaravan caravan)
            {
                VehicleRange.Reach? reach = VehicleRange.For(caravan);
                if (reach == null)
                {
                    Explain(selected, "no range line: nothing in the caravan burns fuel");
                    continue;
                }
                foreach ((Vector3 a, Vector3 b) in reach.Edge) GenDraw.DrawWorldLineBetween(a, b, RangeMaterial, 0.6f);
            }
            else if (selected is AerialVehicleInFlight aircraft && aircraft.Vehicle != null)
            {
                if (UpdateCircle(aircraft))
                {
                    for (int i = 0; i < CircleSegments; i++) GenDraw.DrawWorldLineBetween(circle[i], circle[(i + 1) % CircleSegments], RangeMaterial, 0.6f);
                }
            }
        }
    }

    /// <summary>Works the aircraft's circle out again when needed; false when there is none to draw.</summary>
    bool UpdateCircle(AerialVehicleInFlight aircraft)
    {
        VehiclePawn vehicle = aircraft.Vehicle;
        CompFueledTravel? fuel = vehicle.CompFueledTravel;
        CompVehicleLauncher? launcher = vehicle.CompVehicleLauncher;
        if (fuel == null || launcher == null)
        {
            Explain(aircraft, "no range circle: no fuel tank or no launcher");
            return false;
        }
        Vector3 centre = aircraft.DrawPos;
        int fuelKey = Mathf.RoundToInt(fuel.Fuel / Mathf.Max(0.01f, fuel.FuelCapacity) * 100f);
        if (circleFor == aircraft.ID && fuelKey == circleFuelKey && (centre - circleCentre).sqrMagnitude < 0.01f) return circleShown;
        circleFor = aircraft.ID;
        circleFuelKey = fuelKey;
        circleCentre = centre;
        double tiles = ReachRule.FlightTiles(fuel.Fuel, launcher.FlightSpeed, fuel.ConsumptionRatePerTickRaw, launcher.FuelConsumptionWorldMultiplier);
        float tilesPerRadian = Find.WorldGrid.ApproxDistanceInTiles(1f);
        if (tiles <= 0 || tilesPerRadian <= 0)
        {
            Explain(aircraft, "no range circle: it burns no fuel the game can measure in flight, or has none");
            return circleShown = false;
        }
        // On the globe: every point the angle away from the aircraft, never past the far side.
        float angle = Mathf.Min((float)(tiles / tilesPerRadian), Mathf.PI * 0.98f);
        float radius = centre.magnitude;
        Vector3 up = centre.normalized;
        Vector3 across = Vector3.Cross(up, Mathf.Abs(up.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
        Vector3 along = Vector3.Cross(up, across);
        for (int i = 0; i < CircleSegments; i++)
        {
            float turn = i * Mathf.PI * 2f / CircleSegments;
            Vector3 direction = Mathf.Cos(angle) * up + Mathf.Sin(angle) * (Mathf.Cos(turn) * across + Mathf.Sin(turn) * along);
            circle[i] = direction * (radius + Lift);
        }
        if (VehicleSystemsMod.Logger.DebugEnabled)
        {
            VehicleSystemsMod.Logger.Debug(vehicle.LabelShort + " (" + vehicle.ThingID + ") in flight: " + fuel.Fuel.ToString("0.#") + " fuel, flight speed "
                + launcher.FlightSpeed.ToString("0.##") + ", burn " + fuel.ConsumptionRatePerTickRaw.ToString("0.#####") + " x " + launcher.FuelConsumptionWorldMultiplier.ToString("0.##")
                + " a tick: about " + tiles.ToString("0") + " tiles of range");
        }
        return circleShown = true;
    }

    /// <summary>A verbose line saying why nothing is drawn, once a selection.</summary>
    void Explain(WorldObject selected, string why)
    {
        if (!VehicleSystemsMod.Logger.DebugEnabled || lastExplained == selected.ID) return;
        lastExplained = selected.ID;
        VehicleSystemsMod.Logger.Debug(selected.LabelShort + " (world object " + selected.ID + "): " + why);
    }
}
