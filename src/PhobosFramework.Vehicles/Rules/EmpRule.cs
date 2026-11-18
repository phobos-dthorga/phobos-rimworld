namespace Phobos.Framework.Vehicles.Rules;

/// <summary>How well a vehicle's electrics are guarded against EMP. ByClass lets the rule decide from what the vehicle is.</summary>
public enum EmpHardening
{
    ByClass,
    /// <summary>Ordinary vehicles with engine computers and unshielded wiring: the full effect.</summary>
    Civilian,
    /// <summary>Built to military hardening standards (shielded wiring, filtered power): a reduced effect.</summary>
    Military,
    /// <summary>Nothing electronic keeps the engine running (steam, mechanical injection): never stalled, though its electrics are still hit.</summary>
    Mechanical,
}

/// <summary>
/// EMP on vehicles, game-free (Framework 0.109.0; soft-dependency-crosslinks.md, SD9; owner, 13 Nov 2026: hardening by class, Vehicle
/// Framework's own stun scaled by it, RimThunder's vehicles never stunned but hardened). Vehicle Framework rates each part's EMP
/// exposure from None to Severe (0 to 4) and stuns a vehicle only if some part is rated; no vehicle mod on the owner's list rates any,
/// so ours rates the electrical parts it binds. The class figures are authored: real military vehicles are built to electromagnetic
/// hardening standards (US MIL-STD-461 and MIL-STD-464), modern civilian ones are not, and a purely mechanical engine has little to stop.
/// </summary>
public static class EmpRule
{
    /// <summary>The class a vehicle falls in: the loadout's choice, or military when it carries a weapon and civilian otherwise.</summary>
    public static EmpHardening ClassFor(EmpHardening chosen, bool armed) =>
        chosen != EmpHardening.ByClass ? chosen : armed ? EmpHardening.Military : EmpHardening.Civilian;

    /// <summary>
    /// The rating an electrical part gets, as Vehicle Framework's severity (0 None to 4 Severe). A rating the vehicle's author gave
    /// stays: the author knows their design. Military parts take the military rating; civilian and mechanical ones the civilian rating.
    /// </summary>
    public static int Severity(EmpHardening hardening, int authorRating, int civilianRating, int militaryRating)
    {
        if (authorRating > 0) return authorRating;
        int rating = hardening == EmpHardening.Military ? militaryRating : civilianRating;
        return rating < 0 ? 0 : rating > 4 ? 4 : rating;
    }

    /// <summary>
    /// The rating a part has on a vehicle that has been hardened by kit (Framework 0.158.0; vehicle-militarised-service-vehicles.md, "The
    /// kits"; owner, 19 Nov 2026: hardening is its own kit): the military rating, where the part was rated any worse. A kit only ever
    /// helps, so a part already at or below the military rating keeps its own, and a part with no rating stays unharmed.
    /// </summary>
    public static int HardenedSeverity(int partRating, int militaryRating)
    {
        if (partRating <= 0) return 0;
        int military = militaryRating < 0 ? 0 : militaryRating > 4 ? 4 : militaryRating;
        return partRating < military ? partRating : military;
    }

    /// <summary>
    /// Whether Vehicle Framework's EMP step must be run for it (Framework 0.162.0): Combat Extended's armour setting replaces the whole
    /// damage method on a vehicle with its own, which has no EMP step, so an EMP never reached a vehicle. Only an EMP that still carries
    /// an amount, only when the method did not run, and only when Combat Extended is the reason: a hit another mod held back on purpose
    /// (a hull under water, an aircraft in the air) has its amount set to nothing and stays held back.
    /// </summary>
    public static bool EmpStepMissed(bool isEmp, float amount, bool methodRan, bool combatExtendedTakesVehicleDamage) =>
        isEmp && amount > 0f && !methodRan && combatExtendedTakesVehicleDamage;

    /// <summary>Whether an EMP may stun (stall) the vehicle: never a mechanical one, nor one whose author opted out of being stunned.</summary>
    public static bool Stuns(EmpHardening hardening, bool authorSaysNoStun) => hardening != EmpHardening.Mechanical && !authorSaysNoStun;
}
