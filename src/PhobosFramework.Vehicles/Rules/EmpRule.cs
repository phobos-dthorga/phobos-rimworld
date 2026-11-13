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

    /// <summary>Whether an EMP may stun (stall) the vehicle: never a mechanical one, nor one whose author opted out of being stunned.</summary>
    public static bool Stuns(EmpHardening hardening, bool authorSaysNoStun) => hardening != EmpHardening.Mechanical && !authorSaysNoStun;
}
