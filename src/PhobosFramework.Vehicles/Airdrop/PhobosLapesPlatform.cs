using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A vehicle on its platform sliding to a stop after a low-altitude extraction (LAPES; D'thorga Framework 0.323.0; docs/development/airdrop.md,
/// "LAPES"). Pulled out of the ramp a few feet up by its extraction chute, it touches down at once and slows evenly to rest along the strip
/// (LapesRule), throwing dust, the chute trailing ahead of it; at rest the vehicle is put down as a parachute platform's is
/// (<see cref="ParachuteLanding.LandExtracted"/>), with its spent platform and chute left beside it. It never loses what it carries: destroyed
/// early, it puts the vehicle down where it is.
/// </summary>
public class PhobosLapesPlatform : Thing, IThingHolder
{
    /// <summary>Ticks between two puffs of dust while it slides (authored).</summary>
    const int DustEveryTicks = 4;
    /// <summary>How far ahead of the platform its extraction chute trails, cells (authored, for the drawing only).</summary>
    const float ChuteAhead = 3f;

    ThingOwner<Thing> inner;
    // Saved.
    float fromX, fromZ, toX, toZ;
    int ticksDone, ticksTotal = 1;
    /// <summary>Lands hard at rest (rolled as it was let go).</summary>
    public bool hard;
    /// <summary>The way it slides, and faces.</summary>
    public Rot4 slideRot = Rot4.North;
    bool landed;

    public PhobosLapesPlatform() => inner = new ThingOwner<Thing>(this, oneStackOnly: true);

    public ThingOwner GetDirectlyHeldThings() => inner;

    public void GetChildHolders(List<IThingHolder> outChildren) => ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());

    /// <summary>The vehicle it carries, if any is left.</summary>
    VehiclePawn? Load => inner.Count > 0 ? inner[0] as VehiclePawn : null;

    float Share => (float)LapesRule.SlidShare(ticksDone / (float)Mathf.Max(1, ticksTotal));

    public override Vector3 DrawPos
    {
        get
        {
            float s = Share;
            return new Vector3(Mathf.Lerp(fromX, toX, s), def.Altitude, Mathf.Lerp(fromZ, toZ, s));
        }
    }

    protected override void Tick()
    {
        base.Tick();
        if (!Spawned || landed) return;
        ticksDone++;
        Vector3 at = DrawPos;
        IntVec3 cell = at.ToIntVec3().ClampInsideMap(Map);
        if (cell != Position) Position = cell;
        if (ticksDone % DustEveryTicks == 0 && ticksDone < ticksTotal) FleckMaker.ThrowDustPuffThick(at, Map, Rand.Range(1.2f, 2f), new Color(0.6f, 0.55f, 0.45f));
        if (ticksDone >= ticksTotal) ComeToRest();
    }

    /// <summary>At rest: the vehicle put down facing the way it slid, the spent platform and extraction chute beside it.</summary>
    void ComeToRest()
    {
        if (landed) return;
        landed = true;
        Map map = Map;
        IntVec3 at = Position;
        if (Load is { } vehicle)
        {
            inner.Remove(vehicle);
            ParachuteLanding.LandExtracted(vehicle, map, at, slideRot, hard);
        }
        foreach (Thing rest in inner.ToList())
        {
            inner.Remove(rest);
            GenPlace.TryPlaceThing(rest, at, map, ThingPlaceMode.Near);
        }
        GenPlace.TryPlaceThing(ThingMaker.MakeThing(AirdropDefOf.Phobos_SpentDropPlatform), at, map, ThingPlaceMode.Near);
        GenPlace.TryPlaceThing(ThingMaker.MakeThing(AirdropDefOf.Phobos_SpentCargoParachute), at, map, ThingPlaceMode.Near);
        Seen.Note("airdrop.lapes-landed", this, variant: hard ? "hard" : "soft");
        if (Spawned) Destroy();
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        // Destroyed before it came to rest (a developer, a mod, the map going): it puts what it carries down where it is.
        if (!landed && Spawned && inner.Count > 0) ComeToRest();
        base.Destroy(mode);
    }

    // ---- Drawing ----

    static Graphic? platform, chute;

    static Graphic Platform => platform ??= GraphicDatabase.Get<Graphic_Single>("Phobos/VehicleSystems/Airdrop/DropPlatform", ShaderDatabase.Cutout, Vector2.one, Color.white);
    static Graphic Chute => chute ??= GraphicDatabase.Get<Graphic_Single>("Phobos/VehicleSystems/Airdrop/CanopyOpen", ShaderDatabase.Cutout, Vector2.one, Color.white);

    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        Vector3 at = DrawPos;
        float alt = def.Altitude;
        if (Load is { } vehicle)
        {
            IntVec2 size = vehicle.VehicleDef.Size;
            Vector2 plate = slideRot.IsHorizontal ? new Vector2(size.z, size.x) : new Vector2(size.x, size.z);
            DrawScaled(Platform, new Vector3(at.x, alt - 0.02f, at.z), plate * 1.05f);
            vehicle.DynamicDrawPhaseAt(DrawPhase.Draw, new Vector3(at.x, alt, at.z), flip: flip);
            // The extraction chute trails ahead, pulling against the slide, smaller as the platform slows.
            Vector3 ahead = slideRot.FacingCell.ToVector3();
            float reach = Mathf.Max(plate.x, plate.y) / 2f + ChuteAhead;
            float chuteSize = Mathf.Lerp(2.6f, 1.6f, Share);
            var c = new Vector3(at.x + ahead.x * reach, alt + 0.03f, at.z + ahead.z * reach);
            GenDraw.DrawLineBetween(new Vector3(at.x, alt, at.z), c, SimpleColor.White, 0.04f);
            DrawScaled(Chute, c, new Vector2(chuteSize, chuteSize));
        }
    }

    static void DrawScaled(Graphic graphic, Vector3 at, Vector2 size)
    {
        Matrix4x4 matrix = Matrix4x4.TRS(at, Quaternion.identity, new Vector3(size.x, 1f, size.y));
        Graphics.DrawMesh(MeshPool.plane10, matrix, graphic.MatSingle, 0);
    }

    // ---- Saving ----

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref inner, "PhobosLapes_Inner", this);
        Scribe_Values.Look(ref fromX, "PhobosLapes_FromX");
        Scribe_Values.Look(ref fromZ, "PhobosLapes_FromZ");
        Scribe_Values.Look(ref toX, "PhobosLapes_ToX");
        Scribe_Values.Look(ref toZ, "PhobosLapes_ToZ");
        Scribe_Values.Look(ref ticksDone, "PhobosLapes_TicksDone");
        Scribe_Values.Look(ref ticksTotal, "PhobosLapes_TicksTotal", 1);
        Scribe_Values.Look(ref hard, "PhobosLapes_Hard");
        Scribe_Values.Look(ref slideRot, "PhobosLapes_Rot", Rot4.North);
        Scribe_Values.Look(ref landed, "PhobosLapes_Landed");
    }

    // ---- Letting go ----

    /// <summary>
    /// Pulls <paramref name="vehicle"/> (in no container, not spawned) out at <paramref name="exit"/> on its platform, sliding the rule's distance
    /// along <paramref name="rot"/> to rest, kept inside the map.
    /// </summary>
    public static PhobosLapesPlatform Release(Map map, VehiclePawn vehicle, Vector3 exit, Rot4 rot, bool hard)
    {
        var slider = (PhobosLapesPlatform)ThingMaker.MakeThing(AirdropDefOf.Phobos_LapesPlatform);
        int cells = LapesRule.SlideCells(FlightRules.MetresPerCell);
        Vector3 to = exit + rot.FacingCell.ToVector3() * cells;
        slider.fromX = exit.x;
        slider.fromZ = exit.z;
        slider.toX = Mathf.Clamp(to.x, 1f, map.Size.x - 1.01f);
        slider.toZ = Mathf.Clamp(to.z, 1f, map.Size.z - 1.01f);
        slider.ticksTotal = Mathf.Max(1, Mathf.RoundToInt((float)(LapesRule.SlideSeconds * 60.0)));
        slider.hard = hard;
        slider.slideRot = rot;
        vehicle.Rotation = rot;
        slider.inner.TryAdd(vehicle, canMergeWithExistingStacks: false);
        GenSpawn.Spawn(slider, exit.ToIntVec3().ClampInsideMap(map), map, WipeMode.Vanish);
        AirdropSounds.Play(AirdropSounds.CanopyOpens, map, slider.Position);
        return slider;
    }
}
