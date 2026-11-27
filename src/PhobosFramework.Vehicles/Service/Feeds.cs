using System.Linq;
using Phobos.Framework.Defs;
using Phobos.Framework.Vehicles.Rules;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Whether a powered fitting can work now, and if not why, in words for the player (Framework 0.115.0, W4). Reads the vehicle; the rule
/// (<see cref="FeedRule"/>) decides. The player's vehicles only, as everything that drains a battery is: another faction's vehicle is
/// never refused.
/// </summary>
public static class Feeds
{
    /// <summary>
    /// What the feed is short of now, or <see cref="FeedShortfall.None"/>: the one reading every powered fitting goes through, its refusal
    /// and whether it works alike. Another faction's vehicle, one ticked at less than full detail, a vehicle without systems and a fitting
    /// with no feed are never short. Allocates nothing.
    /// </summary>
    public static FeedShortfall Shortfall(CompPhobosVehicleSystems? comp, FeedSpec? feed)
    {
        if (comp == null || feed == null || !comp.Usable || comp.Tier != Phobos.Framework.Rules.DetailTier.Full) return FeedShortfall.None;
        float quantity = 1f;
        bool hasQuantity = feed.quantity != null && comp.TryGetQuantity(feed.quantity, out quantity);
        if (!hasQuantity) quantity = 1f;
        return FeedRule.Check(feed.engineRunning, comp.EngineRunning, feed.load != null,
            feed.load == null || comp.LoadCouldRun(feed.load), hasQuantity, quantity, feed.atLeast);
    }

    /// <summary>Why <paramref name="what"/> cannot work now, or null.</summary>
    public static string? Refusal(CompPhobosVehicleSystems? comp, FeedSpec? feed, string what)
    {
        FeedShortfall shortfall = Shortfall(comp, feed);
        if (shortfall == FeedShortfall.None) return null;
        if (Seen.On) Seen.Note("vehicles.feed-refused." + (feed!.load ?? feed.quantity ?? "engine"), comp!.Vehicle, shortfall.ToString());
        string vehicle = comp!.Vehicle.LabelShort;
        return shortfall switch
        {
            FeedShortfall.EngineOff => "PhobosFeeds_EngineOff".Translate(what.Named("WHAT"), vehicle.Named("VEHICLE")).Resolve(),
            FeedShortfall.NoPower => "PhobosFeeds_NoPower".Translate(what.Named("WHAT"), vehicle.Named("VEHICLE")).Resolve(),
            FeedShortfall.Short => "PhobosFeeds_Short".Translate(what.Named("WHAT"), vehicle.Named("VEHICLE"), QuantityLabel(comp, feed!.quantity!).Named("SUPPLY")).Resolve(),
            _ => null,
        };
    }

    /// <summary>The feed of the fitting that grants this ability, if one is fitted.</summary>
    public static FeedSpec? ForAbility(CompPhobosVehicleSystems? comp, string ability) => comp?.Fittings.FeedFor(ability);

    /// <summary>
    /// Whether a fitting granting this ability is on a standing part and its feed is met now (the owner's card of 28 Nov 2026, "Unread
    /// feed": the route kit, the uncrewed kit, the mech link and the Radio's sets had checked their load by name; now each fitting's own
    /// <c>feed</c> says what it needs, as for the rescue hoist, the recovery winch and the cabin oxygen). Allocates nothing past the grant.
    /// </summary>
    public static bool Works(CompPhobosVehicleSystems? comp, string ability) =>
        comp != null && comp.Fittings.Grants(ability) && Shortfall(comp, ForAbility(comp, ability)) == FeedShortfall.None;

    /// <summary>As <see cref="Works"/>, for a vehicle.</summary>
    public static bool Works(VehiclePawn vehicle, string ability) => Works(vehicle.GetComp<CompPhobosVehicleSystems>(), ability);

    static string QuantityLabel(CompPhobosVehicleSystems comp, string name)
    {
        foreach (ResolvedSystem system in comp.Props.systems)
        {
            QuantitySpec? spec = system.def.quantities.FirstOrDefault(q => q.name == name);
            if (spec != null) return spec.label;
        }
        return name;
    }
}
