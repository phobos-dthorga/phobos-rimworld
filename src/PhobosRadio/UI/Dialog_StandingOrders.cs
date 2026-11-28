using Phobos.Radio.Services;
using UnityEngine;
using Verse;

namespace Phobos.Radio.UI;

/// <summary>
/// A station's standing orders (Radio 0.73.0; docs/development/radio-life-on-the-air.md, RN5): tick boxes for what the colonist listening at
/// the radio may do unasked. Presents only: each change is one call to <see cref="RadioStandingOrders"/>.
/// </summary>
public class Dialog_PhobosStandingOrders : Window
{
    private readonly CompRadio station;
    private string silverBuffer;

    public Dialog_PhobosStandingOrders(CompRadio station)
    {
        this.station = station;
        silverBuffer = station.orderTraderSilver.ToString();
        doCloseX = true;
        doCloseButton = true;
        closeOnClickedOutside = true;
        absorbInputAroundWindow = true;
        forcePause = false;
    }

    public override Vector2 InitialSize => new(560f, 400f);

    public override void DoWindowContents(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(new Rect(inRect.x, inRect.y, inRect.width, inRect.height - CloseButSize.y - 10f));
        Text.Font = GameFont.Medium;
        listing.Label("PhobosRadioOrders_Title".Translate(station.parent.LabelCap.Named("STATION")));
        Text.Font = GameFont.Small;
        GUI.color = ColoredText.SubtleGrayColor;
        listing.Label("PhobosRadioOrders_Intro".Translate());
        GUI.color = Color.white;
        listing.Gap();

        bool answer = station.orderAnswerNeighbours;
        listing.CheckboxLabeled("PhobosRadioOrders_AnswerNeighbours".Translate(), ref answer, "PhobosRadioOrders_AnswerNeighboursDesc".Translate());
        if (answer != station.orderAnswerNeighbours) RadioStandingOrders.SetAnswerNeighbours(station, answer);
        listing.Gap();

        bool trader = station.orderAskTrader;
        listing.CheckboxLabeled("PhobosRadioOrders_AskTrader".Translate(), ref trader, "PhobosRadioOrders_AskTraderDesc".Translate());
        if (trader != station.orderAskTrader) RadioStandingOrders.SetAskTrader(station, trader);
        listing.Label("PhobosRadioOrders_TraderSilver".Translate(station.orderTraderSilver.Named("SILVER")));
        int silver = station.orderTraderSilver;
        // The game's own number box with its arrows, in steps of 100.
        Widgets.IntEntry(listing.GetRect(28f), ref silver, ref silverBuffer, RadioStandingOrders.TraderSilverStep);
        if (silver != station.orderTraderSilver)
        {
            RadioStandingOrders.SetTraderSilver(station, silver);
            // Only when the bounds changed it, so typing a number is not interrupted.
            if (station.orderTraderSilver != silver) silverBuffer = station.orderTraderSilver.ToString();
        }
        GUI.color = ColoredText.SubtleGrayColor;
        Text.Font = GameFont.Tiny;
        listing.Label("PhobosRadioOrders_AskTraderCost".Translate());
        Text.Font = GameFont.Small;
        GUI.color = Color.white;
        listing.End();
    }
}
