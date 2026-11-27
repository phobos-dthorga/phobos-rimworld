using Phobos.Dosing.Rules;
using Xunit;

namespace Phobos.Dosing.Rules.Tests;

/// <summary>
/// Medical care, round two (docs/development/medical-care-round-two.md), slice 1: the pain ladder and disease classes. Synthetic figures:
/// a mild painkiller that takes 30% of the pain at no cost, and a strong one that takes 80% at a cost of 0.1 consciousness.
/// </summary>
public class RoundTwoRuleTests
{
    static readonly DoseWeights Weights = new(0.5f, 0.5f, 1f, 0.5f, 0.25f, 0.25f);
    static readonly DoseEffect Mild = DoseEffectRule.Combine(new[] { new StageEffect(0.7f, 0f, 0f, 1f, 0f, 1f) }, 0f, false, 0.1f);
    static readonly DoseEffect Strong = DoseEffectRule.Combine(new[] { new StageEffect(0.2f, 0f, -0.1f, 1f, 0f, 1f) }, 0f, false, 0.1f);
    static readonly DoseEffect Middling = DoseEffectRule.Combine(new[] { new StageEffect(0.5f, 0f, 0f, 1f, 0f, 1f) }, 0f, false, 0.1f);

    static DoseCandidate[] Both => new[] { new DoseCandidate(Mild, true), new DoseCandidate(Strong, true) };

    [Fact]
    public void WithoutALadderTheBestScoreIsGivenWhateverThePain()
    {
        // Strong scores 0.8 - 0.05 = 0.75; mild 0.3. As before Medical 0.33.0.
        Assert.Equal(1, DoseChoiceRule.Choose(Both, DoseNeed.PainRelief, false, Weights));
    }

    [Fact]
    public void MildPainTakesTheMildestThatDoesTheJob()
    {
        // 45% pain, brought under 40%: the mild one leaves 31.5%, enough, and costs nothing.
        Assert.Equal(0, DoseChoiceRule.Choose(Both, DoseNeed.PainRelief, false, Weights, new PainLadder(0.45f, 0.4f)));
    }

    [Fact]
    public void StrongPainTakesWhatDoesTheJob()
    {
        // 90% pain: the mild one leaves 63%, not enough; the strong one leaves 18%.
        Assert.Equal(1, DoseChoiceRule.Choose(Both, DoseNeed.PainRelief, false, Weights, new PainLadder(0.9f, 0.4f)));
    }

    [Fact]
    public void WhenNothingIsEnoughTheStrongestIsGiven()
    {
        // 95% pain, only the mild and a middling one: neither brings it under 40%, so the stronger of the two.
        var candidates = new[] { new DoseCandidate(Mild, true), new DoseCandidate(Middling, true) };
        Assert.Equal(1, DoseChoiceRule.Choose(candidates, DoseNeed.PainRelief, false, Weights, new PainLadder(0.95f, 0.4f)));
    }

    [Fact]
    public void OfTwoThatCostTheSameTheGentlerIsGiven()
    {
        // 45% pain: the middling one (50%) and the mild one (30%) both do the job at no cost.
        var candidates = new[] { new DoseCandidate(Middling, true), new DoseCandidate(Mild, true) };
        Assert.Equal(1, DoseChoiceRule.Choose(candidates, DoseNeed.PainRelief, false, Weights, new PainLadder(0.45f, 0.4f)));
    }

    [Fact]
    public void TheLadderLeavesOtherNeedsAlone()
    {
        // A drug for fighting disease is chosen by its score, as before, ladder or not.
        DoseEffect weak = DoseEffectRule.Combine(new[] { new StageEffect(1f, 0f, 0f, 1f, 0f, 1f, immunityGain: 0.1f) }, 0f, false, 0.1f);
        DoseEffect strong = DoseEffectRule.Combine(new[] { new StageEffect(1f, 0f, 0f, 1f, 0f, 1f, immunityGain: 0.35f) }, 0f, false, 0.1f);
        var candidates = new[] { new DoseCandidate(weak, true), new DoseCandidate(strong, true) };
        Assert.Equal(1, DoseChoiceRule.Choose(candidates, DoseNeed.Immunity, false, Weights, new PainLadder(0.45f, 0.4f)));
    }

    [Fact]
    public void AnAntibioticActsOnBacteriaOnly()
    {
        string[] antibiotic = { "PhobosDisease_Bacterial" };
        Assert.True(DiseaseClassRule.Acts(antibiotic, "PhobosDisease_Bacterial"));
        Assert.False(DiseaseClassRule.Acts(antibiotic, "PhobosDisease_Viral"));
    }

    [Fact]
    public void ADrugNamingNoClassActsOnEveryDisease()
    {
        Assert.True(DiseaseClassRule.Acts(System.Array.Empty<string>(), "PhobosDisease_Viral"));
        Assert.True(DiseaseClassRule.Acts(System.Array.Empty<string>(), null));
    }

    [Fact]
    public void ADiseaseNobodyHasClassedTakesAnyDrug()
    {
        // The owner's card of 27 Nov 2026: a new mod's disease is never left untreated because we had not read it.
        Assert.True(DiseaseClassRule.Acts(new[] { "PhobosDisease_Bacterial" }, null));
    }
}
