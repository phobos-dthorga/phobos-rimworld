using System.Collections.Generic;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A jamming aircraft's choice of what its pods jam: radar, radios or both (Framework 0.385.0; docs/development/electronic-warfare.md, EW4).
/// The first user is the EA-6B Prowler, through D'thorga Patches: its own mod works out where and how far it jams; this says how much of
/// that reach stays on radar, and which radio fields go beside it. Radios are jammed only where something hears them, so while D'thorga
/// Radio Framework is not installed every aircraft jams radar, whatever its saved choice. With a separate communications jammer fitted and
/// powered (a fitting granting <see cref="CommsJammerGrant"/>: the Prowler's ICAP III), "both" keeps the pods on radar at full strength and
/// the radio jammer takes the radios (<see cref="JamFieldRule.Share"/>). Any faction's aircraft; another faction's keeps its default, radar.
/// </summary>
public static class AircraftJamming
{
    /// <summary>What a separate communications jammer grants (the ICAP III suite, Fittings_EA6BProwler.xml).</summary>
    public const string CommsJammerGrant = "CommsJammer";

    static PhobosVehicleRulesDef Rules => PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();

    /// <summary>Whether radios can be jammed at all: only with D'thorga Radio Framework, which hears the noise.</summary>
    public static bool RadiosHeard => Phobos.Framework.OptionalMods.IsActive(Phobos.Framework.OptionalMods.RadioFramework);

    /// <summary>The choices offered on the button, in order. Off is not one: the aircraft's own mod decides when it jams.</summary>
    public static readonly GroundJamMode[] Choices = { GroundJamMode.Radar, GroundJamMode.Radios, GroundJamMode.Both };

    /// <summary>The choice in force: the saved one, or radar while radios cannot be heard.</summary>
    public static GroundJamMode ChoiceOf(CompPhobosVehicleSystems? systems) =>
        systems == null || !RadiosHeard ? GroundJamMode.Radar : systems.AircraftJamChoice;

    /// <summary>Whether a separate communications jammer is fitted and has power now.</summary>
    public static bool CommsJammer(CompPhobosVehicleSystems? systems) => systems is { Usable: true } && systems.Fittings.GrantsPowered(CommsJammerGrant);

    /// <summary>How this aircraft's power is shared now.</summary>
    public static AircraftJamShare ShareOf(CompPhobosVehicleSystems? systems)
    {
        GroundJamMode choice = ChoiceOf(systems);
        return JamFieldRule.Share(choice is GroundJamMode.Radar or GroundJamMode.Both, choice is GroundJamMode.Radios or GroundJamMode.Both,
            CommsJammer(systems), Rules.jetJammerBothRadarShare);
    }

    /// <summary>The factor on the aircraft's radar reach: 1 on radar alone, √½ sharing, 0 on radios alone.</summary>
    public static float RadarReachFactor(CompPhobosVehicleSystems? systems) => (float)JamFieldRule.RadarReach(1.0, ShareOf(systems).PodsOnRadar);

    /// <summary>
    /// The fields an aircraft jamming at <paramref name="centre"/> makes, given its radar reach there (already shared, through
    /// <see cref="RadarReachFactor"/>): the pods' field, radar and radio watts together, and the communications jammer's beside it.
    /// </summary>
    public static IEnumerable<JamField> FieldsAt(VehiclePawn vehicle, Vector3 centre, float radarReach)
    {
        CompPhobosVehicleSystems? systems = vehicle.GetComp<CompPhobosVehicleSystems>();
        AircraftJamShare share = ShareOf(systems);
        float podWatts = share.PodsOnRadios * Rules.jetJammerRadioWatts;
        if (radarReach > 0f || podWatts > 0f) yield return new JamField(centre, radarReach, vehicle.Faction, vehicle, podWatts, Rules.jetJammerRadioMHz);
        if (share.CommsJammer && Rules.commsJammerRadioWatts > 0f)
            yield return new JamField(centre, 0f, vehicle.Faction, vehicle, Rules.commsJammerRadioWatts, Rules.commsJammerRadioMHz);
    }

    /// <summary>Sets the player's choice and says so. The button's one call.</summary>
    public static void SetChoice(CompPhobosVehicleSystems systems, GroundJamMode choice)
    {
        systems.AircraftJamChoice = choice;
        string said = "PhobosJam_AircraftChoiceSet".Translate(systems.Vehicle.LabelShort.Named("VEHICLE"),
            ("PhobosJam_Mode" + choice).Translate().Named("MODE")).Resolve();
        Messages.Message(said, systems.Vehicle, MessageTypeDefOf.SilentInput, historical: false);
        string variant = choice.ToString();
        if (Seen.Wants("vehicles.aircraft-jam-choice", systems.Vehicle, variant)) Seen.Note("vehicles.aircraft-jam-choice", systems.Vehicle, variant, variant);
    }
}
