"""Rehearse scripts/art-run.py against a stand-in ComfyUI: every way a night's run can end. No GPU, no model, no real server."""
import datetime as dt
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
import importlib.util
import json
import os
from pathlib import Path
import struct
import sys
import tempfile
import threading
import time
import unittest
from unittest import mock
import urllib.parse
import uuid
import zlib

ROOT = Path(__file__).resolve().parents[1]
REAL_SLEEP = time.sleep
_spec = importlib.util.spec_from_file_location('art_run_rehearsal', ROOT / 'scripts' / 'art-run.py')
art_run = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(art_run)
art = art_run.art


def tiny_png():
    """A valid 1x1 white PNG."""
    def chunk(kind, data):
        return struct.pack('>I', len(data)) + kind + data + struct.pack('>I', zlib.crc32(kind + data))
    return (b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', 1, 1, 8, 0, 0, 0, 0)) +
            chunk(b'IDAT', zlib.compress(b'\x00\xff')) + chunk(b'IEND', b''))


class FakeComfy:
    """Enough of ComfyUI's API. `behaviour` decides each queued image: done, error or hang."""

    def __init__(self, behaviour=None, installed=('flux1-schnell-fp8.safetensors',)):
        self.behaviour = behaviour or (lambda index: 'done')
        self.installed = list(installed)
        self.prompts = []
        self.calls = []
        self.history = {}
        self.down = False
        fake = self

        class Handler(BaseHTTPRequestHandler):
            def log_message(self, *args):
                pass

            def reply(self, payload, kind='application/json'):
                body = payload if isinstance(payload, bytes) else json.dumps(payload).encode('utf-8')
                self.send_response(200)
                self.send_header('Content-Type', kind)
                self.send_header('Content-Length', str(len(body)))
                self.end_headers()
                self.wfile.write(body)

            def do_GET(self):
                path = urllib.parse.urlparse(self.path).path
                if fake.down:
                    self.send_error(503)
                elif path == '/system_stats':
                    self.reply({'system': {'comfyui_version': 'fake'}, 'devices': []})
                elif path == '/object_info':
                    self.reply(fake.object_info())
                elif path.startswith('/history/'):
                    prompt_id = path.rsplit('/', 1)[1]
                    entry = fake.history.get(prompt_id)
                    self.reply({prompt_id: entry} if entry else {})
                elif path == '/view':
                    self.reply(tiny_png(), 'image/png')
                else:
                    self.send_error(404)

            def do_POST(self):
                length = int(self.headers.get('Content-Length', 0))
                payload = json.loads(self.rfile.read(length) or b'{}')
                fake.calls.append(self.path)
                if fake.down:
                    self.send_error(503)
                elif self.path == '/prompt':
                    self.reply({'prompt_id': fake.queue(payload['prompt'])})
                else:
                    self.reply({})

        self.server = ThreadingHTTPServer(('127.0.0.1', 0), Handler)
        self.url = f'http://127.0.0.1:{self.server.server_address[1]}'
        threading.Thread(target=self.server.serve_forever, daemon=True).start()

    def object_info(self):
        loader = {'input': {'required': {'ckpt_name': [self.installed, {}]}}}
        return {'CheckpointLoaderSimple': loader, 'CLIPTextEncode': {}, 'EmptySD3LatentImage': {}, 'KSampler': {}, 'VAEDecode': {}, 'SaveImage': {}}

    def queue(self, graph):
        index = len(self.prompts)
        self.prompts.append(graph)
        prompt_id = str(uuid.uuid4())
        action = self.behaviour(index)
        if action == 'done':
            self.history[prompt_id] = {'status': {'status_str': 'success'}, 'outputs': {'7': {'images': [{'filename': 'x.png', 'subfolder': '', 'type': 'output'}]}}}
        elif action == 'error':
            self.history[prompt_id] = {'status': {'status_str': 'error', 'messages': ['out of memory']}, 'outputs': {}}
        return prompt_id  # 'hang' never reaches the history, as an image still rendering.

    def close(self):
        self.server.shutdown()
        self.server.server_close()


