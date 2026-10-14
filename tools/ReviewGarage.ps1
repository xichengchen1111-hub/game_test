param([ValidateSet('garage','verify')][string]$Mode='garage',[int]$Width=1920,[int]$Height=1080,[string]$Output='output/demo/garage/review-1080',[string]$Player='builds/windows-staging/TownDemo.exe')
# Native review of the E stone-barn garage (B07) in an isolated garage-review-save.json; never touches slice-save.json.
# 'garage' runs the full walkthrough (--fresh); 'verify' relaunches and checks the saved state in a new process.
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$out=[IO.Path]::GetFullPath((Join-Path $root $Output));New-Item -ItemType Directory -Path $out -Force | Out-Null
$flags=if($Mode -eq 'garage'){'--fresh --review-garage'}else{'--verify-garage-save'}
$marker=if($Mode -eq 'garage'){'GARAGE_REVIEW_PASS'}else{'GARAGE_SAVE_PASS'}
$log=Join-Path $out ($Mode+'-player.log')
$arguments='--review {0} -screen-width {1} -screen-height {2} -screen-fullscreen 0 --capture-dir "{3}" -logFile "{4}"' -f $flags,$Width,$Height,$out,$log
$process=Start-Process -FilePath (Join-Path $root $Player) -ArgumentList $arguments -PassThru
[void]$process.Handle
$deadline=[DateTime]::UtcNow.AddMinutes(8)
while(!$process.WaitForExit(1000)) {
    if(Test-Path -LiteralPath $log){$live=Get-Content -Raw -LiteralPath $log;if($live -match 'GARAGE_REVIEW_FAIL|NullReferenceException|MissingReferenceException|IndexOutOfRange|InvalidOperationException'){Start-Sleep -Seconds 1;Stop-Process -Id $process.Id -ErrorAction SilentlyContinue;throw "Garage review failed: $log"}}
    if([DateTime]::UtcNow -gt $deadline){Stop-Process -Id $process.Id;throw "Garage review timed out: $log"}
}
$text=Get-Content -Raw -LiteralPath $log
if(!$text.Contains($marker) -or $text -match 'GARAGE_REVIEW_FAIL|NullReferenceException|MissingReferenceException'){throw "Garage review failed: $log"}
Write-Output "GARAGE_NATIVE_PASS $Mode $out"
