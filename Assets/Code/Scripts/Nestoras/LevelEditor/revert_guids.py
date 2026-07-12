from pathlib import Path
from tqdm import tqdm
import subprocess
import sys
import re

try:
    from tqdm import tqdm
except ImportError:
    print("Missing dependency: tqdm")
    print("Install with: pip install tqdm")
    sys.exit(1)

def run(cmd):
    return subprocess.check_output(cmd, text=True, shell=True)

def main(file_path):
    file_path = str(Path(file_path).as_posix())

    print(f"Generating git diff for: {file_path}")

    diff = run(f'git diff -- "{file_path}"')

    if not diff.strip():
        print("No changes detected.")
        return

    lines = diff.splitlines()

    replacements = []

    for i in range(len(lines) - 1):
        m1 = re.match(r"-\s*guid:\s*(.+)", lines[i])
        m2 = re.match(r"\+\s*guid:\s*(.+)", lines[i + 1])

        if m1 and m2:
            old_guid = m1.group(1).strip()
            new_guid = m2.group(1).strip()
            replacements.append((new_guid, old_guid))

    print(f"Found {len(replacements)} GUID changes")

    if not replacements:
        print("No GUID replacements found.")
        return

    text = Path(file_path).read_text(encoding="utf-8")

    count = 0

    for new_guid, old_guid in tqdm(replacements, desc="Reverting GUIDs", unit="guid"):
        key = f"guid: {new_guid}"
        if key in text:
            text = text.replace(key, f"guid: {old_guid}")
            count += 1

    # Path(file_path).write_text(text, encoding="utf-8", newline="\n")
    with Path(file_path).open("w", encoding="utf-8", newline="\n") as f:
        f.write(text)

    print(f"Done. Reverted {count} GUID changes")

if __name__ == "__main__":
    if len(sys.argv) != 2:
        print("Usage: python revert_unity_guids.py <path-to-prefab-or-scene>")
        sys.exit(1)

    main(sys.argv[1])