"""The OpenAI image API (gpt-image-2) as a paid art engine: request checks, cost, the spending cap and the call itself.

Used by scripts/generate-art.py for requests with "engine": "openai". Nothing here runs on the graphics card: every image is
made on OpenAI's servers and **paid for per image** from the owner's prepaid credit. The key comes from the OPENAI_API_KEY
environment variable only; it is never written to a file, a record or a report, and any key-like text in an error is
blanked out before it is shown.

A request without references is a plain generation; one with references (up to MAX_REFERENCES) is an edit, the route for
"the north body, turned a quarter: fix the light". The API has no seed, so a request's "seeds" are attempt numbers that
name the files; running the same attempt again gives a different picture.

Figures and sources (read 8 November 2026; they move, so re-read before trusting a cost):
- Request fields, sizes and response: OpenAI's API reference, https://developers.openai.com/api/reference/resources/images
- Token prices: OpenAI's pricing page, https://developers.openai.com/api/docs/pricing (standard tier, gpt-image-2).
- Per-image estimates: OpenAI's image generation guide, https://developers.openai.com/api/docs/guides/image-generation,
  read through a summary rather than the page's own words, so unverified. They are only the cap's look-ahead; the cost
  recorded for each image is worked out from the token counts the API returns.
"""
import base64
import json
import re
import time
import urllib.error
import urllib.request
import uuid

API = 'https://api.openai.com/v1'
MODEL = 'gpt-image-2'
KEY_VARIABLE = 'OPENAI_API_KEY'
TERMS_READ = '2026-11-08'
TERMS_NOTE = ('OpenAI terms of use as summarised on ' + TERMS_READ + ': the user owns the outputs, which may not be unique; '
              'no use of outputs to build competing models (secondary source, ConductAtlas; the openai.com page refused the fetch).')

QUALITIES = ('low', 'medium', 'high')
BACKGROUNDS = ('auto', 'opaque', 'transparent')  # transparent is a preview feature on gpt-image-2 (API reference).
MODERATION = ('auto', 'low')
MAX_REFERENCES = 16      # "For GPT image models, you can provide up to 16 images" (API reference).
SIZE_STEP = 16           # Width and height must both be divisible by 16 (API reference, and the API's own refusal).
MAX_ASPECT = 3           # Aspect ratio between 1:3 and 3:1 (API reference).
MAX_EDGE = 3840          # Maximum supported resolution 3840x2160 (API reference).
MAX_PIXELS = 3840 * 2160
REFERENCE_SUFFIXES = ('.png', '.jpg', '.jpeg', '.webp')

# US dollars per million tokens, standard tier (pricing page). Text output has no price listed, so output is all charged at
# the image rate, which can only overstate the cost.
PRICE_PER_MILLION = {'text_input': 5.00, 'image_input': 8.00, 'image_output': 30.00}

# The cap's look-ahead, per image before the call. The guide's figures (unverified, see above): low 0.006, medium 0.053,
# high 0.211 at 1024x1024, and less at 1024x1536. The highest per quality is used, scaled up for anything larger than
# 1024x1536. Each reference adds an authored allowance of 6,000 image-input tokens (US$0.048), an overstatement on purpose:
# the guide gives no reference-token figure for gpt-image-2.
ESTIMATE_PER_IMAGE = {'low': 0.006, 'medium': 0.053, 'high': 0.211}
ESTIMATE_BASE_PIXELS = 1024 * 1536
REFERENCE_TOKEN_ALLOWANCE = 6000

TIMEOUT_SECONDS = 600              # One image can take a minute or two; a timed-out call may still be billed.
RETRY_DELAYS_SECONDS = (15, 60)    # For rate limits and server errors only (authored).
RETRYABLE_STATUS = (429, 500, 502, 503, 504)
FATAL_CODES = ('insufficient_quota', 'billing_hard_limit_reached', 'invalid_api_key', 'organization_verification_required')
KEY_PATTERN = re.compile(r'sk-[A-Za-z0-9_\-*.]{4,}')


class ApiError(RuntimeError):
    """A refusal or failure from the API. fatal means no later image can succeed either (no credit, a bad key, an account
    check), so the run ends instead of spending its failures one by one."""

    def __init__(self, message, status=None, code=None, fatal=False):
        super().__init__(message)
        self.status, self.code, self.fatal = status, code, fatal


def redact(text):
    """Blank out anything shaped like an API key, so an error message can be printed and saved safely."""
    return KEY_PATTERN.sub('sk-[hidden]', str(text))


