"""fal.ai's model API as a paid art engine: one prepaid key reaching several makers' image models.

Used by scripts/generate-art.py for requests with "engine": "fal" and a "model" naming one of MODELS. Nothing here runs on the
graphics card: every image is made on fal's servers and **paid for per image** from the owner's prepaid credit (owner's choice
of 18 Nov 2026, for pictures larger than the OpenAI engine makes: the V-22 Osprey's body). The key comes from the FAL_KEY
environment variable only; it is never written to a file, a record or a report, and it is blanked out of any error before the
error is shown.

The same rules as the OpenAI engine (docs/development/paid-image-generation-rules.md): **only our own art may be sent** as a
reference, checked before anything is sent; a dry run and a spending cap come first; every picture gets a record. The cap,
the error type and the reference helpers are the OpenAI engine's own, so the two cannot drift apart.

A request gives a width and a height like any other. Models that take a size class and a shape (Google's and Black Forest
Labs') get the smallest class that holds the request and the shape it names; a shape the model does not offer is refused, never
bent. The record keeps the size that came back.

**What a picture cost is an estimate here**, from the table below: fal's queue answers carry no price. Each estimate is set at
or above the price as read, so the cap can only overstate. Where fal lets the key read the account's balance, the run prints
how far it fell, which is the true spend; otherwise fal's dashboard has it.

Figures and sources, read 18 November 2026 by a research helper from fal's own pages (they move: re-read before trusting):
- The queue: https://fal.ai/docs/model-apis/model-endpoints/queue and its API reference. Submit with POST
  https://queue.fal.run/<model>; poll the status_url the answer gives; fetch the response_url. "A failed or cancelled request
  also has the status COMPLETED."
- Each model's fields: its page, https://fal.ai/models/<model>/api. Prices: https://fal.ai/pricing.
- Images in: "Models also accept base64-encoded data URIs" (fal's CDN page). No REST upload route is documented, so
  references go in the request, which may be 64 MiB at most; ours are far smaller.
- Charging: "Server errors (HTTP 500+) are never charged"; an invalid input (422) "may still be charged if a runner spent GPU
  time" (fal's FAQ). So a request that ran and then failed counts against the cap.
"""
import base64
import json
import re
import time
import urllib.error
import urllib.request

import openai_images as shared

ApiError = shared.ApiError
Budget = shared.Budget
reference_paths = shared.reference_paths
reference_roles = shared.reference_roles
png_size = shared.png_size

QUEUE = 'https://queue.fal.run'
BILLING = 'https://api.fal.ai/v1/account/billing?expand=credits'
KEY_VARIABLE = 'FAL_KEY'
SERVICE = 'fal'
SENDS_ATTEMPT = True   # The caller passes the attempt number, which goes to the model as its seed where it takes one.
TERMS_READ = '2026-11-18'
TERMS_NOTE = ('fal Terms of Service (last updated 8 Sep 2026) and API Services Terms, read in part on ' + TERMS_READ + ': the '
              'customer keeps its inputs; the part read has no sentence saying the customer owns outputs, and says fal does not '
              'warrant that outputs are original; access is "for your own personal or internal business use". fal\'s FAQ: "Most '
              'models on fal are available for commercial use and are marked with a Commercial use badge on the model page". API '
              'Services Terms 2(d): fal "will not use Client Content to create, train, develop" its products. Inputs and outputs '
              'are kept on the platform for 90 days by default. Each model also falls under its maker\'s own terms, not read. '
              'The owner confirms the current text.')

TIMEOUT_SECONDS = 900          # A 4K picture "can take several minutes" (the FLUX 3 page).
POLL_SECONDS = 3
RETRY_DELAYS_SECONDS = (15, 60)    # For a refused submission only: nothing has run yet, so nothing is billed.
RETRYABLE_STATUS = (429, 500, 502, 503, 504)
MAX_BODY_BYTES = 24 * 1024 * 1024  # Far under fal's 64 MiB, and far over any reference of ours.
SHAPE_TOLERANCE = 0.015            # How far a request's shape may sit from the model's nearest, as a share.
REFERENCE_SUFFIXES = shared.REFERENCE_SUFFIXES
KEY_PATTERN = re.compile(r'[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F-]{20,}:[0-9a-fA-F]{16,}')

GEMINI_SHAPES = ('21:9', '16:9', '3:2', '4:3', '5:4', '1:1', '4:5', '3:4', '2:3', '9:16', '4:1', '1:4', '8:1', '1:8')
FLUX3_SHAPES = ('21:9', '2:1', '16:9', '3:2', '7:5', '4:3', '5:4', '1:1', '4:5', '3:4', '5:7', '2:3', '9:16', '1:2')

