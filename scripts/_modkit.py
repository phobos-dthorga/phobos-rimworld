"""Puts the pinned phobos-modkit submodule's Python package on the import path.

phobos-modkit (https://github.com/phobos-dthorga/phobos-modkit) holds the shared, game-free tools; this repository
pins it at external/phobos-modkit. A fix to a shared tool goes there first, then the pin moves here.
"""
from pathlib import Path
import sys

PACKAGE_ROOT = Path(__file__).resolve().parents[1] / 'external' / 'phobos-modkit' / 'python'


class MissingModKit(RuntimeError):
    pass


def ensure(package_root=PACKAGE_ROOT):
    """Make `import phobos_modkit` work, or say how to fetch the submodule."""
    if not (Path(package_root) / 'phobos_modkit' / '__init__.py').is_file():
        raise MissingModKit('phobos-modkit is missing; fetch it with:  git submodule update --init')
    if str(package_root) not in sys.path:
        sys.path.insert(0, str(package_root))
