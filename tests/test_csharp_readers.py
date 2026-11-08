"""scripts/_csharp.py on made-up C#: comments, arguments, ModSettings keys and their defaults, and disagreements."""
from pathlib import Path
import sys
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'scripts'))
import _csharp as cs  # noqa: E402

SETTINGS = '''namespace Phobos.Example;

public enum Mode { Plain, Operator, Engineer }

/// <summary>The owner's settings: an apostrophe here must not hide the class { from the reader.</summary>
public class PartSettings : ModSettings
{
    public float rate = 0.5f;
    public override void ExposeData()
    {
        Scribe_Values.Look(ref rate, "PhobosEx_Rate", 0.5f);
    }
}

public class ExampleSettings : ModSettings
{
    const float DefaultSpeed = 2f;
    public Mode mode = Mode.Operator;
    public bool strip;
    public bool warm = true;          // starts on, but a missing key loads as off
    public float speed = DefaultSpeed;
    public List<string> off = new();
    public PartSettings part = new();

    public override void ExposeData()
    {
        base.ExposeData();
        /* Scribe_Values.Look(ref ghost, "PhobosEx_Ghost", true); */
        Scribe_Values.Look(ref mode, "PhobosEx_Mode", Mode.Operator);
        Scribe_Values.Look(ref strip, "PhobosEx_Strip");
        Scribe_Values.Look(ref warm, "PhobosEx_Warm", false);
        Scribe_Values.Look(ref speed, "PhobosEx_Speed", DefaultSpeed);
        Scribe_Collections.Look(ref off, "PhobosEx_Off", LookMode.Value);
        part.ExposeData();
    }
}
'''


class Readers(unittest.TestCase):
    def test_comments_are_blanked_but_lines_and_strings_kept(self):
        text = 'a // it\'s gone\nb = "// kept"; /* x\ny */ c'
        stripped = cs.strip_comments(text)
        self.assertEqual(stripped.count('\n'), text.count('\n'))
        self.assertIn('"// kept"', stripped)
        self.assertNotIn("it's", stripped)
        self.assertTrue(stripped.rstrip().endswith('c'))

    def test_arguments_split_outside_brackets_and_strings(self):
        text = 'Call(a, Fmt("x, y"), new[] { 1, 2 }, (b, c))'
        self.assertEqual(cs.top_level_arguments(cs.call_arguments(text, text.index('('))),
                         ['a', 'Fmt("x, y")', 'new[] { 1, 2 }', '(b, c)'])

    def test_settings_keys_with_written_defaults_and_parts(self):
        classes = cs.read_settings_classes({'src/Example/Settings.cs': SETTINGS})
        example = classes['Phobos.Example.ExampleSettings']
        keys = {k.key: (k.default, k.initial) for k in cs.all_keys(example, classes)}
        self.assertEqual(keys, {
            'PhobosEx_Mode': ('Operator', 'Operator'),
            'PhobosEx_Strip': ('False', 'False'),
            'PhobosEx_Warm': ('False', 'True'),
            'PhobosEx_Speed': ('2', '2'),
            'PhobosEx_Off': ('(empty)', None),
            'PhobosEx_Rate': ('0.5', '0.5'),
        })
        self.assertNotIn('PhobosEx_Ghost', keys)
        self.assertEqual(next(k.line for k in example.keys if k.key == 'PhobosEx_Mode'), 29)

    def test_disagreements_name_the_field_that_starts_differently(self):
        classes = cs.read_settings_classes({'src/Example/Settings.cs': SETTINGS})
        self.assertEqual([(c.name, k.key) for c, k in cs.disagreements(classes)], [('ExampleSettings', 'PhobosEx_Warm')])


if __name__ == '__main__':
    unittest.main()


class OurSettings(unittest.TestCase):
    def test_every_scribe_call_in_our_settings_classes_is_read(self):
        import importlib.util
        root = Path(__file__).resolve().parents[1]
        spec = importlib.util.spec_from_file_location('mod_settings', root / 'scripts' / 'mod-settings.py')
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        document = module.defaults_document(root)
        vehicles = document['classes']['Phobos.VehicleSystems.VehicleSystemsSettings']
        self.assertEqual(vehicles['PhobosVehicleSystems_DisplayMode']['default'], 'Plain')
        self.assertEqual(vehicles['PhobosVehicleSystems_DisabledSystems']['default'], '(empty)')
        medical = document['classes']['Phobos.Medical.MedicalSettings']
        self.assertTrue(any(key.startswith('PhobosBurns') for key in medical), 'the parts write into the Medical file')
        sources = [p for p in (root / 'src').rglob('*.cs') if 'obj' not in p.parts and 'bin' not in p.parts]
        settings_files = [p for p in sources if cs.SETTINGS_CLASS.search(p.read_text(encoding='utf-8'))]
        calls = sum(len(cs.SCRIBE.findall(cs.strip_comments(p.read_text(encoding='utf-8')))) for p in settings_files)
        read = sum(len(c.keys) for c in cs.read_settings_classes({p.as_posix(): p.read_text(encoding='utf-8')
                                                                  for p in settings_files}).values())
        self.assertEqual(read, calls, 'a Scribe call in a settings class was not read')
