import argparse
import csv
import json
import re
import sys
from pathlib import Path
from typing import Any


INDEXED_KEY_PATTERN = re.compile(r"^(.*)\.(\d+)$")
TAG_PATTERN = re.compile(r"\[[^\]]*\]")
ITALIC_PATTERN = re.compile(
    r"\[i\]\[font_size=[^\]]+\](.*?)\[/font_size\]\[/i\]",
    re.DOTALL,
)
GOLD_PATTERN = re.compile(r"\[gold\](.*?)\[/gold\]", re.DOTALL)
PLAYER_PATTERN = re.compile(
    r"\[sine\]\[blue\](.*?)\[/blue\]\[/sine\]"
    r"|\[blue\]\[sine\](.*?)\[/sine\]\[/blue\]",
    re.DOTALL,
)
LANGUAGE_CODE_PATTERN = re.compile(r"^[A-Za-z0-9_-]+$")


def italic_to_plain(match: re.Match[str]) -> str:
    return f"~{match.group(1)}~"


def convert_localization(input_path: Path, output_path: Path) -> int:
    with input_path.open("r", encoding="utf-8") as input_file:
        translations: Any = json.load(input_file)

    if not isinstance(translations, dict):
        raise ValueError("The localization JSON must contain an object.")

    for key, raw_text in translations.items():
        if not isinstance(key, str) or not isinstance(raw_text, str):
            raise ValueError("Localization keys and values must all be strings.")

    previous_group = None
    row_count = 0

    with output_path.open("w", encoding="utf-8-sig", newline="") as output_file:
        writer = csv.writer(output_file)
        writer.writerow(["Key", "PlainText"])

        for key, raw_text in translations.items():
            match = INDEXED_KEY_PATTERN.fullmatch(key)
            group = match.group(1) if match else None
            index = match.group(2) if match else None

            if index == "0" and previous_group is not None and group != previous_group:
                writer.writerow([])

            plain_text = ITALIC_PATTERN.sub(italic_to_plain, raw_text)
            plain_text = GOLD_PATTERN.sub(r"#\1#", plain_text)
            plain_text = PLAYER_PATTERN.sub(
                lambda match: f"%{match.group(1) if match.group(1) is not None else match.group(2)}%",
                plain_text,
            )
            plain_text = TAG_PATTERN.sub("", plain_text)
            writer.writerow([key, plain_text])
            row_count += 1
            previous_group = group

    return row_count


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Export a localization relics.json file to a spreadsheet-friendly CSV."
    )
    parser.add_argument("language", help="Localization language code, such as jpn or eng.")
    args = parser.parse_args()

    language = args.language.strip().lower()
    if not LANGUAGE_CODE_PATTERN.fullmatch(language):
        parser.error("Language code may contain only letters, numbers, underscores, and hyphens.")

    project_root = Path(__file__).resolve().parent
    input_path = project_root / "ChattyVakuusEarring" / "localization" / language / "relics.json"
    output_path = project_root / "output" / f"{language}_relics.csv"

    if not input_path.is_file():
        print(f"[ERROR] Localization file not found: {input_path}", file=sys.stderr)
        return 1

    try:
        row_count = convert_localization(input_path, output_path)
    except (OSError, json.JSONDecodeError, ValueError) as error:
        print(f"[ERROR] Could not convert localization: {error}", file=sys.stderr)
        return 1

    print(f"Created {output_path} ({row_count} entries).")
    return 0


if __name__ == "__main__":
    sys.exit(main())
