#!/usr/bin/env python3
"""
Lint / Format Script

Formats the solution using JetBrains ReSharper Command Line Tools
(JetBrains.ReSharper.GlobalTools). Fails when 'jb' is not available
unless --install is passed, which installs it as a .NET global tool.

Code style rules come from the root .editorconfig, which jb applies
automatically to every file under it.

By default a missing jb is installed at the latest version; CI passes
--pin-version to force the predefined TOOL_VERSION instead.

With --check, exits with code 1 when formatting changes any file that was
not already modified before the run - used by CI to enforce formatting.

With --include, only the given files/patterns are formatted (passed as
jb --include="a;b;c", supports * and ** wildcards). Repeatable:
  python scripts/lint.py --include "source/**/*.cs" --include source/Foo/Bar.cs

With --changed, only currently changed files are formatted (staged +
unstaged + untracked via git status). With --staged, only staged files.
  python scripts/lint.py --changed
  python scripts/lint.py --staged
  python scripts/lint.py --changed --include "source/**/*.cs"  # intersection
"""

import argparse
import fnmatch
import logging
import shutil
import subprocess
import sys
from pathlib import Path
from pathlib import PurePosixPath

SOLUTION = Path("Xenia Manager.sln")
TOOL_PACKAGE = "JetBrains.ReSharper.GlobalTools"
TOOL_COMMAND = "jb"
TOOL_VERSION = "2026.2.1"
DEFAULT_PROFILE = "Built-in: Reformat & Apply Syntax Style"

logger = logging.getLogger(__name__)


def tools_directory() -> Path:
    """Return the .NET global tools directory for the current user."""
    return Path.home() / ".dotnet" / "tools"


def find_tool() -> Path | None:
    """Locate the jb executable, falling back to the global tools directory."""
    on_path = shutil.which(TOOL_COMMAND)
    if on_path:
        return Path(on_path)

    executable = tools_directory() / (
        f"{TOOL_COMMAND}.exe" if sys.platform == "win32" else TOOL_COMMAND
    )
    if executable.is_file():
        return executable

    return None


def install_tool(pin: bool) -> None:
    """Install ReSharper CLT as a .NET global tool, optionally at the pinned version.

    'dotnet tool update' is used when pinning because it installs the tool
    when missing and re-pins an existing copy in a single command.
    """
    target = f"v{TOOL_VERSION}" if pin else "latest"
    logger.info("Installing %s (%s)...", TOOL_PACKAGE, target)
    if pin:
        command = [
            "dotnet",
            "tool",
            "update",
            "--global",
            TOOL_PACKAGE,
            "--version",
            TOOL_VERSION,
        ]
    else:
        command = ["dotnet", "tool", "install", "--global", TOOL_PACKAGE]
    result = subprocess.run(command, check=False)
    if result.returncode != 0:
        raise RuntimeError(
            f"Failed to install {TOOL_PACKAGE}. Is the .NET SDK installed?"
        )


def tool_version() -> str | None:
    """Return the installed version of the global tools package, if listed."""
    result = subprocess.run(
        ["dotnet", "tool", "list", "--global"],
        capture_output=True,
        text=True,
        check=False,
    )
    for line in result.stdout.splitlines():
        columns = line.split()
        if columns and columns[0].lower() == TOOL_PACKAGE.lower():
            return columns[1] if len(columns) > 1 else None
    return None


def ensure_tool(install: bool, pin: bool) -> Path:
    """Return the jb executable, installing it first when missing.

    With pin, the global tools copy is forced to TOOL_VERSION; otherwise
    any existing installation is used as-is.
    """
    tool = find_tool()
    if tool is not None:
        mismatched = (
            pin
            and tool.parent.resolve() == tools_directory().resolve()
            and tool_version() != TOOL_VERSION
        )
        if not mismatched:
            logger.info("Using %s at %s", TOOL_COMMAND, tool)
            return tool

    if not install:
        raise RuntimeError(
            f"{TOOL_COMMAND} not found. Install it with: "
            f"dotnet tool install --global {TOOL_PACKAGE} "
            "(or rerun with --install)"
        )

    install_tool(pin)

    tool = find_tool()
    if tool is None:
        raise RuntimeError(
            f"{TOOL_COMMAND} still not found after installation "
            f"(expected in {tools_directory()})."
        )
    logger.info("Installed %s at %s", TOOL_COMMAND, tool)
    return tool


