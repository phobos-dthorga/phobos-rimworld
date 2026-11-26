using System;
using System.Collections.Generic;
using Phobos.Framework.Environment;
using Phobos.Framework.Rules;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.Framework.Effects;

/// <summary>
/// One kind of puff a thing gives off (Framework 0.321.0; docs/development/vehicle-effects.md): the game's fleck, how many an hour, where on
/// the thing (cells ahead of its middle and to its right, along its heading), how it looks and how it moves. Looks only: never saved.
/// </summary>
public struct FxEmission
{
    public FleckDef def;
    public float perHour;
    public float scale;
    /// <summary>A tint over the fleck's own colour (alpha too); null keeps the game's.</summary>
    public Color? colour;
    public float forward;
    public float right;
    /// <summary>How far, in cells, each puff may land from its place.</summary>
    public float spread;
    /// <summary>How fast a thrown fleck leaves its place, cells a second.</summary>
    public float speed;
    /// <summary>The direction a thrown fleck leaves in, along the thing's heading (0 ahead, 180 behind); NaN any way.</summary>
    public float towards;
    /// <summary>Carried off by the map's wind (smoke and steam), rather than thrown along <see cref="towards"/>.</summary>
    public bool drift;
    /// <summary>Seconds a fleck stays whole before it fades (a rut lasts); NaN keeps the fleck's own.</summary>
    public float solidSeconds;
    /// <summary>A fleck laid along the heading, stretched to this many cells long and <see cref="width"/> wide (a rut); 0 keeps it round.</summary>
    public float length;
    public float width;

    public static FxEmission Of(FleckDef def, float perHour, float scale) => new()
    {
        def = def, perHour = perHour, scale = scale, towards = float.NaN, solidSeconds = float.NaN,
    };
}

/// <summary>A thing that works its own effects out each pass while it is on screen (a vehicle throwing up dust as it moves).</summary>
public interface IFxSource
{
    /// <summary>Make this pass's flecks (through <see cref="MapComponent_PhobosFx.Make"/>); <paramref name="ticks"/> since the last pass.</summary>
    void Emit(Map map, int ticks);
}

/// <summary>A source that also draws something each frame while it is on screen (Framework 0.326.0: lamps and burning flares, which must
/// stay put on a moving, turning thing, as a fleck left in place each pass would not). Drawing only: decide what to draw in
/// <see cref="IFxSource.Emit"/> and keep it, so a frame costs a few draw calls.</summary>
public interface IFxDrawn
{
    void Draw(Map map);
}

/// <summary>
/// A soft glow drawn straight to the screen for one frame (Framework 0.326.0, slice 3: lamps, beacons, flares): the game's own fire glow
/// picture, tinted, through its additive glow shader, on the layer above the night's darkness, so a lamp shines at night as a lamp does.
/// No picture of ours.
/// </summary>
public static class PhobosGlow
{
    static Material? material;
    static readonly MaterialPropertyBlock block = new();
    static readonly int ColourId = Shader.PropertyToID("_Color");

    /// <summary>The height glows are drawn at: above the darkness of night and over aircraft in the air, under the fog of war.</summary>
    public static readonly float Altitude = AltitudeLayer.VisEffects.AltitudeFor();

    static Material Material => material ??= MaterialPool.MatFrom("Things/Mote/FireGlow", ShaderDatabase.MoteGlow, Color.white);

    /// <summary>Draws one glow of <paramref name="size"/> cells across at this place on the map, in this colour (its alpha the brightness).</summary>
    public static void Draw(Vector3 at, float size, Color colour)
    {
        if (size <= 0f || colour.a <= 0f) return;
        block.SetColor(ColourId, colour);
        Matrix4x4 matrix = Matrix4x4.TRS(new Vector3(at.x, Altitude, at.z), Quaternion.identity, new Vector3(size, 1f, size));
        Graphics.DrawMesh(MeshPool.plane10, matrix, Material, 0, null, 0, block);
    }
}

