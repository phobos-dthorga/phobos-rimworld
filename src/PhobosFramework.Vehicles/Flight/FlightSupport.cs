using System.Collections.Generic;
using System.Linq;
using Phobos.Framework;
using Phobos.Framework.Rules;
using RimWorld;
using SmashTools;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The aircraft of ours in the air on any map (Framework 0.147.0). Every patch our flight needs asks <see cref="Any"/> first, so each costs
/// one empty check while nothing of ours flies. Derived, never saved: an aircraft saved in the air registers itself again as it spawns.
/// </summary>
public static class FlightRegistry
{
    static readonly Dictionary<VehiclePawn, CompPhobosFlight> airborne = new();
    static Game? forGame;

    /// <summary>Whether any aircraft of ours is in the air now.</summary>
    public static bool Any
    {
        get
        {
            if (airborne.Count == 0) return false;
            if (Current.Game != forGame) airborne.Clear();
            return airborne.Count > 0;
        }
    }

    public static void Add(CompPhobosFlight comp)
    {
        if (Current.Game != forGame)
        {
            airborne.Clear();
            forGame = Current.Game;
        }
        if (comp.Vehicle != null) airborne[comp.Vehicle] = comp;
    }

    public static void Remove(CompPhobosFlight comp)
    {
        if (comp.Vehicle != null) airborne.Remove(comp.Vehicle);
    }

    /// <summary>The flight of this vehicle if it is in the air on its map now, else null.</summary>
    public static CompPhobosFlight? Get(VehiclePawn? vehicle) =>
        vehicle != null && Any && airborne.TryGetValue(vehicle, out CompPhobosFlight? comp) && comp.Airborne && vehicle.Spawned ? comp : null;

    /// <summary>Whether this vehicle is in the air on its map on our flight.</summary>
    public static bool InAir(VehiclePawn? vehicle) => Get(vehicle) != null;
}

/// <summary>The rules our flight reads from the game: where it may come down, what its engines leave it, and its headings.</summary>
public static class FlightRules
{
    /// <summary>Metres a map cell stands for, for the shadow's slide and the Engineer line (authored: a person is about one cell).</summary>
    public const float MetresPerCell = 1.5f;

    /// <summary>The engine model's hover margin now (1 is just enough): 0 with every engine out, NaN where the vehicle has no engine model.</summary>
    public static float Margin(VehiclePawn vehicle)
    {
        Powertrain? train = CompPhobosVehicleSystems.Of(vehicle)?.Powertrain;
        if (train == null || !train.JudgesHover) return float.NaN;
        if (train.Engines().working <= 0) return 0f;
        (bool dry, bool wet, double margin) = train.HoverCheck();
        return dry || wet ? Mathf.Max(1f, (float)margin) : (float)margin;
    }

    /// <summary>
    /// What the engines leave it able to do, under the player's hover-limit setting: refuse (the default) reads the three thresholds; allow
    /// at a cost lets it hover past the limit, straining the engines, down to the forward-flight share; warn only, or figures only, never
    /// brings it down for its margin. With every engine out it falls whatever the setting.
    /// </summary>
    public static FlightHold Hold(VehiclePawn vehicle, float margin, float forwardShare)
    {
        if (!float.IsNaN(margin) && margin <= 0f) return FlightHold.Falling;
        VehicleSystemsSettings settings = VehicleSystemsMod.Settings;
        if (settings.showOnly || settings.hoverLimit == VehicleSystemsSettings.HoverLimit.WarnOnly) return FlightHold.Hover;
        FlightHold judged = FlightPathRule.Judge(margin, forwardShare);
        if (settings.hoverLimit == VehicleSystemsSettings.HoverLimit.AllowWithDamage && judged == FlightHold.ForwardOnly) return FlightHold.Hover;
        return judged;
    }

    /// <summary>Whether a straight lift-off is refused for weight, heat or height under the player's setting; the margin it judged.</summary>
    public static bool HoverRefused(VehiclePawn vehicle, out float margin)
    {
        margin = Margin(vehicle);
        if (float.IsNaN(margin) || margin >= 1f) return false;
        VehicleSystemsSettings settings = VehicleSystemsMod.Settings;
        return !settings.showOnly && settings.hoverLimit == VehicleSystemsSettings.HoverLimit.RefusePadUseRunway;
    }

