using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class MessageLevelRuleTests
{
    [Theory]
    [InlineData("RejectInput", MessageKind.Reply)]
    [InlineData("SilentInput", MessageKind.Reply)]
    [InlineData("CautionInput", MessageKind.Reply)]
    [InlineData("NeutralEvent", MessageKind.Routine)]
    [InlineData("PositiveEvent", MessageKind.Routine)]
    [InlineData("TaskCompletion", MessageKind.Routine)]
    [InlineData("NegativeEvent", MessageKind.Trouble)]
    [InlineData("ThreatBig", MessageKind.Trouble)]
    [InlineData("SomeOtherModsType", MessageKind.Trouble)]
    [InlineData(null, MessageKind.Trouble)]
    public void TheGamesMessageTypesMapToKinds(string? defName, MessageKind expected) => Assert.Equal(expected, MessageLevelRule.KindOf(defName));

    [Theory]
    [InlineData(MessageKind.Reply)]
    [InlineData(MessageKind.Routine)]
    [InlineData(MessageKind.Trouble)]
    public void AtAllNothingIsHidden(MessageKind kind) => Assert.False(MessageLevelRule.Hides(MessageLevel.All, kind, askedFor: false));

    [Theory]
    [InlineData(MessageLevel.TroublesOnly)]
    [InlineData(MessageLevel.LettersOnly)]
    public void RepliesAreNeverHidden(MessageLevel level) => Assert.False(MessageLevelRule.Hides(level, MessageKind.Reply, askedFor: false));

    [Theory]
    [InlineData(MessageLevel.TroublesOnly, MessageKind.Routine)]
    [InlineData(MessageLevel.LettersOnly, MessageKind.Routine)]
    [InlineData(MessageLevel.LettersOnly, MessageKind.Trouble)]
    public void WhatThePlayerAskedForAlwaysShows(MessageLevel level, MessageKind kind) => Assert.False(MessageLevelRule.Hides(level, kind, askedFor: true));

    [Fact]
    public void TroublesOnlyHidesRoutineNewsAndKeepsTroubles()
    {
        Assert.True(MessageLevelRule.Hides(MessageLevel.TroublesOnly, MessageKind.Routine, askedFor: false));
        Assert.False(MessageLevelRule.Hides(MessageLevel.TroublesOnly, MessageKind.Trouble, askedFor: false));
    }

    [Fact]
    public void LettersOnlyHidesEveryUnaskedMessage()
    {
        Assert.True(MessageLevelRule.Hides(MessageLevel.LettersOnly, MessageKind.Routine, askedFor: false));
        Assert.True(MessageLevelRule.Hides(MessageLevel.LettersOnly, MessageKind.Trouble, askedFor: false));
    }
}
