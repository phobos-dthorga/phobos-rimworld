"""The modders' reference website: D'thorga Framework's C# reference, its modding guides, and how each XML-facing type is written.

The owner's decision D3 (23 Nov 2026): a reference site for modders, built with DocFX from the C# `///` comments and the Markdown
guides, to be published on GitHub Pages. Modders write XML, so every type they name in XML (a comp's properties, a Def, a mod
extension, a vehicle system's input, condition or effect) gets an example of how it is written: its element or `<li Class="...">`
with every public field and that field's default, read from our source.

    python scripts/reference-site.py list                 the XML-facing types, by kind, and which have no <summary>
    python scripts/reference-site.py check                fails when an XML-facing type has no <summary> and is not a known gap
    python scripts/reference-site.py check --write-baseline   records today's gaps as known (config/reference-site.json)
    python scripts/reference-site.py pages --work DIR     writes the generated pages only (no DocFX)
    python scripts/reference-site.py build                generates the pages, runs DocFX, writes the site
    python scripts/reference-site.py verify               the gate's step: the whole build in a throwaway folder; fails on any
                                                          DocFX warning or error (needs DocFX)

- **Our source is read as text, never compiled or loaded**: phobos-modkit's C# declaration scanner finds the types and fields; no
  assembly of ours or anyone's is loaded or run.
- **DocFX reads the C# files themselves** ("source-file mode"), with only the game's reference assemblies and Harmony from NuGet as
  references, so it needs no build and runs on GitHub's machines, where the other mods' assemblies (Vehicle Framework and the rest)
  are absent and may not be copied. Types from those mods are left unresolved (`allowCompilationErrors`): a page then names such a
  base class without linking it. The owner's PC builds the same site as GitHub does.
- The DocFX project is `docs/reference/` (docfx.json, toc.yml, index.md); its `metadata` paths are relative to that folder and are
  made absolute here, its `references` are filled in here from the NuGet cache. Everything generated goes to the work folder and the
  site folder, by default in the kit's cache (`rimworld-reference-site`, on K: on the owner's PC); `--work` and `--site` choose
  others (GitHub's workflow does).
- **Every page's footer says when the site was built**, from which version of the Framework and which commit (owner, 28 Nov
  2026), added to the run's copy of docfx.json, so the committed project never changes.
- **Four pages for modders are generated from the code and the changelogs** on every build (28 Nov 2026), so none can fall
  behind: the extension points (public interfaces and abstract classes), what we patch (the kit's Harmony reader, as doc-tables'
  patch list), the dev actions (their [DebugAction] attributes) and the changelog lines marked for modders.
- The work folder is emptied before each build, and only when it holds this tool's marker file, so a mistyped `--work` never
  empties anything else.
"""
from __future__ import annotations

import argparse
from dataclasses import dataclass, field
import fnmatch
import html
import io
import json
import os
from pathlib import Path
import re
import shutil
import stat
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(Path(__file__).resolve().parent))
import _modkit  # noqa: E402  (the shared tools' import path)
import _csharp  # noqa: E402  (comments blanked with strings kept; constants)

_modkit.ensure()
from phobos_modkit.code.scan import ATTRIBUTES, declarations  # noqa: E402
from phobos_modkit.progress import Progress  # noqa: E402

CONFIG = 'config/reference-site.json'
PROJECT = 'docs/reference'
MARKER = '.reference-site'
CACHE_NAMESPACE = 'rimworld-reference-site'
TYPE_KINDS = ('class', 'record')
SKIPPED_DIRS = {'obj', 'bin'}
FIELD_MODIFIERS = {'public', 'protected', 'internal', 'private', 'new', 'required', 'volatile', 'unsafe'}
EXCLUDED_MODIFIERS = {'static', 'const', 'readonly'}
NUMBER = re.compile(r'^-?(?:\d[\d_]*\.?\d*|\.\d+)(?:[eE][+-]?\d+)?[fFdDmMuUlL]*$')
NUMBER_TYPES = {'int', 'float', 'double', 'long', 'short', 'byte', 'uint', 'ulong', 'ushort', 'sbyte', 'decimal'}
COLLECTION = re.compile(r'^(?:System\.Collections\.Generic\.)?(?:List|IList|HashSet|IEnumerable|ICollection|IReadOnlyList)<(.+)>$')
DICTIONARY = re.compile(r'^(?:System\.Collections\.Generic\.)?Dictionary<(.+)>$')
TARGET_NEW = re.compile(r'^new\s*(?P<type>[\w.]+)?\s*\((?P<args>.*)\)$')       # new IntRange(1, 3), or new(1, 3)
CURVE_POINT = re.compile(r'^new\s*(?:CurvePoint)?\s*\((?P<args>.*)\)$')
ARITHMETIC = re.compile(r'^[\d\s.fFdD+\-*/()]+$')
# The game's own constants a default is sometimes written with (RimWorld 1.7's GenDate and GenTicks).
GAME_CONSTANTS = {'GenDate.TicksPerHour': '2500', 'GenDate.TicksPerDay': '60000', 'GenDate.TicksPerTwelfth': '150000',
                  'GenDate.TicksPerQuadrum': '900000', 'GenDate.TicksPerYear': '3600000', 'GenTicks.TicksPerRealSecond': '60'}
EMPTY_NEW = re.compile(r'^(?:new\s*(?:[\w.<>, \[\]?]+)?\s*\(\s*\)|new\s*(?:[\w.<>, ?]+)?\s*\[\s*0?\s*\]|\[\s*\]|'
                       r'(?:System\.)?Array\.Empty<[^>]*>\(\s*\))$')
NEW_WITH_ITEMS = re.compile(r'^(?:new\s*(?:[\w.<>, \[\]?]+)?\s*(?:\(\s*\))?\s*\{(?P<braced>.*)\}|\[(?P<bracketed>.*)\])$', re.DOTALL)
LINK = re.compile(r'(?P<bang>!?)\[(?P<text>[^\]]*)\]\((?P<target>[^)\s]+)\)')
PACKAGE = re.compile(r'<PackageReference\s+Include="(?P<name>[^"]+)"\s+Version="(?P<version>[^"]+)"')
TAG = re.compile(r'<[^>]+>')
SEE = re.compile(r'<(?:see|seealso)\s+(?:cref|langword|href)="(?:[A-Z]:)?(?P<name>[^"]+)"\s*/>')
PARAMREF = re.compile(r'<(?:paramref|typeparamref)\s+name="(?P<name>[^"]+)"\s*/>')
CODE = re.compile(r'<c>(?P<text>.*?)</c>', re.DOTALL)


class SiteError(RuntimeError):
    pass


# ----------------------------------------------------------------------------------------------------------------------------
# Reading our source


@dataclass
class FieldInfo:
    name: str
    type: str
    initial: str | None          # the initialiser as written, None when there is none
    summary: str = ''


@dataclass
class TypeInfo:
    full: str                    # Namespace.Outer.Inner
    name: str
    namespace: str
    kind: str
    base: str | None             # the base class as written, generics and namespace kept
    public: bool
    abstract: bool
    path: str                    # relative to the repository root
    line: int
    summary: str = ''
    documented: bool = False     # has a <summary> or an <inheritdoc/>
    nested_in: str | None = None
    fields: list = field(default_factory=list)
    category: str | None = None  # the XML kind, once classified
    chain: list = field(default_factory=list)   # our own bases, nearest first

    @property
    def xml_class(self):
        """The name XML gives the type: nested types join with '+', as the game's type lookup reads them."""
        if self.nested_in:
            return self.nested_in + '+' + self.name
        return self.full


def doc_comment(lines, start):
    """The /// lines directly above a declaration (start is its first line, 1-based, attributes included), without the slashes."""
    found = []
    index = start - 2
    while index >= 0:
        text = lines[index].strip()
        if text.startswith('///'):
            found.append(text[3:].strip())
            index -= 1
        elif text.startswith('[') and text.endswith(']'):    # an attribute on its own line above the doc comment's end
            index -= 1
        else:
            break
    return '\n'.join(reversed(found))


def summary_text(doc):
    """A doc comment's <summary> as plain text: see and c tags become their names and words, whitespace is folded."""
    match = re.search(r'<summary>(.*?)</summary>', doc, re.DOTALL)
    if not match:
        return ''
    text = SEE.sub(lambda m: m.group('name').split('.')[-1].split('(')[0], match.group(1))
    text = PARAMREF.sub(lambda m: m.group('name'), text)
    text = CODE.sub(lambda m: m.group('text'), text)
    text = TAG.sub('', text)
    return ' '.join(html.unescape(text).split())


def documented(doc):
    return '<summary>' in doc or '<inheritdoc' in doc


def base_of(signature):
    """The base class named in a type's header ("public class A : B<C>, IThing where ..."): the first entry after the colon."""
    text = ATTRIBUTES.sub('', signature)
    depth, colon = 0, -1
    for index, char in enumerate(text):
        if char in '<([':
            depth += 1
        elif char in '>)]':
            depth -= 1
        elif char == ':' and depth == 0:
            colon = index
            break
    if colon < 0:
        return None
    rest = re.split(r'\bwhere\b', text[colon + 1:])[0]
    depth, first = 0, ''
    for char in rest:
        if char in '<([':
            depth += 1
        elif char in '>)]':
            depth -= 1
        elif depth == 0 and char in ',{':
            break
        first += char
    first = re.sub(r'\(.*\)$', '', first.strip()).strip()      # a primary constructor's arguments to the base
    return first or None