/// <summary>
/// The visual effects emitter (Framework 0.321.0; docs/development/vehicle-effects.md; the owner's cards of 26 Nov 2026). Every
/// <see cref="PassTicks"/> ticks, on the map being looked at only, it makes the flecks of every thing on screen: the steady puffs asked for
/// with <see cref="Hold"/> (a stage's smoke, spread evenly rather than in a clump at each systems update) and whatever each registered
/// <see cref="IFxSource"/> makes. Nothing is saved and nothing runs on a map nobody is looking at, so a convoy elsewhere costs nothing.
/// </summary>
public sealed class MapComponent_PhobosFx : MapComponent
{
    /// <summary>Ticks between passes: four a second at normal speed.</summary>
    public const int PassTicks = 15;
    /// <summary>The most flecks one pass makes, whatever is asked (a guard, not a figure to tune).</summary>
    const int MostPerPass = 120;

    /// <summary>The heading of a thing, degrees clockwise from north; a mod whose things turn freely sets its own (vehicles).</summary>
    public static Func<Thing, float> HeadingOf = thing => thing.Rotation.AsAngle;

    sealed class Held
    {
        public object key = null!;
        public FxEmission emission;
        public int until;
    }

    readonly Dictionary<Thing, List<Held>> holds = new();
    readonly Dictionary<Thing, IFxSource> sources = new();
    readonly List<Thing> scratch = new();
    int madeThisPass;
    Vector3 downwind;

    public MapComponent_PhobosFx(Map map) : base(map) { }

    public static MapComponent_PhobosFx? For(Map? map) => map?.GetComponent<MapComponent_PhobosFx>();

    /// <summary>Keeps a thing giving off this emission for <paramref name="ticks"/> more ticks, one per <paramref name="key"/>; asked again
    /// before it runs out, it simply carries on.</summary>
    public void Hold(Thing thing, object key, FxEmission emission, int ticks)
    {
        if (emission.def == null || emission.perHour <= 0f) return;
        if (!holds.TryGetValue(thing, out List<Held>? list)) holds[thing] = list = new List<Held>();
        int until = Find.TickManager.TicksGame + ticks;
        foreach (Held held in list)
        {
            if (held.key != key) continue;
            held.emission = emission;
            held.until = until;
            return;
        }
        list.Add(new Held { key = key, emission = emission, until = until });
    }

    /// <summary>Makes <paramref name="count"/> flecks at once (a burst: a tyre blowing, a cough at the key), if the thing is on screen.</summary>
    public void Burst(Thing thing, FxEmission emission, int count)
    {
        if (count <= 0 || emission.def == null || !Visible(thing)) return;
        downwind = PhobosWind.Downwind(map);
        for (int i = 0; i < count; i++) Make(thing, emission);
    }

    /// <summary>Registers a thing that works its own effects out each pass; one per thing. One that also draws each frame
    /// (<see cref="IFxDrawn"/>) is drawn while it is on screen.</summary>
    public void AddSource(Thing thing, IFxSource source)
    {
        sources[thing] = source;
        if (source is IFxDrawn drawnSource) drawn[thing] = drawnSource;
        else drawn.Remove(thing);
    }

    public void RemoveSource(Thing thing)
    {
        sources.Remove(thing);
        drawn.Remove(thing);
        holds.Remove(thing);
    }

    /// <summary>The source registered for a thing, or null (Framework 0.326.0: a salvo's flares are handed to the aircraft's own).</summary>
    public IFxSource? SourceOf(Thing thing) => sources.TryGetValue(thing, out IFxSource? source) ? source : null;

    readonly Dictionary<Thing, IFxDrawn> drawn = new();
    readonly List<Thing> drawScratch = new();

    /// <summary>Each frame, on the map being looked at: draws every drawing source on screen (Framework 0.326.0). Nothing when none is.</summary>
    public override void MapComponentUpdate()
    {
        if (drawn.Count == 0 || map != Find.CurrentMap) return;
        CellRect view = Find.CameraDriver.CurrentViewRect.ExpandedBy(4);
        drawScratch.Clear();
        drawScratch.AddRange(drawn.Keys);
        foreach (Thing thing in drawScratch)
        {
            if (!thing.Spawned || thing.Map != map)
            {
                drawn.Remove(thing);
                continue;
            }
            if (!view.Contains(thing.Position)) continue;
            try
            {
                drawn[thing].Draw(map);
            }
            catch (Exception error)
            {
                // Looks only: a drawing that fails is dropped, never allowed to throw each frame.
                drawn.Remove(thing);
                FrameworkMod.Logger.WarnOnce("a light on " + thing.LabelShort + " failed and is switched off for it: " + error.Message);
            }
        }
    }