def settings(req):
    """The request's API settings with their defaults: quality medium (the plan's choice), background and moderation left
    to the API unless the request names them."""
    return {'quality': req.get('quality', 'medium'), 'background': req.get('background'), 'moderation': req.get('moderation')}


def request_problems(req):
    """Everything the API would refuse about a request, found before any money is spent."""
    problems = []
    name = req.get('name', '?')
    width, height = req.get('width'), req.get('height')
    if not isinstance(width, int) or not isinstance(height, int) or width <= 0 or height <= 0:
        return [f'{name}: width and height must be whole numbers above 0']
    if width % SIZE_STEP or height % SIZE_STEP:
        problems.append(f'{name}: {width}x{height} is refused: width and height must both divide by {SIZE_STEP}')
    if max(width, height) > MAX_ASPECT * min(width, height):
        problems.append(f'{name}: {width}x{height} is refused: the shape must lie between 1:{MAX_ASPECT} and {MAX_ASPECT}:1')
    if max(width, height) > MAX_EDGE or width * height > MAX_PIXELS:
        problems.append(f'{name}: {width}x{height} is refused: the largest size is {MAX_EDGE}x2160')
    chosen = settings(req)
    if chosen['quality'] not in QUALITIES:
        problems.append(f'{name}: quality {chosen["quality"]!r} is not one of {", ".join(QUALITIES)}')
    if chosen['background'] is not None and chosen['background'] not in BACKGROUNDS:
        problems.append(f'{name}: background {chosen["background"]!r} is not one of {", ".join(BACKGROUNDS)}')
    if chosen['moderation'] is not None and chosen['moderation'] not in MODERATION:
        problems.append(f'{name}: moderation {chosen["moderation"]!r} is not one of {", ".join(MODERATION)}')
    references = req.get('references', [])
    if len(references) > MAX_REFERENCES:
        problems.append(f'{name}: {len(references)} references; the API takes at most {MAX_REFERENCES}')
    for ref in references:
        if not str(ref).lower().endswith(REFERENCE_SUFFIXES):
            problems.append(f'{name}: reference {ref} is not a PNG, JPEG or WebP file')
    for unused in ('init', 'lora', 'steps', 'cfg', 'model'):
        if unused in req:
            problems.append(f'{name}: {unused!r} is a local-engine setting the OpenAI engine cannot use')
    if not req.get('prompt'):
        problems.append(f'{name}: the OpenAI engine needs a prompt')
    return problems


def estimate_usd(req):
    """The most one image of this request should cost, for the cap's look-ahead (see ESTIMATE_PER_IMAGE)."""
    quality = settings(req)['quality']
    scale = max(1.0, req['width'] * req['height'] / ESTIMATE_BASE_PIXELS)
    references = len(req.get('references', []))
    return ESTIMATE_PER_IMAGE.get(quality, ESTIMATE_PER_IMAGE['high']) * scale + \
        references * REFERENCE_TOKEN_ALLOWANCE * PRICE_PER_MILLION['image_input'] / 1e6


def cost_usd(usage):
    """What an image cost, from the token counts the API returned; None if it returned none."""
    if not usage:
        return None
    details = usage.get('input_tokens_details') or {}
    text_in = details.get('text_tokens')
    image_in = details.get('image_tokens')
    if text_in is None and image_in is None:  # No split given: charge all input at the dearer image rate.
        text_in, image_in = 0, usage.get('input_tokens', 0)
    output = usage.get('output_tokens', 0)
    return (float(text_in or 0) * PRICE_PER_MILLION['text_input'] + float(image_in or 0) * PRICE_PER_MILLION['image_input']
            + float(output) * PRICE_PER_MILLION['image_output']) / 1e6


class Budget:
    """The run's spending cap in US dollars. An image is only asked for when what is already spent plus that image's
    estimate stays within the cap, so the cap is never passed on the estimate's terms."""

    def __init__(self, max_usd):
        if max_usd is None or max_usd <= 0:
            raise ValueError('a paid run needs a spending cap above 0 (--max-usd)')
        self.max_usd = float(max_usd)
        self.spent_usd = 0.0
        self.images = 0

    def refusal(self, estimate):
        """Why the next image must not be asked for, or None."""
        if self.spent_usd + estimate > self.max_usd:
            return (f'the next image could cost up to US${estimate:.3f}, which would pass the US${self.max_usd:.2f} cap '
                    f'(US${self.spent_usd:.3f} spent so far)')
        return None

    def add(self, usd):
        self.spent_usd += usd
        self.images += 1


