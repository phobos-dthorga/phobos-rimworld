using System.Collections.Generic;
using Phobos.Framework.Rules;
using Verse;

namespace Phobos.Framework.Persistence;

/// <summary>
/// Each person's hours of practice at each kind of thing (Framework 0.190.0; docs/development/vehicle-emergent-play.md, EM11): a kind is
/// any string a mod chooses, such as "vehicle/" and a vehicle's defName. Kept by the person's load ID, so a person who has died or left
/// keeps their hours without the save holding a reference to them. Read with <see cref="Practice"/>. A few dozen entries in an ordinary
/// colony; nothing here runs on a tick.
/// </summary>
public sealed class GameComponent_PhobosPractice : GameComponent
{
    Dictionary<string, float> hours = new();

    public GameComponent_PhobosPractice(Game game) { }

    /// <summary>The record of the game in play, or null before a game exists.</summary>
    public static GameComponent_PhobosPractice? Current => Verse.Current.Game?.GetComponent<GameComponent_PhobosPractice>();

    static string Key(Pawn pawn, string kind) => pawn.GetUniqueLoadID() + "|" + kind;

    public float HoursOf(Pawn pawn, string kind) => hours.TryGetValue(Key(pawn, kind), out float value) ? value : 0f;

    public void Add(Pawn pawn, string kind, float more)
    {
        if (more <= 0f) return;
        string key = Key(pawn, kind);
        hours[key] = (hours.TryGetValue(key, out float value) ? value : 0f) + more;
    }

    public override void ExposeData()
    {
        Scribe_Collections.Look(ref hours, "PhobosPractice_Hours", LookMode.Value, LookMode.Value);
        hours ??= new Dictionary<string, float>();
    }
}

/// <summary>A person's practice at a kind of thing: hours, and the share of the gain they give (<see cref="PracticeRule"/>).</summary>
public static class Practice
{
    public static float Hours(Pawn pawn, string kind) => GameComponent_PhobosPractice.Current?.HoursOf(pawn, kind) ?? 0f;

    public static float Share(Pawn pawn, string kind) => (float)PracticeRule.Share(Hours(pawn, kind));

    public static void Add(Pawn pawn, string kind, float hours) => GameComponent_PhobosPractice.Current?.Add(pawn, kind, hours);
}
