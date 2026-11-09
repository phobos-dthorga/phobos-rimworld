using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>When each settlement can next sell each fluid in bulk. Saved: nothing else knows it.</summary>
public class GameComponent_PhobosBulkOrders : GameComponent
{
    Dictionary<string, int> nextSaleTick = new();

    public GameComponent_PhobosBulkOrders(Game game)
    {
    }

    public override void ExposeData()
    {
        Scribe_Collections.Look(ref nextSaleTick, "PhobosBulkNextSale", LookMode.Value, LookMode.Value);
        nextSaleTick ??= new Dictionary<string, int>();
    }

    public int NextSale(string key) => nextSaleTick.TryGetValue(key, out int tick) ? tick : 0;

    public void Record(string key, int tick) => nextSaleTick[key] = tick;
}

/// <summary>One fluid a settlement will sell, at the game's own buying price for one of its items.</summary>
public sealed class BulkOffer
{
    public PhobosTankContentsDef Fluid = null!;
    public Port_Items Port = null!;
    public float PricePerItem;
    /// <summary>One unspawned item, for the game's price and its count control.</summary>
    public Thing Sample = null!;
}

/// <summary>
/// Buying a fluid in bulk at a settlement the player has travelled to, with the tanker in the caravan (owner, 9 Nov 2026: the player should have
/// reasons to go out and explore). It does not touch the trade window, which many mods patch: the game's own price and trader rules are
/// read and the silver and fluid change hands here. It only buys, so it can never make money. Gizmos call these; nothing else changes the tank.
/// </summary>
public static class BulkOrders
{
    public static Settlement? VisitedSettlement(VehiclePawn vehicle) =>
        vehicle.GetVehicleCaravan() is { } caravan ? CaravanVisitUtility.SettlementVisitedNow(caravan) : null;

    static GameComponent_PhobosBulkOrders? Ledger => Current.Game?.GetComponent<GameComponent_PhobosBulkOrders>();

    static string LedgerKey(Settlement settlement, PhobosTankContentsDef fluid) => settlement.ID + "/" + fluid.defName;

    static Pawn? Negotiator(VehiclePawn vehicle, Settlement settlement) =>
        vehicle.GetVehicleCaravan() is { } caravan ? BestCaravanPawnUtility.FindBestNegotiator(caravan, settlement.Faction, settlement.TraderKind) : null;

    /// <summary>Why this settlement will not take a bulk order at all, in-world, or null.</summary>
    public static string? SettlementRefusal(VehiclePawn vehicle, Settlement settlement)
    {
        if (!settlement.CanTradeNow || settlement.TraderKind == null) return "PhobosVehicleSystems_TankBulkNotTrading".Translate(settlement.LabelCap.Named("SETTLEMENT")).Resolve();
        if (Negotiator(vehicle, settlement) == null) return "PhobosVehicleSystems_TankBulkNoNegotiator".Translate().Resolve();
        return null;
    }

    /// <summary>What this settlement deals in that this tank can hold, by the game's own rule for what its trader handles.</summary>
    public static List<BulkOffer> Offers(CompPhobosTank tank, Settlement settlement)
    {
        var offers = new List<BulkOffer>();
        Pawn? negotiator = Negotiator(tank.Vehicle, settlement);
        TraderKindDef? kind = settlement.TraderKind;
        if (negotiator == null || kind == null) return offers;
        foreach (PhobosTankContentsDef fluid in tank.ContentsChoices())
        {
            Port_Items? port = fluid.ports.OfType<Port_Items>().FirstOrDefault(p => p.thing != null);
            if (port == null || !kind.WillTrade(port.thing!)) continue;
            Thing sample = ThingMaker.MakeThing(port.thing, GenStuff.DefaultStuffFor(port.thing));
            float priceType = kind.PriceTypeFor(port.thing, TradeAction.PlayerBuys).PriceMultiplier();
            float price = TradeUtility.GetPricePlayerBuy(sample, priceType, 1f, negotiator.GetStatValue(StatDefOf.TradePriceImprovement), settlement.TradePriceImprovementOffsetForPlayer);
            offers.Add(new BulkOffer { Fluid = fluid, Port = port, PricePerItem = price, Sample = sample });
        }
        return offers;
    }

