using System;
using Phobos.Radio.Physics;
using Phobos.Radio.Services;
using PipeSystem;
using RimWorld;
using Verse;

namespace Phobos.Radio;

/// <summary>
/// The remote guns' "fire on their own" switch, from outside the radio (Radio 0.18.0): D'thorga Framework keeps it in its Vehicle
/// Framework part, which the radio's Vehicle Framework tie-in hands over at start-up. Without Vehicle Framework there is no switch, and a
/// base turret fires only while a station watches it [A].
/// </summary>
public static class RemoteTurrets
{
    public static Func<bool>? FireAloneSwitch;

    public static bool FireAlone => FireAloneSwitch?.Invoke() == true;
}

/// <summary>
/// A base turret worked from a radio station (Radio 0.18.0; docs/development/autonomous-vehicles.md, the base turret). It takes no figures:
/// the hands an operator has are the radio rules' vehicles per operator, and the gun is its turret Def's.
/// </summary>
public class CompProperties_RemoteTurret : CompProperties
{
    public CompProperties_RemoteTurret() => compClass = typeof(CompRemoteTurret);
}

/// <summary>
/// A turret that fires only while a radio station's operator watches it, or when remote guns may fire on their own (the owner's rule of
/// 25 Nov 2026). Watched means: on the feedline of a powered station whose console a colonist works, and among that operator's hands,
/// after the station's vehicles (the owner's card of 26 Nov 2026). The net works that out in its own pass; this reads its answer.
/// <para>Cost: registering with the net on spawn; one set lookup when the gun looks for a target.</para>
/// </summary>
public class CompRemoteTurret : ThingComp
{
    public CompResource? Port => parent.TryGetComp<CompFeedlinePort>();

    RadioNet? Net => parent.Spawned ? RadioNet.For(parent.Map) : null;

    /// <summary>Whether a station's operator watches it now.</summary>
    public bool Watched => Net?.TurretWatched(parent) == true;

    public bool MayFire => RemoteTurretRule.MayFire(Watched, RemoteTurrets.FireAlone);

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        Net?.AddTurret(this);
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        RadioNet.For(map)?.RemoveTurret(this);
        base.PostDeSpawn(map, mode);
    }

    public override string CompInspectStringExtra()
    {
        if (Net is not { } net) return string.Empty;
        Thing? station = net.TurretStation(parent);
        if (station == null)
        {
            return RemoteTurrets.FireAlone ? "PhobosRadio_TurretFiresAlone".Translate() : "PhobosRadio_TurretNoStation".Translate();
        }
        if (Watched) return "PhobosRadio_TurretWatched".Translate(station.LabelShort.Named("STATION"));
        if (RemoteTurrets.FireAlone) return "PhobosRadio_TurretFiresAlone".Translate();
        return net.OperatorAt(station) == null
            ? "PhobosRadio_TurretNoOperator".Translate(station.LabelShort.Named("STATION"))
            : "PhobosRadio_TurretNoHand".Translate(station.LabelShort.Named("STATION"));
    }
}

/// <summary>
/// The base turret building (Radio 0.18.0): the game's own turret, holding fire unless <see cref="CompRemoteTurret.MayFire"/>. It looks
/// for no target while it may not fire, and a player may aim it by hand only while a station's operator watches it, through the picture.
/// No patch: the game's turret leaves these to its subclasses.
/// </summary>
public class Building_PhobosRemoteTurret : Building_TurretGun
{
    CompRemoteTurret? remote;

    CompRemoteTurret? Remote => remote ??= GetComp<CompRemoteTurret>();

    public override LocalTargetInfo TryFindNewTarget() => Remote is { MayFire: false } ? LocalTargetInfo.Invalid : base.TryFindNewTarget();

    protected override bool CanSetForcedTarget => Faction == Faction.OfPlayer && Remote is { Watched: true };

    protected override void BeginBurst()
    {
        if (Remote is { MayFire: false }) return;
        // Seen in play (Radio 0.18.0, row 540): a base turret firing, watched by a station or on its own.
        if (Remote is { } watched && Seen.Wants("radio.remote-turret", this, watched.Watched ? "watched" : "alone"))
        {
            Seen.Note("radio.remote-turret", this, LabelShort, watched.Watched ? "watched" : "alone");
        }
        base.BeginBurst();
    }
}
