"""Small readers for our own C# source: a call's arguments, and each ModSettings class's saved keys with their defaults.

Shared by scripts/check-mod-layout.py (translation keys, settings defaults that disagree) and scripts/mod-settings.py
(the defaults beside the owner's saved settings). Regular expressions over our own, consistently formatted code, not a
C# parser: what they cannot read they leave out rather than guess.
"""
from __future__ import annotations

from dataclasses import dataclass, field
import re

NAMESPACE = re.compile(r'^\s*namespace\s+([\w.]+)\s*[;{]', re.MULTILINE)
SETTINGS_CLASS = re.compile(r'\bclass\s+(\w+)\s*:\s*(?:Verse\.)?ModSettings\b')
ENUM = re.compile(r'\benum\s+(\w+)\s*\{([^}]*)\}')
FIELD = re.compile(r'^\s*(?:\[[^\]]*\]\s*)*(?:public|internal|private|protected)(?:\s+(?:static|readonly|const))*\s+'
                   r'(?P<type>[A-Za-z_][\w.<>,?\[\] ]*?)\s+(?P<name>\w+)\s*(?:=\s*(?P<init>[^;]+?))?\s*;', re.MULTILINE)
EXPOSE = re.compile(r'\bvoid\s+ExposeData\s*\(\s*\)')
SCRIBE = re.compile(r'\bScribe_(?P<kind>Values|Collections|Defs)\s*\.\s*Look\s*(?:<[^>]*>)?\s*\(')
DELEGATE = re.compile(r'\b(\w+)\s*\.\s*ExposeData\s*\(\s*\)')
NUMERIC = re.compile(r'^-?(?:\d+\.?\d*|\.\d+)(?:[eE][+-]?\d+)?[fFdDmM]?$')
NUMBER_TYPES = {'int', 'float', 'double', 'long', 'short', 'byte', 'uint', 'decimal', 'Int32', 'Single', 'Double'}
CONSTANT = re.compile(r'\b(?:const|static\s+readonly)\s+[\w.<>?]+\s+(?P<name>\w+)\s*=\s*(?P<value>[^;]+?)\s*;')
OWNER = re.compile(r'\b(?:class|struct|record)\s+(\w+)')
EMPTY_LIST = '(empty)'   # as rimworld-settings shows a list written with nothing in it


def read_constants(sources):
    """Constants and static readonly fields with a literal value: {'Owner.Name': value, 'Name': value when unique}."""
    qualified, bare = {}, {}
    for text in sources.values():
        for found in CONSTANT.finditer(text):
            owners = OWNER.findall(text, 0, found.start())
            name, value = found.group('name'), found.group('value').strip()
            if owners:
                qualified[f'{owners[-1]}.{name}'] = value
            bare.setdefault(name, set()).add(value)
    constants = dict(qualified)
    constants.update({name: values.pop() for name, values in bare.items() if len(values) == 1})
    return constants


def strip_comments(text):
    """The source with every // and /* */ comment blanked to spaces (newlines kept, so line numbers hold), strings left
    alone: an apostrophe in a comment ("the owner's rule") would otherwise read as the start of a character literal."""
    out, index, quote = [], 0, None
    while index < len(text):
        char = text[index]
        if quote:
            out.append(char)
            if char == '\\' and quote != 'verbatim' and index + 1 < len(text):
                out.append(text[index + 1])
                index += 1
            elif quote == 'verbatim' and char == '"':
                if text[index + 1:index + 2] == '"':
                    out.append('"')
                    index += 1
                else:
                    quote = None
            elif char == quote:
                quote = None
        elif text.startswith('//', index):
            end = text.find('\n', index)
            end = len(text) if end < 0 else end
            out.append(' ' * (end - index))
            index = end
            continue
        elif text.startswith('/*', index):
            end = text.find('*/', index + 2)
            end = len(text) if end < 0 else end + 2
            out.append(re.sub(r'[^\n]', ' ', text[index:end]))
            index = end
            continue
        elif char == '@' and text[index + 1:index + 2] == '"':
            out.append('@"')
            quote = 'verbatim'
            index += 1
        elif char in '"\'':
            out.append(char)
            quote = char
        else:
            out.append(char)
        index += 1
    return ''.join(out)


