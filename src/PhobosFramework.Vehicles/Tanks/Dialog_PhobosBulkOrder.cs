using Phobos.Framework.Rules;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>What the game's own count control adjusts: a number of items in an order. Holds no game state.</summary>
public sealed class BulkOrderCount : Transferable
{
    readonly Thing sample;
    readonly int most;
    int count;

    public BulkOrderCount(Thing sample, int most)
    {
        this.sample = sample;
        this.most = most;
    }

    public override Thing AnyThing => sample;
    public override ThingDef ThingDef => sample.def;
    public override bool Interactive => true;
    public override bool HasAnyThing => true;
    public override string Label => sample.def.label;
    public override string TipDescription => sample.def.description;
    public override TransferablePositiveCountDirection PositiveCountDirection => TransferablePositiveCountDirection.Destination;

    public override int CountToTransfer
    {
        get => count;
        protected set
        {
            count = value;
            EditBuffer = value.ToStringCached();
        }
    }

    public override int GetMinimumToTransfer() => 0;
    public override int GetMaximumToTransfer() => most;
}

/// <summary>How much of a fluid to buy, with the number box and arrows of the game's trade window. Presentation only: it reads the offer and
/// hands the order to BulkOrders.</summary>
public sealed class Dialog_PhobosBulkOrder : Window
{
    const float RowHeight = 30f;
    const float ButtonHeight = 35f;
    const float ButtonWidth = 140f;
    const float CountWidth = 240f;

    readonly CompPhobosTank tank;
    readonly Settlement settlement;
    readonly BulkOffer offer;
    readonly BulkOrderCount count;

    public Dialog_PhobosBulkOrder(CompPhobosTank tank, Settlement settlement, BulkOffer offer)
    {
        this.tank = tank;
        this.settlement = settlement;
        this.offer = offer;
        count = new BulkOrderCount(offer.Sample, BulkOrderRule.MaxItems(tank.Space, offer.Port.unitsPerItem));
        forcePause = true;
        absorbInputAroundWindow = true;
        closeOnClickedOutside = true;
        doCloseX = true;
    }

    public override Vector2 InitialSize => new(460f, 240f);

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 35f), "PhobosVehicleSystems_TankBulkWindowTitle".Translate(offer.Fluid.LabelCap.Named("CONTENTS"), settlement.LabelCap.Named("SETTLEMENT")));
        Text.Font = GameFont.Small;
        float y = inRect.y + 45f;

        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(new Rect(inRect.x, y, inRect.width - CountWidth - 10f, RowHeight), offer.Sample.def.LabelCap);
        Text.Anchor = TextAnchor.UpperLeft;
        TransferableUIUtility.DoCountAdjustInterface(new Rect(inRect.xMax - CountWidth, y, CountWidth, RowHeight), count, 0, count.GetMinimumToTransfer(), count.GetMaximumToTransfer());
        y += RowHeight + 10f;

        float unitsPerItem = offer.Port.unitsPerItem;
        float units = count.CountToTransfer * unitsPerItem;
        int cost = BulkOrderRule.Cost(offer.PricePerItem, units, unitsPerItem);
        int silver = BulkOrders.Silver(tank.Vehicle);
        Widgets.Label(new Rect(inRect.x, y, inRect.width, 24f), "PhobosVehicleSystems_TankBulkAmount".Translate(units.ToString("N0").Named("UNITS"), offer.Fluid.unitLabel.Named("UNIT"), cost.Named("COST")));
        y += 24f;
        if (cost > silver) GUI.color = ColorLibrary.RedReadable;
        Widgets.Label(new Rect(inRect.x, y, inRect.width, 24f), "PhobosVehicleSystems_TankBulkSilverHeld".Translate(silver.Named("SILVER")));
        GUI.color = Color.white;

        Rect buy = new(inRect.xMax - ButtonWidth, inRect.yMax - ButtonHeight, ButtonWidth, ButtonHeight);
        if (Widgets.ButtonText(buy, "PhobosVehicleSystems_TankBulkBuy".Translate(), active: count.CountToTransfer > 0) && BulkOrders.Buy(tank, settlement, offer, units))
        {
            Close();
        }
    }
}
