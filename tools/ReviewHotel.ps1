param([ValidateSet('hotel','verify')][string]$Mode='hotel',[int]$Width=1920,[int]$Height=1080,[string]$Output='output/demo/hotel/review-1080',[string]$Player='builds/windows-staging/TownDemo.exe')
# Native review of the A gabled old inn (B08) in an isolated hotel-review-save.json; never touches slice-save.json.
# 'hotel' runs the full walkthrough (--fresh); 'verify' relaunches and checks the saved state in a new process.
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$out=[IO.Path]::GetFullPath((Join-Path $root $Output));New-Item -ItemType Directory -Path $out -Force | Out-Null
$flags=if($Mode -eq 'hotel'){'--fresh --review-hotel'}else{'--verify-hotel-save'}
$marker=if($Mode -eq 'hotel'){'HOTEL_REVIEW_PASS'}else{'HOTEL_SAVE_PASS'}
$log=Join-Path $out ($Mode+'-player.log')
$arguments='--review {0} -screen-width {1} -screen-height {2} -screen-fullscreen 0 --capture-dir "{3}" -logFile "{4}"' -f $flags,$Width,$Height,$out,$log
$process=Start-Process -FilePath (Join-Path $root $Player) -ArgumentList $arguments -PassThru
[void]$process.Handle
$deadline=[DateTime]::UtcNow.AddMinutes(8)
while(!$process.WaitForExit(1000)) {
    if(Test-Path -LiteralPath $log){$live=Get-Content -Raw -LiteralPath $log;if($live -match 'HOTEL_REVIEW_FAIL|Review movement blocked|NullReferenceException|MissingReferenceException|IndexOutOfRange|InvalidOperationException'){Start-Sleep -Seconds 1;Stop-Process -Id $process.Id -ErrorAction SilentlyContinue;throw "Hotel review failed: $log"}}
    if([DateTime]::UtcNow -gt $deadline){Stop-Process -Id $process.Id;throw "Hotel review timed out: $log"}
}
$text=Get-Content -Raw -LiteralPath $log
if(!$text.Contains($marker) -or $text -match 'HOTEL_REVIEW_FAIL|NullReferenceException|MissingReferenceException'){throw "Hotel review failed: $log"}
Write-Output "HOTEL_NATIVE_PASS $Mode $out"