def simple_name(type_name):
    """A type name without its namespace, generic arguments or nullable mark: "Verse.CompProperties<T>?" -> "CompProperties"."""
    text = type_name.strip().rstrip('?')
    text = text.split('<')[0]
    return text.replace('global::', '').split('.')[-1]


def split_type_and_declarators(text):
    """'List<int> a = new(), b' -> ('List<int>', 'a = new(), b'): the type read with its brackets, then the rest."""
    depth = 0
    for index, char in enumerate(text):
        if char in '<([':
            depth += 1
        elif char in '>)]':
            depth -= 1
        elif char.isspace() and depth == 0:
            head = text[:index].strip()
            rest = text[index:].strip()
            if head and rest and not head.endswith(',') and (rest[0].isalpha() or rest[0] in '_@'):
                return head, rest
    return None, None


def parse_field(text):
    """A field declaration's text (comments gone, strings kept) as [(type, name, initialiser)], or [] when it is not an XML
    field: static, const or readonly fields are not read from XML."""
    text = ATTRIBUTES.sub('', text).strip().rstrip(';').strip()
    words = text.split()
    modifiers = set()
    while words and words[0] in FIELD_MODIFIERS | EXCLUDED_MODIFIERS:
        modifiers.add(words.pop(0))
    if 'public' not in modifiers or modifiers & EXCLUDED_MODIFIERS:
        return []
    type_name, rest = split_type_and_declarators(' '.join(words))
    if not type_name:
        return []
    found = []
    for declarator in _csharp.top_level_arguments(rest):
        name, _, initial = declarator.partition('=')
        name = name.strip().lstrip('@')
        if not re.fullmatch(r'\w+', name):
            continue
        found.append((type_name, name, initial.strip() or None))
    return found


def scan_text(text, path):
    """Every class and record in one file, with its public instance fields."""
    lines = text.split('\n')
    clean = _csharp.strip_comments(text).split('\n')
    decls = declarations(text)
    types = {}
    type_names = {d.full for d in decls if d.kind in TYPE_KINDS + ('struct', 'interface', 'enum')}
    for decl in decls:
        if decl.kind not in TYPE_KINDS:
            continue
        doc = doc_comment(lines, decl.start)
        header = _csharp_header(decl.signature)
        nested_in = decl.owner if decl.owner in type_names else None
        namespace = decl.owner
        while namespace in type_names:
            namespace = namespace.rsplit('.', 1)[0] if '.' in namespace else ''
        info = TypeInfo(full=decl.full, name=decl.name, namespace=namespace, kind=decl.kind, base=base_of(decl.signature),
                        public='public' in header, abstract='abstract' in header, path=path, line=decl.start,
                        summary=summary_text(doc), documented=documented(doc), nested_in=nested_in)
        types[decl.full] = info
    for decl in decls:
        if decl.kind != 'field' or decl.owner not in types:
            continue
        source = '\n'.join(clean[decl.start - 1:decl.end])
        source = source[:source.rfind(';') + 1] if ';' in source else source
        summary = summary_text(doc_comment(lines, decl.start))
        for type_name, name, initial in parse_field(source):
            types[decl.owner].fields.append(FieldInfo(name, type_name, initial, summary))
    # A nested type is public only when everything holding it is.
    for info in types.values():
        holder = info.nested_in
        while holder and info.public:
            outer = types.get(holder)
            if outer is None:
                break
            info.public = outer.public
            holder = outer.nested_in
    return types


def _csharp_header(signature):
    """The modifier words before the type keyword."""
    text = ATTRIBUTES.sub('', signature)
    match = re.search(r'\b(class|record|struct|interface)\b', text)
    return set(text[:match.start()].split()) if match else set()


def enum_members(text):
    """{'EnumName': ['First', ...]} for every enum in the text, each list in declared order, the zero member first when one is set."""
    found = {}
    for match in re.finditer(r'\benum\s+(\w+)\s*(?::\s*\w+\s*)?\{([^}]*)\}', _csharp.strip_comments(text)):
        members, zero = [], None
        for part in match.group(2).split(','):
            name, _, value = ATTRIBUTES.sub('', part).strip().partition('=')
            name = name.strip()
            if not re.fullmatch(r'\w+', name):
                continue
            members.append(name)
            if value.strip() == '0':
                zero = name
        if zero:
            members.remove(zero)
            members.insert(0, zero)
        found[match.group(1)] = members
    return found


def source_files(root, projects):
    for project in projects:
        folder = Path(root) / project
        for path in sorted(folder.rglob('*.cs')):
            if SKIPPED_DIRS & set(path.relative_to(folder).parts[:-1]):
                continue
            yield path


@dataclass
class Source:
    types: dict
    enums: dict
    constants: dict


def read_source(root, projects, progress=None):
    """Every type, enum and literal constant in the named project folders."""
    types, enums, texts = {}, {}, {}
    files = list(source_files(root, projects))
    for number, path in enumerate(files, 1):
        if progress and number % 50 == 1:
            progress('Reading the C# source', number, len(files))
        text = path.read_text(encoding='utf-8-sig')
        relative = path.relative_to(root).as_posix()
        texts[relative] = text
        for full, info in scan_text(text, relative).items():
            if full in types:        # a partial type: one entry, its fields and documentation merged
                types[full].fields.extend(info.fields)
                types[full].documented |= info.documented
                types[full].summary = types[full].summary or info.summary
                types[full].base = types[full].base or info.base
                types[full].abstract |= info.abstract
            else:
                types[full] = info
        enums.update(enum_members(text))
    stripped = {path: _csharp.strip_comments(text) for path, text in texts.items()}
    return Source(types, enums, _csharp.read_constants(stripped))


# ----------------------------------------------------------------------------------------------------------------------------
# Which types modders name in XML


def our_base(types, info):
    """Our own type the given type derives from, or None when its base is another's (the game's, a mod's) or it has none."""
    if not info.base:
        return None
    name = info.base.split('<')[0].replace('global::', '')
    candidates = [t for t in types.values() if t.full == name or t.full.endswith('.' + name) or t.name == simple_name(name)]
    if not candidates:
        return None
    same = [t for t in candidates if t.namespace == info.namespace]
    return (same or candidates)[0]


def classify(types, categories):
    """Each type's XML kind (the first category whose base pattern matches its nearest base), and its chain of our own bases."""
    for info in types.values():
        info.category, info.chain = None, []
        seen, current = set(), info
        while current is not None and current.full not in seen and current.base:
            seen.add(current.full)
            base = simple_name(current.base)
            if info.category is None:
                info.category = next((c['name'] for c in categories if any(fnmatch.fnmatchcase(base, p) for p in c['bases'])),
                                     None)
            current = our_base(types, current)
            if current is not None:
                info.chain.append(current)     # every base of ours, so the example lists the fields it inherits


def modder_types(source, categories):
    classify(source.types, categories)
    order = {c['name']: index for index, c in enumerate(categories)}
    return sorted((t for t in source.types.values() if t.public and t.category),
                  key=lambda t: (order[t.category], t.full))


def gaps(types):
    """XML-facing types with no <summary>."""
    return sorted(t.full for t in types if not t.documented)


# ----------------------------------------------------------------------------------------------------------------------------
# How a type is written in XML


@dataclass
class Shown:
    xml: str | None              # the value as XML text, None when the field has no value to show (null, unset, code)
    text: str                    # the default as the table says it
    items: list | None = None    # a list's elements, when the field is a list
    wrapper: str | None = None   # the element the items sit in (a curve's <points>), when not the field itself


def _number(text):
    text = text.replace('_', '').rstrip('fFdDmMuUlL')
    if text.endswith('.'):
        text = text[:-1]
    return text


def _literal(text, constants, depth=0):
    """A literal's XML text, or None: numbers, true and false, strings, nameof, and names of literal constants."""
    text = text.strip()
    if NUMBER.match(text):
        return _number(text)
    if text in ('true', 'false'):
        return text
    if len(text) >= 2 and text[0] == '"' and text[-1] == '"':
        return bytes(text[1:-1], 'utf-8').decode('unicode_escape') if '\\' in text else text[1:-1]
    if text.startswith('@"') and text.endswith('"'):
        return text[2:-1].replace('""', '"')
    match = re.fullmatch(r'nameof\((?:[\w.]+\.)?(\w+)\)', text)
    if match:
        return match.group(1)
    if text in GAME_CONSTANTS:
        return GAME_CONSTANTS[text]
    if ARITHMETIC.match(text) and re.search(r'\d', text) and re.search(r'[+\-*/]', text.lstrip('-')):
        return _arithmetic(text)
    if depth < 3 and re.fullmatch(r'[\w.]+', text) and text in constants:
        return _literal(constants[text], constants, depth + 1)
    if depth < 3 and re.fullmatch(r'[\w.]+', text) and text.split('.')[-1] in constants and '.' in text:
        return _literal(constants[text.split('.')[-1]], constants, depth + 1)
    return None


def _arithmetic(text):
    """A sum written in numbers only ("21f / 175f") worked out, to six significant figures; None when it is not one."""
    expression = re.sub(r'(?<=\d)[fFdD]', '', text)
    if not re.fullmatch(r'[\d\s.+\-*/()]+', expression):
        return None
    try:
        value = eval(expression, {'__builtins__': {}}, {})     # digits, points, brackets and + - * / only, checked above
    except (SyntaxError, ZeroDivisionError, TypeError):
        return None
    return f'{value:.6g}'


