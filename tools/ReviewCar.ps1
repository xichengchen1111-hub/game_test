param([ValidateSet('car','verify')][string]$Mode='car',[int]$Width=1280,[int]$Height=720,[string]$Output='output/demo/hotel-car/review',[string]$Player='builds/car-staging/TownDemo.exe',[string]$Checkpoint='')
# Native review of the hotel car in an isolated car-review-save.json; never touches slice-save.json.
$ErrorActionPreference='Stop'
$carRoot=Split-Path -Parent $PSScriptRoot
$carOutput=[IO.Path]::GetFullPath((Join-Path $carRoot $Output));New-Item -ItemType Directory -Path $carOutput -Force | Out-Null
# Locate the player's persistentDataPath by its normal save rather than spelling the Chinese product name
# (Windows PowerShell 5.1 reads this BOM-less script as ANSI).
$saveDir=(Get-ChildItem -LiteralPath (Join-Path $env:USERPROFILE 'AppData\LocalLow\Qingchuan Studio') -Directory | Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'slice-save.json') } | Select-Object -First 1).FullName
if(!$saveDir){throw 'Player save folder not found'}
if($Mode -eq 'verify') {
    if(!$Checkpoint){throw 'verify needs -Checkpoint (car-checkpoint.json from a car review)'}
    Copy-Item -LiteralPath $Checkpoint -Destination (Join-Path $saveDir 'car-review-save.json') -Force
}
$carFlags=if($Mode -eq 'car'){'--fresh --review-car'}else{'--verify-car-save'}
$carMarker=if($Mode -eq 'car'){'CAR_REVIEW_PASS'}else{'CAR_SAVE_PASS'}
$carArgs='--review {0} -screen-width {1} -screen-height {2} -screen-fullscreen 0 --capture-dir "{3}" -logFile "{3}\player.log"' -f $carFlags,$Width,$Height,$carOutput
$carProcess=Start-Process -FilePath (Join-Path $carRoot $Player) -ArgumentList $carArgs -PassThru
[void]$carProcess.Handle
$carDeadline=[DateTime]::UtcNow.AddMinutes(6)
while(!$carProcess.WaitForExit(1000)) {
    $carLogPath=Join-Path $carOutput 'player.log'
    if(Test-Path -LiteralPath $carLogPath){$carLive=Get-Content -Raw -LiteralPath $carLogPath;if($carLive-match 'CAR_REVIEW_FAIL|NullReferenceException|MissingReferenceException'){Stop-Process -Id $carProcess.Id -ErrorAction SilentlyContinue;throw "Car review failed: $carOutput"}}
    if([DateTime]::UtcNow-gt $carDeadline){Stop-Process -Id $carProcess.Id;throw "Car review timed out: $carOutput"}
}
$carLog=Get-Content -Raw -LiteralPath (Join-Path $carOutput 'player.log')
if(!$carLog.Contains($carMarker) -or $carLog-match 'CAR_REVIEW_FAIL|NullReferenceException|MissingReferenceException'){throw "Car review failed: $carOutput"}
Write-Output "CAR_NATIVE_PASS $Mode $carOutput"
