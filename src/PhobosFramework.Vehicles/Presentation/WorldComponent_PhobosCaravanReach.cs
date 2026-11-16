using Phobos.Framework.Interface;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>How far an aircraft in flight can still fly on the fuel aboard (Framework 0.114.3): the one figure the circle on the globe and
/// the line in the inspect pane both read. Vehicle Framework's own launch formula turned round (ReachRule.FlightTiles), in the game's
/// tiles; our factors on world-flight fuel come through our postfix on FuelConsumptionWorldMultiplier.</summary>
public readonly struct FlightRange
{
    /// <summary>The tiles it can still fly; 0 when it has no fuel the game can measure in flight.</summary>
    public readonly double Tiles;
    /// <summary>The same as an angle at the planet's centre, in radians.</summary>
    public readonly double Angle;

    public FlightRange(double tiles, double angle)
    {
        Tiles = tiles;
        Angle = angle;
    }

    public bool Any => Tiles > 0;
    /// <summary>Whether it reaches every point on the planet, so there is no circle to draw.</summary>
    public bool Anywhere => ReachRule.CoversPlanet(Angle);

    public static FlightRange Of(AerialVehicleInFlight aircraft)
    {
        VehiclePawn? vehicle = aircraft.Vehicle;
        CompFueledTravel? fuel = vehicle?.CompFueledTravel;
        CompVehicleLauncher? launcher = vehicle?.CompVehicleLauncher;
        if (fuel == null || launcher == null) return default;
        double tiles = ReachRule.FlightTiles(fuel.Fuel, launcher.FlightSpeed, fuel.ConsumptionRatePerTickRaw, launcher.FuelConsumptionWorldMultiplier);
        return new FlightRange(tiles, ReachRule.FlightAngle(tiles, Find.WorldGrid.ApproxDistanceInTiles(1f)));
    }
}

/// <summary>
/// Says in an aircraft's inspect pane, on the world map, how far its fuel still takes it (Framework 0.114.3). The circle alone was not
/// enough (the owner's reports of 16 Nov 2026): a helicopter's lies some 30 degrees of arc away, off-screen at an ordinary zoom, and an
/// aircraft that can reach anywhere has no circle at all. Presentation only. Added to Vehicle Framework's AerialVehicle by patch.
/// </summary>
public class WorldObjectComp_PhobosFlightRange : WorldObjectComp
{
    const string GuardKey = "An aircraft's range line on the world map";

    public override string? CompInspectStringExtra()
    {
        // Guarded: the game asks for this every frame while the thing is selected (UiGuard).
        if (UiGuard.Resting(GuardKey)) return UiGuard.FallbackText;
        try
        {
            if (parent is not AerialVehicleInFlight aircraft || aircraft.Faction != Faction.OfPlayer) return null;
            FlightRange range = FlightRange.Of(aircraft);
            if (!range.Any) return null;
            NamedArgument tiles = ((int)range.Tiles).ToString("N0").Named("TILES");
            return (range.Anywhere ? "PhobosVehicleSystems_FlightRangeAnywhere" : "PhobosVehicleSystems_FlightRange").Translate(tiles).Resolve();
        }
        catch (System.Exception e) { UiGuard.Report(GuardKey, e); return null; }
    }
}

