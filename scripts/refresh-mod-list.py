"""Refresh the owner's mod list (docs/development/owner-mod-list.md) from the game's own files.

Reads ModsConfig.xml (what is switched on, in load order), every installed mod's About/About.xml (the
DLCs, the game's Mods folder and the Steam Workshop folder) and Steam's Workshop manifest (when each item
last updated). Rewrites only the document's generated blocks; the hand-written suggestions stay as they
are. Never writes to the game, its config or Steam. With --online it also asks Steam's public Web API
(no key, nothing personal sent) for the official Vanilla Expanded collection and saves the answer beside
the document, so later offline runs keep that table current.

    python scripts/refresh-mod-list.py             refresh the document
    python scripts/refresh-mod-list.py --online    also re-read the Vanilla Expanded collection
    python scripts/refresh-mod-list.py --check     change nothing; exit 1 if the document is out of date

Paths come from arguments, environment variables, .local/install-settings.json or defaults, as in
deploy.py: rimworldPath (RIMWORLD_PATH), workshopPath (RIMWORLD_WORKSHOP_PATH; default beside the game
in steamapps) and rimworldConfigPath (RIMWORLD_CONFIG_PATH; default in AppData/LocalLow).
"""
import argparse
from dataclasses import dataclass, field
import datetime as _dt
import json
from pathlib import Path
import re
import sys
import urllib.parse
import urllib.request
import xml.etree.ElementTree as ET

sys.path.insert(0, str(Path(__file__).resolve().parent))
import deploy  # noqa: E402  (shared path settings)

ROOT = Path(__file__).resolve().parents[1]
DOC = ROOT / 'docs' / 'development' / 'owner-mod-list.md'
VE_CACHE = ROOT / 'docs' / 'development' / 'vanilla-expanded-collection.json'
DEFAULT_CONFIG = Path.home() / 'AppData' / 'LocalLow' / 'Ludeon Studios' / 'RimWorld by Ludeon Studios' / 'Config'
APP_ID = '294100'
VE_COLLECTION_ID = '1884025115'  # "Vanilla Expanded", the official collection by Oskar Potocki
CE_PACKAGE_ID = 'ceteam.combatextended'
VE_PACKAGE_PREFIXES = ('vanillaexpanded.', 'oskarpotocki.', 'vanillaracesexpanded.')
STEAM_API = 'https://api.steampowered.com/ISteamRemoteStorage/'
STEAM_BATCH = 100
WORKSHOP_URL = 'https://steamcommunity.com/sharedfiles/filedetails/?id={}'
STEAM_SUFFIX = '_steam'  # the game's suffix for a Workshop copy of a mod that is also installed locally
SUGGESTIONS = re.compile(r'<!-- BEGIN SUGGESTIONS -->(.*?)<!-- END SUGGESTIONS -->', re.S)
WORKSHOP_LINK = re.compile(r'filedetails/\?id=(\d+)')
BARE_AMPERSAND = re.compile(r'&(?!(?:[A-Za-z]+|#\d+|#x[0-9A-Fa-f]+);)')
VERSION_TAG = re.compile(r'\d+\.\d+')
SERIES_SEPARATOR = re.compile(r'\s[-–—]\s')  # "Vanilla Animals Expanded — Endangered" and the like


@dataclass
class Mod:
    package_id: str
    name: str
    authors: str = ''
    source: str = 'Workshop'  # Ludeon, Local or Workshop
    workshop_id: str = ''
    versions: tuple = ()
    dependencies: list = field(default_factory=list)  # (accepted package IDs, display name)
    load_after: set = field(default_factory=set)
    load_before: set = field(default_factory=set)
    incompatible: set = field(default_factory=set)
    folder: Path = None


# ---- Reading the game's files ----

def _named(element, tag):
    return [child for child in element if isinstance(child.tag, str) and child.tag.lower() == tag.lower()]


def _first(element, tag):
    found = _named(element, tag) if element is not None else []
    return found[0] if found else None


def _text(element, tag):
    found = _first(element, tag)
    return (found.text or '').strip() if found is not None else ''


