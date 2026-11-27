"""What every vehicle with our systems was bound to, from the report D'thorga Framework writes at each start, and what looks wrong.

D'thorga Framework 0.256.0 writes VehicleBindings.json beside the patched-Defs file (the RimWorld user folder's PhobosPatchedDefs) once
the designs are resolved: each design's loadout, its systems and their roles, each role's parts, and every part. This reads that file,
so the audit is of what the game really did, never of a copy of its rules (the owner's report of 23 Nov 2026 that the Systems and Health
tabs disagreed on many aircraft). Reads the file only. Its path comes from --file, or from rimworldConfigPath as query-defs.py's does.

    python scripts/vehicle-bindings.py                      the audit: what looks wrong, by kind, with the vehicles each touches
    python scripts/vehicle-bindings.py show C17_Globemaster  one vehicle: its systems, the parts each covers, and parts no role binds
    python scripts/vehicle-bindings.py --json               the audit as JSON

The audit's kinds:
  - unbound system: a system that names roles, none bound on the vehicle (the Systems tab shows readings with no part).
  - cover-like part: a role bound to a part whose key or label reads like a cover, door, hatch, panel or window.
  - unwatched part: an internal part that moves a stat but no role binds, so no system speaks for it.
  - below the standard: a vehicle missing a system or part role the vehicle standard asks of its kind (config/vehicle-standard.json;
    docs/development/vehicle-standard.md). A line the standard marks as planned is listed apart, as still to come.
"""
from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import _modkit  # noqa: E402  (the shared tools' import path)
import deploy  # noqa: E402  (shared path settings)

REPORT = ('PhobosPatchedDefs', 'VehicleBindings.json')   # where D'thorga Framework writes it (Systems/SystemParts.cs)
COVER_WORDS = re.compile(r'(?<!re)cover|door|hatch|panel|window|armou?r(?!ed)', re.IGNORECASE)
EXPECTED = Path(__file__).resolve().parents[1] / 'config' / 'vehicle-bindings.json'
STANDARD = Path(__file__).resolve().parents[1] / 'config' / 'vehicle-standard.json'


def expected(path: Path = EXPECTED) -> dict:
    """What the audit treats as expected by design (config/vehicle-bindings.json): an empty set when the file is missing."""
    return json.loads(path.read_text(encoding='utf-8')) if path.is_file() else {}


def _cover_expected(rules: dict, role: str, key: str) -> bool:
    return any(rule.get('role') == role and rule.get('partContains', '').lower() in key.lower() for rule in rules.get('coverLike', []))


def _unwatched_expected(rules: dict, key: str, categories: list) -> bool:
    for rule in rules.get('unwatched', []):
        if 'partContains' in rule and rule['partContains'].lower() in key.lower():
            return True
        if 'categoryPrefix' in rule and categories and all(c.startswith(rule['categoryPrefix']) for c in categories):
            return True
    return False


def standard(path: Path = STANDARD) -> dict:
    """The vehicle standard as data (config/vehicle-standard.json): an empty standard when the file is missing."""
    return json.loads(path.read_text(encoding='utf-8')) if path.is_file() else {}


def kind_of(vehicle: dict, std: dict) -> str:
    """The vehicle's kind by the standard's rules, the first match winning: a loadout name containing a word, a part role it has, or a
    system it runs, or the vehicle named in a list. 'other' when no rule matches."""
    systems = {system['def'] for system in vehicle.get('systems', [])}
    for rule in std.get('kinds', []):
        match = rule.get('match', {})
        if 'vehicles' in match and vehicle['def'] not in match['vehicles']:
            continue
        if 'loadoutContains' in match and match['loadoutContains'] not in (vehicle.get('loadout') or ''):
            continue
        if 'hasRole' in match and not vehicle.get('roles', {}).get(match['hasRole']):
            continue
        if 'hasSystem' in match and match['hasSystem'] not in systems:
            continue
        return rule['kind']
    return 'other'


