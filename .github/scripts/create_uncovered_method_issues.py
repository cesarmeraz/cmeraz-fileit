#!/usr/bin/env python3
"""
Scans every C# .App project in the repository for public methods declared in
public classes.  For each method that is not referenced in the companion .Test
project a GitHub issue is created.

Companion test project resolution:
  FileIt.Common.App   →  FileIt.Common.Test   (replace ".App" with ".Test")
  FileIt.SimpleFlow.App  →  FileIt.SimpleFlow.Test

A method is considered *covered* when both the class name and the method name
appear as whole-word identifiers together inside at least one .cs file in the
test project.
"""

from __future__ import annotations

import json
import os
import re
import urllib.error
import urllib.request
from pathlib import Path


GITHUB_API_BASE = "https://api.github.com"

# ---------------------------------------------------------------------------
# Regex patterns
# ---------------------------------------------------------------------------

# Matches a public class declaration (including abstract classes, since their
# subclasses are tested and the methods may be invoked through them).
# Captures the class name.
_CLASS_RE = re.compile(
    r"^\s*public\s+(?:abstract\s+|sealed\s+|static\s+|partial\s+)*class\s+(\w+)",
    re.MULTILINE,
)

# Matches a public instance/static method declaration.
# Captures the method name.
# Deliberately excludes:
#   - constructors  (filtered out by comparing against class names)
#   - properties    (use { get; … } – no opening paren at the end of the line)
#   - fields        (= initialiser – no opening paren)
# Handles nullable return types (e.g. string?, int?) via the optional \??.
_METHOD_RE = re.compile(
    r"^\s*public\s+(?:async\s+|override\s+|virtual\s+|static\s+|new\s+)*"
    r"(?:Task(?:<[^>]+>)?|void|bool\??|byte\??|int\??|long\??|double\??|"
    r"decimal\??|float\??|char\??|string\??|object\??|"
    r"[A-Z]\w*(?:<[^>]+>)?(?:\[\])?\??)\s+"
    r"(\w+)\s*\(",
    re.MULTILINE,
)


# ---------------------------------------------------------------------------
# Project discovery
# ---------------------------------------------------------------------------


def find_app_projects(root: Path) -> list[Path]:
    """Return directories that contain a *.App.csproj project file."""
    return [p.parent for p in root.rglob("*.App.csproj")]


def find_test_project(app_dir: Path) -> Path | None:
    """
    Given an .App project directory return the companion .Test directory, or
    None when no such directory exists.
    """
    test_name = app_dir.name.replace(".App", ".Test")
    candidate = app_dir.parent / test_name
    return candidate if candidate.is_dir() else None


# ---------------------------------------------------------------------------
# C# source analysis
# ---------------------------------------------------------------------------


def public_methods_in_file(path: Path) -> list[tuple[str, str]]:
    """
    Return (class_name, method_name) pairs for every public method found in a
    C# source file.  Constructors are excluded.
    """
    text = path.read_text(encoding="utf-8")

    class_positions: list[tuple[int, str]] = [
        (m.start(), m.group(1)) for m in _CLASS_RE.finditer(text)
    ]
    if not class_positions:
        return []

    public_class_names = {name for _, name in class_positions}
    results: list[tuple[str, str]] = []

    for m in _METHOD_RE.finditer(text):
        method_name = m.group(1)
        if method_name in public_class_names:
            continue  # skip constructors

        # Determine the enclosing class by finding the last class declaration
        # that appears before this method in the file.
        pos = m.start()
        enclosing_class: str | None = None
        for class_pos, class_name in class_positions:
            if class_pos <= pos:
                enclosing_class = class_name

        if enclosing_class:
            results.append((enclosing_class, method_name))

    return results


def method_is_covered(
    class_name: str, method_name: str, test_texts: list[str]
) -> bool:
    """
    Return True when both the class name and the method name appear as
    whole-word identifiers in at least one test file's content.
    """
    class_re = re.compile(rf"\b{re.escape(class_name)}\b")
    method_re = re.compile(rf"\b{re.escape(method_name)}\b")
    for text in test_texts:
        if class_re.search(text) and method_re.search(text):
            return True
    return False


# ---------------------------------------------------------------------------
# GitHub API helpers
# ---------------------------------------------------------------------------


