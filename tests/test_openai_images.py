"""Offline checks of the paid OpenAI image engine (scripts/art/openai_images.py) and how the art scripts use it.

Nothing here reaches OpenAI: every call goes to a stand-in, so no key is read and nothing is spent."""
import base64
import importlib.util
import io
import json
import os
from pathlib import Path
import sys
import tempfile
import unittest
from unittest import mock
import urllib.error

ROOT = Path(__file__).resolve().parents[1]
_spec = importlib.util.spec_from_file_location('art_run_paid', ROOT / 'scripts' / 'art-run.py')
art_run = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(art_run)
art = art_run.art
paid = art.paid

PNG = b'\x89PNG\r\n\x1a\nfake'
USAGE = {'input_tokens': 120, 'input_tokens_details': {'text_tokens': 100, 'image_tokens': 20}, 'output_tokens': 1000, 'total_tokens': 1120}


def good_request(**changes):
    req = {'name': 'truck', 'engine': 'openai', 'prompt': 'a truck seen from directly above', 'width': 1024, 'height': 1536,
           'seeds': [1, 2]}
    req.update(changes)
    return req


class Reply:
    def __init__(self, payload):
        self.payload = payload

    def __enter__(self):
        return self

    def __exit__(self, *exc):
        return False

    def read(self):
        return json.dumps(self.payload).encode('utf-8')


def http_error(status, code, message='refused'):
    body = json.dumps({'error': {'message': message, 'code': code}}).encode('utf-8')
    return urllib.error.HTTPError('https://api.openai.com/v1/images/generations', status, 'error', {}, io.BytesIO(body))


class FakeOpenAI:
    """Answers in order from a list: a dict is a reply, an exception is raised."""

    def __init__(self, *answers):
        self.answers = list(answers)
        self.requests = []

    def __call__(self, request, timeout=None):
        self.requests.append(request)
        answer = self.answers.pop(0)
        if isinstance(answer, BaseException):
            raise answer
        return Reply(answer)


def image_reply(usage=USAGE):
    return {'data': [{'b64_json': base64.b64encode(PNG).decode('ascii')}], 'usage': usage, 'quality': 'medium', 'size': '1024x1536'}


class RequestCheckTests(unittest.TestCase):
    def test_a_good_request_has_no_problems(self):
        self.assertEqual(paid.request_problems(good_request()), [])

    def test_sizes_the_api_refuses_are_caught_first(self):
        self.assertTrue(any('divide by 16' in p for p in paid.request_problems(good_request(width=1000))))
        self.assertTrue(any('1:3' in p for p in paid.request_problems(good_request(width=320, height=1536))))
        self.assertTrue(any('largest' in p for p in paid.request_problems(good_request(width=4096, height=2048))))

    def test_settings_and_references_are_checked(self):
        self.assertTrue(paid.request_problems(good_request(quality='ultra')))
        self.assertTrue(paid.request_problems(good_request(background='green')))
        self.assertTrue(paid.request_problems(good_request(references=['a.png'] * 17)))
        self.assertTrue(paid.request_problems(good_request(references=['a.dds'])))

    def test_local_engine_settings_are_refused_rather_than_ignored(self):
        problems = paid.request_problems(good_request(lora='v1.safetensors', steps=20))
        self.assertEqual(len(problems), 2)


class CostTests(unittest.TestCase):
    def test_cost_comes_from_the_token_counts(self):
        expected = (100 * 5 + 20 * 8 + 1000 * 30) / 1e6
        self.assertAlmostEqual(paid.cost_usd(USAGE), expected)

    def test_input_without_a_split_is_charged_at_the_dearer_image_rate(self):
        self.assertAlmostEqual(paid.cost_usd({'input_tokens': 1000, 'output_tokens': 0}), 1000 * 8 / 1e6)

    def test_no_usage_gives_no_cost(self):
        self.assertIsNone(paid.cost_usd(None))

    def test_the_estimate_grows_with_size_and_references(self):
        base = paid.estimate_usd(good_request())
        self.assertAlmostEqual(base, paid.ESTIMATE_PER_IMAGE['medium'])
        self.assertGreater(paid.estimate_usd(good_request(width=2048, height=2048)), base)
        self.assertGreater(paid.estimate_usd(good_request(references=['a.png'])), base)

    def test_the_cap_refuses_an_image_that_could_pass_it(self):
        budget = paid.Budget(0.10)
        self.assertIsNone(budget.refusal(0.06))
        budget.add(0.05)
        self.assertIn('cap', budget.refusal(0.06))

    def test_a_paid_run_needs_a_cap(self):
        with self.assertRaises(ValueError):
            paid.Budget(None)
        with self.assertRaises(ValueError):
            paid.Budget(0)