def uncommitted_files() -> list[str]:
    """Return files with uncommitted modifications per git."""
    result = subprocess.run(
        ["git", "-c", "core.quotepath=false", "status", "--porcelain"],
        capture_output=True,
        text=True,
        check=False,
    )
    files = []
    for line in result.stdout.splitlines():
        status, path = line[:2], line[3:]
        if status.strip() == "R":
            # Rename entries look like "R  old -> new"; report the new name.
            path = path.split(" -> ", maxsplit=1)[-1]
        files.append(path)
    return files


def _glob_match(path: str, pattern: str) -> bool:
    """Match posix path against glob pattern with * and ** (segment-aware).

    *  matches within a single path segment (no slash crossing)
    ** matches zero or more segments
    ?  matches single character within a segment
    """
    pat_segs = pattern.split("/")
    path_segs = path.split("/")
    # Memoization for DP
    from functools import lru_cache

    @lru_cache(maxsize=None)
    def dp(i: int, j: int) -> bool:
        if i == len(pat_segs) and j == len(path_segs):
            return True
        if i == len(pat_segs):
            return False
        if pat_segs[i] == "**":
            # ** can match zero segments
            if dp(i + 1, j):
                return True
            # or one or more segments
            if j < len(path_segs) and dp(i, j + 1):
                return True
            return False
        if j >= len(path_segs):
            return False
        if not fnmatch.fnmatch(path_segs[j], pat_segs[i]):
            return False
        return dp(i + 1, j + 1)

    return dp(0, 0)


def _matches_any(path: str, patterns: list[str]) -> bool:
    """Return True if posix path matches any of the include patterns.

    Supports exact matches, * (single segment), ** (recursive) via
    _glob_match, plus plain directory prefix matches (e.g. "source/Foo"
    matches "source/Foo/Bar.cs"). Patterns without a slash that contain
    wildcards are also matched against the basename.
    """
    posix_path = path.replace("\\", "/")
    for raw in patterns:
        pat = raw.replace("\\", "/")
        if not pat:
            continue
        if posix_path == pat:
            return True
        # Plain directory prefix without wildcards
        if not any(ch in pat for ch in "*?[]"):
            if posix_path == pat or posix_path.startswith(pat.rstrip("/") + "/"):
                return True
            continue
        # Glob match (segment-aware)
        if _glob_match(posix_path, pat):
            return True
        # For patterns without slash, also try basename (e.g. "*.cs" matches any depth)
        if "/" not in pat and fnmatch.fnmatch(PurePosixPath(posix_path).name, pat):
            return True
    return False


def _status_files(staged_only: bool = False) -> list[str]:
    """Return staged (if staged_only) or all changed files via git status.

    Filters out deletions (D in either column) and non-files.
    Includes untracked (??) when not staged_only.
    """
    result = subprocess.run(
        ["git", "-c", "core.quotepath=false", "status", "--porcelain"],
        capture_output=True,
        text=True,
        check=False,
    )
    # If not a git repo, status returns non-zero and empty stdout
    if result.returncode != 0 and not result.stdout:
        return []

    files: list[str] = []
    for line in result.stdout.splitlines():
        if not line or len(line) < 3:
            continue
        status, path = line[:2], line[3:]
        if status.strip() == "R":
            path = path.split(" -> ", maxsplit=1)[-1]
            # fall through to staged/unstaged checks with resolved path
        # Skip deletions
        if status[0] == "D" or status[1] == "D":
            continue
        if staged_only:
            # Staged means first column indicates change
            if status[0] in (" ", "?", "!"):
                continue
        # For staged_only, we already filter to staged; for changed, keep all non-deletions
        files.append(path)

    # Only return files that currently exist on disk
    return [f for f in files if Path(f).is_file()]


def changed_files() -> list[str]:
    """Return staged + unstaged + untracked files (existing only)."""
    return _status_files(staged_only=False)


def staged_files() -> list[str]:
    """Return only staged files (existing only)."""
    return _status_files(staged_only=True)


