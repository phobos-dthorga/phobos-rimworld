using Phobos.Framework.Rules;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A turbine aircraft's intake separator, and the inlet barrier filter's clogging (Framework 0.365.0; docs/development/vehicle-inlet-separator.md;
/// the owner's answers of 28 Nov 2026: a switch, automatic by default; the barrier clogs and the compressor wash cleans it). The separator
/// is on aircraft whose engines are turboshafts (turboprops and helicopters, as our model draws them) with no barrier fitted; while on it
/// keeps out half the landing dust, grit and rough-ground fouling, for 3 % more asked of the engines. A fitted barrier instead clogs on
/// rough ground (the compressor wash system's <see cref="ClogQuantity"/>), its power cost rising as it does. Both act through what exists:
/// the powertrain's demand, the landing surface, the grit rule and the fouling input read <see cref="Reaching"/> and <see cref="ExtraDemand"/>.
/// </summary>
public sealed class IntakeSeparator
{
    public const string ClogQuantity = "barrierClog";

    readonly CompPhobosVehicleSystems comp;
    /// <summary>The switch's position, saved by the comp.</summary>
    internal SeparatorMode mode = SeparatorMode.Automatic;
    /// <summary>Whether it is on, as last worked out (each update, a press, a touch-down or a lift-off); derived, never saved.</summary>
    bool onNow;
    /// <summary>Whether it has a separator, as last worked out; derived, never saved.</summary>
    bool fittedNow;

    public IntakeSeparator(CompPhobosVehicleSystems comp) => this.comp = comp;

    public SeparatorMode Mode => mode;

    /// <summary>The barrier filter's fitted share of power, 0 without one.</summary>
    float BarrierCost => comp.Vehicle?.GetStatValue(AircraftUpgradeDefOf.Phobos_InletBarrierLoss) ?? 0f;

    /// <summary>Whether it has a separator, as last worked out: for the Dashboard's switch, which never computes it.</summary>
    public bool Fitted => fittedNow;

    /// <summary>Whether this aircraft has a separator: the setting on, a player's aircraft followed in full, turboshaft engines, and no barrier.</summary>
    bool HasOne => VehicleSystemsMod.Settings.intakeSeparators && comp.Tier == DetailTier.Full
        && comp.Powertrain is { IsAircraft: true, Def: { } def } && def.engine.kind == EngineKind.Turboshaft && BarrierCost <= 0f;

    /// <summary>Whether the separator is on, as last worked out: read by the tabs and the powertrain, which never compute it.</summary>
    public bool On => onNow;

    /// <summary>Works out afresh whether it is on: at a touch-down or a lift-off, where the ground under it has just changed.</summary>
    public bool Refresh()
    {
        fittedNow = HasOne;
        bool on = fittedNow && SeparatorRule.On(mode, comp.Vehicle.Spawned, StripDebris.RoughShare(comp.Vehicle));
        if (on && !onNow && Seen.Wants("vehicles.intake-separator", comp.Vehicle, mode.ToString())) Seen.Note("vehicles.intake-separator", comp.Vehicle, null, mode.ToString());
        onNow = on;
        return on;
    }

    /// <summary>What reaches the engines of the dust, grit and fouling a separator keeps out: half while it is on, else all.</summary>
    public double Reaching => SeparatorRule.Reaching(onNow);

    /// <summary><see cref="Reaching"/>, worked out afresh first: for a touch-down or a lift-off.</summary>
    public double ReachingNow() => SeparatorRule.Reaching(Refresh());

    /// <summary>The intake's extra demand on the engines now: the separator while on, and a clogged barrier's extra cost.</summary>
    public double ExtraDemand
    {
        get
        {
            if (!VehicleSystemsMod.Settings.intakeSeparators) return 0;
            float clog = comp.TryGetQuantity(ClogQuantity, out float c) ? c : 0f;
            return SeparatorRule.ExtraDemand(onNow, BarrierCost, clog);
        }
    }

    /// <summary>The switch's button: automatic, held on, held off, and round again.</summary>
    public void Press()
    {
        mode = SeparatorRule.Next(mode);
        Refresh();
        Messages.Message(SetTo(mode, comp.Vehicle.LabelShort).Resolve(), comp.Vehicle, MessageTypeDefOf.SilentInput, historical: false);
    }

    /// <summary>At each update: whether it is on, for everything that reads it until the next.</summary>
    public void Update() => Refresh();

    /// <summary>What a press says it did, by the switch's new position (literal keys, so the text check reads them).</summary>
    static TaggedString SetTo(SeparatorMode mode, string vehicle) => mode switch
    {
        SeparatorMode.On => "PhobosVehicleSystems_SeparatorSetToOn".Translate(vehicle.Named("VEHICLE")),
        SeparatorMode.Off => "PhobosVehicleSystems_SeparatorSetToOff".Translate(vehicle.Named("VEHICLE")),
        _ => "PhobosVehicleSystems_SeparatorSetToAutomatic".Translate(vehicle.Named("VEHICLE")),
    };

    /// <summary>The switch's position in words, for its button.</summary>
    public string ModeLabel => mode switch
    {
        SeparatorMode.On => "PhobosVehicleSystems_SeparatorModeOn".Translate(),
        SeparatorMode.Off => "PhobosVehicleSystems_SeparatorModeOff".Translate(),
        _ => "PhobosVehicleSystems_SeparatorModeAutomatic".Translate(),
    };
}
