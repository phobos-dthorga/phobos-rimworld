using Verse;

namespace Phobos.Framework.Defs;

/// <summary>
/// The base of every "rules" Def: the starting values a design says to tune in play, as one Def whose first loaded instance
/// wins, so other mods can patch the figures in XML. Player preferences are mod settings, never rules. Subclass it, declare one
/// instance in XML, and read it through <see cref="Current{T}"/>.
/// </summary>
public abstract class PhobosRulesDef : Def
{
    /// <summary>The first loaded rules Def of a type, or a built-in default (with a warning) when none is loaded.</summary>
    public static T Current<T>() where T : PhobosRulesDef, new() => RulesCache<T>.Current;

    static class RulesCache<T> where T : PhobosRulesDef, new()
    {
        static T? current;

        public static T Current
        {
            get
            {
                if (current == null)
                {
                    var all = DefDatabase<T>.AllDefsListForReading;
                    if (all.Count > 0)
                    {
                        current = all[0];
                    }
                    else
                    {
                        current = new T { defName = typeof(T).Name + "_Fallback" };
                        FrameworkMod.Logger.Warn("No " + typeof(T).Name + " loaded; using built-in defaults.");
                    }
                }
                return current;
            }
        }
    }
}
