$ErrorActionPreference = 'Stop'
# Regenerate the E stone-barn garage (B07) sources in order (Windows host):
#   1. layout contract (asserts the B07 entry of docs/maps/town-current/plan-data.json)
#   2. text resources (documents, mechanic dialogue) and the typeset record papers
#   3. procedural textures and the mechanic placeholder recolour
#   4. Blender shell/furniture -> assets/source/garage/RepairGarage.blend/.fbx + Unity copy
# Afterwards build with BuildSlice.BuildStaging (or Build; BuildSlice.RebuildGarage for the prefab only)
# and review with tools/ReviewGarage.ps1.
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    python -X utf8 tools/garage/garage_layout.py
    if ($LASTEXITCODE -ne 0) { throw 'garage_layout.py failed' }
    python -X utf8 tools/garage/write_garage_texts.py
    if ($LASTEXITCODE -ne 0) { throw 'write_garage_texts.py failed' }
    python -X utf8 tools/garage/make_garage_assets.py
    if ($LASTEXITCODE -ne 0) { throw 'make_garage_assets.py failed' }
    python -X utf8 tools/garage/make_mechanic_texture.py
    if ($LASTEXITCODE -ne 0) { throw 'make_mechanic_texture.py failed' }
    $blender = 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe'
    if (-not (Test-Path -LiteralPath $blender)) { throw 'Update the Blender path in tools/BuildGarage.ps1' }
    & $blender --background --factory-startup --python-exit-code 1 --python tools/garage/build_garage.py -- `
        --layout assets/source/garage/garage-layout.json --output assets/source/garage --unity game/Assets/Art/Garage `
        --previews output/demo/garage/blender
    if ($LASTEXITCODE -ne 0) { throw 'Blender garage generator failed' }
} finally { Pop-Location }