def element_type(type_name):
    """A list type's element type, or None when the type is not a list."""
    text = type_name.strip().rstrip('?')
    match = COLLECTION.match(text)
    if match:
        return match.group(1).strip()
    if text.endswith('[]'):
        return text[:-2]
    return None


def default_value(type_name, initial, enums, constants):
    """How a field's default is shown: its XML text and its words."""
    plain = type_name.strip().rstrip('?')
    simple = simple_name(type_name)
    items_type = element_type(type_name)
    if initial is None:
        if plain in NUMBER_TYPES:
            return Shown('0', '0')
        if plain == 'bool':
            return Shown('false', 'false')
        if simple in ('IntRange', 'FloatRange') and not type_name.strip().endswith('?'):
            return Shown('0~0', '0~0')
        if simple in enums and not type_name.strip().endswith('?') and enums[simple]:
            return Shown(enums[simple][0], enums[simple][0])
        return Shown(None, 'none')
    initial = ' '.join(initial.split())
    if initial in ('null', 'default', 'null!', 'default!'):
        return Shown(None, 'none')
    if items_type is not None or DICTIONARY.match(plain):
        if EMPTY_NEW.match(initial):
            return Shown(None, 'empty', [])
        match = NEW_WITH_ITEMS.match(initial)
        if match and items_type is not None:
            inner = match.group('braced') if match.group('braced') is not None else match.group('bracketed')
            values = [_literal(part, constants) for part in _csharp.top_level_arguments(inner)]
            if values and all(v is not None for v in values):
                return Shown(None, ', '.join(values), values)
        return Shown(None, f'set in code: `{initial}`')
    if simple == 'SimpleCurve':
        match = NEW_WITH_ITEMS.match(initial)
        if match and match.group('braced') is not None:
            points = []
            for part in _csharp.top_level_arguments(match.group('braced')):
                point = CURVE_POINT.match(part.strip())
                values = [_literal(v, constants) for v in _csharp.top_level_arguments(point.group('args'))] if point else []
                if len(values) != 2 or not all(values):
                    break
                points.append(f'({values[0]}, {values[1]})')
            else:
                if points:
                    return Shown(None, 'points ' + ', '.join(points), points, 'points')
    match = TARGET_NEW.match(initial)
    if match and (match.group('type') or simple) in ('IntRange', 'FloatRange', 'Color', 'UnityEngine.Color', 'IntVec2', 'IntVec3'):
        parts = [_literal(p, constants) for p in _csharp.top_level_arguments(match.group('args'))]
        if parts and all(parts):
            value = f'{parts[0]}~{parts[1]}' if 'Range' in (match.group('type') or simple) and len(parts) == 2 else                 '(' + ', '.join(parts) + ')'
            return Shown(value, value)
    if EMPTY_NEW.match(initial):
        return Shown(None, f'a new {simple} with its own defaults')
    match = re.fullmatch(r'typeof\((?P<name>[\w.]+)\)', initial)
    if match:
        return Shown(match.group('name'), match.group('name'))
    value = _literal(initial, constants)
    if value is not None:
        if value == '' and simple == 'string':
            return Shown(None, 'empty')
        return Shown(value, value)
    member = re.fullmatch(r'(?:[\w.]+\.)?(\w+)\.(\w+)', initial)
    if member and member.group(1) == simple:
        return Shown(member.group(2), member.group(2))
    return Shown(None, f'set in code: `{initial}`')


def _comment(text):
    """Text safe inside an XML comment."""
    return re.sub(r'-{2,}', '-', text).strip().rstrip('-')


def _escape(text):
    return text.replace('&', '&amp;').replace('<', '&lt;').replace('>', '&gt;')


def _first_sentence(text, limit=110):
    sentence = re.split(r'(?<=[.;])\s', text, maxsplit=1)[0]
    return sentence if len(sentence) <= limit else sentence[:limit - 1].rstrip() + '…'


def field_lines(info, enums, constants, indent):
    """The XML lines for a type's fields, its own first, then each of our base classes' in turn."""
    lines, rows = [], []
    groups = [(info, info.fields)] + [(base, base.fields) for base in info.chain]
    for owner, fields in groups:
        if not fields:
            continue
        if owner is not info:
            lines.append(f'{indent}<!-- from {owner.name} -->')
        for item in fields:
            shown = default_value(item.type, item.initial, enums, constants)
            note = _comment(f'{item.type}' + (f': {_first_sentence(item.summary).rstrip(".;")}' if item.summary else ''))
            if shown.items:
                lines.append(f'{indent}<{item.name}>  <!-- {note} -->')
                inner = indent + '  '
                if shown.wrapper:
                    lines.append(f'{inner}<{shown.wrapper}>')
                    inner += '  '
                lines.extend(f'{inner}<li>{_escape(value)}</li>' for value in shown.items)
                if shown.wrapper:
                    lines.append(f'{indent}  </{shown.wrapper}>')
                lines.append(f'{indent}</{item.name}>')
            elif shown.xml is not None:
                lines.append(f'{indent}<{item.name}>{_escape(shown.xml)}</{item.name}>  <!-- {note} -->')
            else:
                example = '<li>…</li>' if element_type(item.type) is not None else '…'
                lines.append(f'{indent}<!-- <{item.name}>{example}</{item.name}>  {note}; default {_comment(shown.text)} -->')
            rows.append((item.name, item.type, shown.text, item.summary, owner.name if owner is not info else ''))
    return lines, rows


def xml_example(info, category, enums, constants):
    """The XML snippet for one type, as Markdown text with a fenced block."""
    if category['form'] == 'def':
        root = info.full
        body, rows = field_lines(info, enums, constants, '    ')
        snippet = ['<Defs>', f'  <{root}>', '    <defName>MyMod_Example</defName>'] + body + [f'  </{root}>', '</Defs>']
    else:
        container = category.get('container')
        element = category.get('element', 'li')
        indent = '    ' if container else '  '
        body, rows = field_lines(info, enums, constants, indent)
        opening = f'<{element} Class="{info.xml_class}"'
        if body:
            inner = [f'{indent[:-2]}{opening}>'] + body + [f'{indent[:-2]}</{element}>']
        else:
            inner = [f'{indent[:-2]}{opening} />']
        snippet = ([f'<{container}>'] + inner + [f'</{container}>']) if container else inner
    return snippet, rows


def added_in_code(settings):
    """The types the mods add in code, by full name (config "addedInCode"; the owner's card, 28 Nov 2026)."""
    return {entry['type']: entry for entry in (settings.get('addedInCode') or {}).get('types', [])}


def overwrite_page(info, category, enums, constants, added=None):
    """A DocFX overwrite file adding the XML example and the defaults table to the type's API page. A type the mods add in code
    (``added``, its config entry) says so first; one never to be written in XML shows no example to copy."""
    snippet, rows = xml_example(info, category, enums, constants)
    out = ['---', f'uid: {info.full}', 'example: [*content]', '---', '']
    where = category.get('where', '')
    if added:
        never = added.get('never', True)
        out.append(f'**Added for you:** {added.get("note", "")}'.strip())
        out.append('')
        out.append('**Never write it in XML.**' if never else 'Write it in XML only where the line above says so:')
        if never:
            if rows:
                out += ['', 'Its fields, as the code sets them:', '']
                out += ['| Field | Type | Default | What it does |', '| --- | --- | --- | --- |']
                for name, type_name, default, summary, owner in rows:
                    what = summary + (f' (from {owner})' if owner else '')
                    out.append(f'| `{name}` | `{type_name}` | {_cell(default)} | {_cell(what)} |')
            return '\n'.join(out) + '\n'
        out.append('')
    if info.abstract:
        out.append(f'**Abstract**: name a class derived from it in XML, not this one. {where}'.strip())
    else:
        out.append(f'**Where it goes:** {where}' if where else '**In XML:**')
    out += ['', '```xml'] + snippet + ['```', '']
    if rows:
        out += ['| Field | Type | Default | What it does |', '| --- | --- | --- | --- |']
        for name, type_name, default, summary, owner in rows:
            what = summary + (f' (from {owner})' if owner else '')
            out.append(f'| `{name}` | `{type_name}` | {_cell(default)} | {_cell(what)} |')
        out.append('')
        out.append('Defaults are read from the C# source. A field left out of the XML keeps its default; "set in code" means the '
                   'default is worked out in C# rather than written as a value.')
    else:
        out.append('It has no public fields of its own: naming the class is all the XML it takes.')
    return '\n'.join(out) + '\n'


def _cell(text):
    return text.replace('|', '\\|').replace('\n', ' ') or ' '