# The models this engine may call. "classes" maps the model's size class to the long side it stands for (authored: 1K is
# 1,024 px, and so on; the size that comes back is recorded). "usd" is the most one picture should cost. Sources are fal's
# pricing page and model pages unless a line says otherwise.
MODELS = {
    'fal-ai/nano-banana-pro/edit': {
        'label': 'Google Nano Banana Pro (Gemini 3 Pro Image), with references',
        'classes': {'1K': 1024, '2K': 2048, '4K': 4096}, 'shapes': GEMINI_SHAPES, 'references': (1, 14), 'seed': True,
        # "$0.15 per images" (pricing page); the model page as quoted by a search summary: 4K is charged at double.
        'usd': {'1K': 0.15, '2K': 0.15, '4K': 0.30}},
    'fal-ai/nano-banana-pro': {
        'label': 'Google Nano Banana Pro (Gemini 3 Pro Image)',
        'classes': {'1K': 1024, '2K': 2048, '4K': 4096}, 'shapes': GEMINI_SHAPES, 'references': (0, 0), 'seed': True,
        'usd': {'1K': 0.15, '2K': 0.15, '4K': 0.30}},
    'fal-ai/nano-banana-2/edit': {
        'label': 'Google Nano Banana 2, with references',
        'classes': {'0.5K': 512, '1K': 1024, '2K': 2048, '4K': 4096}, 'shapes': GEMINI_SHAPES, 'references': (1, 14),
        'seed': True,
        # "$0.08 per images" (pricing page), with no price by size. The larger sizes' figures are authored ceilings: Google's
        # own price for this family at 4K is about three times its 1K price.
        'usd': {'0.5K': 0.08, '1K': 0.08, '2K': 0.16, '4K': 0.24}},
    'fal-ai/bytedance/seedream/v4.5/edit': {
        'label': 'ByteDance Seedream 4.5, with references',
        # "Width and height must be between 1920 and 4096, or total number of pixels must be between 2560*1440 and 4096*4096."
        'pixels': {'side': (1920, 4096), 'area': (2560 * 1440, 4096 * 4096)}, 'references': (1, 10), 'seed': True,
        'usd_flat': 0.04},   # "$0.04 per images"
    'blackforestlabs/flux-3/edit-image': {
        'label': 'Black Forest Labs FLUX 3 Image, with references',
        'classes': {'1k': 1024, '1.5k': 1536, '2k': 2048, '4k': 4096}, 'shapes': FLUX3_SHAPES, 'references': (1, 10),
        'seed': False,
        # "$0.048 per megapixels". Charged here on a square of the size class and on every reference at the 4 megapixels a
        # reference is cut down to, which can only overstate.
        'usd_per_megapixel': 0.048},
}


def redact(text, api_key=None):
    """Blank out the key, and anything shaped like one, so an error can be printed and saved safely."""
    text = shared.redact(text)
    if api_key:
        for part in [api_key] + [piece for piece in str(api_key).split(':') if len(piece) >= 8]:
            text = text.replace(part, '[hidden]')
    return KEY_PATTERN.sub('[hidden]', text)


def model_of(req):
    return req.get('model', '')


def _shape_value(shape):
    across, up = shape.split(':')
    return float(across) / float(up)


def nearest_shape(width, height, shapes):
    """The model's shape nearest the request's, and how far off it is as a share."""
    wanted = width / height
    best = min(shapes, key=lambda shape: abs(_shape_value(shape) - wanted) / wanted)
    return best, abs(_shape_value(best) - wanted) / wanted


def size_class(longest, classes):
    """The smallest size class that holds this long side, or None when none does."""
    for name, side in sorted(classes.items(), key=lambda item: item[1]):
        if longest <= side:
            return name
    return None


def settings(req):
    """What the request asks of its model: the model, and its size as the model takes it. Empty for an unknown model."""
    model = MODELS.get(model_of(req))
    if not model or not isinstance(req.get('width'), int) or not isinstance(req.get('height'), int):
        return {'model': model_of(req)}
    chosen = {'model': model_of(req)}
    if 'classes' in model:
        chosen['resolution'] = size_class(max(req['width'], req['height']), model['classes'])
        chosen['aspect_ratio'] = nearest_shape(req['width'], req['height'], model['shapes'])[0]
    else:
        chosen['image_size'] = {'width': req['width'], 'height': req['height']}
    return chosen


