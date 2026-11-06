"""Add art files to an archive branch on GitHub, without touching the working tree or the current branch.

    python scripts/art/archive_branch.py art-masters manifest.json --message "Masters of the 7 Nov picks" [--push]

The owner keeps art that the mods don't ship apart from main (owner, 7 Nov 2026): full-size masters on `art-masters`, drawings and
images kept only as references or for comparison on `art-references`. The manifest is a JSON list of {"source": path, "dest": path
in the branch}. Each run adds one commit on top of the branch's tip (or starts the branch with no history if it doesn't exist), so
the branch only ever grows, and a push is a plain fast-forward, never forced.

Refused, whatever the manifest says: the game's sprites (.local/references), the private style model and its dataset, and model
files. Absolute machine paths inside JSON sidecars are replaced with a note, so no path from this PC is published.
"""
import argparse
import json
import os
import re
import subprocess
import sys
import tempfile
from pathlib import Path

REFUSED = (
    re.compile(r'(^|/)\.local/references(/|$)'),  # The game's own sprites, extracted for reference only.
    re.compile(r'lora-experiments/[^/]+/dataset(/|$)'),  # The private style model's training images.
    re.compile(r'\.(safetensors|ckpt|pt|gguf)$', re.IGNORECASE),  # Model files.
)
ABSOLUTE = re.compile(r'^([A-Za-z]:[\\/]|/(Users|home|c|d)/)')
PRIVATE_NOTE = '(a path on the owner\'s PC, not published)'


def refused(path):
    text = str(path).replace('\\', '/')
    return any(pattern.search(text) for pattern in REFUSED)


def scrub(value):
    """A JSON value with every absolute path string replaced."""
    if isinstance(value, dict):
        return {key: scrub(item) for key, item in value.items()}
    if isinstance(value, list):
        return [scrub(item) for item in value]
    if isinstance(value, str) and ABSOLUTE.match(value):
        return PRIVATE_NOTE
    return value


def content_of(source):
    data = Path(source).read_bytes()
    if Path(source).suffix.lower() == '.json':
        data = (json.dumps(scrub(json.loads(data.decode('utf-8'))), indent=2) + '\n').encode('utf-8')
    return data


def git(args, repo, env=None, data=None):
    result = subprocess.run(['git', *args], cwd=repo, env=env, input=data, capture_output=True, check=False)
    if result.returncode != 0:
        raise SystemExit(f'git {" ".join(args)} failed: {result.stderr.decode("utf-8", "replace").strip()}')
    return result.stdout.decode('utf-8').strip()


def tip(repo, branch):
    """The branch's newest commit, local first, then the fetched remote; None when it has never existed."""
    for ref in (f'refs/heads/{branch}', f'refs/remotes/origin/{branch}'):
        result = subprocess.run(['git', 'rev-parse', '--verify', '--quiet', ref], cwd=repo, capture_output=True, check=False)
        if result.returncode == 0:
            return result.stdout.decode('utf-8').strip()
    return None


def archive(repo, branch, entries, message):
    """Commit the entries on top of the branch and move the local branch to it. Returns the new commit."""
    for entry in entries:
        if refused(entry['source']):
            raise SystemExit(f'refused: {entry["source"]} is game art, the private model or a model file')
        if not Path(repo, entry['source']).is_file():
            raise SystemExit(f'missing: {entry["source"]}')
    base = tip(repo, branch)
    handle, index = tempfile.mkstemp(prefix='archive-index-')
    os.close(handle)
    os.remove(index)
    env = dict(os.environ, GIT_INDEX_FILE=index)
    try:
        if base:
            git(['read-tree', base], repo, env)
        else:
            git(['read-tree', '--empty'], repo, env)
        for entry in entries:
            blob = git(['hash-object', '-w', '--stdin'], repo, env, content_of(Path(repo, entry['source'])))
            git(['update-index', '--add', '--cacheinfo', f'100644,{blob},{entry["dest"]}'], repo, env)
        tree = git(['write-tree'], repo, env)
    finally:
        if os.path.exists(index):
            os.remove(index)
    if base and git(['rev-parse', f'{base}^{{tree}}'], repo) == tree:
        print(f'{branch}: nothing new')
        return base
    commit = git(['commit-tree', tree, *(['-p', base] if base else []), '-F', '-'], repo, data=message.encode('utf-8'))
    git(['update-ref', f'refs/heads/{branch}', commit, *([base] if base else [])], repo)
    print(f'{branch}: {commit[:7]}, {len(entries)} files')
    return commit


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('branch')
    parser.add_argument('manifest')
    parser.add_argument('--message', required=True)
    parser.add_argument('--push', action='store_true', help='push the branch to origin (a fast-forward, never forced)')
    args = parser.parse_args(argv)
    repo = git(['rev-parse', '--show-toplevel'], '.')
    entries = json.loads(Path(args.manifest).read_text(encoding='utf-8'))
    archive(repo, args.branch, entries, args.message)
    if args.push:
        git(['push', 'origin', f'refs/heads/{args.branch}:refs/heads/{args.branch}'], repo)
        print(f'pushed {args.branch}')
    return 0


if __name__ == '__main__':
    sys.exit(main())
