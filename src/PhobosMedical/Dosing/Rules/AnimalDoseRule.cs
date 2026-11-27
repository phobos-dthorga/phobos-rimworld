namespace Phobos.Dosing.Rules;

/// <summary>
/// What an animal may be given (Medical 0.34.0; docs/development/medical-care-round-two.md, the owner's card of 27 Nov 2026): never
/// anything addictive, and nothing its race cannot swallow (the game's own test, which refuses an item marked for people only). Which
/// needs an animal has at all is each care need's own Def. Game-free.
/// </summary>
public static class AnimalDoseRule
{
    public static DoseRefusal Check(bool animal, float addictiveness, bool raceCanTake)
    {
        if (!animal) return DoseRefusal.None;
        if (!raceCanTake) return DoseRefusal.AnimalCannotTake;
        if (addictiveness > 0f) return DoseRefusal.AddictiveForAnimal;
        return DoseRefusal.None;
    }

    /// <summary>The pain from which an animal is dosed: only past the sedation band, where pain is extreme (the owner's card).</summary>
    public static float PainThreshold(bool animal, float painThreshold, float sedationThreshold) => animal ? sedationThreshold : painThreshold;
}
