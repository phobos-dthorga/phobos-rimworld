using Phobos.Framework.Effects;
using Phobos.Framework.Environment;
using Phobos.Framework.Rules;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// What a moving vehicle throws up, and an aircraft near the ground (Framework 0.321.0, slice 2; docs/development/vehicle-effects.md; the
/// owner's cards of 26 Nov 2026): exhaust that follows the engine's load; dust, sand, mud, snow and water spray from the wheels; ruts on
/// soft ground and snow that fade; rotor downwash; a rough landing's burst. One per spawned vehicle with our systems, any faction's, run by
/// the Framework core's emitter only while it is on screen on the map being looked at. Nothing is saved.
/// </summary>
public sealed partial class VehicleMotionFx : IFxSource
{
    readonly CompPhobosVehicleSystems comp;
    Vector3 lastPos;
    bool hasLast;
    int lastEmit = -1;
    float rutCarry;
    bool rightSide;

    static FleckDef? rut;
    static FleckDef? dirtBits;
    static FleckDef Rut => rut ??= DefDatabase<FleckDef>.GetNamedSilentFail("PhobosFleck_Rut") ?? FleckDefOf.DustPuff;
    static FleckDef DirtBits => dirtBits ??= DefDatabase<FleckDef>.GetNamedSilentFail("DirtBits") ?? FleckDefOf.DustPuff;
    static PhobosVehicleFxRulesDef Rules => PhobosVehicleFxRulesDef.Rules;

    VehicleMotionFx(CompPhobosVehicleSystems comp) => this.comp = comp;

    /// <summary>Registers the vehicle with its map's emitter as it spawns (registering only: nothing is made here).</summary>
    public static void Register(CompPhobosVehicleSystems comp)
    {
        if (comp.Props.loadout == null || comp.Vehicle?.Map is not { } map) return;
        MapComponent_PhobosFx.For(map)?.AddSource(comp.Vehicle, new VehicleMotionFx(comp));
    }

    public static void Unregister(VehiclePawn vehicle, Map? map) => MapComponent_PhobosFx.For(map)?.RemoveSource(vehicle);

    public void Emit(Map map, int ticks)
    {
        FxLevel level = VehicleFx.Level;
        if (level == FxLevel.Off || MapComponent_PhobosFx.For(map) is not { } fx)
        {
            anyLampLit = false;
            return;
        }
        VehiclePawn v = comp.Vehicle;
        Vector3 pos = v.DrawPos;
        int now = Find.TickManager.TicksGame;
        // Back on screen after a while (the emitter skips it off screen): where it was then says nothing of its speed now.
        bool fresh = lastEmit < 0 || now - lastEmit > 2 * ticks;
        if (fresh) hasLast = false;
        lastEmit = now;
        float moved = hasLast ? new Vector2(pos.x - lastPos.x, pos.z - lastPos.z).magnitude : 0f;
        lastPos = pos;
        hasLast = true;
        bool jumped = moved > 6f;    // a jump (a teleport, a load) is no drive
        float speed = jumped ? 0f : moved / (ticks / 60f);    // cells a second
        float density = (float)FxRule.Density(level, false, Rules.lightShare);
        string? kind = comp.Props.loadout?.kind;
        bool aircraft = kind == "Aircraft";
        bool airborne = OnMapFlight.Airborne(v);
        // Braking is judged only between two looks on the ground in a row; a jump or a return to the screen starts afresh.
        LookAtLamps(v, speed, !aircraft && kind != "Boat" && !airborne && !jumped && !fresh);
        HeatShimmer(fx, v, density, ticks, aircraft);
        MoveFlares(fx, density);
        if (aircraft)
        {
            Downwash(fx, map, v, density, ticks);
            return;
        }
        if (kind == "Boat" || airborne) return;
        if (!comp.Props.trailer) Exhaust(fx, v, density, ticks);
        if (speed < Rules.groundMinSpeed || jumped) return;
        Wheels(fx, map, v, speed, moved, density, level, ticks);
    }

    // ---- Exhaust ----

