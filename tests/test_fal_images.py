"""Offline checks of the paid fal engine (scripts/art/fal_images.py) and how the art scripts use it.

Nothing here reaches fal: every call goes to a stand-in, so no key is read and nothing is spent."""
import base64
import contextlib
import importlib.util
import io
import json
import os
from pathlib import Path
import tempfile
import unittest
from unittest import mock
import urllib.error

ROOT = Path(__file__).resolve().parents[1]
_spec = importlib.util.spec_from_file_location('art_run_fal', ROOT / 'scripts' / 'art-run.py')
art_run = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(art_run)
art = art_run.art
fal = art.fal
paid = art.paid

PNG = b'\x89PNG\r\n\x1a\n' + b'\x00\x00\x00\rIHDR' + (3072).to_bytes(4, 'big') + (4096).to_bytes(4, 'big') + b'rest'
PRO = 'fal-ai/nano-banana-pro/edit'
SEEDREAM = 'fal-ai/bytedance/seedream/v4.5/edit'
FLUX3 = 'blackforestlabs/flux-3/edit-image'
KEY = '0a1b2c3d-1111-2222-3333-444455556666:0123456789abcdef0123456789abcdef'


def good_request(**changes):
    req = {'name': 'osprey', 'engine': 'fal', 'model': PRO, 'prompt': 'an aircraft seen from directly above',
           'width': 3072, 'height': 4096, 'seeds': [1, 2], 'references': ['assets/own/plan.png']}
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
        return self.payload if isinstance(self.payload, bytes) else json.dumps(self.payload).encode('utf-8')


def http_error(status, payload):
    return urllib.error.HTTPError('https://queue.fal.run/x', status, 'error', {}, io.BytesIO(json.dumps(payload).encode('utf-8')))


SUBMITTED = {'status': 'IN_QUEUE', 'request_id': 'r1', 'status_url': 'https://queue.fal.run/fal-ai/nano-banana-pro/requests/r1/status',
             'response_url': 'https://queue.fal.run/fal-ai/nano-banana-pro/requests/r1',
             'cancel_url': 'https://queue.fal.run/fal-ai/nano-banana-pro/requests/r1/cancel'}
RESULT = {'images': [{'url': 'https://fal.media/files/a.png', 'content_type': 'image/png', 'width': 3072, 'height': 4096}],
          'description': 'done'}


class FakeFal:
    """Answers in order from a list: bytes or a dict is a reply, an exception is raised."""

    def __init__(self, *answers):
        self.answers = list(answers)
        self.requests = []

    def __call__(self, request, timeout=None):
        self.requests.append(request)
        answer = self.answers.pop(0)
        if isinstance(answer, Exception):
            raise answer
        return Reply(answer)


def happy():
    return FakeFal(SUBMITTED, {'status': 'IN_PROGRESS'}, {'status': 'COMPLETED'}, RESULT, PNG)