def call_arguments(text, start):
    """The text between the parenthesis at start and its partner, skipping strings; '' when unbalanced."""
    depth, index, quote = 0, start, None
    while index < len(text):
        char = text[index]
        if quote:
            if char == '\\':
                index += 1
            elif char == quote:
                quote = None
        elif char in '"\'':
            quote = char
        elif char == '(':
            depth += 1
        elif char == ')':
            depth -= 1
            if depth == 0:
                return text[start + 1:index]
        index += 1
    return ''


def top_level_arguments(arguments):
    """A call's arguments split at the commas outside brackets and strings."""
    parts, depth, quote, current = [], 0, None, ''
    index = 0
    while index < len(arguments):
        char = arguments[index]
        current += char
        if quote:
            if char == '\\' and index + 1 < len(arguments):
                current += arguments[index + 1]
                index += 1
            elif char == quote:
                quote = None
        elif char in '"\'':
            quote = char
        elif char in '([{':
            depth += 1
        elif char in ')]}':
            depth -= 1
        elif char == ',' and depth == 0:
            parts.append(current[:-1].strip())
            current = ''
        index += 1
    if current.strip():
        parts.append(current.strip())
    return parts


def block_after(text, start):
    """The text inside the first {...} at or after start, braces balanced (strings skipped); '' when there is none."""
    open_at = text.find('{', start)
    if open_at < 0:
        return ''
    depth, index, quote = 0, open_at, None
    while index < len(text):
        char = text[index]
        if quote:
            if char == '\\':
                index += 1
            elif char == quote:
                quote = None
        elif char in '"\'':
            quote = char
        elif char == '{':
            depth += 1
        elif char == '}':
            depth -= 1
            if depth == 0:
                return text[open_at + 1:index]
        index += 1
    return ''


@dataclass
class SavedKey:
    key: str
    field: str
    kind: str                      # Values, Collections or Defs
    default: str                   # what a missing key loads as, written the way RimWorld writes it
    initial: str | None = None     # the field's starting value, when it has an initialiser
    path: str = ''
    line: int = 0


@dataclass
class SettingsClass:
    name: str
    namespace: str
    path: str
    fields: dict = field(default_factory=dict)        # name -> (type, initialiser or None)
    keys: list = field(default_factory=list)          # SavedKey, in ExposeData order
    delegates: list = field(default_factory=list)     # field names whose ExposeData this class calls

    @property
    def full_name(self):
        return f'{self.namespace}.{self.name}' if self.namespace else self.name


def written(literal, enums, constants=None):
    """A C# literal as RimWorld writes the value: True/False, 0.5 not 0.5f, an enum member's name, a string's text."""
    text = literal.strip()
    constants = constants or {}
    for _ in range(4):          # a constant may be defined as another constant
        if text not in constants:
            break
        text = constants[text].strip()
    if text in ('true', 'false'):
        return text.capitalize()
    if NUMERIC.match(text):
        text = text.rstrip('fFdDmM')
        return text[:-2] if text.endswith('.0') else text
    if text.startswith('"') and text.endswith('"'):
        return text[1:-1]
    if text in ('null', 'default'):
        return ''
    if re.fullmatch(r'[A-Za-z_]\w*(\.\w+)+', text) and text.split('.')[-2] in enums:
        return text.rsplit('.', 1)[1]
    return text


def implicit_default(type_name, enums):
    """What a missing key loads as when Scribe_Values.Look names no default: the type's default value."""
    bare = type_name.strip().rstrip('?')
    if bare == 'bool':
        return 'False'
    if bare in NUMBER_TYPES:
        return '0'
    if bare in enums and enums[bare]:
        return enums[bare][0]
    return ''