def generation_body(req):
    """The JSON body for a plain generation."""
    chosen = settings(req)
    body = {'model': MODEL, 'prompt': req['prompt'], 'size': f'{req["width"]}x{req["height"]}', 'quality': chosen['quality'],
            'n': 1, 'output_format': 'png'}
    for key in ('background', 'moderation'):
        if chosen[key] is not None:
            body[key] = chosen[key]
    return body


def edit_form(req, references):
    """The multipart body for an edit: the same fields as a generation, plus each reference as an image[] file, in order, so
    the prompt can say "the first image" and "the second image". references: (file name, bytes) pairs."""
    fields = {key: str(value) for key, value in generation_body(req).items()}
    boundary = f'phobos-{uuid.uuid4().hex}'
    parts = []
    for key, value in fields.items():
        parts.append(f'--{boundary}\r\nContent-Disposition: form-data; name="{key}"\r\n\r\n{value}\r\n'.encode('utf-8'))
    for file_name, data in references:
        kind = 'image/jpeg' if file_name.lower().endswith(('.jpg', '.jpeg')) else (
            'image/webp' if file_name.lower().endswith('.webp') else 'image/png')
        parts.append(f'--{boundary}\r\nContent-Disposition: form-data; name="image[]"; filename="{file_name}"\r\n'
                     f'Content-Type: {kind}\r\n\r\n'.encode('utf-8') + data + b'\r\n')
    parts.append(f'--{boundary}--\r\n'.encode('utf-8'))
    return b''.join(parts), f'multipart/form-data; boundary={boundary}'


def _error_from(status, body):
    try:
        error = json.loads(body).get('error') or {}
    except (ValueError, AttributeError):
        error = {}
    if isinstance(error, str):
        error = {'message': error}
    code = error.get('code') or error.get('type')
    message = redact(error.get('message') or body[:300])
    fatal = status in (401, 403) or code in FATAL_CODES or (status == 429 and code == 'insufficient_quota')
    if status == 403 and 'verif' in message.lower():
        message += ' (the organisation may need verifying in the OpenAI developer console before image models work)'
    return ApiError(f'OpenAI refused the image ({status}{", " + code if code else ""}): {message}', status, code, fatal)


def post(path, data, content_type, api_key, opener=urllib.request.urlopen, sleep=time.sleep):
    """POST to the API, retrying only rate limits and server errors. A timeout is not retried: OpenAI may already have
    made, and billed, that image."""
    attempts = len(RETRY_DELAYS_SECONDS) + 1
    for attempt in range(attempts):
        request = urllib.request.Request(f'{API}{path}', data=data, method='POST',
                                         headers={'Authorization': f'Bearer {api_key}', 'Content-Type': content_type})
        try:
            with opener(request, timeout=TIMEOUT_SECONDS) as response:
                return json.loads(response.read())
        except urllib.error.HTTPError as http_error:
            with http_error:
                error = _error_from(http_error.code, http_error.read().decode('utf-8', 'replace'))
            if http_error.code in RETRYABLE_STATUS and not error.fatal and attempt < attempts - 1:
                sleep(RETRY_DELAYS_SECONDS[attempt])
                continue
            raise error from None
        except (urllib.error.URLError, TimeoutError, OSError) as network:
            raise ApiError(f'no answer from OpenAI: {redact(network)}') from None
    raise ApiError('no answer from OpenAI')


def make_image(req, references, api_key, opener=urllib.request.urlopen, sleep=time.sleep):
    """One image. references: (file name, bytes) pairs, empty for a plain generation. Returns the PNG bytes and what the
    record keeps: the endpoint, the settings the API reports it used, the token counts and the cost."""
    if references:
        data, content_type = edit_form(req, references)
        path = '/images/edits'
    else:
        data, content_type = json.dumps(generation_body(req)).encode('utf-8'), 'application/json'
        path = '/images/generations'
    reply = post(path, data, content_type, api_key, opener, sleep)
    images = [item.get('b64_json') for item in reply.get('data') or [] if item.get('b64_json')]
    if not images:
        raise ApiError('OpenAI answered without an image')
    usage = reply.get('usage')
    cost = cost_usd(usage)
    info = {'endpoint': path, 'usage': usage, 'cost_usd': round(cost, 4) if cost is not None else None,
            'cost_is_estimate': cost is None}
    for key in ('quality', 'size', 'background', 'output_format'):
        if reply.get(key) is not None:
            info[f'reported_{key}'] = reply[key]
    return base64.b64decode(images[0]), info
