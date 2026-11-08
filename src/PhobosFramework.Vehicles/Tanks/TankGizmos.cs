using System.Collections.Generic;
using Phobos.Framework.Rules;
using RimWorld.Planet;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>The tanker's buttons. Presentation only: each reads the comp and calls one of its service methods.</summary>
public static class TankGizmos
{
    const string Icons = "Phobos/VehicleSystems/Tank/";

    public static IEnumerable<Gizmo> For(CompPhobosTank tank)
    {
        if (tank.Compartments.Count > 1) yield return CompartmentButton(tank);
        yield return ContentsButton(tank);
        yield return DeployButton(tank);
        if (!tank.Deployed) yield break;
        yield return ModeButton(tank, TankPumpMode.Load, "PhobosVehicleSystems_TankGizmoLoad".Translate(), "PhobosVehicleSystems_TankGizmoLoadDesc".Translate(), "Load");
        yield return ModeButton(tank, TankPumpMode.Unload, "PhobosVehicleSystems_TankGizmoUnload".Translate(), "PhobosVehicleSystems_TankGizmoUnloadDesc".Translate(), "Unload");
        yield return ModeButton(tank, TankPumpMode.Hold, "PhobosVehicleSystems_TankGizmoHold".Translate(), "PhobosVehicleSystems_TankGizmoHoldDesc".Translate(), "Hold");
    }

    /// <summary>In a caravan: the compartment button, and at a settlement the bulk order.</summary>
    public static IEnumerable<Gizmo> ForCaravan(CompPhobosTank tank)
    {
        Settlement? settlement = BulkOrders.VisitedSettlement(tank.Vehicle);
        if (settlement == null) yield break;
        if (tank.Compartments.Count > 1) yield return CompartmentButton(tank);
        yield return BulkOrderButton(tank, settlement);
    }

    static Gizmo BulkOrderButton(CompPhobosTank tank, Settlement settlement)
    {
        var command = new Command_Action
        {
            defaultLabel = "PhobosVehicleSystems_TankGizmoBulkOrder".Translate().Resolve(),
            defaultDesc = "PhobosVehicleSystems_TankGizmoBulkOrderDesc".Translate(settlement.LabelCap.Named("SETTLEMENT")).Resolve(),
            icon = SystemIcons.For(Icons + "BulkOrder"),
            action = () => Find.WindowStack.Add(new FloatMenu(OfferOptions(tank, settlement))),
        };
        string? refused = BulkOrders.SettlementRefusal(tank.Vehicle, settlement);
        if (refused != null) command.Disable(refused);
        return command;
    }

    static List<FloatMenuOption> OfferOptions(CompPhobosTank tank, Settlement settlement)
    {
        var options = new List<FloatMenuOption>();
        foreach (BulkOffer offer in BulkOrders.Offers(tank, settlement))
        {
            BulkOffer captured = offer;
            int per100 = BulkOrderRule.Cost(offer.PricePerItem, 100f, offer.Port.unitsPerItem);
            string label = "PhobosVehicleSystems_TankBulkOffer".Translate(offer.Fluid.LabelCap.Named("CONTENTS"), per100.Named("PRICE"), offer.Fluid.unitLabel.Named("UNIT"));
            string? refused = BulkOrders.OfferRefusal(tank, settlement, offer);
            options.Add(refused != null
                ? new FloatMenuOption(label + ": " + refused, null)
                : new FloatMenuOption(label, () => Find.WindowStack.Add(new FloatMenu(AmountOptions(tank, settlement, captured)))));
        }
        if (options.Count == 0) options.Add(new FloatMenuOption("PhobosVehicleSystems_TankBulkNothing".Translate(), null));
        return options;
    }

