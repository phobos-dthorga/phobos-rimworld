namespace Phobos.Factory.Rules;

/// <summary>
/// The output limit (Pipe System's <c>maxOutputCount</c>) that never loses a finished batch.
/// When a batch finishes, Pipe System adds it to a matching stack already in the output cell whenever that
/// stack holds no more than the limit; whatever doesn't fit on the stack is never placed, and the batch is
/// gone. The game counts two identical items as stackable even when their stack limit is 1, so the limit
/// must keep stack plus batch within the stack limit, and an item that doesn't stack needs a limit below 1:
/// then any identical item already there makes the machine wait. A limit of 0 means "no limit" and is never
/// safe. Read from Pipe System's Process.TrySpawnAtCell and the game's Thing.CanStackWith, 29 Oct 2026.
/// No game types: this file is compiled into the unit tests too.
/// </summary>
public static class OutputCapRule
{
    /// <summary>The limit for an output that doesn't fit even on a stack of one: wait while any is there.</summary>
    public const float WaitWhileAnyIsThere = 0.5f;

    /// <summary>The largest safe limit for one result.</summary>
    public static float Largest(int stackLimit, int batch) =>
        stackLimit - batch >= 1 ? stackLimit - batch : WaitWhileAnyIsThere;

    /// <summary>Whether a limit loses nothing for this result.</summary>
    public static bool IsSafe(float limit, int stackLimit, int batch) =>
        limit > 0f && limit <= Largest(stackLimit, batch);

    /// <summary>The wanted limit if it is safe, otherwise the largest safe one; 0 or less asks for the largest.</summary>
    public static float Clamp(float wanted, int stackLimit, int batch) =>
        IsSafe(wanted, stackLimit, batch) ? wanted : Largest(stackLimit, batch);
}
