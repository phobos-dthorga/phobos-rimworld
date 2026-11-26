using System.Collections.Generic;
using SmashTools.Rendering;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// An aeroplane's propellers seen from straight above (D'thorga Framework 0.316.0; the owner's card of 26 Nov 2026 for the C-27J: "Edge-on
/// bar"): a tractor propeller spins in an upright plane ahead of its nacelle, so from above it shows edge-on, a thin bar across the nose, not
/// the disc a helicopter's rotor shows. Vehicle Framework's propeller overlay turns a disc in the screen's plane, which suits rotors only. This
/// draws each propeller as a still bar while it stands, and as a see-through blurred bar while it turns, after our flight's own spin-up and
/// run-down (<see cref="CompPhobosFlight.RotorShare"/>). Pictures and places are the Def's; another author may give it to an aeroplane of theirs.
/// </summary>
public class CompProperties_PhobosPropellers : VehicleCompProperties
{
    /// <summary>The still propeller seen edge-on, a bar, centred on its hub.</summary>
    public string stillPath = "";
    /// <summary>The turning propeller's blur, centred on its hub; drawn see-through.</summary>
    public string spinPath = "";
    /// <summary>Its drawn size, cells: across the blades, and fore and aft.</summary>
    public Vector2 drawSize = new(4.8f, 0.6f);
    /// <summary>Each propeller's hub facing north, cells from the vehicle's centre: x to the right, y forward.</summary>
    public List<Vector2> hubs = new();
    /// <summary>The share of full speed from which it draws the blur rather than the still blades (authored).</summary>
    public float blurFromShare = 0.3f;

    public CompProperties_PhobosPropellers() => compClass = typeof(CompPhobosPropellers);

    public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
    {
        foreach (string error in base.ConfigErrors(parentDef)) yield return error;
        if (stillPath.NullOrEmpty() || spinPath.NullOrEmpty()) yield return "propellers need stillPath and spinPath";
        if (hubs.Count == 0) yield return "propellers need at least one hub";
        if (drawSize.x <= 0f || drawSize.y <= 0f) yield return "propellers need a positive drawSize";
    }
}

/// <summary>Draws an aeroplane's propellers as bars from above; see <see cref="CompProperties_PhobosPropellers"/>. Holds no state of its own.</summary>
public class CompPhobosPropellers : VehicleComp
{
    Renderer? renderer;
    Graphic? still;
    Graphic? spin;
    CompPhobosFlight? flight;
    bool flightRead;

    CompProperties_PhobosPropellers Props => (CompProperties_PhobosPropellers)props;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        AddRenderer();
    }

    public override void PostLoad()
    {
        base.PostLoad();
        AddRenderer();
    }

    void AddRenderer()
    {
        if (renderer != null || Vehicle.DrawTracker == null) return;
        renderer = new Renderer(this);
        Vehicle.DrawTracker.AddRenderer(renderer);
    }

    /// <summary>The share of full speed the propellers turn at now: our flight's, else turning while the engines run.</summary>
    float Share()
    {
        if (!flightRead)
        {
            flight = Vehicle.GetComp<CompPhobosFlight>();
            flightRead = true;
        }
        if (flight != null) return flight.RotorShare;
        return !Vehicle.Spawned || Vehicle.Drafted ? 1f : 0f;
    }

    void Draw(in TransformData transform)
    {
        bool turning = Share() >= Props.blurFromShare;
        Graphic graphic = turning
            ? spin ??= GraphicDatabase.Get<Graphic_Single>(Props.spinPath, ShaderDatabase.Transparent, Vector2.one, Color.white)
            : still ??= GraphicDatabase.Get<Graphic_Single>(Props.stillPath, ShaderDatabase.Cutout, Vector2.one, Color.white);
        float facing = transform.orientation.AsAngle + transform.rotation;
        Quaternion turn = Quaternion.AngleAxis(facing, Vector3.up);
        var size = new Vector3(Props.drawSize.x, 1f, Props.drawSize.y);
        foreach (Vector2 hub in Props.hubs)
        {
            Vector3 at = transform.position + turn * new Vector3(hub.x, 0f, hub.y);
            at.y = transform.position.y + Altitudes.AltInc * 3f;
            Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(at, turn, size), graphic.MatSingle, 0);
        }
        if (Seen.Wants("vehicles.propellers-drawn", Vehicle, turning ? "turning" : "still"))
            Seen.Note("vehicles.propellers-drawn", Vehicle, variant: turning ? "turning" : "still");
    }

    sealed class Renderer : IParallelRenderer
    {
        readonly CompPhobosPropellers comp;

        public Renderer(CompPhobosPropellers comp) => this.comp = comp;

        public bool IsDirty { get; set; }

        public void DynamicDrawPhaseAt(DrawPhase phase, in TransformData transformData, bool forceDraw = false)
        {
            if (phase == DrawPhase.Draw) comp.Draw(in transformData);
        }
    }
}