def format_solution(
    tool: Path,
    solution: Path,
    profile: str | None,
    settings: Path | None,
    no_build: bool = False,
    includes: list[str] | None = None,
) -> int:
    """Run jb cleanupcode over the solution. Returns the process exit code."""
    command = [str(tool), "cleanupcode", str(solution)]

    if profile:
        command.append(f"--profile={profile}")
    if settings:
        command.append(f"--settings={settings}")
    if no_build:
        command.append("--no-build")
    if includes:
        normalized = [p.replace("\\", "/") for p in includes if p]
        if normalized:
            command.append(f"--include={';'.join(normalized)}")

    logger.info("Running: %s", " ".join(command))
    result = subprocess.run(command, check=False)

    if result.returncode == 0:
        logger.info("Formatting complete")
    elif result.returncode == 3 and includes:
        # jb returns 3 when --include matches no solution items (e.g. --changed
        # only touched docs/scripts). Treat as success - nothing to format.
        logger.info("No solution files matched the include filter - nothing to format")
        return 0
    else:
        logger.error("Formatting failed with exit code %d", result.returncode)

    return result.returncode


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Format the solution with ReSharper Command Line Tools",
    )
    parser.add_argument(
        "--solution",
        type=Path,
        default=SOLUTION,
        help=f"Solution file to format (default: {SOLUTION})",
    )
    parser.add_argument(
        "--profile",
        default=DEFAULT_PROFILE,
        help=(
            "Cleanup profile to apply, e.g. 'Built-in: Full Cleanup' "
            f"(default: {DEFAULT_PROFILE})"
        ),
    )
    parser.add_argument(
        "--settings",
        type=Path,
        help="Path to a .DotSettings file overriding shared settings",
    )
    parser.add_argument(
        "--install",
        action="store_true",
        help=f"Install {TOOL_PACKAGE} as a .NET global tool when missing",
    )
    parser.add_argument(
        "--pin-version",
        action="store_true",
        help=(
            "Install/force the predefined tool version "
            f"({TOOL_VERSION}) instead of the latest; used by CI"
        ),
    )
    parser.add_argument(
        "--no-build",
        action="store_true",
        help="Skip building the solution before formatting",
    )
    parser.add_argument(
        "--check",
        action="store_true",
        help="Fail with exit code 1 when formatting changes any file",
    )
    parser.add_argument(
        "--include",
        action="append",
        dest="includes",
        default=None,
        metavar="PATTERN",
        help=(
            "Only format files matching this pattern (repeatable, supports "
            "* and ** globs; e.g. --include \"source/**/*.cs\"). "
            "Passed as jb --include"
        ),
    )
    parser.add_argument(
        "--changed",
        action="store_true",
        help="Only format changed files (staged + unstaged + untracked)",
    )
    parser.add_argument(
        "--staged",
        action="store_true",
        help="Only format staged files (git index)",
    )
    parser.add_argument("--debug", action="store_true", help="Enable debug logging")
    args = parser.parse_args()

    logging.basicConfig(
        level=logging.DEBUG if args.debug else logging.INFO,
        format="[%(levelname)s] %(message)s",
    )

    if args.changed and args.staged:
        parser.error("--changed and --staged are mutually exclusive")

    if not args.solution.is_file():
        logger.error("Solution not found: %s", args.solution)
        return 1

    # Resolve effective includes from --include / --changed / --staged
    effective_includes: list[str] | None = None

    if args.changed or args.staged:
        if args.staged:
            git_files = staged_files()
            label = "staged"
        else:
            git_files = changed_files()
            label = "changed (staged+unstaged+untracked)"

        if not git_files:
            logger.info("No %s files found - nothing to format", label)
            return 0

        logger.info("Found %d %s file(s)", len(git_files), label)
        for name in git_files[:20]:
            logger.debug("  %s", name)
        if len(git_files) > 20:
            logger.debug("  ... and %d more", len(git_files) - 20)

        if args.includes:
            filtered = [f for f in git_files if _matches_any(f, args.includes)]
            if not filtered:
                logger.warning("No %s files match --include patterns", label)
                logger.info("  --include: %s", ";".join(args.includes))
                return 0
            if len(filtered) != len(git_files):
                logger.info(
                    "Filtered to %d file(s) matching --include", len(filtered)
                )
            effective_includes = filtered
        else:
            effective_includes = git_files
    elif args.includes:
        effective_includes = [p for p in args.includes if p]
        if not effective_includes:
            logger.warning("No --include patterns given")
            return 0
        logger.info("Formatting %d --include pattern(s)", len(effective_includes))
        for pat in effective_includes:
            logger.debug("  %s", pat)

    try:
        tool = ensure_tool(args.install, args.pin_version)
    except RuntimeError as error:
        logger.error(str(error))
        return 1

    already_dirty = set(uncommitted_files())

    exit_code = format_solution(
        tool, args.solution, args.profile, args.settings, args.no_build, effective_includes
    )
    if exit_code != 0 or not args.check:
        return exit_code

    reformatted = sorted(set(uncommitted_files()) - already_dirty)
    if effective_includes is not None:
        reformatted = [f for f in reformatted if _matches_any(f, effective_includes)]
    if not reformatted:
        logger.info("Check passed: everything is formatted")
        return 0

    logger.error("Not linted - formatting changed %d file(s):", len(reformatted))
    for name in reformatted[:20]:
        logger.error("  %s", name)
    if len(reformatted) > 20:
        logger.error("  ... and %d more", len(reformatted) - 20)
    logger.error("Run 'python scripts/lint.py' and commit the result.")
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
