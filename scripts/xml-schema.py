"""An XML Schema (XSD) of D'thorga's XML, so a modder's editor offers our Defs, their fields and their values (a prototype).

    python scripts/xml-schema.py build --out FILE.xsd          writes the schema, generated from our C# source
    python scripts/xml-schema.py validate FILE.xsd XML...      validates XML files against it with the JDK's own validator
                                                               (Xerces, as VS Code's XML extension and Rider use), as they do

Generated from the same reading of our source as the modders' reference site (`scripts/reference-site.py`, imported, never
copied): every XML-facing type, its public fields, their types, defaults and summaries. Nothing is compiled or loaded.

How RimWorld XML is described, and why (an XSD 1.0 schema, no target namespace, as the editors read only XSD 1.0):

- **Every type is open, text included** (mixed: the game's own enums, colours and rectangles are written as text where the
  scanner sees only a class). Each element's content is "any of these known fields, in any order, plus any other unqualified
  element, unchecked" (`xs:choice` of the fields and `xs:any namespace="##local" processContents="skip"`), with any attribute.
  RimWorld reads fields the scanner cannot see (the game's own base classes, non-public fields, `LoadAlias` names), so a closed
  type would mark valid XML as wrong; false errors are worse than no schema. The editors still offer the known fields, check
  their values and show their summaries.
- That content model is ambiguous by XSD 1.0's letter (Unique Particle Attribution): XSD 1.1 allows it (a declared element wins
  over the wildcard), and Xerces, the validator VS Code's XML extension and Rider use, checks it only when "schema full
  checking" is on, which VS Code's never turns on. libxml2 (lxml, xmllint) refuses such a schema.
- **A type is chosen by its Class attribute in RimWorld; XSD cannot do that** (XSD 1.1's `xs:alternative` could, but neither
  editor reads XSD 1.1). So a list item or field typed by one of our base classes offers that base's fields only. A modder can
  add `xsi:type="<the class's full name>"` beside `Class` to get that class's fields offered; every class of ours derives (by
  restriction) from its C# base, so the editor offers the right names there.
- Global elements: `Defs` (our Defs offered, anything else accepted unchecked), each concrete Def of ours by its full class
  name, and skeletons of the game's and Vehicle Framework's Defs our comps go in (their `comps` and `modExtensions` only).
"""
from __future__ import annotations

import argparse
from dataclasses import dataclass, field
import importlib.util
from pathlib import Path
import re
import shutil
import subprocess
import sys
import tempfile
from xml.sax.saxutils import escape, quoteattr

ROOT = Path(__file__).resolve().parents[1]
SCRIPTS = Path(__file__).resolve().parent

XS = 'http://www.w3.org/2001/XMLSchema'
OPEN = 'rw.Open'                 # any content, any attribute: the root of every complex type here
DEF = 'rw.Def'                   # Verse.Def's fields every Def has, open
STRING = 'rw.string'
BOOL = 'rw.bool'
BLANK = 'rw.blank'
NUMBERS = {'int': 'xs:int', 'long': 'xs:long', 'short': 'xs:short', 'byte': 'xs:unsignedByte', 'sbyte': 'xs:byte',
           'uint': 'xs:unsignedInt', 'ulong': 'xs:unsignedLong', 'ushort': 'xs:unsignedShort', 'decimal': 'xs:decimal',
           'float': 'xs:float', 'double': 'xs:double'}
# The game's types written as one text value ("1~3", "(1, 2, 3)", a type's name): checked as text only.
TEXT_TYPES = {'string', 'char', 'Type', 'IntRange', 'FloatRange', 'IntVec2', 'IntVec3', 'Vector2', 'Vector3', 'Color', 'Color32',
              'Rot4', 'QualityRange', 'CurvePoint', 'Guid', 'TaggedString'}
