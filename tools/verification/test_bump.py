"""Success: all three versions update together, invalid inputs write nothing."""
import importlib.util
from pathlib import Path
import tempfile
import unittest

SPEC = importlib.util.spec_from_file_location('bump', Path(__file__).resolve().parents[1] / 'bump.py')
bump = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(bump)


class VersionChecks(unittest.TestCase):
    def setUp(self):
        scratch = bump.ROOT / 'artifacts/verification/test-temp'
        scratch.mkdir(parents=True, exist_ok=True)
        self.temp = tempfile.TemporaryDirectory(dir=scratch)
        assert Path(self.temp.name).resolve().is_relative_to(scratch.resolve())
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        fixtures = ['const string Version = "1.2.0"; const string BuildKeywords = "old";',
                    '<Project><Version>1.2.0</Version></Project>', '{"version_number": "1.2.0"}']
        for name, content in zip(bump.PATTERNS, fixtures):
            path = self.root / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(content, encoding='utf-8')

    def snapshot(self):
        return {name: (self.root / name).read_bytes() for name in bump.PATTERNS}

    def test_parity_and_escaped_keywords(self):
        bump.bump('1.3.4', 'quote" and \\ path', self.root)
        self.assertEqual(bump.check(self.root), '1.3.4')
        self.assertIn('quote\\" and \\\\ path', (self.root / 'HollowSaintMod/Plugin.cs').read_text())

    def test_invalid_input_does_not_write(self):
        before = self.snapshot()
        for version in ['1.2', '1.2.3-beta', '01.2.3', '1.2.3\n']:
            with self.assertRaises(ValueError):
                bump.bump(version, 'test', self.root)
            self.assertEqual(self.snapshot(), before)

    def test_missing_late_input_does_not_partially_update(self):
        (self.root / 'HollowSaintMod/Package/manifest.json').write_text('{}')
        before = self.snapshot()
        with self.assertRaises(ValueError):
            bump.bump('1.3.0', 'test', self.root)
        self.assertEqual(self.snapshot(), before)

    def test_check_detects_drift_without_writes(self):
        path = self.root / 'HollowSaintMod/HollowSaint.csproj'
        path.write_text('<Project><Version>9.0.0</Version></Project>')
        before = self.snapshot()
        with self.assertRaises(ValueError):
            bump.check(self.root)
        self.assertEqual(self.snapshot(), before)
