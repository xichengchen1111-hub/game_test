#!/usr/bin/env python3
"""Pre-commit repository size guard (REPO-01, 2026-10-12).

Blocks a commit when the index contains
  * any file larger than 45 MB (GitHub warns at 50 MB and rejects files over 100 MB), or
  * a new or changed PNG, video or archive under output/ (evidence frames go in as JPG via
    tools/evidence_jpg.py; videos and archives stay outside Git).
Other files over 10 MB are listed as a warning only.

Activate once per clone:  git config core.hooksPath tools/git-hooks
Exception (only with the user's approval):  ALLOW_LARGE_COMMIT=1 git commit ...
"""
import os
import subprocess
import sys

HARD_LIMIT = 45 * 1024 * 1024
WARN_LIMIT = 10 * 1024 * 1024
EVIDENCE_MEDIA = ('.png', '.mp4', '.mov', '.webm', '.gif', '.zip')


def git(args, data=None):
    return subprocess.run(['git'] + args, input=data, capture_output=True, check=True).stdout


def mb(size):
    return '%.1f MB' % (size / 1048576)


def main():
    try:
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    except AttributeError:
        pass
    if os.environ.get('ALLOW_LARGE_COMMIT') == '1':
        print('pre-commit: size guard skipped (ALLOW_LARGE_COMMIT=1)')
        return 0
    changed = [p for p in git(['diff', '--cached', '--name-only', '--diff-filter=ACMRT', '-z']).split(b'\0') if p]
    if not changed:
        return 0
    wanted = set(changed)
    blobs = {}
    for entry in git(['ls-files', '-s', '-z']).split(b'\0'):
        if not entry:
            continue
        meta, path = entry.split(b'\t', 1)
        if path in wanted:
            blobs[path] = meta.split()[1]
    paths = [p for p in changed if p in blobs]
    lines = git(['cat-file', '--batch-check'], b''.join(blobs[p] + b'\n' for p in paths)).splitlines()
    errors, warnings = [], []
    for raw, line in zip(paths, lines):
        fields = line.split()
        if len(fields) < 3 or fields[1] != b'blob':
            continue
        path, size = raw.decode('utf-8', 'replace'), int(fields[2])
        if size > HARD_LIMIT:
            errors.append('%s  %s  (over 45 MB)' % (mb(size), path))
        elif path.startswith('output/') and path.lower().endswith(EVIDENCE_MEDIA):
            errors.append('%s  %s  (evidence PNG/video/archive)' % (mb(size), path))
        elif size > WARN_LIMIT:
            warnings.append('%s  %s' % (mb(size), path))
    if warnings:
        print('pre-commit: large files in this commit (allowed, please keep them rare):')
        for line in warnings:
            print('  ' + line)
    if errors:
        print('pre-commit: commit blocked by the repository size guard (AGENTS.md, REPO-01):')
        for line in errors:
            print('  ' + line)
        print('  Evidence frames: python3 tools/evidence_jpg.py <dir>  (PNG -> JPG, updates references)')
        print('  Re-captured tracked PNGs you do not need: git restore --staged --worktree <path>')
        print('  Videos and archives: keep them in builds/ or share outside Git; record the link and checksum.')
        print('  Approved exception only: ALLOW_LARGE_COMMIT=1 git commit ...')
        return 1
    return 0


if __name__ == '__main__':
    sys.exit(main())
