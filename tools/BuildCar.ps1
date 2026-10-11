param([switch]$AssetsOnly,[switch]$Staging)
# Regenerates the editable hotel car (Blender) and copies the FBX/layout contract into Unity.
# Without -AssetsOnly it then builds the Windows player (or builds/car-staging with -Staging).
$ErrorActionPreference='Stop'
$carRepo=Split-Path -Parent $PSScriptRoot
Push-Location $carRepo
try {
    & 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python-exit-code 1 --python tools/car/build_hotel_car.py -- --output assets/source/car
    if($LASTEXITCODE -ne 0){throw 'Hotel car source generation failed'}
    New-Item -ItemType Directory -Force -Path game/Assets/Art/Car | Out-Null
    Copy-Item -LiteralPath assets/source/car/HotelCar.fbx -Destination game/Assets/Art/Car/HotelCar.fbx
    Copy-Item -LiteralPath assets/source/car/hotel-car-layout.json -Destination game/Assets/Resources/HotelCarLayout.json
    if($AssetsOnly){return}
    if($Staging) {
        $unity='C:\Program Files\Unity\Hub\Editor\6000.3.18f1\Editor\Unity.exe'
        $log=Join-Path $carRepo 'builds\review\car\logs\staging-build.log'
        New-Item -ItemType Directory -Force -Path (Split-Path $log) | Out-Null
        $build=Start-Process -FilePath $unity -ArgumentList ('-batchmode -quit -projectPath "{0}" -executeMethod BuildSlice.BuildCarStaging -logFile "{1}"' -f (Join-Path $carRepo 'game'),$log) -WindowStyle Hidden -PassThru
        [void]$build.Handle;$build.WaitForExit()
        if($build.ExitCode -ne 0 -or -not (Select-String -LiteralPath $log -SimpleMatch 'SLICE_BUILD_PASS')){throw "Staging build failed. See $log"}
    } else { & tools/BuildDemo.ps1 }
} finally {Pop-Location}
