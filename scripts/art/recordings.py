"""Real recordings of a real machine, from their public pages to candidates on the listening page.

    python scripts/art/recordings.py fetch assets/phobos-sounds/recordings/osprey.json [--record]
    python scripts/art/recordings.py cut assets/phobos-sounds/recordings/osprey.json
    python scripts/art/sound_review.py <work folder>/recordings/osprey/candidates

The owner's rule of 19 Nov 2026 (assets/AGENTS.md): a real, named machine may sound like itself from real recordings that are public
domain, CC0 or CC BY with credit. A manifest names each source (its page, the file it links, its credit and SHA-256) and each cut
(a role, a start and a length, whether it loops). fetch downloads each source into its own folder under the art tools' working
folder (scripts/art/art_paths.py; K: on the owner's PC), refuses a DVIDS page that does not say PUBLIC DOMAIN, checks the file's
SHA-256 against the manifest (--record writes it there instead, for a new source), and keeps its sound as lossless FLAC. cut writes
each cut as a FLAC with a record beside it, in the folder layout sound_review.py reads (one folder per role), so recordings are
judged on the same page, with the same spectrograms and flags, as generated sounds. Downloads are untrusted data: only ffmpeg reads
them, and nothing in them is run. Uses no graphics card. ffmpeg as sound_review.py finds it.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys
import time
import urllib.request

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import art_paths  # noqa: E402
from sound_review import find_ffmpeg  # noqa: E402

USER_AGENT = 'Mozilla/5.0 (compatible; phobos-rimworld recordings tool)'
PUBLIC_DOMAIN = re.compile(r'PUBLIC\s+DOMAIN', re.I)
CHUNK = 1 << 20


def sha256_file(path):
    digest = hashlib.sha256()
    with open(path, 'rb') as handle:
        for block in iter(lambda: handle.read(CHUNK), b''):
            digest.update(block)
    return digest.hexdigest()


def get(url, timeout=120):
    return urllib.request.urlopen(urllib.request.Request(url, headers={'User-Agent': USER_AGENT}), timeout=timeout)


def page_is_public_domain(html):
    """Whether a DVIDS page marks its media PUBLIC DOMAIN (its usage line); a page without it is refused."""
    return bool(PUBLIC_DOMAIN.search(html))


def set_folder(manifest):
    return art_paths.work('recordings', manifest['set'])


def cut_name(cut):
    """A stable file name for a cut: role, source and start in tenths of a second."""
    return f'{cut["role"]}-{cut["source"]}-{int(round(float(cut["start"]) * 10)):05d}'


def cut_record(manifest, cut):
    """The record written beside a cut: what sound_review.py reads (loop, seconds) and what an export's record must carry."""
    source = manifest['sources'][cut['source']]
    return {'kind': 'recording', 'loop': bool(cut.get('loop')), 'seconds': cut['seconds'], 'start': cut['start'],
            'role': cut['role'], 'note': cut.get('note', ''), 'source_page': source['page'], 'source_title': source['title'],
            'credit': source['credit'], 'licence': manifest['licence'], 'source_sha256': source.get('sha256', '')}


def fetch(manifest_path, record=False):
    manifest = json.loads(Path(manifest_path).read_text(encoding='utf-8'))
    exe = find_ffmpeg()
    folder = set_folder(manifest)
    changed = False
    for key, source in manifest['sources'].items():
        html = get(source['page'], 60).read().decode('utf-8', 'replace')
        if not page_is_public_domain(html):
            print(f'{key}: refused, its page does not say PUBLIC DOMAIN ({source["page"]})')
            continue
        target = folder / 'downloads' / key / source['file_url'].rsplit('/', 1)[1]
        target.parent.mkdir(parents=True, exist_ok=True)
        if not target.is_file():
            with get(source['file_url']) as response, open(target, 'wb') as handle:
                for block in iter(lambda: response.read(CHUNK), b''):
                    handle.write(block)
        digest = sha256_file(target)
        if record and source.get('sha256') != digest:
            source['sha256'], changed = digest, True
        elif source.get('sha256') != digest:
            print(f'{key}: refused, SHA-256 {digest} is not the manifest\'s {source.get("sha256")}; --record writes it if the new file is wanted')
            continue
        audio = folder / 'audio' / f'{key}.flac'
        audio.parent.mkdir(parents=True, exist_ok=True)
        if not audio.is_file():
            subprocess.run([exe, '-v', 'error', '-y', '-i', str(target), '-vn', '-c:a', 'flac', str(audio)], check=True)
        print(f'{key}: {target.stat().st_size // 1_000_000} MB, audio at {audio}')
    if changed:
        Path(manifest_path).write_text(json.dumps(manifest, indent=1, ensure_ascii=False) + '\n', encoding='utf-8')
        print(f'wrote the new hashes into {manifest_path}')
    (folder / 'fetched.json').write_text(json.dumps({'when': time.strftime('%Y-%m-%d %H:%M'), 'manifest': str(manifest_path)}),
                                         encoding='utf-8')


def cut(manifest_path):
    manifest = json.loads(Path(manifest_path).read_text(encoding='utf-8'))
    exe = find_ffmpeg()
    folder = set_folder(manifest)
    made = 0
    for item in manifest['cuts']:
        audio = folder / 'audio' / f'{item["source"]}.flac'
        if not audio.is_file():
            print(f'{cut_name(item)}: skipped, {audio} is missing (run fetch first)')
            continue
        out = folder / 'candidates' / item['role'] / f'{cut_name(item)}.flac'
        out.parent.mkdir(parents=True, exist_ok=True)
        subprocess.run([exe, '-v', 'error', '-y', '-ss', str(item['start']), '-t', str(item['seconds']), '-i', str(audio),
                        '-c:a', 'flac', str(out)], check=True)
        out.with_suffix('.json').write_text(json.dumps(cut_record(manifest, item), indent=1, ensure_ascii=False), encoding='utf-8')
        made += 1
    print(f'{made} cuts under {folder / "candidates"}; next: python scripts/art/sound_review.py "{folder / "candidates"}"')


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.split('\n\n')[0])
    parser.add_argument('command', choices=('fetch', 'cut'))
    parser.add_argument('manifest')
    parser.add_argument('--record', action='store_true', help='fetch: write each file\'s SHA-256 into the manifest')
    args = parser.parse_args(argv)
    if args.command == 'fetch':
        fetch(args.manifest, args.record)
    else:
        cut(args.manifest)


if __name__ == '__main__':
    main()
