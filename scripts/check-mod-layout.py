"""Check the mods/ folder layout and each mod's About.xml; no game installation needed.

Each mods/<Mod>/ holds About/About.xml (RimWorld's mod identity), a README and a CHANGELOG.md.
About.xml needs the fields RimWorld requires, a packageId of the house form PhobosDthorga.<ModName>
(unique in the repository, ignoring case as RimWorld does) and a MAJOR.MINOR.PATCH modVersion. Every
non-version folder LoadFolders.xml names must exist; version folders such as 1.6 may hold only built
assemblies, which git ignores. Build products never belong in mods/: they are built from src/.
"""
import argparse
import json
import os
from pathlib import Path
import re
import subprocess
import xml.etree.ElementTree as ET

PACKAGE_PREFIX = 'PhobosDthorga.'
PACKAGE_ID = re.compile(r'^[A-Za-z0-9]+(\.[A-Za-z0-9]+)+$')
MOD_VERSION = re.compile(r'^\d+\.\d+\.\d+$')
GAME_VERSION = re.compile(r'^\d+\.\d+$')
REQUIRED_FIELDS = ('name', 'packageId', 'description')
BUILD_SUFFIXES = ('.dll', '.pdb', '.exe', '.deps.json', '.pyc')
BUILD_FOLDERS = ('bin', 'obj', '__pycache__')
ABOUT = 'About/About.xml'
LOAD_FOLDERS = 'LoadFolders.xml'


def listed_files(root):
    """Tracked and untracked, non-ignored files under mods/, as POSIX paths relative to root."""
    out = subprocess.check_output(['git', 'ls-files', '--cached', '--others', '--exclude-standard', '-z', '--', 'mods'],
                                  cwd=root)
    return [name for name in out.decode('utf-8').split('\0') if name and (Path(root) / name).is_file()]


def parse(data, label, errors, mod):
    try:
        return ET.fromstring(data)
    except ET.ParseError as error:
        errors.append(f'{mod}: {label} is not valid XML ({error})')
        return None


def items(element, tag):
    found = element.find(tag)
    return [] if found is None else [(li.text or '').strip() for li in found.findall('li')]


def check_about(mod, about, errors):
    """Check one About.xml root; return its packageId, or None."""
    if about.tag != 'ModMetaData':
        errors.append(f'{mod}: {ABOUT} root is <{about.tag}>, not <ModMetaData>')
        return None
    for field in REQUIRED_FIELDS:
        if not (about.findtext(field) or '').strip():
            errors.append(f'{mod}: {ABOUT} has no <{field}>')
    if not (about.findtext('author') or '').strip() and not any(items(about, 'authors')):
        errors.append(f'{mod}: {ABOUT} has no <author> or <authors>')
    versions = items(about, 'supportedVersions')
    if not versions:
        errors.append(f'{mod}: {ABOUT} lists no <supportedVersions>')
    for version in versions:
        if not GAME_VERSION.match(version):
            errors.append(f'{mod}: {ABOUT} supportedVersions entry "{version}" is not a game version like 1.6')
    mod_version = (about.findtext('modVersion') or '').strip()
    if not MOD_VERSION.match(mod_version):
        errors.append(f'{mod}: {ABOUT} modVersion "{mod_version}" is not MAJOR.MINOR.PATCH')
    package_id = (about.findtext('packageId') or '').strip()
    if package_id and not PACKAGE_ID.match(package_id):
        errors.append(f'{mod}: packageId "{package_id}" must be letters and digits in parts separated by periods')
    elif package_id and not package_id.lower().startswith(PACKAGE_PREFIX.lower()):
        errors.append(f'{mod}: packageId "{package_id}" does not start with {PACKAGE_PREFIX}')
    return package_id or None


def check_load_folders(mod, load_folders, rels, errors):
    for version in load_folders:
        for li in version.findall('li'):
            folder = (li.text or '').strip().strip('/')
            if not folder or GAME_VERSION.match(folder):
                continue
            if not any(rel.startswith(folder + '/') for rel in rels):
                errors.append(f'{mod}: {LOAD_FOLDERS} names {folder} under <{version.tag}>, but that folder is empty or missing')


def check(root, files=None, read=None):
    """Check the mods among files (default: git's list); read(path) returns a file's bytes."""
    files = listed_files(root) if files is None else files
    read = read or (lambda name: (Path(root) / name).read_bytes())
    by_mod = {}
    for name in files:
        parts = name.split('/')
        if len(parts) >= 3 and parts[0] == 'mods':
            by_mod.setdefault(parts[1], []).append('/'.join(parts[2:]))
    errors = []
    owners = {}
    for mod, rels in sorted(by_mod.items()):
        if ABOUT in rels:
            about = parse(read(f'mods/{mod}/{ABOUT}'), ABOUT, errors, mod)
            package_id = check_about(mod, about, errors) if about is not None else None
            if package_id:
                owners.setdefault(package_id.lower(), []).append(mod)
        else:
            errors.append(f'{mod}: no {ABOUT} (RimWorld needs it to list the mod)')
        if LOAD_FOLDERS in rels:
            load_folders = parse(read(f'mods/{mod}/{LOAD_FOLDERS}'), LOAD_FOLDERS, errors, mod)
            if load_folders is not None:
                check_load_folders(mod, load_folders, rels, errors)
        if 'README.md' not in rels:
            errors.append(f'{mod}: no README.md at the top of the mod folder')
        if 'CHANGELOG.md' not in rels:
            errors.append(f'{mod}: no CHANGELOG.md')
        for rel in rels:
            if rel.lower().endswith(BUILD_SUFFIXES) or any(part in BUILD_FOLDERS for part in rel.split('/')[:-1]):
                errors.append(f'{mod}: {rel} is a build product; build it from src/ instead of committing it')
    for package_id, mods in sorted(owners.items()):
        if len(mods) > 1:
            errors.append(f'packageId {package_id} is used by {", ".join(mods)}; RimWorld loads only the first')
    return {'status': 'invalid' if errors else 'valid', 'mods': sorted(by_mod), 'errors': errors}


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.parse_args()
    result = check(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
    print(json.dumps(result, indent=2))
    raise SystemExit(bool(result['errors']))
