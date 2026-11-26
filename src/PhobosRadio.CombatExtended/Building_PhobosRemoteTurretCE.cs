using CombatExtended;
using RimWorld;
using Verse;

namespace Phobos.Radio.CombatExtended;

/// <summary>
/// The remote gun turret under Combat Extended (Radio 0.19.0): Combat Extended's own turret, with its magazine and reloading, holding
/// fire unless <see cref="CompRemoteTurret.MayFire"/>, as <see cref="Building_PhobosRemoteTurret"/> does for the game's turret. It looks
/// for no target while it may not fire, and takes a forced target only while a station's operator watches it. No patch: Combat
/// Extended's turret leaves these to its subclasses, as the game's does. Its patches swap it in for the radio's own class.
/// </summary>
public class Building_PhobosRemoteTurretCE : Building_TurretGunCE
{
    CompRemoteTurret? remote;

    CompRemoteTurret? Remote => remote ??= GetComp<CompRemoteTurret>();

    public override LocalTargetInfo TryFindNewTarget() => Remote is { MayFire: false } ? LocalTargetInfo.Invalid : base.TryFindNewTarget();

    protected override bool CanSetForcedTarget => Faction == Faction.OfPlayer && Remote is { Watched: true };

    public override void BeginBurst()
    {
        if (Remote is { MayFire: false }) return;
        // Seen in play (Radio 0.18.0 and 0.19.0, row 540): a base turret firing, watched by a station or on its own.
        if (Remote is { } watched && Seen.Wants("radio.remote-turret", this, watched.Watched ? "watched" : "alone"))
        {
            Seen.Note("radio.remote-turret", this, LabelShort, watched.Watched ? "watched" : "alone");
        }
        base.BeginBurst();
    }
}
