using Phobos.Framework.Environment;
using Phobos.Framework.Rules;
using RimWorld;
using Verse;

namespace Phobos.Framework.Power;

/// <summary>Standby power on one kind of building (Framework 0.222.0): which kind it is. Put on by <see cref="StandbyKinds"/> at start-up.</summary>
public class CompProperties_PhobosStandby : CompProperties
{
    public StandbyKind kind;

    public CompProperties_PhobosStandby()
    {
        compClass = typeof(CompPhobosStandby);
    }
}

/// <summary>
/// For modders (Framework 0.222.0): <c>never</c> keeps standby power off a building of yours that standby power would otherwise take up,
/// and <c>kind</c> takes one up that it would not find by itself, as that kind. Either way the building needs the game's own power comp.
/// </summary>
public class PhobosStandbyExtension : DefModExtension
{
    public bool never;
    public StandbyKind? kind;
}

/// <summary>
/// Standby power on one building (Framework 0.222.0; the owner's choices of 22 Nov 2026, docs/development/standby-power.md). The building
/// draws its listed power while in use and its standby share while idle; a light that is off draws nothing and goes dark. The map's
/// once-a-second look (<see cref="MapComponent_PhobosStandby"/>) decides "in use" and calls <see cref="Settle"/>; nothing here runs
/// every tick. The draw is written into the game's own power comp only when it changes, and the game's power grid reads it as it reads
/// any other. A light asks the game to light it again through the game's own question, <see cref="IThingGlower"/>, so no patch is needed.
/// Nothing is saved: the game sets every building's draw from its Def again when a map loads, and the next look sets ours.
/// </summary>
public class CompPhobosStandby : ThingComp, IThingGlower
{
    static readonly Phobos.Logging.PhobosLog Logger = Logging.Logs.For("D'thorga Framework", "Standby power");

    CompPowerTrader? power;
    CompGlower? glower;
    CompFacility? facility;
    /// <summary>In use as last settled; a building starts in use (lit, full power) until the first look.</summary>
    bool inUse = true;
    /// <summary>The draw last written, watts; below zero before the first.</summary>
    float lastWritten = -1f;
    /// <summary>Another mod sets this building's draw itself: standby power leaves it alone until it is built again.</summary>
    bool standsDown;

    public CompProperties_PhobosStandby Props => (CompProperties_PhobosStandby)props;
    public StandbyKind Kind => Props.kind;
    public bool InUse => inUse;
    public CompFacility? Facility => facility;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        power = parent.GetComp<CompPowerTrader>();
        glower = parent.GetComp<CompGlower>();
        facility = parent.GetComp<CompFacility>();
        MapRegistry<CompPhobosStandby>.Add(parent.Map, this);
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        base.PostDeSpawn(map, mode);
        MapRegistry<CompPhobosStandby>.Remove(map, this);
        inUse = true;
        lastWritten = -1f;
    }

    /// <summary>The game's question to every comp of a light: lit unless this is a light no one is using.</summary>
    public bool ShouldBeLitNow() => Kind != StandbyKind.Light || inUse;

    /// <summary>
    /// Sets the building in use or idle and writes its draw if it differs from what the grid has. A draw neither ours nor the game's means
    /// another mod sets it: standby power then stands down for this building, at full power and lit.
    /// </summary>
    internal void Settle(bool wantInUse, int standbyPercent)
    {
        if (standsDown || power == null) return;
        float listed = power.Props.PowerConsumption;
        // The field, not PowerOutput: the property reads nothing while the building is stunned by an EMP.
        float found = -power.powerOutputInt;
        if (!StandbyPowerRule.DrawIsOursOrTheGames(found, lastWritten, listed))
        {
            standsDown = true;
            if (Logger.DebugEnabled) Logger.Debug(parent.LabelShort + " (" + parent.def.defName + "): its draw is set elsewhere (" + found + " W), left alone");
            SetInUse(true);
            return;
        }
        SetInUse(wantInUse);
        float draw = StandbyPowerRule.Draw(listed, inUse, Kind, standbyPercent);
        if (lastWritten < 0f || !StandbyPowerRule.Same(found, draw))
        {
            power.PowerOutput = -draw;
            lastWritten = draw;
        }
    }

    void SetInUse(bool now)
    {
        if (now == inUse) return;
        inUse = now;
        if (Kind == StandbyKind.Light && glower != null && parent.Spawned) glower.UpdateLit(parent.Map);
        if (now)
        {
            if (Seen.Wants("framework.standby.woke", parent)) Seen.Note("framework.standby.woke", parent, null, Kind.ToString());
        }
        else if (Seen.Wants("framework.standby.idle", parent))
        {
            Seen.Note("framework.standby.idle", parent, null, Kind.ToString());
        }
        if (Logger.DebugEnabled) Logger.Debug(parent.LabelShort + ": " + (now ? "in use" : "standby") + " (" + Kind + ")");
    }

    public override string? CompInspectStringExtra()
    {
        if (inUse || standsDown || power == null || !power.PowerOn) return null;
        return Kind switch
        {
            StandbyKind.Light => "PhobosFramework_StandbyDark".Translate(),
            StandbyKind.Turret => "PhobosFramework_StandbyNoTarget".Translate(),
            StandbyKind.Grower => "PhobosFramework_StandbyNothingGrowing".Translate(),
            StandbyKind.Facility => "PhobosFramework_StandbyNothingServed".Translate(),
            StandbyKind.Door => "PhobosFramework_StandbyClosed".Translate(),
            _ => "PhobosFramework_StandbyIdle".Translate(),
        };
    }
}
