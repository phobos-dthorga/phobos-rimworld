using System.Collections.Generic;

// Not "Phobos.Framework.UI": a namespace named UI would hide the game's Verse.UI class from every Phobos.Framework.* namespace.
namespace Phobos.Framework.Interface;

/// <summary>
/// Per-thing state for an inspector tab (scroll positions, a pending scroll target): the game makes one shared instance of each
/// tab type, so a tab keeps its state here, by the thing's ID, and clears it when it closes.
/// </summary>
public sealed class TabState<T> where T : class, new()
{
    readonly Dictionary<int, T> states = new();

    public T Get(int thingId)
    {
        if (!states.TryGetValue(thingId, out T? state))
        {
            state = new T();
            states[thingId] = state;
        }
        return state;
    }

    public bool TryGet(int thingId, out T? state) => states.TryGetValue(thingId, out state);

    public void Remove(int thingId) => states.Remove(thingId);

    public void Clear() => states.Clear();

    public int Count => states.Count;

    public IEnumerable<int> Keys => states.Keys;
}
