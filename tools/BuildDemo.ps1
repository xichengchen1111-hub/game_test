$ErrorActionPreference = 'Stop'
$gameRoot = Split-Path -Parent $PSScriptRoot
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.3.18f1\Editor\Unity.exe'
if (-not (Test-Path -LiteralPath $unityEditor)) { throw 'Install Unity 6000.3.18f1 or update the editor path in this script.' }
$projectPath = Join-Path $gameRoot 'game'
$outputDirectory = Join-Path $gameRoot 'output\demo'
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
$logPath = Join-Path $outputDirectory 'build.log'
$arguments = '-batchmode -quit -projectPath "{0}" -executeMethod BuildSlice.Build -logFile "{1}"' -f $projectPath, $logPath
$buildProcess = Start-Process -FilePath $unityEditor -ArgumentList $arguments -WindowStyle Hidden -PassThru
[void]$buildProcess.Handle
$buildProcess.WaitForExit()
if ($buildProcess.ExitCode -ne 0 -or -not (Select-String -LiteralPath $logPath -SimpleMatch 'SLICE_BUILD_PASS')) { throw "Build did not succeed. See $logPath" }
Write-Output (Join-Path $gameRoot 'builds\windows\TownDemo.exe')
