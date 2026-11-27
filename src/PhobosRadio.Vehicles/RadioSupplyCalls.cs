using System;
using Phobos.Radio.Physics;
using Phobos.Radio.Services;
using Phobos.Radio.UI;
using Phobos.VehicleSystems;
using RimWorld;
using Verse;

namespace Phobos.Radio.Vehicles;

/// <summary>
/// The allied supply drop on the radio call (Radio 0.54.0; the owner's pick of 27 Nov 2026): D'thorga Framework's drop, which the comms
/// console offers to every ally, asked for over the radio from a faction in reach, cheaper on a clean line and misheard now and then on a
/// marginal one, as the radio's other requests. The console's offer is unchanged. Presentation in <see cref="Option"/>; the request in
/// <see cref="Request"/>.
/// </summary>
public static class RadioSupplyCalls
{
    /// <summary>The call's option: refused with the drop's own reason when it cannot be asked for.</summary>
    public static DiaOption? Option(Pawn negotiator, Faction faction, FactionDoctorLink link, Func<DiaNode> back)
    {
        if (GameComponent_PhobosSupplyDrops.Current is not { } drops || negotiator.Map is not { } map) return null;
        int cost = RadioDiplomacy.SupplyGoodwill(RadioDiplomacy.Line(link), GameComponent_PhobosSupplyDrops.GoodwillCost);
        var option = new DiaOption("PhobosSupply_Option".Translate(cost.Named("GOODWILL")));
        if (drops.Refusal(faction, map) is { } refused)
        {
            option.Disable(refused);
            return option;
        }
        var choose = new DiaNode("PhobosSupply_Choose".Translate(faction.Name.Named("FACTION"), GameComponent_PhobosSupplyDrops.ValueSilver.ToString("0").Named("VALUE")));
        foreach (SupplyKind kind in new[] { SupplyKind.Food, SupplyKind.Medicine, SupplyKind.Materials })
        {
            SupplyKind chosen = kind;
            choose.options.Add(new DiaOption(GameComponent_PhobosSupplyDrops.KindLabel(kind))
            {
                linkLateBind = () => RadioCallDialog.ReplyNode(Request(negotiator, map, faction, chosen, link)),
            });
        }
        choose.options.Add(new DiaOption("GoBack".Translate()) { linkLateBind = back });
        option.link = choose;
        return option;
    }

    /// <summary>Asks for the drop over this line: misheard now and then on a marginal line, the goodwill spent; else the Framework's drop at the line's price.</summary>
    public static string Request(Pawn negotiator, Map map, Faction faction, SupplyKind kind, FactionDoctorLink link)
    {
        LinkOutcome line = RadioDiplomacy.Line(link);
        int cost = RadioDiplomacy.SupplyGoodwill(line, GameComponent_PhobosSupplyDrops.GoodwillCost);
        RadioDiplomacy.Called(negotiator, faction, link, "supply");
        if (RadioDiplomacy.Misheard(line))
        {
            Faction.OfPlayer.TryAffectGoodwillWith(faction, -cost, canSendMessage: false, canSendHostilityLetter: true);
            return RadioDiplomacy.MisheardReply(faction);
        }
        GameComponent_PhobosSupplyDrops.Current?.Request(faction, map, kind, cost);
        return "PhobosSupply_Sent".Translate(faction.Name.Named("FACTION")).Resolve();
    }
}
