"""scripts/art-audit.py and config/art-audit.json: the real config's patterns on hand-written records, and no stale acceptances."""
import importlib.util
import io
import json
import os
from pathlib import Path
import shutil
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]


def load(name, relative):
    spec = importlib.util.spec_from_file_location(name, os.path.join(ROOT, relative))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


CALLER = load('art_audit_script', 'scripts/art-audit.py')
CALLER._modkit.ensure()
from phobos_modkit import artaudit  # noqa: E402  (after the kit is on the path)

CONFIG = json.loads((ROOT / 'config' / 'art-audit.json').read_text(encoding='utf-8'))
TEXTURES = 'mods/PhobosExample/Common/Textures/Phobos/Example'

# Records written in the forms this repository's records take, by hand.
RECORDS = {
    'Generated': '**AI-generated, then picked by the owner.** The generator was given our own stand-in drawing '
                 '`Generated-large.png`, drawn in code, as a reference.\n',
    'Replaced': 'Made with FLUX.2 Klein through `scripts/art-run.py`: **AI-generated** (one attempt, beside the stand-in it '
                'replaces, drawn with Pillow).\n',
    'Drawn': '**A stand-in, drawn in code** by `assets/x/draw_icons.py` (Pillow). Generated art can replace it.\n',
    'Placeholder': '**Drawn in code, no image generator**, by `make_textures.py`. A **placeholder**, meant to stay drawn by script.\n',
    'Whip': '**AI-generated, then picked by the owner.** Made with ComfyUI.\n\n**A stand-in:** a mast used for the whip.\n',
    'Cut': '**AI-generated, cut by script.** Cut from an overlay made with OpenAI gpt-image-2 through the image API.\n',
}


class Fixture:
    def __init__(self):
        self.folder = tempfile.TemporaryDirectory()
        self.root = Path(self.folder.name)
        textures = self.root / TEXTURES
        textures.mkdir(parents=True)
        for name, record in RECORDS.items():
            (textures / f'{name}.png').write_bytes(b'')
            (textures / f'{name}.record.md').write_text(record, encoding='utf-8')
        (textures / 'Orphan.png').write_bytes(b'')
        defs = self.root / 'mods/PhobosExample/Common/Defs'
        defs.mkdir(parents=True)
        (defs / 'Things.xml').write_text(
            '<Defs><ThingDef><defName>Phobos_TentLamp</defName><graphicData>'
            '<texPath>Things/Building/Furniture/LampStanding</texPath></graphicData></ThingDef></Defs>', encoding='utf-8')
        (self.root / 'config').mkdir()
        shutil.copy(ROOT / 'config' / 'art-audit.json', self.root / 'config' / 'art-audit.json')

    def close(self):
        self.folder.cleanup()


class RealConfig(unittest.TestCase):
    def test_the_config_reads(self):
        self.assertEqual(CONFIG['game'], 'rimworld')
        self.assertTrue(artaudit.kinds_of(CONFIG))
        self.assertTrue(artaudit.accepted_of(CONFIG))

    def test_records_are_classed_by_their_first_statement(self):
        fixture = Fixture()
        try:
            result = artaudit.audit(fixture.root, artaudit.load_config(fixture.root))
        finally:
            fixture.close()
        kinds = {Path(i.path).stem: i.kind for i in result.images}
        self.assertEqual(kinds, {'Generated': 'generated', 'Replaced': 'generated', 'Drawn': 'code-drawn',
                                 'Placeholder': 'code-drawn', 'Whip': 'generated', 'Cut': 'generated',
                                 'Orphan': artaudit.NO_RECORD})
        called = {Path(i.path).stem for i in result.images if i.stand_in}
        self.assertEqual(called, {'Drawn', 'Placeholder', 'Whip'})
        lamp = [u for u in result.uses if u.owner == 'Phobos_TentLamp']
        self.assertEqual([u.status for u in lamp], [artaudit.NOT_OURS])

    def test_every_acceptance_still_matches_something(self):
        result = artaudit.audit(ROOT, CONFIG)
        reasons = {i.accepted for i in result.images} | {u.accepted for u in result.uses}
        stale = [a.reason for a in artaudit.accepted_of(CONFIG) if a.reason not in reasons]
        self.assertEqual(stale, [], 'an accepted entry no image or use matches: remove it from config/art-audit.json')


class Caller(unittest.TestCase):
    def test_report_by_default_and_check(self):
        fixture = Fixture()
        try:
            out = io.StringIO()
            self.assertEqual(CALLER.main(['--root', str(fixture.root)], out=out), 0)
            self.assertIn('# Art audit: 7 images; 2 drawn in code, 1 without a record, 1 borrowed pictures open', out.getvalue())
            out = io.StringIO()
            self.assertEqual(CALLER.main(['check', '--root', str(fixture.root)], out=out), 1)
            self.assertIn('Orphan.png', out.getvalue())
        finally:
            fixture.close()


if __name__ == '__main__':
    unittest.main()
