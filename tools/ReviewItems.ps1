param([ValidateSet('items','verify-coach','verify-bench','verify-travel','verify-next-town')][string]$Mode='items',[int]$Width=1280,[int]$Height=720,[string]$Output='output/demo/rail-items/review')
$ErrorActionPreference='Stop'
$itemsRoot=Split-Path -Parent $PSScriptRoot
$itemsOutput=[IO.Path]::GetFullPath((Join-Path $itemsRoot $Output));New-Item -ItemType Directory -Path $itemsOutput -Force | Out-Null
$itemsFlags=switch($Mode){'items'{'--fresh --review-items'}'verify-bench'{'--verify-items-save --bench-checkpoint'}'verify-travel'{'--verify-items-save --travel-after-rest'}'verify-next-town'{'--verify-items-save --next-town-checkpoint'}default{'--verify-items-save'}}
$itemsMarker=if($Mode-eq 'items'){'ITEM_REVIEW_PASS'}else{'ITEM_SAVE_PASS'}
$itemsArgs='--review {0} -screen-width {1} -screen-height {2} -screen-fullscreen 0 --capture-dir "{3}" -logFile "{3}\player.log"' -f $itemsFlags,$Width,$Height,$itemsOutput
$itemsProcess=Start-Process -FilePath (Join-Path $itemsRoot 'builds/windows/TownDemo.exe') -ArgumentList $itemsArgs -PassThru
[void]$itemsProcess.Handle
$itemsDeadline=[DateTime]::UtcNow.AddMinutes(4)
while(!$itemsProcess.WaitForExit(1000)) {
    $itemsLogPath=Join-Path $itemsOutput 'player.log'
    if(Test-Path -LiteralPath $itemsLogPath){$itemsLiveLog=Get-Content -Raw -LiteralPath $itemsLogPath;if($itemsLiveLog-match 'ITEM_REVIEW_FAIL|RAIL_REVIEW_FAIL|NullReferenceException|MissingReferenceException'){Stop-Process -Id $itemsProcess.Id -ErrorAction SilentlyContinue;throw "Item review failed: $itemsOutput"}}
    if([DateTime]::UtcNow-gt $itemsDeadline){Stop-Process -Id $itemsProcess.Id;throw "Item review timed out: $itemsOutput"}
}
$itemsLog=Get-Content -Raw -LiteralPath (Join-Path $itemsOutput 'player.log')
if($itemsProcess.ExitCode-ne 0 -or !$itemsLog.Contains($itemsMarker) -or $itemsLog-match 'ITEM_REVIEW_FAIL|RAIL_REVIEW_FAIL|NullReferenceException|MissingReferenceException'){throw "Item review failed: $itemsOutput"}
Write-Output "ITEM_NATIVE_PASS $Mode $itemsOutput"
