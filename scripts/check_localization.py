#!/usr/bin/env python3
"""
Localization Checker Script
Extracts keys from en.axaml and checks which ones are used in code files.
Also detects hardcoded text in AXAML files that should be localized.
"""

import argparse
import logging
import re
import sys
from collections import defaultdict
from dataclasses import dataclass, field
from pathlib import Path

SOURCE_PATH = Path("source")
LANGUAGE_FILE = SOURCE_PATH / "XeniaManager" / "Resources" / "Language" / "en.axaml"
IGNORE_FOLDERS = {"Resources", "obj", "bin"}

DYNAMIC_RESOURCE_PATTERN = re.compile(r"\{DynamicResource\s+([^}]+)\}")
LOCALIZATION_HELPER_PATTERN = re.compile(
    r'LocalizationHelper\.GetText\(["\']([^"\']+)["\']\)'
)
KEY_PATTERN = re.compile(r'x:Key="([^"]+)"')
# Skip keys that don't contain a dot - these are typically theme resources (brushes, colors, styles)
# Localization keys always have a dot (e.g., "GameDetailsEditor.Background.Clear")
SKIP_PATTERN = re.compile(r"^[^.]+$")

# Pattern to detect hardcoded text in common text properties
# Matches: Text, Header, Title, Description, Content, Watermark, StatusText, ProgressText, ToolTip.Tip
HARDCODED_TEXT_PATTERN = re.compile(
    r"\b(Text|Header|Title|Description|Content|Watermark|StatusText|ProgressText|ToolTip\.Tip)"
    r'\s*=\s*"([^"]+)"',
    re.IGNORECASE,
)

# Pattern to detect hardcoded text in _messageBoxService method calls
# Matches: _messageBoxService.ShowInfoAsync("title", "message") or _messageBoxService.ShowInfoAsync("title", $"...")
MESSAGEBOX_SERVICE_PATTERN = re.compile(
    r'_messageBoxService\.(?:ShowInfoAsync|ShowWarningAsync|ShowErrorAsync|ShowConfirmationAsync|ShowCustomDialogAsync)\s*\(\s*"([^"]+)"(?:\s*,\s*(\$)?"([^"]+)")?',
    re.IGNORECASE,
)

# Patterns to skip - bindings, markup extensions, empty strings, design-time only, single symbols
SKIP_TEXT_PATTERN = re.compile(
    r"^(\{|x:|d:|\$|\*|<|>|/|\\|\[|\]|\(|\)|#|@|!|\?|\.|,|;|:|\+|-|_|=|%|&|\||\^|~|`)"
)

logger = logging.getLogger(__name__)


@dataclass
class ScanResults:
    found: dict[str, list[str]] = field(default_factory=lambda: defaultdict(list))
    missing: dict[str, list[tuple[str, int, str]]] = field(
        default_factory=lambda: defaultdict(list)
    )  # (file, line_number, line_content)
    hardcoded: list[tuple[str, str, str, int, str]] = field(
        default_factory=list
    )  # (file, property, text, line_number, line_content)

    def merge(self, other: "ScanResults") -> None:
        for key, paths in other.found.items():
            self.found[key].extend(paths)
        for key, paths in other.missing.items():
            self.missing[key].extend(paths)
        self.hardcoded.extend(other.hardcoded)


def extract_keys_from_en_axaml(file_path: Path) -> set[str]:
    """Extract all localization keys from en.axaml."""
    content = file_path.read_text(encoding="utf-8")
    keys = set(KEY_PATTERN.findall(content))
    logger.info("Found %d keys in en.axaml", len(keys))
    return keys


def should_skip_key(key: str) -> bool:
    """Return True for non-text resources like brushes, colors, and styles."""
    return bool(SKIP_PATTERN.search(key))


def should_ignore_file(file_path: Path) -> bool:
    """Return True if the file lives inside an ignored folder."""
    return bool(IGNORE_FOLDERS.intersection(file_path.parts))


