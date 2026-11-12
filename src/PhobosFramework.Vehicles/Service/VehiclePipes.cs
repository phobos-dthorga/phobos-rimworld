using System;
using System.Collections.Generic;
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
    public interface ILink
    {
        /// <summary>Joins or leaves as things now stand.</summary>
        void Update(CompPhobosVehicleSystems comp);

        /// <summary>Takes up everything it laid.</summary>
        void Leave(CompPhobosVehicleSystems comp);

        /// <summary>After a load, once the map's things stand: takes back, once, the old hidden pieces earlier versions laid beneath the vehicle and a loading
        /// fault of Framework 0.107.0 orphaned (PipeUnderlay.AdoptOnce).</summary>
        void AfterLoad(CompPhobosVehicleSystems comp);
    }

    static readonly List<ILink> links = new();

    public static void Register(ILink link) => links.Add(link);

    public static void Update(CompPhobosVehicleSystems comp) => Each(link => link.Update(comp));

    public static void Leave(CompPhobosVehicleSystems comp) => Each(link => link.Leave(comp));

    public static void AfterLoad(CompPhobosVehicleSystems comp) => Each(link => link.AfterLoad(comp));

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