def _items(element, tag):
    """Lower-cased <li> texts of element/tag."""
    found = _first(element, tag)
    if found is None:
        return []
    return [li.text.strip().lower() for li in _named(found, 'li') if li.text and li.text.strip()]


def _parse_xml(path):
    """An About.xml root, forgiving the bare ampersands some authors leave in; None if unreadable."""
    raw = path.read_text(encoding='utf-8-sig', errors='replace')
    for attempt in (raw, BARE_AMPERSAND.sub('&amp;', raw)):
        try:
            return ET.fromstring(attempt)
        except ET.ParseError:
            continue
    return None


def _about_file(folder):
    for about_dir in (d for d in folder.iterdir() if d.is_dir() and d.name.lower() == 'about'):
        for candidate in about_dir.iterdir():
            if candidate.name.lower() == 'about.xml':
                return candidate
    return None


def parse_about(folder, source='Workshop', workshop_id='', game_version=''):
    """The Mod described by folder/About/About.xml, or None when there is no readable one."""
    folder = Path(folder)
    about = _about_file(folder)
    root = _parse_xml(about) if about else None
    if root is None:
        return None
    package_id = _text(root, 'packageId').lower()
    if not package_id:
        return None
    by_version = f'v{game_version}'
    author_list = _first(root, 'authors')
    listed = [li.text.strip() for li in _named(author_list, 'li') if li.text] if author_list is not None else []
    authors = _text(root, 'author') or ', '.join(listed)
    dependencies = []
    lists = [_first(root, 'modDependencies'), _first(_first(root, 'modDependenciesByVersion'), by_version)]
    for container in (c for c in lists if c is not None):
        for li in _named(container, 'li'):
            accepted = [_text(li, 'packageId').lower()] + _items(li, 'alternativePackageIds')
            accepted = tuple(pid for pid in accepted if pid)
            if accepted:
                dependencies.append((accepted, _text(li, 'displayName') or accepted[0]))

    def rule(tag):
        found = set(_items(root, tag)) | set(_items(root, 'force' + tag[0].upper() + tag[1:]))
        found |= set(_items(_first(root, tag + 'ByVersion'), by_version))
        return found

    return Mod(
        package_id=package_id,
        name=_text(root, 'name') or package_id,
        authors=authors,
        source=source,
        workshop_id=workshop_id,
        versions=tuple(_items(root, 'supportedVersions')),
        dependencies=dependencies,
        load_after=rule('loadAfter'),
        load_before=rule('loadBefore'),
        incompatible=set(_items(root, 'incompatibleWith')) | set(_items(_first(root, 'incompatibleWithByVersion'), by_version)),
        folder=folder,
    )


def read_mods_config(path):
    """(game version like 1.6, full version text, active package IDs in load order) from ModsConfig.xml."""
    root = ET.parse(path).getroot()
    full = (root.findtext('version') or '').strip()
    match = re.match(r'(\d+\.\d+)', full)
    active_list = root.find('activeMods')
    active = [li.text.strip().lower() for li in (active_list if active_list is not None else []) if li.text]
    return (match.group(1) if match else ''), full, active


def default_workshop(game):
    """steamapps/workshop/content/294100 beside steamapps/common/RimWorld."""
    return Path(game).parents[1] / 'workshop' / 'content' / APP_ID


def scan(game, workshop, game_version=''):
    """Package ID (as ModsConfig.xml names it) -> Mod, for the DLCs, the Mods folder and the Workshop."""
    mods = {}
    for source, root in (('Ludeon', Path(game) / 'Data'), ('Local', Path(game) / 'Mods'), ('Workshop', Path(workshop))):
        if not root.is_dir():
            continue
        for folder in sorted(root.iterdir(), key=lambda p: p.name):
            if not folder.is_dir():
                continue
            mod = parse_about(folder, source, folder.name if source == 'Workshop' else '', game_version)
            if mod is None:
                continue
            key = mod.package_id
            if key in mods and source == 'Workshop':
                key += STEAM_SUFFIX
            mods.setdefault(key, mod)
    return mods


