"""Fault-injection checks for document registration, traceability and local links."""
import copy
import importlib.util
import subprocess
import tempfile
import unittest
from pathlib import Path

SPEC = importlib.util.spec_from_file_location("docs_tool", Path(__file__).resolve().parents[1] / "docs.py")
DOCS = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(DOCS)


class DocumentContractTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.data = {
            "schema_version": 1,
            "kinds": {
                "index": {"paths": ["docs/catalog.md", "docs/README.md", "docs/documentation.md", "docs/design/README.md"], "purpose": "Navigation."},
                "foundation": {"paths": ["docs/foundation.md"], "purpose": "Product intent."},
                "design": {"paths": ["docs/design/*.md"], "purpose": "Rules."},
                "task": {"paths": ["docs/planning/tasks/*.md"], "purpose": "Bounded work."},
                "evidence": {"paths": ["docs/playtests/*.md"], "purpose": "Observed results."},
            },
            "documents": [
                self.entry("catalog", "docs/catalog.md", "index", []),
                self.entry("foundation", "docs/foundation.md", "foundation", []),
                self.entry("design", "docs/design/combat.md", "design", ["foundation"]),
                self.entry("task", "docs/planning/tasks/GDJ-99.md", "task", ["design"]),
                self.entry("evidence", "docs/playtests/result.md", "evidence", ["task"]),
                self.entry("map", "docs/README.md", "index", []),
                self.entry("contract", "docs/documentation.md", "index", []),
                self.entry("gdd", "docs/design/README.md", "index", []),
            ],
            "resource_groups": [],
        }
        for doc in self.data["documents"]:
            self.write(doc["path"], "# " + doc["id"] + "\n")
        self.write(DOCS.REGISTRY, "{}")
        self.write("docs/design/combat.md", "# Combat\n[Intent](../foundation.md#foundation)\n")
        self.refresh()

    @staticmethod
    def entry(doc_id, path, kind, upstream):
        return dict(id=doc_id, path=path, kind=kind, purpose="Fixture purpose.", maintenance="current", upstream=upstream)

    def write(self, path, text):
        target = self.root / path
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text(text, encoding="utf-8")

    def refresh(self):
        self.write(DOCS.CATALOG, DOCS.catalogue(self.data))

    def audit(self, **kwargs):
        files = [p.relative_to(self.root).as_posix() for p in self.root.rglob("*") if p.is_file()]
        return DOCS.audit(self.root, self.data, files, **kwargs)[0]

    def assertFailure(self, text):
        self.assertTrue(any(text in error for error in self.audit()), self.audit())

    def test_valid_connected_documents_and_trace(self):
        self.assertEqual([], self.audit())
        result = DOCS.trace(self.data, "docs/planning/tasks/GDJ-99.md")
        self.assertIn("foundation: docs/foundation.md", result)
        self.assertIn("evidence: docs/playtests/result.md", result)

    def test_new_unregistered_file_is_rejected(self):
        self.write("docs/design/orphan.md", "# Undocumented addition\n")
        self.assertFailure("Unregistered document: docs/design/orphan.md")

    def test_missing_registered_file_is_rejected(self):
        (self.root / "docs/design/combat.md").unlink()
        self.assertFailure("Missing registered document")

    def test_duplicate_ids_and_paths_are_rejected(self):
        self.data["documents"].append(copy.deepcopy(self.data["documents"][2]))
        self.assertFailure("Duplicate ID")
        self.assertFailure("Duplicate path")

    def test_wrong_location_is_rejected(self):
        self.data["documents"][2]["kind"] = "task"
        self.assertFailure("Wrong location for task")

    def test_unknown_and_missing_upstream_are_rejected(self):
        self.data["documents"][2]["upstream"] = ["invented-id"]
        self.assertFailure("Unknown upstream")
        self.data["documents"][2]["upstream"] = []
        self.assertFailure("Missing upstream basis")

    def test_broken_link_and_heading_are_rejected(self):
        self.write("docs/design/combat.md", "# Combat\n[Missing](absent.md)\n[Wrong heading](../foundation.md#absent)\n")
        self.assertFailure("Broken path")
        self.assertFailure("Broken anchor")

    def test_fenced_examples_and_reference_links(self):
        self.write("docs/design/combat.md", "# Combat\n```md\n[Example](not-an-actual-file.md)\n```\n[Intent][source]\n[source]: ../foundation.md#foundation\n")
        self.assertEqual([], self.audit())
        self.write("docs/design/combat.md", "[Intent][source]\n[source]: ../foundation.md#missing\n")
        self.assertFailure("Broken anchor")

    def test_stale_catalogue_and_regeneration_preview(self):
        self.data["documents"][2]["purpose"] = "Changed reason for the document."
        self.assertFailure("Catalogue is stale")
        self.assertEqual([], self.audit(check_catalog=False))
        self.refresh()
        self.assertEqual([], self.audit())

    def test_raw_resources_need_one_owner(self):
        self.write("docs/playtests/evidence/result.json", "{}")
        self.assertFailure("exactly one owner (0)")
        group = dict(glob="docs/playtests/evidence/*", owner="evidence", purpose="Result inputs.")
        self.data["resource_groups"] = [group]
        self.refresh()
        self.assertEqual([], self.audit())
        self.data["resource_groups"].append(copy.deepcopy(group))
        self.assertFailure("exactly one owner (2)")

    def test_legacy_encoding_is_reported(self):
        (self.root / "docs/design/combat.md").write_bytes(b"# Combat\nrange 1\x962")
        self.assertFailure("Document must be UTF-8")

    def test_outside_path_is_rejected(self):
        self.data["documents"][2]["path"] = "../private.md"
        self.assertFailure("path escapes repository")

    def test_feedback_cycles_stop_at_previously_seen_record(self):
        self.data["documents"][1]["upstream"] = ["evidence"]
        result = DOCS.trace(self.data, "task")
        self.assertIn("task: docs/planning/tasks/GDJ-99.md (already shown)", result)
        self.assertLess(len(result.splitlines()), 15)

    def test_git_inventory_includes_untracked_but_not_ignored(self):
        subprocess.run(["git", "init", "--quiet", str(self.root)], check=True, capture_output=True)
        self.write(".gitignore", "ignored.md\n")
        self.write("ignored.md", "# Generated cache\n")
        self.write("docs/design/untracked.md", "# New proposal\n")
        found = DOCS.inventory(self.root)
        self.assertIn("docs/design/untracked.md", found)
        self.assertNotIn("ignored.md", found)


if __name__ == "__main__":
    unittest.main()
