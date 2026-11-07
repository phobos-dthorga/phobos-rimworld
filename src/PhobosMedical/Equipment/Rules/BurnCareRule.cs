namespace Phobos.Medical.Rules;

/// <summary>What a burn unit bed's blower is doing, in the order a player needs to fix things.</summary>
public enum BurnCareStatus
{
    SwitchedOff,
    NoPower,
    Waiting,
    /// <summary>Someone is in the bed, but they have no open burn.</summary>
    NotNeeded,
    Treating,
}

/// <summary>What a burn unit bed should say. Game-free.</summary>
public static class BurnCareRule
{
    public static BurnCareStatus Status(bool switchedOn, bool powered, int patients, int notNeeding = 0)
    {
        if (!switchedOn) return BurnCareStatus.SwitchedOff;
        if (!powered) return BurnCareStatus.NoPower;
        if (patients > 0) return BurnCareStatus.Treating;
        return notNeeding > 0 ? BurnCareStatus.NotNeeded : BurnCareStatus.Waiting;
    }
}