def read_workshop_times(manifest):
    """Workshop ID -> last update (Unix time) from Steam's appworkshop_294100.acf; {} if absent."""
    manifest = Path(manifest)
    if not manifest.is_file():
        return {}
    text = manifest.read_text(encoding='utf-8', errors='replace')
    return {m.group(1): int(m.group(2)) for m in re.finditer(r'"(\d+)"\s*\{[^{}]*?"timeupdated"\s*"(\d+)"', text)}


def ce_patch_names(mods):
    """Normalised names of the mods Combat Extended ships its own patches for (its ModPatches folders)."""
    ce = mods.get(CE_PACKAGE_ID)
    folder = ce.folder / 'ModPatches' if ce and ce.folder else None
    if not folder or not folder.is_dir():
        return set()
    return {normalise(d.name) for d in folder.iterdir() if d.is_dir()}


def normalise(name):
    return re.sub(r'[^a-z0-9]', '', name.lower())


# ---- Steam's public Web API (only with --online) ----

def _steam_post(method, data):
    request = urllib.request.Request(STEAM_API + method, data=urllib.parse.urlencode(data).encode())
    with urllib.request.urlopen(request, timeout=60) as response:
        return json.load(response)['response']


def _version_key(version):
    return tuple(int(part) for part in version.split('.'))


def fetch_collection(collection_id=VE_COLLECTION_ID, post=_steam_post, today=None):
    """Title, game versions and last update of every item in a Workshop collection."""
    details = post('GetCollectionDetails/v1/', {'collectioncount': 1, 'publishedfileids[0]': collection_id})
    children = sorted(details['collectiondetails'][0].get('children', []), key=lambda c: c.get('sortorder', 0))
    ids = [child['publishedfileid'] for child in children]
    items = []
    for start in range(0, len(ids), STEAM_BATCH):
        chunk = ids[start:start + STEAM_BATCH]
        data = {'itemcount': len(chunk)}
        data.update({f'publishedfileids[{i}]': item for i, item in enumerate(chunk)})
        for item in post('GetPublishedFileDetails/v1/', data)['publishedfiledetails']:
            if not item.get('title'):
                continue  # removed from the Workshop
            versions = sorted((t['tag'] for t in item.get('tags', []) if VERSION_TAG.fullmatch(t.get('tag', ''))), key=_version_key)
            items.append({'id': item['publishedfileid'], 'title': item['title'], 'versions': versions,
                          'updated': _date(item.get('time_updated'))})
    return {'collection': collection_id, 'checked': (today or _dt.date.today()).isoformat(), 'items': items}


# ---- Checks ----

@dataclass
class ModList:
    game_version: str
    full_version: str
    active: list
    mods: dict
    as_of: str = ''
    times: dict = field(default_factory=dict)
    collection: dict = None
    ce_patches: set = field(default_factory=set)

    def is_on(self, package_id):
        on = self._on()
        return package_id in on or package_id + STEAM_SUFFIX in on

    def _on(self):
        return set(self.active)

    def position(self, package_id):
        for index, active in enumerate(self.active):
            if active in (package_id, package_id + STEAM_SUFFIX):
                return index
        return None

    def name(self, package_id):
        mod = self.mods.get(package_id) or self.mods.get(package_id + STEAM_SUFFIX)
        return mod.name if mod else package_id

    def by_workshop_id(self):
        return {mod.workshop_id: key for key, mod in self.mods.items() if mod.workshop_id}

    def workshop_status(self, workshop_id):
        key = self.by_workshop_id().get(workshop_id)
        if key is None:
            return 'No'
        return 'On' if key in self._on() else 'Subscribed, off'


