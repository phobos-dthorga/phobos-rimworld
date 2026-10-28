"""Check a Phobos factory mod's ProcessDefs and patches against the installed game and the active mods.

Reads the game's Data folder, the Mods folder, the Steam Workshop folder and ModsConfig.xml (what is
switched on), the way scripts/refresh-mod-list.py does, and never writes to any of them. For the mod
named (default PhobosFactoryAdditions) it loads the folders LoadFolders.xml would load for the active
set, then checks:

- every PatchOperation xpath (Add, Replace, AddModExtension, inside Sequence and FindMod) matches exactly
  one node in the loaded Defs (the game's, the active mods' and this mod's own);
- every Def name a ProcessDef uses (ingredient thing or category, result, research, sustainer, effecter)
  exists among the loaded Defs;
- every hopper-only ProcessDef sets maxOutputCount, and keeps it at or below the result's stack limit
  minus the batch count (above that, Pipe System's output step destroys the surplus);
- every ProcessDef this mod defines is added to a machine by one of its patches, and every name a patch
  adds to a processes list is a ProcessDef the mod defines.

    python scripts/check-factory-patches.py                 check PhobosFactoryAdditions
    python scripts/check-factory-patches.py --mod <Name>    check another mod in mods/

Paths: --game, --workshop and --config, or RIMWORLD_PATH, RIMWORLD_WORKSHOP_PATH and
RIMWORLD_CONFIG_PATH, or .local/install-settings.json (rimworldPath, workshopPath, rimworldConfigPath),
or the Steam defaults, as in deploy.py. Exit code 1 on any error. An offline check is not an in-game test.
"""
import argparse
import importlib.util
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(Path(__file__).resolve().parent))
import deploy  # noqa: E402  (shared path settings)

_spec = importlib.util.spec_from_file_location('refresh_mod_list', Path(__file__).resolve().parent / 'refresh-mod-list.py')
refresh = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(refresh)

DEFAULT_MOD = 'PhobosFactoryAdditions'
DEFAULT_STACK_LIMIT = 75  # Verse.ThingDef's default
IMPLIED_PREFIXES = ('Meat_', 'Leather_', 'Corpse_')  # Defs the game generates from each race ThingDef
VERSION_FOLDER = re.compile(r'^\d+\.\d+$')
XPATH_OPERATIONS = ('PatchOperationAdd', 'PatchOperationReplace', 'PatchOperationAddModExtension',
                    'PatchOperationInsert', 'PatchOperationRemove', 'PatchOperationSetName',
                    'PatchOperationAttributeSet', 'PatchOperationAttributeAdd')


def _read_xml(path):
    try:
        return ET.parse(path).getroot()
    except ET.ParseError:
        root = refresh._parse_xml(path)
        return root


def load_folders_for(mod_folder, version, active):
    """The folders RimWorld loads from mod_folder for game version like '1.7', honouring LoadFolders.xml.

    With no LoadFolders.xml: the root, Common and the newest version folder not above the game's. With one:
    the block for the game's version, else the newest lower block; entries with IfModActive load only when
    one of the named package IDs is active (case-insensitive)."""
    mod_folder = Path(mod_folder)
    load_file = next((p for p in mod_folder.iterdir() if p.is_file() and p.name.lower() == 'loadfolders.xml'), None)
    if load_file is None:
        folders = [mod_folder, mod_folder / 'Common']
        versions = sorted((p for p in mod_folder.iterdir() if p.is_dir() and VERSION_FOLDER.match(p.name)
                           and _version_key(p.name) <= _version_key(version)), key=lambda p: _version_key(p.name))
        if versions:
            folders.append(versions[-1])
        return [f for f in folders if f.is_dir()]
    root = _read_xml(load_file)
    if root is None:
        return [mod_folder]
    blocks = {child.tag[1:]: child for child in root if child.tag.startswith('v') and VERSION_FOLDER.match(child.tag[1:])}
    usable = sorted((v for v in blocks if _version_key(v) <= _version_key(version)), key=_version_key)
    if not usable:
        return [mod_folder]
    folders = []
    for li in blocks[usable[-1]].findall('li'):
        wanted = li.get('IfModActive')
        if wanted and not any(pid.strip().lower() in active for pid in wanted.split(',')):
            continue
        rel = (li.text or '').strip().strip('/').replace('\\', '/')
        folder = mod_folder / rel if rel else mod_folder
        if folder.is_dir():
            folders.append(folder)
    return folders


def _version_key(text):
    return tuple(int(n) for n in text.split('.'))