def describe(req):
    """The request's model and size in a few words, for a dry run."""
    chosen = settings(req)
    size = f'{chosen["resolution"]} {chosen["aspect_ratio"]}' if 'resolution' in chosen else 'exact size'
    return f'{chosen["model"]}, {size}'


def request_problems(req, allowed_roots=None, root=None):
    """Everything fal or the model would refuse about a request, found before any money is spent, plus what our own rules
    refuse: a reference outside allowed_roots (resolved against root when relative) is never sent."""
    from pathlib import Path
    problems = []
    name = req.get('name', '?')
    model_name = model_of(req)
    model = MODELS.get(model_name)
    if model is None:
        return [f'{name}: the fal engine needs a "model" it knows; {model_name!r} is not one of {", ".join(MODELS)}']
    width, height = req.get('width'), req.get('height')
    if not isinstance(width, int) or not isinstance(height, int) or width <= 0 or height <= 0:
        return [f'{name}: width and height must be whole numbers above 0']
    if 'classes' in model:
        if size_class(max(width, height), model['classes']) is None:
            problems.append(f'{name}: {width}x{height} is refused: the largest size of {model_name} is '
                            f'{max(model["classes"].values())} px on its long side')
        shape, off = nearest_shape(width, height, model['shapes'])
        if off > SHAPE_TOLERANCE:
            problems.append(f'{name}: {width}x{height} is not a shape {model_name} offers (the nearest is {shape}); '
                            'ask for that shape and crop by script')
    else:
        low, high = model['pixels']['side']
        least, most = model['pixels']['area']
        if not ((low <= width <= high and low <= height <= high) or least <= width * height <= most):
            problems.append(f'{name}: {width}x{height} is refused: {model_name} wants each side between {low} and {high}, or '
                            f'between {least:,} and {most:,} pixels in all')
    for ref in req.get('references', []):
        if isinstance(ref, dict) and ('image' not in ref or set(ref) - {'image', 'role'}):
            problems.append(f'{name}: a reference entry needs "image" and may have "role", nothing else')
    references = reference_paths(req)
    fewest, most_refs = model['references']
    if not fewest <= len(references) <= most_refs:
        problems.append(f'{name}: {len(references)} references; {model_name} takes '
                        + ('none' if most_refs == 0 else f'between {fewest} and {most_refs}'))
    total = 0
    for ref in references:
        if not str(ref).lower().endswith(REFERENCE_SUFFIXES):
            problems.append(f'{name}: reference {ref} is not a PNG, JPEG or WebP file')
            continue
        path = Path(ref) if Path(ref).is_absolute() or root is None else Path(root) / ref
        if allowed_roots is not None and shared.outside_own_art(path, allowed_roots):
            problems.append(f'{name}: reference {ref} is outside our own art, so it may not be sent to a paid service '
                            "(the game's textures and other authors' art stay on this PC)")
        elif path.is_file():
            total += path.stat().st_size
    if total * 4 / 3 > MAX_BODY_BYTES:
        problems.append(f'{name}: the references come to {total / 1e6:.1f} MB, more than this engine sends in one request')
    for unused in ('init', 'lora', 'steps', 'cfg', 'quality', 'background', 'moderation'):
        if unused in req:
            problems.append(f'{name}: {unused!r} is another engine\'s setting the fal engine cannot use')
    if not req.get('prompt'):
        problems.append(f'{name}: the fal engine needs a prompt')
    return problems


def estimate_usd(req):
    """The most one image of this request should cost: the table's figure for its model and size."""
    model = MODELS[model_of(req)]
    chosen = settings(req)
    if 'usd_flat' in model:
        return model['usd_flat']
    if 'usd_per_megapixel' in model:
        side = model['classes'][chosen['resolution']]
        megapixels = side * side / 1e6 + 4 * len(reference_paths(req))
        return model['usd_per_megapixel'] * megapixels
    return model['usd'][chosen['resolution']]


def data_uri(file_name, data):
    kind = 'image/jpeg' if file_name.lower().endswith(('.jpg', '.jpeg')) else (
        'image/webp' if file_name.lower().endswith('.webp') else 'image/png')
    return f'data:{kind};base64,' + base64.b64encode(data).decode('ascii')