def check(mod_list):
    """Problems the game's own rules would show: missing requirements, versions, load order, clashes."""
    mods = mod_list.mods
    found = {'not_installed': [], 'missing': [], 'version': [], 'order': [], 'incompatible': []}
    seen_pairs = set()
    for index, package_id in enumerate(mod_list.active):
        mod = mods.get(package_id)
        if mod is None:
            found['not_installed'].append(package_id)
            continue
        for accepted, display in mod.dependencies:
            if not any(mod_list.is_on(pid) for pid in accepted):
                found['missing'].append((mod.name, display))
        if mod.source != 'Ludeon' and mod_list.game_version and mod_list.game_version not in mod.versions:
            found['version'].append((mod.name, ', '.join(mod.versions) or 'none'))
        for other in sorted(mod.load_after):
            other_index = mod_list.position(other)
            if other_index is not None and other_index > index:
                found['order'].append((mod.name, 'after', mod_list.name(other)))
        for other in sorted(mod.load_before):
            other_index = mod_list.position(other)
            if other_index is not None and other_index < index:
                found['order'].append((mod.name, 'before', mod_list.name(other)))
        for other in sorted(mod.incompatible):
            pair = frozenset((mod.package_id, other))
            if mod_list.is_on(other) and pair not in seen_pairs:
                seen_pairs.add(pair)
                found['incompatible'].append((mod.name, mod_list.name(other)))
    return found


def suggestion_status(text, mod_list):
    """(workshop ID, status) for every Workshop link in the hand-written suggestions, in order."""
    block = SUGGESTIONS.search(text)
    ids = list(dict.fromkeys(WORKSHOP_LINK.findall(block.group(1)))) if block else []
    return [(item, mod_list.workshop_status(item)) for item in ids]


# ---- Rendering ----

def _date(unix_time):
    return _dt.datetime.fromtimestamp(int(unix_time), _dt.timezone.utc).date().isoformat() if unix_time else ''


def cell(text):
    text = str(text).replace('\r', ' ').replace('\n', ' ').strip()
    for old, new in (('\\', '\\\\'), ('|', '\\|'), ('<', '&lt;'), ('[', '\\['), (']', '\\]'), ('*', '\\*'), ('_', '\\_')):
        text = text.replace(old, new)
    return text


def link(name, workshop_id):
    return f'[{cell(name)}]({WORKSHOP_URL.format(workshop_id)})' if workshop_id else cell(name)


def short_authors(authors):
    parts = [p.strip() for p in re.split(r',|;|&| and ', authors) if p.strip()]
    if len(parts) <= 2:
        return cell(', '.join(parts))
    return cell(f'{parts[0]}, {parts[1]} and {len(parts) - 2} more')


def _bullet(label, items, none_text, describe):
    if not items:
        return f'- **{label}:** {none_text}'
    lines = [f'- **{label}: {len(items)}**']
    lines += [f'  - {describe(item)}' for item in items]
    return '\n'.join(lines)


def render_summary(mod_list):
    on = [mod_list.mods[p] for p in mod_list.active if p in mod_list.mods]
    counts = {source: sum(1 for m in on if m.source == source) for source in ('Ludeon', 'Workshop', 'Local')}
    off = inactive(mod_list)
    playable = [m for m in on if m.source != 'Ludeon']
    ready = sum(1 for m in playable if '1.7' in m.versions)
    local = f', {counts["Local"]} local' if counts['Local'] else ''
    return (f'**As of {mod_list.as_of}** (the date of the game\'s mod list): RimWorld {mod_list.full_version}, '
            f'**{len(mod_list.active)} mods switched on** ({counts["Ludeon"]} game and DLCs, '
            f'{counts["Workshop"]} Workshop{local}), and **{len(off)} more subscribed but switched off**. '
            f'{ready} of the {len(playable)} switched-on mods already declare RimWorld 1.7.')


def render_checks(mod_list, suggestions):
    found = check(mod_list)
    lines = [
        _bullet('Missing requirements', found['missing'], 'none.', lambda f: f'{cell(f[0])} needs **{cell(f[1])}**'),
        _bullet('Switched on but not installed', found['not_installed'], 'none.', lambda f: f'`{f}`'),
        _bullet(f'Not marked for {mod_list.game_version}', found['version'], 'none; every switched-on mod declares it.',
                lambda f: f'{cell(f[0])} (declares {cell(f[1])})'),
        _bullet('Against a mod\'s own load-order rule', found['order'], 'none.',
                lambda f: f'{cell(f[0])} should load {f[1]} {cell(f[2])}'),
        _bullet('Marked incompatible with each other', found['incompatible'], 'none.',
                lambda f: f'{cell(f[0])} and {cell(f[1])}'),
    ]
    names = {mod.workshop_id: mod.name for mod in mod_list.mods.values() if mod.workshop_id}
    taken = [(i, s) for i, s in suggestions if s != 'No']
    lines.append(_bullet('Suggested mods you already have (on, or subscribed but off)', taken, 'none yet.',
                         lambda f: f'{link(names.get(f[0], f[0]), f[0])}: **{f[1].lower()}**'))
    return '\n'.join(lines)


