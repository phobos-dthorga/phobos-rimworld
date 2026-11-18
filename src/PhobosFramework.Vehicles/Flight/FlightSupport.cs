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
        // Fault-read W3 (Framework 0.167.0): only with the full drivetrain model, as the hover limits, landing check and cruise height.
        if (train == null || !train.JudgesHover || VehicleSystemsMod.Settings.drivetrainMode != DrivetrainMode.Full) return float.NaN;
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

    /// <summary>Whether it could still hold a hover with this much more weight on it (Framework 0.165.0, shared by our flight and the sling for
    /// any helicopter): the engine model's hover check at the heavier weight, under the player's hover-limit setting; true where its engines
    /// are not judged.</summary>
    public static bool WouldHoverWith(VehiclePawn vehicle, double extraKg)
    {
        if (CompPhobosVehicleSystems.Of(vehicle) is not { Powertrain: { JudgesHover: true } train } systems) return true;
        if (VehicleSystemsMod.Settings.drivetrainMode != DrivetrainMode.Full) return true;
        if (train.Engines().working <= 0) return false;
        (bool dry, bool wet, double m) = train.HoverCheck(train.MassKg() + System.Math.Max(0, extraKg), systems.Ambient, systems.AirPressure);
        float share = train.Profile?.forwardFlightShare ?? 0.6f;
        return Hold(vehicle, dry || wet ? Mathf.Max(1f, (float)m) : (float)m, share) == FlightHold.Hover;
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
    public static string? SpotRefusal(VehiclePawn vehicle, IntVec3 cell, Rot4 rot, bool forced) => SpotRefusal(vehicle, cell, rot, forced, knowsGround: false);

    /// <summary>The same, for an aircraft whose crew know the ground (a raid's, fault-read F8's split, Framework 0.163.0): unseen ground is no
    /// bar, but everything else is asked as of a chosen landing.</summary>
    public static string? SpotRefusal(VehiclePawn vehicle, IntVec3 cell, Rot4 rot, bool forced, bool knowsGround)
    {
        Map map = vehicle.Map;
        if (map == null || !cell.IsValid) return "PhobosFlight_SpotEdge".Translate().Resolve();
        // Fault-read F4 (Framework 0.163.0): a chosen spot's centre must be where its flight can bring it, inside the edge band. Not asked of a
        // forced landing, which comes down where its held-in centre already is (its cell's centre may round half a cell outward).
        if (!forced && !InsideEdge(vehicle, map, cell, rot)) return "PhobosFlight_SpotEdge".Translate().Resolve();
        VehiclePositionManager claims = map.GetDetachedMapComponent<VehiclePositionManager>();
        // Fault-read F10: Vehicle Framework's own rect (the game's is a row or column off for an even side facing south or west).
        foreach (IntVec3 c in vehicle.VehicleRect(cell, rot))
        {
            if (CellRefusal(vehicle, map, claims, c, forced, needsFloor: false, knowsGround) is { } why) return why;
        }
        return null;
    }

    /// <summary>Whether the vehicle's centre, standing at this cell facing this way, lies inside the band its flight holds it in: half its
    /// longest side and a cell in from every edge (CompPhobosFlight's EdgeBounds).</summary>
    public static bool InsideEdge(VehiclePawn vehicle, Map map, IntVec3 cell, Rot4 rot)
    {
        IntVec2 size = vehicle.VehicleDef.Size;
        float edge = Mathf.Max(size.x, size.z) / 2f + 1f;
        Vector3 c = Ext_Vehicles.TrueCenter(cell, new Rot8(rot), size, 0f);
        return c.x >= edge - 0.01f && c.x <= map.Size.x - edge + 0.01f && c.z >= edge - 0.01f && c.z <= map.Size.z - edge + 0.01f;
    }

    /// <summary>Why one cell will not take the vehicle on the ground, as words finishing "it cannot land there:", or null.</summary>
    static string? CellRefusal(VehiclePawn vehicle, Map map, VehiclePositionManager claims, IntVec3 c, bool forced, bool needsFloor, bool knowsGround = false)
    {
        if (!c.InBounds(map)) return "PhobosFlight_SpotEdge".Translate().Resolve();
        if (!forced && !knowsGround && c.Fogged(map)) return "PhobosFlight_SpotUnseen".Translate().Resolve();
        if (c.Roofed(map)) return "PhobosFlight_SpotRoof".Translate().Resolve();
        if (c.GetEdifice(map) != null) return "PhobosFlight_SpotBuilding".Translate().Resolve();
        if (claims.ClaimedBy(c) is { } other && other != vehicle) return "PhobosFlight_SpotVehicle".Translate().Resolve();
        if (!c.Standable(vehicle, map))
        {
            // Fault-read F8 (Framework 0.163.0): trees, bushes or rubble refuse an ordered landing, said as such; they do not wreck a forced
            // one, which wrecks only on impassable or deep-water ground (and a building or another vehicle, above).
            if (forced) return c.Walkable(map) ? null : "PhobosFlight_SpotGround".Translate().Resolve();
            return c.Walkable(map) ? "PhobosFlight_SpotObstructed".Translate().Resolve() : "PhobosFlight_SpotGround".Translate().Resolve();
        }
        if (needsFloor && !c.GetTerrain(map).layerable) return "PhobosFlight_SpotNoFloor".Translate().Resolve();
        return null;
    }

    /// <summary>
    /// The cells of a strip (Framework 0.151.0; the design's slice 4): the footprint run straight ahead from this cell, facing this way, for so
    /// many cells, as wide as the aircraft.
    /// </summary>
    public static IEnumerable<IntVec3> StripCells(VehiclePawn vehicle, IntVec3 start, Rot4 rot, int cells)
    {
        var seen = new HashSet<IntVec3>();
        IntVec3 step = rot.FacingCell;
        for (int i = 0; i <= cells; i++)
        {
            foreach (IntVec3 c in vehicle.VehicleRect(start + step * i, rot))
            {
                if (seen.Add(c)) yield return c;
            }
        }
    }

    /// <summary>Why a strip will not do for a roll, as words finishing "it cannot land there:", or null: every cell as a landing spot's,
    /// and a built floor where the aircraft needs one. The first blocked cell, for the picture.</summary>
    public static string? StripRefusal(VehiclePawn vehicle, IntVec3 start, Rot4 rot, int cells, bool needsFloor, out IntVec3 blockedAt)
    {
        blockedAt = IntVec3.Invalid;
        Map map = vehicle.Map;
        if (map == null || !start.IsValid) return "PhobosFlight_SpotEdge".Translate().Resolve();
        // Fault-read F4: both ends where its flight can bring its centre.
        if (!InsideEdge(vehicle, map, start, rot) || !InsideEdge(vehicle, map, start + rot.FacingCell * cells, rot))
        {
            blockedAt = start + rot.FacingCell * cells;
            return "PhobosFlight_SpotEdge".Translate().Resolve();
        }
        VehiclePositionManager claims = map.GetDetachedMapComponent<VehiclePositionManager>();
        foreach (IntVec3 c in StripCells(vehicle, start, rot, cells))
        {
            if (CellRefusal(vehicle, map, claims, c, forced: false, needsFloor) is { } why)
            {
                blockedAt = c;
                return why;
            }
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
        bool busy = flight.State is FlightState.LiftingOff or FlightState.ComingDown or FlightState.TakeoffRoll or FlightState.RollingLanding or FlightState.LineWork;
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
        if (flight.HoldRefusal() is { } holdRefused) hold.Disable(holdRefused);
        yield return hold;

        // Circling (Framework 0.153.0): a centre to fly round, and the size and way round, chosen from preset buttons.
        var circle = new Command_Action
        {
            defaultLabel = "PhobosFlight_Circle".Translate(),
            defaultDesc = "PhobosFlight_CircleDesc".Translate(who),
            icon = Icon("Circle"),
            action = () => Find.Targeter.BeginTargeting(CellsOnly, target => flight.Circle(target.Cell)),
        };
        if (busy || flight.State == FlightState.Settling) circle.Disable("PhobosFlight_RefuseNotFlying".Translate(who));
        yield return circle;

        foreach (Gizmo gizmo in LineGizmos(flight)) yield return gizmo;

        string size = SizeName(flight.OrbitSize);
        string way = WayName(flight.OrbitClockwise);
        yield return new Command_Action
        {
            defaultLabel = "PhobosFlight_CircleSettings".Translate(size.Named("SIZE"), way.Named("WAY")),
            defaultDesc = "PhobosFlight_CircleSettingsDesc".Translate(who),
            icon = Icon("CircleSize"),
            action = () => Find.WindowStack.Add(new FloatMenu(CircleOptions(flight))),
        };

        var land = new Command_Action
        {
            defaultLabel = "PhobosFlight_Land".Translate(),
            defaultDesc = "PhobosFlight_LandDesc".Translate(who),
            icon = Icon("Land"),
            action = () => LandingTargeter.Instance.BeginTargeting(vehicle, vehicle.Map, (target, rot) => flight.Land(target.Cell, rot),
                target => flight.LandRefusal(target.Cell, vehicle.Rotation) == null, allowRotating: vehicle.VehicleDef.rotatable),
        };
        if (busy || flight.State == FlightState.Settling) land.Disable("PhobosFlight_RefuseNotFlying".Translate(who));
        yield return land;
    }

    /// <summary>Ropes and the hoist (Framework 0.155.0): shown only with their fittings; a stop while at work.</summary>
    static IEnumerable<Gizmo> LineGizmos(CompPhobosFlight flight)
    {
        VehiclePawn vehicle = flight.Vehicle;
        NamedArgument who = vehicle.LabelShort.Named("VEHICLE");
        if (flight.State == FlightState.LineWork)
        {
            yield return new Command_Action
            {
                defaultLabel = "PhobosFlight_StopLines".Translate(),
                defaultDesc = "PhobosFlight_StopLinesDesc".Translate(who),
                icon = Icon("Hold"),
                action = flight.StopLines,
            };
        }
        if (flight.CanRope)
        {
            var rope = new Command_Action
            {
                defaultLabel = "PhobosFlight_RopeDown".Translate(),
                defaultDesc = "PhobosFlight_RopeDownDesc".Translate(who, flight.Props.workHeightMetres.ToString("0").Named("METRES")),
                icon = SystemIcons.For("Phobos/VehicleSystems/Fittings/FastRope"),
                action = flight.RopeDown,
            };
            if (flight.RopeRefusal() is { } refused) rope.Disable(refused);
            yield return rope;
        }
        if (flight.HasHoist)
        {
            var hoist = new Command_Action
            {
                defaultLabel = "PhobosFlight_Hoist".Translate(),
                defaultDesc = "PhobosFlight_HoistDesc".Translate(who, flight.Props.hoistRatedKilograms.ToString("0").Named("RATED")),
                icon = SystemIcons.For("Phobos/VehicleSystems/Fittings/RescueHoist"),
                action = () => Find.WindowStack.Add(new FloatMenu(HoistOptions(flight))),
            };
            if (flight.HoistRefusal() is { } refused) hoist.Disable(refused);
            yield return hoist;
        }
    }

    /// <summary>Everyone the hoist could lift, and everyone aboard it could lower: one button each, a refusal shown greyed with its reason.</summary>
    static List<FloatMenuOption> HoistOptions(CompPhobosFlight flight)
    {
        var options = new List<FloatMenuOption>();
        foreach (Pawn pawn in flight.HoistCandidates())
        {
            string label = "PhobosFlight_HoistLift".Translate(pawn.LabelShort.Named("PERSON")).Resolve();
            options.Add(flight.HoistUpRefusal(pawn) is { } why
                ? new FloatMenuOption("PhobosFlight_OptionRefused".Translate(label.Named("OPTION"), why.Named("WHY")).Resolve(), null)
                : new FloatMenuOption(label, () => flight.HoistUp(pawn)));
        }
        foreach (Pawn pawn in flight.Vehicle.AllPawnsAboard.ToList())
        {
            if (pawn.Downed || flight.Vehicle.handlers.Any(h => h.role.HandlingTypes != 0 && h.thingOwner != null && h.thingOwner.Contains(pawn))) continue;
            string label = "PhobosFlight_HoistLower".Translate(pawn.LabelShort.Named("PERSON")).Resolve();
            options.Add(flight.HoistDownRefusal(pawn) is { } why
                ? new FloatMenuOption("PhobosFlight_OptionRefused".Translate(label.Named("OPTION"), why.Named("WHY")).Resolve(), null)
                : new FloatMenuOption(label, () => flight.HoistDown(pawn)));
        }
        if (options.Count == 0)
        {
            options.Add(new FloatMenuOption("PhobosFlight_HoistNobody".Translate(flight.Props.hoistReachCells.ToString("0").Named("CELLS")).Resolve(), null));
        }
        return options;
    }

    static string SizeName(int size) => size switch
    {
        0 => "PhobosFlight_CircleTight".Translate().Resolve(),
        1 => "PhobosFlight_CircleWide".Translate().Resolve(),
        _ => "PhobosFlight_CircleVeryWide".Translate().Resolve(),
    };

    static string WayName(bool clockwise) =>
        (clockwise ? "PhobosFlight_Clockwise" : "PhobosFlight_Anticlockwise").Translate().Resolve();

    /// <summary>The preset sizes, the present one marked, and the other way round: each one button, as the game's own menus are.</summary>
    static List<FloatMenuOption> CircleOptions(CompPhobosFlight flight)
    {
        var options = new List<FloatMenuOption>();
        for (int i = 0; i < OrbitRule.SizeShares.Length; i++)
        {
            int size = i;
            string label = "PhobosFlight_CircleSizeOption".Translate(SizeName(size).Named("SIZE"),
                Mathf.RoundToInt(flight.RadiusOfSize(size)).Named("CELLS")).Resolve();
            if (size == flight.OrbitSize) label = "PhobosFlight_CircleCurrent".Translate(label.Named("OPTION")).Resolve();
            options.Add(new FloatMenuOption(label, () => flight.SetOrbitSize(size)));
        }
        string other = WayName(!flight.OrbitClockwise);
        options.Add(new FloatMenuOption("PhobosFlight_CircleOtherWay".Translate(other.Named("WAY")).Resolve(), flight.ReverseOrbit));
        return options;
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
            FlightState.TakeoffRoll => "PhobosFlight_StateTakeoffRoll".Translate().Resolve(),
            FlightState.RollingLanding => "PhobosFlight_StateRollingLanding".Translate().Resolve(),
            FlightState.Moving => "PhobosFlight_StateMoving".Translate().Resolve(),
            FlightState.Orbit => "PhobosFlight_StateOrbit".Translate().Resolve(),
            FlightState.LineWork when flight.Lines == LineJob.RopeDown => "PhobosFlight_StateRoping".Translate().Resolve(),
            FlightState.LineWork => "PhobosFlight_StateHoisting".Translate().Resolve(),
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

    // The strip last worked out (Framework 0.151.0): drawn every frame while selected, worked out again every half second.
    static VehiclePawn? stripFor;
    static int stripAt = -1000;
    static readonly List<IntVec3> stripCells = new();
    static bool stripClear;
    static IntVec3 stripFrom;
    static Rot4 stripFacing;
    const int StripEveryTicks = 30;

    /// <summary>
    /// The strip ahead of a selected aircraft on the ground that is too heavy to lift straight up but can roll (Framework 0.151.0): its
    /// outline in green while clear, red while something blocks it. Nothing is drawn for one that can lift straight up or cannot leave.
    /// </summary>
    public static void Strip(CompPhobosFlight flight)
    {
        VehiclePawn v = flight.Vehicle;
        int now = Find.TickManager.TicksGame;
        if (stripFor != v || now - stripAt >= StripEveryTicks || now < stripAt || v.Position != stripFrom || v.Rotation != stripFacing)
        {
            stripFor = v;
            stripAt = now;
            stripFrom = v.Position;
            stripFacing = v.Rotation;
            stripCells.Clear();
            (RollVerdict verdict, int cells, _) = flight.RollNeed();
            if (verdict == RollVerdict.Rolls)
            {
                stripCells.AddRange(FlightRules.StripCells(v, v.Position, v.Rotation, cells));
                stripClear = flight.StripAheadRefusal(cells) == null;
            }
        }
        if (stripCells.Count > 0) GenDraw.DrawFieldEdges(stripCells, stripClear ? Color.green : Color.red);
    }
}