def scan_file(file_path: Path, en_keys: set[str], pattern: re.Pattern) -> ScanResults:
    """Scan a single file using the given pattern and classify matches."""
    logger.debug("Scanning %s...", file_path.name)
    results = ScanResults()

    try:
        content = file_path.read_text(encoding="utf-8")
    except UnicodeDecodeError:
        content = file_path.read_text(encoding="utf-8-sig")

    relative_path = str(file_path.relative_to(SOURCE_PATH))

    for match in pattern.finditer(content):
        key = match.group(1).strip()
        if should_skip_key(key):
            continue

        line_number = content[: match.start()].count("\n") + 1
        line_start = content.rfind("\n", 0, match.start()) + 1
        line_end = content.find("\n", match.end())
        if line_end == -1:
            line_end = len(content)
        line = content[line_start:line_end]

        if key in en_keys:
            results.found[key].append(relative_path)
        else:
            results.missing[key].append((relative_path, line_number, line.strip()))

    return results


def scan_hardcoded_text(file_path: Path) -> ScanResults:
    """Scan AXAML file for hardcoded text that should be localized."""
    logger.debug("Checking %s for hardcoded text...", file_path.name)
    results = ScanResults()

    try:
        content = file_path.read_text(encoding="utf-8")
    except UnicodeDecodeError:
        content = file_path.read_text(encoding="utf-8-sig")

    relative_path = str(file_path.relative_to(SOURCE_PATH))
    lines = content.split("\n")

    # Check for attribute-based text (Text="...", Header="...", etc.)
    for match in HARDCODED_TEXT_PATTERN.finditer(content):
        property_name = match.group(1)
        text_value = match.group(2).strip()

        # Skip empty strings, bindings, markup extensions, single symbols, and single characters
        if (
            not text_value
            or len(text_value) <= 1
            or SKIP_TEXT_PATTERN.match(text_value)
        ):
            continue

        # Skip design-time properties (d: prefix in the line)
        line_start = content.rfind("\n", 0, match.start()) + 1
        line_end = content.find("\n", match.end())
        if line_end == -1:
            line_end = len(content)
        line = content[line_start:line_end]
        if "d:" in line or "x:" in line:
            continue

        line_number = content[: match.start()].count("\n") + 1
        results.hardcoded.append(
            (relative_path, property_name, text_value, line_number, line.strip())
        )

    # Check for plain text content in UserControl/Window tags (e.g., <UserControl>About Page</UserControl>)
    plain_text_pattern = re.compile(
        r"<(?:UserControl|Window)[^>]*>\s*([A-Za-z][A-Za-z\s]{2,}?)\s*<", re.IGNORECASE
    )
    for match in plain_text_pattern.finditer(content):
        text_value = match.group(1).strip()
        # Skip if it's just whitespace or looks like XAML
        if text_value and not text_value.startswith("{") and len(text_value) > 2:
            line_number = content[: match.start()].count("\n") + 1
            line_start = content.rfind("\n", 0, match.start()) + 1
            line_end = content.find("\n", match.end())
            if line_end == -1:
                line_end = len(content)
            line = content[line_start:line_end]
            results.hardcoded.append(
                (relative_path, "Content", text_value, line_number, line.strip())
            )

    return results


def scan_messagebox_service(file_path: Path) -> ScanResults:
    """Scan C# file for hardcoded text in _messageBoxService method calls."""
    logger.debug("Checking %s for _messageBoxService usage...", file_path.name)
    results = ScanResults()

    try:
        content = file_path.read_text(encoding="utf-8")
    except UnicodeDecodeError:
        content = file_path.read_text(encoding="utf-8-sig")

    relative_path = str(file_path.relative_to(SOURCE_PATH))

    for match in MESSAGEBOX_SERVICE_PATTERN.finditer(content):
        title = match.group(1).strip()
        is_interpolated = match.group(2) == "$"
        message = match.group(3).strip() if match.group(3) else ""

        # Skip if the text uses LocalizationHelper
        if "LocalizationHelper" in match.group(0):
            continue

        line_number = content[: match.start()].count("\n") + 1
        line_start = content.rfind("\n", 0, match.start()) + 1
        line_end = content.find("\n", match.end())
        if line_end == -1:
            line_end = len(content)
        line = content[line_start:line_end]

        # Skip empty strings or single characters
        if title and len(title) > 1 and not SKIP_TEXT_PATTERN.match(title):
            results.hardcoded.append(
                (relative_path, "Title", title, line_number, line.strip())
            )

        if message and len(message) > 1:
            # For interpolated strings, note that they contain dynamic content
            if is_interpolated:
                results.hardcoded.append(
                    (
                        relative_path,
                        "Message (interpolated)",
                        message,
                        line_number,
                        line.strip(),
                    )
                )
            else:
                results.hardcoded.append(
                    (relative_path, "Message", message, line_number, line.strip())
                )

    return results


