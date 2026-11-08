using Phobos.Framework.Rules;
using Verse;

namespace Phobos.Framework.Persistence;

/// <summary>
/// Saves and loads a history ring as one compact string (HistoryCodec). History cannot be recomputed, so saving it is the one
/// exception to "derived state is never saved", granted by the owner for the vehicle graphs (8 Nov 2026); a few hundred bytes per
/// quantity. A missing or malformed entry loads as an empty ring.
/// </summary>
public static class ScribeHistory
{
    public static void Look(ref HistoryRing? ring, string key, int capacity, float min, float max)
    {
        string? text = null;
        if (Scribe.mode == LoadSaveMode.Saving && ring != null && ring.Count > 0) text = HistoryCodec.Encode(ring, min, max);
        Scribe_Values.Look(ref text, key);
        if (Scribe.mode == LoadSaveMode.LoadingVars) ring = HistoryCodec.Decode(text, capacity, min, max);
    }
}
