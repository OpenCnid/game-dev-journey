"""Validate the document registry, local references and generated catalogue (stdlib only)."""
from __future__ import annotations

import argparse
import fnmatch
import json
import re
import subprocess
import sys
from pathlib import Path
from urllib.parse import unquote, urlsplit

REGISTRY = "docs/registry.json"
CATALOG = "docs/catalog.md"
MAINTENANCE = {"current", "draft", "mixed", "historical", "template"}


def inventory(root: Path) -> list[str]:
    result = subprocess.run(
        ["git", "-C", str(root), "ls-files", "-z", "--cached", "--others", "--exclude-standard"],
        check=True, capture_output=True,
    )
    return sorted(set(result.stdout.decode("utf-8").rstrip("\0").split("\0")) - {""})


def safe_path(root: Path, value: str) -> Path:
    if not isinstance(value, str) or not value or "\\" in value or ":" in value:
        raise ValueError(f"not a repository-relative path: {value!r}")
    target = (root / value).resolve()
    if not target.is_relative_to(root.resolve()) or Path(value).is_absolute():
        raise ValueError(f"path escapes repository: {value}")
    return target


def without_fences(text: str) -> str:
    lines = []
    fence = None
    for line in text.splitlines():
        match = re.match(r"^\s*(`{3,}|~{3,})", line)
        if match:
            marker = match.group(1)
            if fence is None:
                fence = marker
            elif marker[0] == fence[0] and len(marker) >= len(fence):
                fence = None
            continue
        if fence is None:
            lines.append(line)
    return "\n".join(lines)


def anchors(text: str) -> set[str]:
    text = without_fences(text)
    result = set(re.findall(r'<a\s+(?:id|name)=["\']([^"\']+)["\']', text))
    counts: dict[str, int] = {}
    for title in re.findall(r"^#{1,6}\s+(.+?)\s*#*\s*$", text, re.M):
        title = re.sub(r"\[([^]]+)\]\([^)]*\)", r"\1", title)
        title = re.sub(r"<[^>]*>", "", title)
        slug = re.sub(r"[^\w\- ]", "", title.lower()).replace(" ", "-")
        count = counts.get(slug, 0)
        counts[slug] = count + 1
        result.add(slug + (f"-{count}" if count else ""))
    return result


def local_links(text: str):
    text = without_fences(text)
    pattern = r'!?\[[^\]\n]*\]\(\s*(<[^>]+>|[^\s)]+)(?:\s+"[^"]*")?\s*\)'
    targets = re.findall(pattern, text)
    targets += re.findall(r"^\s*\[[^]]+\]:\s*(<[^>]+>|\S+)", text, re.M)
    for target in targets:
        target = target.strip("<>")
        if re.match(r"^[a-zA-Z][a-zA-Z0-9+.-]*:", target) or target.startswith("//"):
            continue
        parts = urlsplit(target)
        yield unquote(parts.path), unquote(parts.fragment), target


def catalogue(data: dict) -> str:
    docs = sorted(data["documents"], key=lambda d: (d["kind"], d["id"]))
    by_id = {d["id"]: d for d in docs}

    def link(doc_id):
        path = by_id[doc_id]["path"]
        relative = path[5:] if path.startswith("docs/") else "../" + path
        return f"[{doc_id}]({relative})"

    lines = ["# Document catalogue", "", "Generated from [registry.json](registry.json) by `python tools/docs.py --write-index`. Edit the registry, then regenerate this file.", "", "[Start here](README.md) · [Placement and traceability rules](documentation.md) · [GDD chapters](design/README.md)", "", "Maintenance describes the document, not product approval or Linear status. Upstream links explain its basis; the command `python tools/docs.py --trace <id>` also shows downstream consumers. Read dates, sections and source revisions before treating a historical rule as current.", ""]
    for kind, rule in data["kinds"].items():
        entries = [d for d in docs if d["kind"] == kind]
        if not entries:
            continue
        lines += [f"## {kind}", "", rule["purpose"], "", "| ID and document | Why it exists | Maintenance | Upstream basis |", "| --- | --- | --- | --- |"]
        for doc in entries:
            refs = ", ".join(link(x) for x in doc["upstream"]) or "Entry / original record"
            lines.append(f"| {link(doc['id'])} | {doc['purpose'].replace('|', '/')} | {doc['maintenance']} | {refs} |")
        lines.append("")
    lines += ["## Supporting files", "", "Raw files are indexed by their owning report or guide rather than hundreds of separate navigation entries. Markdown within these groups still requires its own document entry.", "", "| Location | Owner | Why it lives here |", "| --- | --- | --- |"]
    for group in data["resource_groups"]:
        lines.append(f"| `{group['glob']}` | {link(group['owner'])} | {group['purpose']} |")
    return "\n".join(lines) + "\n"


