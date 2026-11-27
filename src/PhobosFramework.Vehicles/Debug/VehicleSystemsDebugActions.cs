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

    /// <summary>Select vehicles with systems: updates them once and logs each one's systems, quantities and stages.</summary>
    [DebugAction(DebugCategory.Name, "Vehicle systems: log selected", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void LogSelected()
    {
        foreach (CompPhobosVehicleSystems comp in Selected())
        {
            comp.UpdateNow();
            Log.Message(VehicleSystemsMod.LogPrefix + " " + comp.DebugDescription());
        }
    }

    /// <summary>A submersible with a crew at the clicked cell, for trying one out: a dive needs deep water, and a design may not be buildable.</summary>
    [DebugAction(DebugCategory.Name, "Vehicle systems: spawn a submersible", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void SpawnSubmersible() =>
        DebugChoices.Open(DefDatabase<VehicleDef>.AllDefs.Where(d => d.GetCompProperties<CompProperties_PhobosSubmersible>() != null)
            .Select(def => DebugChoices.Choice(def.defName, () => DebugTools.curTool = new DebugTool("Spawn " + def.defName,
                () => VehicleSpawner.SpawnVehicleRandomized(def, UI.MouseCell(), Find.CurrentMap, Faction.OfPlayer, Rot4.North, autoFill: true)))),
            "No submersible design is loaded.");

    const string SelectAVehicle = "Select a vehicle with vehicle systems first.";

    /// <summary>Leaves the selected submersibles a few minutes of reserve, so a forced return to the surface can be seen without waiting hours.</summary>
    [DebugAction(DebugCategory.Name, "Vehicle systems: nearly empty a submersible", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void NearlyEmptySubmersible()
    {
        const float AFewGameMinutes = 0.1f;
        foreach (VehiclePawn vehicle in Find.Selector.SelectedObjects.OfType<VehiclePawn>()) Submersibles.CompOf(vehicle)?.SetReserveForTesting(AFewGameMinutes);
    }

    /// <summary>Has watchers spot the selected submersibles at once, so the shape in the water and the shelling can be seen without waiting for a raid to notice.</summary>
    [DebugAction(DebugCategory.Name, "Vehicle systems: spot a submersible", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void SpotSubmersible()
    {
        foreach (VehiclePawn vehicle in Find.Selector.SelectedObjects.OfType<VehiclePawn>()) Submersibles.CompOf(vehicle)?.Spot();
    }

    /// <summary>Select vehicles with systems: pick one of the first vehicle's quantities and a value (its minimum, a quarter, half,
    /// three quarters or its maximum), and every selected vehicle is set to it, saved as play would leave it.</summary>
    [DebugAction(DebugCategory.Name, "Vehicle systems: set a quantity", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void SetQuantity()
    {
        CompPhobosVehicleSystems? first = Selected().FirstOrDefault();
        var choices = new List<FloatMenuOption>();
        if (first != null)
        {
            foreach (SystemState state in first.States)
            {
                foreach (QuantitySpec quantity in state.Def.quantities.Where(q => !q.Derived))
                {
                    QuantitySpec captured = quantity;
                    choices.Add(DebugChoices.Submenu(state.Def.defName + " / " + quantity.name, () => new[] { 0f, 0.25f, 0.5f, 0.75f, 1f }.Select(share =>
                    {
                        float value = captured.min + (captured.max - captured.min) * share;
                        return DebugChoices.Choice(value.ToString("0.##"), () =>
                        {
                            foreach (CompPhobosVehicleSystems comp in Selected())
                            {
                                comp.SetQuantity(captured.name, value);
                                comp.UpdateNow();
                            }
                        });
                    }), SelectAVehicle));
                }
            }
        }
        DebugChoices.Open(choices, SelectAVehicle);
    }

    /// <summary>Gives the selected vehicles' part of a role a condition at a severity, so each stage can be seen without arranging a
    /// hit or a cause; a severity of 0 removes it.</summary>
    [DebugAction(DebugCategory.Name, "Vehicle systems: give a part condition", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void GiveCondition()
    {
        CompPhobosVehicleSystems? first = Selected().FirstOrDefault();
        var choices = new List<FloatMenuOption>();
        if (first != null)
        {
            List<string> roles = first.Props.roleKeys.Keys.ToList();
            foreach (PhobosVehicleConditionDef def in DefDatabase<PhobosVehicleConditionDef>.AllDefsListForReading)
            {
                PhobosVehicleConditionDef capturedDef = def;
                choices.Add(DebugChoices.Submenu(def.defName, () => roles.Select(role => DebugChoices.Submenu(role, () =>
                    new[] { 0f, 0.2f, 0.5f, 0.8f, 1f }.Select(severity => DebugChoices.Choice(severity.ToString("0.#"), () =>
                    {
                        foreach (CompPhobosVehicleSystems comp in Selected())
                        {
                            Log.Message(VehicleSystemsMod.LogPrefix + " " + comp.DebugSetCondition(role, capturedDef, severity));
                        }
                    })), SelectAVehicle)), "The selected vehicle has no parts with roles."));
            }
        }
        DebugChoices.Open(choices, SelectAVehicle);
    }

    /// <summary>Asks for a service on the selected vehicles with systems, as the player's own request does, so every system with a
    /// service comes due.</summary>
    [DebugAction(DebugCategory.Name, "Vehicle systems: request a service", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void RequestService()
    {
        foreach (CompPhobosVehicleSystems comp in Selected()) comp.SetServiceRequested(true);
    }

    /// <summary>The uninstall sweep, which cannot be undone: cuts every vehicle's parts back to its design's own, forgets the systems'
    /// state and stops the service for the session. Save straight after, before removing the mod.</summary>
    [DebugAction(DebugCategory.Name, "Vehicle systems: remove from this colony", allowedGameStates = AllowedGameStates.Playing)]
    static void Sweep()
    {
        int changed = VehicleSystems.Sweep();
        Log.Message(VehicleSystemsMod.LogPrefix + " Swept the systems' parts from " + changed + " vehicles; save now before removing the mod.");
    }

    const float DebugHitDamage = 20f;
    const float DebugHitArmourPenetration = 1f;

    /// <summary>
    /// A 20-point hit on a role's first part, two ways. Straight onto the part (its armour skipped) always takes health off, so the
    /// part-hit rules can be seen every time. As a shot, it goes through Vehicle Framework's damage path at one of the part's cells, the
    /// path Combat Extended and the helicopter mods patch, where outer parts and armour may take it first. Each hit says what it did,
    /// including when the part was untouched. The cells are the part's built hit box (<c>Hitbox</c>), not the Def's <c>cells</c> list,
    /// which is empty for a part placed by side or by corners: reading that list made every hit skip silently (owner, 10 Nov 2026).
    /// </summary>
    [DebugAction(DebugCategory.Name, "Vehicle systems: hit a part", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void HitPart()
    {
        CompPhobosVehicleSystems? first = Selected().FirstOrDefault();
        if (first == null)
        {
            DebugChoices.Open(Enumerable.Empty<FloatMenuOption>(), SelectAVehicle);
            return;
        }
        DebugChoices.Open(first.Props.roleKeys.Keys.ToList().Select(role => DebugChoices.Submenu(role, () => new[]
        {
            DebugChoices.Choice("straight onto the part", () => HitRole(role, asShot: false)),
            DebugChoices.Choice("as a shot at its cells (armour may stop it)", () => HitRole(role, asShot: true)),
        }, SelectAVehicle)), "The selected vehicle has no parts with roles.");
    }

    static void HitRole(string role, bool asShot)
    {
        List<string> lines = Selected().Select(comp => HitOne(comp, role, asShot)).ToList();
        string text = lines.Count > 0 ? string.Join("\n", lines) : SelectAVehicle;
        Messages.Message(text, MessageTypeDefOf.NeutralEvent, historical: false);
        Log.Message(VehicleSystemsMod.LogPrefix + " " + text);
    }

    static string HitOne(CompPhobosVehicleSystems comp, string role, bool asShot)
    {
        VehiclePawn vehicle = comp.Vehicle;
        if (!comp.Props.roleKeys.TryGetValue(role, out List<string>? keys) || keys.Count == 0) return "The " + vehicle.LabelShort + " has no part for " + role + ".";
        VehicleComponent? part = vehicle.statHandler.GetComponent(keys[0]);
        if (part == null) return "The " + vehicle.LabelShort + " has no part " + keys[0] + ".";
        float before = part.Health;
        if (before <= 0f) return role + " (" + keys[0] + ") on the " + vehicle.LabelShort + " is already destroyed.";
        var dinfo = new DamageInfo(DamageDefOf.Bullet, DebugHitDamage, DebugHitArmourPenetration, instigator: vehicle);
        if (asShot)
        {
            List<IntVec2>? cells = part.props.hitbox?.Hitbox;
            if (cells == null || cells.Count == 0) return role + " (" + keys[0] + ") on the " + vehicle.LabelShort + " has no hit cells to aim at.";
            vehicle.statHandler.TakeDamage(dinfo, cells.RandomElement());
        }
        else
        {
            part.TakeDamage(vehicle, dinfo, ignoreArmor: true);
        }
        comp.UpdateNow();
        string result = role + " (" + keys[0] + ") on the " + vehicle.LabelShort + ": health " + before.ToString("0.#") + " to "
                        + part.Health.ToString("0.#") + " of " + part.MaxHealth.ToString("0.#");
        if (part.Health >= before) result += asShot ? ", untouched: outer parts or armour took the shot" : ", untouched";
        return result;
    }

    /// <summary>Select vehicles with systems: logs the part-health memory beside each part's health now. Reads only.</summary>
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

    /// <summary>Select vehicles with systems: pick one of the first vehicle's switchable electrical loads, and it is switched on or off
    /// on every selected vehicle, saved as the player's switch is; the log says whether it is now active.</summary>
    [DebugAction(DebugCategory.Name, "Vehicle systems: toggle a load", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void ToggleLoad()
    {
        CompPhobosVehicleSystems? first = Selected().FirstOrDefault();
        var choices = new List<FloatMenuOption>();
        foreach (ElectricalLoadSpec load in first?.Props.loadout?.loads.Where(l => l.toggle) ?? Enumerable.Empty<ElectricalLoadSpec>())
        {
            ElectricalLoadSpec captured = load;
            choices.Add(DebugChoices.Choice(load.name, () =>
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
        DebugChoices.Open(choices, first == null ? SelectAVehicle : "The selected vehicle has no switchable loads.");
    }

    /// <summary>Spawns a jump-start kit at each selected thing's cell on the map.</summary>
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

    /// <summary>Select vehicles: cracks their glass (half its health) or breaks it out, through the same damage play uses, and logs
    /// what happened. With the glass setting off nothing breaks; a vehicle without glass says so.</summary>
    [DebugAction(DebugCategory.Name, "Vehicle systems: crack or break the glass of selected vehicles", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static List<DebugActionNode> DamageGlass()
    {
        var nodes = new List<DebugActionNode>();
        foreach ((string name, float share) in new (string, float)[] { ("crack it (half gone)", 0.5f), ("break it out", 1f) })
        {
            float captured = share;
            nodes.Add(new DebugActionNode(name, DebugActionType.Action, () =>
            {
                foreach (CompPhobosVehicleSystems comp in Selected())
                {
                    if (Glass.PartOf(comp.Vehicle) is not { } glass)
                    {
                        Log.Message(VehicleSystemsMod.LogPrefix + " " + comp.Vehicle.LabelShort + " has no glass (an open cab, a trailer, or a design marked without).");
                        continue;
                    }
                    Glass.Damage(comp.Vehicle, glass.MaxHealth * captured, "a dev action");
                    Log.Message(VehicleSystemsMod.LogPrefix + " " + comp.Vehicle.LabelShort + ": glass " + Glass.KindOf(comp.Vehicle) + ", now "
                        + Glass.StateOf(glass) + " (" + glass.Health.ToString("0") + "/" + glass.MaxHealth.ToString("0") + ")"
                        + (VehicleSystemsMod.Settings.vehicleGlass ? "" : "; the glass setting is off, so nothing broke."));
                }
            }));
        }
        return nodes;
    }

    /// <summary>Spawns a booster pack at each selected thing's cell on the map: full, with one start left, or flat.</summary>
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

    /// <summary>Select vehicles with systems: logs the cab's and the outside's temperature and, for each person aboard, their
    /// comfortable range and the mood stage the cab gives. Reads only.</summary>
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

    /// <summary>Select vehicles with systems: logs each one's charge, whether it is charging, its charge factor and its battery pack's
    /// temperature and wear. Reads only.</summary>
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

    /// <summary>Empties the battery of the selected vehicles with systems (its charge and its starting power), saved as play would
    /// leave it, for trying a jump start or a flat battery.</summary>
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

    /// <summary>On the world map, select vehicle caravans: logs whether each moves or rests and, for each vehicle, its engine, speed
    /// cap, cab, the ground as the drivetrain last read it, cab comfort and its systems. Reads only.</summary>
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

    /// <summary>On the world map, select vehicle caravans: forces their vehicles' speed cap to half or a crawl, or clears it, to check
    /// the caravan slowing (not saved: a load clears it).</summary>
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

    /// <summary>Logs how long the vehicle model's work has taken (counted only while dev mode is on) and how many vehicles run in full,
    /// light or no detail. Reads only.</summary>
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

    /// <summary>Clears the vehicle model's timing counts, so the next timing report starts from now.</summary>
    [DebugAction(DebugCategory.Name, "Vehicle systems: reset timing", allowedGameStates = AllowedGameStates.Playing)]
    static void ResetTiming() => PhobosTiming.Reset();

    /// <summary>Select vehicles with systems: logs each one's detail tier (full, light or off) with its faction and whether it is
    /// spawned. Reads only.</summary>
    [DebugAction(DebugCategory.Name, "Vehicle systems: log tier of selected", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void LogTier()
    {
        foreach (CompPhobosVehicleSystems comp in Selected())
        {
            Log.Message(VehicleSystemsMod.LogPrefix + " " + comp.Vehicle.LabelShort + ": tier " + comp.Tier + " (faction "
                + (comp.Vehicle.Faction?.Name ?? "none") + ", spawned " + comp.Vehicle.Spawned + ").");
        }
    }

    /// <summary>Replaces the selected vehicles' saved graph history with a day of a test curve between each quantity's limits, for
    /// checking the graphs without waiting a day.</summary>
    [DebugAction(DebugCategory.Name, "Vehicle systems: fill history with a test curve", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void FillHistory()
    {
        foreach (CompPhobosVehicleSystems comp in Selected()) comp.History.DebugFill(comp.States);
    }

    /// <summary>Opens the Dashboard tab of the selected vehicle.</summary>
    [DebugAction(DebugCategory.Name, "Vehicle systems: open the Dashboard tab", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void OpenDashboard() => InspectPaneUtility.OpenTab(typeof(ITab_VehicleDashboard));

    /// <summary>Opens the Systems tab of the selected vehicle.</summary>
    [DebugAction(DebugCategory.Name, "Vehicle systems: open the Systems tab", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void OpenSystems() => InspectPaneUtility.OpenTab(typeof(ITab_VehicleSystems));

    /// <summary>Shows the screen guard at work without a real fault (Framework 0.94.0): the Systems tab rests for a few seconds with its
    /// fallback line, and the log gains one error, as it would if the tab had thrown.</summary>
    [DebugAction(DebugCategory.Name, "Vehicle systems: test the screen guard on the Systems tab", allowedGameStates = AllowedGameStates.Playing)]
    static void TestScreenGuard()
    {
        Phobos.Framework.Interface.UiGuard.Report(ITab_VehicleSystems.GuardKey, new System.InvalidOperationException("A test fault, asked for by the dev action. Nothing is wrong."));
        (int screens, int faults) = Phobos.Framework.Interface.UiGuard.Tally();
        Log.Message(VehicleSystemsMod.LogPrefix + " Screen guard: " + screens + " screen(s) have failed " + faults + " time(s) this session. Open the Systems tab now: it shows one line for "
                    + Phobos.Framework.Interface.UiGuard.RestSeconds + " seconds, then draws again.");
    }

    /// <summary>Logs how many vehicles the Systems tab and the Health tab hold state for. Reads only.</summary>
    [DebugAction(DebugCategory.Name, "Vehicle systems: log tab state", allowedGameStates = AllowedGameStates.Playing)]
    static void LogTabState() => Log.Message(VehicleSystemsMod.LogPrefix + " Systems tab holds state for " + ITab_VehicleSystems.StateCount
        + " vehicles; Health tab holds views for " + ITab_PhobosVehicleHealth.ViewCount + ".");

    /// <summary>Opens D'thorga Framework's Health tab of the selected vehicle.</summary>
    [DebugAction(DebugCategory.Name, "Vehicle systems: open the Health tab", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    static void OpenHealth() => InspectPaneUtility.OpenTab(typeof(ITab_PhobosVehicleHealth));

    /// <summary>Throws away the Health tab's built views, so each is built again from the vehicle the next time it is drawn.</summary>
    [DebugAction(DebugCategory.Name, "Vehicle systems: rebuild the health view", allowedGameStates = AllowedGameStates.Playing)]
    static void RebuildHealthView() => ITab_PhobosVehicleHealth.Invalidate();
}
