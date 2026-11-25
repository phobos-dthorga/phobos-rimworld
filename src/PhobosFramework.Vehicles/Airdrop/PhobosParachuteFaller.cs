using System;
using System.Linq;
using Phobos.Framework.Environment;
using Phobos.Framework.Rules;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A load coming down under a parachute (D'thorga Framework 0.288.0; docs/development/airdrop.md): a person, a bundle of cargo or a vehicle on
/// its platform. It is a true skyfaller and an active transporter, so Combat Extended's CIWS sees it as it sees a drop pod, leading it by the
/// game's own skyfaller sums. Its drawn height falls with the ticks left (straight up the screen, as CE reads it), while its ground cell moves
/// along its drift from where it was let go to where the wind brings it, so the drawing, CE's aim and CE's hit test agree to within a cell.
/// It never hits a roof and never destroys what it carries: it lands it through <see cref="ParachuteLanding"/>.
/// </summary>
public class PhobosParachuteFaller : Skyfaller, IActiveTransporter
{
    const float CanopyRise = 1.1f;

    // Saved.
    public ParachuteLoad load;
    float groundX, groundZ, stepX, stepZ;
    int plannedTicks;
    public float heightFeet;
    public Rot4 vehicleRot = Rot4.North;
    public string from = "";
    public bool landWithCrew;
    /// <summary>The packer's quality of its canopy (the game's, 0 Awful to 6 Legendary; -1 unknown). Framework 0.293.0.</summary>
    public int chuteQuality = -1;
    /// <summary>A guided canopy, steering itself onto its point (Framework 0.293.0).</summary>
    public bool guided;
    /// <summary>Brought down by another mod's interceptor (Framework 0.297.0): it lands at once, its canopy shredded. Not saved: it lands
    /// in the same tick.</summary>
    public bool shotDown;

    ActiveTransporterInfo? info;

    /// <summary>What the game's transporter code and Combat Extended's CIWS read: nothing of ours is kept in it.</summary>
    public ActiveTransporterInfo Contents => info ??= new ActiveTransporterInfo();

    /// <summary>
    /// Sets where it falls before it is spawned: let go over <paramref name="release"/> (a true point on the map), it comes down on
    /// <paramref name="landing"/> after <paramref name="ticks"/>.
    /// </summary>
    public void Plan(ParachuteLoad kind, Vector3 release, Vector3 landing, int ticks, float feet)
    {
        load = kind;
        plannedTicks = Mathf.Max(30, ticks);
        groundX = release.x;
        groundZ = release.z;
        stepX = (landing.x - release.x) / plannedTicks;
        stepZ = (landing.z - release.z) / plannedTicks;
        heightFeet = feet;
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        if (respawningAfterLoad) return;
        ticksToImpact = plannedTicks;
        // Straight up the screen: the drawn offset is all height, as Combat Extended reads a skyfaller.
        angle = 0f;
    }

    public override Vector3 DrawPos
    {
        get
        {
            float up = Mathf.Max(ticksToImpact, 0) * def.skyfaller.speed;
            // Framework 0.293.0: drawn no higher than the map's top edge, since Combat Extended puts what a CIWS shoots down at the drawn
            // point; above the edge it would be off the map.
            float top = Spawned ? Map.Size.z - 1.01f : float.MaxValue;
            return new Vector3(groundX, def.Altitude, Mathf.Min(groundZ + up, top));
        }
    }

    protected override void Tick()
    {
        if (Spawned && !hasImpacted)
        {
            groundX += stepX;
            groundZ += stepZ;
            IntVec3 cell = new IntVec3(Mathf.FloorToInt(groundX), 0, Mathf.FloorToInt(groundZ)).ClampInsideMap(Map);
            if (cell != Position) Position = cell;
        }
        base.Tick();
    }

    /// <summary>
    /// Brought down by an interceptor that would otherwise end it its own way (Combat Extended's CIWS kills everyone inside): it comes down
    /// where it is now, its canopy shredded, as an intercepted load does (the owner's ask of 26 Nov 2026 for a softer outcome).
    /// </summary>
    public void ShootDown()
    {
        if (hasImpacted || !Spawned) return;
        shotDown = true;
        hasImpacted = true;
        ParachuteLanding.Land(this);
        Destroy();
    }

    protected override void Impact()
    {
        hasImpacted = true;
        ParachuteLanding.Land(this);
        Destroy();
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        // Destroyed before it landed (a developer, a mod, the map going), it puts what it carries down where it is, never losing it.
        // Combat Extended's CIWS empties it first, by its own rule, so it finds nothing here.
        if (!hasImpacted && Spawned && innerContainer.Count > 0)
        {
            hasImpacted = true;
            ParachuteLanding.Land(this);
        }
        base.Destroy(mode);
    }

    // ---- Drawing ----

    static Graphic? canopy, bundle, platform;

    static Graphic Canopy => canopy ??= GraphicDatabase.Get<Graphic_Single>("Phobos/VehicleSystems/Airdrop/CanopyOpen", ShaderDatabase.Cutout, Vector2.one, Color.white);
    static Graphic Bundle => bundle ??= GraphicDatabase.Get<Graphic_Single>("Phobos/VehicleSystems/Airdrop/CargoBundle", ShaderDatabase.Cutout, Vector2.one, Color.white);
    static Graphic Platform => platform ??= GraphicDatabase.Get<Graphic_Single>("Phobos/VehicleSystems/Airdrop/DropPlatform", ShaderDatabase.Cutout, Vector2.one, Color.white);

    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        Vector3 at = DrawPos;
        Thing? inside = innerContainer.Count > 0 ? innerContainer[0] : null;
        float alt = def.Altitude;
        switch (load)
        {
            case ParachuteLoad.Vehicle when inside is VehiclePawn vehicle:
            {
                IntVec2 size = vehicle.VehicleDef.Size;
                Vector2 plate = vehicleRot.IsHorizontal ? new Vector2(size.z, size.x) : new Vector2(size.x, size.z);
                DrawScaled(Platform, new Vector3(at.x, alt - 0.02f, at.z), plate * 1.05f);
                vehicle.DynamicDrawPhaseAt(DrawPhase.Draw, new Vector3(at.x, alt, at.z), flip: flip);
                float span = Mathf.Max(plate.x, 2f);
                float each = Mathf.Clamp(span * 0.75f, 2.5f, 5f);
                float top = at.z + plate.y / 2f + CanopyRise;
                for (int i = -1; i <= 1; i++)
                {
                    var c = new Vector3(at.x + i * each * 0.7f, alt + 0.03f, top + (i == 0 ? each * 0.25f : 0f));
                    DrawLine(new Vector3(at.x + i * plate.x * 0.4f, alt, at.z + plate.y * 0.4f), c);
                    DrawScaled(Canopy, c, new Vector2(each, each));
                }
                break;
            }
            case ParachuteLoad.Person when inside is Pawn pawn:
            {
                pawn.DrawNowAt(new Vector3(at.x, alt, at.z), flip);
                var c = new Vector3(at.x, alt + 0.03f, at.z + CanopyRise + 0.6f);
                DrawLine(new Vector3(at.x - 0.25f, alt, at.z + 0.3f), c);
                DrawLine(new Vector3(at.x + 0.25f, alt, at.z + 0.3f), c);
                DrawScaled(Canopy, c, new Vector2(2.4f, 2.4f));
                break;
            }
            default:
            {
                DrawScaled(Bundle, new Vector3(at.x, alt, at.z), new Vector2(1.3f, 1.3f));
                var c = new Vector3(at.x, alt + 0.03f, at.z + CanopyRise + 0.9f);
                DrawLine(new Vector3(at.x - 0.4f, alt, at.z + 0.4f), c);
                DrawLine(new Vector3(at.x + 0.4f, alt, at.z + 0.4f), c);
                DrawScaled(Canopy, c, new Vector2(3.4f, 3.4f));
                break;
            }
        }
        DrawDropSpotShadow();
    }

    static void DrawScaled(Graphic graphic, Vector3 at, Vector2 size)
    {
        Matrix4x4 matrix = Matrix4x4.TRS(at, Quaternion.identity, new Vector3(size.x, 1f, size.y));
        Graphics.DrawMesh(MeshPool.plane10, matrix, graphic.MatSingle, 0);
    }

    static void DrawLine(Vector3 from, Vector3 to) => GenDraw.DrawLineBetween(from, to, SimpleColor.White, 0.04f);

    // ---- Saving ----

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref load, "PhobosChute_Load");
        Scribe_Values.Look(ref groundX, "PhobosChute_GroundX");
        Scribe_Values.Look(ref groundZ, "PhobosChute_GroundZ");
        Scribe_Values.Look(ref stepX, "PhobosChute_StepX");
        Scribe_Values.Look(ref stepZ, "PhobosChute_StepZ");
        Scribe_Values.Look(ref plannedTicks, "PhobosChute_Ticks");
        Scribe_Values.Look(ref heightFeet, "PhobosChute_HeightFeet");
        Scribe_Values.Look(ref vehicleRot, "PhobosChute_VehicleRot", Rot4.North);
        Scribe_Values.Look(ref from, "PhobosChute_From", "");
        Scribe_Values.Look(ref landWithCrew, "PhobosChute_WithCrew");
        Scribe_Values.Look(ref chuteQuality, "PhobosChute_Quality", -1);
        Scribe_Values.Look(ref guided, "PhobosChute_Guided");
    }

    // ---- Letting go ----

    /// <summary>The faller Def for a kind of load.</summary>
    public static ThingDef DefFor(ParachuteLoad kind) => kind switch
    {
        ParachuteLoad.Person => AirdropDefOf.Phobos_ParachuteFaller_Person,
        ParachuteLoad.Bundle => AirdropDefOf.Phobos_ParachuteFaller_Bundle,
        _ => AirdropDefOf.Phobos_ParachuteFaller_Vehicle,
    };

    /// <summary>
    /// Lets one load go over <paramref name="release"/>, from <paramref name="heightFeet"/>: works out where the wind carries it
    /// (ParachuteRule, the map's wind, the map's scale) and how long it falls, and spawns it falling. The load must be in no container and
    /// not spawned. Returns where it is expected to land (before scatter is known to the player: the drifted point).
    /// </summary>
    public static PhobosParachuteFaller Release(Map map, Thing contents, ParachuteLoad kind, Vector3 release, float heightFeet, string from, bool withCrew = false,
        int quality = -1, Vector3? guidedTo = null)
    {
        ParachuteRule.Figures figures = ParachuteRule.For(kind);
        Vector3 landing = guidedTo is { } aim ? Guided(map, aim) : Landing(map, kind, release, heightFeet, scatter: true);
        double seconds = ParachuteRule.Opens(heightFeet)
            ? ParachuteRule.DescentSeconds(heightFeet, figures.DescentFeetPerSecond)
            : Math.Sqrt(2 * heightFeet * ParachuteRule.MetresPerFoot / 9.81);
        int ticks = Mathf.RoundToInt((float)(seconds * ParachuteRule.MapDescentTimeShare * 60.0));
        var faller = (PhobosParachuteFaller)ThingMaker.MakeThing(DefFor(kind));
        faller.Plan(kind, release, landing, ticks, heightFeet);
        faller.from = from;
        faller.landWithCrew = withCrew;
        faller.chuteQuality = quality;
        faller.guided = guidedTo != null;
        if (contents is VehiclePawn vehicle) faller.vehicleRot = vehicle.Rotation;
        faller.innerContainer.TryAdd(contents, canMergeWithExistingStacks: false);
        IntVec3 cell = release.ToIntVec3().ClampInsideMap(map);
        GenSpawn.Spawn(faller, cell, map, WipeMode.Vanish);
        return faller;
    }

    /// <summary>Where a load let go over <paramref name="release"/> comes down: drifted downwind by the map's share of the real drift, with a
    /// random scatter when asked (none for the targeter's preview). A true point, kept inside the map.</summary>
    public static Vector3 Landing(Map map, ParachuteLoad kind, Vector3 release, float heightFeet, bool scatter)
    {
        float cells = DriftCells(map, kind, heightFeet);
        Vector3 downwind = PhobosWind.Downwind(map);
        Vector3 point = release + downwind * cells;
        if (scatter)
        {
            Vector2 spread = Rand.InsideUnitCircle * (float)ParachuteRule.ScatterCells(cells);
            point += new Vector3(spread.x, 0f, spread.y);
        }
        point.x = Mathf.Clamp(point.x, 1f, map.Size.x - 1.01f);
        point.z = Mathf.Clamp(point.z, 1f, map.Size.z - 1.01f);
        point.y = 0f;
        return point;
    }

    /// <summary>Where a guided canopy comes down: on its point, within its small spread, whatever the wind (Framework 0.293.0).</summary>
    static Vector3 Guided(Map map, Vector3 aim)
    {
        Vector2 spread = Rand.InsideUnitCircle * (float)ParachuteRule.GuidedScatterCells;
        var point = new Vector3(Mathf.Clamp(aim.x + spread.x, 1f, map.Size.x - 1.01f), 0f, Mathf.Clamp(aim.z + spread.y, 1f, map.Size.z - 1.01f));
        return point;
    }

    /// <summary>How far a load drifts on this map's wind from this height, in cells: the real drift at the map's share (ParachuteRule).</summary>
    public static float DriftCells(Map map, ParachuteLoad kind, float heightFeet)
    {
        if (!ParachuteRule.Opens(heightFeet)) return 0f;
        double metres = ParachuteRule.DriftMetres(ParachuteRule.For(kind).DriftConstant, heightFeet, PhobosWind.Knots(map));
        return (float)(metres * ParachuteRule.MapDriftShare / FlightRules.MetresPerCell);
    }

    /// <summary>The real drift in metres, for the targeter's words.</summary>
    public static float DriftMetres(Map map, ParachuteLoad kind, float heightFeet) =>
        ParachuteRule.Opens(heightFeet) ? (float)ParachuteRule.DriftMetres(ParachuteRule.For(kind).DriftConstant, heightFeet, PhobosWind.Knots(map)) : 0f;
}