def def_files(folders, kind):
    """Every XML file under <folder>/<kind> (Defs or Patches) for the folders, in load order."""
    files = []
    for folder in folders:
        sub = next((p for p in folder.iterdir() if p.is_dir() and p.name.lower() == kind.lower()), None)
        if sub is not None:
            files += sorted(p for p in sub.rglob('*.xml'))
    return files


class DefIndex:
    """All Def elements loaded so far, in one <Defs> root, with names and ThingDef inheritance."""

    def __init__(self):
        self.root = ET.Element('Defs')
        self.wrapper = ET.Element('root')
        self.wrapper.append(self.root)
        self.names = set()          # every defName
        self.by_name = {}            # ThingDef Name="..." -> element (for ParentName chains)
        self.unreadable = []

    def add_file(self, path):
        root = _read_xml(path)
        if root is None or root.tag != 'Defs':
            if root is None:
                self.unreadable.append(str(path))
            return
        for element in list(root):
            if not isinstance(element.tag, str):
                continue
            self.root.append(element)
            def_name = element.findtext('defName')
            if def_name:
                self.names.add(def_name.strip())
            name = element.get('Name')
            if name:
                self.by_name[name] = element

    def has(self, def_name):
        """Whether a Def exists, counting the meat, leather and corpse Defs the game generates per race."""
        if def_name in self.names:
            return True
        for prefix in IMPLIED_PREFIXES:
            if def_name.startswith(prefix) and def_name[len(prefix):] in self.names:
                return True
        return False

    def stack_limit(self, def_name):
        element = next((e for e in self.root.findall('ThingDef') if (e.findtext('defName') or '').strip() == def_name), None)
        seen = set()
        while element is not None:
            text = element.findtext('stackLimit')
            if text and text.strip().isdigit():
                return int(text.strip())
            parent = element.get('ParentName')
            if not parent or parent in seen:
                break
            seen.add(parent)
            element = self.by_name.get(parent)
        return DEFAULT_STACK_LIMIT

    def matches(self, xpath):
        """How many nodes an xpath like Defs/ThingDef[defName="X"]/comps matches (RimWorld's form)."""
        try:
            return len(self.wrapper.findall(xpath))
        except SyntaxError as error:
            raise ValueError(f'unsupported xpath {xpath}: {error}') from None


def process_errors(process, index, active, where):
    """Errors for one PipeSystem.ProcessDef element; [] when it is fine or inactive by MayRequire."""
    wanted = process.get('MayRequire')
    if wanted and not any(pid.strip().lower() in active for pid in wanted.split(',')):
        return []
    name = (process.findtext('defName') or '?').strip()
    errors = []

    def need(def_name, role):
        if def_name and not index.has(def_name):
            errors.append(f'{where}: {name}: {role} {def_name} is not a loaded Def')

    for li in process.findall('ingredients/li'):
        need((li.findtext('thing') or '').strip(), 'ingredient')
        need((li.findtext('thingCategory') or '').strip(), 'ingredient category')
    for li in process.findall('researchPrerequisites/li'):
        need((li.text or '').strip(), 'research')
    need((process.findtext('sustainerDef') or '').strip(), 'sustainer')
    need((process.findtext('effecterDef') or '').strip(), 'effecter')
    results = process.findall('results/li')
    for li in results:
        need((li.findtext('thing') or '').strip(), 'result')
    if (process.findtext('onlyGrabAndOutputToFactoryHoppers') or '').strip().lower() == 'true' and results:
        result = (results[0].findtext('thing') or '').strip()
        count = int((results[0].findtext('count') or '1').strip() or 1)
        cap_text = (process.findtext('maxOutputCount') or '0').strip()
        cap = int(float(cap_text)) if cap_text else 0
        if cap <= 0:
            errors.append(f'{where}: {name}: hopper-only process without maxOutputCount (a blocked output is destroyed)')
        elif index.has(result):
            limit = index.stack_limit(result)
            if cap > limit - count:
                errors.append(f'{where}: {name}: maxOutputCount {cap} is above the stack limit of {result} ({limit}) '
                              f'minus the batch of {count}; the surplus of a batch would be destroyed')
    return errors


