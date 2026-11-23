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
- The work folder is emptied before each build, and only when it holds this tool's marker file, so a mistyped `--work` never
  empties anything else.
"""
from __future__ import annotations

import argparse
from dataclasses import dataclass, field
import fnmatch
import html
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


def overwrite_page(info, category, enums, constants):
    """A DocFX overwrite file adding the XML example and the defaults table to the type's API page."""
    snippet, rows = xml_example(info, category, enums, constants)
    out = ['---', f'uid: {info.full}', 'example: [*content]', '---', '']
    where = category.get('where', '')
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


def catalogue_page(types, categories, repository):
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
            marker = ' *(abstract)*' if info.abstract else ''
            source = f'{repository}/blob/main/{info.path}#L{info.line}' if repository else ''
            link = f'<xref:{info.full}?text={info.name}>' + (f' ([source]({source}))' if source else '')
            out.append(f'| {link}{marker} | {_cell(summary)} |')
        out.append('')
    return '\n'.join(out)


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
    """Every generated input in the work folder: the project's own files, the guides, the XML examples and the XML types page.
    Returns the XML-facing types."""
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
    examples = work / 'xml-examples'
    examples.mkdir(exist_ok=True)
    for info in chosen:
        page = overwrite_page(info, by_name[info.category], source.enums, source.constants)
        (examples / f'{info.full}.md').write_text(page, encoding='utf-8')
    (work / 'xml-types.md').write_text(with_uid(catalogue_page(chosen, categories, repository), 'xml-types'), encoding='utf-8')
    return chosen


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
    chosen = write_pages(root, settings, work)
    project_config = json.loads((root / PROJECT / 'docfx.json').read_text(encoding='utf-8-sig'))
    config = effective_config(project_config, root, root / PROJECT, found)
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
    progress('Counting the pages', 4, 4)
    pages, api = count_pages(site)
    log(f'Site written to {site}: {pages} pages ({api} API pages), {len(chosen)} XML-facing types with an XML example')
    log(f'Reading the C#: {metadata.warnings} compiler warnings (the other mods\' types are left unresolved, and DocFX compiles '
        f'without our nullable setting), {metadata.errors} errors')
    log(f'Building the site: {site_run.warnings} warnings, {site_run.errors} errors' +
        (f'; read them in {work / "docfx-build.log"}' if site_run.warnings else ''))
    log(f'Open {site / "index.html"} in a browser to look at it.')
    return pages, api, len(chosen)


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
            chosen = write_pages(ROOT, settings, work, Progress())
            print(f'{len(chosen)} XML-facing types; pages written to {work}')
            return 0
        work = args.work or default_folder('work')
        site = args.site or default_folder('site')
        build(ROOT, settings, work, site, args.docfx, progress=Progress(), restore=not args.no_restore)
        return 0
    except SiteError as error:
        print(f'reference-site: {error}', file=sys.stderr)
        return 1


if __name__ == '__main__':
    sys.exit(main())