def catalogue_page(types, categories, repository, added=None):
    """The XML types page: every XML-facing type by kind, linked to its API page."""
    out = ['# XML types', '',
           'Every class a modder names in XML, by the kind of XML it goes in. Each type\'s page shows the element as written, '
           'with every public field and its default, read from the C# source. Generated by `scripts/reference-site.py`.', '']
    by_category = {}
    for info in types:
        by_category.setdefault(info.category, []).append(info)
    for category in categories:
        chosen = by_category.get(category['name'], [])
        if not chosen:
            continue
        out += [f'## {category["name"]} ({len(chosen)})', '']
        if category.get('where'):
            out += [category['where'], '']
        out += ['| Type | What it is |', '| --- | --- |']
        for info in chosen:
            summary = _first_sentence(info.summary, 160) if info.summary else '*(no summary yet)*'
            marker = ' *(abstract)*' if info.abstract else ' *(added for you)*' if info.full in (added or {}) else ''
            source = f'{repository}/blob/main/{info.path}#L{info.line}' if repository else ''
            link = f'<xref:{info.full}?text={info.name}>' + (f' ([source]({source}))' if source else '')
            out.append(f'| {link}{marker} | {_cell(summary)} |')
        out.append('')
    return '\n'.join(out)


# ----------------------------------------------------------------------------------------------------------------------------
# Pages for modders made from the code and the changelogs: extension points, what we patch, dev actions, changes for modders.
# Each is generated on every build, so none can fall behind the code it describes.

OURS_ONLY = re.compile(r'\b(?:ours only|only ours|for our own use|our own use only|not for other mods|not meant for other mods|'
                       r'internal use)\b', re.IGNORECASE)
CODE_FOLDERS = ('src',)                     # where our C# is, for the patches and dev actions pages (config "codeFolders")
CHANGELOGS = 'mods/*/CHANGELOG.md'          # config "changelogs"


def lead_sentence(text, limit=200):
    """A summary's first sentence: up to the first full stop outside brackets that ends a sentence, so "(Framework 0.182.0;
    docs/x.md)" stays whole. Cut with an ellipsis past the limit."""
    depth = 0
    for index, char in enumerate(text):
        if char in '([':
            depth += 1
        elif char in ')]':
            depth = max(0, depth - 1)
        elif char in '.!?' and depth == 0 and (index + 1 == len(text) or text[index + 1].isspace()):
            text = text[:index + 1]
            break
    return text if len(text) <= limit else text[:limit - 1].rstrip() + '…'


def _plain(text):
    """Text from a summary, safe in a Markdown table cell: angle brackets and ampersands as entities, bars escaped."""
    return _cell(html.escape(text, quote=False))


def _generic_arity(signature, name):
    """How many type parameters a declaration has ("class Foo<T, U>" gives 2), for its DocFX uid (Foo`2)."""
    match = re.search(r'\b' + re.escape(name) + r'\s*<([^<>]*)>', ATTRIBUTES.sub('', signature))
    return len(match.group(1).split(',')) if match else 0


def xref(uid, text):
    """A DocFX cross-reference: the uid as DocFX names it (a generic type's arity after a backtick, encoded), with link text."""
    from urllib.parse import quote
    return f'<xref:{quote(uid, safe=".")}?text={quote(text, safe="")}>'


def source_link(repository, path, line=None):
    """A file of ours on GitHub, at a line when one is given; '' without a repository."""
    if not repository:
        return ''
    return f'{repository}/blob/main/{path}' + (f'#L{line}' if line else '')


def project_of(path):
    """The project a source file belongs to: the folder under src/ (or the first folder) it sits in."""
    parts = Path(path).parts
    return parts[1] if len(parts) > 2 and parts[0] == 'src' else parts[0]


def comment_above(lines, start):
    """What a declaration's comment says: its /// <summary>, or else the plain // lines directly above it."""
    summary = summary_text(doc_comment(lines, start))
    if summary:
        return summary
    found, index = [], start - 2
    while index >= 0:
        text = lines[index].strip()
        if text.startswith('//') and not text.startswith('///'):
            found.append(text[2:].strip())
            index -= 1
        elif text.startswith('[') and text.endswith(']'):
            index -= 1
        else:
            break
    return ' '.join(reversed(found)).strip()


@dataclass
class ExtensionPoint:
    project: str
    kind: str                    # 'interface' or 'abstract class'
    full: str
    name: str
    uid: str
    summary: str
    path: str
    line: int


def extension_points(root, projects):
    """Every public interface and public abstract class in the projects that another mod's C# could implement or subclass.
    Left out: what is not public all the way out (internal, or nested in a type that is not), and what its summary says is ours
    only."""
    found = []
    for path in source_files(root, projects):
        text = path.read_text(encoding='utf-8-sig')
        relative = path.relative_to(root).as_posix()
        lines = text.split('\n')
        decls = declarations(text)
        types = {d.full: d for d in decls if d.kind in ('class', 'record', 'interface', 'struct', 'enum')}
        for decl in decls:
            if decl.kind not in ('class', 'record', 'interface'):
                continue
            header = _csharp_header(decl.signature)
            if 'public' not in header or (decl.kind != 'interface' and 'abstract' not in header):
                continue
            holder, public = decl.owner, True
            while holder in types and public:
                public = 'public' in _csharp_header(types[holder].signature)
                holder = types[holder].owner
            summary = summary_text(doc_comment(lines, decl.start))
            if not public or OURS_ONLY.search(summary):
                continue
            arity = _generic_arity(decl.signature, decl.name)
            found.append(ExtensionPoint(project_of(relative), 'interface' if decl.kind == 'interface' else 'abstract class',
                                        decl.full, decl.name, decl.full + (f'`{arity}' if arity else ''), summary, relative,
                                        decl.start))
    return found


def extension_points_page(points, projects, repository):
    """The extension points page: by project in the config's order, then interfaces before abstract classes."""
    out = ['# Extension points', '',
           'Every public interface and abstract class in the D\'thorga mods\' code that your own C# mod can implement or '
           'subclass, by project. Each links to its page in the C# reference, which says how to register it where it needs '
           'registering; most are also described in the guide of the mod they belong to. Generated from the source by '
           '`scripts/reference-site.py` on every build, so the list is the code\'s own.', '',
           '- **An interface**: implement it on your own class (often a comp) and the mod finds it, or register it as its page '
           'says.',
           '- **An abstract class**: subclass it. One you name in XML (`Class="..."`) is also in the XML types page.', '']
    order = {Path(p).name: index for index, p in enumerate(projects)}
    by_project = {}
    for point in points:
        by_project.setdefault(point.project, []).append(point)
    for project in sorted(by_project, key=lambda p: (order.get(p, len(order)), p)):
        chosen = by_project[project]
        out += [f'## {project} ({len(chosen)})', '']
        for kind, heading in (('interface', 'Interfaces'), ('abstract class', 'Abstract classes')):
            kinds = sorted((p for p in chosen if p.kind == kind), key=lambda p: p.full)
            if not kinds:
                continue
            out += [f'### {heading} ({len(kinds)})', '', '| Type | What it is |', '| --- | --- |']
            for point in kinds:
                summary = _plain(lead_sentence(point.summary)) if point.summary else '*(no summary yet)*'
                link = xref(point.uid, point.name)
                source = source_link(repository, point.path, point.line)
                out.append(f'| {link}' + (f' ([source]({source}))' if source else '') + f' | {summary} |')
            out.append('')
    if not by_project:
        out += ['No project has a public interface or abstract class.', '']
    out.append(f'{len(points)} extension points.')
    return '\n'.join(out) + '\n'


@dataclass
class PatchRow:
    project: str
    target: str                  # "Type.Method", "Type.Method (getter)", or "by name in TargetMethod"
    by_name: bool                # the target is found at run time
    kinds: tuple
    our_class: str
    path: str
    line: int
    summary: str


def _class_at(decls, line):
    """The type declared at a line (the patch class the kit's Harmony reader names by its first line)."""
    return next((d for d in decls if d.kind in ('class', 'struct', 'record') and d.start == line), None)


BY_NAME_SOURCES = ('AccessTools.Method', 'AccessTools.DeclaredMethod', 'AccessTools.PropertyGetter', 'AccessTools.PropertySetter',
                   'AccessTools.Constructor', 'AccessTools.DeclaredConstructor', 'GetMethod', 'HarmonyPatch')


def _by_name_target(scan, path, decls, owner):
    """The members a patch class's TargetMethod (or TargetMethods) looks up, as the kit's target scanner (check-targets'
    reader) read them, else ''. Only lookups inside that method count: one in a transpiler names a call to change, not the
    method patched. The type is as the code writes it, often in full, since it is another mod's."""
    spans = [(d.start, d.end) for d in decls if d.owner == owner.full and d.kind == 'method'
             and d.name in ('TargetMethod', 'TargetMethods')]
    kinds = {'getter': ' (getter)', 'setter': ' (setter)', 'ctor': ' (constructor)', 'cctor': ' (static constructor)'}
    labels = sorted({t.type_name + (f'.{t.member}' if t.member else '') + kinds.get(t.member_kind, '')
                     for t in scan.targets if t.file == path and t.source in BY_NAME_SOURCES
                     and any(start <= t.line <= end for start, end in spans)})
    return ', '.join(labels)


