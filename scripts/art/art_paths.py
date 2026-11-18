"""Where the art tools keep their own working files: the background AI's masks, Segment Anything's parts, staging and review pages.

The owner's direction of 19 Nov 2026: "Just use K: from now on instead of D:" (D: had about 2% free). The folder comes from, in
order: the PHOBOS_ART_WORK environment variable, "artWorkPath" in .local/install-settings.json (as deploy.py reads its paths), the
central settings file (D'thorga Companion's settings.json, read through the shared tools when they are there), and .local/art in this
repository. New files go to that folder; files made before it moved are still read from .local/art, so
nothing made earlier is lost or needs copying. Shipped textures and committed files stay in the repository.
"""
import json
import os
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[2]
SETTINGS_FILE = ROOT / '.local' / 'install-settings.json'
OLD = ROOT / '.local' / 'art'
ENV_VAR = 'PHOBOS_ART_WORK'
KEY = 'artWorkPath'


def work_root(environ=None, settings_file=SETTINGS_FILE):
    """The art tools' working folder: environment, then install-settings.json, then .local/art."""
    environ = os.environ if environ is None else environ
    if environ.get(ENV_VAR):
        return Path(environ[ENV_VAR])
    settings_file = Path(settings_file)
    if settings_file.is_file():
        configured = json.loads(settings_file.read_text(encoding='utf-8')).get(KEY)
        if configured:
            return Path(configured)
    return _central(environ) or OLD


def _central(environ):
    """artWorkPath from the central settings file; None without one, or without the shared tools to read it."""
    try:
        sys.path.insert(0, str(ROOT / 'scripts'))
        import _modkit
        _modkit.ensure()
        from phobos_modkit.paths import central_settings_file, read_settings_file
    except Exception:  # noqa: BLE001 (the art tools work without the shared tools; only the central layer goes without)
        return None
    configured = read_settings_file(central_settings_file(environ)).get(KEY)
    return Path(configured) if configured else None


def work(*parts, environ=None, settings_file=SETTINGS_FILE):
    """A folder or file under the working folder, for writing."""
    return work_root(environ, settings_file).joinpath(*parts)


def find(*parts, environ=None, settings_file=SETTINGS_FILE):
    """A file or folder to read: under the working folder if it is there, else where it was made before the move, else None."""
    for base in (work_root(environ, settings_file), OLD):
        candidate = base.joinpath(*parts)
        if candidate.exists():
            return candidate
    return None
