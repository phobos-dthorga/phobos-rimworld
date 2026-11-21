using System.Collections.Generic;
using System.Linq;
using DubsBadHygiene;
using HarmonyLib;
using Phobos.Framework.Hygiene;
using Phobos.Shared;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems.Hygiene;

/// <summary>
/// Water in tow is used on the road (Framework 0.216.0; the owner, 22 Nov 2026; docs/development/vehicle-trailers.md, "Water on the road").
/// Dubs Bad Hygiene gives a caravan water only from a packed water butt, bottles, rivers and rain; a towed water bowser, a shower trailer or a
/// field kitchen's tank gave it none. Registered once at startup: the caravan's tanks become its water for the kitchen's kettle, and a look
/// just ahead of Dubs' own lets its people drink and wash from them. In Dubs' lite mode there is no water, so nothing is registered.
/// </summary>
[StaticConstructorOnStartup]
public static class CaravanWaterSetup
{
    static CaravanWaterSetup()
    {
        if (PlumbingFeed.LiteMode) return;
        CaravanWater.Register(new WaterAboard());
        HarmonyPatchApplier.ApplyEach(new Harmony(VehicleSystemsMod.HarmonyId + ".Hygiene"), typeof(CaravanWaterSetup).Assembly, VehicleSystemsMod.LogPrefix);
    }
}

/// <summary>
/// The water a caravan's vehicles carry, driven or in tow: every tank compartment holding a fluid that plumbs to Dubs' pipes (water), and a
/// field kitchen's own tank. A store that runs down as it is used and is refilled by the player's people, never by itself.
/// </summary>
public sealed class WaterAboard : CaravanWater.ISource
{
    static List<PhobosTankContentsDef>? waters;

    /// <summary>The tank contents that are water: those a port of ours pumps to and from Dubs' plumbing. Found once.</summary>
    static List<PhobosTankContentsDef> Waters =>
        waters ??= DefDatabase<PhobosTankContentsDef>.AllDefsListForReading.Where(def => def.ports.Any(port => port is Port_DubsPlumbing)).ToList();

    /// <summary>Every vehicle of the caravan, and everything it tows (a towed vehicle rides in its hitch and is no member).</summary>
    static IEnumerable<VehiclePawn> Carriers(Caravan caravan)
    {
        foreach (Pawn pawn in caravan.PawnsListForReading)
        {
            if (pawn is VehiclePawn vehicle) yield return vehicle;
        }
        foreach (VehiclePawn towed in Hitches.InTow(caravan)) yield return towed;
    }

    public float Litres(Caravan caravan)
    {
        float litres = 0f;
        foreach (VehiclePawn carrier in Carriers(caravan))
        {
            if (carrier.GetComp<CompPhobosTank>() is { } tank)
            {
                foreach (TankCompartment compartment in tank.Compartments)
                {
                    if (compartment.contents != null && compartment.amount > 0f && Waters.Contains(compartment.contents)) litres += compartment.amount;
                }
            }
            if (carrier.GetComp<CompPhobosTrailerPlumbing>() is { OwnsWater: true } kitchen) litres += kitchen.OwnWater;
        }
        return litres;
    }

    public float Take(Caravan caravan, float litres, bool kitchenFirst) => Take(caravan, litres, kitchenFirst, out _);

    /// <summary>Takes up to this much; second, the worst quality it came from (Dubs' contamination level, as the tanks remember it).</summary>
    public static float Take(Caravan caravan, float litres, bool kitchenFirst, out int quality)
    {
        quality = 0;
        float taken = 0f;
        List<VehiclePawn> carriers = Carriers(caravan).ToList();
        for (int pass = 0; pass < 2 && taken < litres - 0.0001f; pass++)
        {
            bool kitchens = pass == 0 ? kitchenFirst : !kitchenFirst;
            foreach (VehiclePawn carrier in carriers)
            {
                if (taken >= litres - 0.0001f) break;
                if (kitchens)
                {
                    // A kitchen's own water does not keep its quality while packed: taken as it was filled (a simplification).
                    if (carrier.GetComp<CompPhobosTrailerPlumbing>() is { OwnsWater: true } kitchen) taken += kitchen.TakeOwnWater(litres - taken);
                    continue;
                }
                if (carrier.GetComp<CompPhobosTank>() is not { } tank) continue;
                foreach (PhobosTankContentsDef water in Waters)
                {
                    if (taken >= litres - 0.0001f) break;
                    float got = tank.TakeOut(water, litres - taken, out int from);
                    if (got <= 0f) continue;
                    taken += got;
                    if (from > quality) quality = from;
                }
            }
        }
        return taken;
    }

