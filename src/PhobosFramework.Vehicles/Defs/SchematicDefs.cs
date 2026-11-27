using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Vehicles.Rules;
using UnityEngine;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>How a part is drawn on a schematic page.</summary>
public enum SchematicShape
{
    /// <summary>A block, filled in its tone: an engine, an alternator, a turbocharger, a starter.</summary>
    Box,
    /// <summary>A dark store filled to its level: a fuel tank, a sump, a battery.</summary>
    Tank,
    /// <summary>A pump: a block with an arrow through it.</summary>
    Pump,
    /// <summary>A block crossed by fins.</summary>
    Radiator,
    /// <summary>A block crossed by pleats.</summary>
    Filter,
}

/// <summary>What a pipe on a schematic carries, which sets its colour while something flows.</summary>
public enum SchematicMedium
{
    Fuel,
    Oil,
    Coolant,
    Air,
    Exhaust,
    Electric,
}

/// <summary>When a pipe carries something.</summary>
public enum SchematicFlow
{
    /// <summary>While the engine runs and the part driving the pipe works.</summary>
    EngineRunning,
    /// <summary>Never shown flowing: a line drawn to show what joins what, such as the battery's lead to the starter.</summary>
    Never,
}

/// <summary>One part on a schematic page.</summary>
public class SchematicNode
{
    /// <summary>The node's name on the page, which pipes use; unique within the page.</summary>
    public string key = "";
    [MustTranslate] public string label = "";
    public SchematicShape shape = SchematicShape.Box;
    /// <summary>Where it sits, in the page's drawing units: x and y of its top left corner, then its width and height.</summary>
    public Rect rect;
    /// <summary>The part role it stands for: its health colours the node, and pointing at it lights the part on the vehicle. Empty for
    /// none. Without the role on the vehicle the node, and every pipe to it, is left out.</summary>
    public string? role;
    /// <summary>The system it belongs to; a click opens the Systems tab there. Without that system on the vehicle, or with it switched
    /// off, the node and its pipes are left out.</summary>
    public PhobosVehicleSystemDef? system;
    /// <summary>The system's quantity it reads: its stage colours the node, and a tank fills to it.</summary>
    public string? quantity;
    /// <summary>A tank that shows the vehicle's own fuel tank, Vehicle Framework's, instead of a quantity.</summary>
    public bool fuelLevel;
    /// <summary>What a tank holds, for the colour it fills with.</summary>
    public SchematicMedium holds = SchematicMedium.Fuel;
}

/// <summary>One pipe on a schematic page, between two nodes or from one node to open air.</summary>
public class SchematicPipe
{
    public SchematicMedium medium;
    /// <summary>Its path, in the page's drawing units: at least two points, joined by straight runs.</summary>
    public List<Vector2> points = new();
    /// <summary>The node it leaves, and the node it reaches (empty: it ends in open air, as an exhaust does). Left out when either is.</summary>
    public string from = "";
    public string? to;
    /// <summary>The node whose work lets it flow: a pump, a belt. Empty: <see cref="from"/>.</summary>
    public string? drivenBy;
    public SchematicFlow flow = SchematicFlow.EngineRunning;
    /// <summary>Drawn only while this node is left out: the way air reaches an engine with no turbocharger.</summary>
    public string? onlyWithout;

    public string Driver => drivenBy.NullOrEmpty() ? from : drivenBy!;
}

/// <summary>
/// A live systems schematic page (Framework 0.363.0; docs/development/vehicle-schematic.md, SC1 and SC5): parts drawn as plain symbols,
/// joined by pipes in the colour of what they carry, each part coloured by its reading and its health. One page serves every vehicle of a
/// kind: a part the vehicle lacks is left out with its pipes. Other modders add pages in XML.
/// </summary>
public class PhobosVehicleSchematicDef : Def
{
    /// <summary>The loadout kinds this page serves (PhobosVehicleLoadoutDef.kind): CombustionLand, CombustionHeavy and the rest.</summary>
    public List<string> loadoutKinds = new();
    /// <summary>The engine kinds it serves; empty for any.</summary>
    public List<EngineKind> engineKinds = new();
    /// <summary>The page's size in drawing units; the window fits it to its space without stretching.</summary>
    public Vector2 size = new(720f, 360f);
    public List<SchematicNode> nodes = new();
    public List<SchematicPipe> pipes = new();

    /// <summary>The page for a vehicle, or null when none serves it: its loadout's kind, and its engine's.</summary>
    public static PhobosVehicleSchematicDef? For(CompPhobosVehicleSystems comp)
    {
        string? kind = comp.Props.loadout?.kind;
        if (kind == null) return null;
        EngineKind? engine = comp.Powertrain?.Def?.engine.kind;
        foreach (PhobosVehicleSchematicDef page in DefDatabase<PhobosVehicleSchematicDef>.AllDefsListForReading)
        {
            if (!page.loadoutKinds.Contains(kind)) continue;
            if (page.engineKinds.Count > 0 && (engine == null || !page.engineKinds.Contains(engine.Value))) continue;
            return page;
        }
        return null;
    }

    public SchematicNode? Node(string? key) => key.NullOrEmpty() ? null : nodes.FirstOrDefault(n => n.key == key);

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (loadoutKinds.Count == 0) yield return "a schematic page needs at least one loadout kind";
        if (size.x <= 0f || size.y <= 0f) yield return "a schematic page needs a size";
        foreach (IGrouping<string, SchematicNode> twice in nodes.GroupBy(n => n.key).Where(g => g.Count() > 1)) yield return "two nodes are called " + twice.Key;
        foreach (SchematicNode node in nodes)
        {
            if (node.key.NullOrEmpty()) yield return "a node has no key";
            if (node.rect.width <= 0f || node.rect.height <= 0f) yield return "node " + node.key + " has no size";
            if (!node.quantity.NullOrEmpty() && node.system != null && node.system.quantities.All(q => q.name != node.quantity))
            {
                yield return "node " + node.key + " reads " + node.quantity + ", which " + node.system.defName + " does not have";
            }
            if (!node.quantity.NullOrEmpty() && node.system == null) yield return "node " + node.key + " reads a quantity but names no system";
        }
        foreach (SchematicPipe pipe in pipes)
        {
            if (pipe.points.Count < 2) yield return "a pipe from " + pipe.from + " needs at least two points";
            if (Node(pipe.from) == null) yield return "a pipe leaves " + pipe.from + ", which is not a node";
            if (!pipe.to.NullOrEmpty() && Node(pipe.to) == null) yield return "a pipe reaches " + pipe.to + ", which is not a node";
            if (Node(pipe.Driver) == null) yield return "a pipe is driven by " + pipe.Driver + ", which is not a node";
            if (!pipe.onlyWithout.NullOrEmpty() && Node(pipe.onlyWithout) == null) yield return "a pipe is drawn without " + pipe.onlyWithout + ", which is not a node";
        }
    }
}