def audit(root: Path, data: dict, files: list[str], check_catalog=True) -> tuple[list[str], int, int]:
    errors: list[str] = []
    if data.get("schema_version") != 1:
        return ["Unsupported registry schema_version; expected 1"], 0, 0
    kinds = data.get("kinds", {})
    docs = data.get("documents", [])
    if not isinstance(kinds, dict) or not kinds or not isinstance(docs, list):
        return ["Registry needs kinds and documents"], 0, 0
    by_id = {}
    by_path = {}
    for doc in docs:
        if not isinstance(doc, dict):
            errors.append("Each document must be an object")
            continue
        missing = {"id", "path", "kind", "purpose", "maintenance", "upstream"} - doc.keys()
        if missing:
            errors.append(f"Incomplete entry {doc.get('id', '?')}: {sorted(missing)}")
            continue
        doc_id, path = doc["id"], doc["path"]
        if not isinstance(doc_id, str) or not re.fullmatch(r"[a-z0-9][a-z0-9-]*", doc_id):
            errors.append(f"Invalid stable ID: {doc_id!r}")
            continue
        if doc_id in by_id:
            errors.append(f"Duplicate ID: {doc_id}")
        if path in by_path:
            errors.append(f"Duplicate path: {path}")
        by_id[doc_id], by_path[path] = doc, doc
        try:
            target = safe_path(root, path)
            if not target.is_file():
                errors.append(f"Missing registered document: {path}")
        except ValueError as exc:
            errors.append(str(exc))
        rule = kinds.get(doc["kind"])
        if not isinstance(rule, dict) or not isinstance(rule.get("paths"), list):
            errors.append(f"Unknown/malformed kind: {doc['kind']}")
        elif not any(fnmatch.fnmatchcase(path, pat) for pat in rule["paths"]):
            errors.append(f"Wrong location for {doc['kind']}: {path}")
        if doc["maintenance"] not in MAINTENANCE:
            errors.append(f"Invalid maintenance state: {doc_id}")
        if not isinstance(doc["purpose"], str) or not doc["purpose"].strip():
            errors.append(f"Missing purpose: {doc_id}")
        if not isinstance(doc["upstream"], list) or any(not isinstance(x, str) for x in doc["upstream"]):
            errors.append(f"Upstream must be a list of IDs: {doc_id}")
        elif doc["kind"] in {"design", "task", "evidence"} and not doc["upstream"]:
            errors.append(f"Missing upstream basis: {doc_id}")
    for doc in by_id.values():
        if not isinstance(doc["upstream"], list):
            continue
        for ref in doc["upstream"]:
            if not isinstance(ref, str) or ref not in by_id:
                errors.append(f"Unknown upstream {ref!r} in {doc['id']}")
            elif ref == doc["id"]:
                errors.append(f"Self reference in {ref}")
    for path in files:
        if path.lower().endswith(".md") and path not in by_path:
            errors.append(f"Unregistered document: {path}")

    resources = [p for p in files if p.startswith("docs/") and not p.lower().endswith(".md") and p != REGISTRY]
    groups = data.get("resource_groups", [])
    for group in groups:
        if not isinstance(group, dict) or not {"glob", "owner", "purpose"} <= group.keys():
            errors.append("Resource group needs glob, owner and purpose")
        elif group["owner"] not in by_id or not group["purpose"]:
            errors.append(f"Invalid resource owner/purpose: {group['glob']}")
    if errors:
        return errors, 0, len(resources)
    for path in resources:
        matches = [g for g in groups if fnmatch.fnmatchcase(path, g["glob"])]
        if len(matches) != 1:
            errors.append(f"Supporting file needs exactly one owner ({len(matches)}): {path}")

    contents = {}
    for path in by_path:
        try:
            contents[path] = safe_path(root, path).read_text(encoding="utf-8-sig")
        except UnicodeError:
            errors.append(f"Document must be UTF-8: {path}")
    if not check_catalog:
        contents[CATALOG] = catalogue(data)
    count = 0
    for path, content in contents.items():
        for relative, anchor, raw in local_links(content):
            count += 1
            target = (root / path).parent / relative if relative else root / path
            target = target.resolve()
            if not target.is_relative_to(root.resolve()):
                errors.append(f"Link escapes repository: {path} -> {raw}")
            elif not target.exists():
                errors.append(f"Broken path: {path} -> {raw}")
            elif anchor and target.suffix.lower() == ".md":
                key = target.relative_to(root.resolve()).as_posix()
                if key not in contents:
                    errors.append(f"Linked Markdown is unregistered/unreadable: {path} -> {raw}")
                elif anchor not in anchors(contents[key]):
                    errors.append(f"Broken anchor: {path} -> {raw}")
    if check_catalog and contents.get(CATALOG) != catalogue(data):
        errors.append("Catalogue is stale: run python tools/docs.py --write-index")
    return errors, count, len(resources)


