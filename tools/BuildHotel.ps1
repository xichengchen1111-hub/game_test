$ErrorActionPreference = 'Stop'
# Regenerate the A gabled old inn (B08) sources in order (Windows host):
#   1. layout contract (asserts the B08 entry of docs/maps/town-current/plan-data.json)
#   2. text resources (documents, porter/landlady dialogue) and the typeset record papers
#   3. procedural textures
#   4. Blender shell/furniture -> assets/source/hotel/Hotel.blend/.fbx + Unity copy
# Afterwards build with BuildSlice.BuildStaging (or Build; BuildSlice.RebuildHotel for the prefab only)
# and review with tools/ReviewHotel.ps1.
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    python -X utf8 tools/hotel/hotel_layout.py
    if ($LASTEXITCODE -ne 0) { throw 'hotel_layout.py failed' }
    python -X utf8 tools/hotel/write_hotel_texts.py
    if ($LASTEXITCODE -ne 0) { throw 'write_hotel_texts.py failed' }
    python -X utf8 tools/hotel/make_hotel_assets.py
    if ($LASTEXITCODE -ne 0) { throw 'make_hotel_assets.py failed' }
    $blender = 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe'
    if (-not (Test-Path -LiteralPath $blender)) { throw 'Update the Blender path in tools/BuildHotel.ps1' }
    & $blender --background --factory-startup --python-exit-code 1 --python tools/hotel/build_hotel.py -- `
        --layout assets/source/hotel/hotel-layout.json --output assets/source/hotel --unity game/Assets/Art/Hotel `
        --previews output/demo/hotel/blender
    if ($LASTEXITCODE -ne 0) { throw 'Blender hotel generator failed' }
} finally { Pop-Location }
