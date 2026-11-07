using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using Phobos.Framework.Debug;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>Dev-mode actions on the selected vehicles, under the one D'thorga category.</summary>
public static class VehicleSystemsDebugActions
{
    static IEnumerable<CompPhobosVehicleSystems> Selected() =>
        Find.Selector.SelectedObjects.OfType<VehiclePawn>().Select(v => v.GetComp<CompPhobosVehicleSystems>()).Where(c => c != null && c.Usable);

    [DebugAction(DebugCategory.Name, "Vehicle systems: log selected", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void LogSelected()
    {
        foreach (CompPhobosVehicleSystems comp in Selected())
        {
            comp.UpdateNow();
            Log.Message(VehicleSystemsMod.LogPrefix + " " + comp.DebugDescription());
        }
    }

    [DebugAction(DebugCategory.Name, "Vehicle systems: set a quantity", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static List<DebugActionNode> SetQuantity()
    {
        var nodes = new List<DebugActionNode>();
        CompPhobosVehicleSystems? first = Selected().FirstOrDefault();
        if (first == null) return nodes;
        foreach (SystemState state in first.States)
        {
            foreach (QuantitySpec quantity in state.Def.quantities.Where(q => !q.Derived))
            {
                var node = new DebugActionNode(state.Def.defName + " / " + quantity.name);
                foreach (float share in new[] { 0f, 0.25f, 0.5f, 0.75f, 1f })
                {
                    float value = quantity.min + (quantity.max - quantity.min) * share;
                    string name = quantity.name;
                    node.AddChild(new DebugActionNode(value.ToString("0.##"), DebugActionType.Action, () =>
                    {
                        foreach (CompPhobosVehicleSystems comp in Selected())
                        {
                            comp.SetQuantity(name, value);
                            comp.UpdateNow();
                        }
                    }));
                }
                nodes.Add(node);
            }
        }
        return nodes;
    }

    [DebugAction(DebugCategory.Name, "Vehicle systems: request a service", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void RequestService()
    {
        foreach (CompPhobosVehicleSystems comp in Selected()) comp.SetServiceRequested(true);
    }

    [DebugAction(DebugCategory.Name, "Vehicle systems: remove from this colony", allowedGameStates = AllowedGameStates.Playing)]
    static void Sweep()
    {
        int changed = VehicleSystems.Sweep();
        Log.Message(VehicleSystemsMod.LogPrefix + " Swept the systems' parts from " + changed + " vehicles; save now before removing the mod.");
    }
}
