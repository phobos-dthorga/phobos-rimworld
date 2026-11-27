using System.Collections.Generic;
using System.Linq;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A part for each engine, for a system that asks for one (Framework 0.360.0; docs/development/vehicle-standard.md, batch 3; the owner's
/// card of 27 Nov 2026: each aircraft engine its own fuel and oil pump). For every system of the design with a
/// <see cref="PhobosVehicleSystemDef.partPerEngine"/>, and every engine role the loadout bound (Engine1 to EngineN, else the one Engine), a
/// part sits among that engine's cells, bound to the role numbered as the engine is (Engine2's fuel pump plays FuelPump2), so the numbered
/// roles roll up into the plain one afterwards (FuelPump is every fuel pump). Appended after every other part we add, because Vehicle
/// Framework matches saved parts to the design by position: a saved aircraft gains them whole at its first load. A role the design's own
/// parts already play is left as it is.
/// </summary>
public static class EnginePumps
{
    /// <summary>Appends the parts, at start-up, after every other part we add and before the numbered roles roll up.</summary>
    internal static int Apply(VehicleDef def, CompProperties_PhobosVehicleSystems props)
    {
        if (props.trailer || def.components == null) return 0;
        int added = 0;
        List<string> engines = Enumerable.Range(1, Powertrain.MaxEngines).Select(i => "Engine" + i).Where(props.roleKeys.ContainsKey).ToList();
        bool numbered = engines.Count > 0;
        if (!numbered)
        {
            if (!props.roleKeys.ContainsKey("Engine")) return 0;
            engines.Add("Engine");
        }
        foreach (ResolvedSystem system in props.systems)
        {
            if (system.def.partPerEngine is not { add: { } add } each || each.role.NullOrEmpty()) continue;
            foreach (string engine in engines)
            {
                string number = numbered ? engine.Substring("Engine".Length) : "";
                string role = each.role + number;
                string key = add.key + number;
                if (props.roleKeys.ContainsKey(role)) continue;
                if (def.components.Any(p => p.key == key))
                {
                    VehicleSystems.Problems.Add(def.defName + ": already has a part called " + key);
                    continue;
                }
                string engineKey = props.roleKeys[engine][0];
                string label = numbered ? EachPart.Label(def, add, engineKey) : add.label;
                def.components.Add(VehicleSystems.MakePart(def, add, props, referenceKey: engineKey, label: label, key: key));
                props.roleKeys[role] = new List<string> { key };
                props.addedKeys.Add(key);
                if (!add.countsTowardDestruction) props.uncountedKeys.Add(key);
                added++;
            }
        }
        return added;
    }

    /// <summary>The role of the part a system gives engine role <paramref name="engineRole"/>: "FuelPump2" for "Engine2", the plain
    /// <paramref name="role"/> for the one "Engine".</summary>
    public static string RoleFor(string role, string engineRole) =>
        engineRole.Length > "Engine".Length && engineRole.StartsWith("Engine") ? role + engineRole.Substring("Engine".Length) : role;
}
