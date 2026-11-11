using System.Collections.Generic;
using System.Linq;
using System.Text;
using Phobos.Framework.Bills;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>The parts one upgrade-tree node asks for, by the node's key. The node's own ingredients in XML are its shell.</summary>
public class NodeBill
{
    public string key = "";
    public List<BillPart> parts = new();

    [Unsaved]
    public List<ThingDefCountClass>? shell;
    [Unsaved]
    public List<BillRow> rows = new();
}

/// <summary>
/// A vehicle's parts bill (docs/development/parts-bills.md; Framework 0.95.0): the core's bill, aimed at the vehicle's blueprint, with
/// what only a vehicle has. Its size is the vehicle's empty weight in kilograms unless the bill gives one, so a heavier vehicle takes a
/// heavier engine; and it may fill the ingredients of the vehicle's upgrade-tree nodes from the same roles. Node keys are only read.
/// </summary>
public class PhobosVehicleBillDef : PhobosBillDef
{
    public List<NodeBill> nodes = new();

    public VehicleDef? Vehicle => (target as VehicleBuildDef)?.thingToSpawn;

    UpgradeTreeDef? Tree => Vehicle?.GetCompProperties<CompProperties_UpgradeTree>()?.def;

    /// <summary>A node by its key, or null: Vehicle Framework's own lookup throws for a key it does not have.</summary>
    static UpgradeNode? NodeOf(UpgradeTreeDef? tree, string key) => tree?.nodes?.FirstOrDefault(n => n.key == key);

    public override double Size => !float.IsNaN(size) ? size : Vehicle is { } vehicle ? Powertrain.KerbFor(vehicle, null, null) : double.NaN;

    public override void Apply(bool othersAllowed)
    {
        base.Apply(othersAllowed);
        // A vehicle Def that carries a cost of its own (Vehicle Framework copies it to a blueprint it generates) says the same.
        if (Vehicle is { costList: not null } vehicle && target != null) vehicle.costList = PartsBills.Copy(target.costList);
        UpgradeTreeDef? tree = Tree;
        if (tree == null) return;
        double kilograms = Size;
        foreach (NodeBill bill in nodes)
        {
            UpgradeNode? node = NodeOf(tree, bill.key);
            if (node == null) continue;    // a config error names it
            bill.shell ??= PartsBills.Copy(node.ingredients);
            bill.rows.Clear();
            node.ingredients = PartsBills.Resolve(bill.shell, bill.parts, kilograms, tags, othersAllowed, bill.rows);
        }
    }

    public override void AppendToReport(StringBuilder text)
    {
        UpgradeTreeDef? tree = Tree;
        foreach (NodeBill bill in nodes)
        {
            text.Append("\n  node ").Append(bill.key).Append(": ").Append(PartsBills.Describe(NodeOf(tree, bill.key)?.ingredients));
            foreach (BillRow row in bill.rows) text.Append("\n    ").Append(PartsBills.Describe(row));
        }
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (target != null && target is not VehicleBuildDef) yield return "its target " + target.defName + " is not a vehicle's blueprint (a Vehicles.VehicleBuildDef)";
        UpgradeTreeDef? tree = Tree;
        foreach (NodeBill bill in nodes)
        {
            if (tree == null) yield return "lists the node " + bill.key + ", but the vehicle has no upgrade tree";
            else if (NodeOf(tree, bill.key) == null) yield return "lists the node " + bill.key + ", which " + tree.defName + " does not have";
            foreach (string error in PartErrors(bill.parts, "node " + bill.key + " ")) yield return error;
        }
    }
}
