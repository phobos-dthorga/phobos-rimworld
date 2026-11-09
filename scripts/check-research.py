"""Check our research tree (docs/development/research-tree.md): what the game loads silently, or gets wrong without saying.

Reads every XML file under mods/ and, for hard requirements, the research the installed game defines. Writes nothing.

For each project on one of our tabs:

- its defName starts PhobosResearch_;
- it has a hard requirement (no MayRequire), since a project whose requirements all drop away opens at once;
- its hard requirements reach research that is not ours, with no loops;
- each requirement is ours, or the game's, or is conditional (MayRequire, or in a Mods/ folder loaded only with that mod);
- it names a tab we define, sits within the screen, and is not so close to another project that the game moves it;
- it lists what it replaces, for the one-time grant to existing colonies, or says it is brand new with an empty <replaces />.

Across mods/: every PhobosResearch_ name used anywhere is a project we define, every upgrade's research is ours, the game's or
conditional, and every upgrade tree of ours carries the marker that makes that research count.

A conditional name is not looked up: a misspelt one would silently drop in game. Spot-check them with query-defs.py who.

    python scripts/check-research.py

Paths as in deploy.py. Without an installed game the check of hard requirements against the game's research is skipped, and says so.
Exit code 1 on any error. An offline check is not an in-game test.
"""
import argparse
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(Path(__file__).resolve().parent))

PREFIX = 'PhobosResearch_'
PROJECT_TAG = 'ResearchProjectDef'
TAB_TAG = 'ResearchTabDef'
TREE_TAG = 'Vehicles.UpgradeTreeDef'
REPLACES_CLASS = 'Phobos.Framework.Research.ResearchReplacesExtension'
MARKER_CLASS = 'Phobos.VehicleSystems.PhobosEnforceUpgradeResearch'
MIN_DX, MIN_DY, MAX_Y = 0.5, 0.25, 6.5  # the game nudges closer pairs and clamps Y (ResearchProjectDef)
NAME_IN_TEXT = re.compile(PREFIX + r'\w+')
CONDITIONS = ('MayRequire', 'MayRequireAnyOf')


def parse(path):
    try:
        return ET.parse(path).getroot()
    except ET.ParseError:
        return None  # check-mod-layout.py reports files that do not parse


def is_conditional_file(path, mods_dir):
    """True for a file in a mod's Mods/ folder, which LoadFolders.xml loads only with that mod."""
    parts = path.relative_to(mods_dir).parts
    return len(parts) > 1 and parts[1] == 'Mods'


def text_of(element):
    return (element.text or '').strip()


def has_condition(element):
    return any(element.get(name) for name in CONDITIONS)


class Project:
    def __init__(self, element, path, conditional_file, parents):
        self.element = element
        self.path = path
        self.conditional_file = conditional_file
        self.parents = parents
        self.name = (element.findtext('defName') or '').strip()

    def field(self, name):
        """A field, looked up through ParentName as the game's XML inheritance would."""
        element, seen = self.element, set()
        while element is not None:
            value = element.findtext(name)
            if value is not None:
                return value.strip()
            parent = element.get('ParentName')
            if not parent or parent in seen:
                return None
            seen.add(parent)
            element = self.parents.get(parent)
        return None

    def requirements(self):
        return self.element.findall('prerequisites/li')

    def replaces(self):
        for li in self.element.findall('modExtensions/li'):
            if li.get('Class') == REPLACES_CLASS:
                return li.find('replaces')
        return None


def game_research(game):
    """The research the installed game and its expansions define, or None when no game is available."""
    if game is None or not (game / 'Data').is_dir():
        return None
    names = set()
    for path in (game / 'Data').glob('*/Defs/**/*.xml'):
        root = parse(path)
        if root is None:
            continue
        for element in root.findall(PROJECT_TAG):
            name = (element.findtext('defName') or '').strip()
            if name:
                names.add(name)
    return names


def read_mods(mods_dir):
    """Our XML, parsed: projects, tabs, every (path, root) and every (path, text)."""
    files = []
    for path in sorted(mods_dir.glob('*/**/*.xml')):
        if 'Languages' in path.relative_to(mods_dir).parts:
            continue
        root = parse(path)
        if root is not None:
            files.append((path, root))
    parents = {}
    for _, root in files:
        for element in root.findall(PROJECT_TAG):
            if element.get('Name'):
                parents[element.get('Name')] = element
    projects, tabs = [], set()
    for path, root in files:
        for element in root.findall(PROJECT_TAG):
            if element.get('Abstract', '').lower() != 'true':
                projects.append(Project(element, path, is_conditional_file(path, mods_dir), parents))
        for element in root.findall(TAB_TAG):
            tabs.add((element.findtext('defName') or '').strip())
    return projects, tabs, files


