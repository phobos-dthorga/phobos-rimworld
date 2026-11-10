using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A diagnosis letter that opens as a report window (Framework 0.78.0, P1 step two) instead of the game's text dialog. The finding is
/// saved with the letter, so opening it again later, from the letter stack or the archive, shows what the mechanic found on the day. Its
/// coloured text body stays: the archive and other mods read it, and it is what the letter opens if the vehicle's Def is gone.
/// </summary>
public class Letter_PhobosReport : StandardLetter
{
    public const string LetterDefName = "PhobosVehicleSystems_DiagnosisReport";

    public DiagnosisReport? report;

    /// <summary>Sends the report as a letter pointing at the vehicle; a plain letter with the same text if our letter Def is missing.</summary>
    public static void Send(DiagnosisReport report, TaggedString label, string text, VehiclePawn vehicle)
    {
        LetterDef? def = DefDatabase<LetterDef>.GetNamedSilentFail(LetterDefName);
        if (def == null || LetterMaker.MakeLetter(label, text, def, new LookTargets(vehicle)) is not Letter_PhobosReport letter)
        {
            Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.NeutralEvent, vehicle);
            return;
        }
        letter.report = report;
        Find.LetterStack.ReceiveLetter(letter);
    }

    public override void OpenLetter()
    {
        if (report?.vehicleDef == null)
        {
            base.OpenLetter();
            return;
        }
        Find.WindowStack.Add(new Dialog_PhobosDiagnosis(this, report));
    }

    /// <summary>The window's Jump to: the camera goes to the vehicle and selects it, and the letter is read, as the game's own Jump to does.</summary>
    public void JumpTo()
    {
        GlobalTargetInfo target = lookTargets?.TryGetPrimaryTarget() ?? GlobalTargetInfo.Invalid;
        if (CameraJumper.CanJump(target)) CameraJumper.TryJumpAndSelect(target);
        Dismiss();
    }

    public bool CanJump => lookTargets != null && CameraJumper.CanJump(lookTargets.TryGetPrimaryTarget());

    /// <summary>Read: off the letter stack and into the archive, as the game's Close does. Opened from the archive, it stays there.</summary>
    public void Dismiss()
    {
        if (Find.LetterStack.LettersListForReading.Contains(this)) Find.LetterStack.RemoveLetter(this);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref report, "PhobosVehicleSystems_Report");
    }
}
