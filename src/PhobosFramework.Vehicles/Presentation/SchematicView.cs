using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Interface;
using Phobos.Framework.Rules;
using Phobos.Framework.Vehicles.Rules;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// What a schematic page shows of one vehicle at one moment (Framework 0.363.0; docs/development/vehicle-schematic.md, SC6): each node's
/// tone, reading and level, and whether each pipe flows. Built from the vehicle's kept figures and never saved; the window builds it again
/// when the figures can have moved (every <see cref="RebuildTicks"/>, or when a part's health changes), so drawing reads only this.
/// </summary>
public sealed class SchematicView
{
    /// <summary>How often the view is built again while the game runs: the systems update every 250 ticks, the engine every 60.</summary>
    public const int RebuildTicks = 60;

    public sealed class NodeView
    {
        public bool Shown;
        public SchematicTone Tone = SchematicTone.Good;
        /// <summary>The reading's label and figure ("oil pressure: 92%"), empty for none.</summary>
        public string Reading = "";
        /// <summary>The figure alone, for under the node, empty for none.</summary>
        public string Figure = "";
        /// <summary>The reading's stage, empty when it has none.</summary>
        public string Stage = "";
        /// <summary>The worst health share of the role's parts; NaN for a node with no role.</summary>
        public float Health = float.NaN;
        /// <summary>How full a tank is, 0 to 1; NaN when it shows no level.</summary>
        public float Level = float.NaN;
        /// <summary>Whether its reading's stage stops what it drives (a red stage).</summary>
        public bool Stopped;
        /// <summary>The share of its work it does, from its role's parts; 1 with no role.</summary>
        public float Work = 1f;
        public SystemState? State;
        public VehicleComponent? Part;
    }

    public readonly PhobosVehicleSchematicDef Page;
    public readonly NodeView[] Nodes;
    /// <summary>Per pipe: drawn at all, and flowing now.</summary>
    public readonly (bool shown, bool flowing)[] Pipes;
    public readonly bool Running;
    public readonly int BuiltAt;
    public readonly int HealthVersion;
    /// <summary>The engine its per-engine nodes show, from 0 (Framework 0.373.0, the engine tabs).</summary>
    public readonly int Engine;
    /// <summary>For the heat gradient (Framework 0.378.0, SC8): the outside air, the turbine's temperature as shown (NaN without one), and the
    /// engine's top limit (NaN without one).</summary>
    public float Ambient, TurbineTemperature = float.NaN, TurbineLimit = float.NaN;
    const string TurbineTemperatureQuantity = "turbineTemperature";

    SchematicView(PhobosVehicleSchematicDef page, int builtAt, int healthVersion, bool running, int engine)
    {
        Page = page;
        Engine = engine;
        Nodes = page.nodes.Select(_ => new NodeView()).ToArray();
        Pipes = new (bool, bool)[page.pipes.Count];
        BuiltAt = builtAt;
        HealthVersion = healthVersion;
        Running = running;
    }

    /// <summary>Whether this view no longer says what the vehicle does: time has run on, or a part's health changed.</summary>
    public bool Stale(CompPhobosVehicleSystems comp, int now) => now >= BuiltAt + RebuildTicks || now < BuiltAt || comp.HealthViewVersion != HealthVersion;

    /// <summary>The view of a page, with its per-engine nodes showing engine <paramref name="engine"/> (from 0).</summary>
    public static SchematicView Build(CompPhobosVehicleSystems comp, PhobosVehicleSchematicDef page, int engine = 0)
    {
        var view = new SchematicView(page, Find.TickManager?.TicksGame ?? 0, comp.HealthViewVersion, comp.EngineRunning, engine);
        for (int i = 0; i < page.nodes.Count; i++) Fill(comp, page.nodes[i], view.Nodes[i], engine);
        view.Ambient = comp.Ambient;
        if (comp.TryGetQuantity(TurbineTemperatureQuantity, out float turbine)) view.TurbineTemperature = turbine;
        if (comp.Powertrain?.Turbine is { } spec) view.TurbineLimit = (float)spec.Top.TemperatureLimit;
        for (int i = 0; i < page.pipes.Count; i++)
        {
            SchematicPipe pipe = page.pipes[i];
            NodeView? from = view.NodeOf(pipe.from), to = pipe.to.NullOrEmpty() ? null : view.NodeOf(pipe.to);
            NodeView? without = pipe.onlyWithout.NullOrEmpty() ? null : view.NodeOf(pipe.onlyWithout);
            bool shown = from is { Shown: true } && (pipe.to.NullOrEmpty() || to is { Shown: true }) && without is not { Shown: true };
            NodeView? driver = view.NodeOf(pipe.Driver);
            bool flowing = shown && pipe.flow == SchematicFlow.EngineRunning && driver != null
                && SchematicRule.Flows(view.Running, driver.Work, driver.Stopped);
            view.Pipes[i] = (shown, flowing);
        }
        return view;
    }