def collect_source_files(
    source_path: Path, language_file: Path
) -> tuple[list[Path], list[Path]]:
    """Return (axaml_files, cs_files), excluding ignored folders and the language file."""
    axaml_files = [
        f
        for f in source_path.rglob("*.axaml")
        if f != language_file and not should_ignore_file(f)
    ]
    cs_files = [f for f in source_path.rglob("*.cs") if not should_ignore_file(f)]
    return axaml_files, cs_files


def report(en_keys: set[str], results: ScanResults) -> bool:
    """Print the full report. Returns True if there are errors."""
    all_referenced = set(results.found) | set(results.missing)
    unused_keys = en_keys - all_referenced
    div = "=" * 60

    print(f"\n{div}\nSUMMARY\n{div}")
    print(f"Total keys in en.axaml:              {len(en_keys)}")
    print(f"Total unique keys referenced:        {len(all_referenced)}")
    print(f"Keys found in en.axaml:              {len(results.found)}")
    print(f"Keys missing from en.axaml:          {len(results.missing)}")
    print(f"Hardcoded text detected:             {len(results.hardcoded)}")
    print(
        f"\n{div}\nUNUSED KEYS - defined in en.axaml but not referenced in code\n{div}"
    )

    if unused_keys:
        print(f"\n{len(unused_keys)} unused key(s):")
        for key in sorted(unused_keys):
            print(f"  {key}")
    else:
        print("\n[OK] All keys in en.axaml are being used")

    print(f"\n{div}\nHARDCODED TEXT - text that may need localization\n{div}")
    if results.hardcoded:
        print(f"\n{len(results.hardcoded)} instance(s) found:")
        for (
            file_path,
            property_name,
            text_value,
            line_number,
            line_content,
        ) in results.hardcoded:
            print(f"  {file_path}:{line_number}")
            print(f'    {property_name}="{text_value}"')
            print(f"    {line_content}")
        print("\n[WARN] Review the above text for potential localization")
    else:
        print("\n[OK] There are no hardcoded text properties")

    print(
        f"\n{div}\nMISSING KEYS - referenced in code but not defined in en.axaml\n{div}"
    )
    if results.missing:
        for key in sorted(results.missing):
            print(f"  {key}")
            for path, line_number, line_content in results.missing[key]:
                print(f"   - {path}:{line_number}")
                print(f"     {line_content}")
        return True

    print("\n[OK] All keys referenced in code are defined in en.axaml")
    return False


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Check localization keys in en.axaml against code usage"
    )
    parser.add_argument("--debug", action="store_true", help="Enable debug logging")
    args = parser.parse_args()

    logging.basicConfig(
        level=logging.DEBUG if args.debug else logging.INFO,
        format="[%(levelname)s] %(message)s",
    )

    logger.info("Source path: %s", SOURCE_PATH.absolute())
    logger.info("Language file: %s", LANGUAGE_FILE.absolute())

    en_keys = extract_keys_from_en_axaml(LANGUAGE_FILE)
    axaml_files, cs_files = collect_source_files(SOURCE_PATH, LANGUAGE_FILE)

    logger.info(
        "Found %d .axaml files and %d .cs files to scan",
        len(axaml_files),
        len(cs_files),
    )

    results = ScanResults()

    logger.info("Scanning AXAML files for DynamicResource usage...")
    for f in axaml_files:
        results.merge(scan_file(f, en_keys, DYNAMIC_RESOURCE_PATTERN))

    logger.info("Scanning C# files for LocalizationHelper.GetText() usage...")
    for f in cs_files:
        results.merge(scan_file(f, en_keys, LOCALIZATION_HELPER_PATTERN))

    logger.info("Checking AXAML files for hardcoded text...")
    for f in axaml_files:
        results.merge(scan_hardcoded_text(f))

    logger.info("Checking C# files for _messageBoxService usage...")
    for f in cs_files:
        results.merge(scan_messagebox_service(f))

    has_errors = report(en_keys, results)

    div = "=" * 60
    print(f"\n{div}")
    print(
        "RESULT: FAILED - Missing localization keys detected"
        if has_errors
        else "RESULT: PASSED - All localization keys are valid"
    )
    print(div)

    sys.exit(1 if has_errors else 0)


if __name__ == "__main__":
    main()
