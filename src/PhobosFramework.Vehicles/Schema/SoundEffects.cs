using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Phobos.Framework;
using Phobos.Framework.Sounds;
using RimWorld;
using Vehicles;
using Verse;
using Verse.Sound;

namespace Phobos.VehicleSystems;

/// <summary>
/// The vehicle systems' sounds (docs/development/sound-catalogue.md, the generic library): which vehicles may play them, a petrol engine's
/// own take where one exists, the loops a stage keeps going, and the warning chime. Presentation only: nothing here changes what a vehicle
/// does. Other mods' vehicles running our systems play them too unless the player turns that off (owner, 10 Nov 2026).
/// </summary>
public static class VehicleSystemSounds
{
    /// <summary>The start of every package ID of ours, for telling our vehicles from other mods'.</summary>
    const string OurPackagePrefix = "PhobosDthorga.";
    /// <summary>After a vehicle spawns or a save loads, its stages are entered again (they are not saved), so one-shot sounds and the chime
    /// wait this long, or a save with a burst tyre would replay the burst. Loops resume at once.</summary>
    const int QuietAfterSpawnTicks = 600;
    /// <summary>The least time between two chimes from one vehicle, so several lamps lighting together chime once.</summary>
    const int ChimeCooldownTicks = 600;

    static readonly ConditionalWeakTable<CompPhobosVehicleSystems, Dictionary<object, SustainedSound>> Loops = new();
    static readonly ConditionalWeakTable<CompPhobosVehicleSystems, StrongBox<int>> QuietUntil = new();
    static readonly ConditionalWeakTable<CompPhobosVehicleSystems, StrongBox<int>> LastChime = new();

    public static bool IsOurs(VehiclePawn vehicle) =>
        vehicle.def.modContentPack?.PackageId?.StartsWith(OurPackagePrefix, StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>Whether this vehicle may play the systems' sounds now.</summary>
    public static bool Allowed(VehiclePawn vehicle) =>
        vehicle.Spawned && (FrameworkMod.Settings.systemSoundsOnOtherVehicles || IsOurs(vehicle));

    /// <summary>The sound for this vehicle's engine: the petrol take when it burns petrol and one is given, else the main one.</summary>
    public static SoundDef? ForEngine(CompPhobosVehicleSystems comp, SoundDef? sound, SoundDef? soundPetrol) =>
        soundPetrol != null && comp.EngineFuel == EngineFuel.Petrol ? soundPetrol : sound;

    /// <summary>Called when the vehicle spawns or loads: one-shots and the chime wait a moment while its stages are entered again.</summary>
    public static void Settle(CompPhobosVehicleSystems comp)
    {
        int until = Find.TickManager.TicksGame + QuietAfterSpawnTicks;
        if (QuietUntil.TryGetValue(comp, out StrongBox<int>? box)) box.Value = until;
        else QuietUntil.Add(comp, new StrongBox<int>(until));
    }

    static bool Settling(CompPhobosVehicleSystems comp) =>
        QuietUntil.TryGetValue(comp, out StrongBox<int>? box) && Find.TickManager.TicksGame < box.Value;

    /// <summary>Plays a sound once at the vehicle, unless it is still settling after a load.</summary>
    public static void PlayOnce(CompPhobosVehicleSystems comp, SoundDef? sound)
    {
        VehiclePawn vehicle = comp.Vehicle;
        if (sound == null || !Allowed(vehicle) || Settling(comp)) return;
        sound.PlayOneShot(new TargetInfo(vehicle.Position, vehicle.Map));
    }

    /// <summary>Keeps one loop of this vehicle's, named by key, playing while on holds.</summary>
    public static void Loop(CompPhobosVehicleSystems comp, object key, SoundDef? sound, bool on)
    {
        Dictionary<object, SustainedSound> loops = Loops.GetOrCreateValue(comp);
        if (!loops.TryGetValue(key, out SustainedSound? loop)) loops[key] = loop = new SustainedSound();
        loop.Set(sound, on && Allowed(comp.Vehicle), comp.Vehicle);
    }

    /// <summary>Ends every loop of this vehicle's, when it leaves the map.</summary>
    public static void EndAll(CompPhobosVehicleSystems comp)
    {
        if (!Loops.TryGetValue(comp, out Dictionary<object, SustainedSound>? loops)) return;
        foreach (SustainedSound loop in loops.Values) loop.End();
    }

    /// <summary>One soft chime when a red lamp lights on one of the player's vehicles (owner, 10 Nov 2026: red lamps only, once).</summary>
    public static void Chime(CompPhobosVehicleSystems comp)
    {
        if (comp.Vehicle.Faction != Faction.OfPlayer || Settling(comp)) return;
        int now = Find.TickManager.TicksGame;
        StrongBox<int> last = LastChime.GetValue(comp, _ => new StrongBox<int>(int.MinValue / 2));
        if (now - last.Value < ChimeCooldownTicks) return;
        last.Value = now;
        PlayOnce(comp, VehicleSystemSoundDefOf.PhobosVehicleSystems_WarningChime);
    }
}

[DefOf]
public static class VehicleSystemSoundDefOf
{
    public static SoundDef PhobosVehicleSystems_WarningChime = null!;
    public static SoundDef PhobosVehicleSystems_JumpStartKit = null!;
    public static SoundDef PhobosVehicleSystems_BoosterPack = null!;

    static VehicleSystemSoundDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(VehicleSystemSoundDefOf));
}

/// <summary>Plays a sound once when the stage starts or the event fires (a tyre bursting, air brakes recharging after a stop). A petrol
/// engine plays <c>soundPetrol</c> when given. Not a consequence, so it plays whatever the player's figures-only setting.</summary>
public class Effect_Sound : VehicleSystemEffect
{
    public SoundDef? sound;
    public SoundDef? soundPetrol;
    public override bool Consequential => false;

    public override void OnEnter(SystemContext context) =>
        VehicleSystemSounds.PlayOnce(context.Comp, VehicleSystemSounds.ForEngine(context.Comp, sound, soundPetrol));
}

/// <summary>Keeps a sound looping while the stage lasts (coolant boiling, an engine knocking), and with <c>whileEngineRunning</c> only while
/// the engine runs. It picks up again after a load at the next update.</summary>
public class Effect_SoundLoop : VehicleSystemEffect
{
    public SoundDef? sound;
    public SoundDef? soundPetrol;
    public bool whileEngineRunning;
    public override bool Consequential => false;

    public override void OnEnter(SystemContext context) => Update(context);
    public override void Apply(SystemContext context, float hours) => Update(context);
    public override void OnExit(SystemContext context) => VehicleSystemSounds.Loop(context.Comp, this, null, false);

    void Update(SystemContext context) =>
        VehicleSystemSounds.Loop(context.Comp, this, VehicleSystemSounds.ForEngine(context.Comp, sound, soundPetrol),
            !whileEngineRunning || context.Comp.EngineRunning);
}
