using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Phobos.Framework.Vehicles.Rules;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Which parts each system covers, and where its warnings sit: one answer that the Systems tab, the Health tab and anything later read
/// (Framework 0.256.0; the owner's report of 23 Nov 2026 that the two tabs disagreed on many aircraft, and the owner's ask: "genericize how
/// these talk with each other, so any future updates/additions can just be picked up"). Before, the Systems tab listed a system's parts by
/// every role it names, while the Health tab hung its warnings on the part of its first role, and only for roles in the loadout's own list;
/// a system whose first role was unbound, a part bound by code (weapon mounts, glass, fire-fighting), or a system with no part at all showed
/// on one tab and not the other. Everything here is derived from the design's bindings and never saved.
/// </summary>
public static class SystemParts
{
    /// <summary>The parts a system covers, in the order of its roles, each once, with the first of its roles that binds it.</summary>
    public static IEnumerable<(string role, VehicleComponent part)> Of(CompPhobosVehicleSystems comp, SystemState system)
    {
        var seen = new HashSet<VehicleComponent>();
        foreach (string role in system.Def.roles)
        {
            if (!comp.Props.roleKeys.TryGetValue(role, out List<string>? keys)) continue;
            foreach (string key in keys)
            {
                VehicleComponent? part = comp.Vehicle.statHandler?.GetComponent(key);
                if (part != null && seen.Add(part)) yield return (role, part);
            }
        }
    }

    /// <summary>The part a system's warnings sit under on the Health tab: the first part it covers, or null for a system with none (its
    /// warnings then sit with the whole vehicle's).</summary>
    public static VehicleComponent? Home(CompPhobosVehicleSystems comp, SystemState system) => Of(comp, system).Select(p => p.part).FirstOrDefault();

    /// <summary>Every bound part's key with the role it is listed by: the loadout's own roles first, in its order, then those bound by code
    /// (weapon mounts, glass, fire-fighting, a base role made of numbered ones).</summary>
    public static Dictionary<string, string> RoleByKey(CompPhobosVehicleSystems comp)
    {
        var roleOf = new Dictionary<string, string>();
        IEnumerable<string> ordered = (comp.Props.loadout?.partRoles.Select(spec => spec.role) ?? Enumerable.Empty<string>()).Concat(comp.Props.roleKeys.Keys);
        foreach (string role in ordered)
        {
            if (!comp.Props.roleKeys.TryGetValue(role, out List<string>? keys)) continue;
            foreach (string key in keys)
            {
                if (!roleOf.ContainsKey(key)) roleOf[key] = role;
            }
        }
        return roleOf;
    }

    // ---- At start-up ----

    /// <summary>
    /// Rolls numbered roles up into their base role (RoleFamilyRule): a design that binds Engine1 to EngineN has its base role "Engine"
    /// bound to all of them, in place of whatever one part a generic finder took for it, keeping a part of its own that names the role
    /// (Framework 0.258.0, KeepsBaseBinding). Called once per design, after every part is bound.
    /// </summary>
    public static void RollUpFamilies(CompProperties_PhobosVehicleSystems props)
    {
        foreach ((string baseRole, List<string> members) in RoleFamilyRule.Families(props.roleKeys.Keys.ToList()))
        {
            var keys = new List<string>();
            // Framework 0.258.0: what the base role held of its own first, where it names the base role (the thruster craft's "Engine").
            // Framework 0.258.1: never a part another role claims (the Cherokee's "front engine", read as its rotor drive's gearing).
            if (props.roleKeys.TryGetValue(baseRole, out List<string>? own))
            {
                foreach (string key in own)
                {
                    if (RoleFamilyRule.KeepsBaseBinding(key, baseRole) && !keys.Contains(key) && !ClaimedElsewhere(props, key, baseRole, members)) keys.Add(key);
                }
            }
            foreach (string member in members)
            {
                foreach (string key in props.roleKeys[member])
                {
                    if (!keys.Contains(key)) keys.Add(key);
                }
            }
            if (keys.Count > 0) props.roleKeys[baseRole] = keys;
        }
    }

    /// <summary>Whether a role other than the base role and its numbered members binds this part (Framework 0.258.1).</summary>
    static bool ClaimedElsewhere(CompProperties_PhobosVehicleSystems props, string key, string baseRole, List<string> members) =>
        props.roleKeys.Any(pair => pair.Key != baseRole && !members.Contains(pair.Key) && pair.Value.Contains(key));

