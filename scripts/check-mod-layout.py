"""Check the mods/ folder layout and each mod's About.xml; no game installation needed.

Each mods/<Mod>/ holds About/About.xml (RimWorld's mod identity), a README and a CHANGELOG.md.
About.xml needs the fields RimWorld requires, a packageId of the house form PhobosDthorga.<ModName>
(unique in the repository, ignoring case as RimWorld does) and a MAJOR.MINOR.PATCH modVersion. Every
non-version folder LoadFolders.xml names must exist; version folders such as 1.7 may hold only built
assemblies, which git ignores. No two folders in one LoadFolders.xml block may hold a Defs or Patches file
at the same path, since the game would load only one of them (files_the_game_loads). Build products never
belong in mods/: they are built from src/. A C# project under src/ that names a mod in <ModFolder> carries that mod's
version in <Version>, and any ModVersion constant in its source matches it too (check_versions).
"""
import argparse
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import xml.etree.ElementTree as ET

sys.path.insert(0, str(Path(__file__).resolve().parent))
from _csharp import call_arguments, disagreements, read_settings_classes, strip_comments, top_level_arguments  # noqa: E402

PACKAGE_PREFIX = 'PhobosDthorga.'
PACKAGE_ID = re.compile(r'^[A-Za-z0-9]+(\.[A-Za-z0-9]+)+$')
MOD_VERSION = re.compile(r'^\d+\.\d+\.\d+$')
GAME_VERSION = re.compile(r'^\d+\.\d+$')
NUMBERED_PLACEHOLDER = re.compile(r'\{\d+\}')
KEYED_FILE = re.compile(r'(^|/)Languages/[^/]+/Keyed/[^/]+\.xml$')
REQUIRED_FIELDS = ('name', 'packageId', 'description')
BUILD_SUFFIXES = ('.dll', '.pdb', '.exe', '.deps.json', '.pyc')
BUILD_FOLDERS = ('bin', 'obj', '__pycache__')
ABOUT = 'About/About.xml'
LOAD_FOLDERS = 'LoadFolders.xml'
XML_CONTENT_FOLDERS = ('Defs/', 'Patches/')  # read through Verse.DirectXmlLoader.XmlAssetsInModFolder
PROJECT_VERSION = re.compile(r'<Version>\s*([^<\s]+)\s*</Version>')
PROJECT_MOD_FOLDER = re.compile(r'<ModFolder>\s*([^<\s]+)\s*</ModFolder>')
VERSION_CONSTANT = re.compile(r'\bModVersion\s*=\s*"([^"]+)"')