def _table(header, rows):
    out = ['| ' + ' | '.join(header) + ' |', '| ' + ' | '.join('---' for _ in header) + ' |']
    out += ['| ' + ' | '.join(row) + ' |' for row in rows]
    return '\n'.join(out)


def family(mod):
    if mod.source == 'Ludeon':
        return 'Game and DLCs'
    if mod.package_id.startswith(VE_PACKAGE_PREFIXES):
        return 'Vanilla Expanded'
    return 'Everything else'


def render_active(mod_list):
    groups = {'Game and DLCs': [], 'Vanilla Expanded': [], 'Everything else': []}
    for index, package_id in enumerate(mod_list.active, start=1):
        mod = mod_list.mods.get(package_id)
        if mod:
            groups[family(mod)].append((index, mod))
    parts = []
    for title, members in groups.items():
        members.sort(key=lambda pair: (pair[0] if title == 'Game and DLCs' else 0, pair[1].name.lower()))
        rows = [[str(index), link(mod.name, mod.workshop_id), short_authors(mod.authors), f'`{mod.package_id}`',
                 _date(mod_list.times.get(mod.workshop_id)) or '—', 'yes' if '1.7' in mod.versions else '—']
                for index, mod in members]
        parts.append(f'### {title} ({len(members)})\n\n'
                     + _table(['Load #', 'Mod', 'Author', 'Package ID', 'Updated', '1.7'], rows))
    return '\n\n'.join(parts)


def inactive(mod_list):
    on = set(mod_list.active)
    return sorted((m for key, m in mod_list.mods.items() if m.source != 'Ludeon' and key not in on),
                  key=lambda m: m.name.lower())


def render_inactive(mod_list):
    off = inactive(mod_list)
    if not off:
        return 'None: everything subscribed is switched on.'
    rows = [[link(m.name, m.workshop_id), short_authors(m.authors), f'`{m.package_id}`',
             _date(mod_list.times.get(m.workshop_id)) or '—',
             'yes' if mod_list.game_version in m.versions else '**no**'] for m in off]
    return _table(['Mod', 'Author', 'Package ID', 'Updated', mod_list.game_version or 'Version'], rows)


def render_collection(mod_list):
    collection = mod_list.collection
    if not collection or not collection.get('items'):
        return 'Not read yet: run `python scripts/refresh-mod-list.py --online`.'
    items = collection['items']
    statuses = {item['id']: mod_list.workshop_status(item['id']) for item in items}
    on = sum(1 for s in statuses.values() if s == 'On')
    off = sum(1 for s in statuses.values() if s == 'Subscribed, off')
    ce = bool(mod_list.ce_patches)
    rows = []
    for item in sorted(items, key=lambda i: (SERIES_SEPARATOR.split(i['title'])[0].lower(), i['title'].lower())):
        status = statuses[item['id']]
        row = [link(item['title'], item['id']), f'**{status}**' if status == 'On' else status]
        if ce:
            row.append('yes' if normalise(item['title']) in mod_list.ce_patches else '—')
        row += ['yes' if mod_list.game_version in item['versions'] else '**no**', item.get('updated', '')]
        rows.append(row)
    header = ['Mod', 'You have it'] + (['CE patch'] if ce else []) + [mod_list.game_version or 'Version', 'Updated']
    ce_note = (' **CE patch** says whether Combat Extended ships its own patch for that mod (read from CE\'s '
               '`ModPatches` folder); "—" means it doesn\'t, so its guns and armour rely on the mod\'s own CE '
               'support or on CE\'s autopatchers.') if ce else ''
    return (f'Read from [the collection]({WORKSHOP_URL.format(collection["collection"])}) on {collection["checked"]}: '
            f'{len(items)} mods. You have **{on} switched on** and {off} subscribed but switched off.{ce_note}\n\n'
            + _table(header, rows))


