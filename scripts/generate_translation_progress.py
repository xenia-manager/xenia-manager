#!/usr/bin/env python3
r"""
Translation Progress Generator

Compares translation keys between English (en.axaml) and other language files
to identify missing or extra translations.

Automatically finds all .axaml language files across every language folder
(XeniaManager and XeniaManager.BigScreen) and compares them against their
respective en.axaml.

Usage:
    python generate_translation_progress.py [--directory DIR [DIR ...]] [--verbose] [--json]

Examples:
    python generate_translation_progress.py
    python generate_translation_progress.py --directory "E:\Development\Xenia Manager\Avalonia\source\XeniaManager\Resources\Language"
    python generate_translation_progress.py --verbose
    python generate_translation_progress.py --json
"""

import argparse
import json
import logging
import os
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path
from typing import Dict, List, Set, Tuple

logger = logging.getLogger(__name__)


def extract_keys(file_path: str) -> Dict[str, str]:
    """
    Extract all x:Key attributes and their values from an AXAML file.

    Args:
        file_path: Path to the .axaml file

    Returns:
        Dictionary mapping keys to their translated values
    """
    if not os.path.exists(file_path):
        raise FileNotFoundError(f"File not found: {file_path}")

    with open(file_path, "r", encoding="utf-8") as f:
        content = f.read()

    # Pattern to match <sys:String x:Key="...">value</sys:String>
    pattern = r'<sys:String\s+x:Key="([^"]+)">([^<]*(?:&#10;[^<]*)*)</sys:String>'

    translations = {}
    for match in re.finditer(pattern, content):
        key = match.group(1)
        value = match.group(2)
        translations[key] = value

    return translations


def count_translations(file_path: str, is_main_file: bool = False) -> int:
    """
    Count translation strings in an AXAML file.

    Args:
        file_path: Path to the .axaml file
        is_main_file: If True, count all strings (for en.axaml).
                     If False, count only non-empty strings that are not #NOTTRANSLATED#

    Returns:
        Count of translation strings
    """
    try:
        tree = ET.parse(file_path)
        root = tree.getroot()

        # Find all sys:String elements (need to handle XML namespaces)
        namespaces = {"sys": "clr-namespace:System;assembly=System.Runtime"}
        string_elements = root.findall(".//sys:String", namespaces)

        # If namespace didn't work, try without it
        if not string_elements:
            string_elements = root.findall(
                ".//{clr-namespace:System;assembly=System.Runtime}String"
            )

        # Fallback: regex-based extraction if XML parsing fails
        if not string_elements:
            with open(file_path, "r", encoding="utf-8") as f:
                content = f.read()
            pattern = r'<sys:String\s+x:Key="[^"]+">([^<]*(?:&#10;[^<]*)*)</sys:String>'
            matches = re.findall(pattern, content)
            if is_main_file:
                return len(matches)
            else:
                count = 0
                for value in matches:
                    trimmed = value.strip()
                    if trimmed and trimmed != "#NOTTRANSLATED#":
                        count += 1
                return count

        if is_main_file:
            return len(string_elements)
        else:
            count = 0
            for elem in string_elements:
                text = (elem.text or "").strip()
                if text and text != "#NOTTRANSLATED#":
                    count += 1
            return count

    except ET.ParseError as e:
        logger.error("Failed to parse XML in %s: %s", file_path, e)
        return 0
    except Exception as e:
        logger.error("Failed to process %s: %s", file_path, e)
        return 0


def compare_translations(
    base_file: str, compare_file: str, base_lang: str = "en", compare_lang: str = "hr"
) -> Tuple[Set[str], Set[str], Dict[str, str]]:
    """
    Compare translation keys between two language files.

    Args:
        base_file: Path to the base language file (e.g., en.axaml)
        compare_file: Path to the comparison language file (e.g., hr.axaml)
        base_lang: Name of the base language for display
        compare_lang: Name of the comparison language for display

    Returns:
        Tuple of (missing_keys, extra_keys, base_translations)
    """
    base_keys = extract_keys(base_file)
    compare_keys = extract_keys(compare_file)

    base_key_set = set(base_keys.keys())
    compare_key_set = set(compare_keys.keys())

    missing_keys = base_key_set - compare_key_set
    extra_keys = compare_key_set - base_key_set

    return missing_keys, extra_keys, base_keys


