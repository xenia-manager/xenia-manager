"""Sync locale .axaml files with en.axaml as the reference.

- Adds missing keys with #NOTTRANSLATED# value in correct insertion position
- Removes stale keys (present in locale but absent from en.axaml)
- Preserves comment section headers and all key ordering from en.axaml

Usage:
    python scripts/sync_localization.py          # normal output
    python scripts/sync_localization.py -v       # verbose (per-key debug)
    python scripts/sync_localization.py -n       # dry-run (log only, no writes)
"""

import argparse
import logging
import os
import re
import sys

logger = logging.getLogger("sync_localization")

LANG_DIR = os.path.join(
    os.path.dirname(__file__),
    "..",
    "source",
    "XeniaManager",
    "Resources",
    "Language",
)
REFERENCE = "en.axaml"
LOCALES = ["hr.axaml", "pt-BR.axaml", "ru.axaml", "tr.axaml", "zh-CN.axaml"]

KEY_RE = re.compile(r'(\s*)<sys:String x:Key="([^"]*)">(.*)</sys:String>')
COMMENT_RE = re.compile(r"^\s*<!--")
FOOTER_LINE = "</ResourceDictionary>"


def setup_logging(verbose: bool) -> None:
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(errors="replace")
    level = logging.DEBUG if verbose else logging.INFO
    logging.basicConfig(
        level=level,
        format="[%(levelname)s] %(message)s",
        stream=sys.stdout,
    )


def parse_template(filepath):
    """Parse reference file into an ordered list of entry tuples.

    Each entry is one of:
        ('key', key_name, indent, en_value, raw_line)
        ('comment', raw_line)
        ('header', raw_line)
        ('footer', raw_line)
        ('blank', raw_line)
        ('other', raw_line)
    """
    logger.info("Parsing template: %s", filepath)
    with open(filepath, "r", encoding="utf-8") as f:
        lines = f.readlines()

    entries = []
    section = None
    for i, line in enumerate(lines, 1):
        stripped = line.rstrip("\r\n")
        m = KEY_RE.match(stripped)
        if m:
            entries.append(("key", m.group(2), m.group(1), m.group(3), line))
            logger.debug("  [%4d] key   %s = %s", i, m.group(2), m.group(3)[:60])
        elif COMMENT_RE.match(stripped):
            section = stripped.strip().strip("<!--").strip("-->").strip()
            entries.append(("comment", line))
            logger.debug("  [%4d] cmt   <!-- %s -->", i, section)
        elif stripped.startswith("<ResourceDictionary"):
            entries.append(("header", line))
            logger.debug("  [%4d] hdr   %s", i, stripped[:80])
        elif stripped == FOOTER_LINE:
            entries.append(("footer", line))
            logger.debug("  [%4d] ftr   %s", i, stripped)
        elif not stripped:
            entries.append(("blank", line))
        else:
            entries.append(("other", line))
            logger.debug("  [%4d] other %s", i, stripped[:80])

    logger.info(
        "Template: %d total entries, %d keys, %d sections",
        len(entries),
        sum(1 for e in entries if e[0] == "key"),
        sum(1 for e in entries if e[0] == "comment"),
    )
    return entries


def build_locale_map(filepath):
    """Return dict mapping key -> (value, raw_line) for a locale file."""
    logger.info("Reading locale: %s", os.path.basename(filepath))
    mapping = {}
    with open(filepath, "r", encoding="utf-8") as f:
        for line in f:
            m = KEY_RE.match(line.rstrip("\r\n"))
            if m:
                mapping[m.group(2)] = (m.group(3), line)
    logger.debug("  Found %d existing keys", len(mapping))
    return mapping


def rebuild(entries, locale_map, locale_name):
    """Rebuild locale content walking the en.axaml template in order.

    - Existing keys keep their original locale line (preserving formatting).
    - Missing keys are emitted with #NOTTRANSLATED# and en.axaml's indentation.
    - Stale keys (in locale but not in template) are silently dropped.
    """
    output = []
    for entry in entries:
        if entry[0] == "key":
            key, indent = entry[1], entry[2]
            if key in locale_map:
                output.append(locale_map[key][1])
                logger.debug("    keep %s = %s", key, locale_map[key][0][:60])
            else:
                output.append(
                    f'{indent}<sys:String x:Key="{key}">#NOTTRANSLATED#</sys:String>\n'
                )
        else:
            output.append(entry[-1])
    return output


def main():
    parser = argparse.ArgumentParser(description="Sync locale files with en.axaml")
    parser.add_argument(
        "-v",
        "--verbose",
        action="store_true",
        help="Show per-key debug output",
    )
    parser.add_argument(
        "-n",
        "--dry-run",
        action="store_true",
        help="Log changes without writing to files",
    )
    args = parser.parse_args()
    setup_logging(args.verbose)

    ref_path = os.path.join(LANG_DIR, REFERENCE)
    if not os.path.exists(ref_path):
        logger.error("Reference file not found: %s", ref_path)
        sys.exit(1)

    entries = parse_template(ref_path)
    ref_keys = {e[1] for e in entries if e[0] == "key"}
    total_ok = 0
    total_added = 0
    total_removed = 0

    for locale in LOCALES:
        path = os.path.join(LANG_DIR, locale)
        if not os.path.exists(path):
            logger.warning("Locale file not found, skipping: %s", locale)
            continue

        locale_map = build_locale_map(path)
        locale_keys = set(locale_map)

        stale = locale_keys - ref_keys
        missing = ref_keys - locale_keys

        if missing:
            for k in sorted(missing):
                logger.info("  + %s  (MISSING — #NOTTRANSLATED#)", k)

        if stale:
            for k in sorted(stale):
                logger.warning("  - %s  (STALE — removed)", k)

        if args.dry_run:
            logger.info(
                "[DRY-RUN] %s — %d keys, +%d added, -%d removed (not written)",
                locale,
                len(ref_keys),
                len(missing),
                len(stale),
            )
        else:
            new_content = rebuild(entries, locale_map, locale)
            with open(path, "w", encoding="utf-8") as f:
                f.writelines(new_content)

            logger.info(
                "%s — %d keys, +%d added, -%d removed",
                locale,
                len(ref_keys),
                len(missing),
                len(stale),
            )

        total_ok += 1
        total_added += len(missing)
        total_removed += len(stale)

    action = "would be" if args.dry_run else ""
    logger.info(
        "Done. %d/%d locales %s (+%d added, -%d removed)",
        total_ok,
        len(LOCALES),
        "checked (dry-run)" if args.dry_run else "synced",
        total_added,
        total_removed,
    )


if __name__ == "__main__":
    main()
