$ErrorActionPreference = 'Stop'
# Regenerate the D police station sources in order (Windows host):
#   1. layout contract (asserts the B10 entry of docs/maps/town-current/plan-data.json)
#   2. text resources (documents, report rules, duty-officer dialogue)
#   3. duty-officer placeholder texture (navy recolour of the approved clerk)
#   4. Blender shell/furniture -> assets/source/police/PoliceStation.blend/.fbx + Unity copy
# Textures/sounds come from tools/police/make_police_assets.py (stdlib only; run on any host,
# e.g. the Mac mini, then copy OUTPUT/Textures and OUTPUT/Sounds into game/Assets/Art/Police/).
# Afterwards build with BuildSlice.BuildStaging (or Build) and review with --review-police.
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    python -X utf8 tools/police/police_layout.py
    if ($LASTEXITCODE -ne 0) { throw 'police_layout.py failed' }
    python -X utf8 tools/police/write_police_texts.py
    if ($LASTEXITCODE -ne 0) { throw 'write_police_texts.py failed' }
    python -X utf8 tools/police/make_officer_texture.py
    if ($LASTEXITCODE -ne 0) { throw 'make_officer_texture.py failed' }
    $blender = 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe'
    if (-not (Test-Path -LiteralPath $blender)) { throw 'Update the Blender path in tools/BuildPolice.ps1' }
    & $blender --background --factory-startup --python-exit-code 1 --python tools/police/build_police_station.py -- `
        --layout assets/source/police/police-layout.json --output assets/source/police --unity game/Assets/Art/Police `
        --previews output/demo/police-station/blender
    if ($LASTEXITCODE -ne 0) { throw 'Blender police generator failed' }
} finally { Pop-Location }
