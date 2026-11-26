using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Phobos.Framework.Rules;
using RimWorld;
using RimWorld.Planet;
using System.Text;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The world map's ground under the player's convoys (Framework 0.325.0; docs/development/vehicle-road-work.md): heavy vehicles bog on soft,
/// wet ground (EM43; the owner's card of 26 Nov 2026: a winch, a tow, a load of stone or waiting all free one), and a Talus spreads grit
/// on winter tiles so later convoys go easier (EM39). Looked at once each time a moving convoy reaches a new tile. Every rule is
/// <see cref="GroundRule"/>'s; buttons and the convoy's line only ask.
/// </summary>
public static class RoadGround
{
    /// <summary>The convoy's waits by cause: turned back from an ambush, or stuck in the ground.</summary>
    public const int HoldAmbush = 0;
    public const int HoldBogged = 1;

    /// <summary>How soft a tile is now: its swampiness or yearly rain, firm when the season freezes it.</summary>
    public static float Softness(PlanetTile tile)
    {
        Tile ground = Find.WorldGrid[tile];
        float temperature = GenTemperature.GetTemperatureFromSeasonAtTile(GenTicks.TicksAbs, tile);
        return GroundRule.Softness(ground.swampiness, ground.rainfall, (int)ground.hilliness, temperature);
    }

    /// <summary>A moving convoy of the player's reached a new tile: grit it if a Talus is spreading, then sink its heavy vehicles.</summary>
    public static void Entered(WorldComponent_PhobosRoadWork work, VehicleCaravan convoy, PlanetTile tile)
    {
        Grit(work, convoy, tile);
        if (VehicleSystemsMod.Settings.bogging) Sink(work, convoy, tile);
    }

    // ---- Bogging (EM43) ----

    static void Sink(WorldComponent_PhobosRoadWork work, VehicleCaravan convoy, PlanetTile tile)
    {
        float softness = Softness(tile);
        if (softness <= 0f) return;
        foreach (VehiclePawn vehicle in TankLarder.Vehicles(convoy).ToList())
        {
            if (vehicle.CompVehicleLauncher != null) continue;
            bool tracked = vehicle.GetComp<CompPhobosVehicleSystems>()?.Props.trackKeys.Count > 0;
            float sunk = work.AddSink(vehicle, GroundRule.Sink((float)CompPhobosSling.LoadKilograms(vehicle), softness, tracked));
            if (sunk < 1f) continue;
            work.ClearSink(vehicle);
            Bog(work, convoy, vehicle);
            return;
        }
    }

    /// <summary>The vehicle sticks: the convoy stops, and is held for as long as its best way out takes; stone can free it sooner.</summary>
    static void Bog(WorldComponent_PhobosRoadWork work, VehicleCaravan convoy, VehiclePawn stuck)
    {
        bool winch = convoy.VehiclesListForReading.Any(v => v.GetComp<CompPhobosHitch>() is { CanWinch: true });
        bool tow = convoy.VehiclesListForReading.Any(v => v != stuck && v.GetComp<CompPhobosHitch>() != null);
        BogRecovery way = GroundRule.Best(winch, tow);
        convoy.vehiclePather.StopDead();
        work.Hold(convoy, Find.TickManager.TicksGame + (int)(GroundRule.Hours(way) * GenDate.TicksPerHour), HoldBogged, stuck);
        string wayText = way switch
        {
            BogRecovery.Winch => "PhobosGround_WayWinch".Translate(GroundRule.WinchHours.ToString("0.#").Named("HOURS")).Resolve(),
            BogRecovery.Tow => "PhobosGround_WayTow".Translate(GroundRule.TowHours.ToString("0").Named("HOURS")).Resolve(),
            _ => "PhobosGround_WayWait".Translate(GroundRule.WaitHours.ToString("0").Named("HOURS")).Resolve(),
        };
        Messages.Message("PhobosGround_Bogged".Translate(stuck.LabelShort.Named("VEHICLE"), wayText.Named("WAY"), GroundRule.StoneChunks.Named("CHUNKS")).Resolve(),
            convoy, MessageTypeDefOf.NegativeEvent, historical: true);
        VehicleStory.Of(stuck, RoadWorkDefOf.PhobosLog_Bogged, detail: wayText);
        if (Seen.Wants("vehicles.road-bog", stuck, way.ToString())) Seen.Note("vehicles.road-bog", stuck, CompPhobosSling.LoadKilograms(stuck).ToString("0") + " kg", way.ToString());
    }

    /// <summary>Stone chunks the convoy carries, its towed trailers' cargo too.</summary>
    static List<Thing> Stone(Caravan caravan) => Contracts.Carried(caravan).Where(t => t.def.IsWithinCategory(ThingCategoryDefOf.StoneChunks)).ToList();

    /// <summary>Takes this many stone chunks from the convoy; false, taking none, when it has fewer.</summary>
    static bool TakeStone(Caravan caravan, int count)
    {
        List<Thing> chunks = Stone(caravan);
        if (chunks.Sum(t => t.stackCount) < count) return false;
        int left = count;
        foreach (Thing chunk in chunks)
        {
            if (left <= 0) break;
            int take = System.Math.Min(left, chunk.stackCount);
            chunk.SplitOff(take).Destroy(DestroyMode.Vanish);
            left -= take;
        }
        if (caravan is VehicleCaravan convoy) convoy.RecacheVehicles();
        return true;
    }