# Attributes RimWorld reads on any element (offered by name; any other attribute is accepted too).
ATTRIBUTES = [
    ('Name', 'This node\'s name, for another to inherit from with ParentName.'),
    ('ParentName', 'The Name of the node this one inherits from: its fields are copied, then this node\'s override them.'),
    ('Abstract', 'True: a template for others to inherit from; never loaded as a Def itself.'),
    ('Class', 'The C# class this element is read as (full name). An editor cannot follow it: add xsi:type with the same name '
              'to have that class\'s fields offered.'),
    ('Inherit', 'False: a list here replaces the inherited one instead of adding to it.'),
    ('MayRequire', 'Load this node only when the named mod (package ID) is active.'),
    ('MayRequireAnyOf', 'Load this node only when any of the named mods (package IDs, comma-separated) is active.'),
    ('IsNull', 'True: set the field to null.'),
]
# Verse.Def's fields, offered on every Def (the rest of the game's are accepted unchecked).
DEF_FIELDS = [
    ('defName', STRING, 'The Def\'s unique name.'),
    ('label', STRING, 'The name players see.'),
    ('description', STRING, 'The description players see.'),
]
# The game's and Vehicle Framework's Defs our comps go in: element name, {container field: category of what goes in it}.
# RimWorld's own knowledge, so data: a next game names its own. Only these containers are described; the rest is open.
HOSTS = [
    ('ThingDef', {'comps': 'Comps'}),
    ('HediffDef', {'comps': 'Hediff comps'}),
    ('Vehicles.VehicleDef', {'comps': 'Vehicle comps'}),
]
EXTENSIONS_CATEGORY = 'Def extensions'
# The base class's own fields a category's classes inherit from the game (the rest are accepted unchecked).
CATEGORY_BASE_FIELDS = {
    'Comps': [('compClass', STRING, 'The comp class this properties class makes (full name).')],
    'Hediff comps': [('compClass', STRING, 'The hediff comp class this properties class makes (full name).')],
    'Vehicle comps': [('compClass', STRING, 'The vehicle comp class this properties class makes (full name).')],
}
FLAGS_ENUM = re.compile(r'\[\s*(?:System\.)?Flags(?:Attribute)?\s*\][^{;]*?\benum\s+(\w+)')


def load_site():
    """scripts/reference-site.py as a module (its file name has a hyphen); the same module when a test loaded it first."""
    if 'reference_site' in sys.modules:
        return sys.modules['reference_site']
    spec = importlib.util.spec_from_file_location('reference_site', SCRIPTS / 'reference-site.py')
    module = importlib.util.module_from_spec(spec)
    sys.modules['reference_site'] = module      # dataclasses look their module up while the class is made
    spec.loader.exec_module(module)
    return module


site = load_site()
from phobos_modkit.code.scan import declarations  # noqa: E402  (reference-site.py put the kit on the path)


# ----------------------------------------------------------------------------------------------------------------------------
# What the source says beyond the reference site's reading


@dataclass
class Extra:
    flags: set = field(default_factory=set)       # enums marked [Flags]: written "A, B", so any text is accepted
    custom: set = field(default_factory=set)      # types read by their own LoadDataFromXmlCustom: any children


def read_extra(root, projects):
    extra = Extra()
    for path in site.source_files(root, projects):
        text = path.read_text(encoding='utf-8-sig')
        extra.flags.update(FLAGS_ENUM.findall(text))
        if 'LoadDataFromXmlCustom' in text:
            extra.custom.update(d.owner for d in declarations(text) if d.kind == 'method' and d.name == 'LoadDataFromXmlCustom')
    return extra


def type_name(name):
    """A C# type name as written, without nullable mark or global:: prefix."""
    return name.strip().rstrip('?').replace('global::', '').strip()


# ----------------------------------------------------------------------------------------------------------------------------
# The schema


def _doc(text, indent):
    text = (text or '').strip()
    if not text:
        return []
    return [f'{indent}<xs:annotation><xs:documentation>{escape(text)}</xs:documentation></xs:annotation>']


def _type_id(category_name):
    return 'rw.kind.' + re.sub(r'\W+', '_', category_name).strip('_')


