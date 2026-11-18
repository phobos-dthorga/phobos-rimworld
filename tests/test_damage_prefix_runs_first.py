"""A prefix of ours that refuses or shrinks a hit on a vehicle must run before every other prefix on that method.

Every hit on a Vehicle Framework vehicle ends in ``VehicleStatHandler.ApplyDamage``. Other mods put prefixes there that apply
the damage themselves and skip the original: Combat Extended's does so for every hit while its vehicle armour setting is on.
Harmony runs prefixes of equal priority in the order their mods load, so a prefix of ours at the usual priority ran after such a
one, and a hull that was under water took the whole hit our prefix was written to refuse (found by reading, 18 Nov 2026; fixed in
Framework 0.140.1). Several mods already use Harmony's highest named priority there, so ours goes above it.

This test reads our C# as text. Any class of ours that patches that method with a prefix returning ``bool`` must give the prefix
a ``HarmonyPriority`` built on ``Priority.First``.
"""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = re.compile(r'\[HarmonyPatch\(\s*typeof\(VehicleStatHandler\)\s*,\s*nameof\(VehicleStatHandler\.ApplyDamage\)\s*\)\]\s*'
                    r'public\s+static\s+class\s+(\w+)\s*\{')
BOOL_PREFIX = re.compile(r'((?:\[[^\]]*\]\s*)*)public\s+static\s+bool\s+Prefix\s*\(')
PRIORITY = re.compile(r'\[HarmonyPriority\(\s*([^)]*)\)\]')
CONSTANT = r'const\s+int\s+%s\s*=\s*([^;]*);'


def strip_comments(text):
    text = re.sub(r'/\*.*?\*/', '', text, flags=re.S)
    return re.sub(r'//[^\n]*', '', text)


def body_at(text, open_brace):
    depth = 0
    for i in range(open_brace, len(text)):
        if text[i] == '{':
            depth += 1
        elif text[i] == '}':
            depth -= 1
            if depth == 0:
                return text[open_brace:i + 1]
    return text[open_brace:]


def offences(text):
    text = strip_comments(text)
    out = []
    for match in TARGET.finditer(text):
        body = body_at(text, match.end() - 1)
        prefix = BOOL_PREFIX.search(body)
        if prefix is None:
            continue
        priority = PRIORITY.search(prefix.group(1))
        value = priority.group(1).strip() if priority else ''
        named = re.search(CONSTANT % re.escape(value), body) if re.fullmatch(r'\w+', value) else None
        if named:
            value = named.group(1)
        if 'Priority.First' not in value:
            out.append(match.group(1))
    return out


GOOD = '''
[HarmonyPatch(typeof(VehicleStatHandler), nameof(VehicleStatHandler.ApplyDamage))]
public static class Sample
{
    public const int BeforeEveryOtherPrefix = Priority.First + 100;

    [HarmonyPriority(BeforeEveryOtherPrefix)]
    public static bool Prefix(VehiclePawn ___vehicle, ref DamageInfo dinfo) { return true; }
}
'''


class DamagePrefixRunsFirst(unittest.TestCase):
    def test_a_prefix_at_the_usual_priority_is_refused(self):
        self.assertEqual(offences(GOOD.replace('    [HarmonyPriority(BeforeEveryOtherPrefix)]\n', '')), ['Sample'])

    def test_a_low_priority_is_refused(self):
        self.assertEqual(offences(GOOD.replace('BeforeEveryOtherPrefix)]', 'Priority.Low)]')), ['Sample'])

    def test_a_priority_built_on_the_highest_passes(self):
        self.assertEqual(offences(GOOD), [])
        self.assertEqual(offences(GOOD.replace('[HarmonyPriority(BeforeEveryOtherPrefix)]', '[HarmonyPriority(Priority.First)]')), [])

    def test_a_postfix_or_a_void_prefix_is_not_judged(self):
        self.assertEqual(offences(GOOD.replace('    [HarmonyPriority(BeforeEveryOtherPrefix)]\n', '').replace('static bool Prefix', 'static void Postfix')), [])

    def test_our_damage_prefixes_run_first(self):
        found, seen = [], 0
        for path in sorted((ROOT / 'src').rglob('*.cs')):
            if 'obj' in path.parts or 'bin' in path.parts:
                continue
            text = path.read_text(encoding='utf-8-sig')
            if 'VehicleStatHandler.ApplyDamage' not in text:
                continue
            seen += len(TARGET.findall(strip_comments(text)))
            found += [f'{path.relative_to(ROOT).as_posix()}: {name}' for name in offences(text)]
        self.assertGreater(seen, 0, 'no patch class on VehicleStatHandler.ApplyDamage was found: this test no longer reads the code it guards')
        self.assertEqual(found, [], 'a prefix that can refuse a hit runs after other mods\' prefixes: ' + ', '.join(found))


if __name__ == '__main__':
    unittest.main()