    void Exhaust(MapComponent_PhobosFx fx, VehiclePawn v, float density, int ticks)
    {
        if (!comp.EngineRunning || comp.Powertrain?.Spec.Engine != EngineKind.Piston) return;
        PhobosVehicleFxRulesDef r = Rules;
        float load = comp.EngineLoad;
        double rate = FxRule.ExhaustRate(load, r.exhaustIdleLoad, r.exhaustCruisePerHour, r.exhaustHardPerHour) * density;
        int count = FxRule.Count(rate, ticks, Rand.Value);
        if (count <= 0) return;
        bool diesel = comp.EngineFuel == EngineFuel.Diesel;
        bool cold = comp.Ambient < r.exhaustCondensesBelow
            && (!comp.TryGetQuantity("coolantTemperature", out float coolant) || coolant < r.exhaustWarmCoolant);
        FxEmission e = VehicleFx.Emission(v, FleckDefOf.Smoke, FxTint.Plain, FxSpot.Exhaust, 0f, 0.45f + 0.45f * load, drift: true);
        e.colour = cold ? r.steam : diesel ? r.exhaustDiesel : r.exhaustPetrol;
        for (int i = 0; i < count; i++) fx.Make(v, e);
        string variant = cold ? "cold" : diesel ? "diesel" : "petrol";
        if (Seen.Wants("vehicles.fx-exhaust", v, variant)) Seen.Note("vehicles.fx-exhaust", v, load.ToString("0.00"), variant);
    }

    // ---- The wheels ----

    enum Ground { None, Dust, Sand, Mud, Snow, Water }

    static Ground GroundAt(Map map, IntVec3 cell)
    {
        if (!cell.InBounds(map)) return Ground.None;
        TerrainDef? terrain = cell.GetTerrain(map);
        if (terrain == null) return Ground.None;
        if (terrain.IsWater) return Ground.Water;
        if (map.snowGrid.GetDepth(cell) >= Rules.snowFromDepth) return Ground.Snow;
        if (!terrain.takeFootprints) return Ground.None;    // paving, floors, bare rock: nothing thrown up, no ruts
        if (MapComponent_PhobosGroundWetness.At(map, cell) >= Rules.mudFromWetness) return Ground.Mud;
        return terrain.defName.IndexOf("Sand", System.StringComparison.OrdinalIgnoreCase) >= 0 ? Ground.Sand : Ground.Dust;
    }

    void Wheels(MapComponent_PhobosFx fx, Map map, VehiclePawn v, float speed, float moved, float density, FxLevel level, int ticks)
    {
        PhobosVehicleFxRulesDef r = Rules;
        (float halfLength, float halfWidth) = VehicleFx.Half(v);
        float heading = MapComponent_PhobosFx.HeadingOf(v);
        float back = -halfLength + 0.3f;
        float side = Mathf.Max(0.2f, halfWidth - 0.25f);
        (double bx, double bz) = FxRule.Offset(heading, back, 0);
        IntVec3 cell = new Vector3(v.DrawPos.x + (float)bx, 0f, v.DrawPos.z + (float)bz).ToIntVec3();
        Ground ground = GroundAt(map, cell);
        if (ground == Ground.None) return;

        // What the wheels throw up, by speed and, for dust, by the place's dust.
        double dust = ground is Ground.Dust or Ground.Sand
            ? Mathf.Clamp((float)DustRule.Factor(map.TileInfo?.rainfall ?? 1000f, map.weatherManager.SandRate, cell.Roofed(map)) / 3f, 0.3f, 4f)
            : 1;
        double rate = r.groundPerHour * Mathf.Clamp01(speed / r.groundFullSpeed) * density * dust;
        int count = Mathf.Min(8, FxRule.Count(rate, ticks, Rand.Value));
        for (int i = 0; i < count; i++)
        {
            rightSide = !rightSide;
            float right = rightSide ? side : -side;
            if (ground == Ground.Water)
            {
                (double wx, double wz) = FxRule.Offset(heading, back, right);
                FleckMaker.WaterSplash(new Vector3(v.DrawPos.x + (float)wx, v.DrawPos.y, v.DrawPos.z + (float)wz), map, Rand.Range(1f, 1.6f), Rand.Range(1f, 2f));
                continue;
            }
            FxEmission e = FxEmission.Of(ground == Ground.Mud ? DirtBits : FleckDefOf.DustPuff, 0f, ground == Ground.Mud ? 0.8f : 1f);
            e.colour = ground switch { Ground.Mud => r.mud, Ground.Snow => r.snow, Ground.Sand => r.sand, _ => r.dust };
            e.forward = back;
            e.right = right;
            e.spread = 0.15f;
            e.towards = 180f;
            e.speed = 0.6f + 0.2f * speed;
            fx.Make(v, e);
        }
        if (count > 0 && Seen.Wants("vehicles.fx-ground", v, ground.ToString())) Seen.Note("vehicles.fx-ground", v, speed.ToString("0.0") + " cells/s", ground.ToString());

        // Ruts on soft ground and snow, a pair every so far, laid back along the way it came.
        if (ground == Ground.Water) return;
        rutCarry += moved;
        int laid = 0;
        while (rutCarry >= r.rutEveryCells && laid < 6)
        {
            rutCarry -= r.rutEveryCells;
            float along = back - rutCarry;
            foreach (float right in new[] { side, -side })
            {
                FxEmission e = FxEmission.Of(Rut, 0f, 1f);
                e.colour = ground == Ground.Snow ? r.rutSnow : r.rutSoil;
                e.forward = along;
                e.right = right;
                e.length = r.rutEveryCells * 1.4f;
                e.width = r.rutWidth;
                e.solidSeconds = level == FxLevel.Light ? r.rutSecondsLight : r.rutSeconds;
                fx.Make(v, e);
            }
            laid++;
        }
        if (laid >= 6) rutCarry = 0f;
        if (laid > 0 && Seen.Wants("vehicles.fx-ruts", v, ground == Ground.Snow ? "snow" : "soil")) Seen.Note("vehicles.fx-ruts", v, null, ground == Ground.Snow ? "snow" : "soil");
    }