def listed_files(root):
    """Tracked and untracked, non-ignored files under mods/ and src/ (for check_versions) and the constants catalogue, as POSIX
    paths relative to root."""
    out = subprocess.check_output(['git', 'ls-files', '--cached', '--others', '--exclude-standard', '-z', '--', 'mods', 'src',
                                   'config/maintained-constants.json'],
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


# The disclaimer Ludeon's EULA requires on each mod, word for word (https://rimworldgame.com/eula/).
EULA_DISCLAIMER = (
    'Portions of the materials used to create this content/mod are trademarks and/or copyrighted works of Ludeon Studios Inc.',
    'All rights reserved by Ludeon. This content/mod is not official and is not endorsed by Ludeon.',
)


def check_about(mod, about, errors):
    """Check one About.xml root; return its packageId, or None."""
    if about.tag != 'ModMetaData':
        errors.append(f'{mod}: {ABOUT} root is <{about.tag}>, not <ModMetaData>')
        return None
    for field in REQUIRED_FIELDS:
        if not (about.findtext(field) or '').strip():
            errors.append(f'{mod}: {ABOUT} has no <{field}>')
    description = ' '.join((about.findtext('description') or '').split())
    if description and not all(sentence in description for sentence in EULA_DISCLAIMER):
        errors.append(f'{mod}: {ABOUT} description lacks the disclaimer Ludeon\'s EULA requires, word for word')
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


def files_the_game_loads(folder_files):
    """Which of a mod's Defs and Patches files RimWorld loads.

    The game (Verse.DirectXmlLoader.XmlAssetsInModFolder with ModContentPack.InitLoadFolders, read from the
    1.7 assembly on 29 Oct 2026) walks the load folders from the last one LoadFolders.xml lists to the first,
    and keys each file by its path inside its load folder. The first file to claim a path loads; any other
    file with that path is skipped without a message. folder_files is [(folder, [paths inside it])] in
    LoadFolders order. Returns ({path: folder that loads it}, [(skipped folder, path, winning folder)])."""
    loaded, skipped = {}, []
    for folder, paths in reversed(folder_files):
        for path in paths:
            if path in loaded:
                skipped.append((folder, path, loaded[path]))
            else:
                loaded[path] = folder
    return loaded, skipped


def check_load_folders(mod, load_folders, rels, errors):
    for version in load_folders:
        folder_files = []
        for li in version.findall('li'):
            folder = (li.text or '').strip().strip('/')
            prefix = folder + '/' if folder else ''
            inside = [rel[len(prefix):] for rel in rels if rel.startswith(prefix)]
            folder_files.append((folder or '/', [p for p in inside if p.startswith(XML_CONTENT_FOLDERS)]))
            # Version folders (1.7, Mods/HarrierVtol/1.7) hold only built assemblies, which are never committed.
            if not folder or GAME_VERSION.match(folder.rsplit('/', 1)[-1]):
                continue
            if not inside:
                errors.append(f'{mod}: {LOAD_FOLDERS} names {folder} under <{version.tag}>, but that folder is empty or missing')
        # Every IfModActive folder counts: a player may run any mix of the optional mods.
        for folder, path, winner in files_the_game_loads(folder_files)[1]:
            errors.append(f'{mod}: under <{version.tag}>, {folder}/{path} is never loaded, because {winner}/{path} '
                          f'has the same path and its folder is listed later; give one of them another name')


def check_keyed(mod, path, data, errors):
    """Translators need to know what a placeholder means, so a message names them (AGENTS.md)."""
    root = parse(data, path, errors, mod)
    if root is None:
        return
    for entry in root:
        if NUMBERED_PLACEHOLDER.search(entry.text or ''):
            errors.append(f'{mod}: {path}: {entry.tag} uses a numbered placeholder; name it, as in {{COUNT}}')


def check_versions(files, read, about_versions, errors):
    """Every copy of a mod's version that lives in src/ matches its About.xml: a project's <Version> and its ModVersion constants."""
    for project in sorted(name for name in files if name.startswith('src/') and name.endswith('.csproj')):
        text = read(project).decode('utf-8', 'replace')
        folder = PROJECT_MOD_FOLDER.search(text)
        if not folder or folder.group(1) not in about_versions:
            continue
        expected = about_versions[folder.group(1)]
        version = PROJECT_VERSION.search(text)
        if version and version.group(1) != expected:
            errors.append(f'{project}: <Version> {version.group(1)} differs from mods/{folder.group(1)}/{ABOUT} modVersion {expected}')
        source = project.rsplit('/', 1)[0] + '/'
        for name in sorted(n for n in files if n.startswith(source) and n.endswith('.cs')):
            for found in VERSION_CONSTANT.finditer(read(name).decode('utf-8', 'replace')):
                if found.group(1) != expected:
                    errors.append(f'{name}: ModVersion {found.group(1)} differs from mods/{folder.group(1)}/{ABOUT} modVersion {expected}')


ENGLISH_KEYED = re.compile(r'(^|/)Languages/English/Keyed/[^/]+\.xml$')
TRANSLATE_CALL = re.compile(r'"(Phobos[A-Za-z0-9_]+)"\s*\.\s*(?:Translate|TranslateSimple)\s*\(')
LABEL_KEY = re.compile(r'\blabelKey\s*=\s*"(Phobos[A-Za-z0-9_]+)"')
NAMED_ARGUMENT = re.compile(r'\.Named\(\s*"([A-Za-z0-9_]+)"')
TEXT_PLACEHOLDER = re.compile(r'\{([A-Za-z][A-Za-z0-9]*)(?:_[^}]*)?\}')


def check_translation_keys(files, read, errors):
    """Every key our C# translates by name has an English entry (else the game shows players the key itself), and when the
    call names its placeholders, every {NAME} in the English text is among them (else players see {NAME}). Keys built at
    run time, or chosen inside a switch before .Translate(), cannot be read this way and are not checked."""
    keyed = {}
    for name in files:
        if name.startswith('mods/') and ENGLISH_KEYED.search(name):
            try:
                root = ET.fromstring(read(name))
            except ET.ParseError:
                continue  # check_keyed reports it
            for entry in root:
                keyed.setdefault(entry.tag, entry.text or '')
    for name in sorted(n for n in files if n.startswith('src/') and n.endswith('.cs')):
        text = strip_comments(read(name).decode('utf-8', 'replace'))  # a commented-out call is not a call
        for found in LABEL_KEY.finditer(text):
            if found.group(1) not in keyed:
                line = text.count('\n', 0, found.start()) + 1
                errors.append(f'{name}:{line}: tab label "{found.group(1)}" has no English Keyed entry; the tab would show the key')
        for found in TRANSLATE_CALL.finditer(text):
            key, line = found.group(1), text.count('\n', 0, found.start()) + 1
            if key not in keyed:
                errors.append(f'{name}:{line}: "{key}" has no English Keyed entry; players would see the key itself')
                continue
            arguments = call_arguments(text, found.end() - 1)
            # Only a call whose every argument is spelt out as .Named("...") can be read: a variable may carry any name.
            parts = top_level_arguments(arguments)
            if parts and not all(NAMED_ARGUMENT.search(part) for part in parts):
                continue
            named = {n.upper() for n in NAMED_ARGUMENT.findall(arguments)}
            wanted = {p.upper() for p in TEXT_PLACEHOLDER.findall(keyed[key])}
            if wanted - named:
                missing = ', '.join('{' + p + '}' for p in sorted(wanted - named))
                errors.append(f'{name}:{line}: "{key}" is translated without {missing}; players would see it as written')


def check_settings_defaults(files, read, errors):
    """A saved setting whose field starts at one value while a missing key loads as another (the third argument of
    Scribe_Values.Look): a new player and one whose settings file predates the key get different settings."""
    sources = {n: read(n).decode('utf-8', 'replace') for n in files if n.startswith('src/') and n.endswith('.cs')}
    for cls, key in disagreements(read_settings_classes(sources)):
        errors.append(f'{key.path}:{key.line}: {cls.name}.{key.field} starts at {key.initial} but "{key.key}" loads as '
                      f'{key.default} when missing; make them the same')


CATALOGUE = 'config/maintained-constants.json'


def check_catalogue(files, read, about_versions, errors):
    """Every copy of a mod's version is registered in the constants catalogue, so scripts/release.py constants --set updates
    it: a copy left out would keep its old number at the next bump (the drift check above would catch it only afterwards)."""
    if CATALOGUE not in files:
        return
    try:
        constants = json.loads(read(CATALOGUE).decode('utf-8'))['constants']
    except (ValueError, KeyError) as error:
        errors.append(f'{CATALOGUE}: unreadable ({error})')
        return
    for mod in sorted(about_versions):
        key = f'{mod}.version'
        if key not in constants:
            errors.append(f'{CATALOGUE}: no {key} for mods/{mod}; add it with every copy of the version')
            continue
        listed = {target.get('path') for target in constants[key].get('targets', [])}
        copies = [f'mods/{mod}/{ABOUT}']
        for project in sorted(name for name in files if name.startswith('src/') and name.endswith('.csproj')):
            folder = PROJECT_MOD_FOLDER.search(read(project).decode('utf-8', 'replace'))
            if not folder or folder.group(1) != mod:
                continue
            copies.append(project)
            source = project.rsplit('/', 1)[0] + '/'
            copies += [n for n in sorted(files) if n.startswith(source) and n.endswith('.cs')
                       and VERSION_CONSTANT.search(read(n).decode('utf-8', 'replace'))]
        for copy in copies:
            if copy not in listed:
                errors.append(f'{CATALOGUE}: {copy} carries mods/{mod}\'s version but {key} does not list it; add it to the targets')


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
    about_versions = {}
    for mod, rels in sorted(by_mod.items()):
        if ABOUT in rels:
            about = parse(read(f'mods/{mod}/{ABOUT}'), ABOUT, errors, mod)
            package_id = check_about(mod, about, errors) if about is not None else None
            if about is not None and (about.findtext('modVersion') or '').strip():
                about_versions[mod] = about.findtext('modVersion').strip()
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
            if KEYED_FILE.search(rel):
                check_keyed(mod, rel, read(f'mods/{mod}/{rel}'), errors)
            if rel.lower().endswith(BUILD_SUFFIXES) or any(part in BUILD_FOLDERS for part in rel.split('/')[:-1]):
                errors.append(f'{mod}: {rel} is a build product; build it from src/ instead of committing it')
    check_versions(files, read, about_versions, errors)
    check_catalogue(files, read, about_versions, errors)
    check_translation_keys(files, read, errors)
    check_settings_defaults(files, read, errors)
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
