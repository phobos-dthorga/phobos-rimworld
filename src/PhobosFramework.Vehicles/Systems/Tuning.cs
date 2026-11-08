using System;
using System.Collections.Generic;
using System.Reflection;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>Marks a field of <see cref="CompProperties_PhobosVehicleSystems"/> as the player's tuning of one system's parameter.</summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class TunesParameterAttribute : Attribute
{
    public readonly string system;
    public readonly string parameter;

    public TunesParameterAttribute(string system, string parameter)
    {
        this.system = system;
        this.parameter = parameter;
    }
}

/// <summary>
/// The figures a player may tune per vehicle in Vehicle Framework's own settings window. Vehicle Framework shows fields, not
/// dictionaries, so each tunable parameter of the shipped systems has a field on the comp's properties: the service fills it with
/// the vehicle's own value while Defs load, Vehicle Framework snapshots that as the default and saves the player's change in its
/// own settings file, and the systems read the change back through Vehicle Framework's settings cache. Another mod's own
/// parameters are tuned in its loadouts, not here.
/// </summary>
public static class Tuning
{
    /// <summary>The "system" name for the drivetrain's own tunable figures, which no system Def owns.</summary>
    public const string Drivetrain = "drivetrain";
    public const string EmptyKilograms = "emptyKilograms";

    static Dictionary<(string system, string parameter), FieldInfo>? fields;

    static Dictionary<(string, string), FieldInfo> Fields
    {
        get
        {
            if (fields != null) return fields;
            fields = new Dictionary<(string, string), FieldInfo>();
            foreach (FieldInfo info in typeof(CompProperties_PhobosVehicleSystems).GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                TunesParameterAttribute? tunes = info.GetCustomAttribute<TunesParameterAttribute>();
                if (tunes != null) fields[(tunes.system, tunes.parameter)] = info;
            }
            return fields;
        }
    }

    /// <summary>Fills the tuning fields with this design's values (or the system's defaults where it lacks the system).</summary>
    public static void Fill(CompProperties_PhobosVehicleSystems props, VehicleDef vehicleDef)
    {
        foreach (KeyValuePair<(string system, string parameter), FieldInfo> pair in Fields)
        {
            if (pair.Key.system == Drivetrain)
            {
                if (pair.Key.parameter == EmptyKilograms && props.drivetrain?.def != null)
                {
                    pair.Value.SetValue(props, (float)Powertrain.KerbFor(vehicleDef, props.drivetrain.def, props.drivetrain.set));
                }
                continue;
            }
            double? value = null;
            foreach (ResolvedSystem system in props.systems)
            {
                if (system.def.defName == pair.Key.system && system.parameters.TryGetValue(pair.Key.parameter, out double found)) value = found;
            }
            if (value == null)
            {
                PhobosVehicleSystemDef? def = DefDatabase<PhobosVehicleSystemDef>.GetNamedSilentFail(pair.Key.system);
                if (def != null && def.parameters.values.TryGetValue(pair.Key.parameter, out float fallback)) value = fallback;
            }
            if (value != null) pair.Value.SetValue(props, (float)value.Value);
        }
    }

    /// <summary>The parameter as the player tuned it for this vehicle design, or <paramref name="resolved"/>.</summary>
    public static double Value(VehicleDef? def, string system, string parameter, double resolved)
    {
        if (def == null || !Fields.TryGetValue((system, parameter), out FieldInfo? info)) return resolved;
        try
        {
            return SettingsCache.TryGetValue(def, typeof(CompProperties_PhobosVehicleSystems), info.Name, (float)resolved);
        }
        catch (Exception)
        {
            return resolved;
        }
    }
}
