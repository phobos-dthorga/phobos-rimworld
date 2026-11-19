using Phobos.Framework.Vehicles.Rules;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The drawn rotors of an aircraft on our own flight (Framework 0.173.0; the owner's report of 19 Nov 2026: the Cherokee's blades stood still).
/// Vehicle Framework turns rotor overlays (Graphic_Propeller and its reverse) only inside its own take-off and landing animation, by handing its
/// overlay renderer a rate each tick; our flight replaces that animation, so this hands it one: up to full while the engines run, on the ground
/// or in the air, and down to rest once they stop (<see cref="RotorRule"/>). An aircraft with no rotor overlay turns nothing. Derived and never
/// saved: a loaded aircraft's rotors start from rest and spin up.
/// </summary>
public partial class CompPhobosFlight
{
    /// <summary>The rotors' rate now, degrees a tick.</summary>
    float rotorRate;

    float? rotorFull;

    /// <summary>The rotors' full rate, degrees a tick: the Def's, else the full rate of the aircraft's own take-off animation, else none.</summary>
    float RotorFull => rotorFull ??= ReadRotorFull();

    float ReadRotorFull()
    {
        if (!float.IsNaN(Props.rotorDegreesPerTick)) return Mathf.Max(0f, Props.rotorDegreesPerTick);
        // The Def's own protocol, not the vehicle's copy: a loaded vehicle's copy holds its ticks, and its properties are the Def's anyway.
        LaunchProtocol? protocol = Vehicle.CompVehicleLauncher?.Props?.launchProtocol ?? Vehicle.CompVehicleLauncher?.launchProtocol;
        if (protocol is PropellerTakeoff propeller && propeller.LaunchProperties_Propeller?.angularVelocityPropeller is { } curve)
        {
            return Mathf.Max(0f, curve.Evaluate(1f));
        }
        return 0f;
    }

    /// <summary>Whether the engines turn the rotors now: always in the air (a dead engine's rotor windmills on the way down), and on the ground
    /// while the engines run.</summary>
    bool RotorsDriven => Airborne || Vehicle.Drafted;

    /// <summary>Whether the rotors still need a tick on the ground: our flight answers for it, it has rotors, and they turn or should.</summary>
    bool RotorsWantTicks => Active && RotorFull > 0f && (Vehicle.Drafted || rotorRate > 0f);

    /// <summary>One tick of the rotors. A few arithmetic steps; nothing for an aircraft with no rotor overlay.</summary>
    void RotorStep()
    {
        float full = RotorFull;
        if (full <= 0f) return;
        GraphicOverlayRenderer? renderer = Vehicle.DrawTracker?.overlayRenderer;
        if (renderer == null) return;
        bool wasStill = rotorRate <= 0f;
        rotorRate = (float)RotorRule.Next(rotorRate, full, RotorsDriven, 1.0 / TicksPerSecond, Props.rotorSpinUpSeconds, Props.rotorRunDownSeconds);
        if (rotorRate <= 0f) return;
        renderer.SetAcceleration(rotorRate);
        if (wasStill && Seen.On) Seen.Note("aircraft.rotors-turning", Vehicle);
    }

    /// <summary>The engines started on the ground: the rotors spin up, so the comp takes its tick.</summary>
    void OnIgnitionOn()
    {
        if (Vehicle.Spawned && RotorsWantTicks) StartTicking();
    }
}