def read_enums(sources):
    """Every enum's members in order, by simple name, from {path: text} (the first member is its default)."""
    enums = {}
    for text in sources.values():
        for found in ENUM.finditer(text):
            members = [m.split('=')[0].strip() for m in found.group(2).split(',')]
            members = [re.sub(r'\[[^\]]*\]', '', m).strip() for m in members]
            enums.setdefault(found.group(1), [m for m in members if m])
    return enums


def read_settings_classes(sources):
    """Every ModSettings subclass in {path: text}, with its fields and the keys its ExposeData saves."""
    sources = {path: strip_comments(text) for path, text in sources.items()}
    enums = read_enums(sources)
    every_constant = read_constants(sources)
    classes = {}
    for path, text in sorted(sources.items()):
        namespace = NAMESPACE.search(text)
        for found in SETTINGS_CLASS.finditer(text):
            body = block_after(text, found.end())
            body_start = text.find('{', found.end()) + 1
            cls = SettingsClass(found.group(1), namespace.group(1) if namespace else '', path)
            # The class's own constants first (by bare name), then every other class's, qualified or unique.
            constants = dict(every_constant)
            constants.update({c.group('name'): c.group('value').strip() for c in CONSTANT.finditer(body)})
            for f in FIELD.finditer(body):
                cls.fields[f.group('name')] = (f.group('type').strip(), f.group('init'))
            expose = EXPOSE.search(body)
            if expose:
                expose_body = block_after(body, expose.end())
                expose_start = body.find('{', expose.end()) + 1
                for d in DELEGATE.finditer(expose_body):
                    if d.group(1) not in ('base', 'this'):
                        cls.delegates.append(d.group(1))
                for call in SCRIBE.finditer(expose_body):
                    parts = top_level_arguments(call_arguments(expose_body, call.end() - 1))
                    if len(parts) < 2 or not parts[0].startswith('ref ') or not parts[1].startswith('"'):
                        continue
                    field_name = parts[0][4:].strip().split('.')[-1]
                    key = parts[1].strip('"')
                    type_name, init = cls.fields.get(field_name, ('', None))
                    kind = call.group('kind')
                    if kind == 'Values':
                        default = written(parts[2], enums, constants) if len(parts) > 2 else implicit_default(type_name, enums)
                    elif kind == 'Collections':
                        default = EMPTY_LIST   # a list is written even when empty
                    else:
                        default = ''
                    initial = None
                    if kind == 'Values' and init is not None and not init.strip().startswith('new'):
                        initial = written(init, enums, constants)
                    elif kind == 'Values' and init is None and type_name:
                        initial = implicit_default(type_name, enums)
                    line = text.count('\n', 0, body_start + expose_start + call.start()) + 1
                    cls.keys.append(SavedKey(key, field_name, kind, default, initial, path, line))
            classes[cls.full_name] = cls
    return classes


def all_keys(cls, classes, seen=None):
    """A class's saved keys and, in order, those of the settings objects its ExposeData hands on to (MedicalSettings'
    parts write into the same file)."""
    seen = seen or set()
    if cls.full_name in seen:
        return []
    seen.add(cls.full_name)
    keys = list(cls.keys)
    by_simple = {c.name: c for c in classes.values()}
    for name in cls.delegates:
        type_name = cls.fields.get(name, ('', None))[0].split('.')[-1]
        if type_name in by_simple:
            keys += all_keys(by_simple[type_name], classes, seen)
    return keys


def disagreements(classes):
    """Saved values whose field starts at one value while a missing key loads as another: a new player and one whose
    settings file predates the key get different settings."""
    found = []
    for cls in classes.values():
        for key in cls.keys:
            if key.kind != 'Values' or key.initial is None:
                continue
            same = key.initial.lower() == key.default.lower()
            try:
                same = same or float(key.initial) == float(key.default)
            except ValueError:
                pass
            if not same:
                found.append((cls, key))
    return found
