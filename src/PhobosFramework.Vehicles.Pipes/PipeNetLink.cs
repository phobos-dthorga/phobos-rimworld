using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Vehicles.Rules;
using PipeSystem;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems.Pipes;

/// <summary>
/// A parked tanker or rig joins any Vanilla Expanded Framework pipe of a fluid it carries or pumps, by laying that net's own pipe beneath it. Since Framework
/// 0.108.0 (owner, 13 Nov 2026) it lays them across its whole footprint whenever it stands with its engine off (or is set up for work), whether or not a run
/// touches it, "so players are not confused"; a fuel line fills the tank only where a run of the line's own reaches it.
/// Since Framework 0.107.0 (owner, 13 Nov 2026) any fuelled vehicle also joins a fuel line, a net whose Def links it to refuelables of the
/// vehicle's own fuel (Vanilla Chemfuel Expanded's chemfuel line, by its <c>linkToRefuelables</c>, never by name), and fills its tank from the
/// net's storage while it stands with its engine off. Buildings with a refuelable tank, such as the depot bay, are filled by Vanilla Expanded
/// Framework itself (AutoLinkRefuelables), so nothing of ours is needed for them.
/// </summary>
[StaticConstructorOnStartup]
public sealed class PipeNetLink : VehiclePipes.ILink
{
    const string Prefix = "PipeNet:";
    /// <summary>The keys of what is laid beneath set-out pieces, after <see cref="Prefix"/>: a net's own key is the vehicle's own footprint.</summary>
    const string OutPrefix = "out:";

    static PipeNetLink() => VehiclePipes.Register(new PipeNetLink());

