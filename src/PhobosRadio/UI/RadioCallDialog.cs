using System.Collections.Generic;
using System.Linq;
using Phobos.Radio.Physics;
using Phobos.Radio.Services;
using RimWorld;
using Verse;

namespace Phobos.Radio.UI;

/// <summary>
/// The radio call to another faction, as the game's negotiation window (Radio 0.53.0; docs/development/radio-comms-console.md): a greeting
/// that says how good the line is, then what a radio call may ask: a trader caravan, military aid, a check-in. Presentation only: each
/// choice is one call to <see cref="RadioDiplomacy"/>, and a choice that cannot be made says why, in the game's own words where they exist.
/// </summary>
public static class RadioCallDialog
{
    public static void Open(Pawn negotiator, Faction faction, FactionDoctorLink link)
    {
        Find.WindowStack.Add(new Dialog_Negotiation(negotiator, faction, Root(negotiator, faction, link), radioMode: true));
    }

    static DiaNode Root(Pawn negotiator, Faction faction, FactionDoctorLink link)
    {
        Map map = negotiator.Map;
        LinkOutcome line = RadioDiplomacy.Line(link);
        string greeting = "PhobosRadioCall_Greeting".Translate(link.Settlement.LabelCap.Named("SETTLEMENT"), faction.Name.Named("FACTION"),
            LinkText.OutcomeLabel(line).Named("LINE")).Resolve();
        if (line == LinkOutcome.Marginal) greeting += "\n\n" + "PhobosRadioCall_MarginalWarning".Translate(Percent(RadioRulesDef.Current.radioMishearChance).Named("CHANCE")).Resolve();
        else if (line == LinkOutcome.Clean) greeting += "\n\n" + "PhobosRadioCall_CleanLine".Translate().Resolve();
        var root = new DiaNode(greeting);
        root.options.Add(TraderOption(negotiator, map, faction, link, line));
        root.options.Add(AidOption(negotiator, map, faction, link, line));
        root.options.Add(CheckInOption(negotiator, faction, link));
        root.options.Add(new DiaOption("(" + "Disconnect".Translate() + ")") { resolveTree = true });
        return root;
    }

    static DiaOption TraderOption(Pawn negotiator, Map map, Faction faction, FactionDoctorLink link, LinkOutcome line)
    {
        int cost = -Faction.OfPlayer.CalculateAdjustedGoodwillChange(faction, -RadioDiplomacy.TraderGoodwill(line));
        var option = new DiaOption("PhobosRadioCall_Trader".Translate(cost.Named("GOODWILL"),
            RadioDiplomacy.TravelTicks(link.Settlement.Tile, map.Tile).ToStringTicksToPeriod().Named("TIME")).Resolve());
        if (RadioDiplomacy.TraderRefusal(map, faction) is { } refused)
        {
            option.Disable(refused);
            return option;
        }
        var kinds = new DiaNode("ChooseTraderKind".Translate(faction.leader));
        foreach (TraderKindDef kind in faction.def.caravanTraderKinds.Where(k => k.requestable))
        {
            TraderKindDef chosen = kind;
            var pick = new DiaOption(kind.LabelCap);
            if (kind.TitleRequiredToTrade != null && (negotiator.royalty == null || kind.TitleRequiredToTrade.seniority > negotiator.GetCurrentTitleSeniorityIn(faction)))
                pick.Disable("PhobosRadioCall_TitleNeeded".Translate(kind.TitleRequiredToTrade.GetLabelCapFor(negotiator).Named("TITLE")).Resolve());
            else pick.linkLateBind = () => Reply(RadioDiplomacy.RequestTrader(negotiator, map, faction, chosen, link));
            kinds.options.Add(pick);
        }
        kinds.options.Add(new DiaOption("GoBack".Translate()) { linkLateBind = () => Root(negotiator, faction, link) });
        option.link = kinds;
        return option;
    }

    static DiaOption AidOption(Pawn negotiator, Map map, Faction faction, FactionDoctorLink link, LinkOutcome line)
    {
        int cost = -Faction.OfPlayer.CalculateAdjustedGoodwillChange(faction, -RadioDiplomacy.AidGoodwill(line));
        var option = new DiaOption("RequestMilitaryAid".Translate(cost));
        if (RadioDiplomacy.AidRefusal(map, faction) is { } refused) option.Disable(refused);
        else option.linkLateBind = () => Reply(RadioDiplomacy.RequestAid(negotiator, map, faction, link));
        return option;
    }

    static DiaOption CheckInOption(Pawn negotiator, Faction faction, FactionDoctorLink link)
    {
        var option = new DiaOption("PhobosRadioCall_CheckIn".Translate(RadioRulesDef.Current.radioCheckInGoodwill.Named("GOODWILL")).Resolve());
        if (RadioDiplomacy.CheckInRefusal(faction) is { } refused) option.Disable(refused);
        else option.linkLateBind = () => Reply(RadioDiplomacy.CheckIn(negotiator, faction, link));
        return option;
    }

    /// <summary>What the faction said back, and the end of the call.</summary>
    static DiaNode Reply(string text)
    {
        var node = new DiaNode(text);
        node.options.Add(new DiaOption("OK".Translate()) { resolveTree = true });
        return node;
    }

    static string Percent(float share) => (share * 100f).ToString("0") + "%";
}
