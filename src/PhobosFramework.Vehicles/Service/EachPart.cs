using System.Collections.Generic;
using System.Linq;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Parts a loadout adds one for each part of another role (Framework 0.250.0, <see cref="PartToAdd.eachOf"/>; per-engine realism round
/// two, the owner's card of 23 Nov 2026: brakes one per wheel, so heavy-duty brakes can go on an axle as a pair). The first part keeps the
/// loadout's key and its place in the list; the others wait until every other part we add is placed and are appended last, because Vehicle
/// Framework matches saved parts to the design by position. A vehicle saved with one part gains the others at its first load, each taking
/// the first part's health (<see cref="HealthFrom"/>, read by PartsOnLoad).
/// </summary>
public static class EachPart
{
    /// <summary>For each design, the parts appended by eachOf and the key of the part whose health they take on an older save. Built at
    /// start-up with the designs, never saved.</summary>
    static readonly Dictionary<VehicleDef, Dictionary<string, string>> healthFrom = new();

    /// <summary>The parts of <paramref name="add"/>'s eachOf role, by key, or null when there are fewer than two (one part as before).</summary>
    public static List<string>? Targets(PartToAdd add, CompProperties_PhobosVehicleSystems props) =>
        add.eachOf != null && props.roleKeys.TryGetValue(add.eachOf, out List<string>? keys) && keys.Count >= 2 ? keys : null;

    /// <summary>The label of the part at <paramref name="targetKey"/>: the loadout's eachLabel with the target's label in it.</summary>
    public static string Label(VehicleDef def, PartToAdd add, string targetKey)
    {
        if (add.eachLabel.NullOrEmpty()) return add.label;
        string target = def.components.FirstOrDefault(p => p.key == targetKey)?.label ?? targetKey;
        return add.eachLabel!.Replace("{PART}", target);
    }

    /// <summary>The key of the part at the <paramref name="index"/>th target: the loadout's own key first, then "key_target".</summary>
    public static string KeyFor(PartToAdd add, string targetKey, int index) => index == 0 ? add.key : add.key + "_" + targetKey;

    /// <summary>Appends the waiting parts after every other part we add, binds them to their role and notes whose health they take. Returns
    /// how many were added.</summary>
    public static int AppendWaiting(VehicleDef def, CompProperties_PhobosVehicleSystems props, List<(string role, PartToAdd add, List<string> targets)> waiting)
    {
        int added = 0;
        foreach ((string role, PartToAdd add, List<string> targets) in waiting)
        {
            for (int i = 1; i < targets.Count; i++)
            {
                string key = KeyFor(add, targets[i], i);
                if (def.components.Any(p => p.key == key))
                {
                    VehicleSystems.Problems.Add(def.defName + ": already has a part called " + key);
                    continue;
                }
                VehicleComponentProperties part = VehicleSystems.MakePart(def, add, props, targets[i], Label(def, add, targets[i]), key);
                def.components.Add(part);
                props.roleKeys[role].Add(key);
                props.addedKeys.Add(key);
                if (!add.countsTowardDestruction) props.uncountedKeys.Add(key);
                if (!healthFrom.TryGetValue(def, out Dictionary<string, string>? from)) healthFrom[def] = from = new Dictionary<string, string>();
                from[key] = add.key;
                added++;
            }
        }
        return added;
    }

    /// <summary>The part whose health a part added to an older save takes, or null for a part born whole.</summary>
    public static string? HealthFrom(VehicleDef def, string key) =>
        healthFrom.TryGetValue(def, out Dictionary<string, string>? from) && from.TryGetValue(key, out string? source) ? source : null;
}
