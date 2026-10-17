$ErrorActionPreference = 'Stop'
# Regenerate the 1931 street-furniture kit (Windows host). Placements live in the map:
#   docs/maps/town-current/build_plan.py (StreetItems) -> python tools/check_town_map.py --write,
#   then tools/build_full_town.py, tools/plan_greenery.py and tools/build_period_streets.py.
#   1. textures (posters, enamel plates, painted-metal / granite / wood detail maps)
#   2. Blender kit -> assets/source/street-items/StreetItems.blend/.fbx + Unity copy and manifest
# Afterwards build with BuildSlice.BuildStaging and review with tools/ReviewStreetItems.ps1.
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    python -X utf8 tools/street_items/street_item_art.py
    if ($LASTEXITCODE -ne 0) { throw 'street_item_art.py failed' }
    $blender = 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe'
    if (-not (Test-Path -LiteralPath $blender)) { throw 'Update the Blender path in tools/BuildStreetItems.ps1' }
    & $blender --background --factory-startup --python-exit-code 1 --python tools/street_items/build_street_items.py -- `
        --output assets/source/street-items --unity game/Assets/Art/StreetItems --previews output/demo/street-items/blender
    if ($LASTEXITCODE -ne 0) { throw 'Blender street item generator failed' }
} finally { Pop-Location }
