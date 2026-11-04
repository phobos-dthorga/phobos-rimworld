"""Moved to scripts/art/export_sprite.py; this keeps the radio folder's command working. Needs Pillow."""
from pathlib import Path
import runpy

runpy.run_path(str(Path(__file__).resolve().parents[2] / 'scripts' / 'art' / 'export_sprite.py'), run_name='__main__')