def check_untranslated(
    base_translations: Dict[str, str],
    compare_translations: Dict[str, str],
    threshold: float = 0.9,
) -> List[str]:
    """
    Check for potentially untranslated strings by looking for #NOTTRANSLATED# marker.

    Args:
        base_translations: Dictionary of base language translations
        compare_translations: Dictionary of comparison language translations
        threshold: Similarity threshold (unused, kept for compatibility)

    Returns:
        List of keys that are marked as untranslated with #NOTTRANSLATED#
    """
    untranslated = []

    for key in compare_translations:
        compare_value = compare_translations[key]

        # Only consider untranslated if it contains the #NOTTRANSLATED# marker
        if "#NOTTRANSLATED#" in compare_value:
            untranslated.append(key)

    return untranslated


def print_summary(
    results: Dict[str, Dict],
    total_strings: int,
    base_lang: str = "en",
    verbose: bool = False,
) -> None:
    """
    Print a summary report for all compared languages.

    Args:
        results: Dictionary with language results
        total_strings: Total number of strings to translate (from en.axaml)
        base_lang: Base language code
        verbose: Whether to show detailed missing keys
    """
    div = "=" * 60

    print(f"\n{div}\nTRANSLATION PROGRESS SUMMARY\n{div}")
    print(f"\n[OK] Total strings to translate: {total_strings}")
    print()

    # Calculate totals
    total_missing = sum(len(r["missing"]) for r in results.values())
    total_extra = sum(len(r["extra"]) for r in results.values())
    total_untranslated = sum(len(r["untranslated"]) for r in results.values())

    # Summary table
    print(
        f"{'Language':<15} {'Progress':<12} {'Translated':<12} {'Missing':<10} {'Extra':<8} {'Untranslated':<12}"
    )
    print("-" * 80)

    for lang in sorted(results.keys()):
        r = results[lang]
        missing_count = len(r["missing"])
        extra_count = len(r["extra"])
        untranslated_count = len(r["untranslated"])
        translated = r["translated_count"]
        percentage = r["percentage"]

        status = "[OK]" if missing_count == 0 else "[FAIL]"

        print(
            f"{status} {lang.upper():<13} {percentage:>5}%    {translated:<12} {missing_count:<10} {extra_count:<8} {untranslated_count:<12}"
        )

    print("-" * 80)
    print()

    # Overall status
    if total_missing == 0 and total_extra == 0 and total_untranslated == 0:
        print("[OK] Status: ALL TRANSLATIONS COMPLETE!")
    elif total_missing == 0:
        print(
            f"[WARN] Status: All translations present, but {total_extra} extra key(s) and/or {total_untranslated} untranslated string(s) found"
        )
    else:
        print(f"[FAIL] Status: {total_missing} translation(s) missing across all languages")

    print()

    # Detailed output for verbose mode
    if verbose:
        for lang in sorted(results.keys()):
            r = results[lang]
            if r["missing"] or r["extra"] or r["untranslated"]:
                print(f"\n{div}\nDETAILS FOR: {lang.upper()}\n{div}")

                if r["missing"]:
                    print(f"\n[FAIL] MISSING ({len(r['missing'])} keys):")
                    for key in sorted(r["missing"]):
                        print(f"   - {key}")

                if r["extra"]:
                    print(f"\n[WARN] EXTRA ({len(r['extra'])} keys):")
                    for key in sorted(r["extra"]):
                        print(f"   - {key}")

                if r["untranslated"]:
                    print(f"\n[WARN] UNTRANSLATED ({len(r['untranslated'])} keys):")
                    for key in sorted(r["untranslated"]):
                        print(f"   - {key}")

                print()

    # Quick reference for missing keys (non-verbose but still useful)
    if not verbose and total_missing > 0:
        print("[INFO] Run with --verbose to see detailed list of missing keys")
        print()

    print(div)


