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
                if path == '/system_stats':
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
                if self.path == '/prompt':
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
        report = self.report()
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


if __name__ == '__main__':
    unittest.main()
