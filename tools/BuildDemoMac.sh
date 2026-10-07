#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project_path="$repo_root/game"
default_editor="$HOME/Applications/Unity-6000.3.18f1/Unity.app/Contents/MacOS/Unity"
hub_editor="/Applications/Unity/Hub/Editor/6000.3.18f1/Unity.app/Contents/MacOS/Unity"
unity_editor="${UNITY_EDITOR:-$default_editor}"
if [[ ! -x "$unity_editor" && -x "$hub_editor" ]]; then unity_editor="$hub_editor"; fi
output_path="$repo_root/builds/macos-arm64/TownDemo.app"
log_path="$repo_root/builds/macos-arm64/build.log"

if [[ ! -x "$unity_editor" ]]; then
  echo "Unity Editor 6000.3.18f1 was not found. Set UNITY_EDITOR to its executable path." >&2
  exit 1
fi

mkdir -p "$(dirname "$log_path")"
rm -rf "$output_path"
"$unity_editor" -batchmode -quit -projectPath "$project_path" \
  -executeMethod BuildSlice.BuildMac -logFile "$log_path"

if ! grep -q "SLICE_BUILD_PASS" "$log_path" || [[ ! -d "$output_path" ]]; then
  echo "macOS build failed. See: $log_path" >&2
  exit 1
fi

echo "macOS ARM64 app built: $output_path"
