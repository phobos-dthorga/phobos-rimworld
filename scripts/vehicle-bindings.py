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
COVER_WORDS = re.compile(r'cover|door|hatch|panel|window|armou?r(?!ed)', re.IGNORECASE)


def report_path(settings_file=None, environ=None) -> Path:
    """The bindings report D'thorga Framework writes: beside the game's Config folder, in the RimWorld user folder."""
    from phobos_modkit.games.rimworld import paths
    return Path(paths.config_path(None, environ, settings_file)).parent.joinpath(*REPORT)


def load(path: Path) -> dict:
    return json.loads(path.read_text(encoding='utf-8'))


def audit(data: dict) -> dict:
    """The findings by kind, each a list of {vehicle, mod, ...}."""
    unbound, cover, unwatched = [], [], []
    for vehicle in data.get('vehicles', []):
        roles = vehicle.get('roles', {})
        parts = {part['key']: part for part in vehicle.get('parts', [])}
        bound = {key for keys in roles.values() for key in keys}
        where = {'vehicle': vehicle['def'], 'label': vehicle.get('label', ''), 'mod': vehicle.get('mod', '')}
        for system in vehicle.get('systems', []):
            if system['roles'] and not any(roles.get(role) for role in system['roles']):
                unbound.append({**where, 'system': system['def'], 'roles': system['roles']})
        for role, keys in roles.items():
            for key in keys:
                part = parts.get(key, {})
                if COVER_WORDS.search(key) or COVER_WORDS.search(part.get('label', '')):
                    cover.append({**where, 'role': role, 'part': key, 'partLabel': part.get('label', '')})
        for key, part in parts.items():
            if key not in bound and part.get('depth') == 'Internal' and part.get('categories'):
                unwatched.append({**where, 'part': key, 'partLabel': part.get('label', ''), 'categories': part['categories']})
    return {'written': data.get('written', ''), 'vehicles': len(data.get('vehicles', [])),
            'unboundSystems': unbound, 'coverLikeParts': cover, 'unwatchedParts': unwatched}


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
    found = audit(data)
    if args.json:
        print(json.dumps(found, indent=2), file=out)
    else:
        print_audit(found, out)
    return 0


if __name__ == '__main__':
    sys.exit(main())
