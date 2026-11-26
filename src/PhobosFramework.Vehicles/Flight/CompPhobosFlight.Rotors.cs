using Phobos.Framework.Vehicles.Rules;
using RimWorld;
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

    /// <summary>The share of full speed its rotors or propellers turn at now, 0 to 1 (Framework 0.316.0: what <see cref="CompPhobosPropellers"/>
    /// draws by); with no rotor rate of its own, 1 while they are driven.</summary>
    public float RotorShare => RotorFull > 0f ? Mathf.Clamp01(rotorRate / RotorFull) : RotorsDriven ? 1f : 0f;

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
        bool wasStill = rotorRate <= 0f;
        rotorRate = (float)RotorRule.Next(rotorRate, full, RotorsDriven, 1.0 / TicksPerSecond, Props.rotorSpinUpSeconds, Props.rotorRunDownSeconds);
        if (rotorRate <= 0f) return;
        // Framework 0.316.0: the rate is kept whether or not Vehicle Framework draws a rotor overlay, as our propeller bars read it.
        Vehicle.DrawTracker?.overlayRenderer?.SetAcceleration(rotorRate);
        if (wasStill && Seen.On) Seen.Note("aircraft.rotors-turning", Vehicle);
    }

    /// <summary>The share of full speed its rotors must reach before it leaves the ground (Framework 0.174.0; the owner's decision 2, VTOL
    /// Vehicle's figure).</summary>
    const float RotorReadyShare = 0.85f;

    /// <summary>Whether a take-off under way still waits for its rotors: it has rotors, they are below <see cref="RotorReadyShare"/>, and it
    /// has not yet left the ground or begun to roll.</summary>
    bool RotorsSpinningUp => RotorFull > 0f && rotorRate < RotorReadyShare * RotorFull && climb <= 0f && speed <= 0f;

    /// <summary>Says a take-off waits for the rotors, once, as it is ordered.</summary>
    void SayRotorsSpinningUp(VehiclePawn v)
    {
        if (!RotorsSpinningUp) return;
        if (v.Faction == RimWorld.Faction.OfPlayer)
            Messages.Message("PhobosFlight_MsgRotorsSpinningUp".Translate(v.LabelShort.Named("VEHICLE")).Resolve(), v, RimWorld.MessageTypeDefOf.SilentInput, historical: false);
        Seen.Note("aircraft.rotors-waited", v);
    }

    // ---- The sound of its flight (Framework 0.174.0; the audit of 19 Nov 2026, finding 1) ----

    SoundDef? flightSound;
    bool flightSoundRead;

    /// <summary>Whether our flight's sound is playing now. Derived, never saved: a loaded aircraft in the air starts it at its first tick.</summary>
    bool flightSoundOn;

    /// <summary>
    /// The sound it makes in the air on our flight: the Def's <c>flightSound</c>, else the sound its own Def starts on Vehicle Framework's launch
    /// event (the Cherokee's, Louie's and Mosquito's rotors), which Vehicle Framework plays only while it takes off for another place. None for
    /// an aircraft with an engine voice of ours (the Osprey), which already sounds its flight.
    /// </summary>
    SoundDef? FlightSound
    {
        get
        {
            if (flightSoundRead) return flightSound;
            flightSoundRead = true;
            flightSound = Props.flightSound;
            if (flightSound != null || Vehicle.def.GetModExtension<PhobosEngineSoundExtension>() != null) return flightSound;
            if (Vehicle.VehicleDef.soundSustainersOnEvent is { } entries)
            {
                foreach (VehicleSustainerEventEntry<VehicleEventDef> entry in entries)
                {
                    if (entry.start == VehicleEventDefOf.AerialVehicleLaunch && entry.value != null)
                    {
                        flightSound = entry.value;
                        break;
                    }
                }
            }
            return flightSound;
        }
    }

    /// <summary>Starts the flight's sound, replacing any of the same sound already playing (the Mosquito starts it with its engines).</summary>
    void StartFlightSound()
    {
        SoundDef? sound = FlightSound;
        if (flightSoundOn || sound == null || !Vehicle.Spawned || Vehicle.sustainers == null) return;
        Vehicle.sustainers.EndAll(sound);
        Vehicle.sustainers.Spawn(Vehicle, sound);
        flightSoundOn = true;
        if (Seen.On) Seen.Note("aircraft.flight-sound", Vehicle, sound.defName);
    }

    /// <summary>Ends the flight's sound: landed, gone from the map, or handing the sound to Vehicle Framework's own launch.</summary>
    void EndFlightSound()
    {
        if (!flightSoundOn) return;
        flightSoundOn = false;
        if (flightSound != null) Vehicle.sustainers?.EndAll(flightSound);
    }

    /// <summary>The engines started on the ground: the rotors spin up, so the comp takes its tick.</summary>
    void OnIgnitionOn()
    {
        if (Vehicle.Spawned && RotorsWantTicks) StartTicking();
    }
}