def trace(data: dict, selected: str) -> str:
    by_id = {d["id"]: d for d in data["documents"]}
    if selected not in by_id:
        selected = next((d["id"] for d in by_id.values() if d["path"] == selected), selected)
    if selected not in by_id:
        raise ValueError(f"Unknown document ID/path: {selected}")
    lines = []

    def walk(doc_id, depth, seen):
        doc = by_id[doc_id]
        repeated = doc_id in seen
        lines.append(f"{'  ' * depth}{doc_id}: {doc['path']}" + (" (already shown)" if repeated else ""))
        if repeated:
            return
        seen.add(doc_id)
        for ref in doc["upstream"]:
            walk(ref, depth + 1, seen)

    lines.append("UPSTREAM BASIS (read dates/revisions; links alone do not establish approval)")
    walk(selected, 0, set())
    lines.append("DIRECT DOWNSTREAM CONSUMERS")
    downstream = sorted(d["id"] for d in by_id.values() if selected in d["upstream"])
    lines.extend(f"{x}: {by_id[x]['path']}" for x in downstream)
    if not downstream:
        lines.append("None registered.")
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--write-index", action="store_true", help="Regenerate docs/catalog.md before checking")
    parser.add_argument("--trace", help="Show upstream basis and downstream consumers by stable ID or path")
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    try:
        data = json.loads((root / REGISTRY).read_text(encoding="utf-8"))
        files = inventory(root)
        errors, count, resources = audit(root, data, files, check_catalog=not args.write_index)
        if errors:
            for error in errors:
                print("ERROR:", error)
            return 1
        if args.write_index:
            (root / CATALOG).write_text(catalogue(data), encoding="utf-8", newline="\n")
            errors, count, resources = audit(root, data, files)
            if errors:
                for error in errors:
                    print("ERROR:", error)
                return 1
        print(f"PASS: {len(data['documents'])} registered documents, {count} local references, {resources} supporting files.")
        if args.trace:
            print(trace(data, args.trace))
        return 0
    except (OSError, ValueError, KeyError, TypeError, subprocess.CalledProcessError) as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