class Schema:
    def __init__(self, source, categories, extra):
        self.source = source
        self.categories = {c['name']: c for c in categories}
        self.extra = extra
        self.chosen = site.modder_types(source, categories)     # classifies every type: category and chain
        self.by_last = {}
        for info in source.types.values():
            self.by_last.setdefault(info.full.split('.')[-1], []).append(info)
        self.defined = {}            # type name -> its XSD text
        self.fields_of = {}          # complex type name -> [(element name, type, documentation)], inherited first
        self.pending = []
        self.globals = []            # (element name, type, documentation)

    # -- resolving C# names

    def resolve(self, name, namespace):
        name = type_name(name)
        if '<' in name or not name:
            return None
        if name in self.source.types:
            return self.source.types[name]
        found = [t for t in self.by_last.get(name.split('.')[-1], []) if t.full == name or t.full.endswith('.' + name)]
        if not found:
            return None
        same = [t for t in found if t.namespace == namespace]
        return (same or found)[0]

    def is_def(self, info):
        category = self.categories.get(info.category or '')
        return bool(category and category.get('form') == 'def')

    # -- field types

    def field_type(self, cs_type, namespace):
        """The XSD type a field of this C# type is written as, defining it when needed."""
        plain = type_name(cs_type)
        items = site.element_type(plain)
        if items is not None:
            return self.list_type(self.field_type(items, namespace))
        if site.DICTIONARY.match(plain):
            return OPEN
        simple = site.simple_name(plain)
        if plain in NUMBERS:
            return self.number_type(plain)
        if plain == 'bool':
            return BOOL
        if simple in self.source.enums and self.source.enums[simple]:
            return self.enum_type(simple)
        info = self.resolve(plain, namespace)
        if info is not None:
            if self.is_def(info):
                return STRING                         # a Def is named by its defName
            return self.complex_type(info)
        if simple in TEXT_TYPES or simple.endswith('Def'):
            return STRING
        return OPEN

    def number_type(self, cs):
        name = f'rw.{cs}'
        if name not in self.defined:
            base = NUMBERS[cs]
            members = [base, BLANK] + (['rw.infinity'] if cs in ('float', 'double') else [])
            self.defined[name + '.text'] = (f'  <xs:simpleType name="{name}.text"><xs:union memberTypes="{" ".join(members)}"/>'
                                            f'</xs:simpleType>')
            self.defined[name] = self._simple_content(name, name + '.text', f'A number (C# {cs}).')
        return name

    def enum_type(self, enum):
        name = f'enum.{enum}'
        if name not in self.defined:
            values = self.source.enums[enum]
            lines = [f'  <xs:simpleType name="{name}.values"><xs:restriction base="xs:token">']
            lines += [f'    <xs:enumeration value={quoteattr(v)}/>' for v in values]
            lines.append('  </xs:restriction></xs:simpleType>')
            self.defined[name + '.values'] = '\n'.join(lines)
            members = f'{name}.values {BLANK}' + (' xs:string' if enum in self.extra.flags else '')
            self.defined[name + '.text'] = f'  <xs:simpleType name="{name}.text"><xs:union memberTypes="{members}"/></xs:simpleType>'
            note = ' A [Flags] enum: several may be written, comma-separated.' if enum in self.extra.flags else ''
            self.defined[name] = self._simple_content(name, name + '.text', f'One of {enum}\'s values.{note}')
        return name

    def _simple_content(self, name, base, doc):
        return '\n'.join([f'  <xs:complexType name="{name}">'] + _doc(doc, '    ') +
                         [f'    <xs:simpleContent><xs:extension base="{base}"><xs:anyAttribute processContents="skip"/>'
                          '</xs:extension></xs:simpleContent>', '  </xs:complexType>'])

    def list_type(self, item):
        name = f'rw.List.{item}'
        if name not in self.defined:
            self.fields_of[name] = [('li', item, '')]
            self.defined[name] = self._restriction(name, OPEN, self.fields_of[name], 'A list: one <li> per item.')
        return name

    # -- complex types

    def _restriction(self, name, base, fields, doc):
        lines = [f'  <xs:complexType name="{name}" mixed="true">'] + _doc(doc, '    ')
        lines += ['    <xs:complexContent mixed="true">', f'      <xs:restriction base="{base}">',
                  '        <xs:choice minOccurs="0" maxOccurs="unbounded">']
        for element, element_type, element_doc in fields:
            if element_doc:
                lines.append(f'          <xs:element name="{element}" type="{element_type}">')
                lines += _doc(element_doc, '            ')
                lines.append('          </xs:element>')
            else:
                lines.append(f'          <xs:element name="{element}" type="{element_type}"/>')
        lines += ['          <xs:any namespace="##local" processContents="skip"/>', '        </xs:choice>',
                  '        <xs:anyAttribute processContents="skip"/>', '      </xs:restriction>', '    </xs:complexContent>',
                  '  </xs:complexType>']
        return '\n'.join(lines)

    def category_root(self, category_name):
        """The type every class of one kind derives from, so an editor offers them as xsi:type values for that kind."""
        name = _type_id(category_name)
        if name not in self.defined:
            category = self.categories.get(category_name, {})
            base = DEF if category.get('form') == 'def' else OPEN
            fields = list(self.fields_of.get(base, [])) + list(CATEGORY_BASE_FIELDS.get(category_name, []))
            self.fields_of[name] = fields
            self.defined[name] = self._restriction(name, base, fields, f'{category_name}. {category.get("where", "")}'.strip())
        return name

    def base_type(self, info):
        if info.chain and '<' not in info.chain[0].full:
            return self.complex_type(info.chain[0])
        if info.category:
            return self.category_root(info.category)
        return OPEN

    def complex_type(self, info):
        name = info.full
        if name in self.defined or name in self.pending:
            return name
        self.pending.append(name)
        base = self.base_type(info)
        inherited = list(self.fields_of.get(base, []))
        names = {f[0] for f in inherited}
        own = []
        if name not in self.extra.custom:
            for item in info.fields:
                if item.name in names:
                    continue                      # a field hiding an inherited one keeps the inherited declaration
                names.add(item.name)
                shown = site.default_value(item.type, item.initial, self.source.enums, self.source.constants)
                doc = (item.summary + '\n\n' if item.summary else '') + f'C# {item.type}; default {shown.text}. ({info.name})'
                own.append((item.name, self.field_type(item.type, info.namespace), doc))
        self.fields_of[name] = inherited + own
        self.defined[name] = self._restriction(name, base, inherited + own, self.type_doc(info))
        self.pending.remove(name)
        return name

    def type_doc(self, info):
        parts = [info.summary or f'{info.name} (no summary yet).']
        category = self.categories.get(info.category or '')
        if category and category.get('where'):
            parts.append(category['where'])
        if self.is_def(info):
            parts.append(f'Written as <{info.full}> in your Defs.')
        else:
            parts.append(f'Written with Class="{info.xml_class}"; add xsi:type="{info.full}" beside it for its fields to be '
                         'offered.')
        if info.full in self.extra.custom:
            parts.append('It reads its XML with its own code, so any child elements are accepted here.')
        if info.abstract:
            parts.append('Abstract in C#: name a class derived from it.')
        return ' '.join(parts)

    # -- the whole

    def build(self):
        self.defined[BLANK] = (f'  <xs:simpleType name="{BLANK}"><xs:restriction base="xs:string"><xs:pattern value="\\s*"/>'
                               '</xs:restriction></xs:simpleType>')
        self.defined['rw.infinity'] = ('  <xs:simpleType name="rw.infinity"><xs:restriction base="xs:string">'
                                       '<xs:pattern value="\\s*[+\\-]?(Infinity|∞)\\s*"/></xs:restriction></xs:simpleType>')
        self.defined['rw.bool.values'] = ('  <xs:simpleType name="rw.bool.values"><xs:restriction base="xs:token">'
                                          '<xs:enumeration value="true"/><xs:enumeration value="false"/>'
                                          '</xs:restriction></xs:simpleType>')
        self.defined['rw.bool.anycase'] = ('  <xs:simpleType name="rw.bool.anycase"><xs:restriction base="xs:string">'
                                           '<xs:pattern value="\\s*([Tt][Rr][Uu][Ee]|[Ff][Aa][Ll][Ss][Ee])\\s*"/>'
                                           '</xs:restriction></xs:simpleType>')
        self.defined['rw.bool.text'] = ('  <xs:simpleType name="rw.bool.text">'
                                        f'<xs:union memberTypes="rw.bool.values rw.bool.anycase {BLANK}"/></xs:simpleType>')
        self.defined[BOOL] = self._simple_content(BOOL, 'rw.bool.text', 'true or false (any case, as the game reads it).')
        self.defined[STRING] = self._simple_content(STRING, 'xs:string', 'Text.')
        lines = [f'  <xs:complexType name="{OPEN}" mixed="true">'] + _doc('Any content, text included: what this schema does not '
                                                                       'describe.', '    ')
        lines += ['    <xs:choice minOccurs="0" maxOccurs="unbounded">',
                  '      <xs:any namespace="##local" processContents="skip"/>', '    </xs:choice>']
        for attribute, doc in ATTRIBUTES:
            lines.append(f'    <xs:attribute name="{attribute}" type="xs:string">')
            lines += _doc(doc, '      ')
            lines.append('    </xs:attribute>')
        lines += ['    <xs:anyAttribute processContents="skip"/>', '  </xs:complexType>']
        self.defined[OPEN] = '\n'.join(lines)
        self.fields_of[OPEN] = []
        extensions = self.category_root(EXTENSIONS_CATEGORY) if EXTENSIONS_CATEGORY in self.categories else OPEN
        def_fields = list(DEF_FIELDS) + [('modExtensions', self.list_type(extensions),
                                           'Mod extensions: one <li Class="..."> each.')]
        self.fields_of[DEF] = def_fields
        self.defined[DEF] = self._restriction(DEF, OPEN, def_fields, 'What every Def has (Verse.Def); its other fields are '
                                              'accepted unchecked.')
        for info in self.chosen:
            if '<' in info.full:
                continue
            type_ = self.complex_type(info)
            if self.is_def(info) and not info.abstract and not info.nested_in:
                self.globals.append((info.full, type_, self.type_doc(info)))
        for host, containers in HOSTS:
            fields = list(self.fields_of[DEF])
            for container, category in containers.items():
                if category in self.categories:
                    fields.append((container, self.list_type(self.category_root(category)),
                                   f'{category}: one <li Class="..."> each. {self.categories[category].get("where", "")}'))
            name = 'rw.host.' + host
            self.fields_of[name] = fields
            self.defined[name] = self._restriction(name, DEF, fields, f'{host}, the game\'s or another mod\'s Def: only what '
                                                   'D\'thorga adds to it is described; everything else is accepted unchecked.')
            self.globals.append((host, name, f'{host}: only its D\'thorga comps and extensions are described.'))
        return self.text()

    def text(self):
        out = ['<?xml version="1.0" encoding="utf-8"?>',
               '<!-- Generated by phobos-rimworld scripts/xml-schema.py from D\'thorga\'s C# source; do not edit. -->',
               f'<xs:schema xmlns:xs="{XS}" elementFormDefault="unqualified" attributeFormDefault="unqualified">']
        out += _doc('D\'thorga\'s XML for RimWorld: our Defs, comps, extensions and vehicle system classes, with their fields. '
                    'Anything it does not describe is accepted unchecked.', '  ')
        out.append('  <xs:element name="Defs">')
        out += _doc('A Defs file. D\'thorga\'s Defs are offered; any other Def is accepted unchecked.', '    ')
        out += ['    <xs:complexType>', '      <xs:choice minOccurs="0" maxOccurs="unbounded">']
        out += [f'        <xs:element ref="{name}"/>' for name, _type, _doc_text in self.globals]
        out += ['        <xs:any namespace="##local" processContents="skip"/>', '      </xs:choice>',
                '      <xs:anyAttribute processContents="skip"/>', '    </xs:complexType>', '  </xs:element>']
        for root in ('Patch', 'LanguageData'):
            out.append(f'  <xs:element name="{root}" type="{OPEN}"/>')
        for name, type_, doc in self.globals:
            out.append(f'  <xs:element name="{name}" type="{type_}">')
            out += _doc(doc, '    ')
            out.append('  </xs:element>')
        out += [self.defined[name] for name in sorted(self.defined)]
        out.append('</xs:schema>')
        return '\n'.join(out) + '\n'