class RequestChecks(unittest.TestCase):
    def test_a_good_request_has_no_problems(self):
        self.assertEqual(fal.request_problems(good_request()), [])

    def test_an_unknown_model_is_refused_by_name(self):
        problems = fal.request_problems(good_request(model='fal-ai/who-knows'))
        self.assertEqual(len(problems), 1)
        self.assertIn('fal-ai/who-knows', problems[0])

    def test_a_shape_the_model_does_not_offer_is_refused_not_bent(self):
        problems = fal.request_problems(good_request(width=3600, height=4096))
        self.assertTrue(any('not a shape' in problem and '4:5' in problem for problem in problems))

    def test_a_size_past_the_largest_class_is_refused(self):
        self.assertTrue(any('largest size' in problem for problem in fal.request_problems(good_request(width=4608, height=6144))))

    def test_seedream_takes_an_exact_size_within_its_limits(self):
        self.assertEqual(fal.request_problems(good_request(model=SEEDREAM, width=3300, height=4096)), [])
        self.assertTrue(fal.request_problems(good_request(model=SEEDREAM, width=1024, height=1024)))

    def test_the_number_of_references_is_checked_per_model(self):
        self.assertTrue(any('references' in problem for problem in fal.request_problems(good_request(references=[]))))
        self.assertEqual(fal.request_problems(good_request(model='fal-ai/nano-banana-pro', references=[])), [])

    def test_another_engines_settings_are_refused_rather_than_ignored(self):
        self.assertTrue(any("'quality'" in problem for problem in fal.request_problems(good_request(quality='high'))))

    def test_only_our_own_art_may_be_sent(self):
        with tempfile.TemporaryDirectory() as folder:
            own, game = Path(folder) / 'assets', Path(folder) / '.local' / 'references'
            own.mkdir()
            game.mkdir(parents=True)
            (own / 'plan.png').write_bytes(PNG)
            (game / 'vanilla.png').write_bytes(PNG)
            ours = good_request(references=[str(own / 'plan.png')])
            theirs = good_request(references=[str(game / 'vanilla.png')])
            self.assertEqual(fal.request_problems(ours, [own]), [])
            self.assertTrue(any('outside our own art' in problem for problem in fal.request_problems(theirs, [own])))


class Sizes(unittest.TestCase):
    def test_the_smallest_class_that_holds_the_request_is_chosen(self):
        self.assertEqual(fal.settings(good_request(width=1536, height=2048)), {'model': PRO, 'resolution': '2K', 'aspect_ratio': '3:4'})
        self.assertEqual(fal.settings(good_request())['resolution'], '4K')

    def test_seedream_is_sent_its_exact_size(self):
        self.assertEqual(fal.settings(good_request(model=SEEDREAM, width=3300, height=4096))['image_size'], {'width': 3300, 'height': 4096})


class Costs(unittest.TestCase):
    def test_the_estimate_is_the_tables_figure_for_the_size(self):
        self.assertAlmostEqual(fal.estimate_usd(good_request()), 0.30)
        self.assertAlmostEqual(fal.estimate_usd(good_request(width=1536, height=2048)), 0.15)
        self.assertAlmostEqual(fal.estimate_usd(good_request(model=SEEDREAM, width=3300, height=4096)), 0.04)

    def test_a_price_by_the_megapixel_counts_the_references_too(self):
        one = fal.estimate_usd(good_request(model=FLUX3))
        two = fal.estimate_usd(good_request(model=FLUX3, references=['assets/a.png', 'assets/b.png']))
        self.assertAlmostEqual(one, 0.048 * (4096 * 4096 / 1e6 + 4))
        self.assertAlmostEqual(two - one, 0.048 * 4)