    /// <summary>The bindings report's file: in the game's save-data folder, beside the patched-Defs file, never shared or committed.</summary>
    public static string ReportPath => Path.Combine(GenFilePaths.SaveDataFolderPath, "PhobosPatchedDefs", "VehicleBindings.json");

    /// <summary>
    /// Writes what every design with our systems was bound to (Framework 0.256.0), so `scripts/vehicle-bindings.py` can audit the whole
    /// mod list from what the game really did, not from a copy of its rules: each design's loadout, its systems and their roles, each role's
    /// parts, and every part with its depth and whether any role binds it. Written once at start-up; a fault is logged, never thrown.
    /// </summary>
    public static void WriteReport(IEnumerable<VehicleDef> defs)
    {
        try
        {
            var text = new StringBuilder();
            text.Append("{\n  \"written\": \"").Append(DateTime.UtcNow.ToString("o")).Append("\",\n  \"vehicles\": [");
            bool firstVehicle = true;
            foreach (VehicleDef def in defs)
            {
                if (def.comps?.OfType<CompProperties_PhobosVehicleSystems>().FirstOrDefault() is not { } props) continue;
                text.Append(firstVehicle ? "\n" : ",\n");
                firstVehicle = false;
                text.Append("    {\"def\": ").Append(Json(def.defName)).Append(", \"label\": ").Append(Json(def.label ?? ""))
                    .Append(", \"mod\": ").Append(Json(def.modContentPack?.PackageId ?? ""))
                    .Append(", \"loadout\": ").Append(Json(props.loadout?.defName ?? ""))
                    // Framework 0.368.0 (the vehicle standard's VS11): military as the factory radios and the civilian bays judge it, and the
                    // parts its design has had taken out, so the audit can say what a military vehicle should not show.
                    .Append(", \"military\": ").Append(props.vehicleClass == Phobos.Framework.Vehicles.Rules.VehicleClass.Military ? "true" : "false")
                    // Framework 0.369.0: the class itself (civilian, armed by upgrade, military, mechanical).
                    .Append(", \"class\": ").Append(Json(props.vehicleClass.ToString()))
                    .Append(", \"removed\": [").Append(string.Join(", ", props.removedParts.Select(r => Json(r.key)))).Append("]")
                    .Append(",\n     \"systems\": [");
                text.Append(string.Join(", ", props.systems.Select(s => "{\"def\": " + Json(s.def.defName) + ", \"roles\": [" + string.Join(", ", s.def.roles.Select(Json)) + "]}")));
                text.Append("],\n     \"roles\": {");
                text.Append(string.Join(", ", props.roleKeys.Select(pair => Json(pair.Key) + ": [" + string.Join(", ", pair.Value.Select(Json)) + "]")));
                text.Append("},\n     \"added\": [").Append(string.Join(", ", props.addedKeys.Select(Json))).Append("],\n     \"parts\": [");
                text.Append(string.Join(", ", (def.components ?? new List<VehicleComponentProperties>()).Select(part =>
                    "{\"key\": " + Json(part.key) + ", \"label\": " + Json(part.label ?? "") + ", \"depth\": " + Json(part.depth.ToString())
                    + ", \"categories\": [" + string.Join(", ", (part.categories ?? new List<VehicleStatDef>()).Select(c => Json(c.defName))) + "]}")));
                text.Append("]}");
            }
            text.Append("\n  ]\n}\n");
            string path = ReportPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path + ".writing", text.ToString(), new UTF8Encoding(false));
            if (File.Exists(path)) File.Delete(path);
            File.Move(path + ".writing", path);
        }
        catch (Exception error)
        {
            VehicleSystemsMod.Logger.Warn("could not write the vehicle bindings report (" + error.Message + "); the game is not affected.");
        }
    }

    static string Json(string value)
    {
        var text = new StringBuilder("\"");
        foreach (char c in value)
        {
            switch (c)
            {
                case '"': text.Append("\\\""); break;
                case '\\': text.Append("\\\\"); break;
                case '\n': text.Append("\\n"); break;
                case '\r': break;
                case '\t': text.Append("\\t"); break;
                default:
                    if (c < ' ') text.Append("\\u").Append(((int)c).ToString("x4"));
                    else text.Append(c);
                    break;
            }
        }
        return text.Append('"').ToString();
    }
}

/// <summary>Writes the vehicle bindings report once the designs are resolved (Framework 0.256.0; SystemParts.WriteReport).</summary>
[StaticConstructorOnStartup]
public static class SystemPartsReport
{
    static SystemPartsReport() => SystemParts.WriteReport(DefDatabase<VehicleDef>.AllDefsListForReading);
}
