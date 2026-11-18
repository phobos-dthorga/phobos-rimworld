using System.Collections.Generic;
using Phobos.Framework.Vehicles.Rules;
using SmashTools.Rendering;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>One side's nacelle and rotor, in cells from the body's centre with the vehicle facing north (x east, z north).</summary>
public class TiltrotorSide
{
    /// <summary>The nacelle's part key, whose health shows its damage picture.</summary>
    public string part = "";
    /// <summary>The rotor's hub with the nacelle upright.</summary>
    public Vector2 hub;
    /// <summary>The nacelle's rear end, the pivot it tips about: its picture's bottom edge.</summary>
    public Vector2 pivot;
    /// <summary>Drawn as the left side's pictures turned over, for an aircraft whose two sides are mirror images.</summary>
    public bool mirrored;
    /// <summary>"Left" or "Right": which damage picture is this side's.</summary>
    public string damageSide = "Left";
    /// <summary>The still blades' turn, in degrees, so the two rotors do not look stamped from one picture.</summary>
    public float bladeStart;
}

/// <summary>A nacelle picture set shown while an upgrade is fitted (the Osprey's inlet barrier): its suffix after the angle.</summary>
public class TiltrotorVariant
{
    public string node = "";
    public string suffix = "";
}

public class CompProperties_PhobosTiltrotor : VehicleCompProperties
{
    /// <summary>The nacelle pictures' path before the angle, as Phobos/Vehicles/Osprey/Osprey_Nacelle (then 0, 30...; a mask beside each).</summary>
    public string nacellePath = "";
    public List<float> angles = new() { 0f, 30f, 60f, 90f };
    public List<TiltrotorVariant> variants = new();
    /// <summary>Damage over a nacelle below this share of its health (as our other damage looks).</summary>
    public float damageBelowHealth = 0.6f;
    public string rotorPath = "";
    public string rotorSpinPath = "";
    /// <summary>The rotor picture's width in cells: the real rotor's diameter at the vehicle's scale, plus its margin.</summary>
    public float rotorDrawCells = 14f;
    /// <summary>The pictures' scale.</summary>
    public float pixelsPerCell = 128f;
    /// <summary>How fast the spinning disc turns as drawn, in degrees a tick: a shimmer, not the real rpm [A].</summary>
    public float spinDegreesPerTick = 23f;
    public List<TiltrotorSide> sides = new();

    public CompProperties_PhobosTiltrotor() => compClass = typeof(CompPhobosTiltrotor);

    public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
    {
        foreach (string error in base.ConfigErrors(parentDef)) yield return error;
        if (nacellePath.NullOrEmpty()) yield return "a tiltrotor needs nacellePath";
        if (rotorPath.NullOrEmpty() || rotorSpinPath.NullOrEmpty()) yield return "a tiltrotor needs rotorPath and rotorSpinPath";
        if (angles.Count == 0) yield return "a tiltrotor needs at least one nacelle angle";
        if (sides.Count == 0) yield return "a tiltrotor needs at least one side";
    }
}

/// <summary>
/// Draws a tiltrotor's nacelles and rotors (docs/development/vehicle-osprey.md, "Tilting nacelles"; Framework 0.146.0): a renderer of
/// ours added to Vehicle Framework's draw tracker (public, no Harmony), so it draws on the map and through Vehicle Framework's take-off and
/// landing, which draw the vehicle the same way. The nacelles are the nearest of a few pictures at set angles, in the vehicle's paint;
/// each rotor is still blades with the engines off and a spinning disc with them on, drawn shortened along the aircraft's length by the
/// cosine of the tilt and moved forward as the nacelle tips (TiltrotorRule). Upright on the map; tipping forward through the climb away
/// and back on the approach. Presentation only: it reads the vehicle and changes nothing, and saves nothing.
/// </summary>
public class CompPhobosTiltrotor : VehicleComp
{
    Renderer? renderer;
    readonly Dictionary<string, Graphic> graphics = new();
    readonly Dictionary<string, Vector2> sizes = new();
    Color paintOne;
    Color paintTwo;
    float reachCells = float.NaN;

