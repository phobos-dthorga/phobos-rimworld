using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Phobos.Framework.Sounds;
using RimWorld;
using Vehicles;
using Verse;
using Verse.Sound;

namespace Phobos.VehicleSystems;

/// <summary>One named sound of a vehicle's work, for <see cref="PhobosVehicleSoundsExtension"/>. With <c>node</c>, it plays only while that
/// upgrade is fitted; list such takes before the plain one, as the first that applies is played (the tipper's rock body, the rig's
/// hammer bit).</summary>
public class VehicleWorkSound
{
    public string key = "";
    public SoundDef? sound;
    public string? node;
}

/// <summary>
/// The sounds of a vehicle's own work (docs/development/sound-catalogue.md, phase 2): setting up, pumping, tipping, drilling, surveying,
/// diving. Named in XML by key on the VehicleDef; our comps ask for a key when the moment comes, and a vehicle whose Def names no sound for
/// it stays silent. Keys the comps use: SetUp, PackUp (falls back to SetUp), Work, WorkElectric, Portion, Spent, Console, Array, Dish, Find,
/// Hoses, Pump, Relief, Leak, Purge, Rams, Slide, Tailgate, Dive, Surface, Settle, LiftOff (falls back to Settle), Creak, Scrubber, Lab,
/// Contact. For other modders too: any vehicle with these comps may carry it.
/// </summary>
public class PhobosVehicleSoundsExtension : DefModExtension
{
    public List<VehicleWorkSound> sounds = new();

    /// <summary>The sound this vehicle plays for a key now, or null.</summary>
    public SoundDef? For(VehiclePawn vehicle, string key)
    {
        foreach (VehicleWorkSound entry in sounds)
        {
            if (entry.key == key && entry.sound != null && (entry.node == null || VehicleUpgrades.Unlocked(vehicle, entry.node))) return entry.sound;
        }
        return null;
    }
}

/// <summary>Whether a vehicle has an upgrade fitted, by its node's key.</summary>
public static class VehicleUpgrades
{
    public static bool Unlocked(VehiclePawn vehicle, string key) => vehicle.CompUpgradeTree is { } tree && Unlocked(tree, key);

    public static bool Unlocked(CompUpgradeTree tree, string key)
    {
        UpgradeNode? node = tree.Props.def.GetNode(key);
        return node != null && tree.NodeUnlocked(node);
    }
}

/// <summary>Plays a vehicle's work sounds by key. Presentation only: nothing here changes what a vehicle does, and nothing is saved; a loop
/// is set again from the vehicle's state at its comp's next look after a load.</summary>
public static class VehicleWorkSounds
{
    static readonly ConditionalWeakTable<VehiclePawn, Dictionary<string, SustainedSound>> Loops = new();

    /// <summary>The first of these keys this vehicle has a sound for now, or null.</summary>
    public static SoundDef? Find(VehiclePawn vehicle, params string[] keys)
    {
        if (vehicle.def.GetModExtension<PhobosVehicleSoundsExtension>() is not { } sounds) return null;
        foreach (string key in keys)
        {
            if (sounds.For(vehicle, key) is { } sound) return sound;
        }
        return null;
    }

    /// <summary>Plays the first of these keys this vehicle has a sound for, once, at the vehicle.</summary>
    public static void Play(VehiclePawn vehicle, params string[] keys)
    {
        if (!vehicle.Spawned || Find(vehicle, keys) is not { } sound) return;
        sound.PlayOneShot(new TargetInfo(vehicle.Position, vehicle.Map));
    }

    /// <summary>Keeps one loop of the vehicle's playing while on holds, from the first of these keys it has a sound for (the slot's own
    /// name when none are given). A change of sound, such as the rig going over to shore power, starts the new one in its place.</summary>
    public static void Loop(VehiclePawn vehicle, string slot, bool on, params string[] keys)
    {
        Dictionary<string, SustainedSound> loops = Loops.GetOrCreateValue(vehicle);
        if (!loops.TryGetValue(slot, out SustainedSound? loop))
        {
            if (!on) return;
            loops[slot] = loop = new SustainedSound();
        }
        loop.Set(on ? Find(vehicle, keys.Length == 0 ? new[] { slot } : keys) : null, on, vehicle);
    }

    /// <summary>Ends every work loop of this vehicle's, when it leaves the map.</summary>
    public static void EndAll(VehiclePawn vehicle)
    {
        if (!Loops.TryGetValue(vehicle, out Dictionary<string, SustainedSound>? loops)) return;
        foreach (SustainedSound loop in loops.Values) loop.End();
    }
}

[DefOf]
public static class VehicleWorkEffecterDefOf
{
    public static EffecterDef PhobosVehicleSystems_DrillDust = null!;

    static VehicleWorkEffecterDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(VehicleWorkEffecterDefOf));
}
