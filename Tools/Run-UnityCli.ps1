# Run-UnityCli.ps1 - Safely run Unity CLI validation (compile / EditMode tests)
#
# Background: Unity's crash-recovery can spawn a "headless Unity recovery process"
# that holds Temp/UnityLockfile. The next CLI start then misreads it as
# "another Unity instance is running" and crashes, restarting the loop.
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File Tools\Run-UnityCli.ps1             # default: EditMode tests
#   powershell -ExecutionPolicy Bypass -File Tools\Run-UnityCli.ps1 -Action compile
#
# Steps:
#   1. If a WINDOWED Unity editor is open -> ask user to close it (never auto-kill)
#   2. Kill all HEADLESS Unity processes (crash/recovery residues)
#   3. Remove stale Temp/UnityLockfile
#   4. Run Unity CLI and wait; force-kill on timeout
#   5. Re-clean headless residues and stale lock so the next run starts clean

param(
    [ValidateSet("compile", "tests", "scene")][string]$Action = "tests",
    [string]$Project = "E:\Projects\Roguelite",
    [int]$TimeoutSec = 600
)

$ErrorActionPreference = "Stop"
$Project = [System.IO.Path]::GetFullPath($Project)
$LogsDir = Join-Path $Project "Logs"
$LockFile = Join-Path $Project "Temp\UnityLockfile"
$EditorBin = "C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe"

function Has-Process([int]$id) { try { Get-Process -Id $id -ErrorAction Stop | Out-Null; $true } catch { $false } }

# ---- 1/2. Clean up Unity processes ----
$allUnity = @(Get-Process Unity -ErrorAction SilentlyContinue)
$windowed = @($allUnity | Where-Object { -not [string]::IsNullOrWhiteSpace($_.MainWindowTitle) })
$headless = @($allUnity | Where-Object { [string]::IsNullOrWhiteSpace($_.MainWindowTitle) })

if ($windowed.Count -gt 0) {
    Write-Warning "A windowed Unity editor is open (PID: $($windowed.Id -join ', '))."
    Write-Warning "CLI cannot run in parallel. Please close that editor and rerun this script."
    exit 2
}
foreach ($p in $headless) {
    Write-Host "Killing headless Unity residue PID $($p.Id) ..."
    Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
}
Start-Sleep -Milliseconds 500

# ---- 3. Remove stale lock ----
if (Test-Path $LockFile) {
    Write-Host "Removing stale lock file: $LockFile"
    Remove-Item $LockFile -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 500
}

# ---- 4. Run Unity CLI ----
if (-not (Test-Path $LogsDir)) { New-Item -ItemType Directory -Path $LogsDir | Out-Null }
switch ($Action) {
    "compile" {
        $log = Join-Path $LogsDir "compile.log"
        $unityArgs = @("-batchmode", "-nographics", "-quit", "-projectPath", $Project, "-logFile", $log)
        $label = "compile"
        $resultFile = $null
    }
    "tests" {
        $log = Join-Path $LogsDir "editmode.log"
        $resultFile = Join-Path $LogsDir "editmode.xml"
        if (Test-Path $resultFile) { Remove-Item $resultFile -Force }
        $unityArgs = @("-batchmode", "-nographics", "-projectPath", $Project,
                       "-runTests", "-testPlatform", "EditMode",
                       "-testResults", $resultFile, "-logFile", $log)
        $label = "EditMode tests"
    }
    "scene" {
        $log = Join-Path $LogsDir "scene.log"
        $unityArgs = @("-batchmode", "-nographics", "-quit", "-projectPath", $Project,
                       "-executeMethod", "Roguelite.RogueliteMenu.BuildMainScene",
                       "-logFile", $log)
        $label = "Build Main scene"
        $resultFile = $null
    }
}
Write-Host "[$label] Starting Unity CLI (timeout ${TimeoutSec}s) ..."
$proc = Start-Process -FilePath $EditorBin -ArgumentList $unityArgs -PassThru -NoNewWindow

$deadline = (Get-Date).AddSeconds($TimeoutSec)
while ((Has-Process $proc.Id) -and (Get-Date) -lt $deadline) { Start-Sleep -Seconds 2 }

if (Has-Process $proc.Id) {
    Write-Warning "Unity CLI timed out (${TimeoutSec}s). Force-killing PID $($proc.Id)."
    Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    $exitCode = -1
} else {
    $exitCode = $proc.ExitCode
    Write-Host "[$label] Unity exit code: $exitCode (nonzero is not always failure, see below)"
}

# ---- 5. Re-clean residues ----
Start-Sleep -Milliseconds 800
$residual = @(Get-Process Unity -ErrorAction SilentlyContinue | Where-Object { [string]::IsNullOrWhiteSpace($_.MainWindowTitle) })
foreach ($p in $residual) {
    Write-Host "Cleaning headless Unity residue PID $($p.Id) ..."
    Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
}
if (Test-Path $LockFile) {
    Write-Host "Removing post-run lock file: $LockFile"
    Remove-Item $LockFile -Force -ErrorAction SilentlyContinue
}

# ---- Summarize ----
if ($Action -eq "tests" -and (Test-Path $resultFile)) {
    [xml]$x = Get-Content $resultFile
    $r = $x.'test-run'
    Write-Host ""
    Write-Host "===== TEST RESULTS ====="
    Write-Host "result=$($r.result)  total=$($r.total)  passed=$($r.passed)  failed=$($r.failed)"
    Write-Host "report: $resultFile"
    if ($r.result -eq "Passed") { Write-Host "==> ALL GREEN <==" } else { exit 1 }
} elseif ($Action -eq "compile") {
        $errs = @(Select-String -Path $log -Pattern "error CS" -ErrorAction SilentlyContinue)
        if ($errs.Count -gt 0) {
            Write-Host "==> $($errs.Count) compile error(s) found <=="
            $errs | Select-Object -First 10 | ForEach-Object { Write-Host $_.Line.Trim() }
            exit 1
        } else {
            Write-Host "==> COMPILE OK (no error CS) <=="
        }
    } elseif ($Action -eq "scene") {
        $errs = @(Select-String -Path $log -Pattern "error CS|Exception|Failed" -ErrorAction SilentlyContinue)
        if (Test-Path (Join-Path $Project "Assets\_Project\Scenes\Main.unity")) {
            Write-Host "==> MAIN SCENE CREATED <=="
        } else {
            Write-Host "==> MAIN SCENE NOT FOUND <=="
            $errs | Select-Object -First 10 | ForEach-Object { Write-Host $_.Line.Trim() }
            exit 1
        }
    }
exit 0