def request_body(req, references, attempt=None):
    """The JSON the model is sent. references: (file name, bytes) pairs, in the order the prompt names them."""
    model = MODELS[model_of(req)]
    body = {'prompt': req['prompt'], 'num_images': 1, 'output_format': 'png'}
    chosen = settings(req)
    for key in ('resolution', 'aspect_ratio', 'image_size'):
        if key in chosen:
            body[key] = chosen[key]
    if references:
        body['image_urls'] = [data_uri(file_name, data) for file_name, data in references]
    if model['seed'] and attempt is not None:
        body['seed'] = int(attempt)
    return body


def _error_from(status, body, api_key):
    kind, message = None, body[:300]
    try:
        parsed = json.loads(body)
    except ValueError:
        parsed = None
    if isinstance(parsed, list) and parsed and isinstance(parsed[0], dict):   # A refused input: a list of what and why.
        kind = parsed[0].get('type')
        message = '; '.join(str(item.get('msg', '')) for item in parsed if isinstance(item, dict))[:300]
    elif isinstance(parsed, dict):
        kind = parsed.get('type') or parsed.get('error_type')
        detail = parsed.get('detail') or parsed.get('error') or parsed
        message = (detail if isinstance(detail, str) else json.dumps(detail))[:300]
    fatal = status in (401, 403)   # A bad key; or no credit, a locked account or a key not allowed this model.
    note = ' (the account may be out of credit or locked, or the key may not be allowed this model)' if status == 403 else ''
    return ApiError(f'fal refused the image ({status}{", " + kind if kind else ""}): {redact(message, api_key)}{note}',
                    status, kind, fatal)


def _call(url, api_key, opener, method='GET', data=None, auth=True):
    headers = {'Authorization': f'Key {api_key}'} if auth else {}
    if data is not None:
        headers['Content-Type'] = 'application/json'
    request = urllib.request.Request(url, data=data, method=method, headers=headers)
    with opener(request, timeout=120) as response:
        return response.read()


def _ran_then_failed(error):
    error.maybe_billed = True   # fal's FAQ: an input refused after a runner started may still be charged.
    return error


