using System.Linq;
using Phobos.Framework.Defs;
using Phobos.Framework.Vehicles.Rules;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Whether a powered fitting can work now, and if not why, in words for the player (Framework 0.115.0, W4). Reads the vehicle; the rule
/// (<see cref="FeedRule"/>) decides. The player's vehicles only, as everything that drains a battery is: another faction's vehicle is
/// never refused.
/// </summary>
public static class Feeds
{
    /// <summary>Why <paramref name="what"/> cannot work now, or null.</summary>
    public static string? Refusal(CompPhobosVehicleSystems? comp, FeedSpec? feed, string what)
    {
        if (comp == null || feed == null || !comp.Usable || comp.Tier != Phobos.Framework.Rules.DetailTier.Full) return null;
        float flat = PhobosRulesDef.Current<PhobosKnockOutRulesDef>().batteryFlatBelow;
        bool hasQuantity = feed.quantity != null && comp.TryGetQuantity(feed.quantity, out _);
        float quantity = hasQuantity && comp.TryGetQuantity(feed.quantity!, out float q) ? q : 1f;
        FeedShortfall shortfall = FeedRule.Check(feed.engineRunning, comp.EngineRunning, feed.load != null,
            feed.load == null || comp.LoadCouldRun(feed.load, flat), hasQuantity, quantity, feed.atLeast);
        if (shortfall != FeedShortfall.None) Seen.Note("vehicles.feed-refused." + (feed.load ?? feed.quantity ?? "engine"), comp.Vehicle, shortfall.ToString());
        string vehicle = comp.Vehicle.LabelShort;
        return shortfall switch
        {
            FeedShortfall.EngineOff => "PhobosFeeds_EngineOff".Translate(what.Named("WHAT"), vehicle.Named("VEHICLE")).Resolve(),
            FeedShortfall.NoPower => "PhobosFeeds_NoPower".Translate(what.Named("WHAT"), vehicle.Named("VEHICLE")).Resolve(),
            FeedShortfall.Short => "PhobosFeeds_Short".Translate(what.Named("WHAT"), vehicle.Named("VEHICLE"), QuantityLabel(comp, feed.quantity!).Named("SUPPLY")).Resolve(),
            _ => null,
        };
    }

    /// <summary>The feed of the fitting that grants this ability, if one is fitted.</summary>
    public static FeedSpec? ForAbility(CompPhobosVehicleSystems? comp, string ability) =>
        comp?.Fittings.FittedDefs().FirstOrDefault(def => def.grants.Contains(ability) && def.feed != null)?.feed;

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
