param(
    [Parameter(Mandatory=$false)]
    [string]$GamePath = ""
)

$ErrorActionPreference = "Stop"

$CandidateRef = "6874bc41dbed1a5efc5bbced8cbd1d85676a29ee"
$ArchiveUrl = "https://codeload.github.com/JpegIsHere/LeeZ-Mods/zip/$CandidateRef"

function Resolve-GamePath {
    param([string]$ExplicitPath)

    if (-not [string]::IsNullOrWhiteSpace($ExplicitPath)) {
        return [System.IO.Path]::GetFullPath($ExplicitPath)
    }

    $cursor = [System.IO.DirectoryInfo]$PSScriptRoot
    for ($i = 0; $i -lt 8 -and $cursor -ne $null; $i++) {
        $assembly = Join-Path $cursor.FullName "7DaysToDie_Data\Managed\Assembly-CSharp.dll"
        if (Test-Path $assembly) {
            return $cursor.FullName
        }
        $cursor = $cursor.Parent
    }

    throw "Could not locate the 7 Days To Die game folder. Re-run with -GamePath 'C:\Program Files (x86)\Steam\steamapps\common\7 Days To Die'."
}

function Find-MSBuild {
    $command = Get-Command "MSBuild.exe" -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vswhere) {
        $found = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" 2>$null | Select-Object -First 1
        if ($found -and (Test-Path $found)) {
            return $found
        }
    }

    $roots = @(
        (Join-Path ${env:ProgramFiles} "Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"),
        (Join-Path ${env:ProgramFiles} "Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"),
        (Join-Path ${env:ProgramFiles} "Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"),
        (Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe")
    )

    foreach ($candidate in $roots) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    return $null
}

$game = Resolve-GamePath $GamePath
$managed = Join-Path $game "7DaysToDie_Data\Managed"
$assemblyCSharp = Join-Path $managed "Assembly-CSharp.dll"
$mods = Join-Path $game "Mods"
$modRoot = Join-Path $mods "LeezGrowLights"
$targetDll = Join-Path $modRoot "LeezGrowLights.dll"

if (-not (Test-Path $assemblyCSharp)) {
    throw "Assembly-CSharp.dll was not found at '$assemblyCSharp'."
}

if (-not (Test-Path $modRoot)) {
    throw "LeezGrowLights mod folder was not found at '$modRoot'."
}

$runningGame = Get-Process -ErrorAction SilentlyContinue | Where-Object { $_.ProcessName -like "7DaysToDie*" }
if ($runningGame) {
    throw "7 Days To Die is running. Close the game/server before installing this test DLL."
}

$harmony = Join-Path $mods "0_TFP_Harmony\0Harmony.dll"
if (-not (Test-Path $harmony)) {
    $harmonyMatch = Get-ChildItem -Path $mods -Filter "0Harmony.dll" -Recurse -File -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($harmonyMatch) {
        $harmony = $harmonyMatch.FullName
    }
}

if (-not (Test-Path $harmony)) {
    throw "0Harmony.dll was not found under '$mods'."
}

$msbuild = Find-MSBuild
$dotnet = $null
if (-not $msbuild) {
    $dotnetCommand = Get-Command "dotnet.exe" -ErrorAction SilentlyContinue
    if ($dotnetCommand) {
        $dotnet = $dotnetCommand.Source
    }
}

if (-not $msbuild -and -not $dotnet) {
    throw "No MSBuild or dotnet SDK was found. Install Visual Studio 2022 Build Tools with the .NET desktop build tools workload, then run this installer again."
}

$workRoot = Join-Path $env:TEMP ("LeezGrowLights-MultiplayerTest-" + [Guid]::NewGuid().ToString("N"))
$zipPath = Join-Path $workRoot "source.zip"
$extractPath = Join-Path $workRoot "source"

New-Item -ItemType Directory -Path $workRoot -Force | Out-Null

try {
    Write-Host "[1/4] Downloading pinned multiplayer candidate $CandidateRef ..."
    Invoke-WebRequest -UseBasicParsing -Uri $ArchiveUrl -OutFile $zipPath

    Write-Host "[2/4] Extracting source ..."
    Expand-Archive -Path $zipPath -DestinationPath $extractPath -Force

    $project = Get-ChildItem -Path $extractPath -Filter "LeezGrowLights.csproj" -Recurse -File |
        Where-Object { $_.FullName -match "[\\/]LeezGrowLights[\\/]Source[\\/]LeezGrowLights\.csproj$" } |
        Select-Object -First 1

    if (-not $project) {
        throw "The pinned archive did not contain LeezGrowLights/Source/LeezGrowLights.csproj."
    }

    Write-Host "[3/4] Building against your installed V3.1 game assemblies ..."
    $properties = @(
        "/t:Rebuild",
        "/p:Configuration=Release",
        "/p:GameManagedPath=$managed",
        "/p:HarmonyPath=$harmony",
        "/nologo",
        "/verbosity:minimal"
    )

    if ($msbuild) {
        & $msbuild $project.FullName @properties
    }
    else {
        & $dotnet msbuild $project.FullName @properties
    }

    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE. Your installed mod DLL was NOT changed."
    }

    $sourceDirectory = Split-Path $project.FullName -Parent
    $builtDll = [System.IO.Path]::GetFullPath((Join-Path $sourceDirectory "..\LeezGrowLights.dll"))
    if (-not (Test-Path $builtDll)) {
        throw "Build reported success but '$builtDll' was not created. Your installed mod DLL was NOT changed."
    }

    Write-Host "[4/4] Installing test DLL ..."
    $backupDirectory = Join-Path $PSScriptRoot "backups"
    New-Item -ItemType Directory -Path $backupDirectory -Force | Out-Null

    if (Test-Path $targetDll) {
        $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
        $backup = Join-Path $backupDirectory ("LeezGrowLights-before-mp-" + $stamp + ".dll")
        Copy-Item -Path $targetDll -Destination $backup -Force
        Write-Host "Backup: $backup"
    }

    Copy-Item -Path $builtDll -Destination $targetDll -Force

    $builtPdb = [System.IO.Path]::ChangeExtension($builtDll, ".pdb")
    if (Test-Path $builtPdb) {
        Copy-Item -Path $builtPdb -Destination (Join-Path $modRoot "LeezGrowLights.pdb") -Force
    }

    $marker = Join-Path $PSScriptRoot "MULTIPLAYER_TEST_BUILD.txt"
    @(
        "CandidateCommit: $CandidateRef",
        "Installed: $([DateTimeOffset]::Now.ToString('o'))",
        "AssemblyCSharp: $assemblyCSharp",
        "TargetDll: $targetDll"
    ) | Set-Content -Path $marker -Encoding UTF8

    Write-Host ""
    Write-Host "SUCCESS: Multiplayer colour test candidate installed."
    Write-Host "Candidate: $CandidateRef"
    Write-Host "DLL: $targetDll"
    Write-Host ""
    Write-Host "Next gate: run host/server and a remote client with this same candidate, click the grow-light colour command once, then collect both logs."
}
finally {
    if (Test-Path $workRoot) {
        Remove-Item -Path $workRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
