using RimWorld;
using Verse;

namespace Phobos.Dosing.Defs;

[DefOf]
public static class DosingDefOf
{
    public static JobDef PhobosDose = null!;

    static DosingDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(DosingDefOf));
}