/// <summary>
/// Draws the edge of where a selected vehicle caravan's fuel takes it (Framework 0.85.0; VehicleRange works it out), and since 0.114.1 a
/// circle round a selected aircraft in flight at the distance its fuel still flies (FlightRange). Until 0.112.1 a caravan comp drew the
/// edge from PostDrawExtraSelectionOverlays, but Vehicle Framework's VehicleCaravan.DrawExtraSelectionOverlays (1.6 and 1.7) never calls
/// the base method that runs its comps' overlays, so the edge was never drawn. The game gives every world component a per-frame update,
/// and its own selection overlays are drawn from the world interface's update the same way, so this draws while the world is shown. No
/// Harmony; saves nothing. Presentation only. With verbose logging on for "Vehicle systems", says why nothing is drawn, and the circle's
/// size when one is.
/// </summary>
public sealed class WorldComponent_PhobosCaravanReach : WorldComponent
{
    /// <summary>The colour of the edge (authored: the dashboard's amber, so it reads as "fuel").</summary>
    static Material? rangeMaterial;
    static Material RangeMaterial => rangeMaterial ??= MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.WorldOverlayTransparent, new Color(0.95f, 0.70f, 0.25f, 0.9f), 3590);

    /// <summary>The circle's segments, and how far above the globe it sits (as the caravan's edge).</summary>
    const int CircleSegments = 128;
    const float Lift = 0.08f;
    /// <summary>How far the range must move, as a share, before the verbose log says so again (authored).</summary>
    const double LogStep = 0.05;

    /// <summary>The aircraft circle last worked out, kept until the aircraft, its place or its fuel (to a hundredth) changes.</summary>
    int circleFor = -1;
    Vector3 circleCentre;
    int circleFuelKey = -1;
    readonly Vector3[] circle = new Vector3[CircleSegments];
    bool circleShown;
    /// <summary>What the verbose log last said: the selection it explained, and the range it last gave for an aircraft.</summary>
    int lastExplained = -1;
    int loggedFor = -1;
    double loggedTiles;

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
                Explain(selected, "range line drawn: " + reach.Edge.Count + " edge segments round " + reach.Tiles + " tiles" + (reach.Capped ? " (search limit reached)" : ""));
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
        Vector3 centre = aircraft.DrawPos;
        int fuelKey = fuel == null ? 0 : Mathf.RoundToInt(fuel.Fuel / Mathf.Max(0.01f, fuel.FuelCapacity) * 100f);
        if (circleFor == aircraft.ID && fuelKey == circleFuelKey && (centre - circleCentre).sqrMagnitude < 0.01f) return circleShown;
        circleFor = aircraft.ID;
        circleFuelKey = fuelKey;
        circleCentre = centre;
        FlightRange range = FlightRange.Of(aircraft);
        if (!range.Any)
        {
            Explain(aircraft, "no range circle: no fuel tank, no launcher, no fuel, or it burns none the game can measure in flight");
            return circleShown = false;
        }
        LogRange(aircraft, range);
        if (range.Anywhere) return circleShown = false;
        // On the globe: every point the angle away from the aircraft.
        float angle = (float)range.Angle;
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
        return circleShown = true;
    }

    /// <summary>The aircraft's range for the verbose log: when first worked out for it, and again when it has moved by a twentieth. (0.114.1
    /// wrote a line each time the circle was redrawn round a moving aircraft, four a second: the owner's log of 16 Nov 2026.)</summary>
    void LogRange(AerialVehicleInFlight aircraft, FlightRange range)
    {
        if (!VehicleSystemsMod.Logger.DebugEnabled) return;
        if (loggedFor == aircraft.ID && System.Math.Abs(range.Tiles - loggedTiles) < loggedTiles * LogStep) return;
        loggedFor = aircraft.ID;
        loggedTiles = range.Tiles;
        VehiclePawn vehicle = aircraft.Vehicle;
        CompFueledTravel fuel = vehicle.CompFueledTravel;
        CompVehicleLauncher launcher = vehicle.CompVehicleLauncher;
        VehicleSystemsMod.Logger.Debug(vehicle.LabelShort + " (" + vehicle.ThingID + ") in flight: " + fuel.Fuel.ToString("0.#") + " fuel, flight speed "
            + launcher.FlightSpeed.ToString("0.##") + ", burn " + fuel.ConsumptionRatePerTickRaw.ToString("0.#####") + " x " + launcher.FuelConsumptionWorldMultiplier.ToString("0.##")
            + " a tick: about " + range.Tiles.ToString("0") + " tiles of range, " + (range.Angle * Mathf.Rad2Deg).ToString("0") + " degrees of arc ("
            + Find.WorldGrid.ApproxDistanceInTiles(1f).ToString("0") + " tiles to a radian); "
            + (range.Anywhere ? "no circle: it reaches anywhere on the planet" : "circle drawn at height " + aircraft.DrawPos.magnitude.ToString("0.##")));
    }

    /// <summary>A verbose line saying what is or is not drawn, once a selection.</summary>
    void Explain(WorldObject selected, string why)
    {
        if (!VehicleSystemsMod.Logger.DebugEnabled || lastExplained == selected.ID) return;
        lastExplained = selected.ID;
        VehicleSystemsMod.Logger.Debug(selected.LabelShort + " (world object " + selected.ID + "): " + why);
    }
}
