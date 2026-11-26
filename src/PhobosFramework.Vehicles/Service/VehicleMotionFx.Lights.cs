using System.Collections.Generic;
using Phobos.Framework.Effects;
using Phobos.Framework.Rules;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Slice 3 of vehicles' visual effects (Framework 0.326.0; docs/development/vehicle-effects.md; the owner's go-ahead of 26 Nov 2026): lamps
/// seen on the vehicle while their electrical load runs (headlamps and tail lamps, brake lamps as it slows, an aircraft's or boat's navigation
/// lights, a recovery beacon at work), a salvo's flares and chaff, and heat shimmer behind turbines and jets. Lamps and burning flares are
/// drawn each frame (<see cref="IFxDrawn"/>), so they stay on a moving, turning vehicle; what to draw is decided at each pass of the emitter,
/// so a frame costs only the draw calls. The glows are the game's own fire glow, tinted, and the shimmer the game's own heat-distortion
/// shader: no picture or shader of ours. Nothing is saved.
/// </summary>
public sealed partial class VehicleMotionFx : IFxDrawn
{
    /// <summary>The loads with lamps, and which of them burn, as of the last pass (the comp's loads change with its fittings: the recovery
    /// winch brings its beacon).</summary>
    readonly List<ElectricalLoadSpec> lampLoads = new();
    readonly List<bool> lampLit = new();
    bool anyLampLit;
    int brakeUntil = -1;
    float lastSpeed;

    struct Flare
    {
        public Vector3 pos;
        public Vector3 vel;
        public int born;
    }

    readonly List<Flare> flares = new();
    int flaresMovedAt;

    static FleckDef? shimmer;
    static FleckDef Shimmer => shimmer ??= DefDatabase<FleckDef>.GetNamedSilentFail("PhobosFleck_HeatShimmer") ?? FleckDefOf.HeatGlow;

    /// <summary>Half the vehicle's drawn picture's length and width, cells (an aircraft's wingtips reach past its footprint).</summary>
    static (float halfLength, float halfWidth) DrawnHalf(VehiclePawn vehicle)
    {
        Vector2 size = vehicle.VehicleDef.graphicData?.drawSize ?? Vector2.zero;
        (float l, float w) = VehicleFx.Half(vehicle);
        return (Mathf.Max(l, size.y / 2f), Mathf.Max(w, size.x / 2f));
    }

    // ---- Lamps ----

    /// <summary>Which loads' lamps burn now, and whether it is braking: looked at each pass.</summary>
    void LookAtLamps(VehiclePawn v, float speed, bool onGround)
    {
        anyLampLit = false;
        lampLoads.Clear();
        lampLit.Clear();
        bool hasBrakeLamps = false;
        foreach (ElectricalLoadSpec load in comp.Loads)
        {
            if (load.lamps is not { Count: > 0 } lamps) continue;
            foreach (LampSpec lamp in lamps) hasBrakeLamps |= lamp.brake;
            // The headlights by their own rule, which reaches other factions' vehicles too; any other load by whether it runs.
            bool lit = load.beam != null ? comp.Beams.On : comp.LoadActive(load.name);
            lampLoads.Add(load);
            lampLit.Add(lit);
            if (!lit) continue;
            anyLampLit = true;
            if (Seen.Wants("vehicles.fx-lamps", v, load.name)) Seen.Note("vehicles.fx-lamps", v, null, load.name);
        }
        if (hasBrakeLamps && onGround)
        {
            PhobosVehicleFxRulesDef r = Rules;
            if (FxRule.Slowing(lastSpeed, speed, r.groundMinSpeed, r.brakeDropShare))
            {
                brakeUntil = Find.TickManager.TicksGame + (int)(r.brakeHoldSeconds * GenTicks.TicksPerRealSecond);
                if (Seen.Wants("vehicles.fx-brake-lamps", v)) Seen.Note("vehicles.fx-brake-lamps", v, lastSpeed.ToString("0.0") + " cells/s");
            }
        }
        lastSpeed = onGround ? speed : 0f;
    }

    public void Draw(Map map)
    {
        if (VehicleFx.Level == FxLevel.Off) return;
        VehiclePawn v = comp.Vehicle;
        int now = Find.TickManager.TicksGame;
        bool braking = now < brakeUntil;
        if (anyLampLit || braking) DrawLamps(v, now, braking);
        if (flares.Count > 0) DrawFlares(now);
    }