    /// <summary>Why the convoy cannot lay stone under its stuck vehicle now, or null.</summary>
    public static string? StoneRefusal(VehicleCaravan convoy)
    {
        if (WorldComponent_PhobosRoadWork.Current?.HoldOf(convoy) is not { kind: HoldBogged }) return "PhobosGround_NotStuck".Translate().Resolve();
        int have = Stone(convoy).Sum(t => t.stackCount);
        if (have < GroundRule.StoneChunks) return "PhobosGround_NoStone".Translate(GroundRule.StoneChunks.Named("CHUNKS"), have.Named("HAVE")).Resolve();
        return null;
    }

    /// <summary>A load of stone under the stuck vehicle's wheels frees it at once.</summary>
    public static void LayStone(VehicleCaravan convoy)
    {
        if (StoneRefusal(convoy) is { } refused || !TakeStone(convoy, GroundRule.StoneChunks))
        {
            Messages.Message(StoneRefusal(convoy) ?? "", convoy, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        ConvoyHold hold = WorldComponent_PhobosRoadWork.Current!.HoldOf(convoy)!;
        VehiclePawn? stuck = hold.stuck;
        WorldComponent_PhobosRoadWork.Current.Release(convoy);
        Messages.Message("PhobosGround_Freed".Translate((stuck?.LabelShort ?? convoy.LabelShort).Named("VEHICLE"), GroundRule.StoneChunks.Named("CHUNKS")).Resolve(),
            convoy, MessageTypeDefOf.PositiveEvent, historical: false);
        if (Seen.Wants("vehicles.road-bog", stuck, "Stone")) Seen.Note("vehicles.road-bog", stuck, GroundRule.StoneChunks + " chunks", "Stone");
    }

    // ---- Grit on snow (EM39) ----

    /// <summary>Whether this Talus is spreading grit as it drives.</summary>
    public static bool Spreading(VehiclePawn tipper) => WorldComponent_PhobosRoadWork.Current?.Spreaders.Contains(tipper) == true;

    public static void SetSpreading(VehiclePawn tipper, bool on)
    {
        if (WorldComponent_PhobosRoadWork.Current is not { } work) return;
        if (on && !work.Spreaders.Contains(tipper)) work.Spreaders.Add(tipper);
        if (!on) work.Spreaders.Remove(tipper);
    }

    /// <summary>
    /// A spreading Talus grits a winter tile as the convoy reaches it, for <see cref="GroundRule.GritChunks"/> chunks, easing the tile's
    /// winter cost for every convoy of the player's for <see cref="GroundRule.GritDays"/> days. Out of stone, it stops and says so.
    /// </summary>
    static void Grit(WorldComponent_PhobosRoadWork work, VehicleCaravan convoy, PlanetTile tile)
    {
        if (work.Spreaders.Count == 0 || work.Gritted(tile)) return;
        VehiclePawn? tipper = convoy.VehiclesListForReading.FirstOrDefault(work.Spreaders.Contains);
        if (tipper == null || WinterPathingHelper.GetWinterPercent(tile) <= 0.01f) return;
        if (!TakeStone(convoy, GroundRule.GritChunks))
        {
            work.Spreaders.Remove(tipper);
            Messages.Message("PhobosGround_GritOut".Translate(tipper.LabelShort.Named("VEHICLE")).Resolve(), convoy, MessageTypeDefOf.CautionInput, historical: false);
            return;
        }
        work.Grit(tile, Find.TickManager.TicksGame + (int)(GroundRule.GritDays * GenDate.TicksPerDay));
        if (Seen.Wants("vehicles.road-grit", tipper)) Seen.Note("vehicles.road-grit", tipper, WinterPathingHelper.GetWinterPercent(tile).ToStringPercent() + " winter");
    }

    /// <summary>A tile's winter cost, eased where grit lies (the patches below call it).</summary>
    public static void EaseWinter(PlanetTile tile, ref float cost)
    {
        if (cost > 0f && WorldComponent_PhobosRoadWork.Current is { AnyGrit: true } work && work.Gritted(tile)) cost = GroundRule.Gritted(cost);
    }
}

/// <summary>
/// Grit eases a winter tile for vehicles (Framework 0.325.0, EM39). Vehicle Framework works out a vehicle's winter cost per tile here, with
/// no extension point; three postfixes, one per overload, each only ever lowering the cost on a gritted tile. One set look-up while any
/// grit lies; none otherwise.
/// </summary>
[HarmonyPatch(typeof(WinterPathingHelper), nameof(WinterPathingHelper.GetCurrentWinterMovementDifficultyOffset),
    new[] { typeof(List<VehiclePawn>), typeof(PlanetTile), typeof(StringBuilder) })]
public static class GritEasesWinterForVehicles
{
    public static void Postfix(PlanetTile tile, ref float __result) => RoadGround.EaseWinter(tile, ref __result);
}

[HarmonyPatch(typeof(WinterPathingHelper), nameof(WinterPathingHelper.GetCurrentWinterMovementDifficultyOffset),
    new[] { typeof(List<VehicleDef>), typeof(int), typeof(StringBuilder) })]
public static class GritEasesWinterForVehicleDefs
{
    public static void Postfix(int tile, ref float __result) => RoadGround.EaseWinter((PlanetTile)tile, ref __result);
}

[HarmonyPatch(typeof(WinterPathingHelper), nameof(WinterPathingHelper.GetCurrentWinterMovementDifficultyFor),
    new[] { typeof(VehicleDef), typeof(PlanetTile), typeof(StringBuilder) })]
public static class GritEasesWinterForOneVehicle
{
    public static void Postfix(PlanetTile tile, ref float __result) => RoadGround.EaseWinter(tile, ref __result);
}