class Calls(unittest.TestCase):
    def test_an_image_is_asked_for_waited_for_and_fetched(self):
        opener = happy()
        data, info = fal.make_image(good_request(), [('plan.png', PNG)], KEY, opener, sleep=lambda s: None, attempt=7)
        self.assertEqual(data, PNG)
        first = opener.requests[0]
        self.assertEqual(first.full_url, f'https://queue.fal.run/{PRO}')
        self.assertEqual(first.get_header('Authorization'), f'Key {KEY}')
        body = json.loads(first.data)
        self.assertEqual((body['resolution'], body['aspect_ratio'], body['output_format'], body['num_images'], body['seed']),
                         ('4K', '3:4', 'png', 1, 7))
        self.assertEqual(body['image_urls'], ['data:image/png;base64,' + base64.b64encode(PNG).decode('ascii')])
        self.assertEqual([request.full_url for request in opener.requests[1:4]],
                         [SUBMITTED['status_url'], SUBMITTED['status_url'], SUBMITTED['response_url']])
        self.assertEqual((info['request_id'], info['returned_size'], info['cost_is_estimate'], info['sent_seed']), ('r1', [3072, 4096], True, 7))
        self.assertAlmostEqual(info['cost_usd'], 0.30)

    def test_a_jpeg_answer_is_kept_as_it_came_and_converted(self):
        from PIL import Image
        buffer = io.BytesIO()
        Image.new('RGB', (40, 30), (200, 100, 50)).save(buffer, format='JPEG')
        jpeg = buffer.getvalue()
        opener = FakeFal(SUBMITTED, {'status': 'COMPLETED'}, RESULT, jpeg)
        data, info = fal.make_image(good_request(model=SEEDREAM, width=3300, height=4096), [('plan.png', PNG)], KEY, opener,
                                    sleep=lambda s: None)
        self.assertEqual(fal.png_size(data), [40, 30])
        self.assertEqual(info[fal.ORIGINAL_KEY], ('.jpg', jpeg))
        self.assertEqual(info['returned_format'], 'jpg')

    def test_an_answer_that_is_no_picture_is_refused_and_may_have_been_billed(self):
        opener = FakeFal(SUBMITTED, {'status': 'COMPLETED'}, RESULT, b'not a picture at all')
        with self.assertRaises(paid.ApiError) as caught:
            fal.make_image(good_request(), [('plan.png', PNG)], KEY, opener, sleep=lambda s: None)
        self.assertTrue(caught.exception.maybe_billed)

    def test_the_key_is_never_sent_to_the_address_the_picture_is_fetched_from(self):
        opener = happy()
        fal.make_image(good_request(), [('plan.png', PNG)], KEY, opener, sleep=lambda s: None)
        self.assertIsNone(opener.requests[-1].get_header('Authorization'))

    def test_a_size_that_differs_from_the_request_is_said(self):
        _, info = fal.make_image(good_request(width=3000, height=4000), [('plan.png', PNG)], KEY, happy(), sleep=lambda s: None)
        self.assertEqual(info['size_note'], 'asked for 3000x4000, received 3072x4096')

    def test_a_busy_service_is_waited_out_before_anything_has_run(self):
        waits = []
        opener = FakeFal(http_error(503, {'detail': 'busy'}), SUBMITTED, {'status': 'COMPLETED'}, RESULT, PNG)
        fal.make_image(good_request(), [('plan.png', PNG)], KEY, opener, sleep=waits.append)
        self.assertEqual(waits[0], fal.RETRY_DELAYS_SECONDS[0])

    def test_no_credit_or_a_bad_key_is_fatal_and_never_retried(self):
        for status in (401, 403):
            opener = FakeFal(http_error(status, {'detail': 'no', 'type': 'rejected_key'}))
            with self.assertRaises(paid.ApiError) as caught:
                fal.make_image(good_request(), [('plan.png', PNG)], KEY, opener, sleep=lambda s: None)
            self.assertTrue(caught.exception.fatal)
            self.assertEqual(len(opener.requests), 1)

    def test_a_refused_prompt_is_not_fatal_and_is_not_counted_as_billed(self):
        refused = [{'loc': ['body', 'prompt'], 'msg': 'not allowed', 'type': 'content_policy_violation'}]
        with self.assertRaises(paid.ApiError) as caught:
            fal.make_image(good_request(), [('plan.png', PNG)], KEY, FakeFal(http_error(422, refused)), sleep=lambda s: None)
        self.assertFalse(caught.exception.fatal)
        self.assertFalse(caught.exception.maybe_billed)
        self.assertIn('content_policy_violation', str(caught.exception))

    def test_a_request_that_ran_and_failed_may_have_been_billed(self):
        opener = FakeFal(SUBMITTED, {'status': 'COMPLETED', 'error': 'runner failed'}, http_error(422, {'detail': 'bad input'}))
        with self.assertRaises(paid.ApiError) as caught:
            fal.make_image(good_request(), [('plan.png', PNG)], KEY, opener, sleep=lambda s: None)
        self.assertTrue(caught.exception.maybe_billed)

    def test_a_request_that_never_finishes_is_cancelled_and_counts_as_unanswered(self):
        ticks = iter([0, 0, fal.TIMEOUT_SECONDS + 1, fal.TIMEOUT_SECONDS + 2])
        opener = FakeFal(SUBMITTED, {'status': 'IN_PROGRESS'}, {'status': 'CANCELLATION_REQUESTED'})
        with self.assertRaises(paid.ApiError) as caught:
            fal.make_image(good_request(), [('plan.png', PNG)], KEY, opener, sleep=lambda s: None, clock=lambda: next(ticks))
        self.assertIsNone(caught.exception.status)
        self.assertEqual((opener.requests[-1].get_method(), opener.requests[-1].full_url), ('PUT', SUBMITTED['cancel_url']))

    def test_the_key_in_an_error_is_hidden(self):
        opener = FakeFal(http_error(401, {'detail': f'the key {KEY} is not valid'}))
        with self.assertRaises(paid.ApiError) as caught:
            fal.make_image(good_request(), [('plan.png', PNG)], KEY, opener, sleep=lambda s: None)
        self.assertNotIn(KEY, str(caught.exception))
        self.assertNotIn(KEY.split(':')[1], str(caught.exception))

    def test_a_balance_the_key_may_not_read_is_no_error(self):
        self.assertIsNone(fal.balance(KEY, FakeFal(http_error(403, {'detail': 'no'}))))
        self.assertEqual(fal.balance(KEY, FakeFal({'username': 'x', 'credits': {'current_balance': 9.5, 'currency': 'USD'}})), 9.5)