    void DrawLamps(VehiclePawn v, int now, bool braking)
    {
        float heading = MapComponent_PhobosFx.HeadingOf(v);
        Vector3 centre = v.DrawPos;
        (float footLength, float footWidth) = VehicleFx.Half(v);
        (float drawnLength, float drawnWidth) = DrawnHalf(v);
        float brakeScale = Rules.brakeGlowScale;
        for (int n = 0; n < lampLoads.Count; n++)
        {
            bool lit = lampLit[n];
            foreach (LampSpec lamp in lampLoads[n].lamps!)
            {
                bool braked = lamp.brake && braking;
                if (!braked && (!lit
                    || !FxRule.LampLit(lamp.pattern == LampPattern.Flash, now, lamp.periodSeconds * GenTicks.TicksPerRealSecond, lamp.phase, lamp.litShare)))
                {
                    continue;
                }
                float length = lamp.drawn ? drawnLength : footLength, width = lamp.drawn ? drawnWidth : footWidth;
                (double ox, double oz) = FxRule.Offset(heading, lamp.forward * length, lamp.right * width);
                Color colour = lamp.colour;
                float size = lamp.size;
                if (braked)
                {
                    colour.a = 1f;
                    size *= brakeScale;
                }
                PhobosGlow.Draw(new Vector3(centre.x + (float)ox, 0f, centre.z + (float)oz), size, colour);
            }
        }
    }

    // ---- Flares and chaff ----

    /// <summary>A salvo fired from an aircraft on a map (AirCombat.SpendSalvo): its flares thrown out behind and to both sides, and a glitter
    /// of chaff. Nothing off a map, with the setting off, or for an aircraft with no source of ours.</summary>
    public static void Salvo(VehiclePawn vehicle)
    {
        if (VehicleFx.Level == FxLevel.Off || !vehicle.Spawned) return;
        if (MapComponent_PhobosFx.For(vehicle.Map) is not { } fx || fx.SourceOf(vehicle) is not VehicleMotionFx source) return;
        source.Launch(fx);
    }

    void Launch(MapComponent_PhobosFx fx)
    {
        VehiclePawn v = comp.Vehicle;
        PhobosVehicleFxRulesDef r = Rules;
        float heading = MapComponent_PhobosFx.HeadingOf(v);
        (float drawnLength, _) = DrawnHalf(v);
        (double sx, double sz) = FxRule.Offset(heading, -drawnLength * 0.3, 0);
        Vector3 start = v.DrawPos + new Vector3((float)sx, 0f, (float)sz);
        int now = Find.TickManager.TicksGame;
        if (flares.Count == 0) flaresMovedAt = now;
        for (int i = 0; i < r.flaresPerSalvo; i++)
        {
            float side = i % 2 == 0 ? 1f : -1f;
            float angle = heading + side * Rand.Range(110f, 160f);
            (double vx, double vz) = FxRule.Offset(angle, r.flareSpeed * Rand.Range(0.8f, 1.2f), 0);
            // A pair at a time, a few ticks apart, as a dispenser fires its cartridges in sequence.
            flares.Add(new Flare { pos = start, vel = new Vector3((float)vx, 0f, (float)vz), born = now + i / 2 * 8 });
        }
        FxEmission chaff = FxEmission.Of(FleckDefOf.MicroSparks, 0f, 0.6f);
        chaff.colour = r.chaff;
        chaff.forward = -drawnLength * 0.4f;
        chaff.spread = 0.8f;
        chaff.towards = 180f;
        chaff.speed = 1.2f;
        fx.Burst(v, chaff, r.chaffSparks);
        if (Seen.Wants("aircraft.fx-flares", v)) Seen.Note("aircraft.fx-flares", v, r.flaresPerSalvo + " flares");
    }

