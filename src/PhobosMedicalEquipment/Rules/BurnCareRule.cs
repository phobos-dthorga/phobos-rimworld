namespace Phobos.Medical.Rules;

/// <summary>What a burn unit bed's blower is doing, in the order a player needs to fix things.</summary>
public enum BurnCareStatus
{
    SwitchedOff,
    NoPower,
    Waiting,
    Treating,
}

/// <summary>What a burn unit bed should say. Game-free.</summary>
public static class BurnCareRule
{
    public static BurnCareStatus Status(bool switchedOn, bool powered, int patients)
    {
        if (!switchedOn) return BurnCareStatus.SwitchedOff;
        if (!powered) return BurnCareStatus.NoPower;
        return patients > 0 ? BurnCareStatus.Treating : BurnCareStatus.Waiting;
    }
}
