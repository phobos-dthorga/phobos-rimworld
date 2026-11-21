using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The self-protection jamming pod (Framework 0.226.0; owner, 22 Nov 2026: "self-protection pod"; docs/development/ea6b-prowler.md, step 3): a
/// fitting for any aircraft that deceives fire-control radars aimed at it. A personal bubble, not a field: on a map, an automated turret's
/// shot at the aircraft misses by the rules' chance (AirborneEvasion adds it to any dodge the aircraft has); on the world map, an air
/// defence hits it less often and takes longer to lock on (AirCombat's factors). Manned guns, aimed by eye, are not fooled. It works while its
/// pylon stands; its power is drawn as a load and shed with the others.
/// </summary>
public static class EcmPod
{
    public const string Ability = "EcmPod";

    static PhobosAirCombatRulesDef Rules => PhobosAirCombatRulesDef.Current<PhobosAirCombatRulesDef>();

    /// <summary>Whether this aircraft's pod works now: fitted, its pylon standing.</summary>
    public static bool Works(VehiclePawn? vehicle) => AirCombat.Systems(vehicle) is { Usable: true } systems && systems.Fittings.Grants(Ability);

    /// <summary>The chance the pod makes this shot miss: only a turret that aims by itself (no one mans it).</summary>
    public static float ChanceAgainst(VehiclePawn vehicle, Thing? launcher)
    {
        if (launcher is not Building_Turret turret || turret.GetComp<CompMannable>() != null || !Works(vehicle)) return 0f;
        if (Seen.Wants("aircraft.ecm-pod", vehicle)) Seen.Note("aircraft.ecm-pod", vehicle, turret.LabelShort);
        return Rules.ecmPodMissChance;
    }
}