def _gh_request(method: str, path: str, body: dict | None = None) -> object:
    token = os.environ["GITHUB_TOKEN"]
    url = f"{GITHUB_API_BASE}{path}"
    data = json.dumps(body).encode() if body else None
    req = urllib.request.Request(
        url,
        data=data,
        method=method,
        headers={
            "Authorization": f"token {token}",
            "Accept": "application/vnd.github.v3+json",
            "Content-Type": "application/json",
            "X-GitHub-Api-Version": "2022-11-28",
        },
    )
    try:
        with urllib.request.urlopen(req) as resp:
            return json.loads(resp.read().decode())
    except urllib.error.HTTPError as exc:
        if exc.code == 401:
            raise SystemExit("GitHub API error 401: invalid or missing token.") from exc
        if exc.code in (403, 429):
            raise SystemExit(
                f"GitHub API error {exc.code}: rate-limited or insufficient permissions."
            ) from exc
        raise SystemExit(f"GitHub API HTTP error {exc.code}: {exc.reason}") from exc
    except urllib.error.URLError as exc:
        raise SystemExit(f"Network error contacting GitHub API: {exc.reason}") from exc


def existing_issue_titles(repo: str) -> set[str]:
    """Return all open issue titles (handles pagination, skips pull requests)."""
    titles: set[str] = set()
    page = 1
    while True:
        issues = _gh_request(
            "GET",
            f"/repos/{repo}/issues?state=open&per_page=100&page={page}",
        )
        if not isinstance(issues, list) or not issues:
            break
        for issue in issues:
            if "pull_request" not in issue:
                titles.add(issue["title"])
        if len(issues) < 100:
            break
        page += 1
    return titles


def create_issue(repo: str, title: str, body: str) -> int:
    """Create a GitHub issue and return its number."""
    result = _gh_request(
        "POST",
        f"/repos/{repo}/issues",
        {"title": title, "body": body},
    )
    assert isinstance(result, dict)
    return result["number"]


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------


def main() -> None:
    repo = os.environ["GITHUB_REPOSITORY"]
    workspace = Path(
        os.environ.get("GITHUB_WORKSPACE", str(Path(__file__).resolve().parents[2]))
    )

    print(f"Repository : {repo}")
    print(f"Workspace  : {workspace}")

    app_projects = find_app_projects(workspace)
    print(f"\nFound {len(app_projects)} .App project(s):")
    for p in app_projects:
        print(f"  {p.relative_to(workspace)}")

    known_titles = existing_issue_titles(repo)

    for app_dir in app_projects:
        test_dir = find_test_project(app_dir)
        if test_dir is None:
            print(
                f"\n[WARN] No companion test project found for {app_dir.name} – skipping."
            )
            continue

        print(f"\nProject : {app_dir.name}  →  tests in {test_dir.name}")

        # Read all test files once per project to avoid repeated I/O.
        test_texts = [
            f.read_text(encoding="utf-8") for f in test_dir.rglob("*.cs")
        ]

        for cs_file in sorted(app_dir.rglob("*.cs")):
            for class_name, method_name in public_methods_in_file(cs_file):
                rel_file = cs_file.relative_to(workspace)
                covered = method_is_covered(class_name, method_name, test_texts)
                tag = "covered" if covered else "NOT covered"
                print(f"  {class_name}.{method_name}  [{tag}]  ({rel_file})")

                if covered:
                    continue

                title = (
                    f"Add unit test for {class_name}.{method_name} "
                    f"in {app_dir.name}"
                )

                if title in known_titles:
                    print("    ↳ issue already exists, skipping.")
                    continue

                body = (
                    f"The public method `{method_name}` in class `{class_name}` "
                    f"(`{app_dir.name}`) is not covered by a unit test.\n\n"
                    f"Please add test coverage in **{test_dir.name}**.\n\n"
                    f"| Field | Value |\n"
                    f"|---|---|\n"
                    f"| Project | `{app_dir.name}` |\n"
                    f"| File | `{rel_file}` |\n"
                    f"| Class | `{class_name}` |\n"
                    f"| Method | `{method_name}` |\n"
                    f"| Test project | `{test_dir.name}` |\n"
                )

                number = create_issue(repo, title, body)
                known_titles.add(title)
                print(f"    ↳ created issue #{number}")


if __name__ == "__main__":
    main()
