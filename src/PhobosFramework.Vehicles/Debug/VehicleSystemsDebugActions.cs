using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using Phobos.Framework.Debug;
using Phobos.Framework.Items;
using Phobos.Framework.Rules;
using Phobos.Framework.Vehicles.Rules;
using Phobos.Framework.WorldMap;
using RimWorld;
using Vehicles;
using RimWorld.Planet;
using Vehicles.World;
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

    [DebugAction(DebugCategory.Name, "Vehicle systems: toggle a load", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static List<DebugActionNode> ToggleLoad()
    {
        var nodes = new List<DebugActionNode>();
        CompPhobosVehicleSystems? first = Selected().FirstOrDefault();
        if (first?.Props.loadout == null) return nodes;
        foreach (ElectricalLoadSpec load in first.Props.loadout.loads.Where(l => l.toggle))
        {
            ElectricalLoadSpec captured = load;
            nodes.Add(new DebugActionNode(load.name, DebugActionType.Action, () =>
            {
                foreach (CompPhobosVehicleSystems comp in Selected())
                {
                    comp.SetSwitch(captured, !comp.IsOn(captured));
                    comp.UpdateNow();
                    Log.Message(VehicleSystemsMod.LogPrefix + " " + comp.Vehicle.LabelShort + ": " + captured.name + (comp.IsOn(captured) ? " on" : " off")
                        + (comp.LoadActive(captured.name) ? ", active" : ", not active"));
                }
            }));
        }
        return nodes;
    }

    [DebugAction(DebugCategory.Name, "Vehicle systems: give a jump-start kit", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void GiveKit()
    {
        ThingDef? kit = JumpStarts.KitDef;
        if (kit == null) return;
        foreach (Thing selected in DebugSelection.SelectedThings<Thing>().Where(t => t.Spawned).ToList())
        {
            GenSpawn.Spawn(ThingMaker.MakeThing(kit), selected.Position, selected.Map);
        }
    }

    [DebugAction(DebugCategory.Name, "Vehicle systems: give a booster pack", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static List<DebugActionNode> GivePack()
    {
        var nodes = new List<DebugActionNode>();
        foreach ((string name, float? uses) in new (string, float?)[] { ("full", null), ("one start left", 1f), ("flat", 0f) })
        {
            float? captured = uses;
            nodes.Add(new DebugActionNode(name, DebugActionType.Action, () =>
            {
                ThingDef? def = JumpStarts.PackDef;
                if (def == null) return;
                foreach (Thing selected in DebugSelection.SelectedThings<Thing>().Where(t => t.Spawned).ToList())
                {
                    Thing pack = ThingMaker.MakeThing(def);
                    if (captured is { } set) pack.TryGetComp<CompPhobosCharges>()?.DebugSet(set);
                    GenSpawn.Spawn(pack, selected.Position, selected.Map);
                }
            }));
        }
        return nodes;
    }

    [DebugAction(DebugCategory.Name, "Vehicle systems: log cab comfort", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void LogCabComfort()
    {
        foreach (CompPhobosVehicleSystems comp in Selected()) Log.Message(VehicleSystemsMod.LogPrefix + " " + CabComfortLine(comp));
    }

    /// <summary>The cab, the outside and each person aboard with their comfort range and the stage the cab thought would take.</summary>
    static string CabComfortLine(CompPhobosVehicleSystems comp)
    {
        VehiclePawn vehicle = comp.Vehicle;
        if (!CabClimate.TryGet(vehicle, out float cab)) return vehicle.LabelShort + ": open cab, no cab moods";
        var text = new System.Text.StringBuilder(vehicle.LabelShort + ": cab " + cab.ToString("0.#") + " C, outside " + comp.Ambient.ToString("0.#")
            + " C, moods " + (VehicleSystemsMod.Settings.cabMoods ? "on" : "off (setting)"));
        foreach (Pawn pawn in vehicle.AllPawnsAboard)
        {
            float min = pawn.GetStatValue(StatDefOf.ComfyTemperatureMin);
            float max = pawn.GetStatValue(StatDefOf.ComfyTemperatureMax);
            text.Append("\n  ").Append(pawn.LabelShort).Append(": comfortable ").Append(min.ToString("0")).Append(" to ").Append(max.ToString("0"))
                .Append(" C, ").Append(ShelterComfort.Stage(cab, comp.Ambient, min, max));
        }
        return text.ToString();
    }

    [DebugAction(DebugCategory.Name, "Vehicle systems: log charging", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void LogCharging()
    {
        foreach (CompPhobosVehicleSystems comp in Selected())
        {
            CompFueledTravel? fuel = comp.Vehicle.CompFueledTravel;
            if (fuel == null) continue;
            comp.UpdateNow();
            string pack = comp.TryGetQuantity("packTemperature", out float temperature) ? temperature.ToString("0.#") + " C" : "none";
            string wear = comp.TryGetQuantity("packWear", out float worn) ? worn.ToStringPercent() : "none";
            Log.Message(VehicleSystemsMod.LogPrefix + " " + comp.Vehicle.LabelShort + ": electric " + fuel.Props.ElectricPowered + ", charge "
                + fuel.Fuel.ToString("0.#") + " of " + fuel.FuelCapacity.ToString("0") + ", charging " + fuel.Charging + ", charge factor "
                + comp.ChargeFactor.ToString("0.##") + ", pack " + pack + ", wear " + wear);
        }
    }

    [DebugAction(DebugCategory.Name, "Vehicle systems: flatten the battery", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void FlattenBattery()
    {
        foreach (CompPhobosVehicleSystems comp in Selected())
        {
            comp.SetQuantity("charge", 0f);
            comp.SetQuantity("startingPower", 0f);
            comp.UpdateNow();
        }
    }

    [DebugAction(DebugCategory.Name, "Vehicle systems: log caravan state", allowedGameStates = AllowedGameStates.PlayingOnWorld)]
    static void LogCaravanState()
    {
        foreach (VehicleCaravan caravan in DebugSelection.SelectedWorldObjects<VehicleCaravan>())
        {
            var text = new System.Text.StringBuilder(VehicleSystemsMod.LogPrefix + " " + caravan.LabelCap + ": moving " + caravan.vehiclePather.MovingNow
                + ", resting " + caravan.NightResting);
            foreach (VehiclePawn vehicle in caravan.VehiclesListForReading)
            {
                CompPhobosVehicleSystems? comp = vehicle.GetComp<CompPhobosVehicleSystems>();
                if (comp == null || !comp.Usable) continue;
                comp.UpdateNow();
                text.Append("\n").Append(vehicle.LabelShort).Append(": engine ").Append(comp.EngineRunning ? "running" : "off")
                    .Append(", cap ").Append(comp.SpeedCap.ToString("0.00")).Append(", refusal ").Append(comp.MoveRefusal ?? "none")
                    .Append(", cab ").Append(CabClimate.TryGet(vehicle, out float cab) ? cab.ToString("0.#") + " C" : "open")
                    .Append("\n").Append(GroundLine(comp.Powertrain))
                    .Append("\n").Append(CabComfortLine(comp))
                    .Append("\n").Append(comp.DebugDescription());
            }
            Log.Message(text.ToString());
        }
    }

    /// <summary>The caravan tile's ground as the drivetrain last read it: biome, typical terrain, road, snow and the path cost.</summary>
    static string GroundLine(Powertrain? train)
    {
        if (train?.WorldGroundRead is not { } ground) return "ground: not read yet (the caravan has not moved since load)";
        return "ground: " + ground.Biome.defName + ", typical terrain " + (BiomeGround.TypicalTerrain(ground.Biome)?.defName ?? "none") + " (path cost "
            + ground.TerrainPathCost + "), difficulty " + ground.BiomeDifficulty.ToString("0.#") + ", " + (ground.Road ? "road" : "no road") + ", winter "
            + ground.WinterOffset.ToString("0.##") + ", hills " + ground.Hilliness + " -> path cost " + train.WorldPathCost.ToString("0.#")
            + ", rolling " + DrivetrainModel.RollingResistanceForPathCost(train.WorldPathCost).ToString("0.###")
            + ", grip " + DrivetrainModel.GripForPathCost(train.WorldPathCost).ToString("0.##");
    }

    [DebugAction(DebugCategory.Name, "Vehicle systems: set caravan cap", allowedGameStates = AllowedGameStates.PlayingOnWorld)]
    static List<DebugActionNode> SetCaravanCap()
    {
        var nodes = new List<DebugActionNode>();
        foreach ((string name, double? cap) in new (string, double?)[] { ("half", 0.5), ("crawl", 0.15), ("clear", null) })
        {
            double? captured = cap;
            nodes.Add(new DebugActionNode(name, DebugActionType.Action, () =>
            {
                foreach (VehicleCaravan caravan in DebugSelection.SelectedWorldObjects<VehicleCaravan>())
                {
                    foreach (VehiclePawn vehicle in caravan.VehiclesListForReading)
                    {
                        Powertrain? train = vehicle.GetComp<CompPhobosVehicleSystems>()?.Powertrain;
                        if (train != null) train.DebugCapOverride = captured;
                    }
                    caravan.Notify_VehicleTeleported();
                }
            }));
        }
        return nodes;
    }

    [DebugAction(DebugCategory.Name, "Vehicle systems: timing report", allowedGameStates = AllowedGameStates.Playing)]
    static void TimingReport()
    {
        int full = 0, lite = 0, off = 0;
        foreach (VehiclePawn vehicle in VehicleSystems.AllVehicles())
        {
            CompPhobosVehicleSystems? comp = vehicle.GetComp<CompPhobosVehicleSystems>();
            if (comp == null || !comp.Usable) continue;
            switch (comp.Tier)
            {
                case DetailTier.Full: full++; break;
                case DetailTier.Lite: lite++; break;
                default: off++; break;
            }
        }
        Log.Message(VehicleSystemsMod.LogPrefix + " Timing (dev mode on while the work ran):\n" + PhobosTiming.Report()
            + "\nVehicles with systems: " + full + " full, " + lite + " lite, " + off + " off.");
    }

    [DebugAction(DebugCategory.Name, "Vehicle systems: reset timing", allowedGameStates = AllowedGameStates.Playing)]
    static void ResetTiming() => PhobosTiming.Reset();

    [DebugAction(DebugCategory.Name, "Vehicle systems: log tier of selected", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void LogTier()
    {
        foreach (CompPhobosVehicleSystems comp in Selected())
        {
            Log.Message(VehicleSystemsMod.LogPrefix + " " + comp.Vehicle.LabelShort + ": tier " + comp.Tier + " (faction "
                + (comp.Vehicle.Faction?.Name ?? "none") + ", spawned " + comp.Vehicle.Spawned + ").");
        }
    }

    [DebugAction(DebugCategory.Name, "Vehicle systems: fill history with a test curve", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void FillHistory()
    {
        foreach (CompPhobosVehicleSystems comp in Selected()) comp.History.DebugFill(comp.States);
    }

    [DebugAction(DebugCategory.Name, "Vehicle systems: open the Dashboard tab", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void OpenDashboard() => InspectPaneUtility.OpenTab(typeof(ITab_VehicleDashboard));

    [DebugAction(DebugCategory.Name, "Vehicle systems: open the Systems tab", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void OpenSystems() => InspectPaneUtility.OpenTab(typeof(ITab_VehicleSystems));

    [DebugAction(DebugCategory.Name, "Vehicle systems: log tab state", allowedGameStates = AllowedGameStates.Playing)]
    static void LogTabState() => Log.Message(VehicleSystemsMod.LogPrefix + " Systems tab holds state for " + ITab_VehicleSystems.StateCount
        + " vehicles; Health tab holds views for " + ITab_PhobosVehicleHealth.ViewCount + ".");

    [DebugAction(DebugCategory.Name, "Vehicle systems: open the Health tab", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void OpenHealth() => InspectPaneUtility.OpenTab(typeof(ITab_PhobosVehicleHealth));

    [DebugAction(DebugCategory.Name, "Vehicle systems: rebuild the health view", allowedGameStates = AllowedGameStates.Playing)]
    static void RebuildHealthView() => ITab_PhobosVehicleHealth.Invalidate();
}