def below_standard(data: dict, std: dict | None = None) -> dict:
    """
    Each vehicle against the vehicle standard (the owner's card of 27 Nov 2026, "An audit kind"): for every line that applies to its kind (or
    names it), the systems and part roles it lacks. A line applies to a vehicle with power only when it says so. Built lines that miss are
    findings; planned lines are listed apart, as what a later batch brings; an exception in the standard excuses one vehicle from one line.
    """
    std = standard() if std is None else std
    powered_systems = set(std.get('powered', []))
    excused = {(e.get('vehicle'), e.get('line')) for e in std.get('except', [])}
    kinds: dict[str, int] = {}
    missing, planned = [], []
    for vehicle in data.get('vehicles', []):
        kind = kind_of(vehicle, std)
        kinds[kind] = kinds.get(kind, 0) + 1
        systems = {system['def'] for system in vehicle.get('systems', [])}
        roles = {role for role, keys in vehicle.get('roles', {}).items() if keys}
        powered = bool(systems & powered_systems)
        where = {'vehicle': vehicle['def'], 'label': vehicle.get('label', ''), 'mod': vehicle.get('mod', ''), 'kind': kind}
        for line in std.get('lines', []):
            named = vehicle['def'] in line.get('vehicles', [])
            if not named and kind not in line.get('kinds', []):
                continue
            if line.get('poweredOnly') and not powered:
                continue
            if (vehicle['def'], line['line']) in excused:
                continue
            lacks = [s for s in line.get('systems', []) if s not in systems] + [r for r in line.get('roles', []) if r not in roles]
            if not lacks:
                continue
            finding = {**where, 'line': line['line'], 'what': line.get('label', ''), 'lacks': lacks}
            if line.get('status') == 'planned':
                planned.append({**finding, 'batch': line.get('batch')})
            else:
                missing.append(finding)
    return {'kinds': dict(sorted(kinds.items())), 'belowStandard': missing, 'stillToCome': planned}


def report_path(settings_file=None, environ=None) -> Path:
    """The bindings report D'thorga Framework writes: beside the game's Config folder, in the RimWorld user folder."""
    from phobos_modkit.games.rimworld import paths
    return Path(paths.config_path(None, environ, settings_file)).parent.joinpath(*REPORT)


def load(path: Path) -> dict:
    return json.loads(path.read_text(encoding='utf-8'))


def audit(data: dict, rules: dict | None = None) -> dict:
    """The findings by kind, each a list of {vehicle, mod, ...}. Parts we add ourselves (mounting points) are never cover-like
    findings, and what `rules` (config/vehicle-bindings.json) says is expected by design is left out."""
    rules = expected() if rules is None else rules
    unbound, cover, unwatched = [], [], []
    for vehicle in data.get('vehicles', []):
        roles = vehicle.get('roles', {})
        parts = {part['key']: part for part in vehicle.get('parts', [])}
        bound = {key for keys in roles.values() for key in keys}
        ours = set(vehicle.get('added', []))
        where = {'vehicle': vehicle['def'], 'label': vehicle.get('label', ''), 'mod': vehicle.get('mod', '')}
        for system in vehicle.get('systems', []):
            if system['roles'] and not any(roles.get(role) for role in system['roles']):
                unbound.append({**where, 'system': system['def'], 'roles': system['roles']})
        for role, keys in roles.items():
            for key in keys:
                part = parts.get(key, {})
                if key in ours or _cover_expected(rules, role, key):
                    continue
                if COVER_WORDS.search(key) or COVER_WORDS.search(part.get('label', '')):
                    cover.append({**where, 'role': role, 'part': key, 'partLabel': part.get('label', '')})
        for key, part in parts.items():
            if key not in bound and part.get('depth') == 'Internal' and part.get('categories')                     and not _unwatched_expected(rules, key, part['categories']):
                unwatched.append({**where, 'part': key, 'partLabel': part.get('label', ''), 'categories': part['categories']})
    return {'written': data.get('written', ''), 'vehicles': len(data.get('vehicles', [])),
            'unboundSystems': unbound, 'coverLikeParts': cover, 'unwatchedParts': unwatched}


def full_audit(data: dict, rules: dict | None = None, std: dict | None = None) -> dict:
    """The bindings audit and the standard's together, as the command prints them."""
    return {**audit(data, rules), **below_standard(data, std)}


def print_audit(found: dict, out) -> None:
    print(f"# Vehicle bindings\n\nThe game's report of {found['written'] or 'an unknown time'}: {found['vehicles']} vehicles with our systems.\n", file=out)
    sections = (
        ('Unbound systems', 'unboundSystems', 'A system that names roles, none bound: its readings show with no part.',
         lambda f: f"{f['system']} (roles {', '.join(f['roles'])})"),
        ('Cover-like parts', 'coverLikeParts', 'A role bound to a part that reads like a cover, door, hatch, panel or window.',
         lambda f: f"{f['role']} -> {f['part']} ({f['partLabel']})"),
        ('Unwatched parts', 'unwatchedParts', 'An internal part that moves a stat, which no role binds: no system speaks for it.',
         lambda f: f"{f['part']} ({f['partLabel']}; {', '.join(f['categories'])})"),
    )
    for title, key, what, line in sections:
        items = found[key]
        print(f"## {title}: {len(items)}\n\n{what}\n", file=out)
        if not items:
            print('None.\n', file=out)
            continue
        print('| Vehicle | Mod | Finding |\n| --- | --- | --- |', file=out)
        for item in sorted(items, key=lambda f: (f['mod'], f['vehicle'])):
            print(f"| {item['vehicle']} | {item['mod']} | {line(item)} |", file=out)
        print('', file=out)
    if 'belowStandard' in found:
        print_standard(found, out)