class CallTests(unittest.TestCase):
    def test_a_generation_sends_json_and_returns_the_image_and_its_cost(self):
        fake = FakeOpenAI(image_reply())
        data, info = paid.make_image(good_request(), [], 'sk-test-key', opener=fake)
        self.assertEqual(data, PNG)
        request = fake.requests[0]
        self.assertTrue(request.full_url.endswith('/images/generations'))
        body = json.loads(request.data)
        self.assertEqual(body['model'], 'gpt-image-2')
        self.assertEqual(body['size'], '1024x1536')
        self.assertEqual(body['quality'], 'medium')
        self.assertNotIn('response_format', body)
        self.assertEqual(request.get_header('Authorization'), 'Bearer sk-test-key')
        self.assertAlmostEqual(info['cost_usd'], round(paid.cost_usd(USAGE), 4))

    def test_references_make_an_edit_with_each_image_in_order(self):
        fake = FakeOpenAI(image_reply())
        paid.make_image(good_request(), [('north.png', b'AAA'), ('turned.png', b'BBB')], 'sk-test-key', opener=fake)
        request = fake.requests[0]
        self.assertTrue(request.full_url.endswith('/images/edits'))
        self.assertIn('multipart/form-data; boundary=', request.get_header('Content-type'))
        body = request.data
        self.assertEqual(body.count(b'name="image[]"'), 2)
        self.assertLess(body.index(b'north.png'), body.index(b'turned.png'))
        self.assertIn(b'name="model"\r\n\r\ngpt-image-2', body)

    def test_a_rate_limit_is_waited_out(self):
        fake = FakeOpenAI(http_error(429, 'rate_limit_exceeded'), image_reply())
        waits = []
        data, _ = paid.make_image(good_request(), [], 'k', opener=fake, sleep=waits.append)
        self.assertEqual(data, PNG)
        self.assertEqual(waits, [paid.RETRY_DELAYS_SECONDS[0]])

    def test_no_credit_is_fatal_and_never_retried(self):
        fake = FakeOpenAI(http_error(429, 'insufficient_quota'))
        with self.assertRaises(paid.ApiError) as caught:
            paid.make_image(good_request(), [], 'k', opener=fake, sleep=lambda _: None)
        self.assertTrue(caught.exception.fatal)
        self.assertEqual(len(fake.requests), 1)

    def test_a_refused_prompt_is_not_fatal(self):
        fake = FakeOpenAI(http_error(400, 'moderation_blocked'))
        with self.assertRaises(paid.ApiError) as caught:
            paid.make_image(good_request(), [], 'k', opener=fake)
        self.assertFalse(caught.exception.fatal)

    def test_a_timeout_is_not_retried_because_it_may_have_been_billed(self):
        fake = FakeOpenAI(TimeoutError('timed out'), image_reply())
        with self.assertRaises(paid.ApiError):
            paid.make_image(good_request(), [], 'k', opener=fake)
        self.assertEqual(len(fake.requests), 1)

    def test_a_key_in_an_error_is_hidden(self):
        fake = FakeOpenAI(http_error(401, 'invalid_api_key', 'Incorrect API key provided: sk-proj-abcdef123456'))
        with self.assertRaises(paid.ApiError) as caught:
            paid.make_image(good_request(), [], 'k', opener=fake)
        self.assertNotIn('abcdef123456', str(caught.exception))
        self.assertTrue(caught.exception.fatal)