class PaidRunCase(unittest.TestCase):
    """The whole run, with fal's call replaced: files, records, the cap."""

    def setUp(self):
        self.folder = tempfile.TemporaryDirectory()
        self.addCleanup(self.folder.cleanup)
        root = Path(self.folder.name)
        (root / 'assets').mkdir()
        (root / 'assets' / 'plan.png').write_bytes(PNG)
        # Candidates are written beside the request folder, so the request sits two levels down, inside the temporary folder.
        (root / 'set' / 'requests').mkdir(parents=True)
        self.requests = root / 'set' / 'requests' / 'requests.json'
        self.generated = root / 'set' / 'generated'
        self.requests.write_text(json.dumps([good_request(references=[{'image': str(root / 'assets' / 'plan.png'), 'role': 'layout'}])]),
                                 encoding='utf-8')
        self.calls = []
        patches = [mock.patch.object(art, 'OWN_ART_ROOTS', (root / 'assets',)),
                   mock.patch.object(fal, 'make_image', self.fake_make_image),
                   mock.patch.object(fal, 'balance', lambda key, opener=None: None)]
        for patch in patches:
            patch.start()
            self.addCleanup(patch.stop)

    def fake_make_image(self, req, references, api_key, attempt=None):
        self.calls.append((req['name'], attempt, api_key))
        return PNG, {'endpoint': req['model'], 'request_id': 'r', 'usage': None, 'cost_usd': fal.estimate_usd(req),
                     'cost_is_estimate': True, 'returned_size': [3072, 4096]}

    def run_paid(self, max_usd):
        with contextlib.redirect_stdout(io.StringIO()):
            return art.run(self.requests, budget=paid.Budget(max_usd), api_key=KEY, keep_going=True)

    def test_images_and_records_are_written_with_the_attempt_as_the_seed(self):
        result = self.run_paid(1.00)
        self.assertEqual(result['made'], 2)
        self.assertEqual(self.calls, [('osprey', 1, KEY), ('osprey', 2, KEY)])
        record = json.loads((self.generated / 'osprey' / 'osprey-1.json').read_text(encoding='utf-8'))
        self.assertEqual((record['engine'], record['model'], record['seed'], record['cost_is_estimate']), ('fal', PRO, 1, True))
        self.assertEqual(record['references'][0]['role'], 'layout')
        self.assertIn('fal Terms of Service', record['terms'])
        self.assertNotIn(KEY, json.dumps(record))

    def test_a_converted_pictures_original_is_written_beside_it_and_named_in_the_record(self):
        def jpeg_answer(req, references, api_key, attempt=None):
            data, info = self.fake_make_image(req, references, api_key, attempt)
            info[fal.ORIGINAL_KEY] = ('.jpg', b'\xff\xd8\xffjpeg bytes')
            return data, info
        with mock.patch.object(fal, 'make_image', jpeg_answer):
            self.run_paid(1.00)
        kept = self.generated / 'osprey' / 'osprey-1.original.jpg'
        self.assertEqual(kept.read_bytes(), b'\xff\xd8\xffjpeg bytes')
        record = json.loads((self.generated / 'osprey' / 'osprey-1.json').read_text(encoding='utf-8'))
        self.assertEqual(record['original_file'], 'osprey-1.original.jpg')
        self.assertNotIn(fal.ORIGINAL_KEY, record)

    def test_the_cap_stops_the_run_before_an_image_that_could_pass_it(self):
        result = self.run_paid(0.50)
        self.assertEqual(result['made'], 1)
        self.assertIn('cap', result['stopped'])

    def test_a_request_that_ran_and_failed_counts_against_the_cap(self):
        def failing(req, references, api_key, attempt=None):
            error = paid.ApiError('fal answered without an image', 422)
            error.maybe_billed = True
            raise error
        with mock.patch.object(fal, 'make_image', failing):
            result = self.run_paid(1.00)
        self.assertEqual(result['made'], 0)
        self.assertAlmostEqual(result['spent_usd'], 0.60)

    def test_a_missing_key_is_named(self):
        with mock.patch.dict(os.environ, {}, clear=False):
            os.environ.pop('FAL_KEY', None)
            with self.assertRaises(RuntimeError) as caught, contextlib.redirect_stdout(io.StringIO()):
                art.run(self.requests, budget=paid.Budget(1.00))
        self.assertIn('FAL_KEY', str(caught.exception))