def make_image(req, references, api_key, opener=urllib.request.urlopen, sleep=time.sleep, attempt=None, clock=time.monotonic):
    """One image. references: (file name, bytes) pairs. Returns the PNG bytes and what the record keeps. A request that was
    accepted and then failed, or never answered, is marked as possibly billed, so the cap counts it."""
    model_name = model_of(req)
    body = json.dumps(request_body(req, references, attempt)).encode('utf-8')
    submitted = None
    attempts = len(RETRY_DELAYS_SECONDS) + 1
    for turn in range(attempts):
        try:
            submitted = json.loads(_call(f'{QUEUE}/{model_name}', api_key, opener, 'POST', body))
            break
        except urllib.error.HTTPError as http_error:
            with http_error:
                error = _error_from(http_error.code, http_error.read().decode('utf-8', 'replace'), api_key)
            if http_error.code in RETRYABLE_STATUS and turn < attempts - 1:
                sleep(RETRY_DELAYS_SECONDS[turn])
                continue
            raise error from None
        except (urllib.error.URLError, TimeoutError, OSError) as network:
            raise ApiError(f'no answer from fal: {redact(network, api_key)}') from None
    status_url, response_url = submitted.get('status_url'), submitted.get('response_url')
    if not status_url or not response_url:
        raise ApiError('fal accepted the request without saying where its answer will be')
    deadline = clock() + TIMEOUT_SECONDS
    state = submitted
    while state.get('status') != 'COMPLETED':
        if clock() >= deadline:
            try:   # Ask fal to drop it; a picture already under way may still be made and billed.
                _call(submitted.get('cancel_url') or f'{response_url}/cancel', api_key, opener, 'PUT')
            except (urllib.error.URLError, TimeoutError, OSError):
                pass
            raise ApiError(f'fal had not finished after {TIMEOUT_SECONDS} seconds; it was asked to cancel')
        sleep(POLL_SECONDS)
        try:
            state = json.loads(_call(status_url, api_key, opener))
        except urllib.error.HTTPError as http_error:
            with http_error:
                raise _ran_then_failed(_error_from(http_error.code, http_error.read().decode('utf-8', 'replace'), api_key)) from None
        except (urllib.error.URLError, TimeoutError, OSError):
            continue   # One missed look is not a failure; the deadline still holds.
    try:
        reply = json.loads(_call(response_url, api_key, opener))
    except urllib.error.HTTPError as http_error:
        with http_error:
            raise _ran_then_failed(_error_from(http_error.code, http_error.read().decode('utf-8', 'replace'), api_key)) from None
    except (urllib.error.URLError, TimeoutError, OSError) as network:
        raise ApiError(f'fal made the image but its answer could not be fetched: {redact(network, api_key)}') from None
    images = [item for item in reply.get('images') or [] if isinstance(item, dict) and item.get('url')]
    if not images:
        failure = state.get('error') or reply.get('error') or reply.get('detail') or 'no image in the answer'
        raise _ran_then_failed(ApiError(f'fal answered without an image: {redact(str(failure)[:300], api_key)}', 422))
    url = images[0]['url']
    if url.startswith('data:'):
        data = base64.b64decode(url.split(',', 1)[1])
    elif url.startswith('https://'):
        try:
            data = _call(url, api_key, opener, auth=False)   # A public address: the key is never sent to it.
        except (urllib.error.URLError, TimeoutError, OSError) as network:
            raise ApiError(f'fal made the image but it could not be downloaded: {redact(network, api_key)}') from None
    else:
        raise _ran_then_failed(ApiError('fal answered with an address this engine will not open', 422))
    original = None
    size = png_size(data)
    if size is None:
        # Some models answer in JPEG whatever is asked (Seedream 4.5's edit takes no format at all; found 18 Nov 2026, when the
        # first such picture was paid for and then thrown away). The original is kept byte for byte, as every generated
        # picture is, and a PNG made from it is the candidate the rest of the tools read.
        suffix = original_suffix(data)
        converted = to_png(data) if suffix else None
        if converted is None:
            raise _ran_then_failed(ApiError(f'fal sent {images[0].get("content_type") or "a file"} that is neither a PNG nor a '
                                            'JPEG or WebP picture this engine can convert', 422))
        original, data = (suffix, data), converted
        size = png_size(data)
    estimate = estimate_usd(req)
    info = {'endpoint': model_name, 'request_id': submitted.get('request_id'), 'usage': None,
            'cost_usd': round(estimate, 4), 'cost_is_estimate': True,
            'cost_note': "the table's ceiling for this model and size; fal's answers carry no price",
            'returned_size': size}
    for key, value in settings(req).items():
        if key != 'model':
            info[f'asked_{key}'] = value
    if 'seed' in request_body(req, [], attempt):
        info['sent_seed'] = int(attempt)
    if reply.get('description'):
        info['model_said'] = str(reply['description'])[:500]
    if original:
        info['returned_format'] = original[0].lstrip('.')
        info[ORIGINAL_KEY] = original   # Taken out by the caller, which writes the file; never put in a record.
    if size != [req['width'], req['height']]:
        info['size_note'] = f'asked for {req["width"]}x{req["height"]}, received {size[0]}x{size[1]}'
    return data, info


ORIGINAL_KEY = '_original'


def original_suffix(data):
    """The file suffix for a JPEG or WebP picture, or None for anything else."""
    if data[:3] == b'\xff\xd8\xff':
        return '.jpg'
    if data[:4] == b'RIFF' and data[8:12] == b'WEBP':
        return '.webp'
    return None


def to_png(data):
    """The same picture as PNG bytes, or None if it cannot be read. Pillow is imported here, so the engine runs without it
    until a model answers in another format."""
    try:
        import io
        from PIL import Image
        with Image.open(io.BytesIO(data)) as picture:
            picture.load()
            out = io.BytesIO()
            picture.save(out, format='PNG')
            return out.getvalue()
    except (ImportError, OSError, ValueError):
        return None


def balance(api_key, opener=urllib.request.urlopen):
    """The account's credit in US dollars, or None when the key may not read it (it needs fal's billing or read-only
    permission) or the look fails. Never an error: the true spend is a nicety, the cap is the guard."""
    try:
        reply = json.loads(_call(BILLING, api_key, opener))
        return float(reply['credits']['current_balance'])
    except (urllib.error.URLError, TimeoutError, OSError, ValueError, KeyError, TypeError):
        return None


def record_fields(req):
    """What the record of a fal image says about how it was made."""
    model = MODELS[model_of(req)]
    return {
        'generator': f'{model["label"]}, through fal.ai (paid per image)',
        'engine': 'fal',
        'model': model_of(req),
        'seed_note': ('an attempt number naming the file, also sent to the model as its seed' if model['seed']
                      else 'an attempt number naming the file; this model takes no seed, so a rerun gives a different image'),
        'ai_disclosure': f'Made with generative AI: {model["label"]}, through fal.ai, prompted by the Phobos art scripts.',
        'terms': TERMS_NOTE,
    }
