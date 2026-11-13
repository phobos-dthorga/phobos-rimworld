using System;
using System.Collections.Generic;
using System.Linq;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A parked vehicle's own pipes and conduits (owner, 9 Nov 2026; Framework 0.108.0, owner, 13 Nov 2026: "only appear when the vehicle is powered off" and
/// "should appear though regardless, so players are not confused"): while it stands with its engine off, it lays the networks it uses across its whole
/// footprint, whether or not a run touches it, and takes them up the moment its engine starts or it moves (UnderlayRule). A vehicle set up for work keeps
/// what its work runs through. The piping mods' tie-ins, and the power conduits, register a link; this one decides nothing about any of their pieces.
/// </summary>
public static class VehiclePipes
{
    /// <summary>
    /// What a vehicle lays its pipes through: its own pieces, kept per network, and whether it stands parked for them now. Our vehicle systems hold it for
    /// every vehicle they run; a trailer, which has none, holds its own (Framework 0.111.0: a water bowser takes water, so it has plumbing beneath it too).
    /// </summary>
    public interface IHost
    {
        VehiclePawn Vehicle { get; }

        /// <summary>The pieces it has laid beneath it for one network, made the first time they are asked for.</summary>
        Phobos.Framework.Persistence.PipeUnderlay Underlay(string key);

        /// <summary>The networks it holds pieces for whose keys start like this, for a link to take up what it no longer needs.</summary>
        List<KeyValuePair<string, Phobos.Framework.Persistence.PipeUnderlay>> UnderlaysStartingWith(string prefix);

        /// <summary>Whether its pipes lie beneath it now.</summary>
        bool ParkedForPipes { get; }

        /// <summary>What a fuel line touching it is doing for its own fuel tank, in words; null for nothing.</summary>
        string? PipeFuelLine { get; set; }
    }

    public interface ILink
    {
        /// <summary>Joins or leaves as things now stand.</summary>
        void Update(IHost host);

        /// <summary>Takes up everything it laid.</summary>
        void Leave(IHost host);

        /// <summary>After a load, once the map's things stand: takes back, once, the old hidden pieces earlier versions laid beneath the vehicle and a loading
        /// fault of Framework 0.107.0 orphaned (PipeUnderlay.AdoptOnce).</summary>
        void AfterLoad(IHost host);
    }

    static readonly List<ILink> links = new();

    public static void Register(ILink link) => links.Add(link);

    public static void Update(IHost host) => Each(link => link.Update(host));

    public static void Leave(IHost host) => Each(link => link.Leave(host));

    public static void AfterLoad(IHost host) => Each(link => link.AfterLoad(host));

    /// <summary>Whether the vehicle's own systems use this much water (a service's top-up), for a link deciding whether to lay plumbing.</summary>
    public static bool SystemsTakeWater(IHost host) =>
        host is CompPhobosVehicleSystems systems && systems.Props.systems.Any(system => system.def.service is { waterLitres: > 0f });

    /// <summary>The vehicle's tank, which says by its contents what it pumps through: on a truck or a trailer alike.</summary>
    public static CompPhobosTank? Tank(IHost host) => host.Vehicle.GetComp<CompPhobosTank>();

    /// <summary>A store of pieces keyed by network, for a host to keep and save.</summary>
    public sealed class Store
    {
        Dictionary<string, Phobos.Framework.Persistence.PipeUnderlay> underlays = new();

        public Phobos.Framework.Persistence.PipeUnderlay Get(string key)
        {
            if (!underlays.TryGetValue(key, out Phobos.Framework.Persistence.PipeUnderlay? underlay))
            {
                underlay = new Phobos.Framework.Persistence.PipeUnderlay();
                underlays[key] = underlay;
            }
            return underlay;
        }

        public List<KeyValuePair<string, Phobos.Framework.Persistence.PipeUnderlay>> StartingWith(string prefix) =>
            underlays.Where(pair => pair.Key.StartsWith(prefix, StringComparison.Ordinal)).ToList();

        public void Expose(string saveKey)
        {
            Scribe_Collections.Look(ref underlays, saveKey, LookMode.Value, LookMode.Deep);
            underlays ??= new Dictionary<string, Phobos.Framework.Persistence.PipeUnderlay>();
        }
    }

    static void Each(Action<ILink> act)
    {
        foreach (ILink link in links)
        {
            try
            {
                act(link);
            }
            catch (Exception error)
            {
                Log.ErrorOnce(VehicleSystemsMod.LogPrefix + " A vehicle's pipes failed: " + error, 0x5048_5750);
            }
        }
    }
}