    /// <summary>
    /// Hot water for one shower on the road (Framework 0.228.0): burns this much chemfuel in a packed water heater along (a set-out piece
    /// with Dubs' boiler and a fuel tank, as the shower trailer's), the fullest first. False when none has enough: the shower is cold.
    /// </summary>
    public static bool HeatShower(Caravan caravan, float fuel)
    {
        CompRefuelable? best = null;
        foreach (VehiclePawn carrier in Carriers(caravan))
        {
            if (carrier.GetComp<CompPhobosSetOut>() is not { } setOut) continue;
            foreach (Thing piece in setOut.PackedPieces)
            {
                if (!piece.def.comps.Any(comp => comp is CompProperties_CompBoiler)) continue;
                if (piece.TryGetComp<CompRefuelable>() is not { } tank || tank.Fuel < fuel) continue;
                if (best == null || tank.Fuel > best.Fuel) best = tank;
            }
        }
        if (best == null) return false;
        best.ConsumeFuel(fuel);
        return true;
    }

    /// <summary>Whether the caravan has a shower trailer along: plumbing of ours that is not a kitchen's own tank.</summary>
    public static bool HasShower(Caravan caravan) => Carriers(caravan).Any(carrier => carrier.GetComp<CompPhobosTrailerPlumbing>() is { OwnsWater: false });
}

/// <summary>
/// Drinking and washing from the caravan's tanks. A thirsty member drinks from them before Dubs' own order reaches for bottles; a dirty one
/// washes while the caravan stands still, under the shower trailer's showers if one is along and from a basin's worth of water if not.
/// What the look found is kept a few seconds per caravan, so a caravan with no water aboard costs two need reads and one dictionary read.
/// </summary>
public static class CaravanDrinking
{
    const int LookTicks = 250;

    sealed class Seen_
    {
        public int tick;
        public float litres;
        public bool shower;
        public bool saidEmpty;
        public bool saidCold;
    }

    static readonly Dictionary<int, Seen_> seen = new();

    static Seen_ Aboard(Caravan caravan)
    {
        int now = Find.TickManager.TicksGame;
        if (!seen.TryGetValue(caravan.ID, out Seen_? found)) seen[caravan.ID] = found = new Seen_ { tick = now - LookTicks };
        if (now - found.tick < LookTicks && now >= found.tick) return found;
        found.tick = now;
        found.litres = CaravanWater.Litres(caravan);
        found.shower = found.litres > 0f && WaterAboard.HasShower(caravan);
        if (found.litres > 0f) found.saidEmpty = false;
        return found;
    }

    static bool StandingStill(Caravan caravan) => caravan is VehicleCaravan vehicles ? !vehicles.vehiclePather.MovingNow : !caravan.pather.MovingNow;

    /// <summary>
    /// A shower on the road is hot while the packed water heater has chemfuel, and Dubs' own check then follows (a hot shower eases
    /// hypothermia); cold without it, with Dubs' cold-water memory. Says once, per caravan, when the heater runs dry.
    /// </summary>
    static void Shower(Caravan caravan, Pawn pawn, Seen_ aboard, PhobosVehicleRulesDef rules)
    {
        bool hot = WaterAboard.HeatShower(caravan, rules.caravanShowerFuel);
        SanitationUtil.WaterTempCheck(pawn, cold: !hot, bath: false);
        if (Seen.Wants("trailers.caravan-shower-heat", pawn, hot ? "hot" : "cold")) Seen.Note("trailers.caravan-shower-heat", pawn, caravan.Label, hot ? "hot" : "cold");
        if (hot)
        {
            aboard.saidCold = false;
        }
        else if (!aboard.saidCold)
        {
            aboard.saidCold = true;
            Messages.Message("PhobosCaravanWater_ShowerCold".Translate(caravan.Label.Named("CARAVAN")).Resolve(), caravan, MessageTypeDefOf.CautionInput, historical: false);
            VehicleStory.Convoy(caravan, VehicleStoryDefOf.PhobosLog_HeaterDry);
        }
    }

