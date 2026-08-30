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
"""

import argparse
import logging
import shutil
import subprocess
import sys
from pathlib import Path

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


def format_solution(
    tool: Path,
    solution: Path,
    profile: str | None,
    settings: Path | None,
    no_build: bool = False,
) -> int:
    """Run jb cleanupcode over the solution. Returns the process exit code."""
    command = [str(tool), "cleanupcode", str(solution)]

    if profile:
        command.append(f"--profile={profile}")
    if settings:
        command.append(f"--settings={settings}")
    if no_build:
        command.append("--no-build")

    logger.info("Running: %s", " ".join(command))
    result = subprocess.run(command, check=False)

    if result.returncode == 0:
        logger.info("Formatting complete")
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
    parser.add_argument("--debug", action="store_true", help="Enable debug logging")
    args = parser.parse_args()

    logging.basicConfig(
        level=logging.DEBUG if args.debug else logging.INFO,
        format="[%(levelname)s] %(message)s",
    )

    if not args.solution.is_file():
        logger.error("Solution not found: %s", args.solution)
        return 1

    try:
        tool = ensure_tool(args.install, args.pin_version)
    except RuntimeError as error:
        logger.error(str(error))
        return 1

    already_dirty = set(uncommitted_files())

    exit_code = format_solution(
        tool, args.solution, args.profile, args.settings, args.no_build
    )
    if exit_code != 0 or not args.check:
        return exit_code

    reformatted = sorted(set(uncommitted_files()) - already_dirty)
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
