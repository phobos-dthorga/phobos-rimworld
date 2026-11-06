"""Checks of the art archive branches, in a throwaway git repository: what goes in, what is refused, and that a branch only grows."""
import json
import os
import shutil
import subprocess
import sys
import tempfile
import unittest

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, 'scripts', 'art'))
import archive_branch  # noqa: E402


def git(repo, *args):
    return subprocess.run(['git', *args], cwd=repo, capture_output=True, text=True, check=True).stdout.strip()


class ArchiveBranchTests(unittest.TestCase):
    def setUp(self):
        self.repo = tempfile.mkdtemp(prefix='archive-test-')
        git(self.repo, 'init', '-q', '-b', 'main')
        git(self.repo, 'config', 'user.email', 'test@example.invalid')
        git(self.repo, 'config', 'user.name', 'Test')
        self.write('README.md', 'main\n')
        git(self.repo, 'add', 'README.md')
        git(self.repo, 'commit', '-q', '-m', 'main')

    def tearDown(self):
        shutil.rmtree(self.repo, ignore_errors=True)

    def write(self, name, text):
        path = os.path.join(self.repo, name)
        os.makedirs(os.path.dirname(path) or self.repo, exist_ok=True)
        with open(path, 'w', encoding='utf-8') as handle:
            handle.write(text)

    def files(self, branch):
        return sorted(git(self.repo, 'ls-tree', '-r', '--name-only', branch).splitlines())

    def test_a_new_branch_has_no_history_and_only_the_archived_files(self):
        self.write('gen/a.png', 'image')
        archive_branch.archive(self.repo, 'art-masters', [{'source': 'gen/a.png', 'dest': 'medical/a.png'}], 'first')
        self.assertEqual(self.files('art-masters'), ['medical/a.png'])
        self.assertEqual(git(self.repo, 'rev-list', '--count', 'art-masters'), '1')
        self.assertEqual(git(self.repo, 'rev-parse', '--abbrev-ref', 'HEAD'), 'main')
        self.assertEqual(git(self.repo, 'status', '--porcelain'), '?? gen/')

    def test_a_second_run_adds_on_top_and_keeps_what_was_there(self):
        self.write('gen/a.png', 'a')
        self.write('gen/b.png', 'b')
        archive_branch.archive(self.repo, 'art-masters', [{'source': 'gen/a.png', 'dest': 'a.png'}], 'first')
        archive_branch.archive(self.repo, 'art-masters', [{'source': 'gen/b.png', 'dest': 'b.png'}], 'second')
        self.assertEqual(self.files('art-masters'), ['a.png', 'b.png'])
        self.assertEqual(git(self.repo, 'rev-list', '--count', 'art-masters'), '2')

    def test_nothing_new_makes_no_commit(self):
        self.write('gen/a.png', 'a')
        first = archive_branch.archive(self.repo, 'art-masters', [{'source': 'gen/a.png', 'dest': 'a.png'}], 'first')
        again = archive_branch.archive(self.repo, 'art-masters', [{'source': 'gen/a.png', 'dest': 'a.png'}], 'again')
        self.assertEqual(first, again)

    def test_machine_paths_in_sidecars_are_replaced(self):
        self.write('gen/a.json', json.dumps({'request_file': 'C:/AI/lora-experiments/vanilla/requests/compare.json',
                                             'references': [{'image': '.local/references/vanilla-medical/X.png'}], 'seed': 11}))
        archive_branch.archive(self.repo, 'art-masters', [{'source': 'gen/a.json', 'dest': 'a.json'}], 'first')
        published = json.loads(git(self.repo, 'show', 'art-masters:a.json'))
        self.assertEqual(published['request_file'], archive_branch.PRIVATE_NOTE)
        self.assertEqual(published['references'][0]['image'], '.local/references/vanilla-medical/X.png')
        self.assertEqual(published['seed'], 11)

    def test_game_art_the_private_model_and_model_files_are_refused(self):
        for source in ('.local/references/vanilla-medical/X.png', 'C:/AI/lora-experiments/vanilla/dataset/x.png', 'models/x.safetensors'):
            with self.subTest(source=source):
                self.assertTrue(archive_branch.refused(source))
        self.assertFalse(archive_branch.refused('.local/art/picks-7nov/oxygen-mask-104.png'))
        self.write('.local/references/vanilla-medical/X.png', 'game art')
        with self.assertRaises(SystemExit):
            archive_branch.archive(self.repo, 'art-references',
                                   [{'source': '.local/references/vanilla-medical/X.png', 'dest': 'X.png'}], 'no')
        self.assertNotIn('art-references', git(self.repo, 'branch', '--list'))


if __name__ == '__main__':
    unittest.main()