    public static void Look(Caravan? caravan, Pawn pawn)
    {
        if (caravan == null || pawn.Dead || pawn.needs == null || caravan.Faction != Faction.OfPlayer) return;
        Need_Thirst? thirst = pawn.needs.TryGetNeed<Need_Thirst>();
        Need_Hygiene? hygiene = pawn.needs.TryGetNeed<Need_Hygiene>();
        if (thirst == null && hygiene == null) return;
        PhobosVehicleRulesDef rules = PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();
        bool thirsty = thirst != null && thirst.CurCategory != ThirstCategory.Hydrated;
        bool dirty = hygiene != null && hygiene.CurLevel < rules.caravanWashBelow;
        if (!thirsty && !dirty) return;
        Seen_ aboard = Aboard(caravan);
        if (aboard.litres <= 0f) return;

        if (thirsty && aboard.litres >= rules.caravanDrinkLitres)
        {
            float got = WaterAboard.Take(caravan, rules.caravanDrinkLitres, kitchenFirst: false, out int quality);
            aboard.litres -= got;
            if (got >= rules.caravanDrinkLitres - 0.001f)
            {
                thirst!.CurLevel = 1f;
                SanitationUtil.ContaminationCheckWater(pawn, (ContaminationLevel)quality);
                if (Seen.Wants("trailers.caravan-drink", pawn)) Seen.Note("trailers.caravan-drink", pawn, got.ToString("0.#") + " L");
            }
        }
        if (dirty && StandingStill(caravan))
        {
            float wants = aboard.shower ? rules.caravanShowerLitres : rules.caravanWashLitres;
            if (aboard.litres >= wants)
            {
                float got = WaterAboard.Take(caravan, wants, kitchenFirst: false, out _);
                aboard.litres -= got;
                if (got >= wants - 0.001f)
                {
                    hygiene!.CurLevel = aboard.shower ? rules.caravanShowerTo : rules.caravanWashTo;
                    string how = aboard.shower ? "shower" : "basin";
                    if (aboard.shower) Shower(caravan, pawn, aboard, rules);
                    if (Seen.Wants("trailers.caravan-wash", pawn, how)) Seen.Note("trailers.caravan-wash", pawn, got.ToString("0.#") + " L", how);
                }
            }
        }
        if (aboard.litres < rules.caravanDrinkLitres && !aboard.saidEmpty)
        {
            aboard.saidEmpty = true;
            Messages.Message("PhobosCaravanWater_Empty".Translate(caravan.Label.Named("CARAVAN")).Resolve(), caravan, MessageTypeDefOf.CautionInput, historical: false);
            VehicleStory.Convoy(caravan, VehicleStoryDefOf.PhobosLog_WaterDry);
        }
    }
}

/// <summary>
/// Before the game (and Dubs Bad Hygiene's own prefix on the same method) looks after a caravan member's needs, lets them drink and wash from
/// the caravan's tanks. A prefix that never skips: it only raises two needs and takes the water for them, so Dubs' look then finds a member
/// who is no longer thirsty and leaves the bottles alone. First in line so that it runs ahead of Dubs' prefix.
/// </summary>
[HarmonyPatch(typeof(Caravan_NeedsTracker), "TrySatisfyPawnNeeds", new[] { typeof(Pawn), typeof(int) })]
public static class CaravanWaterNeeds
{
    [HarmonyPriority(Priority.First)]
    public static void Prefix(Caravan_NeedsTracker __instance, Pawn pawn) => CaravanDrinking.Look(__instance.caravan, pawn);
}