class PaidRunCase(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.work = Path(self.temp.name)
        (self.work / 'set' / 'requests').mkdir(parents=True)
        self.requests = self.work / 'set' / 'requests' / 'paid.json'
        self.generated = self.work / 'set' / 'generated'
        self.reference = self.work / 'north.png'
        self.reference.write_bytes(PNG)
        self.calls = []
        # Point ComfyUI at a port nothing listens on: a paid-only run must never need it.
        for patch in (mock.patch.object(art, 'SERVER', 'http://127.0.0.1:9'),
                      mock.patch.object(art.paid, 'make_image', self.fake_make_image)):
            patch.start()
            self.addCleanup(patch.stop)
        self.answers = None

    def write(self, *requests):
        self.requests.write_text(json.dumps(list(requests)), encoding='utf-8')

    def fake_make_image(self, req, references, api_key):
        self.calls.append((req['name'], [name for name, _ in references], api_key))
        answer = self.answers.pop(0) if self.answers else None
        if isinstance(answer, BaseException):
            raise answer
        return PNG, {'endpoint': '/images/edits' if references else '/images/generations', 'usage': USAGE, 'cost_usd': 0.04,
                     'cost_is_estimate': False}

    def run_paid(self, max_usd=1.0):
        with mock.patch('sys.stdout', new=io.StringIO()):
            return art.run(self.requests, budget=paid.Budget(max_usd), api_key='sk-test', keep_going=True)


class PaidRunTests(PaidRunCase):
    def test_images_and_records_without_comfyui(self):
        self.write(good_request(references=[str(self.reference)]))
        result = self.run_paid()
        self.assertEqual(result['made'], 2)
        self.assertAlmostEqual(result['spent_usd'], 0.08)
        self.assertEqual(self.calls[0], ('truck', ['north.png'], 'sk-test'))
        record = json.loads((self.generated / 'truck' / 'truck-1.json').read_text(encoding='utf-8'))
        self.assertEqual(record['engine'], 'openai')
        self.assertEqual(record['cost_usd'], 0.04)
        self.assertIn('generative AI', record['ai_disclosure'])
        self.assertIn('terms', record)
        self.assertEqual(len(record['references']), 1)
        self.assertNotIn('sk-test', json.dumps(record))
        self.assertTrue((self.generated / '.gitignore').is_file())

    def test_the_cap_stops_the_run_before_an_image_that_could_pass_it(self):
        self.write(good_request(seeds=[1, 2, 3]))
        result = self.run_paid(max_usd=0.10)  # Each may cost up to 0.053 and costs 0.04: 0.08 spent, and 0.133 would pass.
        self.assertEqual(result['made'], 2)
        self.assertIn('cap', result['stopped'])

    def test_a_fatal_refusal_ends_the_run_at_once(self):
        self.write(good_request(seeds=[1, 2, 3]))
        self.answers = [paid.ApiError('no credit', 429, 'insufficient_quota', fatal=True)]
        result = self.run_paid()
        self.assertEqual(result['made'], 0)
        self.assertEqual(len(self.calls), 1)
        self.assertIn('no later image', result['stopped'])

    def test_one_refused_image_is_recorded_and_the_rest_made(self):
        self.write(good_request(seeds=[1, 2]))
        self.answers = [paid.ApiError('blocked', 400, 'moderation_blocked')]
        result = self.run_paid()
        self.assertEqual(result['made'], 1)
        self.assertEqual(len(result['failures']), 1)

    def test_an_unanswered_call_still_counts_against_the_cap(self):
        self.write(good_request(seeds=[1]))
        self.answers = [paid.ApiError('no answer from OpenAI')]
        result = self.run_paid()
        self.assertAlmostEqual(result['spent_usd'], paid.ESTIMATE_PER_IMAGE['medium'], places=4)

    def test_existing_images_are_not_paid_for_again(self):
        self.write(good_request())
        self.run_paid()
        self.calls.clear()
        result = self.run_paid()
        self.assertEqual((result['made'], result['skipped'], self.calls), (0, 2, []))

    def test_a_bad_request_sends_nothing(self):
        self.write(good_request(width=1000))
        with self.assertRaises(ValueError):
            self.run_paid()
        self.assertEqual(self.calls, [])

    def test_paid_requests_have_no_graph(self):
        self.write(good_request())
        self.assertEqual(art.preview_graphs(self.requests), {})
        with self.assertRaises(ValueError):
            art.build_graph(good_request(), 1)


class ArtRunPaidTests(PaidRunCase):
    def main(self, *extra, processes=frozenset({'explorer.exe'})):
        out = io.StringIO()
        with mock.patch.object(art_run, 'read_processes', lambda: set(processes)), \
                mock.patch.object(art_run, 'stay_awake', lambda on: None), \
                mock.patch.dict(os.environ, {paid.KEY_VARIABLE: 'sk-test'}), \
                mock.patch('sys.stdout', new=out):
            code = art_run.main(['generate', str(self.requests), '--comfy', str(self.work / 'no-comfy-here'), *extra])
        return code, out.getvalue()

    def test_a_dry_run_shows_the_most_it_could_cost_and_sends_nothing(self):
        self.write(good_request())
        code, out = self.main('--dry-run')
        self.assertEqual(code, 0)
        self.assertIn('US$0.11', out)
        self.assertEqual(self.calls, [])

    def test_a_dry_run_shows_what_would_be_refused(self):
        self.write(good_request(width=1000))
        code, out = self.main('--dry-run')
        self.assertEqual(code, 2)
        self.assertIn('REFUSED', out)

    def test_no_cap_means_no_start(self):
        self.write(good_request())
        code, out = self.main('--stop-at', '23:59')
        self.assertEqual(code, art_run.NOT_STARTED)
        self.assertIn('--max-usd', out)
        self.assertEqual(self.calls, [])

    def test_the_game_running_does_not_stop_a_paid_only_run(self):
        self.write(good_request())
        code, _ = self.main('--stop-at', '23:59', '--max-usd', '1', processes={'rimworldwin64.exe'})
        self.assertEqual(code, 0)
        self.assertEqual(len(self.calls), 2)
        report = next(self.generated.glob('run-*.md')).read_text(encoding='utf-8')
        self.assertIn('**Spent:** about US$0.080 of the US$1.00 cap', report)
        self.assertIn("OpenAI's servers", report)

    def test_a_night_step_carries_its_cap(self):
        args = art_run.argparse.Namespace(comfy='c', min_free_gb=5.0, dry_run=False, also_stop_on=[])
        stop_at = art_run._dt.datetime(2026, 11, 9, 7, 0)
        command = art_run.step_command({'generate': 'paid.json', 'max_usd': 0.5}, stop_at, args)
        self.assertEqual(command[-2:], ['--max-usd', '0.5'])
        self.assertEqual(art_run.night_problems([{'generate': 'paid.json', 'max_usd': 0.5}]), [])
        self.assertTrue(art_run.night_problems([{'train': 'x.yaml', 'max_usd': 0.5}]))


if __name__ == '__main__':
    unittest.main()