def generate(root, settings, progress=None):
    """The schema's text, and the Schema (its counts)."""
    source = site.read_source(root, settings['projects'], progress)
    schema = Schema(source, settings['categories'], read_extra(root, settings['projects']))
    return schema.build(), schema


# ----------------------------------------------------------------------------------------------------------------------------
# Checking it the way the editors do: Xerces, in the JDK


JAVA_CHECK = r'''
import java.io.File;
import javax.xml.XMLConstants;
import javax.xml.transform.stream.StreamSource;
import javax.xml.validation.*;
import org.xml.sax.*;

public class Check {
    static class Report implements ErrorHandler {
        public void warning(SAXParseException e) { print("warning", e); }
        public void error(SAXParseException e) { print("error", e); }
        public void fatalError(SAXParseException e) { print("error", e); }
        void print(String kind, SAXParseException e) {
            String file = e.getSystemId() == null ? "" : e.getSystemId();
            System.out.println(kind + "\t" + file + "\t" + e.getLineNumber() + "\t" + e.getMessage());
        }
    }
    public static void main(String[] args) throws Exception {
        SchemaFactory factory = SchemaFactory.newInstance(XMLConstants.W3C_XML_SCHEMA_NS_URI);
        factory.setFeature("http://apache.org/xml/features/validation/schema-full-checking", args[0].equals("full"));
        Report report = new Report();
        factory.setErrorHandler(report);
        Schema schema = factory.newSchema(new File(args[1]));
        for (int i = 2; i < args.length; i++) {
            System.out.println("file\t" + new File(args[i]).toURI() + "\t0\t");
            Validator validator = schema.newValidator();
            validator.setErrorHandler(report);
            try { validator.validate(new StreamSource(new File(args[i]))); } catch (SAXException e) { }
        }
    }
}
'''


