"""Download the local art models into ComfyUI, checking the licence first and the checksum after.

Reads scripts/art/models.json. For a Hugging Face file it asks the Hub for the repository's declared licence and its
gated flag, refuses a licence that differs from the manifest, and verifies the SHA-256 the Hub lists for the file. A gated
repository is never fetched: this script handles no credentials, so it prints what the owner must do instead. Resumes a
partial download. Uses the GPU not at all, so it is safe while a game runs. A model marked "optional" (a large comparison model,
or one under other terms) is fetched only when --optional or --only asks for it.

    python scripts/art/fetch_models.py                       download everything missing that is not optional
    python scripts/art/fetch_models.py --check               report only
    python scripts/art/fetch_models.py --only stable_audio   only files whose name contains this text, optional or not
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import sys
import urllib.error
import urllib.parse
import urllib.request

HERE = Path(__file__).resolve().parent
MANIFEST = HERE / 'models.json'
COMFY = Path(os.environ.get('COMFYUI_PATH', r'C:\AI\ComfyUI\ComfyUI_windows_portable'))
USER_AGENT = 'phobos-art-fetch'
BLOCK = 1 << 20


def open_url(url, headers=None):
    return urllib.request.urlopen(urllib.request.Request(url, headers={'User-Agent': USER_AGENT, **(headers or {})}), timeout=60)


def hub_info(repo):
    """The Hub's licence, gated flag and per-file SHA-256 values for a repository."""
    with open_url(f'https://huggingface.co/api/models/{repo}') as response:
        info = json.load(response)
    licence = (info.get('cardData') or {}).get('license')
    gated = info.get('gated', False)
    hashes = {}
    if not gated:
        with open_url(f'https://huggingface.co/api/models/{repo}/tree/main?recursive=1') as response:
            for entry in json.load(response):
                if 'lfs' in entry:
                    hashes[entry['path']] = entry['lfs'].get('oid')
    return licence, gated, hashes


def sha256_of(path):
    digest = hashlib.sha256()
    with open(path, 'rb') as fh:
        for block in iter(lambda: fh.read(BLOCK), b''):
            digest.update(block)
    return digest.hexdigest()


def download(url, target):
    """Resume into target.part, then move it into place."""
    part = target.with_name(target.name + '.part')
    done = part.stat().st_size if part.exists() else 0
    headers = {'Range': f'bytes={done}-'} if done else {}
    try:
        response = open_url(url, headers)
    except urllib.error.HTTPError as error:
        if error.code != 416:
            raise
        response = None  # The partial file is already complete.
    if response is not None:
        with response:
            mode = 'ab' if response.status == 206 else 'wb'
            with open(part, mode) as fh:
                for block in iter(lambda: response.read(BLOCK), b''):
                    fh.write(block)
    return part


def source_of(item):
    if 'repo' in item:
        quoted = urllib.parse.quote(item['path'])
        return f"https://huggingface.co/{item['repo']}/resolve/main/{quoted}"
    return item['url']


def wanted(item, only=None, optional=False):
    """Whether this run looks at an entry: --only picks by name, optional or not; otherwise optional entries wait for --optional."""
    if only:
        return only.lower() in item['file'].lower()
    return optional or not item.get('optional', False)


def process(item, check_only):
    target = COMFY / 'ComfyUI' / 'models' / item['folder'] / item['file']
    expected = item.get('sha256')
    if 'repo' in item:
        licence, gated, hashes = hub_info(item['repo'])
        if gated:
            print(f"GATED    {item['file']}: accept the terms at https://huggingface.co/{item['repo']} first; "
                  f"then save it to {target} yourself. This script handles no credentials.")
            return 'gated' if not target.exists() else 'present'
        if licence != item['licence']:
            print(f"REFUSED  {item['file']}: the Hub says the licence is {licence!r}, the manifest expects {item['licence']!r}. "
                  'Read it and update the manifest by hand.')
            return 'refused'
        expected = hashes.get(item['path'], expected)
    if target.exists():
        print(f"PRESENT  {item['file']}")
        return 'present'
    if check_only:
        print(f"MISSING  {item['file']} ({item['licence']})")
        return 'missing'
    target.parent.mkdir(parents=True, exist_ok=True)
    print(f"FETCHING {item['file']} ({item['licence']}) from {source_of(item)}", flush=True)
    part = download(source_of(item), target)
    actual = sha256_of(part)
    if expected and actual != expected:
        print(f"BAD HASH {item['file']}: got {actual}, expected {expected}. Partial file left at {part}.")
        return 'bad-hash'
    part.replace(target)
    print(f"OK       {item['file']} sha256 {actual}{'' if expected else ' (nothing to compare with: record it in the manifest)'}", flush=True)
    return 'fetched'


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('--check', action='store_true', help='report what is present without downloading')
    parser.add_argument('--only', help='only the files whose name contains this text, optional ones included')
    parser.add_argument('--optional', action='store_true', help='include the models marked optional')
    args = parser.parse_args(argv)
    results = {}
    for item in json.loads(MANIFEST.read_text(encoding='utf-8'))['models']:
        if not wanted(item, args.only, args.optional):
            continue
        try:
            results[item['file']] = process(item, args.check)
        except (urllib.error.URLError, OSError) as error:
            print(f"FAILED   {item['file']}: {error}")
            results[item['file']] = 'failed'
    print(json.dumps(results, indent=2))
    return 1 if any(value in ('refused', 'bad-hash', 'failed') for value in results.values()) else 0


if __name__ == '__main__':
    sys.exit(main())