    /// <summary>Whether a thing is on this map, the one being looked at, and on screen (with a margin, so smoke drifting in is seen).</summary>
    public bool Visible(Thing thing) =>
        map == Find.CurrentMap && thing.Spawned && thing.Map == map && Find.CameraDriver.CurrentViewRect.ExpandedBy(4).Contains(thing.Position);

    public override void MapComponentTick()
    {
        if (Find.TickManager.TicksGame % PassTicks != 0 || map != Find.CurrentMap || (holds.Count == 0 && sources.Count == 0)) return;
        madeThisPass = 0;
        downwind = PhobosWind.Downwind(map);
        CellRect view = Find.CameraDriver.CurrentViewRect.ExpandedBy(4);
        int now = Find.TickManager.TicksGame;
        if (holds.Count > 0)
        {
            scratch.Clear();
            scratch.AddRange(holds.Keys);
            foreach (Thing thing in scratch)
            {
                List<Held> list = holds[thing];
                list.RemoveAll(h => h.until < now);
                if (list.Count == 0 || !thing.Spawned || thing.Map != map)
                {
                    if (list.Count == 0 || !thing.Spawned) holds.Remove(thing);
                    continue;
                }
                if (!view.Contains(thing.Position)) continue;
                foreach (Held held in list)
                {
                    int count = FxRule.Count(held.emission.perHour, PassTicks, Rand.Value);
                    for (int i = 0; i < count; i++) Make(thing, held.emission);
                }
            }
        }
        if (sources.Count > 0)
        {
            scratch.Clear();
            scratch.AddRange(sources.Keys);
            foreach (Thing thing in scratch)
            {
                if (!thing.Spawned || thing.Map != map)
                {
                    sources.Remove(thing);
                    continue;
                }
                if (!view.Contains(thing.Position)) continue;
                try
                {
                    sources[thing].Emit(map, PassTicks);
                }
                catch (Exception error)
                {
                    // Looks only: a source that fails is dropped, never allowed to throw each pass.
                    sources.Remove(thing);
                    FrameworkMod.Logger.WarnOnce("a visual effect on " + thing.LabelShort + " failed and is switched off for it: " + error.Message);
                }
            }
        }
    }

    /// <summary>Makes one fleck of the emission at its place on the thing.</summary>
    public void Make(Thing thing, FxEmission e) => MakeAt(thing.DrawPos, HeadingOf(thing), e);

    /// <summary>Makes one fleck of the emission at a place given along a heading from a point (a rotor's ring around its middle).</summary>
    public void MakeAt(Vector3 centre, float heading, FxEmission e)
    {
        if (e.def == null || madeThisPass >= MostPerPass) return;
        (double ox, double oz) = FxRule.Offset(heading, e.forward, e.right);
        var at = new Vector3(centre.x + (float)ox, centre.y, centre.z + (float)oz);
        if (e.spread > 0f) at += new Vector3(Rand.Range(-e.spread, e.spread), 0f, Rand.Range(-e.spread, e.spread));
        if (!at.ToIntVec3().InBounds(map)) return;
        FleckCreationData data = FleckMaker.GetDataStatic(at, map, e.def, e.scale * Rand.Range(0.85f, 1.15f));
        if (e.colour.HasValue) data.instanceColor = e.colour.Value;
        if (!float.IsNaN(e.solidSeconds)) data.solidTimeOverride = e.solidSeconds;
        if (e.length > 0f)
        {
            data.exactScale = new Vector3(Math.Max(0.05f, e.width), 1f, e.length);
            data.rotation = heading;
        }
        else data.rotationRate = Rand.Range(-30f, 30f);
        if (e.drift)
        {
            bool calm = downwind.sqrMagnitude < 0.01f;
            data.velocityAngle = calm ? Rand.Range(-25f, 25f) : downwind.AngleFlat() + Rand.Range(-15f, 15f);
            data.velocitySpeed = e.speed + (calm ? 0.2f : 0.5f);
        }
        else if (e.speed > 0f)
        {
            data.velocityAngle = float.IsNaN(e.towards) ? Rand.Range(0f, 360f) : heading + e.towards + Rand.Range(-25f, 25f);
            data.velocitySpeed = e.speed * Rand.Range(0.7f, 1.3f);
        }
        map.flecks.CreateFleck(data);
        madeThisPass++;
    }
}
