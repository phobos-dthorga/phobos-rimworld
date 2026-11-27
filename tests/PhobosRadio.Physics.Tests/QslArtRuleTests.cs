using System.Linq;
using Phobos.Radio.Physics;
using Xunit;

namespace Phobos.Radio.Physics.Tests;

/// <summary>A QSL card's drawn picture (Radio 0.59.0): ridges, sky, stock, ground and signature.</summary>
public class QslArtRuleTests
{
    [Fact]
    public void TheSameSeedDrawsTheSameRidge() => Assert.Equal(QslArtRule.Ridge(9, 3, 64, 0), QslArtRule.Ridge(9, 3, 64, 0));

    [Fact]
    public void ARidgeStaysInsideThePicture()
    {
        for (int hills = 0; hills <= 5; hills++)
            Assert.All(QslArtRule.Ridge(hills * 11, hills, 128, 0), h => Assert.InRange(h, 0f, 1f));
    }

    [Fact]
    public void MountainsStandHigherAndRougherThanFlatland()
    {
        float[] flat = QslArtRule.Ridge(4, 1, 128, 0), mountains = QslArtRule.Ridge(4, 4, 128, 0);
        Assert.True(mountains.Average() > flat.Average());
        Assert.True(mountains.Max() - mountains.Min() > flat.Max() - flat.Min());
    }

    [Fact]
    public void NearerLayersSitLower() => Assert.True(QslArtRule.Ridge(4, 3, 128, 1).Average() < QslArtRule.Ridge(4, 3, 128, 0).Average());

    [Fact]
    public void NoonIsBrighterThanMidnight() => Assert.True(QslArtRule.Sky(12).Top.Sum() > QslArtRule.Sky(0).Top.Sum());

    [Fact]
    public void DuskGlowsAtTheHorizon() => Assert.True(QslArtRule.Sky(18).Horizon[0] > QslArtRule.Sky(12).Horizon[0]);

    [Theory]
    [InlineData(2, QslStock.Hide)]
    [InlineData(3, QslStock.Parchment)]
    [InlineData(4, QslStock.Card)]
    [InlineData(5, QslStock.Glossy)]
    [InlineData(7, QslStock.Glossy)]
    public void TheStockFollowsTheTechLevel(int tech, QslStock stock) => Assert.Equal(stock, QslArtRule.Stock(tech));

    [Theory]
    [InlineData("AridShrubland", QslGround.Dunes)]
    [InlineData("SeaIce", QslGround.Snow)]
    [InlineData("TemperateForest", QslGround.Trees)]
    [InlineData("TemperateSwamp", QslGround.Swamp)]
    [InlineData("SomeModBiome", QslGround.Grass)]
    public void TheGroundFollowsTheBiomesName(string biome, QslGround ground) => Assert.Equal(ground, QslArtRule.Ground(biome));

    [Fact]
    public void ASignatureIsTheSameForTheSameNameAndDiffersBetweenNames()
    {
        Assert.Equal(QslArtRule.Signature("Combarro"), QslArtRule.Signature("Combarro"));
        Assert.NotEqual(QslArtRule.Signature("Combarro"), QslArtRule.Signature("Twik"));
        Assert.All(QslArtRule.Signature("Combarro"), p => Assert.InRange(p.Y, 0f, 1f));
    }

    [Fact]
    public void TheSunRisesOnTheLeftAndStandsHighestAtNoon()
    {
        Assert.True(QslArtRule.Light(7).X < QslArtRule.Light(17).X);
        Assert.True(QslArtRule.Light(12).Y > QslArtRule.Light(7).Y);
    }
}