def print_standard(found: dict, out) -> None:
    """The standard's part of the audit: the vehicles by kind, what falls below a built line, and what later batches bring."""
    kinds = ', '.join(f"{kind} {count}" for kind, count in found['kinds'].items())
    print(f"## Below the standard: {len(found['belowStandard'])}\n\nAgainst docs/development/vehicle-standard.md, by kind ({kinds}): "
          'a system or part role a built line asks of the vehicle\'s kind, which it lacks.\n', file=out)
    if found['belowStandard']:
        print('| Vehicle | Kind | Line | Lacks |\n| --- | --- | --- | --- |', file=out)
        for item in sorted(found['belowStandard'], key=lambda f: (f['line'], f['kind'], f['vehicle'])):
            print(f"| {item['vehicle']} ({item['mod']}) | {item['kind']} | {item['line']}: {item['what']} | {', '.join(item['lacks'])} |", file=out)
    else:
        print('None.', file=out)
    print('', file=out)
    still = found['stillToCome']
    print(f"## Still to come: {len(still)}\n\nLines the standard plans for a later batch, by line: how many vehicles each will reach.\n", file=out)
    if not still:
        print('None.\n', file=out)
        return
    by_line: dict[tuple, list] = {}
    for item in still:
        by_line.setdefault((item['line'], item['what'], item.get('batch')), []).append(item['vehicle'])
    print('| Line | Batch | Vehicles |\n| --- | --- | --- |', file=out)
    for (line, what, batch), vehicles in sorted(by_line.items(), key=lambda kv: kv[0][0]):
        print(f"| {line}: {what} | {batch or ''} | {len(vehicles)} |", file=out)
    print('', file=out)


def print_vehicle(data: dict, name: str, out) -> int:
    matches = [v for v in data.get('vehicles', []) if v['def'].lower() == name.lower() or v.get('label', '').lower() == name.lower()]
    if not matches:
        print(f"No vehicle called {name} in the report.", file=sys.stderr)
        return 1
    vehicle = matches[0]
    roles = vehicle.get('roles', {})
    labels = {part['key']: part.get('label', '') for part in vehicle.get('parts', [])}
    print(f"# {vehicle['def']} ({vehicle.get('label', '')}), {vehicle.get('mod', '')}\n\nLoadout: {vehicle.get('loadout', '')}\n", file=out)
    print('| System | Parts it covers (the first is where its warnings sit) |\n| --- | --- |', file=out)
    for system in vehicle.get('systems', []):
        seen = []
        for role in system['roles']:
            for key in roles.get(role, []):
                if key not in seen:
                    seen.append(key)
        cell = ', '.join(f"{labels.get(key, key)} ({key})" for key in seen) or ('whole vehicle' if not system['roles'] else 'none bound')
        print(f"| {system['def']} | {cell} |", file=out)
    bound = {key for keys in roles.values() for key in keys}
    free = [part for part in vehicle.get('parts', []) if part['key'] not in bound]
    print(f"\n## Parts no role binds: {len(free)}\n", file=out)
    for part in free:
        print(f"- {part.get('label', '')} ({part['key']}), {part.get('depth', '')}", file=out)
    return 0


def main(argv=None, out=None) -> int:
    out = out or sys.stdout
    if hasattr(out, 'reconfigure'):
        out.reconfigure(encoding='utf-8', errors='replace')
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('command', nargs='?', default='audit', choices=['audit', 'show'])
    parser.add_argument('vehicle', nargs='?', help='for show: the vehicle, by defName or label')
    parser.add_argument('--file', help='the report to read (default: the one D\'thorga Framework writes)')
    parser.add_argument('--json', action='store_true', help='the audit as JSON')
    args = parser.parse_args(argv)
    if args.file:
        path = Path(args.file)
    else:
        try:
            _modkit.ensure()
        except _modkit.MissingModKit as missing:
            print(missing, file=sys.stderr)
            return 2
        path = report_path(deploy.SETTINGS_FILE)
    if not path.is_file():
        print(f"No bindings report at {path}: D'thorga Framework 0.256.0 or later writes it at each start.", file=sys.stderr)
        return 1
    data = load(path)
    if args.command == 'show':
        if not args.vehicle:
            print('show needs a vehicle.', file=sys.stderr)
            return 2
        return print_vehicle(data, args.vehicle, out)
    found = full_audit(data)
    if args.json:
        print(json.dumps(found, indent=2), file=out)
    else:
        print_audit(found, out)
    return 0


if __name__ == '__main__':
    sys.exit(main())
