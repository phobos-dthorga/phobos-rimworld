using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using RimWorld;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The orders a right-click on a schematic part offers (Framework 0.378.0; docs/development/vehicle-schematic.md, SC4; the owner's card of
/// 28 Nov 2026, "Right-click a part"): only work the mod already has, each a one-line call to the service that does it, as its command
/// button would. An order that cannot be given now is shown greyed out with its reason, as the game's own menus do. Presentation only.
/// </summary>
public static class SchematicOrders
{
    const string BatteryRole = "Battery";
    const string StarterRole = "Starter";

    /// <summary>The menu for one part, in a fixed order: the Systems tab, a service, field repairs, a jump-start, fittings, a diagnosis.</summary>
    public static List<FloatMenuOption> For(CompPhobosVehicleSystems comp, SchematicNode node, SchematicView.NodeView view, System.Action openSystem)
    {
        var options = new List<FloatMenuOption>();
        if (view.State != null)
        {
            options.Add(new FloatMenuOption("PhobosVehicleSystems_OrderOpenSystem".Translate(view.State.Def.label.Named("SYSTEM")), openSystem));
        }

        // A service: the vehicle's request, which covers every system with one; this part's system named where it is due.
        if (comp.AllServices().Any())
        {
            bool due = view.State != null && comp.DueServices().Contains(view.State);
            string label = comp.ServiceRequested
                ? "PhobosVehicleSystems_OrderCancelService".Translate()
                : due ? "PhobosVehicleSystems_OrderServiceDue".Translate(view.State!.Def.service!.label.Named("SERVICE")) : "PhobosVehicleSystems_RequestService".Translate();
            options.Add(new FloatMenuOption(label, () => Order(comp, "service", () => comp.SetServiceRequested(!comp.ServiceRequested))));
        }

        // Field repairs: the vehicle's request, while something on it can be patched up where it stands.
        if (comp.Repairs.AnyFieldFixable)
        {
            string label = comp.Repairs.FieldFixRequested ? "PhobosVehicleSystems_OrderCancelFieldFix".Translate() : "PhobosVehicleSystems_RequestFieldFix".Translate();
            options.Add(new FloatMenuOption(label, () => Order(comp, "field-fix", () => comp.Repairs.SetFieldFixRequested(!comp.Repairs.FieldFixRequested))));
        }

        // A jump-start, from the battery or the starter: sent at once where the vehicle needs one, else greyed out with why not.
        if (node.role is BatteryRole or StarterRole && comp.Vehicle.Spawned)
        {
            options.Add(comp.JumpStartNeed() != null
                ? new FloatMenuOption("PhobosVehicleSystems_JumpStartButton".Translate(), () => Order(comp, "jump-start", () => Refuse(JumpStartSend.OrderNearest(comp.Vehicle))))
                : Disabled("PhobosVehicleSystems_JumpStartButton".Translate(), "PhobosVehicleSystems_OrderNoJumpStart".Translate()));
        }

        // Fittings for a part: the window, where a dyno bay is in reach (or the bay rule is off), else greyed out with why not.
        if (view.Part != null && comp.Fittings.AnythingToFit)
        {
            options.Add(comp.Fittings.AtRigShown
                ? new FloatMenuOption("PhobosVehicleSystems_FitGizmo".Translate(), () => Order(comp, "fittings", () => Find.WindowStack.Add(new Dialog_PhobosVehicleFittings(comp))))
                : Disabled("PhobosVehicleSystems_FitGizmo".Translate(), "PhobosVehicleSystems_OrderNeedsRig".Translate()));
        }

        // A diagnosis, once the colony can build a dyno bay to do it at.
        if (VehicleFittings.RigReach > 0)
        {
            string label = comp.Diagnostics.Requested ? "PhobosVehicleSystems_OrderCancelDiagnose".Translate() : "PhobosVehicleSystems_Diagnose".Translate();
            options.Add(new FloatMenuOption(label, () => Order(comp, "diagnose", () => comp.Diagnostics.SetRequested(!comp.Diagnostics.Requested))));
        }
        return options;
    }

    /// <summary>Gives an order, and notes it as seen by its kind.</summary>
    static void Order(CompPhobosVehicleSystems comp, string kind, System.Action give)
    {
        give();
        if (Seen.Wants("vehicles.schematic.order", comp.Vehicle, kind)) Seen.Note("vehicles.schematic.order", comp.Vehicle, null, kind);
    }

    /// <summary>A refusal said, where the service gave one.</summary>
    static void Refuse(string? why)
    {
        if (why != null) Messages.Message(why, MessageTypeDefOf.RejectInput, historical: false);
    }

    static FloatMenuOption Disabled(string label, string why) => new(label + " (" + why + ")", null);
}