def harmony_patches(root, folders=CODE_FOLDERS):
    """Every Harmony patch in our code, read by phobos-modkit's Harmony reader (the one doc-tables' patch list uses), each with
    its class, file and the first sentence of its comment. A patch that finds its target by name is matched with what the
    kit's target scanner (check-targets' reader) read in its TargetMethod, where it could."""
    from phobos_modkit import dotnet_projects
    from phobos_modkit.code import harmony
    from phobos_modkit.targets import scan_tree
    root = Path(root)
    rows, texts = [], {}
    found = []
    for project in dotnet_projects.read_projects(root, list(folders)):
        found += [(project.name, patch) for patch in harmony.read_folder(root / project.folder, relative_to=root).patches]
    scan = scan_tree(root, tuple(f'{folder}/**/*.cs' for folder in folders)) if any(
        p.target.startswith('by name in ') for _, p in found) else None
    for project, patch in found:
        if patch.file not in texts:
            text = (root / patch.file).read_text(encoding='utf-8-sig')
            texts[patch.file] = (text.split('\n'), declarations(text))
        lines, decls = texts[patch.file]
        owner = _class_at(decls, patch.line)
        by_name = patch.target.startswith('by name in ')
        target = patch.target
        if by_name and owner is not None and scan is not None:
            target = _by_name_target(scan, patch.file, decls, owner) or target
        summary = comment_above(lines, patch.line)
        if not summary and owner is not None:          # no comment on the class: the patch method's own
            method = next((d for d in decls if d.owner == owner.full and d.kind == 'method'
                           and d.name.lower() in patch.kinds), None)
            summary = comment_above(lines, method.start) if method else ''
        rows.append(PatchRow(project, target, by_name, patch.kinds, owner.name if owner else '?', patch.file, patch.line,
                             summary))
    return rows


def patches_page(rows, repository):
    """The What we patch page: every patch by project, then by the method it patches."""
    out = ['# What we patch', '',
           '**Why read this:** if your mod patches one of these methods too, both patches run on it, so this is where our mods '
           'and yours may meet (compatibility). Every Harmony patch in the D\'thorga mods\' code: the method it patches, how '
           '(prefix, postfix, transpiler, finalizer), our patch class with its source, and what the patch does.', '',
           'Read from the C# source on every build by phobos-modkit\'s Harmony reader, the same as the patch list in '
           '`docs/development/building.md`. A target shown with "by name" is found at run time; where the code names it, it '
           'is given. To see who else patches a method in a game, look at Harmony\'s own report in the game\'s log.', '']
    by_project = {}
    for row in rows:
        by_project.setdefault(row.project, []).append(row)
    for project in sorted(by_project):
        chosen = sorted(by_project[project], key=lambda r: (r.target.startswith('by name in '), r.target.lower(), r.path,
                                                           r.line))
        out += [f'## {project} ({len(chosen)})', '', '| Patched method | How | Our class | What it does |',
                '| --- | --- | --- | --- |']
        for row in chosen:
            target = row.target if row.target.startswith('by name in ') else f'`{row.target}`'
            if row.by_name and not row.target.startswith('by name in '):
                target += ' (by name)'
            source = source_link(repository, row.path, row.line)
            ours = f'[{row.our_class}]({source})' if source else f'`{row.our_class}`'
            what = _plain(lead_sentence(row.summary)) if row.summary else '*(no comment)*'
            out.append(f'| {target} | {", ".join(row.kinds) or "not found"} | {ours} | {what} |')
        out.append('')
    if not by_project:
        out += ['Nothing is patched with Harmony.', '']
    out.append(f'{len(rows)} patches in {len(by_project)} projects.')
    return '\n'.join(out) + '\n'


DEBUG_ACTION = re.compile(r'\[\s*(?:LudeonTK\.)?DebugAction\s*\(')
GAME_STATES = {'Entry': 'the main menu', 'Playing': 'a game', 'PlayingOnMap': 'a map', 'IsCurrentlyOnMap': 'a map',
               'PlayingOnWorld': 'the world map', 'WorldRenderedNow': 'the world map', 'HasGameCondition': 'a game condition'}
ACTION_TYPES = {'Action': 'runs when chosen', 'ToolMap': 'then click a cell on the map',
                'ToolMapForPawns': 'then click a pawn on the map', 'ToolWorld': 'then click a tile on the world map'}
DLC_FLAGS = {'requiresRoyalty': 'Royalty', 'requiresIdeology': 'Ideology', 'requiresBiotech': 'Biotech',
             'requiresAnomaly': 'Anomaly', 'requiresOdyssey': 'Odyssey'}
DEBUG_POSITIONAL = ('category', 'name') + tuple(DLC_FLAGS)


@dataclass
class DevAction:
    category: str
    label: str
    method: str
    owner: str
    path: str
    line: int
    summary: str
    works_in: str
    how: str


def _constant_text(value, constants, owner):
    """A string argument's text: a literal, or a constant of ours followed to its literal; None when neither."""
    value = value.strip()
    for name in (f'{owner}.{value}', value, '.'.join(value.split('.')[-2:])):
        if name in constants:
            value = constants[name].strip()
            break
    if len(value) >= 2 and value[0] == '"' and value[-1] == '"':
        return value[1:-1]
    return None


def debug_action_arguments(arguments, constants, owner=''):
    """A DebugAction attribute's arguments as {'category', 'name', 'allowedGameStates', 'actionType', ...}: positional ones
    in the game's constructor order (category, name, then the DLC flags), named ones by name; strings followed to their text."""
    found = {}
    for index, argument in enumerate(_csharp.top_level_arguments(arguments)):
        named = re.match(r'^(\w+)\s*[:=](?!=)\s*(.+)$', argument, re.DOTALL)
        key, value = (named.group(1), named.group(2).strip()) if named else (
            DEBUG_POSITIONAL[index] if index < len(DEBUG_POSITIONAL) else f'#{index}', argument.strip())
        if key in ('category', 'name'):
            found[key] = _constant_text(value, constants, owner) or value
        else:
            found[key] = ' '.join(value.split())
    return found


def _needs(arguments, label):
    """Where an action can be used and how, from its attribute (the game's defaults: a game, runs when chosen)."""
    states = re.findall(r'AllowedGameStates\.(\w+)', arguments.get('allowedGameStates', 'AllowedGameStates.Playing'))
    places = []
    for state in states:
        place = GAME_STATES.get(state, state)
        if place not in places:
            places.append(place)
    if 'a map' in places and 'a game' in places:
        places.remove('a game')
    action = re.sub(r'^(?:LudeonTK\.)?DebugActionType\.', '', arguments.get('actionType', 'Action'))
    how = [ACTION_TYPES.get(action, action)]
    if re.search(r'\bselected\b', label, re.IGNORECASE):
        how.append('acts on what is selected')
    dlcs = [name for flag, name in DLC_FLAGS.items() if arguments.get(flag) == 'true']
    if dlcs:
        how.append('needs ' + ' and '.join(dlcs))
    return ' or '.join(places) or 'a game', '; '.join(how)


def debug_actions(root, folders=CODE_FOLDERS):
    """Every [DebugAction(...)] method in our code: its category and label (constants followed), what its summary says, and
    where and how it works, read from the attribute."""
    root = Path(root)
    files = [path for folder in folders for path in sorted((root / folder).rglob('*.cs'))
             if not SKIPPED_DIRS & set(path.relative_to(root / folder).parts[:-1])]
    texts = {path.relative_to(root).as_posix(): path.read_text(encoding='utf-8-sig') for path in files}
    constants = _csharp.read_constants({path: _csharp.strip_comments(text) for path, text in texts.items()})
    found = []
    for relative, text in texts.items():
        clean = _csharp.strip_comments(text)
        if not DEBUG_ACTION.search(clean):
            continue
        lines = text.split('\n')
        methods = [d for d in declarations(text) if d.kind == 'method']
        for match in DEBUG_ACTION.finditer(clean):
            line = clean.count('\n', 0, match.start()) + 1
            method = next((d for d in methods if d.start <= line <= d.end), None)
            if method is None:
                continue
            owner = method.owner.split('.')[-1]
            arguments = debug_action_arguments(_csharp.call_arguments(clean, match.end() - 1), constants, owner)
            label = arguments.get('name', method.name)
            works_in, how = _needs(arguments, label)
            found.append(DevAction(arguments.get('category', ' ').strip() or '(no category)', label, method.name, owner,
                                   relative, line, comment_above(lines, method.start), works_in, how))
    return found


def _label_group(label):
    """The part of a label before its first colon ("Framework: soak the ground" -> "Framework"), the menu's own grouping."""
    head, sep, _ = label.partition(': ')
    return head if sep and len(head) <= 40 else ''


def dev_actions_page(actions, repository):
    """The Dev actions page: by category, then by the label's own prefix where a category has several."""
    out = ['# Dev actions', '',
           'Every dev action the D\'thorga mods add, for trying things out and finding faults while you build on them. **Turn on '
           'Development mode** in the game\'s options; the dev tools then sit at the top of the screen. Open the **debug '
           'actions** window there and find the category below (most are under **D\'thorga**), or type part of a label into '
           'its search box. An action that needs a map is shown only while one is open.', '',
           'Generated from the `[DebugAction]` attributes in the source on every build. "Works in" and "How" come from the '
           'attribute; "acts on what is selected" from the label.', '']
    by_category = {}
    for action in actions:
        by_category.setdefault(action.category, []).append(action)
    for category in sorted(by_category, key=lambda c: (c != "D'thorga", c.lower())):
        chosen = by_category[category]
        out += [f'## {_plain(category)} ({len(chosen)})', '']
        groups = {}
        for action in chosen:
            groups.setdefault(_label_group(action.label), []).append(action)
        for group in sorted(groups, key=lambda g: (g == '', g.lower())):
            if len(groups) > 1:
                out += [f'### {_plain(group) if group else "Others"}', '']
            out += ['| Action | What it does | Works in | How |', '| --- | --- | --- | --- |']
            for action in sorted(groups[group], key=lambda a: a.label.lower()):
                source = source_link(repository, action.path, action.line)
                label = f'[{_plain(action.label)}]({source})' if source else _plain(action.label)
                what = _plain(' '.join(action.summary.split())) if action.summary else '*(no summary: the label says it)*'  # whole: the owner's card of 28 Nov 2026
                out.append(f'| {label} | {what} | {action.works_in} | {action.how} |')
            out.append('')
    if not by_category:
        out += ['No dev actions.', '']
    out.append(f'{len(actions)} dev actions in {len(by_category)} '
               f'categor{"y" if len(by_category) == 1 else "ies"}.')
    return '\n'.join(out) + '\n'


