using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using Phobos.Framework.Debug;
using RimWorld;
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

    /// <summary>A 20-point hit on a role's first part through Vehicle Framework's own damage path (which Combat Extended prefixes),
    /// so the part-hit rules can be checked with and without CE.</summary>
    [DebugAction(DebugCategory.Name, "Vehicle systems: hit a part", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static List<DebugActionNode> HitPart()
    {
        var nodes = new List<DebugActionNode>();
        CompPhobosVehicleSystems? first = Selected().FirstOrDefault();
        if (first == null) return nodes;
        foreach (string role in first.Props.roleKeys.Keys)
        {
            string captured = role;
            nodes.Add(new DebugActionNode(role, DebugActionType.Action, () =>
            {
                foreach (CompPhobosVehicleSystems comp in Selected())
                {
                    if (!comp.Props.roleKeys.TryGetValue(captured, out List<string>? keys) || keys.Count == 0) continue;
                    VehicleComponent? part = comp.Vehicle.statHandler.GetComponent(keys[0]);
                    List<IntVec2>? cells = part?.props.hitbox?.cells;
                    if (part == null || cells == null || cells.Count == 0) continue;
                    comp.Vehicle.statHandler.TakeDamage(new DamageInfo(DamageDefOf.Bullet, 20f, 1f, instigator: comp.Vehicle), cells[0]);
                    comp.UpdateNow();
                    Log.Message(VehicleSystemsMod.LogPrefix + " Hit " + captured + " (" + keys[0] + ") on the " + comp.Vehicle.LabelShort + ": health now " + part.Health.ToString("0.#"));
                }
            }));
        }
        return nodes;
    }

    [DebugAction(DebugCategory.Name, "Vehicle systems: log part health memory", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void LogPartMemory()
    {
        foreach (CompPhobosVehicleSystems comp in Selected())
        {
            Log.Message(VehicleSystemsMod.LogPrefix + " " + comp.Vehicle.LabelShort + "\n" + comp.DebugPartMemory());
        }
    }

    /// <summary>Runs the first explosion effect among the selected vehicle's systems, whatever the player's setting: destructive.</summary>
    [DebugAction(DebugCategory.Name, "Vehicle systems: explode the boiler now", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void ExplodeNow()
    {
        foreach (CompPhobosVehicleSystems comp in Selected())
        {
            CompPhobosVehicleSystems captured = comp;
            (SystemState state, Effect_Explosion effect)? found = null;
            foreach (SystemState state in comp.States)
            {
                foreach (QuantitySpec quantity in state.Def.quantities)
                {
                    foreach (StageSpec stage in quantity.stages)
                    {
                        Effect_Explosion? explosion = stage.effects.OfType<Effect_Explosion>().FirstOrDefault();
                        if (explosion != null && found == null) found = (state, explosion);
                    }
                }
            }
            if (found == null)
            {
                Log.Message(VehicleSystemsMod.LogPrefix + " The " + comp.Vehicle.LabelShort + " has no system that can explode.");
                continue;
            }
            (SystemState foundState, Effect_Explosion foundEffect) = found.Value;
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                "Explode the " + comp.Vehicle.LabelShort + "'s boiler now? This wrecks it and hurts everything near it.",
                () => foundEffect.OnEnter(new SystemContext(captured, foundState)), destructive: true));
        }
    }
}
