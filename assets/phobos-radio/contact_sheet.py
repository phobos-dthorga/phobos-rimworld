"""Moved to scripts/art/contact_sheet.py; this keeps the radio folder's command working. Needs Pillow."""
from pathlib import Path
import runpy
import sys

ROOT = Path(__file__).resolve().parents[2]
sys.argv[1:1] = [str(ROOT / 'assets' / 'phobos-radio' / 'generated')]
runpy.run_path(str(ROOT / 'scripts' / 'art' / 'contact_sheet.py'), run_name='__main__')
