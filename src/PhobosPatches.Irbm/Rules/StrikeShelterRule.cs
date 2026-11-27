namespace Phobos.MissilePatches.Rules;

/// <summary>
/// The flash from a strike (D'thorga Patches 0.36.0; docs/development/nuclear-strike-effects.md, NK18): flash blindness is likelier and lasts
/// longer at night, when the pupils are open. The cover, its shadow and its share of the dose moved to D'thorga Framework's
/// RadiationShelterRule in Patches 0.47.0, so every radiation burst shares them. Game-free.
/// </summary>
public static class StrikeShelterRule
{
    /// <summary>How much likelier flash blindness is by the light: half as likely again in full dark as at noon (authored; the
    /// sources say only that it is worse at night, the pupil being larger).</summary>
    public static float FlashChanceFactor(float sunGlow) => Lerp(1.5f, 1f, sunGlow);

    /// <summary>
    /// The starting severity of the game's flash blindness, which heals about one a game hour: by day it "does not persist for more than
    /// about 2 minutes" (0.04); at night full night vision takes 15 to 35 minutes to return (0.5) (US Army FM 8-9, paragraph 412).
    /// </summary>
    public static float FlashSeverity(float sunGlow) => Lerp(0.5f, 0.04f, sunGlow);

    static float Lerp(float a, float b, float t) => a + (b - a) * (t < 0 ? 0 : t > 1 ? 1 : t);
}