    /// <summary>
    /// Why the vehicle could not come down on this cell facing this way, as words that finish "it cannot land there:", or null when it can.
    /// Every footprint cell on the map, unroofed, free of buildings and impassable ground, not deep water and not claimed by another vehicle;
    /// seen, unless it is falling and has no choice.
    /// </summary>
    public static string? SpotRefusal(VehiclePawn vehicle, IntVec3 cell, Rot4 rot, bool forced)
    {
        Map map = vehicle.Map;
        if (map == null || !cell.IsValid) return "PhobosFlight_SpotEdge".Translate().Resolve();
        VehiclePositionManager claims = map.GetDetachedMapComponent<VehiclePositionManager>();
        foreach (IntVec3 c in GenAdj.OccupiedRect(cell, rot, vehicle.VehicleDef.Size))
        {
            if (!c.InBounds(map)) return "PhobosFlight_SpotEdge".Translate().Resolve();
            if (!forced && c.Fogged(map)) return "PhobosFlight_SpotUnseen".Translate().Resolve();
            if (c.Roofed(map)) return "PhobosFlight_SpotRoof".Translate().Resolve();
            if (c.GetEdifice(map) != null) return "PhobosFlight_SpotBuilding".Translate().Resolve();
            if (claims.ClaimedBy(c) is { } other && other != vehicle) return "PhobosFlight_SpotVehicle".Translate().Resolve();
            if (!c.Standable(vehicle, map)) return "PhobosFlight_SpotGround".Translate().Resolve();
        }
        return null;
    }

    /// <summary>The cell a vehicle stands on when its true centre is here, facing this way (the inverse of Vehicle Framework's TrueCenter).</summary>
    public static IntVec3 CellFor(VehiclePawn vehicle, float x, float z, Rot8 rot)
    {
        Vector3 even = Ext_Vehicles.TrueCenter(IntVec3.Zero, rot, vehicle.VehicleDef.Size, 0f) - new Vector3(0.5f, 0f, 0.5f);
        return new IntVec3(Mathf.FloorToInt(x - even.x), 0, Mathf.FloorToInt(z - even.z));
    }

    /// <summary>The heading of eight nearest an angle that is already a multiple of 45 (clockwise from north).</summary>
    public static Rot8 RotFor(double angle) => ((int)Mathf.Round((float)FlightPathRule.Normalise(angle) / 45f) % 8) switch
    {
        0 => Rot8.North,
        1 => Rot8.NorthEast,
        2 => Rot8.East,
        3 => Rot8.SouthEast,
        4 => Rot8.South,
        5 => Rot8.SouthWest,
        6 => Rot8.West,
        _ => Rot8.NorthWest,
    };

    /// <summary>The heading of four nearest an angle (clockwise from north).</summary>
    public static Rot4 Rot4For(double angle) => ((int)Mathf.Round((float)FlightPathRule.NearestOfFour(angle) / 90f) % 4) switch
    {
        0 => Rot4.North,
        1 => Rot4.East,
        2 => Rot4.South,
        _ => Rot4.West,
    };
}

/// <summary>Our flight's buttons (Framework 0.147.0). Presentation only: each calls one order on the comp, which says what it did or why not.</summary>
public static class FlightGizmos
{
    static Texture2D Icon(string name) => SystemIcons.For("Phobos/VehicleSystems/Flight/" + name);