    CompProperties_PhobosTiltrotor Props => (CompProperties_PhobosTiltrotor)props;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (renderer != null || Vehicle.DrawTracker == null) return;
        renderer = new Renderer(this);
        Vehicle.DrawTracker.AddRenderer(renderer);   // Kept for the vehicle's life: its take-off and landing draw through it too.
    }

    /// <summary>The nacelles' tilt now: upright on the map, and through a VTOL take-off or landing by how far along it is.</summary>
    float Tilt()
    {
        if (Vehicle.Spawned) return 0f;
        if (Vehicle.CompVehicleLauncher?.launchProtocol is not LaunchProtocol protocol) return 0f;
        bool landing = protocol.CurAnimationProperties == protocol.LandingProperties;
        float vertical = protocol is VTOLTakeoff vtol ? vtol.TimeInAnimationVTOL : 1f;
        return TiltrotorRule.LaunchTilt(landing, vertical, protocol.TimeInAnimation);
    }

    /// <summary>Engines running: the rotors spin. On the map, while it is drafted (its engines on); always in a take-off or landing.</summary>
    bool Running() => !Vehicle.Spawned || Vehicle.Drafted;

    void Draw(in TransformData transform)
    {
        if (Props.sides.Count == 0) return;
        if (paintOne != Vehicle.DrawColor || paintTwo != Vehicle.DrawColorTwo)
        {
            paintOne = Vehicle.DrawColor;
            paintTwo = Vehicle.DrawColorTwo;
            foreach (string key in new List<string>(graphics.Keys))
            {
                if (key.StartsWith("paint:")) graphics.Remove(key);
            }
        }
        float tilt = Tilt();
        int picture = TiltrotorRule.NearestPicture(tilt, Props.angles);
        if (picture < 0) return;
        string angle = ((int)Props.angles[picture]).ToString();
        string suffix = VariantSuffix();
        float facing = transform.orientation.AsAngle + transform.rotation;
        Quaternion turn = Quaternion.AngleAxis(facing, Vector3.up);
        float baseY = transform.position.y;
        bool running = Running();
        int ticks = Find.TickManager?.TicksGame ?? 0;
        if (float.IsNaN(reachCells)) reachCells = Reach();
        bool noted = false;
        foreach (TiltrotorSide side in Props.sides)
        {
            Mesh mesh = side.mirrored ? MeshPool.plane10Flip : MeshPool.plane10;
            // The nacelle: its picture's bottom edge on the pivot, so it grows forward as it tips.
            string nacelle = Props.nacellePath + angle + suffix;
            Graphic nacelleGraphic = Painted(nacelle);
            Vector2 nacelleSize = SizeOf(nacelle, nacelleGraphic);
            Vector3 nacelleAt = transform.position + turn * new Vector3(side.pivot.x, 0f, side.pivot.y + nacelleSize.y / 2f);
            nacelleAt.y = baseY + Altitudes.AltInc * 2f;
            Graphics.DrawMesh(mesh, Matrix4x4.TRS(nacelleAt, turn, new Vector3(nacelleSize.x, 1f, nacelleSize.y)), nacelleGraphic.MatSingle, 0);
            if (Vehicle.statHandler?.GetComponent(side.part) is { } part && part.HealthPercent < Props.damageBelowHealth)
            {
                string damage = Props.nacellePath + angle + "Damage" + side.damageSide;
                Graphic damageGraphic = Bare(damage);
                Vector3 damageAt = nacelleAt;
                damageAt.y = baseY + Altitudes.AltInc * 2.5f;
                Graphics.DrawMesh(mesh, Matrix4x4.TRS(damageAt, turn, new Vector3(nacelleSize.x, 1f, nacelleSize.y)), damageGraphic.MatSingle, 0);
            }
            // The rotor: on the hub upright, moving forward as the nacelle tips, its disc shortened along the length by the cosine.
            Graphic rotorGraphic = Bare(running ? Props.rotorSpinPath : Props.rotorPath);
            float forward = TiltrotorRule.RotorForward(tilt, reachCells);
            Vector3 rotorAt = transform.position + turn * new Vector3(side.hub.x, 0f, side.hub.y + forward);
            rotorAt.y = baseY + Altitudes.AltInc * 3f;
            float spin = running
                ? TiltrotorRule.SpinAngle(ticks, Props.spinDegreesPerTick, side.bladeStart, side.mirrored)
                : side.bladeStart;
            Matrix4x4 rotor = Matrix4x4.TRS(rotorAt, turn, new Vector3(1f, 1f, TiltrotorRule.DiscLength(tilt)))
                              * Matrix4x4.TRS(Vector3.zero, Quaternion.AngleAxis(spin, Vector3.up),
                                              new Vector3(Props.rotorDrawCells, 1f, Props.rotorDrawCells));
            Graphics.DrawMesh(mesh, rotor, rotorGraphic.MatSingle, 0);
            noted = true;
        }
        if (noted && Seen.Wants("vehicles.tiltrotor-drawn", Vehicle, VariantOf(tilt, running)))
        {
            Seen.Note("vehicles.tiltrotor-drawn", Vehicle, variant: VariantOf(tilt, running),
                      values: new List<KeyValuePair<string, double>> { new("tilt", tilt) });
        }
    }

    /// <summary>How far the rotor moves forward at 90 degrees: from the hub to the forward nacelle picture's front, in cells.</summary>
    float Reach()
    {
        int last = Props.angles.Count - 1;
        string forward = Props.nacellePath + ((int)Props.angles[last]).ToString();
        Vector2 size = SizeOf(forward, Bare(forward));
        TiltrotorSide side = Props.sides[0];
        return Mathf.Max(0f, side.pivot.y + size.y - side.hub.y);
    }

    static string VariantOf(float tilt, bool running) => tilt <= 0f ? (running ? "spinning" : "still") : tilt >= 90f ? "forward" : "tilting";

    string VariantSuffix()
    {
        if (Props.variants.Count == 0 || Vehicle.GetComp<CompUpgradeTree>() is not { } tree) return "";
        foreach (TiltrotorVariant variant in Props.variants)
        {
            if (tree.Props.def.GetNode(variant.node) is { } node && tree.NodeUnlocked(node)) return variant.suffix;
        }
        return "";
    }

    Graphic Painted(string path)
    {
        string key = "paint:" + path;
        if (!graphics.TryGetValue(key, out Graphic? graphic))
        {
            graphic = GraphicDatabase.Get<Graphic_Single>(path, ShaderDatabase.CutoutComplex, Vector2.one, paintOne, paintTwo);
            graphics[key] = graphic;
        }
        return graphic;
    }

    Graphic Bare(string path)
    {
        if (!graphics.TryGetValue(path, out Graphic? graphic))
        {
            graphic = GraphicDatabase.Get<Graphic_Single>(path, ShaderDatabase.Cutout, Vector2.one, Color.white);
            graphics[path] = graphic;
        }
        return graphic;
    }

    /// <summary>A picture's size in cells, from its texture at the set scale.</summary>
    Vector2 SizeOf(string path, Graphic graphic)
    {
        if (!sizes.TryGetValue(path, out Vector2 size))
        {
            Texture texture = graphic.MatSingle.mainTexture;
            size = texture != null ? new Vector2(texture.width, texture.height) / Props.pixelsPerCell : Vector2.one;
            sizes[path] = size;
        }
        return size;
    }

    /// <summary>Vehicle Framework calls this in every draw phase; it draws in the draw phase only, on the main thread.</summary>
    sealed class Renderer : IParallelRenderer
    {
        readonly CompPhobosTiltrotor comp;

        public Renderer(CompPhobosTiltrotor comp) => this.comp = comp;

        public bool IsDirty { get; set; }

        public void DynamicDrawPhaseAt(DrawPhase phase, in TransformData transformData, bool forceDraw = false)
        {
            if (phase == DrawPhase.Draw) comp.Draw(in transformData);
        }
    }
}