VERSION_HEADING = re.compile(r'^(?P<level>#{2,4})\s+\[(?P<version>[^\]]+)\](?:\s*-\s*(?P<date>\d{4}-\d{2}-\d{2}))?')
HISTORY_HEADING = re.compile(r'^##\s+(?P<name>.+?)\s+history\s*$')
MODDERS_START = re.compile(r'^(?:\*\*)?For modders\b', re.IGNORECASE)
MODDERS_COLON = re.compile(r'\bfor modders:', re.IGNORECASE)
MODDERS_PAREN = re.compile(r'\([^()]*\bfor modders\b[^()]*\)', re.IGNORECASE)
BOLD_OPENING = re.compile(r'^\*\*.+?\*\*')


@dataclass
class ModderChange:
    mod: str
    version: str
    date: str | None             # YYYY-MM-DD; None while unreleased
    section: str                 # Added, Fixed, ... ('' when the version has none)
    text: str
    path: str
    line: int


def _sentence_from(text, start):
    """The text from start to the end of its sentence (a full stop outside brackets, then a capital or the end)."""
    depth = 0
    for index in range(start, len(text)):
        char = text[index]
        if char in '([':
            depth += 1
        elif char in ')]':
            if depth == 0:              # the bracket the sentence sits in closes: the sentence ends with it
                return text[start:index]
            depth -= 1
        elif char == '.' and depth == 0 and (index + 1 == len(text) or (text[index + 1] == ' ' and index + 2 < len(text)
                                                                          and (text[index + 2].isupper() or text[index + 2] in '*('))):
            return text[start:index + 1]
    return text[start:]


def modders_text(bullet, whole=False):
    """The part of a changelog bullet that speaks to modders, or None when it does not: a bullet that opens "For modders" (or
    sits under a heading saying so, ``whole``) whole; a "For modders:" inside one as that sentence, after the bullet's bold
    opening for context; a "(..., for modders)" as the bullet's first sentence. "The notes for modders" and the like are not
    marks."""
    if whole or MODDERS_START.match(bullet):
        return bullet
    opening = BOLD_OPENING.match(bullet)
    colon = MODDERS_COLON.search(bullet)
    if colon:
        sentence = _sentence_from(bullet, colon.start()).strip()
        if sentence and sentence[0].islower():
            sentence = sentence[0].upper() + sentence[1:]
        if opening and colon.start() >= opening.end():
            return f'{opening.group(0)}: {sentence}'
        return sentence
    if MODDERS_PAREN.search(bullet):
        return _sentence_from(bullet, 0).strip()
    return None


def read_changelog(text, path):
    """Every line of one mod's changelog that speaks to modders, with its mod, version, date and section. A merged mod's
    history ("## D'thorga X history") is named as that mod; a version not yet released at a merge takes the next date below."""
    lines = text.split('\n')
    title = next((line[2:].strip() for line in lines if line.startswith('# ')), Path(path).parent.name)
    main = re.sub(r'\s+changelog$', '', title, flags=re.IGNORECASE)
    mod, version, date, version_level, section, modder_section = main, None, None, 0, '', False
    found, bullet, bullet_line = [], None, 0
    undated = []

    def close():
        if bullet is not None and version is not None:
            text = modders_text(' '.join(bullet.split()), whole=modder_section)
            if text:
                found.append(ModderChange(mod, version, date, section, text, path, bullet_line))
                if date is None and version.lower() != 'unreleased':
                    undated.append(found[-1])

    for number, line in enumerate(lines, 1):
        history = HISTORY_HEADING.match(line)
        heading = VERSION_HEADING.match(line)
        if history or heading or line.startswith('#'):
            close()
            bullet = None
            if history:
                mod, version = f'{history.group("name")} (now in {main})', None
            elif heading:
                version, date = heading.group('version'), heading.group('date')
                version_level, section, modder_section = len(heading.group('level')), '', False
                if date:
                    for change in undated:
                        change.date = date
                    undated.clear()
            elif version is not None and len(line) - len(line.lstrip('#')) > version_level:
                section = line.lstrip('#').strip()
                modder_section = 'modders' in section.lower()
            continue
        if line.startswith('- '):
            close()
            bullet, bullet_line = line[2:], number
        elif bullet is not None and line.startswith(' ') and line.strip():
            bullet += ' ' + line.strip()
        else:
            close()
            bullet = None
    close()
    return found


def modder_changes(root, pattern=CHANGELOGS):
    """Every changelog line marked for modders, from every mod's changelog, newest first: unreleased, then by date; within a
    day, by mod, in the changelog's own order."""
    root = Path(root)
    found = []
    for path in sorted(root.glob(pattern)):
        found += read_changelog(path.read_text(encoding='utf-8-sig'), path.relative_to(root).as_posix())
    position = {id(change): index for index, change in enumerate(found)}
    return sorted(found, key=lambda c: (c.date is not None, '' if c.date is None else _descending(c.date), c.mod,
                                        position[id(c)]))


def _descending(date):
    """A date as a key that sorts the newest first."""
    return ''.join(chr(ord('9') - ord(char) + ord('0')) if char.isdigit() else char for char in date)


def _day(date):
    from datetime import date as day
    when = day.fromisoformat(date)
    return f'{when.day} {when:%b %Y}'


def modder_changes_page(changes, repository):
    """The Changes for modders page: one heading per day, newest first, each line with its mod, version and section."""
    out = ['# Changes for modders', '',
           'Every change the D\'thorga mods\' changelogs mark **for modders**, newest first: what changed that may affect a mod '
           'building on them, with the mod and the version it came in. Each mod\'s whole changelog is in its folder on GitHub '
           '(`mods/<Mod>/CHANGELOG.md`). Generated from the changelogs on every build.', '',
           '"Draft" versions are prepared but not yet published on the Steam Workshop.', '']
    current = object()
    for change in changes:
        day = change.date
        if day != current:
            out += ['', f'## {"Not yet released" if day is None else _day(day)}', '']
            current = day
        text = rewrite_links(change.text, change.path, [], repository).replace('<', '&lt;')
        source = source_link(repository, change.path, change.line)
        where = f'{change.mod} {change.version}' if change.version.lower() != 'unreleased' else f'{change.mod}, unreleased'
        where = f'[{where}]({source})' if source else where
        section = f', {change.section.lower()}' if change.section else ''
        out.append(f'- {where}{section}: {text}')
    if not changes:
        out += ['No changelog line is marked for modders yet.']
    out += ['', f'{len(changes)} changes for modders.']
    return '\n'.join(out) + '\n'


MODDER_PAGES = ('extension-points', 'patches', 'dev-actions', 'modder-changes')


def write_modder_pages(root, settings, work):
    """The four generated pages for modders, each with its uid (docs/reference/index.md links to them). Returns their entry
    counts by page."""
    root, work = Path(root), Path(work)
    repository = settings.get('repository', '')
    folders = tuple(settings.get('codeFolders') or CODE_FOLDERS)
    points = extension_points(root, settings['projects'])
    patches = harmony_patches(root, folders)
    actions = debug_actions(root, folders)
    changes = modder_changes(root, settings.get('changelogs') or CHANGELOGS)
    pages = {'extension-points': (extension_points_page(points, settings['projects'], repository), len(points)),
             'patches': (patches_page(patches, repository), len(patches)),
             'dev-actions': (dev_actions_page(actions, repository), len(actions)),
             'modder-changes': (modder_changes_page(changes, repository), len(changes))}
    for uid, (text, _) in pages.items():
        (work / f'{uid}.md').write_text(with_uid(text, uid), encoding='utf-8')
    return {uid: count for uid, (_, count) in pages.items()}


# ----------------------------------------------------------------------------------------------------------------------------
# Guides as articles


def rewrite_links(text, guide, guides, repository):
    """A guide's links made to work on the site: a link to another guide stays between the copies, anything else in the
    repository points at GitHub (an image at its raw file)."""
    folder = Path(guide).parent

    def replace(match):
        target = match.group('target')
        if re.match(r'^[a-z][a-z0-9+.-]*:', target, re.IGNORECASE) or target.startswith('#') or target.startswith('/'):
            return match.group(0)
        path, _, anchor = target.partition('#')
        resolved = os.path.normpath((folder / path).as_posix()).replace('\\', '/')
        if resolved in guides:
            new = Path(resolved).name + (f'#{anchor}' if anchor else '')
        elif repository:
            kind = 'raw' if match.group('bang') else ('tree' if not Path(resolved).suffix else 'blob')
            new = f'{repository}/{kind}/main/{resolved}' + (f'#{anchor}' if anchor else '')
        else:
            return match.group(0)
        return f'{match.group("bang")}[{match.group("text")}]({new})'

    return LINK.sub(replace, text)