    public static IEnumerable<Gizmo> For(CompPhobosFlight flight)
    {
        VehiclePawn vehicle = flight.Vehicle;
        NamedArgument who = vehicle.LabelShort.Named("VEHICLE");
        if (!flight.Airborne)
        {
            var takeOff = new Command_Action
            {
                defaultLabel = "PhobosFlight_TakeOff".Translate(),
                defaultDesc = "PhobosFlight_TakeOffDesc".Translate(who),
                icon = Icon("TakeOff"),
                action = flight.TakeOff,
            };
            if (flight.TakeOffRefusal() is { } refused) takeOff.Disable(refused);
            yield return takeOff;
            yield break;
        }
        bool busy = flight.State is FlightState.LiftingOff or FlightState.ComingDown;
        var flyTo = new Command_Action
        {
            defaultLabel = "PhobosFlight_FlyTo".Translate(),
            defaultDesc = "PhobosFlight_FlyToDesc".Translate(who),
            icon = Icon("FlyTo"),
            action = () => Find.Targeter.BeginTargeting(CellsOnly, target => flight.FlyTo(target.Cell)),
        };
        if (busy) flyTo.Disable("PhobosFlight_RefuseNotFlying".Translate(who));
        yield return flyTo;

        var hold = new Command_Action
        {
            defaultLabel = "PhobosFlight_Hold".Translate(),
            defaultDesc = "PhobosFlight_HoldDesc".Translate(who),
            icon = Icon("Hold"),
            action = flight.Hold,
        };
        if (!flight.CanHold) hold.Disable("PhobosFlight_RefuseNotMoving".Translate(who));
        yield return hold;

        var land = new Command_Action
        {
            defaultLabel = "PhobosFlight_Land".Translate(),
            defaultDesc = "PhobosFlight_LandDesc".Translate(who),
            icon = Icon("Land"),
            action = () => LandingTargeter.Instance.BeginTargeting(vehicle, vehicle.Map, (target, rot) => flight.Land(target.Cell, rot),
                target => flight.LandRefusal(target.Cell, vehicle.Rotation) == null, allowRotating: vehicle.VehicleDef.rotatable),
        };
        if (busy) land.Disable("PhobosFlight_RefuseNotFlying".Translate(who));
        yield return land;
    }

    static readonly TargetingParameters CellsOnly = new()
    {
        canTargetLocations = true,
        canTargetPawns = false,
        canTargetBuildings = false,
        canTargetItems = false,
    };
}

/// <summary>What the inspect pane says of an aircraft in the air, by the player's display mode.</summary>
public static class FlightText
{
    public static string Inspect(CompPhobosFlight flight)
    {
        string doing = flight.State switch
        {
            FlightState.LiftingOff => "PhobosFlight_StateLiftingOff".Translate().Resolve(),
            FlightState.Moving => "PhobosFlight_StateMoving".Translate().Resolve(),
            FlightState.Settling => "PhobosFlight_StateSettling".Translate().Resolve(),
            FlightState.ComingDown => "PhobosFlight_StateComingDown".Translate().Resolve(),
            _ => "PhobosFlight_StateHover".Translate().Resolve(),
        };
        DisplayMode mode = VehicleSystemsMod.Settings.displayMode;
        if (mode == DisplayMode.Plain) return "PhobosFlight_InspectPlain".Translate(doing.Named("DOING")).Resolve();
        float metres = flight.Climb * flight.Props.flightHeightMetres;
        string line = "PhobosFlight_InspectFigures".Translate(doing.Named("DOING"), metres.ToString("0").Named("METRES"),
            flight.Speed.ToString("0.0").Named("SPEED")).Resolve();
        if (mode != DisplayMode.Engineer || float.IsNaN(flight.Margin)) return line;
        return line + "\n" + "PhobosFlight_InspectMargin".Translate(flight.Margin.ToStringPercent().Named("MARGIN")).Resolve();
    }
}

/// <summary>The shadow beneath an aircraft in the air: on the game's shadow layer, sliding away and shrinking as it climbs.</summary>
[StaticConstructorOnStartup]
public static class FlightDrawing
{
    static readonly Material ShadowMaterial = MaterialPool.MatFrom("Things/Skyfaller/SkyfallerShadowCircle", ShaderDatabase.Transparent);

    public static void Shadow(CompPhobosFlight flight)
    {
        IntVec2 size = flight.Vehicle.VehicleDef.Size;
        float climb = flight.Climb;
        (double ox, double oz) = FlightPathRule.ShadowOffset(climb, flight.Props.flightHeightMetres / FlightRules.MetresPerCell);
        Vector3 centre = flight.TrueCenter;
        var at = new Vector3(centre.x + (float)ox, AltitudeLayer.Shadows.AltitudeFor(), centre.z + (float)oz);
        float shrink = 1f - 0.3f * climb;
        var scale = new Vector3(size.x * shrink, 1f, size.z * shrink);
        Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(at, Quaternion.AngleAxis(flight.Heading, Vector3.up), scale), ShadowMaterial, 0);
    }
}
