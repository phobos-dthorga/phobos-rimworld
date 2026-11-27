namespace Phobos.Dosing.Rules;

/// <summary>
/// A vaccine's head start (Medical 0.35.0; docs/development/medical-care-round-two.md, the owner's card of 27 Nov 2026: "A third head
/// start"): when a disease of the vaccine's class takes hold, the body's immunity to it starts part of the way up instead of at zero,
/// so the patient usually wins the race but the old and weak can still lose. Never lowers immunity already gained. Game-free.
/// </summary>
public static class PrimedImmunityRule
{
    public static float Raised(float immunity, float headStart) => immunity < headStart ? headStart : immunity;

    /// <summary>
    /// How much a clean person's chance of catching worms is cut (Medical 0.35.0, the hygiene card: "small, as proposed"): in step with
    /// their hygiene, up to <paramref name="maxCut"/> when spotless. Dubs Bad Hygiene keeps food poisoning, through the cook.
    /// </summary>
    public static float CleanChanceFactor(float hygiene, float maxCut)
    {
        float level = hygiene < 0f ? 0f : hygiene > 1f ? 1f : hygiene;
        float cut = maxCut < 0f ? 0f : maxCut > 1f ? 1f : maxCut;
        return 1f - cut * level;
    }
}