class JavaMissing(RuntimeError):
    pass


def java_validate(schema_path, xml_paths, full=False, java=None):
    """{'schema': [problems in the schema], file: [problems]} from the JDK's validator (its Xerces), with "schema full checking"
    off as VS Code's XML extension leaves it, or on (`full`) as Rider's Validate does. Each problem is (kind, line, message)."""
    java = java or shutil.which('java')
    if not java:
        raise JavaMissing('java is not on PATH')
    folder = Path(tempfile.mkdtemp(prefix='xml-schema-check-'))
    try:
        (folder / 'Check.java').write_text(JAVA_CHECK, encoding='utf-8')
        result = subprocess.run([java, str(folder / 'Check.java'), 'full' if full else 'lax', str(schema_path),
                                 *[str(p) for p in xml_paths]], capture_output=True, text=True, encoding='utf-8',
                                errors='replace', timeout=300)
    finally:
        shutil.rmtree(folder, ignore_errors=True)
    if result.returncode != 0 and 'file\t' not in result.stdout:
        raise RuntimeError(result.stdout + result.stderr)
    found, current = {'schema': []}, 'schema'
    for line in result.stdout.splitlines():
        parts = line.split('\t', 3)
        if len(parts) < 4:
            continue
        if parts[0] == 'file':
            current = parts[1]
            found[current] = []
            continue
        key = current if parts[1] and not parts[1].endswith('.xsd') else 'schema'
        found.setdefault(key, []).append((parts[0], int(parts[2]), parts[3]))
    return found


