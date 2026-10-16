param([ValidateSet('items')][string]$Mode='items',[int]$Width=1920,[int]$Height=1080,[string]$Output='output/demo/street-items/review-1080',[string]$Player='builds/windows-staging/TownDemo.exe')
# Native review of the 1931 street furniture in an isolated street-items-review-save.json; never touches slice-save.json.
# Runs the full street furniture review (--fresh).
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$out=[IO.Path]::GetFullPath((Join-Path $root $Output));New-Item -ItemType Directory -Path $out -Force | Out-Null
$flags='--fresh --review-street-items'
$marker='STREET_ITEMS_REVIEW_PASS'
# Player logs stay in the ignored builds/ folder; output/ keeps only frames (convert with tools/evidence_jpg.py) and results.
$logs=Join-Path $root 'builds/review/street-items/logs';New-Item -ItemType Directory -Path $logs -Force | Out-Null
$log=Join-Path $logs ((Split-Path -Leaf $out)+'.log')
$arguments='--review {0} -screen-width {1} -screen-height {2} -screen-fullscreen 0 --capture-dir "{3}" -logFile "{4}"' -f $flags,$Width,$Height,$out,$log
$process=Start-Process -FilePath (Join-Path $root $Player) -ArgumentList $arguments -PassThru
[void]$process.Handle
$deadline=[DateTime]::UtcNow.AddMinutes(8)
while(!$process.WaitForExit(1000)) {
    if(Test-Path -LiteralPath $log){$live=Get-Content -Raw -LiteralPath $log;if($live -match 'STREET_ITEMS_REVIEW_FAIL|FULL_MAP_REVIEW_FAIL|Review movement blocked|NullReferenceException|MissingReferenceException|IndexOutOfRange|InvalidOperationException'){Start-Sleep -Seconds 1;Stop-Process -Id $process.Id -ErrorAction SilentlyContinue;throw "Street items review failed: $log"}}
    if([DateTime]::UtcNow -gt $deadline){Stop-Process -Id $process.Id;throw "Street items review timed out: $log"}
}
$text=Get-Content -Raw -LiteralPath $log
if(!$text.Contains($marker) -or $text -match 'STREET_ITEMS_REVIEW_FAIL|FULL_MAP_REVIEW_FAIL|NullReferenceException|MissingReferenceException'){throw "Street items review failed: $log"}
Write-Output "STREET_ITEMS_NATIVE_PASS $Mode $out"
