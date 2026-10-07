"""Install the four versioned project skills, without overwriting unknown skills."""

import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil

ROOT = Path(__file__).resolve().parents[1]
SKILLS = ("wrong-date-narrative", "wrong-date-unity", "wrong-date-blender", "wrong-date-playtest")
MANIFEST = ".wrong-date-install.json"


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def install(destination, update=False):
    destination = destination.expanduser().resolve()
    pending = []
    for name in SKILLS:
        source = ROOT / "skills" / name
        frontmatter = (source / "SKILL.md").read_text(encoding="utf-8").split("---", 2)[1]
        if not re.search(rf"(?m)^name: {re.escape(name)}$", frontmatter):
            raise ValueError(f"Skill name mismatch: {name}")
        files = {p.relative_to(source).as_posix(): digest(p) for p in source.rglob("*") if p.is_file() and "__pycache__" not in p.parts}
        target = destination / name
        if source.resolve() == target.resolve() or target.is_symlink():
            raise ValueError(f"Refusing source/self or symlink install: {target}")
        if target.exists():
            installed_files = {p.relative_to(target).as_posix(): digest(p) for p in target.rglob("*")
                               if p.is_file() and p.name != MANIFEST and "__pycache__" not in p.parts}
            if installed_files == files:
                pending.append((name, source, target, files, False))
                continue
            receipt = target / MANIFEST
            if not update or not receipt.exists():
                raise ValueError(f"Existing different skill {target}; --update only updates intact project-managed installs")
            previous = json.loads(receipt.read_text(encoding="utf-8"))
            if previous.get("skill") != name or installed_files != previous.get("files"):
                raise ValueError(f"Existing install has local edits; preserve and review them: {target}")
            if set(installed_files) - set(files):
                raise ValueError(f"Update would leave removed files; review explicitly: {target}")
        pending.append((name, source, target, files, True))
    # All destinations have been checked before any file is written.
    for name, source, target, files, write in pending:
        if write:
            target.mkdir(parents=True, exist_ok=True)
            for relative in files:
                installed = target / relative
                installed.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(source / relative, installed)
            receipt = {"skill": name, "files": files}
            (target / MANIFEST).write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
        if any(digest(target / relative) != value for relative, value in files.items()):
            raise ValueError(f"Installed copy differs from its source: {name}")
        print(f"{'Installed' if write else 'Already current'}: {name} -> {target}")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    task_home = Path(os.environ.get("CODEX_HOME", str(Path.home() / ".codex")))
    parser.add_argument("--dest", type=Path, default=task_home / "skills")
    parser.add_argument("--update", action="store_true")
    args = parser.parse_args()
    try:
        install(args.dest, args.update)
    except (ValueError, OSError, IndexError) as exc:
        parser.exit(1, f"Install failed: {exc}\n")


if __name__ == "__main__":
    main()