    // ---- Rotor downwash ----

    void Downwash(MapComponent_PhobosFx fx, Map map, VehiclePawn v, float density, int ticks)
    {
        if (v.GetComp<CompPhobosFlight>() is not { HoversOnRotors: true } flight) return;
        PhobosVehicleFxRulesDef r = Rules;
        double strength = FxRule.Downwash(flight.RotorShare, flight.HeightMetresNow, r.downwashReachMetres) * density;
        int count = Mathf.Min(12, FxRule.Count(r.downwashPerHour * strength, ticks, Rand.Value));
        if (count <= 0) return;
        Vector3 centre = flight.Airborne ? flight.TrueCenter : v.DrawPos;
        (float halfLength, _) = VehicleFx.Half(v);
        string? seen = null;
        for (int i = 0; i < count; i++)
        {
            float angle = Rand.Range(0f, 360f);
            float radius = halfLength + Rand.Range(0.5f, 2.5f);
            (double ox, double oz) = FxRule.Offset(angle, radius, 0);
            var at = new Vector3(centre.x + (float)ox, v.DrawPos.y, centre.z + (float)oz);
            Ground ground = GroundAt(map, at.ToIntVec3());
            if (ground == Ground.None) continue;    // paving: nothing loose to blow about
            if (ground == Ground.Water)
            {
                FleckMaker.WaterSplash(at, map, Rand.Range(1.2f, 2f), Rand.Range(1.5f, 2.5f));
                seen ??= "water";
                continue;
            }
            FxEmission e = FxEmission.Of(FleckDefOf.DustPuffThick, 0f, 1.2f);
            e.colour = ground switch { Ground.Mud => r.mud, Ground.Snow => r.snow, Ground.Sand => r.sand, _ => r.dust };
            e.forward = radius;
            e.towards = 0f;    // outward, along the spoke it lies on
            e.speed = 1.6f;
            fx.MakeAt(centre, angle, e);
            seen ??= ground.ToString();
        }
        if (seen != null && Seen.Wants("aircraft.fx-downwash", v, seen)) Seen.Note("aircraft.fx-downwash", v, flight.HeightMetresNow.ToString("0") + " m", seen);
    }

    // ---- A rough landing ----

    /// <summary>A rough landing's burst of tyre smoke and dust at the wheels. An aircraft another flight mod flies draws its own touchdowns
    /// (Close Air Support's jets), so ours stands aside for it.</summary>
    public static void Touchdown(VehiclePawn v)
    {
        if (OnMapFlight.FliesItself(v) && !CompPhobosFlight.FliesOnOurs(v)) return;
        int half = Mathf.Max(1, Rules.touchdownPuffs / 2);
        VehicleFx.BurstSign(v, VehicleFx.Emission(v, FleckDefOf.Smoke, FxTint.TyreSmoke, FxSpot.Wheels, 0f, 0.9f, drift: true), half);
        FxEmission dust = VehicleFx.Emission(v, FleckDefOf.DustPuffThick, FxTint.Plain, FxSpot.Wheels, 0f, 1.1f, drift: false);
        dust.colour = Rules.dust;
        VehicleFx.BurstSign(v, dust, half);
        if (Seen.Wants("aircraft.fx-touchdown", v)) Seen.Note("aircraft.fx-touchdown", v);
    }
}
