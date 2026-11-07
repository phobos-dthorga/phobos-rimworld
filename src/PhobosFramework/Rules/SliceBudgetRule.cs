namespace Phobos.Framework.Rules;

/// <summary>
/// How much of one drawn frame a long job may take (FrameWorkQueue). Game-free: compiled into the tests without the game.
/// </summary>
public static class SliceBudgetRule
{
    /// <summary>A modest default for work that runs while the player plays on (agent default).</summary>
    public const float DefaultBudgetMs = 12f;

    /// <summary>
    /// Whether a slice may take one more step: always up to <paramref name="minimumSteps"/>, so a job can't creep when single steps
    /// are slow, then only while the budget lasts.
    /// </summary>
    public static bool MayContinue(double elapsedMs, float budgetMs, int doneThisSlice, int minimumSteps) =>
        doneThisSlice < minimumSteps || elapsedMs < budgetMs;

    public static float Clamp(float value, float min, float max) => value < min ? min : value > max ? max : value;
}
