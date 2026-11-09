using System;
using System.Collections.Generic;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A parked vehicle's own pipes (owner, 9 Nov 2026): while it stands on or against a run of something it uses, it joins that run across its whole footprint, and it takes
/// them up when it moves off, as a gravship carries its own. The piping mods' tie-ins register a link; this one decides nothing about any of their pipes.
/// </summary>
public static class VehiclePipes
{
    public interface ILink
    {
        /// <summary>Joins or leaves as things now stand.</summary>
        void Update(CompPhobosVehicleSystems comp);

        /// <summary>Takes up everything it laid.</summary>
        void Leave(CompPhobosVehicleSystems comp);
    }

    static readonly List<ILink> links = new();

    public static void Register(ILink link) => links.Add(link);

    public static void Update(CompPhobosVehicleSystems comp) => Each(link => link.Update(comp));

    public static void Leave(CompPhobosVehicleSystems comp) => Each(link => link.Leave(comp));

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