def guide_uid(guide):
    """The cross-reference name of a guide on the site (docs/reference/index.md links to it as <xref:guide-...>)."""
    return 'guide-' + Path(guide).stem


def with_uid(text, uid):
    """A Markdown page given a DocFX uid, so a page can link to it with <xref:uid> whatever folder it ends up in."""
    return f'---\nuid: {uid}\n---\n\n' + text


def guides_toc(guides, root):
    lines = []
    for guide in guides:
        text = (Path(root) / guide).read_text(encoding='utf-8-sig')
        title = next((line[2:].strip() for line in text.splitlines() if line.startswith('# ')), Path(guide).stem)
        lines += [f'- name: "{title}"', f'  href: {Path(guide).name}']
    return '\n'.join(lines) + '\n'


# ----------------------------------------------------------------------------------------------------------------------------
# The DocFX project


def nuget_root(environ=None):
    environ = os.environ if environ is None else environ
    if environ.get('NUGET_PACKAGES'):
        return Path(environ['NUGET_PACKAGES'])
    return Path.home() / '.nuget' / 'packages'


def package_versions(root):
    """{package name: version} from Directory.Build.targets, where every project's references are set once."""
    text = (Path(root) / 'Directory.Build.targets').read_text(encoding='utf-8-sig')
    return {m.group('name'): m.group('version') for m in PACKAGE.finditer(text)}


def reference_folders(root, settings, environ=None):
    """The folders of the reference assemblies DocFX compiles against, and the ones missing (not yet restored)."""
    versions = package_versions(root)
    packages = nuget_root(environ)
    found, missing = [], []
    for item in settings['references']:
        version = versions.get(item['package'])
        if not version:
            raise SiteError(f'{item["package"]} has no version in Directory.Build.targets')
        folder = packages / item['package'].lower() / version / item['folder']
        (found if folder.is_dir() else missing).append((folder, item))
    return found, missing


def effective_config(project_config, root, project_dir, references):
    """The committed docfx.json made runnable from the work folder: metadata sources absolute, references filled in."""
    config = json.loads(json.dumps(project_config))
    for item in config.get('metadata', []):
        for entry in item.get('src', []):
            entry['src'] = (Path(project_dir) / entry.get('src', '.')).resolve().as_posix()
        item['references'] = [{'src': folder.as_posix(), 'files': ['*.dll'], 'exclude': list(entry.get('exclude', []))}
                              for folder, entry in references]
    return config


def _git(root, *args):
    """One git command's output in the repository, or None where git or the repository is missing (a test's folder)."""
    try:
        result = subprocess.run(['git', '-C', str(root), *args], capture_output=True, text=True, encoding='utf-8',
                                errors='replace', timeout=60)
    except (OSError, subprocess.SubprocessError):
        return None
    return result.stdout.strip() if result.returncode == 0 else None


def mod_version(root, about):
    """The <modVersion> in an About.xml, or None."""
    path = Path(root) / about
    if not path.is_file():
        return None
    match = re.search(r'<modVersion>\s*([^<\s]+)\s*</modVersion>', path.read_text(encoding='utf-8-sig'))
    return match.group(1) if match else None


def build_stamp(root, settings, now=None, git=_git):
    """The line every page's footer ends with (owner, 28 Nov 2026: the site looked days old when it was not): when it was built,
    from which version of the mod it documents (`stamp` in the config) and which commit, so a reader can tell how fresh it is.
    A build from a working tree with uncommitted changes says so."""
    from datetime import datetime, timezone
    now = now or datetime.now(timezone.utc)
    parts = [f'Built {now.day} {now:%b %Y, %H:%M} UTC']
    if (settings.get('stamp') or {}).get('about'):
        parts[0] += ','
    stamp = settings.get('stamp') or {}
    version = mod_version(root, stamp['about']) if stamp.get('about') else None
    if version:
        parts.append(f'from {stamp.get("name", "the mod")} {version}')
    commit = git(root, 'rev-parse', '--short', 'HEAD')
    if commit:
        changed = git(root, 'status', '--porcelain', '--untracked-files=no')
        parts.append(f'(commit {commit}{", with uncommitted changes" if changed else ""})')
    return ' '.join(parts) + '.'


def stamp_footer(config, stamp):
    """The run's docfx.json with the build stamp after the committed footer; the committed project is left as it was."""
    metadata = config.setdefault('build', {}).setdefault('globalMetadata', {})
    footer = metadata.get('_appFooter', '')
    metadata['_appFooter'] = f'{footer} {html.escape(stamp, quote=False)}'.strip()
    return config


def _remove_readonly(function, path, _info):
    os.chmod(path, stat.S_IWRITE)
    function(path)


def prepare_folder(folder):
    """Empty a work or site folder this tool made before; refuse one it did not make that holds anything."""
    folder = Path(folder)
    if folder.exists() and any(folder.iterdir()):
        if not (folder / MARKER).is_file():
            raise SiteError(f'{folder} holds files this tool did not write ({MARKER} is missing); choose an empty folder')
        shutil.rmtree(folder, onerror=_remove_readonly)
    folder.mkdir(parents=True, exist_ok=True)
    (folder / MARKER).write_text('Written by phobos-rimworld scripts/reference-site.py; emptied on every build.\n', encoding='utf-8')


def write_pages(root, settings, work, progress=None):
    """Every generated input in the work folder: the project's own files, the guides, the XML examples, the XML types page and
    the four pages for modders. Returns the XML-facing types, and the entries on each page for modders by its uid."""
    root, work = Path(root), Path(work)
    project_dir = root / PROJECT
    for path in project_dir.rglob('*'):
        if path.is_file() and path.name != 'docfx.json':
            target = work / path.relative_to(project_dir)
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(path, target)
    repository = settings.get('repository', '')
    guides = settings.get('guides', [])
    (work / 'guides').mkdir(exist_ok=True)
    for guide in guides:
        text = (root / guide).read_text(encoding='utf-8-sig')
        page = with_uid(rewrite_links(text, guide, guides, repository), guide_uid(guide))
        (work / 'guides' / Path(guide).name).write_text(page, encoding='utf-8')
    (work / 'guides' / 'toc.yml').write_text(guides_toc(guides, root), encoding='utf-8')
    source = read_source(root, settings['projects'], progress)
    categories = settings['categories']
    chosen = modder_types(source, categories)
    by_name = {c['name']: c for c in categories}
    added = added_in_code(settings)
    examples = work / 'xml-examples'
    examples.mkdir(exist_ok=True)
    for info in chosen:
        page = overwrite_page(info, by_name[info.category], source.enums, source.constants, added.get(info.full))
        (examples / f'{info.full}.md').write_text(page, encoding='utf-8')
    (work / 'xml-types.md').write_text(with_uid(catalogue_page(chosen, categories, repository, added), 'xml-types'), encoding='utf-8')
    if progress:
        progress('Writing the pages for modders (extension points, patches, dev actions, changes)', 1, 1)
    counts = write_modder_pages(root, settings, work)
    return chosen, counts


@dataclass
class DocfxRun:
    code: int
    output: str
    warnings: int
    errors: int


def run_docfx(step, config_path, docfx='docfx', run=subprocess.run):
    """One DocFX step ("metadata" or "build") on the work folder's docfx.json. The two run apart so the site's own warnings (a
    broken link, a cross-reference that names nothing) are not lost among the compiler's: DocFX stops logging warnings after
    its first thousand, and reading our source without the other mods' assemblies gives thousands."""
    result = run([docfx, step, str(config_path)], capture_output=True, text=True, encoding='utf-8', errors='replace',
                 cwd=str(Path(config_path).parent))
    output = (result.stdout or '') + (result.stderr or '')
    return DocfxRun(result.returncode, output, _count(r'(\d+) warning\(s\)', output), _count(r'(\d+) error\(s\)', output))


def _count(pattern, text):
    match = re.search(pattern, text)
    return int(match.group(1)) if match else 0


def count_pages(site):
    site = Path(site)
    pages = list(site.rglob('*.html'))
    api = [p for p in pages if 'api' in p.relative_to(site).parts]
    return len(pages), len(api)


def write_downloads(root, settings, site, log=print):
    """The scripts a modder downloads, each bundled to one file at the site's root (config "downloads"; the owner's card of
    28 Nov 2026, "Kit, bundled to one file"): phobos-modkit's bundle command inlines the kit modules a script imports, so the
    published copy always matches the code. Returns the published names. Raises SiteError when one cannot be bundled."""
    from phobos_modkit import bundle_cli
    names = []
    for item in settings.get('downloads', []):
        script = Path(root) / item['script']
        target = Path(site) / script.name
        code = bundle_cli.main([str(script), '--python', str(item.get('python', '3.9')), '--out', str(target)], out=io.StringIO())
        if code != 0 or not target.is_file():
            raise SiteError(f'could not bundle {item["script"]} for download (phobos-modkit bundle exit {code})')
        names.append(script.name)
    if names:
        log('Downloads bundled: ' + ', '.join(names))
    return names