    /// <summary>Why this fluid cannot be ordered into the selected compartment now, in-world, or null.</summary>
    public static string? OfferRefusal(CompPhobosTank tank, Settlement settlement, BulkOffer offer)
    {
        int days = BulkOrderRule.DaysUntilRestock(Ledger?.NextSale(LedgerKey(settlement, offer.Fluid)) ?? 0, Find.TickManager.TicksGame, GenDate.TicksPerDay);
        if (days > 0) return "PhobosVehicleSystems_TankBulkSoldOut".Translate(days.Named("DAYS")).Resolve();
        // As the pump refuses to load it: a burst tank would lose the load the moment it was paid for.
        if (!tank.ShellTakesLoad) return "PhobosVehicleSystems_TankHoled".Translate().Resolve();
        // Fluid the compartment already holds can be topped up; anything else goes through the tank's own rules.
        if (tank.Contents != offer.Fluid || !tank.CanTopUp)
        {
            string? refused = tank.CannotHold(offer.Fluid);
            if (refused != null) return refused;
        }
        if (BulkOrderRule.MaxItems(tank.Space, offer.Port.unitsPerItem) <= 0) return "PhobosVehicleSystems_TankBulkNoRoom".Translate().Resolve();
        return null;
    }

    /// <summary>All the silver the caravan carries.</summary>
    public static int Silver(VehiclePawn vehicle)
    {
        if (vehicle.GetVehicleCaravan() is not { } caravan) return 0;
        return CaravanInventoryUtility.AllInventoryItems(caravan).Where(t => t.def == ThingDefOf.Silver).Sum(t => t.stackCount);
    }

    /// <summary>Takes the silver and fills the selected compartment; false (with the reason said) if anything has changed since the menu opened.</summary>
    public static bool Buy(CompPhobosTank tank, Settlement settlement, BulkOffer offer, float units)
    {
        VehiclePawn vehicle = tank.Vehicle;
        string? refused = SettlementRefusal(vehicle, settlement) ?? OfferRefusal(tank, settlement, offer);
        int cost = BulkOrderRule.Cost(offer.PricePerItem, units, offer.Port.unitsPerItem);
        if (refused == null && units > tank.Space) refused = "PhobosVehicleSystems_TankBulkNoRoom".Translate().Resolve();
        if (refused == null && Silver(vehicle) < cost) refused = "PhobosVehicleSystems_TankBulkTooDear".Translate().Resolve();
        if (refused != null || vehicle.GetVehicleCaravan() is not { } caravan)
        {
            Messages.Message(refused ?? "", MessageTypeDefOf.RejectInput, historical: false);
            return false;
        }
        int owed = cost;
        foreach (Thing stack in CaravanInventoryUtility.AllInventoryItems(caravan).Where(t => t.def == ThingDefOf.Silver).ToList())
        {
            if (owed <= 0) break;
            int taken = System.Math.Min(owed, stack.stackCount);
            stack.SplitOff(taken).Destroy();
            owed -= taken;
        }
        tank.ReceiveBulk(offer.Fluid, units);
        Ledger?.Record(LedgerKey(settlement, offer.Fluid), Find.TickManager.TicksGame + tank.Props.bulkRestockDays * GenDate.TicksPerDay);
        Messages.Message("PhobosVehicleSystems_TankBulkBought".Translate(vehicle.LabelShort.Named("VEHICLE"), units.ToString("N0").Named("UNITS"), offer.Fluid.unitLabel.Named("UNIT"), offer.Fluid.label.Named("CONTENTS"), cost.Named("COST")), caravan, MessageTypeDefOf.TaskCompletion, historical: false);
        return true;
    }
}
