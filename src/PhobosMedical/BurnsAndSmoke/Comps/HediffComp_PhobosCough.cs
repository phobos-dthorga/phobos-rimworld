using System.Runtime.CompilerServices;
using Phobos.BurnsAndSmoke.Defs;
using Phobos.Framework.Sounds;
using RimWorld;
using Verse;
using Verse.Sound;

namespace Phobos.BurnsAndSmoke.Comps;

/// <summary>Now and then a cough from someone with this condition (owner, 11 Nov 2026: coughing, yes, but never doubled with another audio
/// mod's). The worse the condition, the more often. Presentation only: it changes nothing about the pawn.</summary>
public class HediffCompProperties_PhobosCough : HediffCompProperties
{
    public SoundDef? sound;
    /// <summary>The mean time between coughs at full severity, in game hours; less severe conditions cough less often (authored).</summary>
    public float mtbHoursAtFull = 2f;
    /// <summary>No coughing below this severity (authored).</summary>
    public float minSeverity = 0.1f;

    public HediffCompProperties_PhobosCough() => compClass = typeof(HediffComp_PhobosCough);
}

public class HediffComp_PhobosCough : HediffComp
{
    const int CheckIntervalTicks = 250;
    /// <summary>One pawn coughs at most this often, however many of its conditions make it cough (authored).</summary>
    const int PawnCooldownTicks = 600;
    const float LeastSeverityForRate = 0.1f;

    static readonly ConditionalWeakTable<Pawn, StrongBox<int>> LastCough = new();

    public HediffCompProperties_PhobosCough Props => (HediffCompProperties_PhobosCough)props;

    public override void CompPostTickInterval(ref float severityAdjustment, int delta)
    {
        base.CompPostTickInterval(ref severityAdjustment, delta);
        if (!Pawn.IsHashIntervalTick(CheckIntervalTicks, delta) || Props.sound == null || !CoughSounds.On) return;
        if (!Pawn.Spawned || Pawn.Dead || !Pawn.RaceProps.Humanlike || !Pawn.Awake() || parent.Severity < Props.minSeverity) return;
        float mtbHours = Props.mtbHoursAtFull / UnityEngine.Mathf.Max(parent.Severity, LeastSeverityForRate);
        if (!Rand.MTBEventOccurs(mtbHours, GenDate.TicksPerHour, CheckIntervalTicks)) return;
        int now = Find.TickManager.TicksGame;
        StrongBox<int> last = LastCough.GetValue(Pawn, _ => new StrongBox<int>(int.MinValue / 2));
        if (now - last.Value < PawnCooldownTicks) return;
        last.Value = now;
        Props.sound.PlayOneShot(new TargetInfo(Pawn.Position, Pawn.Map));
    }
}

/// <summary>Whether coughs play: the player's setting, and no other mod already giving pawns coughs (the rules Def names the words and
/// mods to look for). Worked out once Defs are loaded, by the load line.</summary>
public static class CoughSounds
{
    /// <summary>The other mod that already sounds coughs, or null.</summary>
    public static string? StoodDownFor { get; private set; }

    public static bool On => BurnsAndSmokeMod.Settings.coughSounds && StoodDownFor == null;

    internal static void Decide()
    {
        PhobosBurnRulesDef rules = PhobosBurnRulesDef.Current;
        StoodDownFor = SoundsElsewhere.FirstMod(rules.coughSoundWords, rules.coughSoundMods, BurnsAndSmokeMod.Pack);
    }
}