def build(root, settings, work, site, docfx='docfx', run=subprocess.run, log=print, progress=None, restore=True):
    root, work, site = Path(root), Path(work), Path(site)
    progress = progress or Progress()
    progress('Finding the reference assemblies', 1, 4)
    found, missing = reference_folders(root, settings)
    if missing and restore:
        log(f'Restoring {settings["restoreProject"]} for the game\'s reference assemblies and Harmony')
        result = run(['dotnet', 'restore', str(root / settings['restoreProject']), '--nologo'], capture_output=True, text=True)
        if result.returncode != 0:
            raise SiteError('dotnet restore failed:\n' + (result.stdout or '') + (result.stderr or ''))
        found, missing = reference_folders(root, settings)
    if missing:
        raise SiteError('missing reference assemblies: ' + ', '.join(str(f) for f, _ in missing))
    progress('Writing the generated pages', 2, 4)
    prepare_folder(work)
    chosen, counts = write_pages(root, settings, work)
    project_config = json.loads((root / PROJECT / 'docfx.json').read_text(encoding='utf-8-sig'))
    config = effective_config(project_config, root, root / PROJECT, found)
    stamp = build_stamp(root, settings)
    stamp_footer(config, stamp)
    prepare_folder(site)
    config['build']['output'] = site.resolve().as_posix()
    config_path = work / 'docfx.json'
    config_path.write_text(json.dumps(config, indent=2), encoding='utf-8')
    progress('Running DocFX (the C# reference and the site)', 3, 4)
    metadata = run_docfx('metadata', config_path, docfx, run)
    (work / 'docfx-metadata.log').write_text(metadata.output, encoding='utf-8')
    if metadata.code != 0:
        raise SiteError(f'DocFX could not read the C# ({metadata.errors} errors); its output is in {work / "docfx-metadata.log"}')
    site_run = run_docfx('build', config_path, docfx, run)
    (work / 'docfx-build.log').write_text(site_run.output, encoding='utf-8')
    if site_run.code != 0:
        raise SiteError(f'DocFX could not build the site ({site_run.errors} errors); its output is in {work / "docfx-build.log"}')
    (site / MARKER).write_text('Written by phobos-rimworld scripts/reference-site.py; emptied on every build.\n', encoding='utf-8')
    write_downloads(root, settings, site, log)
    progress('Counting the pages', 4, 4)
    pages, api = count_pages(site)
    log(f'Site written to {site}: {pages} pages ({api} API pages), {len(chosen)} XML-facing types with an XML example')
    log('Pages for modders: ' + ', '.join(f'{uid} {count}' for uid, count in counts.items()))
    log(f'Every page\'s footer says: {stamp}')
    log(f'Reading the C#: {metadata.warnings} compiler warnings (the other mods\' types are left unresolved, and DocFX compiles '
        f'without our nullable setting), {metadata.errors} errors')
    log(f'Building the site: {site_run.warnings} warnings, {site_run.errors} errors' +
        (f'; read them in {work / "docfx-build.log"}' if site_run.warnings else ''))
    log(f'Open {site / "index.html"} in a browser to look at it.')
    return pages, api, len(chosen), site_run.warnings


def verify(root, settings, docfx='docfx', log=print, restore=True):
    """The gate's build (the owner's card of 24 Nov 2026): the whole site into a throwaway folder of its own, so gates of several
    sessions never share one; 1 when DocFX fails or the site has any warning (a broken link, a cross-reference to nothing). The
    compiler's warnings from other mods' unresolved types are not the site's and do not count. The folder is always removed."""
    import tempfile
    scratch = Path(tempfile.mkdtemp(prefix='reference-site-'))
    try:
        (scratch / 'work').mkdir()
        (scratch / 'site').mkdir()
        (scratch / 'work' / MARKER).write_text('throwaway\n', encoding='utf-8')
        (scratch / 'site' / MARKER).write_text('throwaway\n', encoding='utf-8')
        try:
            *_, warnings = build(root, settings, scratch / 'work', scratch / 'site', docfx, log=lambda _line: None, restore=restore)
        except SiteError as error:
            log(f'reference-site: {error}')
            log_file = next(iter(sorted(scratch.glob('work/docfx-*.log'))), None)
            if log_file is not None:
                log(log_file.read_text(encoding='utf-8', errors='replace')[-3000:])
            return 1
        if warnings:
            log(f'The reference site built with {warnings} warnings; DocFX said:')
            text = (scratch / 'work' / 'docfx-build.log').read_text(encoding='utf-8', errors='replace')
            for line in text.splitlines():
                if 'warning' in line.lower():
                    log('  ' + line.strip())
            return 1
        log('The reference site builds with no warnings or errors.')
        return 0
    finally:
        shutil.rmtree(scratch, ignore_errors=True)


# ----------------------------------------------------------------------------------------------------------------------------
# The command


def load_settings(root):
    return json.loads((Path(root) / CONFIG).read_text(encoding='utf-8-sig'))


def default_folder(name):
    from phobos_modkit.cache import folder
    import deploy
    return folder(CACHE_NAMESPACE, settings_file=deploy.SETTINGS_FILE) / name


def check(root, settings, log=print):
    """0 when every XML-facing type without a <summary> is a known gap; 1 when one is new."""
    source = read_source(root, settings['projects'])
    chosen = modder_types(source, settings['categories'])
    missing = gaps(chosen)
    known = set(settings.get('knownGaps', []))
    new = [name for name in missing if name not in known]
    fixed = sorted(known - set(missing))
    log(f'{len(chosen)} XML-facing types; {len(missing)} without a <summary> ({len(missing) - len(new)} known, {len(new)} new)')
    by_name = {t.full: t for t in chosen}
    for name in new:
        info = by_name[name]
        log(f'  {info.path}:{info.line}: {name} ({info.category}) has no <summary>')
    if fixed:
        log(f'{len(fixed)} known gaps now have a summary or are gone; take them out of {CONFIG}'
            ' (check --write-baseline): ' + ', '.join(fixed))
    if new:
        log(f'Give each a /// <summary> saying what it does for a modder writing XML. ({CONFIG} lists the known gaps.)')
    return 1 if new else 0


def write_baseline(root, settings, log=print):
    source = read_source(root, settings['projects'])
    missing = gaps(modder_types(source, settings['categories']))
    path = Path(root) / CONFIG
    data = json.loads(path.read_text(encoding='utf-8-sig'))
    data['knownGaps'] = missing
    path.write_text(json.dumps(data, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
    log(f'{CONFIG}: {len(missing)} known gaps written')


def list_types(root, settings, log=print):
    source = read_source(root, settings['projects'])
    chosen = modder_types(source, settings['categories'])
    counts = {}
    for info in chosen:
        counts.setdefault(info.category, [0, 0])
        counts[info.category][0] += 1
        counts[info.category][1] += 0 if info.documented else 1
    log('| Kind | Types | Without a summary |')
    log('| --- | --- | --- |')
    for category in settings['categories']:
        total, undocumented = counts.get(category['name'], (0, 0))
        log(f'| {category["name"]} | {total} | {undocumented} |')
    log(f'| All | {len(chosen)} | {len(gaps(chosen))} |')


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.split('\n\n')[0])
    sub = parser.add_subparsers(dest='command', required=True)
    sub.add_parser('list', help='the XML-facing types by kind, with how many have no <summary>')
    checker = sub.add_parser('check', help='fail when an XML-facing type has no <summary> and is not a known gap')
    checker.add_argument('--write-baseline', action='store_true', help=f'record today\'s gaps as known in {CONFIG}')
    pages = sub.add_parser('pages', help='write the generated pages into a work folder, without DocFX')
    pages.add_argument('--work', type=Path, help='the work folder (default: the kit cache\'s rimworld-reference-site/work)')
    builder = sub.add_parser('build', help='write the pages, run DocFX and write the site')
    builder.add_argument('--work', type=Path, help='the work folder (default: the kit cache\'s rimworld-reference-site/work)')
    builder.add_argument('--site', type=Path, help='where the site goes (default: the kit cache\'s rimworld-reference-site/site)')
    builder.add_argument('--docfx', default=os.environ.get('DOCFX', 'docfx'), help='the docfx command (default: docfx on PATH)')
    builder.add_argument('--no-restore', action='store_true', help='never run dotnet restore for the reference assemblies')
    verifier = sub.add_parser('verify', help='the gate\'s step: build the site into a throwaway folder; fail on any DocFX warning or error')
    verifier.add_argument('--docfx', default=os.environ.get('DOCFX', 'docfx'), help='the docfx command (default: docfx on PATH)')
    args = parser.parse_args(argv)
    settings = load_settings(ROOT)
    try:
        if args.command == 'list':
            list_types(ROOT, settings)
            return 0
        if args.command == 'check':
            if args.write_baseline:
                write_baseline(ROOT, settings)
                return 0
            return check(ROOT, settings)
        if args.command == 'pages':
            work = args.work or default_folder('work')
            prepare_folder(work)
            chosen, counts = write_pages(ROOT, settings, work, Progress())
            print(f'{len(chosen)} XML-facing types; pages for modders: '
                  + ', '.join(f'{uid} {count}' for uid, count in counts.items()) + f'; pages written to {work}')
            return 0
        if args.command == 'verify':
            return verify(ROOT, settings, args.docfx)
        work = args.work or default_folder('work')
        site = args.site or default_folder('site')
        build(ROOT, settings, work, site, args.docfx, progress=Progress(), restore=not args.no_restore)
        return 0
    except SiteError as error:
        print(f'reference-site: {error}', file=sys.stderr)
        return 1


if __name__ == '__main__':
    sys.exit(main())