    /// <summary>Moves the burning flares on and lays their smoke, each pass; burnt-out ones go.</summary>
    void MoveFlares(MapComponent_PhobosFx fx, float density)
    {
        if (flares.Count == 0) return;
        PhobosVehicleFxRulesDef r = Rules;
        int now = Find.TickManager.TicksGame;
        float lifeTicks = r.flareSeconds * GenTicks.TicksPerRealSecond;
        FxEmission smoke = FxEmission.Of(FleckDefOf.Smoke, 0f, 0.7f);
        smoke.colour = r.flareSmoke;
        smoke.drift = true;
        for (int i = flares.Count - 1; i >= 0; i--)
        {
            Flare flare = flares[i];
            if (now - flare.born > lifeTicks)
            {
                flares.RemoveAt(i);
                continue;
            }
            if (now < flare.born) continue;
            float seconds = (now - Mathf.Max(flaresMovedAt, flare.born)) / (float)GenTicks.TicksPerRealSecond;
            flare.pos += flare.vel * seconds;
            flare.vel *= (float)FxRule.Drag(seconds, r.flareDragSeconds);
            flares[i] = flare;
            if (Rand.Value < density) fx.MakeAt(flare.pos, 0f, smoke);
        }
        flaresMovedAt = now;
    }

    void DrawFlares(int now)
    {
        PhobosVehicleFxRulesDef r = Rules;
        float lifeTicks = r.flareSeconds * GenTicks.TicksPerRealSecond;
        float since = (now - flaresMovedAt) / (float)GenTicks.TicksPerRealSecond;
        for (int i = 0; i < flares.Count; i++)
        {
            Flare flare = flares[i];
            if (now < flare.born) continue;
            float age = now - flare.born;
            Color colour = r.flareGlow;
            // Burning out over its last half second; a flicker from noise, never the game's random numbers, which drawing must not move.
            colour.a *= Mathf.Clamp01((lifeTicks - age) / (0.5f * GenTicks.TicksPerRealSecond));
            float flicker = 0.85f + 0.3f * Mathf.PerlinNoise(Time.realtimeSinceStartup * 12f, i * 3.7f);
            PhobosGlow.Draw(flare.pos + flare.vel * since, r.flareSize * flicker, colour);
        }
    }

    // ---- Heat shimmer ----

    /// <summary>Heat shimmer behind each running turbine or jet, by its output and the engine's load; behind a piston engine, none (its
    /// exhaust shows instead).</summary>
    void HeatShimmer(MapComponent_PhobosFx fx, VehiclePawn v, float density, int ticks, bool aircraft)
    {
        if (!comp.EngineRunning || comp.Powertrain is not { } train) return;
        PhobosVehicleFxRulesDef r = Rules;
        EngineKind kind = train.Spec.Engine;
        float forwardShare = kind switch
        {
            EngineKind.Turbofan => r.shimmerTurbofanForward,
            EngineKind.Turboshaft => r.shimmerTurboshaftForward,
            EngineKind.Thruster => r.shimmerThrusterForward,
            _ => float.NaN,
        };
        if (float.IsNaN(forwardShare)) return;
        (float length, float width) = aircraft ? DrawnHalf(v) : VehicleFx.Half(v);
        float forward = forwardShare * length, plainRight = 0f;
        if (!aircraft) (forward, plainRight, _) = VehicleFx.Place(v, FxSpot.Exhaust);    // a turbine truck's stack
        IReadOnlyList<double> outputs = train.EngineOutputs();
        IReadOnlyList<double>? arms = aircraft && outputs.Count > 1 ? train.EngineArms() : null;
        double widest = 0;
        if (arms != null) foreach (double arm in arms) widest = System.Math.Max(widest, System.Math.Abs(arm));
        double perEngine = r.shimmerPerHour * (r.shimmerIdleShare + (1 - r.shimmerIdleShare) * Mathf.Clamp01(comp.EngineLoad)) * density;
        int engines = Mathf.Max(1, outputs.Count);
        bool made = false;
        for (int i = 0; i < engines; i++)
        {
            double output = i < outputs.Count ? outputs[i] : 1;
            int count = Mathf.Min(4, FxRule.Count(perEngine * output, ticks, Rand.Value));
            if (count <= 0) continue;
            FxEmission e = FxEmission.Of(Shimmer, 0f, r.shimmerScale);
            e.forward = forward;
            e.right = arms != null && widest > 0 && i < arms.Count ? (float)(arms[i] / widest) * r.shimmerSpreadShare * width : plainRight;
            e.spread = 0.1f;
            e.towards = 180f;
            e.speed = 0.7f;
            for (int k = 0; k < count; k++) fx.Make(v, e);
            made = true;
        }
        if (made && Seen.Wants("vehicles.fx-shimmer", v, kind.ToString())) Seen.Note("vehicles.fx-shimmer", v, null, kind.ToString());
    }
}
