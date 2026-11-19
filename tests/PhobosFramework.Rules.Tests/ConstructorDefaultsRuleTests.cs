using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class ConstructorDefaultsRuleTests
{
    class BuiltWithNothing
    {
    }

    /// <summary>The shape of the raid that could not be loaded: a class, a struct and a yes or no.</summary>
    class NeedsThree
    {
        public readonly string? Name;
        public readonly (int, int) Spot;
        public readonly bool Flag;

        public NeedsThree(string? name, (int, int) spot, bool flag)
        {
            Name = name;
            Spot = spot;
            Flag = flag;
        }
    }

    class ShortestWins
    {
        public ShortestWins(string a, int b, int c)
        {
        }

        ShortestWins(int a)
        {
        }
    }

    class HasBothKinds
    {
        public HasBothKinds()
        {
        }

        public HasBothKinds(int a)
        {
        }
    }

    class TwoFitEqually
    {
        public TwoFitEqually(string a)
        {
        }

        public TwoFitEqually(object a)
        {
        }
    }

    class TwoToldApartByAStruct
    {
        public TwoToldApartByAStruct(int a)
        {
        }

        public TwoToldApartByAStruct(long a)
        {
        }
    }

    class OnlyByReference
    {
        public OnlyByReference(ref int a)
        {
        }
    }

    abstract class NeverBuilt
    {
        protected NeverBuilt(int a)
        {
        }
    }

    [Fact]
    public void AClassBuiltWithNothingNeedsNoHelp() => Assert.Null(ConstructorDefaultsRule.EmptyArguments(typeof(BuiltWithNothing)));

    [Fact]
    public void AClassWithAnEmptyConstructorAmongOthersNeedsNoHelp() => Assert.Null(ConstructorDefaultsRule.EmptyArguments(typeof(HasBothKinds)));

    [Fact]
    public void EachArgumentGetsItsEmptyValue()
    {
        object?[]? arguments = ConstructorDefaultsRule.EmptyArguments(typeof(NeedsThree));
        Assert.NotNull(arguments);
        Assert.Equal(3, arguments!.Length);
        Assert.Null(arguments[0]);
        Assert.Equal((0, 0), arguments[1]);
        Assert.Equal(false, arguments[2]);
    }

    [Fact]
    public void TheEmptyValuesBuildTheObject()
    {
        object?[]? arguments = ConstructorDefaultsRule.EmptyArguments(typeof(NeedsThree));
        var built = (NeedsThree)System.Activator.CreateInstance(typeof(NeedsThree),
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, arguments, null, null)!;
        Assert.Null(built.Name);
        Assert.False(built.Flag);
    }

    [Fact]
    public void TheShortestConstructorIsUsedEvenWhenPrivate()
    {
        object?[]? arguments = ConstructorDefaultsRule.EmptyArguments(typeof(ShortestWins));
        Assert.NotNull(arguments);
        Assert.Equal(new object?[] { 0 }, arguments);
    }

    [Fact]
    public void TwoConstructorsTheEmptyValuesFitEquallyAreLeftAlone() => Assert.Null(ConstructorDefaultsRule.EmptyArguments(typeof(TwoFitEqually)));

    [Fact]
    public void AStructArgumentTellsTwoConstructorsApart() => Assert.NotNull(ConstructorDefaultsRule.EmptyArguments(typeof(TwoToldApartByAStruct)));

    [Fact]
    public void AnArgumentByReferenceIsLeftAlone() => Assert.Null(ConstructorDefaultsRule.EmptyArguments(typeof(OnlyByReference)));

    [Fact]
    public void AnAbstractClassIsLeftAlone() => Assert.Null(ConstructorDefaultsRule.EmptyArguments(typeof(NeverBuilt)));

    [Fact]
    public void AStructIsLeftAlone() => Assert.Null(ConstructorDefaultsRule.EmptyArguments(typeof(int)));
}
