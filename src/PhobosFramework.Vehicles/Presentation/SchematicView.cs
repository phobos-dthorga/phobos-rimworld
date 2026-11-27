using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Interface;
using Phobos.Framework.Rules;
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

    SchematicView(PhobosVehicleSchematicDef page, int builtAt, int healthVersion, bool running)
    {
        Page = page;
        Nodes = page.nodes.Select(_ => new NodeView()).ToArray();
        Pipes = new (bool, bool)[page.pipes.Count];
        BuiltAt = builtAt;
        HealthVersion = healthVersion;
        Running = running;
    }

    /// <summary>Whether this view no longer says what the vehicle does: time has run on, or a part's health changed.</summary>
    public bool Stale(CompPhobosVehicleSystems comp, int now) => now >= BuiltAt + RebuildTicks || now < BuiltAt || comp.HealthViewVersion != HealthVersion;

    public static SchematicView Build(CompPhobosVehicleSystems comp, PhobosVehicleSchematicDef page)
    {
        var view = new SchematicView(page, Find.TickManager?.TicksGame ?? 0, comp.HealthViewVersion, comp.EngineRunning);
        for (int i = 0; i < page.nodes.Count; i++) Fill(comp, page.nodes[i], view.Nodes[i]);
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

    static void Fill(CompPhobosVehicleSystems comp, SchematicNode node, NodeView view)
    {
        if (node.system != null)
        {
            view.State = comp.States.FirstOrDefault(s => s.Def == node.system);
            if (view.State == null || !comp.IsEnabled(view.State)) return;
        }
        List<VehicleComponent>? parts = null;
        if (!node.role.NullOrEmpty())
        {
            parts = comp.PartsOf(node.role!).ToList();
            if (parts.Count == 0) return;
            view.Part = parts.OrderBy(p => p.HealthPercent).First();
            view.Health = view.Part.HealthPercent;
            view.Work = comp.PartEfficiency(node.role!, 1f);
        }
        view.Shown = true;
        SchematicTone tone = SchematicTone.Good;
        if (!float.IsNaN(view.Health)) tone = SchematicRule.FromHealth(view.Health);

        int q = view.State != null && !node.quantity.NullOrEmpty() ? System.Array.FindIndex(view.State.Def.quantities.ToArray(), x => x.name == node.quantity) : -1;
        if (view.State != null && q >= 0)
        {
            GaugeReading reading = GaugeReading.For(view.State, q);
            view.Figure = reading.Figure;
            view.Reading = reading.Label.CapitalizeFirst() + ": " + reading.Figure;
            StageSpec? stage = view.State.CurrentStage(q);
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

    /// <summary>A tone's colour, in the colours every Phobos tab uses.</summary>
    public static UnityEngine.Color Colour(SchematicTone tone) => tone switch
    {
        SchematicTone.Bad => PhobosWidgets.Bad,
        SchematicTone.Caution => PhobosWidgets.Caution,
        SchematicTone.Good => PhobosWidgets.Good,
        _ => PhobosWidgets.Off,
    };
}
