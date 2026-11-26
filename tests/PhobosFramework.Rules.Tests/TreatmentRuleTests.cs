using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class TreatmentRuleTests
{
    [Fact]
    public void AQuarterOfWhatPassesIsRunOff()
    {
        TreatmentRule.Pass pass = TreatmentRule.Step(2000f, 0f, 400f);
        Assert.Equal(400f, pass.processed, 3);
        Assert.Equal(100f, pass.rejected, 3);
        Assert.Equal(1900f, pass.amount, 3);
        Assert.Equal(300f, pass.treated, 3);
        Assert.False(pass.clean);
    }

    [Fact]
    public void ACompartmentIsCleanOnceAllHasPassed()
    {
        float amount = 1000f, treated = 0f;
        TreatmentRule.Pass pass = default;
        for (int i = 0; i < 10 && !pass.clean; i++)
        {
            pass = TreatmentRule.Step(amount, treated, 470f);
            amount = pass.amount;
            treated = pass.treated;
        }
        Assert.True(pass.clean);
        // Three quarters of the raw water is kept.
        Assert.Equal(750f, amount, 1);
    }

    [Fact]
    public void NothingPassesInAnEmptyCompartment()
    {
        TreatmentRule.Pass pass = TreatmentRule.Step(0f, 0f, 470f);
        Assert.Equal(0f, pass.processed);
        Assert.False(pass.clean);
    }

    [Theory]
    [InlineData(50000f, 1f)]
    [InlineData(500f, 0.01f)]
    [InlineData(0f, 0f)]
    public void TheMembraneWearsByLitres(float litres, float expected) => Assert.Equal(expected, TreatmentRule.Wear(litres), 4);

    [Theory]
    [InlineData(0.94f, false)]
    [InlineData(0.95f, true)]
    public void ASpentMembraneStops(float wear, bool spent) => Assert.Equal(spent, TreatmentRule.Spent(wear));
}
