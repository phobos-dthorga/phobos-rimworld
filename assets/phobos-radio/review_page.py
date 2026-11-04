"""Moved to scripts/art/review_page.py; this keeps the radio folder's command working."""
from pathlib import Path
import runpy
import sys

ROOT = Path(__file__).resolve().parents[2]
sys.argv[1:1] = [str(ROOT / 'assets' / 'phobos-radio' / 'generated'), '--title', 'Radio framework']
runpy.run_path(str(ROOT / 'scripts' / 'art' / 'review_page.py'), run_name='__main__')