BLOCKS = ('summary', 'checks', 'vanilla-expanded', 'active', 'inactive')


def replace_block(text, name, body):
    """text with the generated block called name replaced by body."""
    begin, end = f'<!-- BEGIN GENERATED: {name} -->', f'<!-- END GENERATED: {name} -->'
    pattern = re.compile(re.escape(begin) + r'.*?' + re.escape(end), re.S)
    if not pattern.search(text):
        raise ValueError(f'the document has no generated block called {name}')
    return pattern.sub(lambda _: f'{begin}\n{body.rstrip()}\n{end}', text, count=1)


def render(text, mod_list):
    """The document with every generated block brought up to date."""
    bodies = {
        'summary': render_summary(mod_list),
        'checks': render_checks(mod_list, suggestion_status(text, mod_list)),
        'vanilla-expanded': render_collection(mod_list),
        'active': render_active(mod_list),
        'inactive': render_inactive(mod_list),
    }
    for name in BLOCKS:
        text = replace_block(text, name, bodies[name])
    return text


# ---- Command line ----

def load(game, workshop, config, collection_file=VE_CACHE):
    mods_config = Path(config) / 'ModsConfig.xml'
    if not mods_config.is_file():
        raise FileNotFoundError(f'no ModsConfig.xml in {config}; is the config path right?')
    game_version, full_version, active = read_mods_config(mods_config)
    mods = scan(game, workshop, game_version)
    collection_file = Path(collection_file)
    return ModList(
        game_version=game_version,
        full_version=full_version,
        active=active,
        mods=mods,
        as_of=_dt.date.fromtimestamp(mods_config.stat().st_mtime).isoformat(),
        times=read_workshop_times(Path(workshop).parents[1] / f'appworkshop_{APP_ID}.acf'),
        collection=json.loads(collection_file.read_text(encoding='utf-8')) if collection_file.is_file() else None,
        ce_patches=ce_patch_names(mods) if CE_PACKAGE_ID in active else set(),
    )


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('--online', action='store_true', help='also re-read the Vanilla Expanded collection from Steam')
    parser.add_argument('--check', action='store_true', help='change nothing; exit 1 if the document is out of date')
    parser.add_argument('--game', help='RimWorld folder')
    parser.add_argument('--workshop', help='RimWorld Workshop content folder (steamapps/workshop/content/294100)')
    parser.add_argument('--config', help="the game's Config folder, which holds ModsConfig.xml")
    parser.add_argument('--doc', default=str(DOC), help='the document to refresh')
    args = parser.parse_args(argv)

    game = deploy.game_path(args.game)
    workshop = deploy.local_path('workshopPath', 'RIMWORLD_WORKSHOP_PATH', default_workshop(game), args.workshop)
    config = deploy.local_path('rimworldConfigPath', 'RIMWORLD_CONFIG_PATH', DEFAULT_CONFIG, args.config)
    if args.online and not args.check:
        VE_CACHE.write_text(json.dumps(fetch_collection(), indent=1) + '\n', encoding='utf-8')
    mod_list = load(game, workshop, config)

    doc = Path(args.doc)
    text = doc.read_text(encoding='utf-8')
    updated = render(text, mod_list)
    found = check(mod_list)
    problems = sum(len(items) for items in found.values())
    print(f'{len(mod_list.active)} switched on, {len(inactive(mod_list))} subscribed but off; '
          f'{problems} problem(s) by the mods\' own rules.')
    if args.check:
        print('The document is up to date.' if updated == text else 'The document is out of date.')
        return 0 if updated == text else 1
    if updated != text:
        doc.write_text(updated, encoding='utf-8', newline='\n')
        print(f'Refreshed {doc.relative_to(ROOT) if doc.is_relative_to(ROOT) else doc}.')
    else:
        print('Nothing changed.')
    return 0


if __name__ == '__main__':
    try:
        sys.exit(main())
    except (FileNotFoundError, ValueError) as error:
        print(error, file=sys.stderr)
        sys.exit(1)
