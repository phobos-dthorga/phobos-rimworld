using System.Collections.Generic;
using System.Linq;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// Moving a saved vehicle over to a design that has had parts taken out (Framework 0.368.0, the vehicle standard's batch 5). Vehicle Framework
/// saves a vehicle's parts as healths by position and binds them to the design's list in order, so a part taken out of the middle would hand its
/// saved health to the next part and shift every later one. Each removal records where the part stood and the layout that took it out; a
/// vehicle saved with an older layout drops the saved entries at those places, last first, so every other part keeps its own wear. Game-free.
/// </summary>
public static class PartLayoutRule
{
    /// <summary>
    /// The saved entries to drop, last first: those removed by a layout newer than the vehicle's, standing inside what the save holds. A save
    /// made before a part was ever added holds fewer entries, so a place past its end is left alone (there is nothing there to drop).
    /// </summary>
    /// <param name="savedCount">How many parts the save holds.</param>
    /// <param name="savedLayout">The layout the vehicle was saved with (0 before layouts were recorded).</param>
    /// <param name="removed">Each removed part's place in the design before it was taken out, and the layout that took it out.</param>
    public static List<int> DropAt(int savedCount, int savedLayout, IEnumerable<(int index, int layout)> removed) =>
        removed.Where(r => r.layout > savedLayout && r.index >= 0 && r.index < savedCount)
            .Select(r => r.index).Distinct().OrderByDescending(i => i).ToList();

    /// <summary>The places parts stood before these were taken out (their indexes in the full list), given the full list's keys and the keys to
    /// take out. Returns them in the list's order.</summary>
    public static List<int> PlacesOf(IReadOnlyList<string> keys, ICollection<string> takenOut)
    {
        var places = new List<int>();
        for (int i = 0; i < keys.Count; i++)
        {
            if (takenOut.Contains(keys[i])) places.Add(i);
        }
        return places;
    }
}
