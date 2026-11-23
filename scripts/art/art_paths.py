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
# The folders outside the repository the art tools use, each a setting (config/settings.json) with today's folder as its default,
# so phobos-modkit's safe-copies can find them by the same keys (21 Nov 2026).
LORA_WORK = ('loraWorkPath', 'PHOBOS_LORA_WORK', Path(r'C:\AI\lora-experiments'))
COMFY = ('comfyUiPath', 'COMFYUI_PATH', Path(r'C:\AI\ComfyUI\ComfyUI_windows_portable'))
AI_TOOLKIT = ('aiToolkitPath', 'PHOBOS_AI_TOOLKIT', Path(r'C:\AI\ai-toolkit'))


def setting(key, env_var, default, environ=None, settings_file=SETTINGS_FILE):
    """A folder setting: the environment, then install-settings.json, then the central settings file, then the default."""
    environ = os.environ if environ is None else environ
    if environ.get(env_var):
        return Path(environ[env_var])
    settings_file = Path(settings_file)
    if settings_file.is_file():
        configured = json.loads(settings_file.read_text(encoding='utf-8')).get(key)
        if configured:
            return Path(configured)
    return _central(key, environ) or default


def work_root(environ=None, settings_file=SETTINGS_FILE):
    """The art tools' working folder: environment, then install-settings.json, then the central file, then .local/art."""
    return setting(KEY, ENV_VAR, OLD, environ, settings_file)


def lora_work_root(environ=None, settings_file=SETTINGS_FILE):
    """Where the style-model trainings live: outside the repository and the synced drive, as some are made from the game's art."""
    return setting(*LORA_WORK, environ, settings_file)


def comfy_root(environ=None, settings_file=SETTINGS_FILE):
    """ComfyUI's portable folder."""
    return setting(*COMFY, environ, settings_file)


def ai_toolkit_root(environ=None, settings_file=SETTINGS_FILE):
    """The ai-toolkit trainer's folder."""
    return setting(*AI_TOOLKIT, environ, settings_file)


def _central(key, environ):
    """A key from the central settings file; None without one, or without the shared tools to read it."""
    try:
        sys.path.insert(0, str(ROOT / 'scripts'))
        import _modkit
        _modkit.ensure()
        from phobos_modkit.paths import central_settings_file, read_settings_file
    except Exception:  # noqa: BLE001 (the art tools work without the shared tools; only the central layer goes without)
        return None
    configured = read_settings_file(central_settings_file(environ)).get(key)
    return Path(configured) if configured else None


def record_path(beside, root=ROOT, make=True):
    """Where a record goes, given the path it would have beside its picture or sound: the same place under assets/records/ for a
    file the mods ship (the owner's choice of 23 Nov 2026, so deploy no longer copies records into the game's Mods folder), else
    unchanged. The folders are made. The mapping is config/art-audit.json's records.mirrors, through the shared tools' art audit,
    which looks for records in the same place. `make=False` only works out the path."""
    sys.path.insert(0, str(ROOT / 'scripts'))
    import _modkit
    _modkit.ensure()
    from phobos_modkit import artaudit
    beside = Path(beside).resolve()
    try:
        relative = beside.relative_to(Path(root).resolve()).as_posix()
    except ValueError:
        return beside
    target = Path(root) / artaudit.record_home(relative, artaudit.mirrors_of(root))
    if make:
        target.parent.mkdir(parents=True, exist_ok=True)
    return target


def records_under(folder, pattern='*.record.md', root=ROOT):
    """Every record for the files in `folder` (a folder the mods ship, or any other), wherever it is kept: beside them, then
    under assets/records/."""
    found = sorted(Path(folder).glob(pattern))
    mirrored = record_path(Path(folder) / '_', root, make=False).parent
    if mirrored.resolve() != Path(folder).resolve():
        found += sorted(mirrored.glob(pattern))
    return found


def shipped_records(pattern='*.record.md', root=ROOT):
    """Every record of a file the mods ship: under assets/records/, and any still beside its file under mods/."""
    return sorted([*(Path(root) / 'mods').rglob(pattern), *(Path(root) / 'assets' / 'records').rglob(pattern)])


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