    static IEnumerable<PipeNetDef> Nets(VehiclePipes.IHost comp)
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
        // A packed piece's store rides on the trailer (Framework 0.347.0: the Billy's paste tank), so its net lies beneath the trailer.
        if (comp.Vehicle.GetComp<CompPhobosSetOut>() is { } setOut)
        {
            foreach (CompPhobosReservoir store in setOut.Reservoirs())
            {
                if (store.parent.Spawned) continue;
                foreach (PipeNetDef net in NetsOf(store)) yield return net;
            }
        }
    }

    /// <summary>The nets a store's load is pumped through (nutrient paste's paste net).</summary>
    static IEnumerable<PipeNetDef> NetsOf(CompPhobosReservoir store)
    {
        if (store.Props.contents is not { } contents) yield break;
        foreach (Port_PipeNet port in contents.ports.OfType<Port_PipeNet>())
        {
            if (port.net != null) yield return port.net;
        }
    }

    /// <summary>
    /// The nets what it has set out draws on (Framework 0.219.0; the owner, 22 Nov 2026: the Billy's burners and the chemfuel line): a trailer's
    /// tent and pieces, a camp. A piece that burns a fuel a net refills (Vanilla Expanded Framework gives every such refuelable a pipe link of its
    /// own, by the net's <c>linkToRefuelables</c>), or that holds a resource of a net (a nutrient paste tap), wants that net. Every cell of what
    /// is set out comes back with each net, so the net's hidden pipe is laid beneath all of it, as the trailer's plumbing is.
    /// </summary>
    static List<(PipeNetDef net, List<IntVec3> cells)> SetOutNets(VehiclePawn vehicle)
    {
        var result = new List<(PipeNetDef, List<IntVec3>)>();
        CompPhobosSetOut? setOut = vehicle.GetComp<CompPhobosSetOut>();
        CompPhobosCamp? camp = vehicle.GetComp<CompPhobosCamp>();
        var pieces = new List<Thing>();
        if (setOut != null) pieces.AddRange(setOut.Placed.Concat(setOut.Structure).Where(t => t.Spawned));
        if (camp != null) pieces.AddRange(camp.StandingPieces);
        if (pieces.Count == 0) return result;
        var nets = new HashSet<PipeNetDef>();
        foreach (Thing piece in pieces)
        {
            if (piece is not ThingWithComps withComps) continue;
            if (withComps.GetComp<CompRefuelable>() is { } refuelable)
            {
                foreach (PipeNetDef net in DefDatabase<PipeNetDef>.AllDefsListForReading)
                {
                    if (net.linkToRefuelables != null && net.linkToRefuelables.Any(option => option.thing != null && refuelable.Props.fuelFilter.Allows(option.thing))) nets.Add(net);
                }
            }
            foreach (ThingComp comp in withComps.AllComps)
            {
                if (comp is CompResource { Props.pipeNet: { } net }) nets.Add(net);
                // A store of ours (the Billy's paste tap, Framework 0.347.0) holds no Vanilla Expanded Framework resource: its load's nets.
                else if (comp is CompPhobosReservoir store) nets.UnionWith(NetsOf(store));
            }
        }
        if (nets.Count == 0) return result;
        var cells = new HashSet<IntVec3>(vehicle.OccupiedRect().Cells);
        if (setOut != null) cells.UnionWith(setOut.SetOutCells());
        if (camp != null) cells.UnionWith(camp.CampCells());
        foreach (PipeNetDef net in nets) result.Add((net, cells.ToList()));
        return result;
    }

    /// <summary>The net that refuels this vehicle's own fuel, and its units to one fuel item: the vehicle's, fuelled, not electric.</summary>
    static PipeNetDef? FuelLine(VehiclePipes.IHost comp, out int unitsPerFuel)
    {
        unitsPerFuel = 1;
        CompFueledTravel? fuel = comp.Vehicle.CompFueledTravel;
        ThingDef? kind = fuel?.Props.fuelType;
        if (fuel == null || fuel.Props.electricPowered || kind == null) return null;
        foreach (PipeNetDef net in DefDatabase<PipeNetDef>.AllDefsListForReading)
        {
            LinkOption? link = net.linkToRefuelables?.FirstOrDefault(option => option.thing == kind);
            if (link == null) continue;
            unitsPerFuel = link.ratio;
            return net;
        }
        return null;
    }

    /// <summary>When each vehicle last looked at its fuel line, for the hours a look covers. Derived, never saved: the first look after a
    /// load moves nothing.</summary>
    static readonly Dictionary<int, int> lastFuelLook = new();
    /// <summary>The vehicles already told their tank is full from the line, until they next leave it.</summary>
    static readonly HashSet<int> toldFull = new();

    // The net's hidden pipe, which draws nothing on the ground (its colour has no alpha) and shows in the net's overlay (owner, 13 Nov 2026: "You need to use the
    // Hidden versions of them"). Framework 0.107.0 and 0.108.0 laid the visible pipe; the pieces of ours they laid are swapped for the hidden one at the next look.
    // A net with no hidden pipe falls back to its first.
    // Hidden is read from what the pipe is, never its name (Framework 0.108.2; owner, 13 Nov 2026: Vanilla Chemfuel Expanded's chemfuel pipe still showed):
    // a pipe drawn with no alpha, or one built the way the game's hidden conduit, Dubs' hidden plumbing and VCHE's hidden chemfuel pipe all are,
    // neither targetable nor damaged by attacks, as a buried line is.
    static bool IsHidden(ThingDef def) =>
        (def.graphicData != null && def.graphicData.color.a == 0f)
        || (def.building is { isTargetable: false, canBeDamagedByAttacks: false });

    static bool IsOldHidden(PipeNetDef net, Thing thing) => net.pipeDefs != null && net.pipeDefs.Contains(thing.def) && IsHidden(thing.def);

    static ThingDef? PipeOf(PipeNetDef net) => net.pipeDefs?.FirstOrDefault(IsHidden) ?? net.pipeDefs?.FirstOrDefault();

    static Thing? Make(PipeNetDef net)
    {
        ThingDef? def = PipeOf(net);
        return def == null ? null : ThingMaker.MakeThing(def, def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null);
    }

    public void Update(VehiclePipes.IHost comp)
    {
        var wanted = new HashSet<string>();
        bool onFuelLine = false;
        if (comp.ParkedForPipes)
        {
            Map map = comp.Vehicle.Map;
            PipeNetManager? manager = map.GetComponent<PipeNetManager>();
            CellRect footprint = comp.Vehicle.OccupiedRect();
            PipeNetDef? fuelLine = FuelLine(comp, out int unitsPerFuel);
            IEnumerable<PipeNetDef> nets = Nets(comp);
            if (fuelLine != null) nets = nets.Append(fuelLine);
            foreach (PipeNetDef net in nets.Distinct())
            {
                if (manager == null) break;
                string key = Prefix + net.defName;
                var underlay = comp.Underlay(key);
                bool Piped(IntVec3 cell) => manager.GetPipeNetAt(cell, net) != null;
                underlay.Renew(PipeOf(net), () => Make(net));
                // Read before laying: only a run that is not ours makes a line worth drawing from.
                bool touches = underlay.TouchesRun(footprint, map, Piped);
                wanted.Add(key);
                underlay.Fill(footprint, map, Piped, () => Make(net), cell => !cell.GetTerrain(map).IsWater);
                if (net != fuelLine || !touches) continue;
                onFuelLine = true;
                Refuel(comp, manager, net, unitsPerFuel);
            }
            // What it has set out: the nets its pieces draw on, beneath all of it (Framework 0.219.0).
            if (manager != null)
            {
                foreach ((PipeNetDef net, List<IntVec3> cells) in SetOutNets(comp.Vehicle))
                {
                    string key = Prefix + OutPrefix + net.defName;
                    var underlay = comp.Underlay(key);
                    bool Piped(IntVec3 cell) => manager.GetPipeNetAt(cell, net) != null;
                    underlay.Renew(PipeOf(net), () => Make(net));
                    wanted.Add(key);
                    foreach (IntVec3 cell in cells)
                    {
                        if (cell.InBounds(map)) underlay.Fill(CellRect.SingleCell(cell), map, Piped, () => Make(net), at => !at.GetTerrain(map).IsWater);
                    }
                    if (Seen.Wants("trailers.set-out-line", comp.Vehicle, net.defName)) Seen.Note("trailers.set-out-line", comp.Vehicle, cells.Count + " cells", net.defName);
                }
            }
        }
        if (comp.ParkedForPipes) FillStores(comp);
        if (!onFuelLine) ForgetFuelLine(comp);
        foreach (KeyValuePair<string, Phobos.Framework.Persistence.PipeUnderlay> pair in comp.UnderlaysStartingWith(Prefix))
        {
            if (!wanted.Contains(pair.Key)) pair.Value.Clear();
        }
    }

    /// <summary>Fills the tank from the line it stands on, while its engine is off and it is not set up for work.</summary>
    static void Refuel(VehiclePipes.IHost comp, PipeNetManager manager, PipeNetDef net, int unitsPerFuel)
    {
        VehiclePawn vehicle = comp.Vehicle;
        CompFueledTravel? fuel = vehicle.CompFueledTravel;
        int id = vehicle.thingIDNumber;
        int now = Find.TickManager.TicksGame;
        int last = lastFuelLook.TryGetValue(id, out int seen) ? seen : -1;
        lastFuelLook[id] = now;
        string line = net.resource?.name?.ToLowerInvariant() ?? net.label ?? net.defName;
        if (fuel == null) return;
        if (fuel.Fuel >= fuel.FuelCapacity - 0.01f)
        {
            comp.PipeFuelLine = null;
            if (toldFull.Add(id) && last >= 0)
            {
                Messages.Message("PhobosVehicleSystems_PipeFuelFull".Translate(vehicle.LabelShort.Named("VEHICLE"), line.Named("LINE")), vehicle,
                    MessageTypeDefOf.SilentInput, historical: false);
            }
            return;
        }
        toldFull.Remove(id);
        if (vehicle.Drafted)
        {
            comp.PipeFuelLine = "PhobosVehicleSystems_PipeFuelEngineOn".Translate(line.Named("LINE")).Resolve();
            return;
        }
        if (vehicle.GetComps<CompPhobosSetUp>().Any(setUp => setUp.Deployed))
        {
            comp.PipeFuelLine = "PhobosVehicleSystems_PipeFuelSetUp".Translate(line.Named("LINE")).Resolve();
            return;
        }
        PipeNet? found = null;
        foreach (IntVec3 cell in vehicle.OccupiedRect())
        {
            found = manager.GetPipeNetAt(cell, net);
            if (found != null) break;
        }
        if (found == null || found.Stored <= 0f)
        {
            comp.PipeFuelLine = "PhobosVehicleSystems_PipeFuelEmpty".Translate(line.Named("LINE")).Resolve();
            return;
        }
        PhobosVehicleRulesDef rules = PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();
        double hours = PipeRefuelRule.Hours(last, now, GenDate.TicksPerHour, rules.pipeRefuelMostHours);
        double want = PipeRefuelRule.FuelThisLook(fuel.Fuel, fuel.FuelCapacity, hours, rules.pipeRefuelPerHour, found.Stored, unitsPerFuel);
        if (want > 0)
        {
            // The net's stored figure is refreshed only now and then, so the tank takes what really came out.
            found.DrawAmongStorage((float)PipeRefuelRule.UnitsFor(want, unitsPerFuel), out float drawn, null);
            if (drawn > 0f) fuel.Refuel(drawn / unitsPerFuel);
        }
        comp.PipeFuelLine = "PhobosVehicleSystems_PipeFuelFilling".Translate(line.Named("LINE"), fuel.Fuel.ToString("0").Named("FUEL"),
            fuel.FuelCapacity.ToString("0").Named("CAPACITY")).Resolve();
    }

    /// <summary>
    /// Fills a set-out piece's store from a run of its load's net (Framework 0.347.0; the owner, 27 Nov 2026: the paste pipes showed nowhere
    /// beneath the Billy). A standing piece draws through the cells it stands on, a packed one through the trailer's own, once the net's
    /// pipe laid there joins a run with storage (Vanilla Nutrient Paste Expanded's vats), carrying what the stores' paste was made from. It
    /// takes what fits at each look, as the game's own paste taps draw from their vats as they serve; a run of only our own pipe holds
    /// nothing, so nothing moves.
    /// </summary>
    static void FillStores(VehiclePipes.IHost comp)
    {
        if (comp.Vehicle.GetComp<CompPhobosSetOut>() is not { } setOut || comp.Vehicle.Map is not { } map
            || map.GetComponent<PipeNetManager>() is not { } manager) return;
        foreach (CompPhobosReservoir store in setOut.Reservoirs())
        {
            if (store.Space < 1f) continue;
            CellRect cells = store.parent.Spawned ? store.parent.OccupiedRect() : comp.Vehicle.OccupiedRect();
            foreach (PipeNetDef net in NetsOf(store))
            {
                PipeNet? found = null;
                foreach (IntVec3 cell in cells)
                {
                    if (cell.InBounds(map) && manager.GetPipeNetAt(cell, net) is { } at)
                    {
                        found = at;
                        break;
                    }
                }
                float wanted = found == null ? 0f : System.Math.Min(store.Space, found.Stored);
                if (found == null || wanted < 1f) continue;
                // The net's stored figure is refreshed only now and then, so the store takes what really came out.
                found.DrawAmongStorage(wanted, out float drawn, null);
                if (drawn <= 0f) continue;
                store.Add(drawn, Port_PipeNet.IngredientsOf(found).ToList());
                if (Seen.Wants("vehicles.reservoir-piped", store.parent, store.parent.Spawned ? "standing" : "packed"))
                    Seen.Note("vehicles.reservoir-piped", store.parent, drawn.ToString("0") + " " + net.defName, store.parent.Spawned ? "standing" : "packed");
                break;
            }
        }
    }

    static void ForgetFuelLine(VehiclePipes.IHost comp)
    {
        int id = comp.Vehicle.thingIDNumber;
        lastFuelLook.Remove(id);
        toldFull.Remove(id);
        comp.PipeFuelLine = null;
    }

    public void AfterLoad(VehiclePipes.IHost comp)
    {
        if (!comp.Vehicle.Spawned) return;
        Map map = comp.Vehicle.Map;
        CellRect footprint = comp.Vehicle.OccupiedRect();
        foreach (KeyValuePair<string, Phobos.Framework.Persistence.PipeUnderlay> pair in comp.UnderlaysStartingWith(Prefix))
        {
            string name = pair.Key.Substring(Prefix.Length);
            if (name.StartsWith(OutPrefix)) continue;
            PipeNetDef? net = DefDatabase<PipeNetDef>.GetNamedSilentFail(name);
            if (net == null) continue;
            pair.Value.AdoptOnce(footprint, map, thing => IsOldHidden(net, thing));
            pair.Value.Renew(PipeOf(net), () => Make(net));
        }
    }

    public void Leave(VehiclePipes.IHost comp)
    {
        ForgetFuelLine(comp);
        foreach (KeyValuePair<string, Phobos.Framework.Persistence.PipeUnderlay> pair in comp.UnderlaysStartingWith(Prefix)) pair.Value.Clear();
    }
}