    public NodeView? NodeOf(string? key)
    {
        if (key.NullOrEmpty()) return null;
        for (int i = 0; i < Page.nodes.Count; i++)
        {
            if (Page.nodes[i].key == key) return Nodes[i];
        }
        return null;
    }

    /// <summary>The role a node stands for on this vehicle: for a per-engine node, the chosen engine's own (Engine2, FuelPump2), or null
    /// where the vehicle has no such engine.</summary>
    static string? RoleOf(CompPhobosVehicleSystems comp, SchematicNode node, int engine)
    {
        if (node.role.NullOrEmpty() || !node.perEngine) return node.role;
        IReadOnlyList<string> engines = EngineFigures.Roles(comp);
        return engine >= 0 && engine < engines.Count ? EnginePumps.RoleFor(node.role!, engines[engine]) : null;
    }

    static void Fill(CompPhobosVehicleSystems comp, SchematicNode node, NodeView view, int engine)
    {
        if (node.system != null)
        {
            view.State = comp.States.FirstOrDefault(s => s.Def == node.system);
            if (view.State == null || !comp.IsEnabled(view.State)) return;
            if (node.shownWhen != null && !node.shownWhen.Holds(new SystemContext(comp, view.State))) return;
        }
        string? role = RoleOf(comp, node, engine);
        if (!node.role.NullOrEmpty())
        {
            if (role == null) return;
            List<VehicleComponent> parts = comp.PartsOf(role).ToList();
            if (parts.Count == 0) return;
            view.Part = parts.OrderBy(p => p.HealthPercent).First();
            view.Health = view.Part.HealthPercent;
            view.Work = comp.PartEfficiency(role, 1f);
        }
        view.Shown = true;
        SchematicTone tone = SchematicTone.Good;
        if (!float.IsNaN(view.Health)) tone = SchematicRule.FromHealth(view.Health);

        int q = view.State != null && !node.quantity.NullOrEmpty() ? System.Array.FindIndex(view.State.Def.quantities.ToArray(), x => x.name == node.quantity) : -1;
        if (view.State != null && q >= 0)
        {
            // Framework 0.373.0: a per-engine node reads the chosen engine's own figure where each keeps one, and its stage from that figure.
            float figure = 0f;
            bool own = node.perEngine && EngineFigures.TryRead(comp, node.quantity!, engine, out figure);
            GaugeReading reading = own ? GaugeReading.For(view.State, q, figure) : GaugeReading.For(view.State, q);
            view.Figure = reading.Figure;
            view.Reading = reading.Label.CapitalizeFirst() + ": " + reading.Figure;
            StageSpec? stage = own ? StageAt(view.State.Def.quantities[q], figure) : view.State.CurrentStage(q);
            if (stage != null)
            {
                view.Stage = stage.label;
                // Only a warning stage counts: a quiet one may still carry harm (a cold engine wears while it runs) without stopping a flow.
                if (stage.notable)
                {
                    view.Stopped = stage.Severe;
                    tone = SchematicRule.Worst(tone, stage.Severe ? SchematicTone.Bad : SchematicTone.Caution);
                }
            }
            if (node.shape == SchematicShape.Tank) view.Level = reading.Fill;
        }
        if (node.fuelLevel && comp.Vehicle.CompFueledTravel is { } tank && tank.FuelCapacity > 0f)
        {
            view.Level = tank.Fuel / tank.FuelCapacity;
            view.Figure = view.Level.ToStringPercent();
            view.Reading = "PhobosVehicleSystems_SchematicFuel".Translate(view.Figure.Named("SHARE")).Resolve();
            if (tank.EmptyTank) tone = SchematicRule.Worst(tone, SchematicTone.Bad);
        }
        view.Tone = tone;
    }

    /// <summary>The stage a figure falls in on a quantity's scale, or null for none.</summary>
    static StageSpec? StageAt(QuantitySpec quantity, float figure)
    {
        if (quantity.stages.Count == 0) return null;
        StageBounds bounds = StageBounds.Of(quantity);
        int index = StageRule.StageIndex(figure, bounds.from, bounds.to);
        return index >= 0 && index < quantity.stages.Count ? quantity.stages[index] : null;
    }

    /// <summary>A tone's colour, in the colours every Phobos tab uses.</summary>
    public static UnityEngine.Color Colour(SchematicTone tone) => tone switch
    {
        SchematicTone.Bad => PhobosWidgets.Bad,
        SchematicTone.Caution => PhobosWidgets.Caution,
        SchematicTone.Good => PhobosWidgets.Good,
        _ => PhobosWidgets.Off,
    };
}
