param([switch]$AssetsOnly)
$ErrorActionPreference='Stop'
$railRepo=Split-Path -Parent $PSScriptRoot
Push-Location $railRepo
try {
    & python -X utf8 tools/rail/build_support_assets.py
    if($LASTEXITCODE -ne 0){throw 'Rail support generation failed'}
    & 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python-exit-code 1 --python tools/rail/build_rail_assets.py -- --output assets/source/rail
    if($LASTEXITCODE -ne 0){throw 'Rail source generation failed'}
    & 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python-exit-code 1 --python tools/rail/build_ticket_source.py
    if($LASTEXITCODE -ne 0){throw 'Editable ticket source generation failed'}
    Copy-Item -LiteralPath assets/source/rail/StationB.fbx -Destination game/Assets/Art/Rail/StationB.fbx
    Copy-Item -LiteralPath assets/source/rail/TrainSet.fbx -Destination game/Assets/Art/Rail/TrainSet.fbx
    & python -X utf8 tools/check_town_map.py
    if($LASTEXITCODE -ne 0){throw 'Map freshness check failed'}
    if(!$AssetsOnly){& tools/BuildDemo.ps1}
} finally {Pop-Location}
