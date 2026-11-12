using System.Collections.Generic;
using System.Linq;
using PipeSystem;
using RimWorld;
using Verse;

namespace Phobos.VehicleSystems.Pipes;

/// <summary>A parked tanker or rig joins any Vanilla Expanded Framework pipe of a fluid it carries or pumps, by laying that net's own pipe beneath it.</summary>
[StaticConstructorOnStartup]
public sealed class PipeNetLink : VehiclePipes.ILink
{
    const string Prefix = "PipeNet:";

    static PipeNetLink() => VehiclePipes.Register(new PipeNetLink());

    static IEnumerable<PipeNetDef> Nets(CompPhobosVehicleSystems comp)
    {
        if (comp.Vehicle.GetComp<CompPhobosTank>() is { } tank)
        {
            foreach (TankCompartment compartment in tank.Compartments)
            {
                if (compartment.contents == null) continue;
                foreach (Port_PipeNet port in compartment.contents.ports.OfType<Port_PipeNet>())
                {
                    if (port.net != null) yield return port.net;
                }
            }
        }
        if (comp.Vehicle.GetComp<CompPhobosDrillRig>() is { } rig)
        {
            foreach (Outlet_PipeNet outlet in rig.Props.bufferOutlets.OfType<Outlet_PipeNet>())
            {
                if (outlet.net != null) yield return outlet.net;
            }
        }
    }

    // The net's ordinary, visible pipe (Framework 0.107.0; owner, 14 Nov 2026: as the game and other mods show a run). A net's hidden pipe draws nothing (its colour
    // has no alpha); earlier versions laid that one, and it is swapped for the visible pipe at the next look.
    static ThingDef? PipeOf(PipeNetDef net) => net.pipeDefs?.FirstOrDefault(def => def.graphicData == null || def.graphicData.color.a != 0f) ?? net.pipeDefs?.FirstOrDefault();

    static Thing? Make(PipeNetDef net)
    {
        ThingDef? def = PipeOf(net);
        return def == null ? null : ThingMaker.MakeThing(def, def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null);
    }

    public void Update(CompPhobosVehicleSystems comp)
    {
        var wanted = new HashSet<string>();
        if (comp.ParkedForPipes)
        {
            Map map = comp.Vehicle.Map;
            PipeNetManager? manager = map.GetComponent<PipeNetManager>();
            CellRect footprint = comp.Vehicle.OccupiedRect();
            foreach (PipeNetDef net in Nets(comp).Distinct())
            {
                if (manager == null) break;
                string key = Prefix + net.defName;
                var underlay = comp.Underlay(key);
                bool Piped(IntVec3 cell) => manager.GetPipeNetAt(cell, net) != null;
                underlay.Renew(PipeOf(net), () => Make(net));
                if (!underlay.TouchesRun(footprint, map, Piped)) continue;
                wanted.Add(key);
                underlay.Fill(footprint, map, Piped, () => Make(net));
            }
        }
        foreach (KeyValuePair<string, Phobos.Framework.Persistence.PipeUnderlay> pair in comp.UnderlaysStartingWith(Prefix))
        {
            if (!wanted.Contains(pair.Key)) pair.Value.Clear();
        }
    }

    public void Leave(CompPhobosVehicleSystems comp)
    {
        foreach (KeyValuePair<string, Phobos.Framework.Persistence.PipeUnderlay> pair in comp.UnderlaysStartingWith(Prefix)) pair.Value.Clear();
    }
}