class RehearsalCase(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.work = Path(self.temp.name)
        (self.work / 'set' / 'requests').mkdir(parents=True)
        self.requests = self.work / 'set' / 'requests' / 'night.json'
        self.generated = self.work / 'set' / 'generated'
        self.processes = {'explorer.exe'}
        self.write_requests(seeds=[1, 2, 3])

    def write_requests(self, seeds, model=None):
        request = {'name': 'tank', 'prompt': 'a tank', 'width': 64, 'height': 64, 'seeds': seeds}
        if model:
            request['model'] = model
        self.requests.write_text(json.dumps([request]), encoding='utf-8')

    def run_night(self, fake, stop_at='23:59', now_after=None, extra=()):
        """Run the real main(), with only the clock, the process list, sleeps and the wake lock replaced."""
        self.addCleanup(fake.close)
        patches = [
            mock.patch.object(art, 'SERVER', fake.url),
            mock.patch.object(art_run, 'read_processes', lambda: self.processes),
            mock.patch.object(art_run, 'POLL_SECONDS', 0),
            mock.patch.object(art_run, 'stay_awake', self.note_awake),
            mock.patch.object(art.time, 'sleep', lambda _: REAL_SLEEP(0.005)),
        ]
        self.awake = []
        for patch in patches:
            patch.start()
            self.addCleanup(patch.stop)
        argv = ['generate', str(self.requests), '--stop-at', stop_at, '--comfy', str(self.work / 'no-comfy-here'), *extra]
        with mock.patch('sys.stdout', new=open(os.devnull, 'w', encoding='utf-8')) as quiet:
            self.addCleanup(quiet.close)
            return art_run.main(argv)

    def note_awake(self, on):
        self.awake.append(on)

    def report(self):
        reports = sorted(self.generated.glob('run-*.md'))
        self.assertEqual(len(reports), 1, 'exactly one report is written, however the run ends')
        return reports[0].read_text(encoding='utf-8')

    def images(self):
        return sorted(p.name for p in (self.generated / 'tank').glob('*.png'))


class HappyPathTests(RehearsalCase):
    def test_a_full_run_makes_images_with_records_and_finishes_cleanly(self):
        fake = FakeComfy()
        self.assertEqual(self.run_night(fake), 0)
        self.assertEqual(self.images(), ['tank-1.png', 'tank-2.png', 'tank-3.png'])
        record = json.loads((self.generated / 'tank' / 'tank-1.json').read_text(encoding='utf-8'))
        self.assertEqual(record['seed'], 1)
        self.assertEqual(record['engine'], 'schnell')
        self.assertIn('seconds', record)
        report = self.report()
        self.assertIn('## Time per image', report)
        self.assertIn('tank (schnell, 4 steps): first image', report)
        self.assertIn('for 2 more', report)
        self.assertIn('the work finished', report)
        self.assertIn('**Images made:** 3', report)
        self.assertIn('/free', fake.calls, 'the graphics memory is freed')
        self.assertEqual(self.awake, [True, False], 'the wake request is released')

    def test_a_second_run_makes_nothing_new(self):
        self.run_night(FakeComfy())
        for report in self.generated.glob('run-*.md'):
            report.unlink()
        fake = FakeComfy()
        self.assertEqual(self.run_night(fake), 0)
        self.assertEqual(fake.prompts, [])
        self.assertIn('**Already there, skipped:** 3', self.report())

    def test_the_review_page_is_written_even_though_the_image_tools_are_missing(self):
        self.run_night(FakeComfy())
        self.assertTrue((self.generated / 'review.html').is_file())
        self.assertIn('Review tools that failed', self.report())


class SafeguardTests(RehearsalCase):
    def test_the_game_already_running_stops_it_before_anything_starts(self):
        self.processes = {'rimworld.exe'}
        fake = FakeComfy()
        self.assertEqual(self.run_night(fake), 3)
        self.assertEqual(fake.prompts, [])
        self.assertEqual(fake.calls, [], 'not even a free call: the server was never involved')

    def test_the_game_starting_mid_image_interrupts_it_and_frees_the_card(self):
        def behaviour(index):
            if index == 1:
                self.processes = {'rimworld.exe'}
                return 'hang'
            return 'done'
        fake = FakeComfy(behaviour)
        self.assertEqual(self.run_night(fake), 0)
        self.assertEqual(self.images(), ['tank-1.png'])
        self.assertIn('rimworld.exe is running', self.report())
        for call in ('/queue', '/interrupt', '/free'):
            self.assertIn(call, fake.calls)

    def test_the_stop_time_passing_mid_image_ends_the_run(self):
        real_reason = art_run.safeguards.stop_reason
        fake = FakeComfy(lambda index: 'hang' if index == 0 else 'done')

        def later_once_queued(now, stop_at, *args, **kwargs):
            return real_reason(stop_at if fake.prompts else now, stop_at, *args, **kwargs)
        with mock.patch.object(art_run.safeguards, 'stop_reason', later_once_queued):
            self.assertEqual(self.run_night(fake), 0)
        self.assertEqual(self.images(), [])
        self.assertIn('stop time', self.report())
        self.assertIn('/interrupt', fake.calls)

    def test_a_full_disk_stops_it_before_anything_starts(self):
        with mock.patch.object(art_run, 'free_gb', lambda paths: {'C:\\': 1.0}):
            self.assertEqual(self.run_night(FakeComfy()), 3)
        self.assertEqual(list(self.generated.glob('*')), [])

    def test_an_unreadable_process_list_stops_it(self):
        self.processes = None
        self.assertEqual(self.run_night(FakeComfy()), 3)

    def test_a_stop_time_is_required(self):
        with mock.patch('sys.stderr', new=open(os.devnull, 'w', encoding='utf-8')) as quiet:
            self.addCleanup(quiet.close)
            with self.assertRaises(SystemExit):
                art_run.main(['generate', str(self.requests)])

    def test_a_dry_run_touches_nothing(self):
        with mock.patch('sys.stdout', new=open(os.devnull, 'w', encoding='utf-8')) as quiet:
            self.addCleanup(quiet.close)
            self.assertEqual(art_run.main(['generate', str(self.requests), '--dry-run']), 0)
        self.assertFalse(self.generated.exists())


class FailureTests(RehearsalCase):
    def test_one_failed_image_is_recorded_and_the_rest_still_made(self):
        fake = FakeComfy(lambda index: 'error' if index == 1 else 'done')
        self.assertEqual(self.run_night(fake), 0)
        self.assertEqual(self.images(), ['tank-1.png', 'tank-3.png'])
        report = self.report()
        self.assertIn('tank-2.png', report)
        self.assertIn('out of memory', report)
        self.assertIn('the work finished', report)

    def test_three_failures_in_a_row_end_the_run(self):
        self.write_requests(seeds=[1, 2, 3, 4, 5])
        fake = FakeComfy(lambda index: 'error')
        self.assertEqual(self.run_night(fake), 1)
        self.assertEqual(len(fake.prompts), 3)
        self.assertIn('3 images failed in a row', self.report())
        self.assertIn('/free', fake.calls)

    def test_a_missing_model_is_caught_before_any_image_is_tried(self):
        self.write_requests(seeds=[1], model='gone.safetensors')
        fake = FakeComfy()
        self.assertEqual(self.run_night(fake), 1)
        self.assertEqual(fake.prompts, [])
        report = self.report()
        self.assertIn('gone.safetensors', report)
        self.assertIn('failed their check', report)

    def test_an_image_that_never_finishes_is_cancelled_before_the_next(self):
        self.write_requests(seeds=[1, 2])
        request = json.loads(self.requests.read_text(encoding='utf-8'))
        request[0]['timeout'] = 0.05
        self.requests.write_text(json.dumps(request), encoding='utf-8')
        fake = FakeComfy(lambda index: 'hang' if index == 0 else 'done')
        self.assertEqual(self.run_night(fake), 0)
        self.assertEqual(self.images(), ['tank-2.png'])
        self.assertIn('/interrupt', fake.calls, 'otherwise the stuck image blocks every later one')

    def test_the_server_vanishing_still_gives_a_report(self):
        fake = FakeComfy(lambda index: 'hang')

        def vanish_once_queued(*args, **kwargs):
            if fake.prompts:
                fake.close()
            return None
        fake_close = fake.close
        closed = []

        def close_once():
            if not closed:
                closed.append(True)
                fake_close()
        fake.close = close_once
        with mock.patch.object(art_run.safeguards, 'stop_reason', vanish_once_queued):
            self.assertEqual(self.run_night(fake), 1)
        self.assertIn('failed in a row', self.report())


class RestartTests(RehearsalCase):
    def bring_up(self, fake):
        """Stand in for starting ComfyUI: the server answers again, and the run holds a process it must stop at the end."""
        process = mock.Mock()
        self.started = []

        def start(_comfy):
            fake.down = False
            self.started.append(process)
            return process
        patch = mock.patch.object(art, 'start_server', start)
        patch.start()
        self.addCleanup(patch.stop)
        return process

    def test_a_crash_gets_a_fresh_server_and_loses_no_image(self):
        def behaviour(index):
            if index == 1:
                fake.down = True
                return 'hang'
            return 'done'
        fake = FakeComfy(behaviour)
        self.bring_up(fake)
        self.assertEqual(self.run_night(fake), 0)
        self.assertEqual(self.images(), ['tank-1.png', 'tank-2.png', 'tank-3.png'])
        self.assertEqual(len(self.started), 1)
        report = self.report()
        self.assertIn('## ComfyUI restarted', report)
        self.assertNotIn('## Failures', report)

    def test_a_server_that_keeps_dying_ends_the_run_with_a_report(self):
        def behaviour(_index):
            fake.down = True
            return 'hang'
        fake = FakeComfy(behaviour)
        self.bring_up(fake)
        self.write_requests(seeds=[1, 2, 3, 4, 5])
        self.assertEqual(self.run_night(fake), 1)
        self.assertEqual(len(fake.prompts), 3, 'three tries, then it stops rather than looping')
        self.assertIn('failed in a row', self.report())
        self.assertEqual(self.images(), [])

    def test_a_second_failure_in_a_row_restarts_a_server_this_run_started(self):
        fake = FakeComfy(lambda index: 'error' if index in (1, 2) else 'done')
        fake.down = True
        process = self.bring_up(fake)
        self.assertEqual(self.run_night(fake), 0)
        self.assertEqual(self.images(), ['tank-1.png', 'tank-3.png'], 'the first failure moved on; the second was tried again')
        self.assertEqual(len(self.started), 2, 'the first start, then the restart')
        self.assertIn('tank-2.png', self.report())
        self.assertTrue(process.terminate.called)

    def test_a_server_this_run_did_not_start_is_never_replaced(self):
        started = []
        with mock.patch.object(art, 'start_server', lambda comfy: started.append(comfy)):
            self.assertEqual(self.run_night(FakeComfy(lambda index: 'error')), 1)
        self.assertEqual(started, [])


class NearCopyTests(RehearsalCase):
    def write_with_references(self, reference='refs/vitals.png'):
        request = {'name': 'tank', 'prompt': 'a tank', 'width': 64, 'height': 64, 'seeds': [1], 'references': [str(self.work / reference)]}
        (self.work / 'refs').mkdir(exist_ok=True)
        (self.work / reference).write_bytes(tiny_png())
        self.requests.write_text(json.dumps([request]), encoding='utf-8')

    def test_sources_are_the_reference_folders_plus_any_named(self):
        requests = [{'references': ['a/one.png', 'a/two.png']}, {'references': ['b/three.png']}, {}]
        sources = art_run.near_copy_sources(requests, ['c'])
        self.assertEqual(sources, [str(art_run.ROOT / 'a'), str(art_run.ROOT / 'b'), 'c'])

    def test_a_processor_run_hides_every_graphics_card(self):
        # 12 Nov 2026: an empty CUDA_VISIBLE_DEVICES left the card visible on Windows; -1 hides them all.
        env = art_run.cpu_env(8)
        self.assertEqual(env['CUDA_VISIBLE_DEVICES'], '-1')
        self.assertEqual(env['OMP_NUM_THREADS'], '8')

    def test_our_own_art_is_not_a_source(self):
        requests = [{'references': ['assets/set/references/mine.png', 'mods/M/Textures/mine.png', '.local/art/picks-7nov/pick.png',
                                    '.local/references/game.png']}]
        self.assertEqual(art_run.near_copy_sources(requests), [str(art_run.ROOT / '.local' / 'references')])

    def post_process(self, flagged, writes=True, steps=None):
        generated = self.work / 'gen'
        for batch in ('tank', 'older-batch'):
            (generated / batch).mkdir(parents=True)
            (generated / batch / f'{batch}-1.png').write_bytes(tiny_png())

        def fake_run(step, **_kwargs):
            if Path(step[2]).name == 'near_copy.py':
                if steps is not None:
                    steps.append(step)
                if writes:
                    (generated / 'near-copy.json').write_text(json.dumps({'flagged': flagged}), encoding='utf-8')
            return mock.Mock(returncode=0 if not flagged else 1, stderr='a traceback')
        with mock.patch.object(art_run.subprocess, 'run', fake_run):
            return art_run.post_process('comfy', generated, ['tank'], ['refs'])

    def test_the_check_reads_only_this_batchs_folders(self):
        # 11 Nov 2026: given the whole generated folder, a run of four flagged 38 pictures from earlier batches.
        steps = []
        self.post_process([], steps=steps)
        self.assertEqual(len(steps), 1)
        step = steps[0]
        self.assertEqual(step[3:5], ['--only', 'tank'])
        self.assertNotIn('older-batch', step)

    def test_a_flag_is_a_finding_and_not_a_broken_tool(self):
        entry = {'output': 'tank-1.png', 'source': 'vitals.png', 'ssim': 0.9, 'hash_distance': 3}
        problems, lines = self.post_process([entry])
        self.assertEqual([p for p in problems if 'near_copy' in p], [])
        self.assertIn('do not pick', lines[0])
        self.assertIn('tank-1.png: too close to vitals.png', lines[1])

    def test_a_clean_batch_says_so(self):
        _, lines = self.post_process([])
        self.assertIn('Nothing flagged', lines[0])

    def test_a_check_that_did_not_run_is_never_read_as_clean(self):
        problems, lines = self.post_process([], writes=False)
        self.assertTrue(any('near_copy' in p for p in problems))
        self.assertIn('did not run', lines[0])

    def test_a_real_night_reports_the_check_when_its_tool_is_missing(self):
        self.write_with_references()
        with mock.patch.object(art, 'stage_init', lambda comfy, ref, name: ('x.png', Path(ref))):
            self.run_night(FakeComfy())
        self.assertIn('did not run', self.report())

    def test_the_review_page_warns_on_a_flagged_image(self):
        self.run_night(FakeComfy())
        (self.generated / 'near-copy.json').write_text(json.dumps({'flagged': [{'output': 'tank-1.png', 'source': 'vitals.png'}]}), encoding='utf-8')
        sys.path.insert(0, str(art_run.ROOT / 'scripts' / 'art'))
        import review_page
        with mock.patch('sys.stdout', new=open(os.devnull, 'w', encoding='utf-8')) as quiet:
            self.addCleanup(quiet.close)
            review_page.build(self.generated)
        page = (self.generated / 'review.html').read_text(encoding='utf-8')
        self.assertIn('Too close to vitals.png: do not pick', page)
        self.assertEqual(page.count('do not pick'), 1)

    def build_review_page(self):
        sys.path.insert(0, str(art_run.ROOT / 'scripts' / 'art'))
        import review_page
        with mock.patch('sys.stdout', new=open(os.devnull, 'w', encoding='utf-8')) as quiet:
            self.addCleanup(quiet.close)
            review_page.build(self.generated)
        return (self.generated / 'review.html').read_text(encoding='utf-8')

    def test_the_review_page_leads_a_subject_with_what_the_mod_shows_now(self):
        self.run_night(FakeComfy())
        (self.generated.parent / 'baselines.json').write_text(
            json.dumps({'tank': {'image': 'sprites/tank.png', 'label': 'In the mod now'}}), encoding='utf-8')
        self.addCleanup((self.generated.parent / 'baselines.json').unlink)
        page = self.build_review_page()
        self.assertIn('src="../sprites/tank.png"', page)
        self.assertLess(page.index('In the mod now'), page.index('tank/tank'))
        self.assertIn('(3)</span>', page, 'the stand-in is not counted as a candidate')

    def test_the_review_page_has_no_comparison_without_a_baseline(self):
        self.run_night(FakeComfy())
        page = self.build_review_page()
        self.assertNotIn('<div class="card baseline">', page)
        self.assertNotIn('<img class="small"', page)


class NightTests(RehearsalCase):
    def plan(self, steps):
        path = self.work / 'night.json'
        path.write_text(json.dumps(steps), encoding='utf-8')
        return path

    def run_plan(self, steps, codes=None):
        path = self.plan(steps)
        self.commands = []
        codes = list(codes or [])

        def fake_step(command):
            self.commands.append(command)
            return (codes.pop(0) if codes else 0), 'a-report.md'
        with mock.patch.object(art_run, 'run_step', fake_step), mock.patch.object(art_run, 'stay_awake', lambda on: None), \
                mock.patch('sys.stdout', new=open(os.devnull, 'w', encoding='utf-8')) as quiet:
            self.addCleanup(quiet.close)
            code = art_run.main(['night', str(path), '--stop-at', '23:59'])
        summaries = list(self.work.glob('night-*.md'))
        return code, summaries[0].read_text(encoding='utf-8') if summaries else None

    def test_steps_run_in_order_under_one_stop_time(self):
        code, _ = self.run_plan([{'generate': 'a.json'}, {'run': ['scripts/art/lora_experiment.py', 'assemble']}, {'train': 'c.yaml'}])
        self.assertEqual(code, 0)
        self.assertEqual([Path(c[3]).name if 'art-run' in c[1] else Path(c[1]).name for c in self.commands], ['a.json', 'lora_experiment.py', 'c.yaml'])
        stops = {c[c.index('--stop-at') + 1] for c in self.commands if '--stop-at' in c}
        self.assertEqual(len(stops), 1, 'every step gets the same end')

    def test_a_failed_step_does_not_end_the_night_and_the_summary_says_what_happened(self):
        code, summary = self.run_plan([{'generate': 'a.json'}, {'generate': 'b.json'}, {'generate': 'c.json'}], codes=[1, 3, 0])
        self.assertEqual(code, 0)
        self.assertEqual(len(self.commands), 3)
        self.assertIn('failed, exit code 1', summary)
        self.assertIn('a safeguard refused to start it', summary)
        self.assertIn('a-report.md', summary)

    def test_extra_near_copy_folders_reach_the_step(self):
        self.run_plan([{'generate': 'a.json', 'near_copy_against': ['dataset']}])
        self.assertIn('dataset', self.commands[0])

    def test_steps_past_the_stop_time_are_skipped_and_reported(self):
        with mock.patch.object(art_run.safeguards, 'parse_stop_at', lambda text, now: now):
            _, summary = self.run_plan([{'generate': 'a.json'}])
        self.assertEqual(self.commands, [])
        self.assertIn('not started: the stop time was reached', summary)

    def test_a_bad_plan_is_refused_before_anything_runs(self):
        bad = [[], {}, [{'generate': 'a', 'train': 'b'}], [{'paint': 'a'}], [{'run': ['../evil.py']}], [{'run': ['C:/elsewhere/tool.py']}],
               [{'run': ['scripts/not-a-real-tool.py']}], [{'train': 'a', 'near_copy_against': ['b']}], [{'generate': 'a', 'stop': 'b'}]]
        for steps in bad:
            self.assertTrue(art_run.night_problems(steps), steps)
        self.assertEqual(art_run.night_problems([{'generate': 'a', 'near_copy_against': ['b']}, {'train': 'c'}, {'run': ['scripts/art/lora_experiment.py', 'x']}]), [])
        code, summary = self.run_plan([{'paint': 'a'}])
        self.assertEqual(code, 2)
        self.assertEqual(self.commands, [])
        self.assertIsNone(summary)

    def test_a_dry_run_notices_a_file_an_earlier_step_must_make(self):
        path = self.plan([{'generate': str(self.work / 'made-later.json')}])
        with mock.patch('sys.stdout', new=open(os.devnull, 'w', encoding='utf-8')) as quiet:
            self.addCleanup(quiet.close)
            self.assertEqual(art_run.main(['night', str(path), '--dry-run']), 0)
        self.assertEqual(list(self.work.glob('night-*.md')), [])


if __name__ == '__main__':
    unittest.main()