# ----------------------------------------------------------------------------------------------------------------------------
# The command


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.split('\n\n')[0])
    sub = parser.add_subparsers(dest='command', required=True)
    builder = sub.add_parser('build', help='write the schema')
    builder.add_argument('--out', type=Path, required=True, help='the .xsd file to write')
    checker = sub.add_parser('validate', help='validate XML files against a schema with the JDK\'s Xerces')
    checker.add_argument('schema', type=Path)
    checker.add_argument('xml', type=Path, nargs='+')
    checker.add_argument('--full', action='store_true', help='with "schema full checking" on, as Rider\'s Validate action')
    args = parser.parse_args(argv)
    if args.command == 'build':
        text, schema = generate(ROOT, site.load_settings(ROOT))
        args.out.parent.mkdir(parents=True, exist_ok=True)
        args.out.write_text(text, encoding='utf-8')
        complex_types = sum(1 for t in schema.defined.values() if t.lstrip().startswith('<xs:complexType'))
        print(f'{args.out}: {len(schema.globals)} global elements, {complex_types} complex types, '
              f'{sum(1 for n in schema.defined if n.startswith("enum.") and n.endswith(".values"))} enums, '
              f'{len(text) // 1024} KB')
        return 0
    try:
        found = java_validate(args.schema, args.xml, args.full)
    except JavaMissing as error:
        print(f'xml-schema: {error}; the check needs a Java runtime', file=sys.stderr)
        return 1
    problems = 0
    for key, items in found.items():
        print(f'{key}: {"no problems" if not items else f"{len(items)} problems"}')
        for kind, line, message in items:
            print(f'  {kind} line {line}: {message}')
            problems += kind == 'error'
    return 1 if problems else 0


if __name__ == '__main__':
    sys.exit(main())