def patch_errors(element, index, active_names, where, added):
    """Errors for one patch operation (recursing into sequences and FindMod blocks); fills added names."""
    cls = element.get('Class', '')
    errors = []
    if cls == 'PatchOperationSequence':
        for li in element.findall('operations/li'):
            errors += patch_errors(li, index, active_names, where, added)
    elif cls == 'PatchOperationFindMod':
        names = {(li.text or '').strip().lower() for li in element.findall('mods/li')}
        branch = element.find('match') if names & active_names else element.find('nomatch')
        if branch is not None:
            errors += patch_errors(branch, index, active_names, where, added)
    elif cls == 'PatchOperationConditional':
        xpath = (element.findtext('xpath') or '').strip()
        branch = element.find('match') if index.matches(xpath) else element.find('nomatch')
        if branch is not None:
            errors += patch_errors(branch, index, active_names, where, added)
    elif cls in XPATH_OPERATIONS:
        xpath = (element.findtext('xpath') or '').strip()
        count = index.matches(xpath)
        if count != 1:
            errors.append(f'{where}: {cls} xpath matches {count} nodes, not 1: {xpath}')
        if cls == 'PatchOperationAdd' and xpath.endswith('/processes'):
            added.update((li.text or '').strip() for li in element.findall('value/li'))
    elif cls:
        errors.append(f'{where}: unhandled patch operation {cls} (extend the checker)')
    return errors


def check(mod_root, game, workshop, config, mods_folder_name='Mods'):
    """(errors, summary text) for the mod at mod_root against the installation; reads only."""
    version, full_version, active = refresh.read_mods_config(Path(config) / 'ModsConfig.xml')
    active = set(active)
    mods = refresh.scan(game, workshop, version)
    mod_root = Path(mod_root)
    own = refresh.parse_about(mod_root, 'Local')
    own_id = own.package_id if own else ''
    index = DefIndex()
    loaded_mods = 0
    for package_id, mod in mods.items():
        if package_id.lower() not in active or mod.package_id == own_id:
            continue  # the checked mod is read from the repository, not from an installed copy
        loaded_mods += 1
        for path in def_files(load_folders_for(mod.folder, version, active), 'Defs'):
            index.add_file(path)
    active_names = {mod.name.strip().lower() for pid, mod in mods.items() if pid.lower() in active}
    own_folders = load_folders_for(mod_root, version, active)
    own_defs = def_files(own_folders, 'Defs')
    for path in own_defs:
        index.add_file(path)
    errors = []
    processes = {}
    for path in own_defs:
        root = _read_xml(path)
        if root is None:
            errors.append(f'{path}: unreadable XML')
            continue
        for process in root.findall('PipeSystem.ProcessDef'):
            errors += process_errors(process, index, active, path.relative_to(mod_root).as_posix())
            wanted = process.get('MayRequire')
            if not wanted or any(pid.strip().lower() in active for pid in wanted.split(',')):
                processes[(process.findtext('defName') or '').strip()] = path
    added = set()
    patch_files = def_files(own_folders, 'Patches')
    for path in patch_files:
        root = _read_xml(path)
        if root is None:
            errors.append(f'{path}: unreadable XML')
            continue
        for operation in root.findall('Operation'):
            errors += patch_errors(operation, index, active_names, path.relative_to(mod_root).as_posix(), added)
    for name in sorted(set(processes) - added):
        errors.append(f'{processes[name].relative_to(mod_root).as_posix()}: {name} is defined but no patch adds it to a machine')
    for name in sorted(added - set(processes)):
        if name not in index.names:
            errors.append(f'a patch adds {name} to a machine, but no loaded Def has that name')
    summary = (f'RimWorld {full_version or version}: {loaded_mods} active mods, {len(index.names)} Def names; '
               f'{mod_root.name}: {len(own_folders)} folders loaded, {len(processes)} ProcessDefs, '
               f'{len(patch_files)} patch files, {len(added)} bills added to machines')
    if index.unreadable:
        summary += f'; {len(index.unreadable)} unreadable Def files skipped'
    return errors, summary


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.split('\n\n')[0])
    parser.add_argument('--mod', default=DEFAULT_MOD, help='mod folder name under mods/ (default PhobosFactoryAdditions)')
    parser.add_argument('--game', help='RimWorld folder (default: RIMWORLD_PATH, .local/install-settings.json, or Steam)')
    parser.add_argument('--workshop', help='Steam Workshop content folder for RimWorld')
    parser.add_argument('--config', help="the game's Config folder, which holds ModsConfig.xml")
    args = parser.parse_args(argv)
    game = deploy.game_path(args.game)
    workshop = deploy.local_path('workshopPath', 'RIMWORLD_WORKSHOP_PATH', refresh.default_workshop(game), args.workshop)
    config = deploy.local_path('rimworldConfigPath', 'RIMWORLD_CONFIG_PATH', refresh.DEFAULT_CONFIG, args.config)
    mod_root = ROOT / 'mods' / args.mod
    if not mod_root.is_dir():
        print(f'no mod folder {mod_root}', file=sys.stderr)
        return 2
    errors, summary = check(mod_root, game, workshop, config)
    print(summary)
    for error in errors:
        print('ERROR', error)
    print(f'{len(errors)} errors')
    return 1 if errors else 0


if __name__ == '__main__':
    sys.exit(main())
