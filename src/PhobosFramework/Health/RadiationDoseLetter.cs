using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace Phobos.Framework.Health;

/// <summary>
/// One letter for the doses the player's people took from one blast (moved from D'thorga Patches' strike, Framework 0.358.0, so every
/// radiation burst tells it the same way; nuclear-strike-effects.md, NK13): each person in words and grays, as where they stood would
/// tell a doctor, grouped by person, the largest first. Doses under 0.05 Gy are left out.
/// </summary>
public static class RadiationDoseLetter
{
    const float LeastTold = 0.05f;

    /// <summary>Whether a dose to this pawn belongs on the letter: the player's own people.</summary>
    public static bool Tells(Pawn pawn) => pawn.Faction == Faction.OfPlayer && pawn.RaceProps.Humanlike;

    /// <summary>Sends the letter under <paramref name="label"/>; nothing when no one took enough to tell.</summary>
    public static void Send(IEnumerable<(Pawn pawn, float grays)> doses, TaggedString label)
    {
        var byPerson = new Dictionary<Pawn, float>();
        foreach ((Pawn pawn, float grays) in doses) byPerson[pawn] = (byPerson.TryGetValue(pawn, out float before) ? before : 0f) + grays;
        var text = new StringBuilder();
        foreach (KeyValuePair<Pawn, float> entry in byPerson.OrderByDescending(e => e.Value))
        {
            if (entry.Value < LeastTold) continue;
            text.Append('\n').Append("PhobosFramework_DoseLine".Translate(entry.Key.Named("PAWN"), Words(entry.Value).Named("BAND"),
                entry.Value.ToString("0.0").Named("GRAYS")).Resolve());
        }
        if (text.Length == 0) return;
        Find.LetterStack.ReceiveLetter(label, "PhobosFramework_DoseLetterText".Translate(text.ToString().Named("LIST")), LetterDefOf.NegativeEvent,
            new LookTargets(byPerson.Keys.Where(p => p.Spawned)));
    }

    /// <summary>The dose in words, by D'thorga Medical's bands: under 1 Gy light, 2 moderate, 4 heavy, 6 severe, then lethal.</summary>
    public static string Words(float grays) => (grays < 1f ? "PhobosFramework_DoseLight" : grays < 2f ? "PhobosFramework_DoseModerate"
        : grays < 4f ? "PhobosFramework_DoseHeavy" : grays < 6f ? "PhobosFramework_DoseSevere" : "PhobosFramework_DoseLethal").Translate();
}