def output_json(results: Dict[str, Dict], total_strings: int) -> None:
    """
    Output translation progress as JSON (for workflow/chart generation).

    Args:
        results: Dictionary with language results
        total_strings: Total number of strings to translate (from en.axaml)
    """
    output = {"TotalStrings": total_strings, "Translations": {}}

    for lang, r in results.items():
        output["Translations"][lang] = {
            "Translated": r["translated_count"],
            "Total": total_strings,
            "Percentage": r["percentage"],
        }

    print(json.dumps(output, indent=2))


def print_report(
    missing_keys: Set[str],
    extra_keys: Set[str],
    untranslated_keys: List[str],
    base_count: int,
    compare_count: int,
    base_lang: str = "en",
    compare_lang: str = "hr",
) -> None:
    """
    Print a formatted comparison report for a single language.
    Deprecated: Use print_summary instead.
    """
    print(f"Report for {compare_lang}...")
    print(
        f"Missing: {len(missing_keys)}, Extra: {len(extra_keys)}, Untranslated: {len(untranslated_keys)}"
    )


def find_language_files(directory: str) -> Dict[str, str]:
    """
    Find all language AXAML files in the specified directory.

    Args:
        directory: Path to the directory containing language files

    Returns:
        Dictionary mapping language codes to file paths
    """
    language_files = {}
    pattern = re.compile(r"^([a-z]{2}(?:-[A-Z]{2})?)\.axaml$")

    for filename in os.listdir(directory):
        match = pattern.match(filename)
        if match:
            lang_code = match.group(1)
            file_path = os.path.join(directory, filename)
            language_files[lang_code] = file_path

    return language_files