    static List<FloatMenuOption> AmountOptions(CompPhobosTank tank, Settlement settlement, BulkOffer offer)
    {
        var options = new List<FloatMenuOption>();
        int silver = BulkOrders.Silver(tank.Vehicle);
        foreach (float share in new[] { 0.25f, 0.5f, 1f })
        {
            float units = BulkOrderRule.UnitsForShare(tank.Space, share, offer.Port.unitsPerItem);
            if (units <= 0f) continue;
            int cost = BulkOrderRule.Cost(offer.PricePerItem, units, offer.Port.unitsPerItem);
            string label = "PhobosVehicleSystems_TankBulkAmount".Translate(units.ToString("N0").Named("UNITS"), offer.Fluid.unitLabel.Named("UNIT"), cost.Named("COST"));
            options.Add(cost > silver
                ? new FloatMenuOption(label + ": " + "PhobosVehicleSystems_TankBulkTooDear".Translate().Resolve(), null)
                : new FloatMenuOption(label, () => BulkOrders.Buy(tank, settlement, offer, units)));
        }
        if (options.Count == 0) options.Add(new FloatMenuOption("PhobosVehicleSystems_TankBulkNoRoom".Translate(), null));
        return options;
    }

    static Gizmo CompartmentButton(CompPhobosTank tank) => new Command_Action
    {
        defaultLabel = "PhobosVehicleSystems_TankGizmoCompartment".Translate((tank.SelectedIndex + 1).Named("INDEX"), tank.Compartments.Count.Named("COUNT")).Resolve(),
        defaultDesc = "PhobosVehicleSystems_TankGizmoCompartmentDesc".Translate().Resolve(),
        icon = SystemIcons.For(Icons + "Compartment"),
        action = tank.SelectNextCompartment,
    };

    static Gizmo ContentsButton(CompPhobosTank tank) => new Command_Action
    {
        defaultLabel = tank.Contents == null
            ? "PhobosVehicleSystems_TankGizmoChoose".Translate().Resolve()
            : "PhobosVehicleSystems_TankGizmoContents".Translate(tank.Contents.label.Named("CONTENTS")).Resolve(),
        defaultDesc = "PhobosVehicleSystems_TankGizmoChooseDesc".Translate().Resolve(),
        icon = SystemIcons.For(Icons + "Contents"),
        action = () => Find.WindowStack.Add(new FloatMenu(ContentsOptions(tank))),
    };

    static List<FloatMenuOption> ContentsOptions(CompPhobosTank tank)
    {
        var options = new List<FloatMenuOption>();
        foreach (PhobosTankContentsDef def in tank.ContentsChoices())
        {
            PhobosTankContentsDef captured = def;
            string? refused = tank.CannotHold(def);
            options.Add(refused != null
                ? new FloatMenuOption(def.LabelCap + ": " + refused, null)
                : new FloatMenuOption(def.LabelCap, () => tank.ChooseContents(captured)));
        }
        if (options.Count == 0) options.Add(new FloatMenuOption("PhobosVehicleSystems_TankNothingToCarry".Translate(), null));
        return options;
    }

    static Gizmo DeployButton(CompPhobosTank tank)
    {
        bool on = tank.Deployed;
        var command = new Command_Action
        {
            defaultLabel = on ? "PhobosVehicleSystems_TankGizmoPackUp".Translate().Resolve() : "PhobosVehicleSystems_TankGizmoSetUp".Translate().Resolve(),
            defaultDesc = on ? "PhobosVehicleSystems_TankGizmoPackUpDesc".Translate().Resolve() : "PhobosVehicleSystems_TankGizmoSetUpDesc".Translate().Resolve(),
            icon = SystemIcons.For(Icons + (on ? "PackUp" : "SetUp")),
            action = () => tank.SetDeployed(!on),
        };
        string? refused = on ? null : tank.CannotDeploy();
        if (refused != null) command.Disable(refused);
        return command;
    }

    static Gizmo ModeButton(CompPhobosTank tank, TankPumpMode mode, string label, string description, string icon)
    {
        var command = new Command_Action
        {
            defaultLabel = label,
            defaultDesc = description,
            icon = SystemIcons.For(Icons + icon),
            action = () => tank.SetMode(mode),
        };
        if (tank.Mode == mode) command.Disable("PhobosVehicleSystems_TankGizmoAlready".Translate().Resolve());
        return command;
    }
}
