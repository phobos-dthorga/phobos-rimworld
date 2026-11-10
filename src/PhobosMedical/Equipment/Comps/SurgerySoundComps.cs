using System.Collections.Generic;
using Phobos.Framework.Sounds;
using RimWorld;
using Verse;

namespace Phobos.Medical.Comps;

/// <summary>A sound a bed's facility makes while an operation is under way in a bed it is linked to (the anaesthesia machine's bellows).
/// Presentation only. Other mods can give their own facilities one in XML.</summary>
public class CompProperties_PhobosSurgerySound : CompProperties
{
    public SoundDef? sound;

    public CompProperties_PhobosSurgerySound() => compClass = typeof(CompPhobosSurgerySound);
}

public class CompPhobosSurgerySound : ThingComp
{
    readonly SustainedSound loop = new();

    public CompProperties_PhobosSurgerySound Props => (CompProperties_PhobosSurgerySound)props;

    public override void CompTickRare()
    {
        base.CompTickRare();
        loop.Set(Props.sound, Powered && OperationUnderWay(), parent);
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        loop.End();
        base.PostDeSpawn(map, mode);
    }

    bool Powered => parent.TryGetComp<CompPowerTrader>() is not { } power || power.PowerOn;

    /// <summary>A doctor at work on a bill for someone lying in one of the linked beds: the game's surgery is a bill on the patient.</summary>
    bool OperationUnderWay()
    {
        if (!parent.Spawned || parent.TryGetComp<CompFacility>()?.LinkedBuildings is not { Count: > 0 } beds) return false;
        IReadOnlyList<Pawn> pawns = parent.Map.mapPawns.AllPawnsSpawned;
        foreach (Thing thing in beds)
        {
            if (thing is not Building_Bed bed) continue;
            foreach (Pawn patient in bed.CurOccupants)
            {
                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn doctor = pawns[i];
                    if (doctor.CurJobDef == JobDefOf.DoBill && doctor.CurJob.targetA.Thing == patient && doctor.Position.AdjacentTo8WayOrInside(patient.Position)) return true;
                }
            }
        }
        return false;
    }
}