def main():
    parser = argparse.ArgumentParser(
        description="Compare translation keys between language files. Automatically compares all languages against en.axaml.",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=__doc__,
    )
    parser.add_argument(
        "--directory",
        "-d",
        nargs="+",
        default=None,
        help="Directories containing language files (default: all known language folders)",
    )
    parser.add_argument(
        "--verbose",
        "-v",
        action="store_true",
        help="Show detailed output including untranslated strings",
    )
    parser.add_argument(
        "--base",
        "-b",
        default="en",
        help="Base language code to compare against (default: en)",
    )
    parser.add_argument(
        "--json",
        action="store_true",
        help="Output progress as JSON (for workflow/chart generation)",
    )
    parser.add_argument(
        "--debug",
        action="store_true",
        help="Enable debug logging",
    )

    args = parser.parse_args()

    # Setup logging
    logging.basicConfig(
        level=logging.DEBUG if args.debug else logging.INFO,
        format="[%(levelname)s] %(message)s",
    )

    # Determine the language files directories
    if args.directory:
        lang_dirs = list(args.directory)
    else:
        script_dir = Path(__file__).parent
        candidates = [
            script_dir.parent / "source" / "XeniaManager" / "Resources" / "Language",
            script_dir.parent.parent
            / "source"
            / "XeniaManager"
            / "Resources"
            / "Language",
        ]
        bigscreen_dir = (
            script_dir.parent
            / "source"
            / "XeniaManager.BigScreen"
            / "Resources"
            / "Language"
        )
        lang_dirs = [str(d) for d in candidates if d.exists()]
        if bigscreen_dir.exists():
            lang_dirs.append(str(bigscreen_dir))

        if not lang_dirs:
            logger.error("Could not find language files directory.")
            logger.error("Please specify it with --directory")
            sys.exit(1)

    total_strings = 0
    all_complete = True
    results: Dict[str, Dict] = {}

    # First pass: validate directories and find language files in each
    all_language_files: Dict[str, Dict[str, str]] = {}
    for lang_dir in lang_dirs:
        # Check if language directory exists
        if not os.path.isdir(lang_dir):
            logger.error("Language directory not found: %s", lang_dir)
            sys.exit(1)

        logger.info("Scanning directory: %s", lang_dir)

        # Find all language files
        language_files = find_language_files(lang_dir)

        if not language_files:
            logger.error("No .axaml files found in language directory")
            sys.exit(1)

        logger.info("Found languages: %s", ", ".join(sorted(language_files.keys())))
        all_language_files[lang_dir] = language_files

    all_langs = sorted({lang for files in all_language_files.values() for lang in files})
    logger.info("Languages across all directories: %s", ", ".join(all_langs))

    for lang_dir, language_files in all_language_files.items():
        base_file = language_files.get(args.base)
        if not base_file:
            logger.error("Main %s.axaml file not found in language directory", args.base)
            sys.exit(1)

        # Get total strings from English file
        dir_total_strings = count_translations(base_file, is_main_file=True)
        total_strings += dir_total_strings
        logger.info(
            "Strings to translate in %s: %d", os.path.basename(lang_dir), dir_total_strings
        )

        # Initialize the base language entry (en) once with full progress
        if args.base not in results:
            results[args.base] = {
                "missing": set(),
                "extra": set(),
                "untranslated": [],
                "base_count": 0,
                "compare_count": 0,
                "translated_count": 0,
                "percentage": 100,
            }
        results[args.base]["base_count"] += dir_total_strings
        results[args.base]["compare_count"] += dir_total_strings
        results[args.base]["translated_count"] += dir_total_strings

        base_translations = extract_keys(base_file)

        for lang in all_langs:
            if lang == args.base:
                continue

            if lang not in results:
                results[lang] = {
                    "missing": set(),
                    "extra": set(),
                    "untranslated": [],
                    "base_count": 0,
                    "compare_count": 0,
                    "translated_count": 0,
                    "percentage": 0,
                }

            r = results[lang]
            compare_file = language_files.get(lang)

            if compare_file is None:
                # Language has no file in this directory: contributes 0 translations,
                # but its keys are not counted as missing
                missing_keys = set()
                extra_keys: Set[str] = set()
                untranslated_keys: List[str] = []
                translated_count = 0
                compare_count = 0
            else:
                missing_keys, extra_keys, _ = compare_translations(
                    base_file, compare_file, args.base, lang
                )
                compare_translations_dict = extract_keys(compare_file)
                untranslated_keys = check_untranslated(
                    base_translations, compare_translations_dict
                )
                translated_count = count_translations(compare_file, is_main_file=False)
                compare_count = len(compare_translations_dict)

            r["missing"] |= missing_keys
            r["extra"] |= extra_keys
            r["untranslated"].extend(untranslated_keys)
            r["base_count"] += len(base_translations)
            r["compare_count"] += compare_count
            r["translated_count"] += translated_count

            logger.info(
                "%s: +%d/%d translated in %s%s",
                lang,
                translated_count,
                dir_total_strings,
                os.path.basename(lang_dir),
                " (no locale file)" if compare_file is None else "",
            )

            if missing_keys:
                all_complete = False

    logger.info("%s: %d/%d (100%%)", args.base, total_strings, total_strings)

    # Calculate overall percentages once all directories are combined
    for lang, r in results.items():
        if lang == args.base:
            continue
        r["percentage"] = (
            round((r["translated_count"] / total_strings) * 100)
            if total_strings > 0
            else 0
        )
        logger.info(
            "%s: %d/%d (%d%%)",
            lang,
            r["translated_count"],
            total_strings,
            r["percentage"],
        )

    if not results:
        logger.error("No translations found")
        sys.exit(1)

    # Output JSON if requested
    if args.json:
        output_json(results, total_strings)
    else:
        # Print summary for all languages
        print_summary(results, total_strings, args.base, args.verbose)

        # Print final result
        div = "=" * 60
        print()
        if all_complete:
            print(f"{div}")
            print("RESULT: PASSED - All translations are complete")
            print(div)
        else:
            print(f"{div}")
            print("RESULT: FAILED - Missing translation keys detected")
            print(div)

    # Always exit successfully - this script is for reporting progress only
    sys.exit(0)


if __name__ == "__main__":
    main()