class DryRun(unittest.TestCase):
    def test_a_dry_run_names_the_model_and_the_most_it_could_cost_and_sends_nothing(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            (root / 'assets').mkdir()
            (root / 'assets' / 'plan.png').write_bytes(PNG)
            (root / 'set' / 'requests').mkdir(parents=True)
            requests = root / 'set' / 'requests' / 'requests.json'
            requests.write_text(json.dumps([good_request(references=[str(root / 'assets' / 'plan.png')])]), encoding='utf-8')
            out = io.StringIO()
            with mock.patch.object(art, 'OWN_ART_ROOTS', (root / 'assets',)), \
                    mock.patch.object(fal, 'make_image', side_effect=AssertionError('nothing may be sent')), \
                    contextlib.redirect_stdout(out):
                code = art_run.main(['generate', str(requests), '--dry-run'])
            self.assertEqual(code, 0)
            self.assertIn(f'{PRO}, 4K 3:4, up to US$0.300 an image', out.getvalue())
            self.assertIn('US$0.60', out.getvalue())

    def test_a_dry_run_shows_an_unknown_model_as_refused(self):
        with tempfile.TemporaryDirectory() as folder:
            (Path(folder) / 'set' / 'requests').mkdir(parents=True)
            requests = Path(folder) / 'set' / 'requests' / 'requests.json'
            requests.write_text(json.dumps([good_request(model='fal-ai/who-knows', references=[])]), encoding='utf-8')
            out = io.StringIO()
            with contextlib.redirect_stdout(out):
                code = art_run.main(['generate', str(requests), '--dry-run'])
            self.assertEqual(code, 2)
            self.assertIn('REFUSED', out.getvalue())


if __name__ == '__main__':
    unittest.main()
