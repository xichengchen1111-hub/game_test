param(
    [ValidateSet('round-trip','verify-save','interrupt','next-town','verify-interrupt','verify-next-town')][string]$Mode='round-trip',
    [int]$Width=1280,[int]$Height=720,[string]$Output='output/demo/rail/review',[switch]$Signs
)
$ErrorActionPreference='Stop'
$railRepo=Split-Path -Parent $PSScriptRoot
$railOutput=[IO.Path]::GetFullPath((Join-Path $railRepo $Output))
New-Item -ItemType Directory -Path $railOutput -Force | Out-Null
$flags=switch($Mode){
    'round-trip' {'--fresh --review-rail'}
    'interrupt' {'--fresh --review-rail --interrupt-rail'}
    'next-town' {'--fresh --review-rail --stop-at-next-town'}
    'verify-save' {'--verify-rail-save'}
    'verify-interrupt' {'--verify-rail-save --load-interrupted'}
    'verify-next-town' {'--verify-rail-save --load-next-town'}
}
if($Signs){$flags+=' --record-rail-signs'}
$railArguments='--review {0} -screen-width {1} -screen-height {2} -screen-fullscreen 0 --capture-dir "{3}" -logFile "{3}\player.log"' -f $flags,$Width,$Height,$railOutput
$railProcess=Start-Process -FilePath (Join-Path $railRepo 'builds/windows/TownDemo.exe') -ArgumentList $railArguments -PassThru
[void]$railProcess.Handle
$railDeadline=[DateTime]::UtcNow.AddMinutes(6)
while(!$railProcess.HasExited -and [DateTime]::UtcNow -lt $railDeadline){[void]$railProcess.WaitForExit(1000)}
if(!$railProcess.HasExited){Stop-Process -Id $railProcess.Id -ErrorAction SilentlyContinue;throw "Rail review timed out: $railOutput"}
$railLog=Get-Content -Raw -LiteralPath (Join-Path $railOutput 'player.log')
$pass=switch($Mode){'round-trip' {'RAIL_REVIEW_PASS'} 'interrupt' {'RAIL_INTERRUPT_CHECKPOINT_PASS'} 'next-town' {'RAIL_NEXT_TOWN_CHECKPOINT_PASS'} default {'RAIL_SAVE_PASS'}}
if($railProcess.ExitCode -ne 0 -or !$railLog.Contains($pass) -or $railLog -match 'RAIL_REVIEW_FAIL|NullReferenceException|MissingReferenceException|referenced script.+missing') {throw "Rail review failed: $railOutput"}
if($Signs -and ($Mode -ne 'round-trip' -or !$railLog.Contains('RAIL_SIGN_REVIEW_PASS: other-town 7 signs'))){throw "Both-town sign review did not finish: $railOutput"}
Write-Output "RAIL_NATIVE_PASS $Mode $railOutput"