def chain_errors(project, ours):
    """Whether following hard requirements reaches research that is not ours, and any loop on the way."""
    errors, reaches = [], [False]

    def walk(current, path):
        for li in current.requirements():
            if has_condition(li):
                continue
            name = text_of(li)
            next_project = ours.get(name)
            if next_project is None:
                reaches[0] = True
            elif name in path:
                errors.append(f'{project.name}: its requirements loop through {name}')
            else:
                walk(next_project, path | {name})

    walk(project, {project.name})
    if not errors and not reaches[0]:
        errors.append(f'{project.name}: its hard requirements never reach research that is not ours')
    return errors


def reference_error(owner, name, conditional, ours, known):
    if name in ours or conditional:
        return None
    if known is not None and name not in known:
        return f'{owner}: research {name} is neither ours nor the game\'s, and is not conditional'
    return None


def position_errors(projects):
    errors, placed = [], {}
    for project in projects:
        try:
            x = float(project.field('researchViewX'))
            y = float(project.field('researchViewY'))
        except (TypeError, ValueError):
            errors.append(f'{project.name}: needs a numeric researchViewX and researchViewY')
            continue
        if y > MAX_Y or y < 0:
            errors.append(f'{project.name}: researchViewY {y} is off the screen (0 to {MAX_Y})')
        placed.setdefault(project.field('tab'), []).append((project.name, x, y))
    for entries in placed.values():
        for i, (name, x, y) in enumerate(entries):
            for other, other_x, other_y in entries[i + 1:]:
                if abs(x - other_x) < MIN_DX and abs(y - other_y) < MIN_DY:
                    errors.append(f'{name} and {other} are too close: the game would move one')
    return errors


def upgrade_errors(files, mods_dir, ours, known):
    """Every upgrade's research, and the marker on every tree of ours."""
    errors = []
    for path, root in files:
        conditional = is_conditional_file(path, mods_dir)
        where = path.relative_to(mods_dir).as_posix()
        for tree in root.findall(TREE_TAG):
            marked = any(li.get('Class') == MARKER_CLASS for li in tree.findall('modExtensions/li'))
            if not marked:
                errors.append(f'{where}: {(tree.findtext("defName") or "?").strip()} lacks {MARKER_CLASS}, so its research does nothing')
        for node in root.iter('li'):
            if node.find('key') is None:
                continue
            for li in node.findall('researchPrerequisites/li'):
                error = reference_error(f'{where}: upgrade {text_of(node.find("key"))}', text_of(li), conditional or has_condition(li), ours, known)
                if error:
                    errors.append(error)
    return errors


def check(mods_dir, known):
    """Errors for the tree under mods_dir. known is the game's research names, or None to skip that comparison."""
    projects, tabs, files = read_mods(mods_dir)
    defined = {project.name: project for project in projects}
    ours = {name: project for name, project in defined.items() if name.startswith(PREFIX)}
    ours.update({project.name: project for project in projects if project.field('tab') in tabs})
    errors = []
    for project in ours.values():
        name = project.name
        if not name.startswith(PREFIX):
            errors.append(f'{name}: a project on our tab must start {PREFIX}')
        if project.field('tab') not in tabs:
            errors.append(f'{name}: tab {project.field("tab")} is not one we define')
        if not any(not has_condition(li) for li in project.requirements()):
            errors.append(f'{name}: needs a hard requirement, or it opens at once when its mods are absent')
        else:
            errors.extend(chain_errors(project, ours))
        for li in project.requirements():
            error = reference_error(name, text_of(li), project.conditional_file or has_condition(li), defined, known)
            if error:
                errors.append(error)
        if project.replaces() is None:
            errors.append(f'{name}: lists nothing it replaces, so an existing colony can never be granted it; a brand new project says so with an empty <replaces />')
    errors.extend(position_errors(ours.values()))
    errors.extend(upgrade_errors(files, mods_dir, defined, known))
    for path in sorted(mods_dir.glob('*/**/*.xml')):
        if 'Languages' in path.relative_to(mods_dir).parts:
            continue
        for name in sorted(set(NAME_IN_TEXT.findall(path.read_text(encoding='utf-8')))):
            if name not in defined:
                errors.append(f'{path.relative_to(mods_dir).as_posix()}: {name} is not a project we define')
    return errors


def find_game(override):
    import deploy
    try:
        return Path(deploy.game_path(override))
    except (SystemExit, Exception):  # no installed game: the comparison is skipped and reported
        return None


def main(argv=None):
    parser = argparse.ArgumentParser(description='Check the D\'thorga research tree.')
    parser.add_argument('--game', help='RimWorld install folder')
    parser.add_argument('--mods', default=str(ROOT / 'mods'), help='folder of mods to check')
    args = parser.parse_args(argv)
    known = game_research(find_game(args.game))
    if known is None:
        print('No installed game found: hard requirements were not compared with the game\'s research.')
    errors = check(Path(args.mods), known)
    for error in errors:
        print(error)
    print(f'{len(errors)} problem(s) in the research tree.')
    return 1 if errors else 0


if __name__ == '__main__':
    sys.exit(main())
