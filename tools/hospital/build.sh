#!/usr/bin/env bash
# Regenerate the hospital (B11) from its layout contract:
#   layout contract -> Blender shell and furniture -> Unity prefab (Assets/Generated/Hospital.prefab).
# The Town scene holds the prefab as an instance, so it follows without regenerating the town.
# Change rooms, openings, furniture, lamps or people's places in tools/hospital/hospital_layout.py;
# never edit the prefab by hand. After changing the outline or outside windows, also update
# docs/maps/town-current/build_plan.py (B11) and rerun the map checks.
set -euo pipefail
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"; cd "$root"
blender="${BLENDER:-$HOME/Applications/Blender-4.5.9/Blender.app/Contents/MacOS/Blender}"
unity="${UNITY_EDITOR:-$HOME/Applications/Unity-6000.3.18f1/Unity.app/Contents/MacOS/Unity}"
mkdir -p builds/hospital/blender
python3 tools/hospital/hospital_layout.py
"$blender" --background --factory-startup --python-exit-code 1 --python tools/hospital/build_hospital.py -- \
  --layout assets/source/hospital/hospital-layout.json --output assets/source/hospital \
  --previews builds/hospital/blender > builds/hospital/blender/build.log 2>&1
grep -q HOSPITAL_BLENDER_PASS builds/hospital/blender/build.log || { echo "Blender generation failed: builds/hospital/blender/build.log" >&2; exit 1; }
rm -f assets/source/hospital/Hospital.blend1
"$unity" -batchmode -quit -projectPath game -executeMethod BuildSlice.RebuildHospital -logFile builds/hospital/prefab.log
grep -q HOSPITAL_PREFAB_PASS builds/hospital/prefab.log || { echo "Unity prefab failed: builds/hospital/prefab.log" >&2; exit 1; }
grep -E "HOSPITAL_AUTHORING_PASS|ZFIGHT_REPAIR Hospital|HOSPITAL_CAST_PENDING" builds/hospital/prefab.log
